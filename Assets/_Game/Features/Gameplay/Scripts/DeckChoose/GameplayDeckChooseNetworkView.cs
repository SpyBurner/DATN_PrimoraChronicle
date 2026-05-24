using Fusion;
using UnityEngine;
using Zenject;

/// <summary>
/// Network seam for the deck-choose flow.
/// Acts as the View in the networked-subsystem architecture — may inject multiple
/// subsystems since it bridges Fusion state to the local DI world.
/// One instance spawned per player at the start of StartPhase.
/// </summary>
public class GameplayDeckChooseNetworkView : NetworkBehaviour, IGameplayDeckChooseNetworkBridge
{
    [Inject(Optional = true)] private IGameplayDeckChooseSubsystem _deckChoose;

    [Networked] public NetworkBool IsReady { get; set; }
    [Networked] public NetworkString<_64> SelectedDeckId { get; set; }

    private ChangeDetector _changeDetector;

    private static readonly string[] _defaultCardIds =
        { "troop_scout", "troop_warrior", "equip_sword", "spell_fireball" };
    private const string DefaultChampionId = "champ_hero";
    private const int DefaultInitialHP = 100;

    public override void Spawned()
    {
        if (_deckChoose == null)
        {
            var ctx = FindFirstObjectByType<SceneContext>();
            _deckChoose = ctx?.Container.Resolve<IGameplayDeckChooseSubsystem>();
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority)
            _deckChoose?.RegisterNetworkBridge(this);

        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
            _deckChoose?.RegisterNetworkBridge(null);
    }

    // ── IGameplayDeckChooseNetworkBridge ──────────────────────────────────

    public void SendConfirmRpc(string championId, string cardIdsJoined, int playerIndex, string playerName)
        => Rpc_ConfirmDeckSelection(Runner.LocalPlayer, championId, cardIdsJoined, playerIndex, playerName);

    public void SendAutoConfirmRpc(int playerIndex)
        => Rpc_AutoConfirmDeckSelection(Runner.LocalPlayer, playerIndex);

    // ── RPCs (client → server) ────────────────────────────────────────────

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_ConfirmDeckSelection(PlayerRef sender, string championId, string cardIdsJoined, int playerIndex, string playerName, RpcInfo info = default)
    {
        string[] cardIds = cardIdsJoined.Split(',');
        SetupPlayerDeck(sender, championId, cardIds, playerIndex, playerName);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_AutoConfirmDeckSelection(PlayerRef sender, int playerIndex, RpcInfo info = default)
    {
        SetupPlayerDeck(sender, DefaultChampionId, _defaultCardIds, playerIndex, "Player " + (playerIndex + 1));
    }

    private void SetupPlayerDeck(PlayerRef playerRef, string championId, string[] cardIds, int playerIndex, string playerName)
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null) return;

        var cardZone = coordinator.GetPlayerCardZoneView(playerRef);
        cardZone?.ServerSetupDeckForMatch(championId, cardIds);

        IsReady = true;
        SelectedDeckId = string.Empty;
    }

    // ── Server-side auto-confirm (called by NetworkGameplayManager on timer expiry) ──

    public void ServerAutoConfirm(int playerIndex)
    {
        if (!HasStateAuthority || IsReady) return;
        SetupPlayerDeck(Object.InputAuthority, DefaultChampionId, _defaultCardIds, playerIndex, "Player " + (playerIndex + 1));
    }

    // ── Downstream: server → all clients ─────────────────────────────────

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
        if (!HasInputAuthority) return;
        if (_deckChoose == null) return;
        _deckChoose.OnAuthoritativeStateReceived(new GameplayDeckChooseStateData
        {
            IsReady = IsReady,
            SelectedDeckId = SelectedDeckId.ToString()
        });
    }
}
