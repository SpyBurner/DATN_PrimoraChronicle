using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

internal class ShopController : IShopController
{
    private const int DailyDealCount = 4;
    private const int CommonCardCount = 8;

    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IShopModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly ICardLoadingManagerSubsystem _cardLoadingManager;

    public void Initialize() { }
    public void Dispose() { }

    public void GenerateShopCards()
    {
        var pool = BuildSpellTroopPool();
        if (pool.Count == 0)
        {
            _debugLogger.LogError("Shop: No Spell or Troop cards available to populate shop.");
            return;
        }

        _model.SetDailyDealCards(PickRandom(pool, DailyDealCount));
        _model.SetCommonCards(PickRandom(pool, CommonCardCount));
        _debugLogger.Log($"Shop: Generated {DailyDealCount} daily deal cards and {CommonCardCount} common cards.");
    }

    public async Task LoadItems()
    {
        try
        {
            _debugLogger.Log("Shop: Loading items...");
            var response = await _httpService.Get<ShopItemsResponse>("/api/shop/items");

            if (response != null && response.items != null)
            {
                _model.SetItems(new List<ShopItemData>(response.items));
                _debugLogger.Log($"Shop: Loaded {response.items.Length} items.");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"Shop: Failed to load items: {ex.Message}");
        }
    }

    public async Task PurchaseItem(string cardStringID)
    {
        try
        {
            _model.SetErrorMessage(string.Empty);
            _debugLogger.Log($"Shop: Purchasing card {cardStringID}...");
            var payload = new BuyCardRequest { cardStringID = cardStringID };
            var response = await _httpService.Post<BuyCardResponse, BuyCardRequest>("/api/shop/buy-card", payload);

            if (response != null && response.status == "success")
            {
                _model.SetUserGold(response.new_gold);
                _debugLogger.Log($"Shop: Purchase successful. Remaining gold: {response.new_gold}");
            }
        }
        catch (Exception ex)
        {
            _model.SetErrorMessage(ex.Message);
            _debugLogger.LogError($"Shop: Purchase failed: {ex.Message}");
        }
    }

    private List<string> BuildSpellTroopPool()
    {
        var pool = new List<string>();
        foreach (var key in _cardLoadingManager.GetSpellCardList().Keys)
        {
            if (_cardLoadingManager.TryGetCardData(key, out var data) && data.rarity == "Common")
                pool.Add(key);
        }
        foreach (var key in _cardLoadingManager.GetTroopCardList().Keys)
        {
            if (_cardLoadingManager.TryGetCardData(key, out var data) && data.rarity == "Common")
                pool.Add(key);
        }
        return pool;
    }

    private static List<ShopCardSlot> PickRandom(List<string> pool, int count)
    {
        var shuffled = pool.OrderBy(_ => UnityEngine.Random.value).ToList();
        var result = new List<ShopCardSlot>();
        for (int i = 0; i < count && i < shuffled.Count; i++)
            result.Add(new ShopCardSlot { StringID = shuffled[i] });
        return result;
    }
}

[System.Serializable]
internal class ShopItemsResponse
{
    public ShopItemData[] items;
}

[System.Serializable]
internal class BuyCardResponse
{
    public string status;
    public int new_gold;
}

[System.Serializable]
internal class BuyCardRequest
{
    public string cardStringID;
}
