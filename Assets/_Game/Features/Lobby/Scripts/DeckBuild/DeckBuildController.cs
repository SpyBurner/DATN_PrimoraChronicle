using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Codice.CM.Common;
using Core;
using UnityEngine;
using WebSocketSharp;
using Zenject;

internal class DeckBuildController : IDeckBuildController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IDeckBuildModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoadingManager;
    [Inject] private readonly IAuthSessionModel _authSessionModel;

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
                ChampionCardSO championCard = ResolveCardByStringId(response.championStringID) as ChampionCardSO;
                List<CardSO> championCards = championCard != null ? new List<CardSO> { championCard } : new List<CardSO>();
                List<CardSO> grantedCards = GetGrantedCards(championCard);

                foreach (var cardId in response.cardIds)
                {
                    CardSO card = ResolveCardByStringId(cardId);
                    if (card != null)
                    {
                        deckCards.Add(card);
                    }
                    else
                    {
                        _debugLogger.LogWarning($"DeckBuild: Could not resolve card ID {cardId}");
                    }
                }

                List<CardSO> availableCards = await GetAllCollection(deckCards);
                _model.SetRenderData(deckCards, championCards, grantedCards, availableCards);
                _debugLogger.Log($"DeckBuild: Loaded deck '{response.name}' with {deckCards.Count} cards, {championCards.Count} champions, and {grantedCards.Count} granted cards");
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

    public async Task CreateEmptyDeck()
    {
        _debugLogger.Log("DeckBuild: Creating empty deck");
        _model.SetCurrentDeck(string.Empty, string.Empty);
        List<CardSO> availableCards = await GetAllCollection();
        _model.SetRenderData(Array.Empty<CardSO>(), Array.Empty<CardSO>(), Array.Empty<CardSO>(), availableCards);
    }

    public async Task LoadAvailableCards()
    {
        try
        {
            _debugLogger.Log("DeckBuild: Loading available cards from collection");
            List<CardSO> currentDeck = new List<CardSO>(_model.DeckCards.Value);
            List<CardSO> availableCards = await GetAllCollection(currentDeck);
            _model.SetAvailableCards(availableCards);
            _debugLogger.Log($"DeckBuild: Loaded {availableCards.Count} available cards");
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"DeckBuild: LoadAvailableCards failed: {ex.Message}");
        }
    }

    public void AddCardToDeck(CardSO card)
    {
        if (card == null) return;

        var deckCards = new List<CardSO>(_model.DeckCards.Value);
        var championCards = new List<CardSO>(_model.ChampionCards.Value);
        var availableCards = new List<CardSO>(_model.AvailableCards.Value);

        if (card is ChampionCardSO championCard)
        {
            // Enforce 1 champion limit
            championCards.Clear();
            championCards.Add(championCard);
        }
        else
        {
            deckCards.Add(card);
            RemoveFirstMatchingCard(availableCards, card);
        }

        List<CardSO> grantedCards = GetGrantedCards(championCards.FirstOrDefault());
        _model.SetRenderData(deckCards, championCards, grantedCards, availableCards);
    }

    public void RemoveCardFromDeck(CardSO card)
    {
        if (card == null) return;

        var deckCards = new List<CardSO>(_model.DeckCards.Value);
        var championCards = new List<CardSO>(_model.ChampionCards.Value);
        var availableCards = new List<CardSO>(_model.AvailableCards.Value);

        if (card is ChampionCardSO)
        {
            championCards.Remove(card);
        }
        else
        {
            deckCards.Remove(card);
            availableCards.Add(card);
        }

        List<CardSO> grantedCards = GetGrantedCards(championCards.FirstOrDefault());
        _model.SetRenderData(deckCards, championCards, grantedCards, availableCards);
    }

    private List<CardSO> GetGrantedCards(CardSO champion)
    {
        List<CardSO> grantedCards = new();
        if (champion == null) return grantedCards;

        if (_cardLoadingManager.TryGetCardData(champion.StringID, out var cardData))
        {
            if (cardData.grants_cards != null)
            {
                foreach (var grant in cardData.grants_cards)
                {
                    if (grant != null)
                    {
                        string stringId = grant.string_id;
                        int count = grant.quantity;

                        CardSO card = ResolveCardByStringId(stringId);
                        if (card != null)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                grantedCards.Add(card);
                            }
                        }
                    }
                }
            }
        }
        return grantedCards;
    }

    public async Task SaveDeck()
    {
        try
        {
            string deckId = _model.CurrentDeckId.Value;
            string deckName = _model.CurrentDeckName.Value;
            string championStringId = _model.ChampionCards.Value.FirstOrDefault()?.StringID;

            _debugLogger.Log($"DeckBuild: Saving deck {deckName} ({deckId})");

            List<string> cardIds = _model.DeckCards.Value.Select(c => c.StringID).ToList();

            // Client-side validation
            if (deckName.IsNullOrEmpty())
            {
                _model.SetErrorMessage("Deck name cannot be empty.");
                return;
            }

            if (cardIds.Count != Constants.DECK_CARD_COUNT)
            {
                _model.SetErrorMessage($"Deck must contain exactly {Constants.DECK_CARD_COUNT} cards (currently {cardIds.Count}).");
                return;
            }

            if (championStringId.IsNullOrEmpty())
            {
                _model.SetErrorMessage("Deck must contain exactly 1 champion.");
                return;
            }

            _model.SetErrorMessage(string.Empty);

            var payload = new SaveDeckRequest
            {
                id = deckId,
                name = deckName,
                championStringID = championStringId,
                cardIds = cardIds
            };

            await _httpService.Post<SaveDeckRequest>($"/api/decks/save", payload);
            _debugLogger.Log("DeckBuild: Deck saved successfully");
        }
        catch (Exception ex)
        {
            _model.SetErrorMessage($"Save failed: {ex.Message}");
            _debugLogger.LogError($"DeckBuild: SaveDeck failed: {ex.Message}");
        }
    }

    private CardSO ResolveCardByStringId(string cardStringId)
    {
        if (string.IsNullOrWhiteSpace(cardStringId))
        {
            return null;
        }

        return _cardLoadingManager
            .GetCardsById()
            .Values
            .FirstOrDefault(card => string.Equals(card?.StringID, cardStringId, StringComparison.Ordinal));
    }

    private List<CardSO> GetAvailableCards()
    {
        List<CardSO> availableCards = new();
        availableCards.AddRange(_cardLoadingManager.GetTroopCardList().Values);
        availableCards.AddRange(_cardLoadingManager.GetSpellCardList().Values);
        return availableCards;
    }

    private async Task<List<CardSO>> GetAllCollection(IEnumerable<CardSO> cardsAlreadyInDeck = null)
    {
        string responseJson = await _httpService.Get($"/api/collection/card-copies");
        Debug.Log($"DeckBuild: Received card copies response: {responseJson}");
        CollectionCardCopyResponse[] cardCopies = DeserializeArray<CollectionCardCopyResponse>(responseJson);

        Dictionary<string, int> copyCountsByStringId = new(StringComparer.Ordinal);
        foreach (CollectionCardCopyResponse cardCopy in cardCopies)
        {
            if (cardCopy == null || string.IsNullOrWhiteSpace(cardCopy.cardStringID))
            {
                continue;
            }

            copyCountsByStringId.TryGetValue(cardCopy.cardStringID, out int count);
            copyCountsByStringId[cardCopy.cardStringID] = count + 1;
        }
        Debug.Log($"DeckBuild: Computed card copy counts for {copyCountsByStringId.Count} unique cards");

        if (cardsAlreadyInDeck != null)
        {
            foreach (CardSO deckCard in cardsAlreadyInDeck)
            {
                string backendStringId = deckCard?.StringID;
                if (string.IsNullOrWhiteSpace(backendStringId))
                {
                    continue;
                }

                if (copyCountsByStringId.TryGetValue(backendStringId, out int count) && count > 0)
                {
                    copyCountsByStringId[backendStringId] = count - 1;
                }
            }
        }

        List<CardSO> availableCards = new();
        AppendCopies(availableCards, _cardLoadingManager.GetTroopCardList().Values, copyCountsByStringId);
        AppendCopies(availableCards, _cardLoadingManager.GetSpellCardList().Values, copyCountsByStringId);
        Debug.Log($"DeckBuild: Computed available cards count: {availableCards.Count}");
        return availableCards;
    }

    private void AppendCopies(List<CardSO> target, IEnumerable<CardSO> sourceCards, IReadOnlyDictionary<string, int> copyCountsByStringId)
    {
        foreach (CardSO card in sourceCards)
        {
            string backendStringId = card?.StringID;
            if (string.IsNullOrWhiteSpace(backendStringId))
            {
                continue;
            }

            // Filter out non-summonable cards (tokens) and non-Common rarity cards
            if (_cardLoadingManager.TryGetCardData(backendStringId, out var cardData))
            {
                if (cardData.is_summonable == 0)
                    continue;
                if (cardData.rarity != "Common")
                    continue;
            }

            if (!copyCountsByStringId.TryGetValue(backendStringId, out int copyCount) || copyCount <= 0)
            {
                continue;
            }

            for (int index = 0; index < copyCount; index++)
            {
                target.Add(card);
            }
        }
    }

    private static void RemoveFirstMatchingCard(List<CardSO> cards, CardSO targetCard)
    {
        if (targetCard == null)
        {
            return;
        }

        int index = cards.FindIndex(card => card != null && card.ID == targetCard.ID);
        if (index >= 0)
        {
            cards.RemoveAt(index);
        }
    }

    private static T[] DeserializeArray<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<T>();
        }

        string wrappedJson = $"{{\"items\":{json}}}";
        ArrayWrapper<T> wrapper = JsonUtility.FromJson<ArrayWrapper<T>>(wrappedJson);
        return wrapper?.items ?? Array.Empty<T>();
    }

    [Serializable]
    private class ArrayWrapper<T>
    {
        public T[] items;
    }

    [Serializable]
    private class CollectionCardCopyResponse
    {
        public string ID;
        public string cardStringID;
    }
}
