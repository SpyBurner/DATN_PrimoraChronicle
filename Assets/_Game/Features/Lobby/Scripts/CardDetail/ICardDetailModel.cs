using UnityObservables;

public interface ICardDetailModel : IModel
{
    Observable<string> CardName { get; }
    Observable<string> CardDescription { get; }
    Observable<int> CardCost { get; }
    Observable<int> CardPower { get; }
    Observable<string> CardImageUrl { get; }
}
