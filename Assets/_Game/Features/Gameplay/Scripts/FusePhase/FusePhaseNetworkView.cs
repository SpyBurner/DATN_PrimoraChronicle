using Fusion;
using Zenject;

public class FusePhaseNetworkView : NetworkBehaviour, IFusePhaseNetworkBridge
{
    [Inject] private readonly IFusePhaseSubsystem _subsystem;

    private ChangeDetector _changeDetector;

    [Networked] public NetworkBool NetworkedIsActive { get; set; }
    [Networked] public NetworkString<_16> NetworkedPrimaryId { get; set; }
    [Networked] public NetworkString<_16> NetworkedSecondaryId { get; set; }

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

    // ── IFusePhaseNetworkBridge (upstream: client → server) ───────────────

    public void SendSetUnitsRpc(string primaryId, string secondaryId) => Rpc_RequestSetUnits(primaryId, secondaryId);
    public void SendFuseRpc() => Rpc_RequestFuse();
    public void SendCancelRpc() => Rpc_RequestCancel();

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestSetUnits(string primaryId, string secondaryId)
    {
        NetworkedPrimaryId = primaryId;
        NetworkedSecondaryId = secondaryId;
        NetworkedIsActive = true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestFuse()
    {
        NetworkedIsActive = false;
        NetworkedPrimaryId = "";
        NetworkedSecondaryId = "";
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestCancel()
    {
        NetworkedIsActive = false;
    }

    // ── Downstream: server → all clients ────────────────────────────────

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var _ in _changeDetector.DetectChanges(this))
        {
            PushState();
            break;
        }
    }

    private void PushState()
    {
        _subsystem.OnAuthoritativeStateReceived(new FusePhaseStateData
        {
            IsActive = NetworkedIsActive,
            PrimaryUnitId = NetworkedPrimaryId.ToString(),
            SecondaryUnitId = NetworkedSecondaryId.ToString()
        });
    }
}
