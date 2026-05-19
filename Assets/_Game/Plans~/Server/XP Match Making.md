# XP-Based Matchmaking — Implementation Plan

**Prerequisite:** Host Mode connection test verified per
`matchmaking_modification_plan.md` Phase 9 checklist.

**Scope:** Full vertical slice — TestBE queue endpoints + DB model, Unity client
queue flow, headless server session creation via `BackendBridge`. `NetworkManager`
and `ServerSession` internals are unchanged.

---

## How the Flow Changes

### Before (Host Mode — removed after this plan)
```
Client presses Host
    → MatchMakingController.StartAsHost()
    → NetworkManager.StartSession(GameMode.Host)
    → Scene loads
```

### After (XP Queue)
```
Client presses Find Match
    → MatchMakingController.JoinQueue()
    → TestBE POST /matchmaking/queue        (register in queue with xpTotal)
    → TestBE matchmaking loop runs          (sort by xp, pair closest)
    → TestBE POST /server/start-session     (to headless server)
    → Headless server creates Photon session
    → TestBE POST /matchmaking/notify       (push session name to both clients)
    → Clients poll GET /matchmaking/status  (receive session name)
    → MatchMakingController receives session name
    → NetworkManager.StartSession(GameMode.Client, sessionName)
    → Scene loads
```

> **Why polling instead of WebSocket from TestBE to client?**
> The client already has `HttpService` and the existing polling pattern fits the
> REST architecture of TestBE. A persistent WebSocket from TestBE to every client
> is a larger infrastructure change not justified at this stage. Polling interval
> is 2 seconds — acceptable latency for a matchmaking confirm screen.

---

## Part 1 — TestBE Changes

### Step 1.1 — In-memory queue in `matchmaking_service.py`

**File:** `services/matchmaking_service.py` (new file)

The queue is transient runtime state — not persistent business data. No DB model
or migration is needed. If TestBE restarts, all in-progress searches reset
intentionally. XP is always fetched from the DB at join time so the in-memory
entry never goes stale.

```python
import asyncio
from datetime import datetime
from uuid import UUID, uuid4
from dataclasses import dataclass, field
from typing import Dict, Optional

@dataclass
class QueueEntry:
    user_id:         UUID
    xp_total:        int
    queued_at:       datetime        = field(default_factory=datetime.utcnow)
    status:          str             = "waiting"  # "waiting" | "matched"
    session_name:    Optional[str]   = None
    matched_user_id: Optional[UUID]  = None

# Single process-lifetime dict — one entry per user
_queue: Dict[UUID, QueueEntry] = {}
_lock  = asyncio.Lock()
```

All read/write operations go through `_lock` to prevent races between the
background loop and concurrent HTTP requests:

```python
async def join_queue(user_id: UUID, xp_total: int) -> QueueEntry:
    async with _lock:
        existing = _queue.get(user_id)
        if existing and existing.status == "waiting":
            return existing          # idempotent re-join
        entry = QueueEntry(user_id=user_id, xp_total=xp_total)
        _queue[user_id] = entry
        return entry

async def get_status(user_id: UUID) -> Optional[QueueEntry]:
    async with _lock:
        return _queue.get(user_id)

async def cancel(user_id: UUID):
    async with _lock:
        _queue.pop(user_id, None)    # silent no-op if not in queue

async def notify_matched(p1_id: UUID, p2_id: UUID, session_name: str):
    async with _lock:
        for uid, other in ((p1_id, p2_id), (p2_id, p1_id)):
            if uid in _queue:
                _queue[uid].status          = "matched"
                _queue[uid].session_name    = session_name
                _queue[uid].matched_user_id = other
```

---

### Step 1.2 — Add schemas

**File:** `schemas.py`

```python
class QueueJoinResponse(BaseModel):
    status:    str       # "waiting"
    queued_at: datetime

class QueueStatusResponse(BaseModel):
    status:       str            # "waiting" | "matched"
    session_name: Optional[str] = None

class MatchNotifyRequest(BaseModel):
    # Called by headless server — server-to-server only, not client-facing
    player1UserID: UUID
    player2UserID: UUID
    sessionName:   str
```

