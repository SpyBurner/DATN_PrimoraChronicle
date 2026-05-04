using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

internal class ShopController : IShopController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IShopModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly ILobbyMainSubsystem _lobbyMain; // To update user's gold globally

    public void Initialize() { }
    public void Dispose() { }

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

    public async Task PurchaseItem(string itemId)
    {
        try
        {
            _debugLogger.Log($"Shop: Purchasing item {itemId}...");
            var payload = new { itemId };
            var response = await _httpService.Post<PurchaseResponse>("/api/shop/purchase", payload);

            if (response != null && response.success)
            {
                _model.SetUserGold(response.remainingGold);
                // Try to update global gold if available
                _lobbyMain?.Initialize(); // Hacky way for now, better if LobbyModel provides gold globally
                _debugLogger.Log($"Shop: Purchase successful. Remaining gold: {response.remainingGold}");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"Shop: Purchase failed: {ex.Message}");
        }
    }
}

[System.Serializable]
internal class ShopItemsResponse
{
    public ShopItemData[] items;
}

[System.Serializable]
internal class PurchaseResponse
{
    public bool success;
    public int remainingGold;
}
