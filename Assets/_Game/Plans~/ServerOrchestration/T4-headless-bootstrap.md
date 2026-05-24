# T4 — Headless Server Bootstrap Plan (no code yet)

> Companion to [orchestration.md](orchestration.md). That doc describes the
> high-level four-phase flow; this doc lists the **concrete code changes
> required** to make it run, and the editor/host-client path that must keep
> working.

Branched from `refactor/T3-debuglogger-rollout`. Implementation will land on a
future `feat/T4-headless-server` branch.

---

## 0. Current state (what exists, what is missing)

| Piece | Status | Notes |
|---|---|---|
| `Core/Bootstrapper.cs` | exists, always loads `_nextSceneName` (Account) | needs branch for `Application.isBatchMode` |
| `NetworkManagerController.StartSession()` | works for host + client | already sets `Runner.ProvideInput = !Application.isBatchMode` |
| `BackendBridgeController` | listens on `http://*:7070` POST `/start-session` (only in batchmode) | port + path mismatch with BE (see §5) |
| BE (TestBE) `/api/server/start-session` target | BE points at `http://headless_server:8080/api/server/start-session` | path + port differ from BackendBridgeController |
| BE matchmaking loop | exists (`_try_pair_players`) | works but is missing the Accept/Reject ready-check (T5) |
| Auth gating | every Lobby endpoint requires `mock_jwt_*` | headless build cannot auth — needs a parallel `X-Server-Secret` path |
| `ICardLoadingManagerSubsystem` | loads card data via authed endpoint | headless build needs a no-auth route OR boot-time JSON injection |
| `GameplayNetworkCoordinator` spawn flow | already gracefully waits for both players | safe to reuse |

---

## 1. Bootstrapper changes

Replace `Bootstrapper.Start()` body with a mode branch.

```csharp
private async void Start()
{
    var args = HeadlessArgsParser.Parse(System.Environment.GetCommandLineArgs());

    if (Application.isBatchMode || args.ForceHeadless)
    {
        Debug.Log("[Bootstrapper] Headless build detected — entering server path.");
        await EnterHeadlessServerPath(args);
        return;
    }

    Debug.Log("[Bootstrapper] Interactive build — entering normal client path.");
    await Initialize();
    await _sceneLoader.LoadScene(_nextSceneName);
    await _uiManager.ShowDefaultScreenForScene(_nextSceneName);
}
```

`HeadlessArgsParser` reads:

| CLI flag | Meaning | Default |
|---|---|---|
| `-headless` | force headless even if not batchmode (for editor testing) | false |
| `-session <name>` | session name. If absent, headless waits on `/start-session` HTTP call. | none |
| `-region <int>` | Photon region (matches BE `RegionCode`) | 0 |
| `-port <int>` | NetworkRunner port (Fusion picks if absent) | 0 |
| `-bench=selfplay` | run benchmark self-play instead of waiting for clients (§8) | off |
| `-rounds=<int>` | self-play match count | 1 |

**Where to put `HeadlessArgsParser`:** `Assets/_Game/Core/Scripts/Helper/HeadlessArgsParser.cs`. Pure static, no Unity types beyond `string[]`. Returns a `HeadlessArgs` POCO.

**Important:** keep editor host/client path intact. If `-headless` is *not*
passed and `Application.isBatchMode` is false, do nothing different — that
preserves the entire current Login → Lobby → Gameplay flow for development.

---

## 2. EnterHeadlessServerPath flow

