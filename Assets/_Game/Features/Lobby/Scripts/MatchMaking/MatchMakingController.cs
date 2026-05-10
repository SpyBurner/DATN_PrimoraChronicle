using System;
using System.Threading.Tasks;
using Fusion;
using Zenject;

internal class MatchMakingController : IMatchMakingController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IMatchMakingModel _model;
    [Inject] private readonly INetworkManagerSubsystem _networkSubsystem;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;

    public void Initialize() { }
    public void Dispose() { }

    public async Task StartMatchmaking()
    {
        try
        {
            _debugLogger.Log("MatchMaking: Starting session via NetworkSubsystem");
            _model.SetStatus("Connecting to Network...");

            // Configure Fusion session args
            var args = new StartGameArgs
            {
                GameMode = GameMode.Shared,
                // SessionName = null, // Use null for random matchmaking in some modes, or a specific lobby
            };

            bool success = await _networkSubsystem.StartSession(args);

            if (success)
            {
                _debugLogger.Log("MatchMaking: Session joined successfully.");
                _model.SetStatus("Match Found!");
                
                // Start confirmation timer
                await StartTimer(5);
            }
            else
            {
                _model.SetStatus("Matchmaking failed");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"MatchMaking: StartMatchmaking failed: {ex.Message}");
            _model.SetStatus($"Error: {ex.Message}");
        }
    }

    private async Task StartTimer(int seconds)
    {
        for (int i = seconds; i >= 0; i--)
        {
            _model.SetTimer(i);
            await Task.Delay(1000);
        }
    }

    public async Task AcceptMatch()
    {
        _debugLogger.Log("MatchMaking: Match accepted, loading Gameplay scene");
        _model.SetStatus("Joining match...");
        
        // In Fusion, scene loading is often handled by the Runner, 
        // but here we follow the existing pattern of using SceneLoader if applicable,
        // or let Fusion handle it if we passed Scene to StartGameArgs.
    }

    public async Task RejectMatch()
    {
        _debugLogger.Log("MatchMaking: Match rejected, shutting down network session");
        _model.SetStatus("Match canceled");
        await _networkSubsystem.ShutdownRunner();
        await Task.Delay(1000);
        _model.SetStatus(string.Empty);
    }

    public async Task CancelMatchmaking()
    {
        try
        {
            _debugLogger.Log("MatchMaking: Canceling matchmaking");
            await _networkSubsystem.ShutdownRunner();
            _model.SetIsSearching(false);
            _model.SetIsMatchFound(false);
            _model.SetStatus(string.Empty);
            _model.SetQueuePosition(0);
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"MatchMaking: CancelMatchmaking failed: {ex.Message}");
        }
    }
}
