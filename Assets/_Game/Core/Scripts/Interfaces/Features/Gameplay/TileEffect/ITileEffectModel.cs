using System;
using System.Collections.Generic;

public interface ITileEffectModel : IModel
{
    event Action<TileEffectInstance> EffectApplied;
    event Action<HexCoord> EffectRemoved;

    IReadOnlyDictionary<HexCoord, TileEffectInstance> Effects { get; }

    void ApplyEffect(TileEffectInstance instance);
    void RemoveEffect(HexCoord coord);
}
