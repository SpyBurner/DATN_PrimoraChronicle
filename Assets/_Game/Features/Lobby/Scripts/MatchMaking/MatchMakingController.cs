using System;
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
        _networkManager.RunnerStateChanged  += HandleRunnerStateChanged;
        _networkManager.PlayerCountChanged  += HandlePlayerCountChanged;
    }

    public void Dispose()
    {
        _networkManager.RunnerStateChanged  -= HandleRunnerStateChanged;
        _networkManager.PlayerCountChanged  -= HandlePlayerCountChanged;
    }

    public async Task JoinQueue()
    {
#if FUSION_SHARED_TEST
        await JoinSharedModeSession();
#else
        await JoinQueueInternal();
#endif
    }

    private async Task JoinQueueInternal()
    {
        try
        {
            _model.ApplyState(new MatchMakingStateData {
                Phase  = MatchMakingPhase.Searching,
                Status = "Finding opponent..."
            });

            var response = await _http.Post<QueueJoinResponse, object>(
                "/api/matchmaking/queue", new { userID = _authSession.UserId });

            if (response == null)
            {
                _model.ApplyState(new MatchMakingStateData {
                    Phase  = MatchMakingPhase.Failed,
                    Status = "Could not join queue."
                });
                return;
            }

            _pollingCts = new System.Threading.CancellationTokenSource();
            _ = PollForMatch();
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

#if FUSION_SHARED_TEST
    private async Task JoinSharedModeSession()
    {
        try
        {
            _model.ApplyState(new MatchMakingStateData {
                Phase  = MatchMakingPhase.Connecting,
                Status = "[TEST] Joining shared session..."
            });

            var args = new StartGameArgs
            {
                GameMode    = GameMode.Shared,
                SessionName = "test-shared-session",
                PlayerCount = 2
            };

            bool success = await _networkManager.StartSession(args);

            if (!success)
            {
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

    public async Task PollForMatch()
    {
        var token = _pollingCts.Token;

        while (!token.IsCancellationRequested && _model.Phase.Value == MatchMakingPhase.Searching)
        {
            try
            {
                await Task.Delay(2000, token);
                if (token.IsCancellationRequested) break;

                var statusRes = await _http.Get<QueueStatusResponse>("/api/matchmaking/status");
                _debugLogger.Log($"[MatchMaking] Polling result: {statusRes?.status}, session: {statusRes?.session_name}");
                if (statusRes != null && statusRes.status == "matched")
                {
                    _model.ApplyState(new MatchMakingStateData {
                        Phase  = MatchMakingPhase.MatchFound,
                        Status = "Match found! Connecting..."
                    });
                    
                    await ConnectToSession(statusRes.session_name);
                    break;
                }
            }
            catch (TaskCanceledException) { break; }
            catch (Exception ex)
            {
                _debugLogger.LogWarning($"[MatchMaking] Polling error: {ex.Message}");
            }
        }
    }

    public async Task ConnectToSession(string sessionName)
    {
        try
        {
            _model.ApplyState(new MatchMakingStateData {
                Phase  = MatchMakingPhase.Connecting,
                Status = $"Joining session {sessionName}..."
            });

            var args = new StartGameArgs
            {
                GameMode    = GameMode.Client,
                SessionName = sessionName,
            };

            bool success = await _networkManager.StartSession(args);

            if (success)
            {
                _model.ApplyState(new MatchMakingStateData {
                    Phase  = MatchMakingPhase.Connected,
                    Status = "Connected!"
                });
            }
            else
            {
                _model.ApplyState(new MatchMakingStateData {
                    Phase  = MatchMakingPhase.Failed,
                    Status = $"Failed: {_networkManager.ErrorMessage}"
                });
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"[MatchMaking] ConnectToSession failed: {ex.Message}");
            _model.ApplyState(new MatchMakingStateData {
                Phase  = MatchMakingPhase.Failed,
                Status = $"Error: {ex.Message}"
            });
        }
    }

    private void HandleRunnerStateChanged(NetworkRunner.States state)
    {
        if (state == NetworkRunner.States.Running)
        {
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Connected,
                Status = "Connected!"
            });
            _sceneLoader.LoadNetworkedScene(_networkManager.Runner, SceneNames.GAMEPLAY);
        }

        if (state == NetworkRunner.States.Shutdown)
        {
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Idle,
                Status = string.Empty
            });
        }
    }

    private void HandlePlayerCountChanged(int count)
    {
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
        return Task.CompletedTask;
    }

    public async Task RejectMatch()
    {
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
            _debugLogger.Log("[TEST] MatchMaking: Canceling shared mode matchmaking");
            
            if (_pollingCts != null)
            {
                _pollingCts.Cancel();
                _pollingCts.Dispose();
                _pollingCts = null;
            }

            if (_networkManager.RunnerState == NetworkRunner.States.Running)
            {
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
            _debugLogger.LogError($"[TEST] MatchMaking: CancelMatchmaking failed: {ex.Message}");
        }
#else
        try
        {
            _debugLogger.Log("MatchMaking: Canceling matchmaking");
            
            if (_pollingCts != null)
            {
                _pollingCts.Cancel();
                _pollingCts.Dispose();
                _pollingCts = null;
            }

            try { await _http.Delete("/api/matchmaking/queue"); } catch { }

            await _networkManager.ShutdownRunner();
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Idle,
                Status = string.Empty
            });
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"MatchMaking: CancelMatchmaking failed: {ex.Message}");
        }
#endif
    }
}
