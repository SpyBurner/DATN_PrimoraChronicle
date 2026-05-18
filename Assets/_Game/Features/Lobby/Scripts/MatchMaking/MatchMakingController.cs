using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Zenject;

internal class MatchMakingController : IMatchMakingController
{
    [Inject] private readonly IDebugLogger             _debugLogger;
    [Inject] private readonly IMatchMakingModel        _model;
    [Inject] private readonly INetworkManagerSubsystem _networkManager;
    [Inject] private readonly ISceneLoaderSubsystem    _sceneLoader;
    [Inject] private readonly IBattleSetupSubsystem    _battleSetup;

    [Inject] private readonly IHttpServiceSubsystem    _http;
    [Inject] private readonly IAuthSessionSubsystem    _authSession;

    private System.Threading.CancellationTokenSource _pollingCts;

    public void Initialize()
    {
        _debugLogger.Log("[MatchMaking] Initializing Controller. Subscribing to NetworkManager events.");
        _networkManager.RunnerStateChanged  += HandleRunnerStateChanged;
        _networkManager.PlayerCountChanged  += HandlePlayerCountChanged;
    }

    public void Dispose()
    {
        _debugLogger.Log("[MatchMaking] Disposing Controller. Unsubscribing from NetworkManager events.");
        _networkManager.RunnerStateChanged  -= HandleRunnerStateChanged;
        _networkManager.PlayerCountChanged  -= HandlePlayerCountChanged;
    }

    public async Task JoinQueue()
    {
#if FUSION_SHARED_TEST
        _debugLogger.Log("[MatchMaking] JoinQueue triggered: Active FUSION_SHARED_TEST compilation target.");
        await JoinSharedModeSession();
#else
        _debugLogger.Log("[MatchMaking] JoinQueue triggered: Standard production matchmaking path.");
#endif
    }

#if FUSION_SHARED_TEST
    private async Task JoinSharedModeSession()
    {
        try
        {
            _debugLogger.Log("[MatchMaking] JoinSharedModeSession: Setting up shared test session 'test-shared-session'");
            _model.ApplyState(new MatchMakingStateData {
                Phase  = MatchMakingPhase.Connecting,
                Status = "[TEST] Joining shared session..."
            });

            var args = new StartGameArgs
            {
                GameMode    = GameMode.Shared,
                SessionName = "test-shared-session",
                PlayerCount = 2,
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    { "ai_count", SessionProperty.Make(1) }
                }
            };

            _debugLogger.Log("[MatchMaking] JoinSharedModeSession: Calling NetworkManager.StartSession (Shared Mode)");
            bool success = await _networkManager.StartSession(args);
            _debugLogger.Log($"[MatchMaking] JoinSharedModeSession: NetworkManager.StartSession complete. Success={success}");

            if (!success)
            {
                _debugLogger.LogError($"[MatchMaking] JoinSharedModeSession: StartSession failed. ErrorMessage={_networkManager.ErrorMessage}");
                _model.ApplyState(new MatchMakingStateData {
                    Phase  = MatchMakingPhase.Failed,
                    Status = $"[TEST] Failed to join shared session: {_networkManager.ErrorMessage}"
                });
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"[MatchMaking] JoinSharedModeSession failed: {ex.Message}");
            _model.ApplyState(new MatchMakingStateData {
                Phase  = MatchMakingPhase.Failed,
                Status = $"[TEST] Error: {ex.Message}"
            });
        }
    }
#endif
    private void HandleRunnerStateChanged(NetworkRunner.States state)
    {
        _debugLogger.Log($"[MatchMaking] HandleRunnerStateChanged: NetworkRunner State transitioned to {state}");
        
        if (state == NetworkRunner.States.Running)
        {
            _debugLogger.Log("[MatchMaking] HandleRunnerStateChanged: Runner is Running. Commencing LoadNetworkedScene to GAMEPLAY.");
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Connected,
                Status = "Connected!"
            });
            _sceneLoader.LoadNetworkedScene(_networkManager.Runner, SceneNames.GAMEPLAY);
        }

        if (state == NetworkRunner.States.Shutdown)
        {
            _debugLogger.Log("[MatchMaking] HandleRunnerStateChanged: Runner is Shutdown. Returning model state to Idle.");
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Idle,
                Status = string.Empty
            });
        }
    }

    private void HandlePlayerCountChanged(int count)
    {
        _debugLogger.Log($"[MatchMaking] HandlePlayerCountChanged: Session Player Count changed to {count}");
        _model.ApplyState(new MatchMakingStateData
        {
            Phase             = _model.Phase.Value,
            Status            = _model.Status.Value,
            Timer             = _model.Timer.Value,
            PlayerJoinedCount = count
        });
    }

    public Task AcceptMatch()
    {
        _debugLogger.Log("[MatchMaking] AcceptMatch called (stub logic, returning completed task).");
        return Task.CompletedTask;
    }

    public async Task RejectMatch()
    {
        _debugLogger.Log("[MatchMaking] RejectMatch called. Command shutting down NetworkRunner.");
        await _networkManager.ShutdownRunner();
        _model.ApplyState(new MatchMakingStateData
        {
            Phase  = MatchMakingPhase.Idle,
            Status = string.Empty
        });
    }

    public async Task CancelMatchmaking()
    {
#if FUSION_SHARED_TEST
        try
        {
            _debugLogger.Log("[TEST] [MatchMaking] CancelMatchmaking: Shared test mode cancellation initiated.");
            
            if (_pollingCts != null)
            {
                _debugLogger.Log("[TEST] [MatchMaking] CancelMatchmaking: Cancelling status polling task.");
                _pollingCts.Cancel();
                _pollingCts.Dispose();
                _pollingCts = null;
            }

            if (_networkManager.RunnerState == NetworkRunner.States.Running)
            {
                _debugLogger.Log("[TEST] [MatchMaking] CancelMatchmaking: NetworkRunner is active. Requesting shutdown.");
                await _networkManager.ShutdownRunner();
            }

            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Idle,
                Status = string.Empty
            });
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"[TEST] [MatchMaking] CancelMatchmaking failed: {ex.Message}");
        }
#else
        try
        {
            _debugLogger.Log("[MatchMaking] CancelMatchmaking: Standard matchmaking cancellation initiated.");
            
            if (_pollingCts != null)
            {
                _debugLogger.Log("[MatchMaking] CancelMatchmaking: Cancelling status polling task.");
                _pollingCts.Cancel();
                _pollingCts.Dispose();
                _pollingCts = null;
            }

            _debugLogger.Log("[MatchMaking] CancelMatchmaking: Calling DELETE /api/matchmaking/queue...");
            try { await _http.Delete("/api/matchmaking/queue"); } catch { }

            _debugLogger.Log("[MatchMaking] CancelMatchmaking: Shutting down NetworkRunner.");
            await _networkManager.ShutdownRunner();
            
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Idle,
                Status = string.Empty
            });
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"[MatchMaking] CancelMatchmaking failed: {ex.Message}");
        }
#endif
    }
}