No `QueueJoinRequest` body — the client's identity comes from their JWT token.
No `QueueCancelRequest` body — same reason, identity from JWT.
No `matchedUserID` in `QueueStatusResponse` — the client only needs `sessionName`
to join the Photon session. `matchedUserID` is internal to the server loop.

---

### Step 1.3 — Add matchmaking router

**File:** `routers/matchmaking.py` (new file)

#### `POST /matchmaking/queue` — client joins queue

- Auth required (JWT)
- Read `current_user.xpTotal` from DB — client never sends its own XP, closing
  the obvious cheat vector of inflating XP for better matchmaking
- Call `matchmaking_service.join_queue(current_user.ID, current_user.xpTotal)`
- Return `QueueJoinResponse`

```python
@router.post("/matchmaking/queue", response_model=QueueJoinResponse)
async def join_queue(current_user: User = Depends(get_current_user)):
    entry = await matchmaking_service.join_queue(current_user.ID, current_user.xpTotal)
    return QueueJoinResponse(status=entry.status, queued_at=entry.queued_at)
```

#### `GET /matchmaking/status` — client polls for match result

- Auth required (JWT)
- Call `matchmaking_service.get_status(current_user.ID)`
- If `None` → 404 (player not in queue — likely cancelled or never joined)
- Return `QueueStatusResponse`
- Client reads `status`. If `"matched"`, `session_name` is populated — client
  joins that Photon session and stops polling.

```python
@router.get("/matchmaking/status", response_model=QueueStatusResponse)
async def get_status(current_user: User = Depends(get_current_user)):
    entry = await matchmaking_service.get_status(current_user.ID)
    if entry is None:
        raise HTTPException(status_code=404, detail="Not in queue")
    return QueueStatusResponse(status=entry.status, session_name=entry.session_name)
```

#### `DELETE /matchmaking/queue` — client cancels

- Auth required (JWT)
- Call `matchmaking_service.cancel(current_user.ID)` — removes entry from dict
- Return 200

```python
@router.delete("/matchmaking/queue")
async def cancel_queue(current_user: User = Depends(get_current_user)):
    await matchmaking_service.cancel(current_user.ID)
    return {"status": "cancelled"}
```

#### `POST /matchmaking/notify` — headless server notifies match created

- **Server-to-server auth** — not JWT. Validate `X-Server-Secret` header against
  an environment variable on TestBE. Reject with 403 if missing or wrong.
- Body: `MatchNotifyRequest`
- Call `matchmaking_service.notify_matched(p1_id, p2_id, session_name)`
- Return 200

> **Why not reuse JWT for server-to-server?** The headless server has no user
> identity. A shared secret in an environment variable on both TestBE and the
> headless server is the standard pattern for internal service auth at this scale.

---

### Step 1.4 — Add matchmaking loop to `matchmaking_service.py`

The loop runs as a background task started in FastAPI's `lifespan`. It reads
directly from `_queue` (under lock) and calls `notify_headless_server` for each
valid pair. Interval: every 3 seconds.

