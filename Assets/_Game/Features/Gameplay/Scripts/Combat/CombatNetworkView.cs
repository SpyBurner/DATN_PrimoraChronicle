using Fusion;
using Zenject;

public class CombatNetworkView : NetworkBehaviour, ICombatNetworkBridge
{
    [Inject] private readonly ICombatSubsystem _subsystem;

    private ChangeDetector _changeDetector;

    [Networked] public NetworkString<_16> NetworkedAttackerId { get; set; }
    [Networked] public NetworkString<_16> NetworkedDefenderId { get; set; }
    [Networked] public NetworkString<_64> NetworkedCombatLog { get; set; }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority)
            _subsystem.RegisterNetworkBridge(this);

        PushState();
    }

    public void OnDestroy()
    {
        if (HasInputAuthority)
            _subsystem.RegisterNetworkBridge(null);
    }

    // ── ICombatNetworkBridge (upstream: client → server) ──────────────────

    public void SendExecuteTurnRpc() => Rpc_RequestExecuteTurn();
    public void SendSkipCombatRpc() => Rpc_RequestSkipCombat();

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestExecuteTurn()
    {
        // Server-side logic would update networked props here
        NetworkedCombatLog = "Executing turn...";
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestSkipCombat()
    {
        NetworkedCombatLog = "Combat skipped.";
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
        _subsystem.OnAuthoritativeStateReceived(new CombatStateData
        {
            CurrentAttackerId = NetworkedAttackerId,
            CurrentDefenderId = NetworkedDefenderId,
            CombatLog = NetworkedCombatLog
        });
    }
}
