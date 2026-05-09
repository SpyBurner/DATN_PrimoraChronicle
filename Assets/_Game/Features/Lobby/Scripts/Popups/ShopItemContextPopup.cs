using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
public class ShopItemContextPopup : UIPanel
{
    [SerializeField] private TMP_Text _itemNameText;
    [SerializeField] private TMP_Text _itemPriceText;
    [SerializeField] private Button _purchaseButton;

    [Inject] private IPopupSubsystem _popupSubsystem;

    private Action _onPurchase;

    protected override void OnEnable()
    {
        base.OnEnable();
        _purchaseButton?.onClick.AddListener(OnPurchaseClicked);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _purchaseButton?.onClick.RemoveListener(OnPurchaseClicked);
    }

    public void Setup(string itemName, int price, Action onPurchase)
    {
        if (_itemNameText != null) _itemNameText.text = itemName;
        if (_itemPriceText != null) _itemPriceText.text = $"Cost: {price} Gold";
        
        _onPurchase = onPurchase;
    }

    private void OnPurchaseClicked()
    {
        _popupSubsystem.SetResult(true);
        _onPurchase?.Invoke();
        OnClose();
    }

    protected override void OnClose()
    {
        _popupSubsystem.SetResult(false);
        base.OnClose();
    }
}
