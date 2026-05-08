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
            _model.SetIsMatchFound(false);
            _model.SetStatus("Searching for opponent...");

            var response = await _httpService.Post<MatchMakingResponse, EmptyRequest>("/api/matchmaking/start", new EmptyRequest());

            if (response != null)
            {
                _model.SetQueuePosition(response.queuePosition);
                _debugLogger.Log($"MatchMaking: Queue position {response.queuePosition}");
                
                // Simulate wait for match
                await Task.Delay(3000);
                
                _debugLogger.Log("MatchMaking: Match found! Waiting for confirmation.");
                _model.SetIsSearching(false);
                _model.SetIsMatchFound(true);
                _model.SetStatus("Match Found!");
                
                // Start confirmation timer
                await StartConfirmationTimer(10);
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

    private async Task StartConfirmationTimer(int seconds)
    {
        for (int i = seconds; i >= 0; i--)
        {
            if (!_model.IsMatchFound.Value) break;
            _model.SetConfirmationTimer(i);
            await Task.Delay(1000);
        }

        if (_model.IsMatchFound.Value)
        {
            _debugLogger.Log("MatchMaking: Confirmation timeout");
            await RejectMatch();
        }
    }

    public async Task AcceptMatch()
    {
        _debugLogger.Log("MatchMaking: Match accepted, loading Gameplay scene");
        _model.SetIsMatchFound(false);
        _model.SetStatus("Joining match...");
        await _sceneLoader.LoadScene("Gameplay");
    }

    public async Task RejectMatch()
    {
        _debugLogger.Log("MatchMaking: Match rejected");
        _model.SetIsMatchFound(false);
        _model.SetStatus("Match canceled");
        await Task.Delay(1000);
        _model.SetStatus(string.Empty);
    }

    public async Task CancelMatchmaking()
    {
        try
        {
            _debugLogger.Log("MatchMaking: Canceling matchmaking");
            await _httpService.Post<EmptyRequest>("/api/matchmaking/cancel", new EmptyRequest());
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

[System.Serializable]
internal class MatchMakingResponse
{
    public int queuePosition;
}
