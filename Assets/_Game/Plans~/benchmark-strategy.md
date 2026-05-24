# Primora Chronicle — Benchmark Strategy

> Companion to the Vietnamese committee-feedback notes in `benchmark plan.md`. This
> document defines **what we measure, how we collect it, and which chart each
> measurement produces** for the thesis defense. The fusion-2 selection theorem,
> game-design balancing, and ML/AI ablation reasoning are explicitly **out of
> scope** here.

The CSV log schema from the DebugLogger upgrade
(`TIMESTAMP_MS, LOG_CODE, CLASS, MESSAGE, NETWORK_LATENCY_MS`) is the single source
of truth. Every benchmark below names the `LOG_CODE`(s) it consumes, so the data
pipeline is grep-then-aggregate, not bespoke per metric.

---

## 0. Why these benchmarks (mapping to committee questions)

| Committee question (from `benchmark plan.md`) | Benchmarks that answer it |
|---|---|
| Q3 — Evaluate AI move time / win-rate / logic | §1 AI Decision Latency · §2 AI Strength · §3 Determinism / Logic Correctness |
| Q5 — Why Photon Fusion 2 for a turn-based game | §4 Network Latency · §5 Tick / Render Stability · §6 Reconciliation Failures |
| Q6 — Manual + automatic testing, system requirement | §7 Functional Suite · §8 Frame-time / GC · §9 Memory & Startup |
| Q8 — AI must explain its parameters | §1.3 Score-component decomposition (table per turn) |

Everything else (Q1, Q2, Q4, Q7) is design rationale, not measurement — skip.

---

## 1. AI Decision Latency

**Goal:** prove that the host-authoritative `ParanoidMinimaxAI` finishes one turn
within the design budget (target: median ≤ 200 ms, p95 ≤ 600 ms on the reference
machine) across realistic board states.

### 1.1 Method