```python
async def run_matchmaking_loop(db_session_factory):
    while True:
        await asyncio.sleep(3)
        await _try_pair_players(db_session_factory)

async def _try_pair_players(db_session_factory):
    # Read SystemConfig thresholds from DB each loop so they can be tuned live
    async with db_session_factory() as db:
        config = db.query(SystemConfig).first()
        base_threshold  = config.matchmakingBaseXpThreshold
        expansion_rate  = config.matchmakingXpExpansionRate
        expansion_secs  = config.matchmakingExpansionInterval

    # Snapshot waiting entries under lock, then release before doing I/O
    async with _lock:
        waiting = sorted(
            [e for e in _queue.values() if e.status == "waiting"],
            key=lambda e: e.xp_total
        )

    i = 0
    while i < len(waiting) - 1:
        p1, p2    = waiting[i], waiting[i + 1]
        xp_diff   = abs(p1.xp_total - p2.xp_total)
        wait_secs = (datetime.utcnow() - p1.queued_at).total_seconds()
        threshold = base_threshold + (wait_secs // expansion_secs) * expansion_rate

        if xp_diff <= threshold:
            await _notify_headless_server(p1, p2)
            i += 2    # both matched, skip
        else:
            i += 1    # p1 stays, try next pair next loop

async def _notify_headless_server(p1: QueueEntry, p2: QueueEntry):
    session_name = f"session_{uuid4().hex[:8]}"
    payload = {
        "SessionName":   session_name,
        "Player1UserId": str(p1.user_id),
        "Player2UserId": str(p2.user_id),
    }
    try:
        async with httpx.AsyncClient() as client:
            resp = await client.post(
                f"{HEADLESS_SERVER_URL}/start-session",
                json=payload,
                headers={"X-Server-Secret": SERVER_SECRET},
                timeout=5.0
            )
        if resp.status_code == 200:
            # Headless server accepted — update in-memory queue
            await notify_matched(p1.user_id, p2.user_id, session_name)
        else:
            # Leave both as "waiting" — retried next loop
            logger.error(f"[Matchmaking] Headless server rejected start-session: {resp.status_code}")
    except Exception as e:
        logger.error(f"[Matchmaking] Failed to reach headless server: {e}")
```

`HEADLESS_SERVER_URL` and `SERVER_SECRET` come from environment variables.

---

### Step 1.5 — Add columns to `SystemConfig`

**File:** `models.py`

Add to `SystemConfig`:
```python
matchmakingBaseXpThreshold  = Column(Integer, default=200)
matchmakingXpExpansionRate   = Column(Integer, default=100)
matchmakingExpansionInterval = Column(Integer, default=30)  # seconds
```

---

## Part 2 — Headless Server Changes (Unity)

### Step 2.1 — Add `MatchNotifyRequest` outbound call to `BackendBridgeController`

**File:** `BackendBridgeController.cs`

After the headless server successfully starts a Fusion session, it must tell TestBE
which session name was assigned and which players are matched, so TestBE can update
both in-memory queue entries to `status = "matched"`.

Add to `IBackendBridgeSubsystem`:
```csharp
Task NotifyMatchCreatedAsync(string sessionName, string player1UserId, string player2UserId);
```

Implementation in `BackendBridgeController`:
```csharp
public async Task NotifyMatchCreatedAsync(
    string sessionName, string player1UserId, string player2UserId)
{
    await _http.Post("/matchmaking/notify", new {
        player1UserID = player1UserId,
        player2UserID = player2UserId,
        sessionName   = sessionName
    });
}
```

Called from `ServerSessionController.StartSession()` after
`_networkManager.StartSession(args)` returns `true`:

```csharp
if (success)
{
    // Notify TestBE — both clients will now see status == "matched"
    await _backendBridge.NotifyMatchCreatedAsync(
        cmd.SessionName, cmd.Player1UserId, cmd.Player2UserId);

    _model.ApplyState(new ServerSessionStateData {
        ActiveSessionName = cmd.SessionName,
        IsRunning         = true
    });
}
```

---

### Step 2.2 — Add `Player1UserId` and `Player2UserId` to `StartSessionCommand`

**File:** `StartSessionCommand.cs`

These are already planned in the original `server_subsystems_implementation_plan.md`
but confirm they are present:

```csharp
public class StartSessionCommand
{
    public string SessionName    { get; set; }
    public string Player1UserId  { get; set; }
    public string Player2UserId  { get; set; }
    public int    RegionCode     { get; set; }
}
```

TestBE already sends these in the body of `POST /server/start-session`.

---

## Part 3 — Unity Client Changes

### Step 3.1 — Add queue DTOs

**File:** `Assets/Features/Lobby/Scripts/MatchMaking/MatchMakingDTOs.cs` (new)

Mirror the TestBE schemas on the client side for JSON serialization:

