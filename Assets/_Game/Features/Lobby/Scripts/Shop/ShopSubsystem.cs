using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Events;
using Zenject;

public class ShopSubsystem : IShopSubsystem
{
    [Inject] private readonly IShopController _controller;
    [Inject] private readonly IShopModel _model;

    public event UnityAction<List<ShopItemData>> ItemsChanged;
    public event UnityAction<int> UserGoldChanged;
    public event UnityAction<List<ShopCardSlot>> DailyDealCardsChanged;
    public event UnityAction<List<ShopCardSlot>> CommonCardsChanged;
    public event UnityAction<string> ErrorMessageChanged;

    public void Initialize()
    {
        if (_model?.Items != null)
            _model.Items.OnChanged += HandleItemsChanged;

        if (_model?.UserGold != null)
            _model.UserGold.OnChanged += HandleUserGoldChanged;

        if (_model?.DailyDealCards != null)
            _model.DailyDealCards.OnChanged += HandleDailyDealCardsChanged;

        if (_model?.CommonCards != null)
            _model.CommonCards.OnChanged += HandleCommonCardsChanged;

        if (_model?.ErrorMessage != null)
            _model.ErrorMessage.OnChanged += HandleErrorMessageChanged;
    }

    public void Dispose()
    {
        if (_model?.Items != null)
            _model.Items.OnChanged -= HandleItemsChanged;

        if (_model?.UserGold != null)
            _model.UserGold.OnChanged -= HandleUserGoldChanged;

        if (_model?.DailyDealCards != null)
            _model.DailyDealCards.OnChanged -= HandleDailyDealCardsChanged;

        if (_model?.CommonCards != null)
            _model.CommonCards.OnChanged -= HandleCommonCardsChanged;

        if (_model?.ErrorMessage != null)
            _model.ErrorMessage.OnChanged -= HandleErrorMessageChanged;
    }

    public Task LoadItems() => _controller.LoadItems();
    public Task PurchaseItem(string itemId) => _controller.PurchaseItem(itemId);
    public void GenerateShopCards() => _controller.GenerateShopCards();

    private void HandleItemsChanged()
    {
        try { ItemsChanged?.Invoke(_model.Items.Value); } catch { }
    }

    private void HandleUserGoldChanged()
    {
        try { UserGoldChanged?.Invoke(_model.UserGold.Value); } catch { }
    }

    private void HandleDailyDealCardsChanged()
    {
        try { DailyDealCardsChanged?.Invoke(_model.DailyDealCards.Value); } catch { }
    }

    private void HandleCommonCardsChanged()
    {
        try { CommonCardsChanged?.Invoke(_model.CommonCards.Value); } catch { }
    }

    private void HandleErrorMessageChanged()
    {
        try { ErrorMessageChanged?.Invoke(_model.ErrorMessage.Value); } catch { }
    }
}
