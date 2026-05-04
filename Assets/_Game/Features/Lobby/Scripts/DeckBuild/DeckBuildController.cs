using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zenject;

internal class DeckBuildController : IDeckBuildController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IDeckBuildModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;

    public void Initialize() { }
    public void Dispose() { }

    public async Task LoadDeck(string deckId)
    {
        try
        {
            _debugLogger.Log($"DeckBuild: Loading deck {deckId}");
            var response = await _httpService.Get<DeckLoadResponse>($"https://api.example.com/deck/{deckId}");

            if (response != null)
            {
                _model.SetDeckCards(new List<string>(response.cards));
                _debugLogger.Log($"DeckBuild: Loaded deck with {response.cards.Length} cards");
            }
            else
            {
                _debugLogger.LogError("DeckBuild: Failed to load deck");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"DeckBuild: LoadDeck failed: {ex.Message}");
        }
    }

    public void AddCardToDeck(string cardId)
    {
        _model.AddCard(cardId);
    }

    public void RemoveCardFromDeck(string cardId)
    {
        _model.RemoveCard(cardId);
    }

    public async Task SaveDeck(string deckName)
    {
        try
        {
            _debugLogger.Log($"DeckBuild: Saving deck {deckName}");
            var payload = new { deckName, cards = _model.DeckCards.Value };
            await _httpService.Post("https://api.example.com/deck/save", payload);
            _debugLogger.Log("DeckBuild: Deck saved successfully");
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"DeckBuild: SaveDeck failed: {ex.Message}");
        }
    }
}

[System.Serializable]
internal class DeckLoadResponse
{
    public string[] cards;
}
