using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core;
using Core.GDS;
using Newtonsoft.Json;
using UnityEngine;
using Zenject;

internal class CardLoadingManagerController : ICardLoadingManagerController
{
    private const string CardResourcesPath = "CardSO";
    private const string CacheFileName = "card_data_cache.json";

    [Inject] private readonly ICardLoadingManagerModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly IDebugLogger _debugLogger;

    private string CacheFilePath => Path.Combine(Application.persistentDataPath, CacheFileName);

    public async Task LoadCardsAsync()
    {
        // 1. Load Visual Assets from Resources (Fast)
        LoadVisualAssets();

        // 2. Load GDS Data from Cache (Immediate offline support)
        LoadGDSFromCache();

        // 3. Sync Latest GDS Data from Server (Network)
        await FetchGDSFromServer();
    }

    public void LoadCards()
    {
        _ = LoadCardsAsync();
    }

    private void LoadVisualAssets()
    {
        Dictionary<string, CardSO> cardsById = new();
        Dictionary<string, ChampionCardSO> championCardsList = new();
        Dictionary<string, SpellCardSO> spellCardList = new();
        Dictionary<string, TroopCardSO> troopCardList = new();

        foreach (CardSO card in Resources.LoadAll<CardSO>(CardResourcesPath))
        {
            if (card == null) continue;

            // Use StringID as the primary key for the bridge between JSON and SO
            if (string.IsNullOrWhiteSpace(card.StringID))
            {
                _debugLogger.LogWarning($"CardSO {card.name} is missing its StringID! Identification will fail.");
                continue;
            }

            if (cardsById.ContainsKey(card.StringID))
            {
                _debugLogger.LogWarning($"Duplicate CardSO StringID detected: {card.StringID}. Overwriting.");
                cardsById[card.StringID] = card;
            }
            else
            {
                cardsById.Add(card.StringID, card);
            }

            switch (card)
            {
                case ChampionCardSO championCard:
                    championCardsList[championCard.StringID] = championCard;
                    break;
                case SpellCardSO spellCard:
                    spellCardList[spellCard.StringID] = spellCard;
                    break;
                case TroopCardSO troopCard:
                    troopCardList[troopCard.StringID] = troopCard;
                    break;
            }
        }

        _model.CardsById.Value = cardsById;
        _model.ChampionCardsList.Value = championCardsList;
        _model.SpellCardList.Value = spellCardList;
        _model.TroopCardList.Value = troopCardList;
        
        _debugLogger.Log($"CardLoadingManager: Loaded {cardsById.Count} visual card assets.");
    }

    private void LoadGDSFromCache()
    {
        string path = CacheFilePath;
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                var response = JsonConvert.DeserializeObject<GDSResponse>(json);
                if (response?.data != null)
                {
                    _model.MasterData.Value = response.data;
                    _debugLogger.Log("CardLoadingManager: Loaded master card data from local cache.");
                }
            }
            catch (Exception ex)
            {
                _debugLogger.LogError($"CardLoadingManager: Failed to deserialize cached JSON: {ex.Message}");
            }
        }
    }

    private async Task FetchGDSFromServer()
    {
        try
        {
            // Endpoint defined in TestBE/app/main.py -> proxies GDS to avoid secret leaking
            string responseJson = await _httpService.Get("/api/game-data/cards");
            var response = JsonConvert.DeserializeObject<GDSResponse>(responseJson);
            
            if (response?.data != null)
            {
                // Overwrite cache with fresh data
                try
                {
                    File.WriteAllText(CacheFilePath, responseJson);
                }
                catch (IOException ioEx)
                {
                    _debugLogger.LogWarning($"CardLoadingManager: Failed to write to cache file: {ioEx.Message}");
                }

                _model.MasterData.Value = response.data;
                _debugLogger.Log($"CardLoadingManager: Successfully synchronized GDS data. Version: {response.data.metadata?.version ?? "unknown"}");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"CardLoadingManager: Network fetch failed: {ex.Message}. Using cached data.");
        }
    }

    public IReadOnlyDictionary<string, CardSO> GetCardsById() => _model.CardsById.Value;
    public IReadOnlyDictionary<string, ChampionCardSO> GetChampionCardsList() => _model.ChampionCardsList.Value;
    public IReadOnlyDictionary<string, SpellCardSO> GetSpellCardList() => _model.SpellCardList.Value;
    public IReadOnlyDictionary<string, TroopCardSO> GetTroopCardList() => _model.TroopCardList.Value;

    public bool TryGetCard(string cardId, out CardSO card)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            card = null;
            return false;
        }
        return _model.CardsById.Value.TryGetValue(cardId, out card);
    }

    public T GetCard<T>(string cardId) where T : CardSO
    {
        return TryGetCard(cardId, out CardSO card) ? card as T : null;
    }

    public MasterGDSData GetMasterGDSData() => _model.MasterData.Value;

    void IInitializable.Initialize() { }
    void IDisposable.Dispose() { }
}