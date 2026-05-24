using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine.Events;
using Zenject;

public class UnitSubsystem : IUnitSubsystem
{
    [Inject] private readonly IUnitController _controller;
    [Inject] private readonly IUnitModel _model;

    public event UnityAction<NetworkId> UnitSpawned;
    public event UnityAction<NetworkId> UnitDied;
    public event UnityAction<NetworkId, int> UnitHPChanged;
    public event UnityAction<NetworkId, HexCoord> UnitMoved;
    public event UnityAction<NetworkId, string, int> StatusApplied;
    public event UnityAction<NetworkId, string> StatusRemoved;
    public event UnityAction<NetworkId, IReadOnlyList<SkillSlot>> OwnUnitSkillsChanged;

    private readonly Dictionary<NetworkId, UnitPublicData> _prevPublic = new();

    public IReadOnlyList<NetworkId> AllUnits => new List<NetworkId>(_model.Units.Keys);

    public bool TryGetPublic(NetworkId id, out UnitPublicData data)
        => _model.Units.TryGetValue(id, out data);

    public bool TryGetOwnSkills(NetworkId id, out IReadOnlyList<SkillSlot> skills)
    {
        return _model.TryGetOwnSkills(id, out skills);
    }

    public void Initialize()
    {
        _model.UnitPublicStateChanged += HandlePublicStateChanged;
        _model.UnitPrivateStateChanged += HandlePrivateStateChanged;
        _model.UnitRemoved += HandleUnitRemoved;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.UnitPublicStateChanged -= HandlePublicStateChanged;
        _model.UnitPrivateStateChanged -= HandlePrivateStateChanged;
        _model.UnitRemoved -= HandleUnitRemoved;
        _prevPublic.Clear();
        _controller.Dispose();
        _model.Dispose();
    }

    public void RegisterNetworkBridge(IUnitPublicNetworkBridge bridge) => _controller.RegisterPublicBridge(bridge);
    public void RegisterPrivateNetworkBridge(IUnitPrivateNetworkBridge bridge) => _controller.RegisterPrivateBridge(bridge);
    public void OnUnitPublicStateReceived(UnitPublicData data) => _controller.OnUnitPublicStateReceived(data);
    public void OnUnitPrivateStateReceived(UnitPrivateData data) => _controller.OnUnitPrivateStateReceived(data);
    public void OnUnitDestroyed(NetworkId unitId) => _controller.OnUnitDestroyed(unitId);

    private void HandlePublicStateChanged(UnitPublicData data)
    {
        bool isNew = !_prevPublic.ContainsKey(data.UnitId);

        if (isNew)
        {
            try { UnitSpawned?.Invoke(data.UnitId); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
        }
        else
        {
            var prev = _prevPublic[data.UnitId];

            if (prev.CurrentHP != data.CurrentHP)
            {
                try { UnitHPChanged?.Invoke(data.UnitId, data.CurrentHP); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }

            if (prev.Position.P != data.Position.P || prev.Position.Q != data.Position.Q)
            {
                try { UnitMoved?.Invoke(data.UnitId, data.Position); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }
        }

        _prevPublic[data.UnitId] = data;
    }

    private void HandlePrivateStateChanged(UnitPrivateData data)
    {
        try { OwnUnitSkillsChanged?.Invoke(data.UnitId, data.Skills ?? new List<SkillSlot>()); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleUnitRemoved(NetworkId unitId)
    {
        _prevPublic.Remove(unitId);
        try { UnitDied?.Invoke(unitId); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