```csharp
[Serializable]
public class QueueJoinResponse
{
    public string entryID;
    public string status;
    public string queuedAt;
}

[Serializable]
public class QueueStatusResponse
{
    public string status;       // "waiting" | "matched" | "cancelled"
    public string sessionName;  // null while waiting
    public string matchedUserID;
}
```

---

### Step 3.2 — Add `MatchMakingPhase` values

**File:** `MatchMakingPhase.cs`

Add two new phases:

```csharp
public enum MatchMakingPhase
{
    Idle,
    Searching,      // in queue, polling
    MatchFound,     // status == "matched", about to connect
    Connecting,     // StartGame(GameMode.Client) in progress
    Connected,      // RunnerState == Running
    Cancelled,
    Failed
}
```

`Searching` is the new state between pressing "Find Match" and receiving
`status == "matched"` from polling. The panel shows a spinner + cancel button
during this phase.

---

### Step 3.3 — Replace `StartAsHost` / `StartAsClient` with `JoinQueue` in `MatchMakingController`

**File:** `MatchMakingController.cs`

Remove `StartAsHost()` and `StartAsClient(string sessionName)`. Replace with:

```csharp
public async Task JoinQueue()
{
    try
    {
        _model.ApplyState(new MatchMakingStateData {
            Phase  = MatchMakingPhase.Searching,
            Status = "Finding opponent..."
        });

        // Register in TestBE queue
        var response = await _http.Post<QueueJoinResponse>(
            "/matchmaking/queue", new { userID = _authSession.UserId });

        if (response == null)
        {
            _model.ApplyState(new MatchMakingStateData {
                Phase  = MatchMakingPhase.Failed,
                Status = "Failed to join queue"
            });
            return;
        }

        // Start polling loop
        await PollForMatch();
    }
    catch (Exception ex)
    {
        _debugLogger.LogError($"[MatchMaking] JoinQueue failed: {ex.Message}");
        _model.ApplyState(new MatchMakingStateData {
            Phase  = MatchMakingPhase.Failed,
            Status = $"Error: {ex.Message}"
        });
    }
}
```

Note: `IHttpServiceSubsystem` and `IAuthSessionSubsystem` are now injected into
`MatchMakingController` — cross-subsystem dependencies, belong in the controller
per the architecture rule.

---

### Step 3.4 — Add `PollForMatch` to `MatchMakingController`

**File:** `MatchMakingController.cs`

```csharp
private CancellationTokenSource _pollCts;

private async Task PollForMatch()
{
    _pollCts = new CancellationTokenSource();

    try
    {
        while (!_pollCts.Token.IsCancellationRequested)
        {
            await Task.Delay(2000, _pollCts.Token);

            var status = await _http.Get<QueueStatusResponse>("/matchmaking/status");

            if (status == null) continue;

            if (status.status == "matched")
            {
                await ConnectToSession(status.sessionName);
                return;
            }

            if (status.status == "cancelled")
            {
                _model.ApplyState(new MatchMakingStateData {
                    Phase  = MatchMakingPhase.Cancelled,
                    Status = "Queue cancelled"
                });
                return;
            }

            // Still "waiting" — update timer display
            _model.ApplyState(new MatchMakingStateData {
                Phase  = MatchMakingPhase.Searching,
                Status = "Finding opponent...",
                Timer  = _model.Timer.Value + 2f
            });
        }
    }
    catch (TaskCanceledException)
    {
        // Normal cancellation via CancelQueue()
    }
}

private async Task ConnectToSession(string sessionName)
{
    _model.ApplyState(new MatchMakingStateData {
        Phase  = MatchMakingPhase.MatchFound,
        Status = "Match found! Connecting..."
    });

    var args = new StartGameArgs {
        GameMode    = GameMode.Client,
        SessionName = sessionName,
    };

    bool success = await _networkManager.StartSession(args);

    if (!success)
    {
        _model.ApplyState(new MatchMakingStateData {
            Phase  = MatchMakingPhase.Failed,
            Status = $"Connection failed: {_networkManager.ErrorMessage}"
        });
    }
    // On success: HandleRunnerStateChanged fires → scene loads (Step 3.3 of
    // matchmaking_modification_plan.md — unchanged)
}
```

