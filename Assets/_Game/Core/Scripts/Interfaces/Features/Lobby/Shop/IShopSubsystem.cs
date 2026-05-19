using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Events;

public interface IShopSubsystem : ISubsystem
{
    event UnityAction<List<ShopItemData>> ItemsChanged;
    event UnityAction<int> UserGoldChanged;
    event UnityAction<List<ShopCardSlot>> DailyDealCardsChanged;
    event UnityAction<List<ShopCardSlot>> CommonCardsChanged;
    event UnityAction<string> ErrorMessageChanged;

    Task LoadItems();
    Task PurchaseItem(string itemId);
    void GenerateShopCards();
}
