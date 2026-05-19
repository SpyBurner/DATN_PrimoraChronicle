using System;
using System.Collections.Generic;

internal class TileEffectModel : ITileEffectModel
{
    private readonly Dictionary<HexCoord, TileEffectInstance> _effects = new();

    public event Action<TileEffectInstance> EffectApplied;
    public event Action<HexCoord> EffectRemoved;

    public IReadOnlyDictionary<HexCoord, TileEffectInstance> Effects => _effects;

    public void Initialize() { }

    public void Dispose() => _effects.Clear();

    public void ApplyEffect(TileEffectInstance instance)
    {
        _effects[instance.Position] = instance;
        EffectApplied?.Invoke(instance);
    }

    public void RemoveEffect(HexCoord coord)
    {
        if (_effects.Remove(coord))
            EffectRemoved?.Invoke(coord);
    }
}
