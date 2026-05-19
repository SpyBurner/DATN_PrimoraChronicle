using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;
using Zenject;

public class UnitSubsystem : IUnitSubsystem
{
    [Inject] private readonly IUnitController _controller;
    [Inject] private readonly IUnitModel _model;

    public event UnityAction<string> UnitSpawned;
    public event UnityAction<string> UnitDied;
    public event UnityAction<string, int> UnitHPChanged;
    public event UnityAction<string, HexCoord> UnitMoved;
    public event UnityAction<string, string, int> StatusApplied;
    public event UnityAction<string, string> StatusRemoved;
    public event UnityAction<string, int> GrowthStacksChanged;

    private readonly Dictionary<string, UnitStateData> _prevStates = new();

    public IReadOnlyList<string> AllUnitIds => new List<string>(_model.Units.Keys);

    public bool TryGetUnit(string unitNetworkId, out UnitStateData data)
        => _model.Units.TryGetValue(unitNetworkId, out data);

    public IReadOnlyList<string> GetUnitsOwnedBy(PlayerRef owner)
    {
        var result = new List<string>();
        foreach (var kvp in _model.Units)
            if (kvp.Value.Owner == owner) result.Add(kvp.Key);
        return result;
    }

    public void Initialize()
    {
        _model.UnitStateChanged += HandleUnitStateChanged;
        _model.UnitRemoved += HandleUnitRemoved;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.UnitStateChanged -= HandleUnitStateChanged;
        _model.UnitRemoved -= HandleUnitRemoved;
        _prevStates.Clear();
        _controller.Dispose();
        _model.Dispose();
    }

    public void RegisterNetworkBridge(IUnitNetworkBridge bridge) => _controller.RegisterBridge(bridge);
    public void OnUnitStateReceived(UnitStateData data) => _controller.OnUnitStateReceived(data);
    public void OnUnitDestroyed(string unitNetworkId) => _controller.OnUnitDestroyed(unitNetworkId);

    private void HandleUnitStateChanged(UnitStateData data)
    {
        bool isNew = !_prevStates.ContainsKey(data.UnitNetworkId);

        if (isNew)
        {
            try { UnitSpawned?.Invoke(data.UnitNetworkId); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
        }
        else
        {
            var prev = _prevStates[data.UnitNetworkId];

            if (prev.CurrentHP != data.CurrentHP)
            {
                try { UnitHPChanged?.Invoke(data.UnitNetworkId, data.CurrentHP); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }

            if (prev.Position != data.Position)
            {
                try { UnitMoved?.Invoke(data.UnitNetworkId, data.Position); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }

            if (prev.GrowthStacks != data.GrowthStacks)
            {
                try { GrowthStacksChanged?.Invoke(data.UnitNetworkId, data.GrowthStacks); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }
        }

        _prevStates[data.UnitNetworkId] = data;
    }

    private void HandleUnitRemoved(string unitNetworkId)
    {
        _prevStates.Remove(unitNetworkId);
        try { UnitDied?.Invoke(unitNetworkId); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
