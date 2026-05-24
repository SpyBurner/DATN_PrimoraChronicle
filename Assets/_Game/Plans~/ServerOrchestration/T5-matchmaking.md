# T5 — Matchmaking Flow Plan (no code yet)

> Builds on [T4-headless-bootstrap.md](T4-headless-bootstrap.md) and the
> existing BE matchmaking scaffolding at
> `D:/UnityProjects/DATN_PrimoraChronicle_OtherSystems/TestBE/app/main.py`.
> The BE already has a queue + pairing loop; this doc lists what is missing
> (notably the Accept/Reject ready check) and how the client polls.

---

## 0. What already works

| Piece | Where | Notes |
|---|---|---|
| Queue join / status / cancel | `POST/GET/DELETE /api/matchmaking/queue` | works, JWT-gated |
| Expanding XP threshold pairing | `_try_pair_players` runs every 3 s | base 200 + 100/30 s |
| Headless notify | BE → `POST {HEADLESS}/api/server/start-session` with `X-Server-Secret` | path mismatch with BackendBridgeController — fixed in T4 §4 |
| Client polling skeleton | `MatchMakingController` exists with `Initialize/Dispose`, runner-state handlers | `AcceptMatch()` is a stub returning `Task.CompletedTask` |

---

## 1. Missing piece #1: Accept/Reject ready check

The current BE pairs two players, immediately tells the headless to spin up,
and marks both as `matched`. **There is no consent step.** Both players go
straight from `waiting` → connecting to GDS. The committee will flag this.

### 1.1 New BE states

Extend `QueueEntry.status` from `"waiting" | "matched"` to:
`"waiting" | "pending_ready" | "ready_check_passed" | "ready_check_failed" | "matched"`.

| Transition | Trigger |
|---|---|
| `waiting` → `pending_ready` | pairing loop matched two players (replaces the current direct jump to `matched`) |
| `pending_ready` → `ready_check_passed` | both players posted `POST /api/matchmaking/accept` within `ready_check_timeout_seconds` |
| `pending_ready` → `ready_check_failed` | either player posted `POST /api/matchmaking/reject` OR the timeout elapsed |
| `ready_check_passed` → `matched` | BE spawned headless successfully and got the session name back |
| `ready_check_failed` → `waiting` | for the **non-rejecting** player only (rejecter is removed from queue per `orchestration.md` §Phase 2) |

### 1.2 New BE endpoints

| Method | Path | Auth | Body | Purpose |
|---|---|---|---|---|
| `POST` | `/api/matchmaking/accept` | JWT | none | move the caller to "accepted". When BOTH accept, advance both to `ready_check_passed` and trigger headless spawn |
| `POST` | `/api/matchmaking/reject` | JWT | none | remove caller from queue, send opposing player back to `waiting` with **preserved expansion timer** (per orchestration spec) |

### 1.3 GET /status response shape change

Add a `phase` field to `QueueStatusResponse`:

```python
class QueueStatusResponse(BaseModel):
    phase: str           # waiting | pending_ready | matched | ready_check_failed
    queued_at: datetime
    opponent: Optional[OpponentInfo] = None    # populated when phase >= pending_ready
    session_name: Optional[str] = None         # populated when phase == matched
    ready_check_deadline: Optional[datetime] = None   # populated when phase == pending_ready

class OpponentInfo(BaseModel):
    user_id: UUID
    username: str
    xp_total: int
```

The opponent's `username` requires the pairing loop to also stash it (currently it stashes only `matched_user_id`).

### 1.4 Anti-pairing memory

Per the user's requirement: "with some kind of mechanic to make sure the 2
players won't see each other again in a while" after a reject. Add a
**cool-down table** keyed by sorted (user_a, user_b) pair → expires_at. Skip
pairs whose key is still in the table during `_try_pair_players`. Default
cooldown: 5 minutes. Store in-memory for now; can be moved to Redis when the
BE grows beyond one process.

---

## 2. Missing piece #2: Client polling + transitions

### 2.1 MatchMakingController extensions

Add the polling loop that doesn't exist yet. The current
`MatchMakingController.AcceptMatch()` is a stub.

```csharp
public async Task<bool> StartMatchmaking(int playerCount)
{
    _model.ApplyState(new MatchMakingStateData { Phase = MatchMakingPhase.Queuing, ... });
    await _http.Post("/api/matchmaking/queue", new { PlayerCount = playerCount });

    _pollingCts = new CancellationTokenSource();
    _ = PollLoop(_pollingCts.Token);
    return true;
}

private async Task PollLoop(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        await Task.Delay(2000, ct);
        var resp = await _http.Get<QueueStatusResponse>("/api/matchmaking/status");
        ApplyServerStatus(resp);
        if (resp.phase == "matched" || resp.phase == "ready_check_failed")
            break;
    }
}
```

`ApplyServerStatus` updates `_model` so the panel shows:
- `waiting` → spinner + queue time
- `pending_ready` → ReadyCheck modal with Accept / Reject buttons + deadline countdown
- `matched` → "Connecting to session…" then call `_network.StartSession({ Client, SessionName = resp.session_name })`
- `ready_check_failed` → toast "Match cancelled" + back to `Idle`

### 2.2 Accept / Reject methods (no longer stubs)

