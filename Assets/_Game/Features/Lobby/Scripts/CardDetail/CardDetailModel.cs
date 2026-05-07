using UnityObservables;

internal class CardDetailModel : ICardDetailModel
{
    private Observable<string> _cardName = new(string.Empty);
    private Observable<string> _cardDescription = new(string.Empty);
    private Observable<int> _cardCost = new(0);
    private Observable<int> _cardPower = new(0);
    private Observable<string> _cardImageUrl = new(string.Empty);
    private Observable<string> _skillName = new(string.Empty);
    private Observable<string> _skillDescription = new(string.Empty);
    private Observable<string> _skillPattern = new(string.Empty);

    public Observable<string> CardName { get => _cardName; }
    public Observable<string> CardDescription { get => _cardDescription; }
    public Observable<int> CardCost { get => _cardCost; }
    public Observable<int> CardPower { get => _cardPower; }
    public Observable<string> CardImageUrl { get => _cardImageUrl; }
    public Observable<string> SkillName { get => _skillName; }
    public Observable<string> SkillDescription { get => _skillDescription; }
    public Observable<string> SkillPattern { get => _skillPattern; }

    public void Initialize() { }

    public void Dispose()
    {
        _cardName.Value = string.Empty;
        _cardDescription.Value = string.Empty;
        _cardCost.Value = 0;
        _cardPower.Value = 0;
        _cardImageUrl.Value = string.Empty;
        _skillName.Value = string.Empty;
        _skillDescription.Value = string.Empty;
        _skillPattern.Value = string.Empty;
    }

    public void SetCardName(string name) => _cardName.Value = name;
    public void SetCardDescription(string desc) => _cardDescription.Value = desc;
    public void SetCardCost(int cost) => _cardCost.Value = cost;
    public void SetCardPower(int power) => _cardPower.Value = power;
    public void SetCardImageUrl(string url) => _cardImageUrl.Value = url;
    public void SetSkill(string name, string desc, string pattern)
    {
        _skillName.Value = name;
        _skillDescription.Value = desc;
        _skillPattern.Value = pattern;
    }
}
