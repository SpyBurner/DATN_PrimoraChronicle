using System.Collections.Generic;
using UnityObservables;

public interface IShopModel : IModel
{
    Observable<List<ShopItemData>> Items { get; }
    Observable<int> UserGold { get; }
    Observable<List<ShopCardSlot>> DailyDealCards { get; }
    Observable<List<ShopCardSlot>> CommonCards { get; }
    Observable<string> ErrorMessage { get; }

    void SetItems(List<ShopItemData> items);
    void SetUserGold(int gold);
    void SetDailyDealCards(List<ShopCardSlot> cards);
    void SetCommonCards(List<ShopCardSlot> cards);
    void SetErrorMessage(string message);
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

[System.Serializable]
public class ShopCardSlot
{
    public string StringID;
}
