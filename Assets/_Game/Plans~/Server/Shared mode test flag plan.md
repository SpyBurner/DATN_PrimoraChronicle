# Shared Mode Test Flag — Implementation Instructions

**Purpose:** Allow two editors (main + ParrelSync clone) to test the full gameplay
scene without a headless server by routing matchmaking into Photon Fusion Shared Mode
behind a scripting define symbol. The real queue flow is preserved and activates when
the flag is removed.

**Flag name:** `FUSION_SHARED_TEST`

**Rule for all changes below:** Every modification must be wrapped in
`#if FUSION_SHARED_TEST` / `#else` / `#endif` blocks. Do not delete or move any
existing logic. The real code paths must remain intact inside the `#else` branch.

---

## Step 1 — Add the Scripting Define Symbol

In Unity Editor on the **main project**:
- Open **Edit → Project Settings → Player → Scripting Define Symbols**
- Add `FUSION_SHARED_TEST` to the list
- Apply

Repeat the same step in the **ParrelSync clone editor**.

This flag must be manually removed from both editors when real headless server
testing resumes.

---

## Step 2 — `MatchMakingController.cs`

### 2.1 — Rename the existing `JoinQueue` body

Rename the current full implementation inside `JoinQueue()` into a new private
method called `JoinQueueInternal()`. The signature, logic, and contents are
identical — only the method name changes.

### 2.2 — Replace `JoinQueue` with a routing method

Replace the body of the public `JoinQueue()` method with a compiler directive that:
- Under `FUSION_SHARED_TEST`: calls a new private method `JoinSharedModeSession()`
- Under `#else`: calls `JoinQueueInternal()`

### 2.3 — Implement `JoinSharedModeSession()`

Add a new private async method `JoinSharedModeSession()` with the following
behaviour:
- Set model state to `Phase = Connecting`, `Status = "[TEST] Joining shared session..."`
- Build a `StartGameArgs` with:
  - `GameMode = GameMode.Shared`
  - `SessionName = "test-shared-session"` (hardcoded fixed name so both clients
    join the same room without any coordination)
  - `PlayerCount = 2`
- Call `_networkManager.StartSession(args)` and await the result
- On failure: set model state to `Phase = Failed` with an error status prefixed
  with `[TEST]`
- On success: do nothing — `HandleRunnerStateChanged` already fires when
  `RunnerState == Running` and triggers the scene load as normal

### 2.4 — Guard `CancelMatchmaking()`

Wrap the body of `CancelMatchmaking()` in a compiler directive:
- Under `FUSION_SHARED_TEST`:
  - Cancel `_pollCts` if it exists
  - If `RunnerState == Running`, call `_networkManager.ShutdownRunner()`
  - Set model state to `Phase = Idle`, `Status = string.Empty`
- Under `#else`: existing implementation unchanged

---

## Step 3 — `NetworkSpawnCoordinator.cs`

### 3.1 — Guard `OnPlayerJoined`

Wrap the body of `OnPlayerJoined` in a compiler directive:

- Under `FUSION_SHARED_TEST`:
  - Return early if `player != runner.LocalPlayer`
  - Each client spawns only their own chess piece at the position returned by
    `GetSpawnPosition(player, runner.SessionInfo.MaxPlayers)`
  - Pass `player` as the `inputAuthority` argument to `Runner.Spawn()`
  - Store the result in `_spawnedPieces[player]`

- Under `#else`:
  - Existing server-authoritative implementation unchanged
  - (`if (!runner.IsServer) return;` guard stays in place)

`OnPlayerLeft` does not need changes — `Runner.Despawn()` works identically in
both modes.

---

## Step 4 — `BoardManager.cs`

### 4.1 — Guard the authority check in `Spawned()`

The current `Spawned()` override returns early if `!HasStateAuthority`. Wrap this
guard in a compiler directive:

- Under `FUSION_SHARED_TEST`:
  - Return early if `!Runner.IsSharedModeMasterClient`
  - This ensures only one client generates the board (the first to connect becomes
    Master Client, which is Shared Mode's equivalent of the host)

- Under `#else`:
  - Existing `if (!HasStateAuthority) return;` guard unchanged

No changes to `GenerateBoard()` or any other method.

---

## Step 5 — Verify nothing else calls `StartAsHost` or `StartAsClient`

Search the entire codebase for any remaining calls to `StartAsHost()` or
`StartAsClient()`. These methods were replaced by `JoinQueue()` in the matchmaking
plan. If any callers remain, update them to call `JoinQueue()` instead.

---

## Step 6 — Rebuild Object Table

After all code changes are saved:
- In Unity, select the `NetworkProjectConfig` asset
- Press **Rebuild Object Table** in the Inspector

This ensures the hex tile prefab and chess piece prefab are registered with Fusion
so all clients agree on which prefab is which when spawning in Shared Mode.

---

## Verification Checklist

- [ ] `FUSION_SHARED_TEST` define is present in both editors (main + ParrelSync clone)
- [ ] Pressing "Find Match" in main editor triggers `JoinSharedModeSession()` — confirm
  via log prefix `[TEST]`
- [ ] ParrelSync clone joins the same session by the same fixed session name
- [ ] Both clients load the gameplay scene
- [ ] Each client spawns its own chess piece at the correct position
- [ ] First client to connect spawns the board (MasterClient only — confirm only one
  board is generated, not two)
- [ ] Camera positions correctly for each client's local slot
- [ ] Cancel button works — runner shuts down, panel returns to Idle phase
- [ ] Removing `FUSION_SHARED_TEST` from both editors restores the original queue
  flow with no compilation errors

---

## Removing the Flag (when headless server is ready)

1. Remove `FUSION_SHARED_TEST` from **Edit → Project Settings → Player →
   Scripting Define Symbols** on both editors
2. The `#if FUSION_SHARED_TEST` blocks compile out entirely
3. `JoinQueue()` now routes to `JoinQueueInternal()` — the real queue flow
4. `NetworkSpawnCoordinator.OnPlayerJoined` uses the server-authoritative path
5. `BoardManager.Spawned()` uses `HasStateAuthority`
6. No code needs to be deleted