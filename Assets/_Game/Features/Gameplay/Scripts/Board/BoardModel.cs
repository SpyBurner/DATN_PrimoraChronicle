using Fusion;
using System.Collections.Generic;
using UnityObservables;

public class BoardModel : NetworkBehaviour, IBoardModel
{
    private ChangeDetector _changeDetector;

    [Networked, Capacity(25)] public NetworkDictionary<int, NetworkString<_16>> NetworkedGrid { get; }

    private ObservableDictionary<int, string> _gridOccupancy = new();
    public ObservableDictionary<int, string> GridOccupancy => _gridOccupancy;

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
        _gridOccupancy.Clear();
        foreach (var pair in NetworkedGrid)
        {
            _gridOccupancy[pair.Key] = pair.Value.ToString();
        }
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
