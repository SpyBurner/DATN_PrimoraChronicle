using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Zenject;

public class TileEffectSubsystem : ITileEffectSubsystem
{
    [Inject] private readonly ITileEffectController _controller;
    [Inject] private readonly ITileEffectModel _model;

    public event UnityAction<TileEffectInstance> EffectApplied;
    public event UnityAction<HexCoord> EffectRemoved;

    public IReadOnlyList<TileEffectInstance> AllEffects
    {
        get
        {
            var list = new List<TileEffectInstance>(_model.Effects.Count);
            foreach (var v in _model.Effects.Values) list.Add(v);
            return list;
        }
    }

    public bool TryGet(HexCoord coord, out TileEffectInstance instance)
        => _model.Effects.TryGetValue(coord, out instance);

    public void Initialize()
    {
        _model.EffectApplied += HandleEffectApplied;
        _model.EffectRemoved += HandleEffectRemoved;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.EffectApplied -= HandleEffectApplied;
        _model.EffectRemoved -= HandleEffectRemoved;
        _controller.Dispose();
        _model.Dispose();
    }

    public void RegisterNetworkBridge(ITileEffectNetworkBridge bridge) => _controller.RegisterBridge(bridge);
    public void OnEffectReceived(TileEffectInstance instance) => _controller.OnEffectReceived(instance);
    public void OnEffectRemovedAt(HexCoord coord) => _controller.OnEffectRemovedAt(coord);

    private void HandleEffectApplied(TileEffectInstance instance)
    {
        try { EffectApplied?.Invoke(instance); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleEffectRemoved(HexCoord coord)
    {
        try { EffectRemoved?.Invoke(coord); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
