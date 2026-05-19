using UnityObservables;

public interface IFusionModel : IModel
{
    Observable<FusionStagingData> Staging { get; }
    Observable<bool> IsConfirmed { get; }

    void ApplyState(FusionStateData data);
    void UpdateStaging(FusionStagingData staging);
}
