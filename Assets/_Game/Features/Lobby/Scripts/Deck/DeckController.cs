using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

internal class DeckController : IDeckController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IDeckModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;

    public void Initialize() { }
    public void Dispose() { }

    public async Task LoadDecks()
    {
        try
        {
            _debugLogger.Log("Deck: Loading decks from server");
            var response = await _httpService.Get<DecksListResponse>("/api/decks");

            if (response != null && response.decks != null)
            {
                List<DeckSummaryData> decks = new(response.decks);
                _model.SetDecks(decks);
                _debugLogger.Log($"Deck: Loaded {decks.Count} decks");
            }
            else
            {
                _debugLogger.LogError("Deck: Failed to load decks");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"Deck: LoadDecks failed: {ex.Message}");
        }
    }
}

[System.Serializable]
internal class DecksListResponse
{
    public DeckSummaryData[] decks;
}

