using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Zenject;

internal class DeckBuildController : IDeckBuildController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IDeckBuildModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoadingManager;

    public void Initialize() { }
    public void Dispose() { }

    public async Task LoadDeck(string deckId)
    {
        try
        {
            _debugLogger.Log($"DeckBuild: Loading deck {deckId}");
            var response = await _httpService.Get<DeckDetailData>($"/api/decks/{deckId}");

            if (response != null)
            {
                _model.SetCurrentDeck(response.id, response.name);
                
                List<CardSO> deckCards = new();
                List<CardSO> championCards = new();

                foreach (var cardId in response.cardIds)
                {
                    if (_cardLoadingManager.TryGetCard(cardId, out var card))
                    {
                        if (card is ChampionCardSO championCard)
                        {
                            championCards.Add(championCard);
                        }
                        else
                        {
                            deckCards.Add(card);
                        }
                    }
                    else
                    {
                        _debugLogger.LogWarning($"DeckBuild: Could not resolve card ID {cardId}");
                    }
                }

                _model.SetRenderData(deckCards, championCards, GetAvailableCards());
                _debugLogger.Log($"DeckBuild: Loaded deck '{response.name}' with {deckCards.Count} cards and {championCards.Count} champions");
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

    public void AddCardToDeck(CardSO card)
    {
        if (card == null) return;

        var deckCards = new List<CardSO>(_model.DeckCards.Value);
        var championCards = new List<CardSO>(_model.ChampionCards.Value);

        if (card is ChampionCardSO championCard)
        {
            championCards.Add(championCard);
        }
        else
        {
            deckCards.Add(card);
        }

        _model.SetRenderData(deckCards, championCards, _model.AvailableCards.Value);
    }

    public void RemoveCardFromDeck(CardSO card)
    {
        if (card == null) return;

        var deckCards = new List<CardSO>(_model.DeckCards.Value);
        var championCards = new List<CardSO>(_model.ChampionCards.Value);

        if (card is ChampionCardSO)
        {
            championCards.Remove(card);
        }
        else
        {
            deckCards.Remove(card);
        }

        _model.SetRenderData(deckCards, championCards, _model.AvailableCards.Value);
    }

    public async Task SaveDeck()
    {
        try
        {
            string deckId = _model.CurrentDeckId.Value;
            string deckName = _model.CurrentDeckName.Value;
            
            _debugLogger.Log($"DeckBuild: Saving deck {deckName} ({deckId})");

            List<string> cardIds = _model.DeckCards.Value.Select(c => c.ID).ToList();
            cardIds.AddRange(_model.ChampionCards.Value.Select(c => c.ID));

            var payload = new 
            { 
                id = deckId,
                name = deckName, 
                cardIds = cardIds 
            };

            await _httpService.Post($"/api/decks/save", payload);
            _debugLogger.Log("DeckBuild: Deck saved successfully");
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"DeckBuild: SaveDeck failed: {ex.Message}");
        }
    }

    private List<CardSO> GetAvailableCards()
    {
        var allCards = _cardLoadingManager.GetCardsById().Values;
        // Return all non-champion cards as available for building? 
        // Or all cards including champions? 
        // For now, return everything.
        return allCards.ToList();
    }
}
