using Fusion;
using Zenject;

public class GameStateNetworkView : NetworkBehaviour, IGameStateNetworkBridge
{
    [Inject] private readonly IGameStateSubsystem _subsystem;

    private ChangeDetector _changeDetector;

    [Networked] public int NetworkedTurn { get; set; }
    [Networked] public NetworkString<_16> NetworkedPhase { get; set; }
    [Networked] public int NetworkedTimer { get; set; }

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

    // ── IGameStateNetworkBridge (upstream: client → server) ───────────────

    public void SendStartMatchRpc() => Rpc_RequestStartMatch();
    public void SendEndTurnRpc() => Rpc_RequestEndTurn();
    public void SendSetPhaseRpc(string phase) => Rpc_RequestSetPhase(phase);

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestStartMatch()
    {
        NetworkedTurn = 1;
        NetworkedPhase = "StartPhase";
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestEndTurn()
    {
        NetworkedTurn++;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestSetPhase(string phase)
    {
        NetworkedPhase = phase;
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
        _subsystem.OnAuthoritativeStateReceived(new GameStateStateData
        {
            CurrentTurn = NetworkedTurn,
            CurrentPhase = NetworkedPhase.ToString(),
            MatchTimer = NetworkedTimer
        });
    }
}
