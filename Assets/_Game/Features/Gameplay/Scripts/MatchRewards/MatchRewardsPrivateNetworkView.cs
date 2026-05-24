using Fusion;
using UnityEngine;
using Zenject;

/// <summary>
/// Per-player private NetworkObject — AoI-restricted to owner only.
/// Host spawns one per player and calls Runner.SetPlayerAlwaysInterested(owner, this, true).
/// Caches values locally so MatchResultPanel stays populated after Runner.Shutdown().
/// </summary>
public class MatchRewardsPrivateNetworkView : NetworkBehaviour, IMatchRewardsPrivateNetworkBridge
{
    [Inject(Optional = true)] private IMatchRewardsSubsystem _rewards;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public int GoldEarned { get; set; }
    [Networked] public int XPEarned { get; set; }

    private ChangeDetector _changeDetector;

    // Local cache — survives runner shutdown so MatchResultPanel stays populated
    private int _cachedGold;
    private int _cachedXP;

    public override void Spawned()
    {
        if (_rewards == null)
        {
            var ctx = FindFirstObjectByType<SceneContext>();
            if (ctx != null)
            {
                _rewards = ctx.Container.Resolve<IMatchRewardsSubsystem>();
                _logger = ctx.Container.Resolve<IDebugLogger>();
            }
            else
            {
                Debug.LogError("[MatchRewardsPrivateNetworkView] SceneContext not found — injection failed.");
                return;
            }
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _rewards.RegisterNetworkBridge(this);

        if (Object.HasStateAuthority)
        {
            // AoI: only the owner should receive this object's state
            if (Owner != PlayerRef.None)
                Runner.SetPlayerAlwaysInterested(Owner, Object, true);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _rewards?.RegisterNetworkBridge(null);
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
        if (_rewards == null || Owner == PlayerRef.None) return;
        _cachedGold = GoldEarned;
        _cachedXP = XPEarned;
        _rewards.OnAuthoritativeStateReceived(new MatchRewardsPrivateData
        {
            Owner = Owner,
            GoldEarned = GoldEarned,
            XPEarned = XPEarned,
        });
    }

    // ── IMatchRewardsPrivateNetworkBridge ─────────────────────────────────

    public void SendRewardsRpc(PlayerRef owner, int gold, int xp)
    {
        if (!Object.HasStateAuthority) return;
        GoldEarned = gold;
        XPEarned = xp;
        _logger?.Log("LOG_MATCHREWARDSPRIVATENETWORKVIEW", nameof(MatchRewardsPrivateNetworkView), $"Rewards set for {owner}: Gold={gold} XP={xp}");
    }

    // ── Server-side helpers ───────────────────────────────────────────────

    public void ServerInitialize(PlayerRef owner)
    {
        if (!Object.HasStateAuthority) return;
        Owner = owner;
        Runner.SetPlayerAlwaysInterested(owner, Object, true);
        _logger?.Log("LOG_MATCHREWARDSPRIVATENETWORKVIEW", nameof(MatchRewardsPrivateNetworkView), $"Initialized AoI for {owner}.");
    }
}
