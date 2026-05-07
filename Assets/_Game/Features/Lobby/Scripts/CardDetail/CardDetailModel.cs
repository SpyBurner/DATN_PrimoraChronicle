using UnityObservables;

internal class CardDetailModel : ICardDetailModel
{
    private Observable<string> _cardName = new(string.Empty);
    private Observable<string> _cardDescription = new(string.Empty);
    private Observable<int> _cardCost = new(0);
    private Observable<int> _cardPower = new(0);
    private Observable<string> _cardImageUrl = new(string.Empty);

    public Observable<string> CardName { get => _cardName; }
    public Observable<string> CardDescription { get => _cardDescription; }
    public Observable<int> CardCost { get => _cardCost; }
    public Observable<int> CardPower { get => _cardPower; }
    public Observable<string> CardImageUrl { get => _cardImageUrl; }

    public void Initialize() { }

    public void Dispose()
    {
        _cardName.Value = string.Empty;
        _cardDescription.Value = string.Empty;
        _cardCost.Value = 0;
        _cardPower.Value = 0;
        _cardImageUrl.Value = string.Empty;
    }

    public void SetCardName(string name) => _cardName.Value = name;
    public void SetCardDescription(string desc) => _cardDescription.Value = desc;
    public void SetCardCost(int cost) => _cardCost.Value = cost;
    public void SetCardPower(int power) => _cardPower.Value = power;
    public void SetCardImageUrl(string url) => _cardImageUrl.Value = url;
}
