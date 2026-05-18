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

    public async Task StartAsHost()
    {
        try
        {
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Connecting,
                Status = "Starting session..."
            });

            var args = new StartGameArgs
            {
                GameMode    = GameMode.Host,
                PlayerCount = _battleSetup.PlayerCnt,
                SessionName = GenerateSessionName(),
                IsVisible   = true,
                IsOpen      = true,
            };

            bool success = await _networkManager.StartSession(args);

            if (success)
            {
                _model.ApplyState(new MatchMakingStateData
                {
                    Phase  = MatchMakingPhase.Connected,
                    Status = "Waiting for opponent..."
                });
            }
            else
            {
                _model.ApplyState(new MatchMakingStateData
                {
                    Phase  = MatchMakingPhase.Failed,
                    Status = $"Failed: {_networkManager.ErrorMessage}"
                });
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"[MatchMaking] StartAsHost failed: {ex.Message}");
            _model.ApplyState(new MatchMakingStateData
            {
                Phase  = MatchMakingPhase.Failed,
                Status = $"Error: {ex.Message}"
            });
        }
    }

    private string GenerateSessionName()
        => $"session_{Guid.NewGuid().ToString().Substring(0, 8)}";

    public async Task StartAsClient(string sessionName)
    {
        try
        {
            _model.ApplyState(new MatchMakingStateData
            {
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
                _model.ApplyState(new MatchMakingStateData
                {
                    Phase  = MatchMakingPhase.Connected,
                    Status = "Connected!"
                });
            }
            else
            {
                _model.ApplyState(new MatchMakingStateData
                {
                    Phase  = MatchMakingPhase.Failed,
                    Status = $"Failed: {_networkManager.ErrorMessage}"
                });
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"[MatchMaking] StartAsClient failed: {ex.Message}");
            _model.ApplyState(new MatchMakingStateData
            {
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
            _sceneLoader.LoadScene(SceneToken.Gameplay);
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
        try
        {
            _debugLogger.Log("MatchMaking: Canceling matchmaking");
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
    }
}
