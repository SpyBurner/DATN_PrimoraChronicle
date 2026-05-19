using System.Collections.Generic;
using UnityEngine.Events;

public interface ITargetingSubsystem : ISubsystem
{
    event UnityAction<TargetingRequest> TargetingStarted;
    event UnityAction<IReadOnlyList<HexCoord>> HighlightedTilesChanged;
    event UnityAction<HexCoord> TargetConfirmed;
    event UnityAction TargetingCancelled;

    bool IsTargeting { get; }
    IReadOnlyList<HexCoord> HighlightedTiles { get; }

    void BeginTargeting(TargetingRequest request, UnityEngine.Events.UnityAction<HexCoord> onConfirmed);
    void HoverTile(HexCoord coord);
    void ConfirmTarget(HexCoord coord);
    void Cancel();
}