```csharp
public async Task AcceptMatch()
{
    await _http.Post("/api/matchmaking/accept", null);
    _model.ApplyState(new MatchMakingStateData { Phase = MatchMakingPhase.AcceptedWaitingPeer, ... });
}

public async Task RejectMatch()
{
    await _http.Post("/api/matchmaking/reject", null);
    _model.ApplyState(new MatchMakingStateData { Phase = MatchMakingPhase.Idle, ... });
    _pollingCts?.Cancel();
}
```

### 2.3 Connecting-to-session retry

The session name arrives a few seconds *before* the headless server is
listening on Photon (depending on hosting strategy — see T6). Wrap the
`StartSession(GameMode.Client, sessionName)` call in a retry loop:

```csharp
for (int attempt = 0; attempt < 10; attempt++)
{
    var ok = await _network.StartSession(new StartGameArgs {
        GameMode = GameMode.Client,
        SessionName = sessionName,
        Scene = SceneRef.FromIndex(GAMEPLAY_SCENE_BUILD_INDEX)
    });
    if (ok) return;
    await Task.Delay(2000);
}
// give up — show error to user, reset to Idle
```

Display "Connecting (attempt N/10)…" to the user during retries.

---

## 3. Schema additions

Add to `Assets/_Game/Core/Scripts/Interfaces/...MatchMaking/`:

```csharp
public enum MatchMakingPhase {
    Idle,
    Queuing,
    PendingReady,          // server has paired us, awaiting our accept/reject
    AcceptedWaitingPeer,   // we accepted, waiting for the other side
    Matched,               // both accepted, headless spawning, we're polling for session
    Connecting,            // we have session name, joining Photon
    Connected,             // runner is running and player count met
    Failed                 // reject / timeout / connect error
}

public class QueueStatusResponse {
    public string phase;
    public DateTime queued_at;
    public OpponentInfo opponent;
    public string session_name;
    public DateTime? ready_check_deadline;
}

public class OpponentInfo {
    public Guid user_id;
    public string username;
    public int xp_total;
}
```

These mirror the BE schema. Keep them under
`Core.Interfaces` so panels and controllers see the same types.

---

## 4. UI (deferred — design only)

`MatchMakingPanel` already exists for the queue. Add a `ReadyCheckPanel`:

| Element | Wired to |
|---|---|
| Opponent username + xp text | `OpponentInfo` from /status |
| Countdown timer | `ready_check_deadline` - now |
| Accept button | `MatchMakingController.AcceptMatch()` |
| Reject button | `MatchMakingController.RejectMatch()` |

Trigger: on `MatchMakingPhase.PendingReady` event from the subsystem. Hide on
any other phase change.

---

## 5. Edge cases (acceptance must address)

| Case | Required behaviour |
|---|---|
| Player closes the app while in queue | BE auto-removes after `queue_heartbeat_timeout` (default 60 s of no /status calls) — needs a `last_seen` field on `QueueEntry` |
| Player accepts then disconnects before headless ready | Treat as reject for them, requeue opponent |
| Both players in queue but BE crashes between accept and spawn | When BE restarts, queue is gone (in-memory). Clients see `404` on next /status → reset to Idle. Document this as known limitation pre-Redis. |
| Headless never reports back | BE marks both players `ready_check_failed`, requeue non-rejecter, anti-pair them for cooldown duration so they don't immediately rematch into the same failure |
| Player count >2 modes | The current pairing loop assumes 2. Extend to N by sorting waiting queue per-mode and picking the first N that fit threshold |

---

## 6. Implementation order

1. **BE** — add `phase` enum + ready-check endpoints + cool-down table.
2. **Client schemas** — update `QueueStatusResponse` + `MatchMakingPhase`.
3. **MatchMakingController** — add `PollLoop`, real `AcceptMatch/RejectMatch`.
4. **ReadyCheckPanel** — new panel + UI router entry.
5. **Connecting-retry loop** — wrap StartSession in retry.
6. **Headless integration** — T4 already lined this up: the BE calls
   `POST {headless}/api/server/start-session` when both accept; T5 just
   triggers it from the new endpoint instead of the pairing loop.

---

## 7. Out of scope (NOT to be implemented in T5)

- ELO / MMR system. Current XP-threshold pairing is fine for thesis.
- Cross-region matchmaking. Single region (`RegionCode=0`) is enough.
- Spectators. Not part of the game design.
- Persistent queue across BE restarts (needs Redis — moved to T6 hosting).

---

## 8. Open questions (to answer before implementation)

1. **Single-player mode**: do we want a "vs AI" button that bypasses
   matchmaking entirely and spawns a local self-play match? If yes, the
   client just calls `_network.StartSession(GameMode.Single)` and skips the BE
   handshake — small change, document if confirmed.
2. **Disconnect rejoin**: a player who lost connection mid-match should be
   able to rejoin within the 30 s grace from T4 §6. The BE needs to remember
   `user_id → session_name` for that window. Add to active-sessions table
   designed in T4 §7.
3. **Ranked vs casual**: orchestration doc mentions modes via `playerCount`
   only. Confirm no need for a `match_mode` field on the queue entry — if
   needed, add `mode: str` to `QueueEntry` and segment the pairing pool.