```csharp
private async Task EnterHeadlessServerPath(HeadlessArgs args)
{
    // 1) Card data — without auth.
    await _cardLoading.LoadFromUnauthedEndpoint();   // see §3

    // 2) Wait for session name. Two paths:
    //    A) CLI gave -session <name>           → go straight to gameplay.
    //    B) No CLI session → poll BackendBridgeController for PendingStartSession
    string sessionName = args.SessionName;
    if (string.IsNullOrEmpty(sessionName))
        sessionName = await _backendBridge.WaitForStartSessionAsync(grace: TimeSpan.FromMinutes(2));

    if (string.IsNullOrEmpty(sessionName))
    {
        Debug.LogError("[Bootstrapper] No session received within grace period. Quitting.");
        Application.Quit(2);
        return;
    }

    // 3) Load Gameplay scene first, THEN start the runner as host.
    await _sceneLoader.LoadScene(SceneNames.GAMEPLAY);

    var result = await _network.StartSession(new StartGameArgs
    {
        GameMode      = GameMode.Server,         // not Host — no local player
        SessionName   = sessionName,
        Scene         = SceneRef.FromIndex(GAMEPLAY_SCENE_BUILD_INDEX),
        PlayerCount   = args.ExpectedPlayers,
        SceneManager  = Runner.GetComponent<NetworkSceneManagerDefault>(),
    });

    if (!result)
        Application.Quit(3);

    // 4) From here, GameplayNetworkCoordinator + NetworkGameplayManager
    //    drive the match. Self-shutdown logic in §6.
}
```

### Why `GameMode.Server` not `Host`
The Photon Fusion docs:
- `Host`: behaves as a peer that ALSO runs simulation. Counts toward
  `SessionInfo.PlayerCount`. **Wrong for dedicated.**
- `Server`: simulation runs but the host is not a player. Players join as
  `Client` and get their own `PlayerRef`.

The existing host-client editor flow uses `GameMode.Host`. Headless must use
`GameMode.Server`. The flag becomes a CLI arg or is decided inside
`EnterHeadlessServerPath` only.

---

## 3. Card data without auth

`CardLoadingManagerController` currently calls
`/api/game-data/cards` which (looking at `main.py` line 386) is **already
unauthenticated** in the BE — it does not depend on `get_current_user_id`. So
the only change is in the client: call the same endpoint in headless mode but
**skip the JWT header**. Add `LoadFromUnauthedEndpoint()` on
`ICardLoadingManagerSubsystem` that wraps the existing fetch with
`Authorization` header omitted.

If `/api/game-data/cards` is ever gated, add a server-secret variant
`/api/server/game-data/cards` that accepts `X-Server-Secret` (mirrors the BE's
existing `/api/matchmaking/notify` pattern).

---

## 4. BackendBridgeController — alignments with BE

The current code has three concrete bugs vs the BE expectation:

