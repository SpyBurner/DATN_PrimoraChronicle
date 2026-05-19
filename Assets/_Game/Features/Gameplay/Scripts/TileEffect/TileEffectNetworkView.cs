using Fusion;
using UnityEngine;
using Zenject;

public class TileEffectNetworkView : NetworkBehaviour, ITileEffectNetworkBridge
{
    [Inject(Optional = true)] private ITileEffectSubsystem _subsystem;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked] public int PositionP { get; set; }
    [Networked] public int PositionQ { get; set; }
    [Networked] public NetworkString<_32> EffectId { get; set; }
    [Networked] public int DurationRemaining { get; set; }
    [Networked] public PlayerRef Owner { get; set; }

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        if (_subsystem == null)
        {
            var ctx = FindObjectOfType<SceneContext>();
            _subsystem = ctx?.Container.Resolve<ITileEffectSubsystem>();
            _logger = ctx?.Container.Resolve<IDebugLogger>();
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        var coord = new HexCoord(PositionP, PositionQ);
        _subsystem?.OnEffectRemovedAt(coord);
    }

    public void ServerInitialize(HexCoord coord, string effectId, int duration, PlayerRef owner)
    {
        if (!Object.HasStateAuthority) return;

        PositionP = coord.P;
        PositionQ = coord.Q;
        EffectId = effectId;
        DurationRemaining = duration;
        Owner = owner;

        _logger?.Log($"[TileEffectNetworkView] Initialized effect '{effectId}' at ({coord.P},{coord.Q}) dur={duration}.");
    }

    public void ServerTickDuration()
    {
        if (!Object.HasStateAuthority) return;
        if (DurationRemaining <= 0) return;

        DurationRemaining--;
        if (DurationRemaining <= 0)
        {
            _logger?.Log($"[TileEffectNetworkView] Effect '{EffectId}' at ({PositionP},{PositionQ}) expired.");
            Runner.Despawn(Object);
        }
    }

    // ── ITileEffectNetworkBridge ─────────────────────────────────────────

    public void SendApplyEffectRpc(HexCoord coord, string effectId, int duration)
        => Rpc_RequestApplyEffect(coord.P, coord.Q, effectId, duration);

    public void SendRemoveEffectRpc(HexCoord coord)
        => Rpc_RequestRemoveEffect(coord.P, coord.Q);

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestApplyEffect(int p, int q, string effectId, int duration)
    {
        ServerInitialize(new HexCoord(p, q), effectId, duration, Object.InputAuthority);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestRemoveEffect(int p, int q)
    {
        if (!Object.HasStateAuthority) return;
        if (PositionP == p && PositionQ == q)
            Runner.Despawn(Object);
    }

    // ── Render ───────────────────────────────────────────────────────────

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

        _subsystem.OnEffectReceived(new TileEffectInstance
        {
            Position = new HexCoord(PositionP, PositionQ),
            EffectId = EffectId.ToString(),
            DurationRemaining = DurationRemaining,
            Owner = Owner
        });
    }
}
