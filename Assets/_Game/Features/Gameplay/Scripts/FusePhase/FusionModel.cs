using UnityObservables;

internal class FusionModel : IFusionModel
{
    private readonly Observable<FusionStagingData> _staging = new(default);
    private readonly Observable<bool> _isConfirmed = new(false);

    public Observable<FusionStagingData> Staging => _staging;
    public Observable<bool> IsConfirmed => _isConfirmed;

    public void Initialize() { }

    public void Dispose()
    {
        _staging.Value = default;
        _isConfirmed.Value = false;
    }

    public void ApplyState(FusionStateData data) => _isConfirmed.Value = data.IsConfirmed;

    public void UpdateStaging(FusionStagingData staging) => _staging.Value = staging;
}
