using Fusion;
using UnityEngine;
using Zenject;

/// <summary>
/// Per-player public NetworkObject — always-replicated to all clients.
/// Host spawns one per player. Pushes PlayerRosterPublicData into IPlayerRosterSubsystem.
/// </summary>
public class PlayerRosterPublicNetworkView : NetworkBehaviour, IPlayerRosterNetworkBridge
{
    [Inject(Optional = true)] private IPlayerRosterSubsystem _roster;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public int HP { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public NetworkString<_32> UserId { get; set; }

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        if (_roster == null)
        {
            var ctx = FindObjectOfType<SceneContext>();
            if (ctx != null)
            {
                _roster = ctx.Container.Resolve<IPlayerRosterSubsystem>();
                _logger = ctx.Container.Resolve<IDebugLogger>();
            }
            else
            {
                Debug.LogError("[PlayerRosterPublicNetworkView] SceneContext not found — injection failed.");
                return;
            }
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _roster.RegisterNetworkBridge(this);
        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _roster?.RegisterNetworkBridge(null);
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
        if (_roster == null || Owner == PlayerRef.None) return;
        _roster.OnAuthoritativeStateReceived(new PlayerRosterPublicData
        {
            Owner = Owner,
            HP = HP,
            PlayerName = PlayerName.Value,
            UserId = UserId.Value,
        });
    }

    // ── IPlayerRosterNetworkBridge ────────────────────────────────────────

    /// <summary>Server calls this to set the owner's HP and replicate.</summary>
    public void SendHPChangedRpc(PlayerRef owner, int newHP)
    {
        if (!Object.HasStateAuthority) return;
        HP = newHP;
    }

    public void SendNameChangedRpc(PlayerRef owner, string name)
    {
        if (!Object.HasStateAuthority) return;
        PlayerName = name;
    }

    public void SendUserIdChangedRpc(PlayerRef owner, string userId)
    {
        if (!Object.HasStateAuthority) return;
        UserId = userId;
    }

    // ── Server-side helpers ───────────────────────────────────────────────

    /// <summary>Called by coordinator after spawning to assign owner + initial data.</summary>
    public void ServerInitialize(PlayerRef owner, int hp, string playerName, string userId)
    {
        if (!Object.HasStateAuthority) return;
        Owner = owner;
        HP = hp;
        PlayerName = playerName;
        UserId = userId;
        _logger?.Log($"[PlayerRosterPublicNetworkView] Initialized for {owner}: HP={hp}, Name={playerName}");
    }
}
