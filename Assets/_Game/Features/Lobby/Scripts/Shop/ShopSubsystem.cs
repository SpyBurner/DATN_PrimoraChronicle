using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Events;
using Zenject;

public class ShopSubsystem : IShopSubsystem, IInitializable, IDisposable
{
    [Inject] private readonly IShopController _controller;
    [Inject] private readonly IShopModel _model;

    public event UnityAction<List<ShopItemData>> ItemsChanged;
    public event UnityAction<int> UserGoldChanged;

    public void Initialize()
    {
        if (_model?.Items != null)
            _model.Items.OnChanged += HandleItemsChanged;
        
        if (_model?.UserGold != null)
            _model.UserGold.OnChanged += HandleUserGoldChanged;
    }

    public void Dispose()
    {
        if (_model?.Items != null)
            _model.Items.OnChanged -= HandleItemsChanged;
        
        if (_model?.UserGold != null)
            _model.UserGold.OnChanged -= HandleUserGoldChanged;
    }

    public Task LoadItems() => _controller.LoadItems();
    public Task PurchaseItem(string itemId) => _controller.PurchaseItem(itemId);

    private void HandleItemsChanged()
    {
        try { ItemsChanged?.Invoke(_model.Items.Value); } catch { }
    }

    private void HandleUserGoldChanged()
    {
        try { UserGoldChanged?.Invoke(_model.UserGold.Value); } catch { }
    }
}
