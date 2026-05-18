using System.Collections.Generic;
using UnityObservables;

internal class ShopModel : IShopModel
{
    private Observable<List<ShopItemData>> _items = new(new List<ShopItemData>());
    private Observable<int> _userGold = new(0);
    private Observable<List<ShopCardSlot>> _dailyDealCards = new(new List<ShopCardSlot>());
    private Observable<List<ShopCardSlot>> _commonCards = new(new List<ShopCardSlot>());
    private Observable<string> _errorMessage = new(string.Empty);

    public Observable<List<ShopItemData>> Items { get => _items; }
    public Observable<int> UserGold { get => _userGold; }
    public Observable<List<ShopCardSlot>> DailyDealCards { get => _dailyDealCards; }
    public Observable<List<ShopCardSlot>> CommonCards { get => _commonCards; }
    public Observable<string> ErrorMessage { get => _errorMessage; }

    public void Initialize() { }

    public void Dispose()
    {
        _items.Value.Clear();
        _userGold.Value = 0;
        _dailyDealCards.Value.Clear();
        _commonCards.Value.Clear();
    }

    public void SetItems(List<ShopItemData> items) => _items.Value = new List<ShopItemData>(items);
    public void SetUserGold(int gold) => _userGold.Value = gold;
    public void SetDailyDealCards(List<ShopCardSlot> cards) => _dailyDealCards.Value = new List<ShopCardSlot>(cards);
    public void SetCommonCards(List<ShopCardSlot> cards) => _commonCards.Value = new List<ShopCardSlot>(cards);
    public void SetErrorMessage(string message) => _errorMessage.Value = message;
}
