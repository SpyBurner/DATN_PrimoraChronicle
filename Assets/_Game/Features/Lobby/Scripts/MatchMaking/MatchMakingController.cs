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

    private void HandleRunnerStateChanged(NetworkRunner.States state)
    {
        _debugLogger.Log($"[MatchMaking] HandleRunnerStateChanged: NetworkRunner State transitioned to {state}");
        
        if (state == NetworkRunner.States.Running && _networkManager.PlayerCount >= _battleSetup.PlayerCnt)
        {
            _debugLogger.Log("[MatchMaking] HandleRunnerStateChanged: Runner is Running and player count met. Commencing LoadNetworkedScene to GAMEPLAY.");
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
