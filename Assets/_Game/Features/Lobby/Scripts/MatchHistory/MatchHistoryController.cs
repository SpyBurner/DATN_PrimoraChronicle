using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zenject;

internal class MatchHistoryController : IMatchHistoryController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IMatchHistoryModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;

    public void Initialize() { }
    public void Dispose() { }

    public async Task LoadMatchHistory(string userId)
    {
        try
        {
            _debugLogger.Log($"MatchHistory: Loading history for {userId}...");
            var response = await _httpService.Get<List<MatchHistoryData>>($"/api/matches?user_id={userId}");

            if (response != null)
            {
                _model.SetMatchHistory(new List<MatchHistoryData>(response));
                _debugLogger.Log($"MatchHistory: Loaded {response.Count} matches.");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"MatchHistory: Failed to load history: {ex.Message}");
        }
    }
}
