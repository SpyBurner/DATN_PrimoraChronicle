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

    public async Task LoadMatchHistory()
    {
        try
        {
            _debugLogger.Log("LOG_LOBBY_HISTORY", nameof(MatchHistoryController), "MatchHistory: Loading history...");
            string responseJson = await _httpService.Get("/api/matches");
            string wrappedJson = $"{{\"items\":{responseJson}}}";
            var wrapper = UnityEngine.JsonUtility.FromJson<MatchHistoryArrayWrapper>(wrappedJson);
            var items = wrapper?.items ?? Array.Empty<MatchHistoryData>();

            _model.SetMatchHistory(new List<MatchHistoryData>(items));
            _debugLogger.Log("LOG_LOBBY_HISTORY", nameof(MatchHistoryController), $"MatchHistory: Loaded {items.Length} matches.");
        }
        catch (Exception ex)
        {
            _debugLogger.LogError("LOG_LOBBY_HISTORY", nameof(MatchHistoryController), $"MatchHistory: Failed to load history: {ex.Message}");
        }
    }
}

[System.Serializable]
internal class MatchHistoryArrayWrapper
{
    public MatchHistoryData[] items;
}
