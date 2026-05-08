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
    [Inject] private readonly IAuthSessionModel _authSessionModel;

    public void Initialize() { }
    public void Dispose() { }

    public async Task LoadDecks()
    {
        try
        {
            string userId = _authSessionModel.CurrentUserId.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                _debugLogger.LogError("Deck: Cannot load decks without a current user id");
                return;
            }

            _debugLogger.Log("Deck: Loading decks from server");
            string encodedUserId = Uri.EscapeDataString(userId);
            var response = await _httpService.Get<DecksListResponse>($"/api/decks?user_id={encodedUserId}");
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

