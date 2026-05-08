using Fusion;
using System.Collections.Generic;
using UnityObservables;

public class BoardModel : NetworkBehaviour, IBoardModel
{
    private ChangeDetector _changeDetector;

    [Networked, Capacity(25)] public NetworkDictionary<int, NetworkString<_16>> NetworkedGrid { get; }

    private Observable<Dictionary<int, string>> _gridOccupancy = new(new Dictionary<int, string>());
    public Observable<Dictionary<int, string>> GridOccupancy => _gridOccupancy;

    public void Initialize() { }
    public void Dispose() { }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        SyncGrid();
    }

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(NetworkedGrid))
            {
                SyncGrid();
            }
        }
    }

    private void SyncGrid()
    {
        var dict = new Dictionary<int, string>();
        foreach (var pair in NetworkedGrid)
        {
            dict[pair.Key] = pair.Value.ToString();
        }
        _gridOccupancy.Value = dict;
    }

    public void RequestPlaceUnit(int cellIndex, string unitId)
    {
        if (Object.HasStateAuthority)
        {
            NetworkedGrid.Set(cellIndex, unitId);
        }
        else
        {
            Rpc_PlaceUnit(cellIndex, unitId);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_PlaceUnit(int cellIndex, string unitId)
    {
        NetworkedGrid.Set(cellIndex, unitId);
    }
}