---

### Step 3.5 — Replace `CancelMatchmaking` implementation

**File:** `MatchMakingController.cs`

```csharp
public async Task CancelMatchmaking()
{
    // 1. Stop the polling loop
    _pollCts?.Cancel();

    // 2. Tell TestBE to remove from queue
    await _http.Delete("/matchmaking/queue");

    // 3. Shut down runner if somehow already connecting
    if (_networkManager.RunnerState == NetworkRunner.States.Running)
        await _networkManager.ShutdownRunner();

    _model.ApplyState(new MatchMakingStateData {
        Phase  = MatchMakingPhase.Idle,
        Status = string.Empty
    });
}
```

---

### Step 3.6 — Update `IMatchMakingSubsystem` and `MatchMakingSubsystem`

**File:** `IMatchMakingSubsystem.cs`

Replace:
```csharp
Task StartAsHost();
Task StartAsClient(string sessionName);
```
With:
```csharp
Task JoinQueue();
```

Keep `CancelMatchmaking`, `AcceptMatch`, `RejectMatch`.

**File:** `MatchMakingSubsystem.cs`

```csharp
public Task JoinQueue() => _controller.JoinQueue();
```

Remove `StartAsHost` and `StartAsClient` delegations.

---

### Step 3.7 — Update `MatchMakingPanel`

**File:** `MatchMakingPanel.cs`

Remove `_hostButton`, `_joinButton`, `_sessionNameInput`. Replace with single
`_findMatchButton`.

```csharp
[SerializeField] private Button _findMatchButton;
// keep: _cancelButton, _statusText, _timerText
// remove: _hostButton, _joinButton, _sessionNameInput, _acceptButton, _rejectButton
```

Wire:
```csharp
_findMatchButton?.onClick.AddListener(OnFindMatch);

private void OnFindMatch() => _matchMaking.JoinQueue();
```

Update `UpdateVisuals`:
```csharp
private void UpdateVisuals(MatchMakingPhase phase)
{
    bool isIdle       = phase == MatchMakingPhase.Idle;
    bool isSearching  = phase == MatchMakingPhase.Searching
                     || phase == MatchMakingPhase.MatchFound
                     || phase == MatchMakingPhase.Connecting;

    _findMatchButton.gameObject.SetActive(isIdle);
    _cancelButton.gameObject.SetActive(isSearching);
    _timerText.gameObject.SetActive(isSearching);
}
```

---

## Part 4 — `IHttpServiceSubsystem` — confirm `Delete` method exists

**File:** `IHttpServiceSubsystem.cs`

`CancelMatchmaking` calls `_http.Delete("/matchmaking/queue")`. Confirm the
`HttpService` exposes a `Delete(url)` method. If not, add:

```csharp
Task<T> Delete<T>(string endpoint);
Task Delete(string endpoint);
```

Implemented in `HttpServiceController` using the same `UnityWebRequest` pattern
as `Post` and `Get`, with `UnityWebRequest.Delete(url)`.

---

## Part 5 — New Injected Dependencies in `MatchMakingController`

| Dependency | Why |
|---|---|
| `IHttpServiceSubsystem _http` | Queue join, status poll, cancel |
| `IAuthSessionSubsystem _authSession` | Read `UserId` for queue join body |
| `INetworkManagerSubsystem _networkManager` | Already present — unchanged |
| `ISceneLoaderSubsystem _sceneLoader` | Already present — unchanged |
| `IBattleSetupSubsystem _battleSetup` | Already present — `PlayerCount` for future server-side validation |

All are cross-subsystem dependencies — all belong in `MatchMakingController`,
not in `MatchMakingSubsystem`.

---

## Part 6 — Files Changed Summary