| Bug | Current | Should be |
|---|---|---|
| Port | `7070` | Read from CLI `-be-port` (default 8080 to match BE's `HEADLESS_SERVER_URL`) |
| Path | `POST /start-session` | `POST /api/server/start-session` (BE calls this exact path) |
| Auth | none — accepts any caller | check `X-Server-Secret` header against env var `SERVER_SECRET` |
| Payload shape | unknown — script reads as `StartSessionCommand` | match BE payload `{SessionName, Player1UserId, Player2UserId, RegionCode}` |

Add a `WaitForStartSessionAsync(TimeSpan grace)` method that polls
`_model.PendingStartSession` (already exists) and returns the session name or
null after timeout.

Also add a `ReportSessionReadyAsync(string sessionName)` that calls the BE so
clients can transition from polling to "connecting" state — see T5.

---

## 5. Editor parity — keep host/client testable

Three things must not break:

1. `LobbyMainController` / `BattleSetupController` still call
   `_network.StartSession(... GameMode.Host)` for in-editor playtest. This
   path runs in interactive mode → `Bootstrapper` never enters the headless
   branch → no regression.
2. `BackendBridgeController.Initialize()` already early-exits when
   `!Application.isBatchMode` — preserve that.
3. Add a debug menu option (`Tools/Primora/Run Headless Locally`) that
   launches a second editor process with `-headless -session devtest` so the
   matchmaking → headless connect flow can be smoke-tested without a real
   build. Document this in `Plans~/ServerOrchestration/local-dev.md` (new file
   to create when implementing).

---

## 6. Self-shutdown lifecycle

`NetworkGameplayManager` already handles match-end reporting via
`BackendBridgeController.ReportMatchResultAsync`. Augment with:

| Trigger | Action |
|---|---|
| **No-show** | Spawn a coroutine that waits `_noShowGraceSeconds` (default 90 s) after server boot. If `Runner.SessionInfo.PlayerCount < ExpectedPlayers`, call `BackendBridge.ReportMatchAbandoned(sessionName, reason="no_show")` then `Application.Quit(10)`. |
| **Mid-game disconnect** | `OnPlayerLeft` starts a 30 s reconnection grace. If unmet, mark winner as the remaining player, write result, `Application.Quit(0)`. |
| **Clean finish** | Already implemented. After `ReportMatchResultAsync`, queue an `Application.Quit(0)` (currently the server stays up — bug). |
| **Failsafe hard-kill** | The BE Orchestrator polls server liveness (out of scope here — covered in T5). |

---

## 7. BE-side changes needed

Touching `D:/UnityProjects/DATN_PrimoraChronicle_OtherSystems/TestBE/app/main.py`:

| Change | Why |
|---|---|
| `HEADLESS_SERVER_URL` → from env var `HEADLESS_SERVER_BASE`. Default `http://localhost:8080` for dev | docker compose already uses an env var; this just formalizes |
| Add `/api/server/report-match-end` POST (already exists implicitly via `/api/matches/result` — confirm shape) | clean teardown signal |
| Add `/api/server/abandon-match` POST | no-show, mid-game forfeit |
| Add an in-memory `active_sessions: Dict[session_name, dict]` tracking spawn time, last-heartbeat, expected players | feeds the §8 failsafe |
| Replace `_notify_headless_server()` mid-call with the **provisioning strategy** chosen for T6 (process spawn vs. container API vs. fly.io machines API) | see T6 |

---

## 8. Self-play benchmark mode (used by T2 §2)

`-bench=selfplay -rounds=N` should:

1. Skip the `WaitForStartSessionAsync` step.
2. Load Gameplay scene.
3. Start a `GameMode.Single` runner (or `Server` with two synthetic AI
   `PlayerRef`s).
4. Wire `ParanoidMinimaxAI` to BOTH players' `currentActor`.
5. On `MatchEnded`, log `BENCH_MATCH_END` and either start the next match or
   `Application.Quit(0)` after N matches.

Implement after T4 base lands. Until then leave the flag parsed but log a
warning.

---

## 9. Test plan (before merging T4)

| Test | Pass criterion |
|---|---|
| Build server-target standalone, run with `-headless -session devtest`. Open editor as client, join `devtest`. | Both clients see board, match plays to completion. Server quits with code 0. |
| Same as above but no `-session` flag. Manually `curl POST localhost:8080/api/server/start-session ...` | Server accepts, then both clients can join. |
| Editor → Play (host mode) without any new flags | Unchanged behaviour (no regression). |
| Headless without `SERVER_SECRET` env set | Server rejects HTTP `/api/server/start-session` with 403. |
| `-bench=selfplay -rounds=2 -PRIMORA_LOG_DIR=/tmp/logs` | Two matches play to completion in batchmode, CSVs land in `/tmp/logs/`. |

---

## 10. Known follow-ups discovered during T3

- `NetworkManagerController` declares the logger field as `_debugLogger`
  (not `_logger`) so the T3 migration script's regex skipped it. Six call
  sites in that file still use the old single-arg signature; **they will
  fail to compile under the new IDebugLogger interface**. Fix this in the
  first T4 commit before any new feature work lands, otherwise headless build
  cannot link.

  Quick patch: search-replace `_debugLogger.Log("[NetworkController] X")`
  with `_debugLogger.Log("LOG_NETWORK", nameof(NetworkManagerController), "X")`
  in [NetworkManagerController.cs](../../Core/Scripts/UI/SubSystem/Network/NetworkManagerController.cs).
  Same regex pattern; the migration tool's `--field` argument would be a
  one-line improvement.
