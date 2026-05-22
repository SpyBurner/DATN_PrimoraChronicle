using Fusion;
using UnityEngine;
using Zenject;

public class MatchResultNetworkView : NetworkBehaviour, IMatchResultNetworkBridge
{
    [Inject(Optional = true)] private IMatchResultSubsystem _subsystem;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked] public PlayerRef Winner { get; set; }
    [Networked] public NetworkBool IsTie { get; set; }
    [Networked] public int GoldEarned { get; set; }
    [Networked] public int XPEarned { get; set; }
    [Networked] public float DurationSeconds { get; set; }
    [Networked] public NetworkBool HasResult { get; set; }

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        if (_subsystem == null)
        {
            var ctx = FindObjectOfType<SceneContext>();
            _subsystem = ctx?.Container.Resolve<IMatchResultSubsystem>();
            _logger = ctx?.Container.Resolve<IDebugLogger>();
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _subsystem?.RegisterNetworkBridge(this);

        if (HasResult)
            PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _subsystem?.RegisterNetworkBridge(null);
    }

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
        if (_subsystem == null) return;
        if (!HasResult) return;

        _subsystem.OnAuthoritativeStateReceived(new GameMatchResult
        {
            Winner = Winner,
            IsTie = IsTie,
            GoldEarned = GoldEarned,
            XPEarned = XPEarned,
            DurationSeconds = DurationSeconds
        });
    }

    // ── IMatchResultNetworkBridge ────────────────────────────────────────

    public void SendEndMatchRpc(GameMatchResult result)
    {
        if (Object.HasStateAuthority)
        {
            ServerCommitResult(result);
        }
        else
        {
            Rpc_RequestEndMatch(result.Winner, result.IsTie, result.GoldEarned, result.XPEarned, result.DurationSeconds);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestEndMatch(PlayerRef winner, NetworkBool isTie, int gold, int xp, float duration)
    {
        ServerCommitResult(new GameMatchResult
        {
            Winner = winner,
            IsTie = isTie,
            GoldEarned = gold,
            XPEarned = xp,
            DurationSeconds = duration
        });
    }

    // ── Server-Side API (called by GameStateNetworkView) ─────────────────

    public void ServerEndMatch(GameMatchResult result)
    {
        if (!Object.HasStateAuthority) return;
        ServerCommitResult(result);
    }

    private void ServerCommitResult(GameMatchResult result)
    {
        Winner = result.Winner;
        IsTie = result.IsTie;
        GoldEarned = result.GoldEarned;
        XPEarned = result.XPEarned;
        DurationSeconds = result.DurationSeconds;
        HasResult = true;

        _logger?.Log($"[MatchResultNetworkView] Match ended. Winner={Winner}, IsTie={IsTie}, Duration={DurationSeconds:F1}s");
    }
}
