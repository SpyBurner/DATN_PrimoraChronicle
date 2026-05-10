using Fusion;
using Zenject;

public class MatchResultNetworkView : NetworkBehaviour, IMatchResultNetworkBridge
{
    [Inject] private readonly IMatchResultSubsystem _subsystem;

    private ChangeDetector _changeDetector;

    [Networked] public NetworkBool NetworkedIsVictory { get; set; }
    [Networked] public int NetworkedGoldEarned { get; set; }
    [Networked] public int NetworkedRankProgress { get; set; }

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

    // ── IMatchResultNetworkBridge (upstream: client → server) ──────────────

    public void SendShowResultRpc(bool victory, int gold, int rank) => Rpc_RequestShowResult(victory, gold, rank);
    public void SendBackToLobbyRpc() => Rpc_RequestBackToLobby();

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestShowResult(bool victory, int gold, int rank)
    {
        NetworkedIsVictory = victory;
        NetworkedGoldEarned = gold;
        NetworkedRankProgress = rank;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestBackToLobby()
    {
        // Server handles scene transition or cleanup
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
        _subsystem.OnAuthoritativeStateReceived(new MatchResultStateData
        {
            IsVictory = NetworkedIsVictory,
            GoldEarned = NetworkedGoldEarned,
            RankProgress = NetworkedRankProgress
        });
    }
}
