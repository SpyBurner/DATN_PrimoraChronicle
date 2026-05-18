using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Events;

public interface IShopSubsystem : ISubsystem
{
    event UnityAction<List<ShopItemData>> ItemsChanged;
    event UnityAction<int> UserGoldChanged;

    Task LoadItems();
    Task PurchaseItem(string itemId);
}
