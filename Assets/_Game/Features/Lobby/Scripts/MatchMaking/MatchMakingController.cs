using System;
using System.Threading.Tasks;
using Zenject;

internal class MatchMakingController : IMatchMakingController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IMatchMakingModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;

    public void Initialize() { }
    public void Dispose() { }

    public async Task StartMatchmaking()
    {
        try
        {
            _debugLogger.Log("MatchMaking: Starting matchmaking");
            _model.SetIsSearching(true);
            _model.SetStatus("Searching for opponent...");

            var response = await _httpService.Post<MatchMakingResponse>("https://api.example.com/matchmaking/start", new { });

            if (response != null)
            {
                _model.SetQueuePosition(response.queuePosition);
                _debugLogger.Log($"MatchMaking: Queue position {response.queuePosition}");
                
                // Simulate wait for match
                await Task.Delay(3000);
                
                _debugLogger.Log("MatchMaking: Match found, loading Gameplay scene");
                _model.SetStatus("Match found!");
                await _sceneLoader.LoadScene("Gameplay");
            }
            else
            {
                _model.SetIsSearching(false);
                _model.SetStatus("Matchmaking failed");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"MatchMaking: StartMatchmaking failed: {ex.Message}");
            _model.SetIsSearching(false);
            _model.SetStatus($"Error: {ex.Message}");
        }
    }

    public async Task CancelMatchmaking()
    {
        try
        {
            _debugLogger.Log("MatchMaking: Canceling matchmaking");
            await _httpService.Post("https://api.example.com/matchmaking/cancel", new { });
            _model.SetIsSearching(false);
            _model.SetStatus(string.Empty);
            _model.SetQueuePosition(0);
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"MatchMaking: CancelMatchmaking failed: {ex.Message}");
        }
    }
}

[System.Serializable]
internal class MatchMakingResponse
{
    public int queuePosition;
}
