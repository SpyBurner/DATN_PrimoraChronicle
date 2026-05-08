using System.Collections.Generic;
using UnityObservables;

public interface IShopModel : IModel
{
    Observable<List<ShopItemData>> Items { get; }
    Observable<int> UserGold { get; }

    void SetItems(List<ShopItemData> items);
    void SetUserGold(int gold);
}

[System.Serializable]
public class ShopItemData
{
    public string id;
    public string name;
    public string description;
    public int price;
    public string imageUrl;
}
