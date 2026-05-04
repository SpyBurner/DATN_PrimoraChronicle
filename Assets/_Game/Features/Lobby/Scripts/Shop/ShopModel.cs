using System.Collections.Generic;
using UnityObservables;

internal class ShopModel : IShopModel
{
    private Observable<List<ShopItemData>> _items = new(new List<ShopItemData>());
    private Observable<int> _userGold = new(0);

    public Observable<List<ShopItemData>> Items { get => _items; }
    public Observable<int> UserGold { get => _userGold; }

    public void Initialize() { }

    public void Dispose()
    {
        _items.Value.Clear();
        _userGold.Value = 0;
    }

    internal void SetItems(List<ShopItemData> items) => _items.Value = new List<ShopItemData>(items);
    internal void SetUserGold(int gold) => _userGold.Value = gold;
}