Instrument `ParanoidMinimaxAI.StartNextCombatTurn()` (and the public entrypoint
the host calls when the current unit's owner is AI) with:

```
T0 = stopwatch start
... full search ...
T1 = stopwatch stop
_logger.Log("BENCH_AI_DECIDE", "ParanoidMinimaxAI",
            $"unit={unitId} branches={branches} depth={depth} score={final}",
            0);
_logger.Log("BENCH_AI_DECIDE_MS", "ParanoidMinimaxAI",
            $"unit={unitId} ms={ms:F1}", 0);
```

Use two log codes intentionally — the second is parsed numerically, the first
holds the contextual breakdown (so charts can group by board complexity).

### 1.2 Scenarios

Run the same fixed deck for both sides; vary only board state. Each scenario =
≥ 100 turns sampled, 5 seeds to spread coin-toss ties.

| Scenario | Setup | What it stresses |
|---|---|---|
| **S1 — empty board** | Round 1, 1 unit each | search depth baseline |
| **S2 — mid-game** | 3 units each, mixed tile effects | branching factor |
| **S3 — saturated** | 5 units each, all tiles owned, 4 active status effects | worst-case fanout |
| **S4 — lethal turn** | Position where mate-in-1 exists | best-case (alpha-beta cutoff) |

### 1.3 Outputs (charts)

| Chart | X axis | Y axis | Source log code |
|---|---|---|---|
| **Box plot** of turn decision time per scenario | scenario | ms | `BENCH_AI_DECIDE_MS` |
| **CDF** of decision time across all turns | ms | %-turns ≤ x | `BENCH_AI_DECIDE_MS` |
| **Stacked bar** of score decomposition (Pressure / Distance / TileEffects) | turn # | weighted score | `BENCH_AI_DECIDE` parsed (the `score=` field is the sum; emit one extra `BENCH_AI_SCORE` row per term with the term's value to make stacked bars trivial) |
| **Scatter** of branches vs ms | branches | ms | `BENCH_AI_DECIDE` + `BENCH_AI_DECIDE_MS` joined on turn # |

The stacked-bar chart directly answers Q8 — the panel can see why the AI picked
move X (it has the highest summed component score, broken down).

### 1.4 Acceptance gate

The thesis can claim "real-time playable AI" if S1–S3 medians stay under 250 ms.
S4 should be near-zero. If S3 p95 > 1 s the search is too deep — reduce ply
before defense.

---

## 2. AI Strength (Win-Rate)

**Goal:** show the AI is non-trivial (better than random) and roughly balanced
against a second AI configuration. Pure measurement; balancing decisions belong
to playtesting (out of scope per the committee notes).

### 2.1 Method

Provide a headless self-play harness. Reuses the dedicated-server entrypoint
from T4 (`-bench=selfplay -rounds=N -ai=A:B`) so no UI thread runs.

For each match:

```
_logger.Log("BENCH_MATCH_END", "MatchHarness",
            $"seed={seed} winner={winnerPlayerId} turns={turns} elapsed_ms={ms}",
            0);
```

### 2.2 Matchups

| Matchup ID | A vs B | Sample size | Purpose |
|---|---|---|---|
| **M1** | ParanoidMinimax vs Random | 500 matches | sanity floor (target ≥ 95% win) |
| **M2** | ParanoidMinimax vs Greedy-1-ply | 500 matches | shows lookahead value |
| **M3** | ParanoidMinimax (P1) vs same AI (P2) | 1000 matches | first-player advantage |
| **M4** | ParanoidMinimax tuned-weights vs default | 500 matches | weight-sensitivity (optional) |

### 2.3 Outputs (charts)

| Chart | Notes |
|---|---|
| **Bar chart** — win-rate per matchup with 95% CI (Wilson interval) | The CI matters more than the point estimate — committee will ask for confidence |
| **Histogram** — match length (turns) per matchup | Shows whether the AI snowballs or grinds |
| **Scatter** — match duration ms vs turns | Confirms decision-time grows linearly with match length |

### 2.4 Acceptance gate

M1 ≥ 95%. M3 within `0.5 ± 0.05` (no severe first-player bias). If M3 is off,
report the bias rather than hiding it — it's an interesting finding.

---

## 3. Determinism / Logic Correctness

**Goal:** prove host-authoritative resolution produces the same outcome on
client and host (no desync), and that damage / status / skill rules match the
rulebook for a curated set of fixtures.

### 3.1 Method — desync probe

In `Render() → PushState()` of every NetworkView, after the client applies the
authoritative state, hash the relevant fields and log:

```
_logger.Log("BENCH_STATE_HASH", "<NetworkView class>",
            $"frame={tick} hash={localHash:X8}", networkLatencyMs);
```

Then dump the host's hash too. Diff host vs client logs by `frame`. Any
mismatch is a desync.

### 3.2 Method — rule fixtures

Write a fixture-driven test (NUnit, runs in batchmode):

```
[TestCase("burning_tick_deals_5_then_expires")]
[TestCase("barkskin_ward_reduces_15_then_breaks")]
[TestCase("decay_speed_floor")]
[TestCase("verdant_evolution_at_4_stacks")]
[TestCase("rooted_unit_skips_move")]
[TestCase("death_anchor_subtract")]
[TestCase("persistent_unit_survives_clear")]
[TestCase("queue_speed_desc_hp_asc_tie_coin")]
public void Rule(string fixtureFile) { ... }
```

Each fixture file is a JSON board snapshot + expected post-condition. Runtime
loads the snapshot, executes one tick of the relevant pipeline, asserts the
post-condition. Drop fixtures under `Assets/Tests/RuleFixtures/`.

For every assertion log:

```
_logger.Log("BENCH_RULE_OK" | "BENCH_RULE_FAIL", "RuleHarness",
            $"id={fixtureId} expected={x} actual={y}", 0);
```

### 3.3 Outputs (charts)

| Chart | Notes |
|---|---|
| **Pass/fail matrix** (heatmap) — rule × build version | grows over time, useful to show stability across commits |
| **Desync count over time** — line per build | should trend to 0 |

### 3.4 Acceptance gate

Rule pass-rate = 100% at release. Desync count = 0 over 50 networked matches.

---

## 4. Network Latency (Photon Fusion 2)

**Goal:** measure round-trip time and one-way state propagation latency on the
actual Fusion stack so we can justify "real-time networking, even for a
turn-based game" with numbers.

### 4.1 Method

Two log codes:

| Code | Where | Value |
|---|---|---|
| `BENCH_RPC_RTT` | Client RPC handler | client sends `T0 = Time.realtimeSinceStartup` in RPC args, server echoes, client computes RTT |
| `BENCH_STATE_PROP_MS` | Each `PushState()` callsite | host's send timestamp travels in a `[Networked] long LastEditMs`; client subtracts on receive |

Pre-existing `NETWORK_LATENCY_MS` column carries the dominant value so the chart
script can just average it.

### 4.2 Scenarios

| Scenario | Setup |
|---|---|
| **N1 — same LAN** | host + client on same machine (loopback) |
| **N2 — regional cloud** | host on Photon cloud relay, client at home |
| **N3 — degraded** | inject `clumsy` (Win) / `tc qdisc` (Linux) 100 ms RTT + 2% loss |

### 4.3 Outputs (charts)

| Chart |
|---|
| **CDF** of RTT per scenario |
| **Line chart** RTT over time during a match (spot spikes around combat phase) |
| **Histogram** state-prop latency per NetworkView (Board / Combat / Unit / GameState) |

### 4.4 Acceptance gate

N1 < 5 ms, N2 < 80 ms median, N3 plays end-to-end without functional failures.

---

## 5. Tick / Render Stability

**Goal:** show Fusion's fixed-tick simulation stays at its configured rate
under realistic load, and that client render frame time stays under 16.7 ms
(60 fps) on the reference machine.

### 5.1 Method

Add a per-frame log every N frames (N = 60 to keep the file lean):

```
_logger.Log("BENCH_FRAME", "GameplayLoop",
            $"frame={Time.frameCount} dt_ms={Time.unscaledDeltaTime*1000:F2}", 0);
```

For tick stability, in `FixedUpdateNetwork()` of `GameStateNetworkView` (already
the master clock per `Plans~/Debug/guideline.md`):

```
_logger.Log("BENCH_TICK", "GameStateNetworkView",
            $"tick={Runner.Tick} dt_ms={Runner.DeltaTime*1000:F2}", 0);
```

### 5.2 Outputs (charts)

| Chart |
|---|
| **Histogram** of frame dt_ms across a match — flag the >16.7 ms tail |
| **Line chart** tick dt_ms over time — should be a near-perfect horizontal line at the configured tick |

### 5.3 Acceptance gate

p99 frame ≤ 16.7 ms during gameplay (excluding the first 60 frames of scene load).
Tick dt_ms within ±5% of nominal across the entire match.

---

## 6. Reconciliation Failures

**Goal:** count how often the client's predicted state had to be corrected by
the server (Fusion does this transparently). High counts = network choppy or
prediction model wrong.

### 6.1 Method

Hook `ChangeDetector.DetectChanges` paths and count corrections per
`[Networked]` field. Aggregate per minute:

```
_logger.Log("BENCH_RECONCILE", "<view>",
            $"field={fieldName} count={n}", 0);
```

### 6.2 Outputs

Stacked bar per field per minute. Spikes = bad.

### 6.3 Acceptance gate

< 5 corrections per minute under N2 conditions (§4.2).

---

## 7. Functional Suite (Manual + Automatic)

**Goal:** answer Q6 — show we have both kinds of testing.

### 7.1 Automated (NUnit in batchmode)

- The §3.2 rule fixtures.
- Unit-level tests for `HexPatternResolver`, `BoardSubsystem.GetTilesInRange`,
  `DamagePipeline` aggregation order.
- Smoke test that loads each scene under `EditModeTests` and asserts no
  exceptions in `Awake` / `OnEnable`.

CI runs them via `Unity.exe -batchmode -runTests -testResults results.xml`.
Parse `results.xml` → log:

```
BENCH_TEST_RUN, NUnitHarness, "total=N passed=P failed=F skipped=S duration_ms=ms"
```

### 7.2 Manual checklist

A short markdown checklist (`Plans~/manual-checklist.md`, separate doc) that an
external grader can run in one sitting (≤ 30 min). Group by feature panel.
**Output: pass-rate per build.**

### 7.3 Outputs (charts)

| Chart |
|---|
| **Pass-rate over commits** for both automated and manual suites |
| **Test runtime** trendline (catches accidental slow tests) |

---

## 8. Frame-time & GC Allocation

**Goal:** prove the game doesn't hitch from GC. The Unity Profiler API does
this without leaving Play mode.

### 8.1 Method

Use `UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()` and
`Profiler.GetMonoUsedSizeLong()` sampled every 5 s:

```
BENCH_GC, ProfilerSampler, "allocMB=12.4 monoMB=8.1 fps_inst=58.2"
```

### 8.2 Outputs

| Chart |
|---|
| **Line** mono memory over match duration |
| **Annotated line** of GC spikes vs FPS dips (proves correlation or lack of one) |

### 8.3 Acceptance gate

Mono memory growth ≤ 5 MB over a 30-minute session (no leak).

---

## 9. Memory & Startup

**Goal:** report the "System Requirement" deliverable the committee asked about
(Q6). Static measurement; do once per release.

| Metric | How |
|---|---|
| Cold-start time to Login scene | Profiler timeline, single run |
| Cold-start to Gameplay scene (host) | Profiler timeline |
| Headless server cold-start to "ready for connections" | Process stderr timestamps from T4 build |
| Build size (Player + Server) | `ls -la` after `BuildPlayer` |
| Peak RAM during match (Player + Server) | Profiler / OS task manager sampled every 5 s |

Document as a single table in the thesis appendix.

---

## 10. Data Pipeline & Chart Generation

### 10.1 Collection

The DebugLogger writes one CSV per session to
`Application.persistentDataPath/Logs/<timestamp>.csv`. For batch runs, point the
harness to a known directory via env var `PRIMORA_LOG_DIR` (add this — small
change to `DebugLogger`).

### 10.2 Aggregation

Single Python script (`tools/bench/aggregate.py`) reads every CSV in
`PRIMORA_LOG_DIR`, filters by `LOG_CODE`, and emits one `.parquet` per metric
family. Pandas does the rest.

Suggested layout:

```
tools/bench/
  aggregate.py            # CSV -> parquet per LOG_CODE prefix
  charts/
    ai_latency.py         # § 1 charts
    ai_winrate.py         # § 2
    network_latency.py    # § 4
    frame_stability.py    # § 5
    gc.py                 # § 8
    rule_fixtures.py      # § 3
  README.md               # how to run; expected output PNGs
```

Output PNGs go into the thesis `Report/figures/` directory ready for LaTeX.

### 10.3 Reproducibility

Every chart caption in the thesis cites:

```
fig X.Y — source: tools/bench/charts/<script>.py · seed: <seed> · build: <git sha>
```

Make `aggregate.py` print the git sha and seed it found in the logs so the
caption can be auto-generated.

---

## 11. What to do *first* (suggested execution order)

1. Add the env var `PRIMORA_LOG_DIR` override to `DebugLogger` — 5-line change.
2. Wire `BENCH_FRAME`, `BENCH_TICK`, `BENCH_STATE_HASH` log codes (cheap,
   already-running call sites). § 5 + § 3.1 yield data with zero new harness.
3. Build the headless self-play harness for § 2 (depends on T4 server work —
   defer until T4 lands).
4. Write rule fixtures (§ 3.2) one per session — pure NUnit, no Unity runtime.
5. Add AI instrumentation **only after** the `ParanoidMinimaxAI` is checked in.
   (Currently absent from the repo — see `Grep ParanoidMinimax` → no files.)
6. Network scenarios (§ 4) last — they need a stable build to run reliably.

---

## 12. Anti-goals (do NOT measure these)

- **Subjective game balance.** Playtest stat — committee explicitly said skip.
- **Card draw RNG fairness.** Believed-correct property, not in scope.
- **Player engagement / session length.** Out of thesis scope.
- **Server scaling / matchmaker throughput.** Belongs to T5 / T6 future work,
  not this defense.
