using UnityObservables;

public interface IFusePhaseModel : IModel
{
    Observable<bool> IsActive { get; }
    Observable<string> PrimaryUnitId { get; }
    Observable<string> SecondaryUnitId { get; }
}