### TestBE
| File | Change |
|---|---|
| `models.py` | Add 3 columns to `SystemConfig` only — no new table |
| `schemas.py` | Add `QueueJoinResponse`, `QueueStatusResponse`, `MatchNotifyRequest` |
| `routers/matchmaking.py` | **New** — 4 endpoints: join, status, cancel, notify |
| `services/matchmaking_service.py` | **New** — in-memory queue dict, lock, background loop, XP pairing logic |
| `main.py` | Register new router; start matchmaking loop in `lifespan` |

### Headless Server (Unity)
| File | Change |
|---|---|
| `StartSessionCommand.cs` | Confirm `Player1UserId`, `Player2UserId` present |
| `IBackendBridgeSubsystem.cs` | Add `NotifyMatchCreatedAsync` |
| `BackendBridgeController.cs` | Implement `NotifyMatchCreatedAsync` |
| `ServerSessionController.cs` | Call `NotifyMatchCreatedAsync` after session starts |

### Unity Client
| File | Change |
|---|---|
| `MatchMakingDTOs.cs` | **New** — `QueueJoinResponse`, `QueueStatusResponse` |
| `MatchMakingPhase.cs` | Add `Searching`, `MatchFound` phases |
| `IMatchMakingSubsystem.cs` | Replace `StartAsHost`/`StartAsClient` with `JoinQueue` |
| `IMatchMakingController.cs` | Same replacement |
| `MatchMakingController.cs` | Full replacement of queue logic; add `IHttpServiceSubsystem`, `IAuthSessionSubsystem` injections; add `PollForMatch`, `ConnectToSession`, updated `CancelMatchmaking` |
| `MatchMakingSubsystem.cs` | Delegate `JoinQueue`; remove old delegations |
| `MatchMakingPanel.cs` | Replace Host/Join buttons with single Find Match button; update `UpdateVisuals` |

---

## Part 7 — Verification Checklist

### TestBE
- [ ] `POST /matchmaking/queue` returns `QueueJoinResponse` with `status = "waiting"`
- [ ] Duplicate queue join while `status == "waiting"` returns existing entry, no duplicate
- [ ] Client XP is always read from DB — sending a manipulated body has no effect
- [ ] Matchmaking loop pairs two players within `baseXpThreshold` XP correctly
- [ ] XP threshold expands every `matchmakingExpansionInterval` seconds
- [ ] `POST /matchmaking/notify` updates both in-memory entries to `status = "matched"`
- [ ] `GET /matchmaking/status` returns `session_name` after match is made
- [ ] `DELETE /matchmaking/queue` removes entry from in-memory dict
- [ ] TestBE restart clears all queue state — no stale entries carried over
- [ ] Server-to-server notify endpoint rejects requests without correct `X-Server-Secret`

### Headless Server
- [ ] `StartSession` command received from TestBE loop → Fusion session created
- [ ] `NotifyMatchCreatedAsync` called with correct `sessionName` and both user IDs
- [ ] Both clients' in-memory queue entries show `status = "matched"` immediately after notification

### Unity Client
- [ ] Pressing "Find Match" → `status` text shows "Finding opponent..."
- [ ] Timer increments every 2 seconds while polling
- [ ] Cancel button stops polling and hits `DELETE /matchmaking/queue`
- [ ] After match found: `MatchMakingPhase.Connecting` → scene loads on both clients
- [ ] Two clients with XP within threshold are paired before two clients far apart
- [ ] If TestBE is unreachable: `Phase = Failed`, error shown, no crash
- [ ] Cancelling mid-connect (runner already started) shuts down runner cleanly

---

## Part 8 — Known Limitations at This Stage

| Limitation | Acceptable now? | Future fix |
|---|---|---|
| Polling every 2s — not instant notification | Yes — acceptable for a matchmaking screen | Replace with SSE or WebSocket push from TestBE when scale requires |
| No timeout if TestBE loop never pairs the player | No — add a max wait (e.g. 3 minutes) in `PollForMatch`, then auto-cancel | Part of the poll loop exit conditions |
| Headless server IP is hardcoded in TestBE | Yes — single server for now | Service discovery or config endpoint when multi-server |
| No re-queue if headless server fails to start session | Add retry count in TestBE notify logic | Low priority until server stability is proven |