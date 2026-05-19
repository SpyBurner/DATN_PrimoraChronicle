using System.Collections.Generic;
using UnityEngine.Events;

public interface ITileEffectSubsystem : ISubsystem
{
    event UnityAction<TileEffectInstance> EffectApplied;
    event UnityAction<HexCoord> EffectRemoved;

    IReadOnlyList<TileEffectInstance> AllEffects { get; }
    bool TryGet(HexCoord coord, out TileEffectInstance instance);

    void RegisterNetworkBridge(ITileEffectNetworkBridge bridge);
    void OnEffectReceived(TileEffectInstance instance);
    void OnEffectRemovedAt(HexCoord coord);
}
