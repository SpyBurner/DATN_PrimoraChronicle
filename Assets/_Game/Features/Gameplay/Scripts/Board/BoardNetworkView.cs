using Fusion;
using Zenject;
using System.Collections.Generic;

public class BoardNetworkView : NetworkBehaviour, IBoardNetworkBridge
{
    [Inject] private readonly IBoardSubsystem _subsystem;

    private ChangeDetector _changeDetector;

    [Networked, Capacity(25)] public NetworkDictionary<int, NetworkString<_16>> NetworkedGrid { get; }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority)
            _subsystem.RegisterNetworkBridge(this);

        PushState();
    }

    public override void OnDestroy()
    {
        if (HasInputAuthority)
            _subsystem.RegisterNetworkBridge(null);
    }

    // ── IBoardNetworkBridge (upstream: client → server) ───────────────────

    public void SendPlaceUnitRpc(int cellIndex, string unitId) => Rpc_RequestPlaceUnit(cellIndex, unitId);

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestPlaceUnit(int cellIndex, string unitId)
    {
        NetworkedGrid.Set(cellIndex, unitId);
    }

    // ── Downstream: server → all clients ────────────────────────────────

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(NetworkedGrid))
            {
                PushState();
                break;
            }
        }
    }

    private void PushState()
    {
        var dict = new Dictionary<int, string>();
        foreach (var pair in NetworkedGrid)
        {
            dict[pair.Key] = pair.Value.ToString();
        }

        _subsystem.OnAuthoritativeStateReceived(new BoardStateData
        {
            Grid = dict
        });
    }
}
