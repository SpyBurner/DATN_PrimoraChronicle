using Fusion;
using UnityEngine;
using Zenject;

public class GameplayDeckChooseNetworkView : NetworkBehaviour, IGameplayDeckChooseNetworkBridge
{
    [Inject(Optional = true)] private IGameplayDeckChooseSubsystem _deckChoose;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked] public NetworkBool IsReady { get; set; }
    [Networked] public NetworkString<_64> SelectedDeckId { get; set; }

    private ChangeDetector _changeDetector;

    private static readonly string[] _defaultCardIds =
        { "troop_scout", "troop_warrior", "equip_sword", "spell_fireball" };
    private const string DefaultChampionId = "champ_hero";

    public override void Spawned()
    {
        if (_deckChoose == null)
        {
            var ctx = FindObjectOfType<SceneContext>();
            _deckChoose = ctx?.Container.Resolve<IGameplayDeckChooseSubsystem>();
            _logger = ctx?.Container.Resolve<IDebugLogger>();
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
        => Rpc_ConfirmDeckSelection(championId, cardIdsJoined, playerIndex, playerName);

    public void SendAutoConfirmRpc(int playerIndex)
        => Rpc_AutoConfirmDeckSelection(playerIndex);

    // ── RPCs (client → server) ────────────────────────────────────────────

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_ConfirmDeckSelection(string championId, string cardIdsJoined, int playerIndex, string playerName)
    {
        string[] cardIds = cardIdsJoined.Split(',');
        SetupPlayerDeck(Object.InputAuthority, championId, cardIds);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_AutoConfirmDeckSelection(int playerIndex)
    {
        SetupPlayerDeck(Object.InputAuthority, DefaultChampionId, _defaultCardIds);
    }

    private void SetupPlayerDeck(PlayerRef playerRef, string championId, string[] cardIds)
    {
        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator == null)
        {
            _logger?.LogWarning("[DeckChooseNetworkView] No coordinator found.");
            return;
        }

        var pczView = coordinator.GetPlayerCardZoneView(playerRef);
        if (pczView == null)
        {
            _logger?.LogWarning($"[DeckChooseNetworkView] No PlayerCardZoneView for {playerRef}.");
            return;
        }

        pczView.ServerSetupDeckForMatch(championId, cardIds);

        IsReady = true;
        SelectedDeckId = string.Empty;
        _logger?.Log($"[DeckChooseNetworkView] Deck confirmed for {playerRef}: champion={championId}");
    }

    // ── Server-side auto-confirm (called by GameStateNetworkView on timer expiry) ──

    public void ServerAutoConfirm()
    {
        if (!Object.HasStateAuthority || IsReady) return;
        SetupPlayerDeck(Object.InputAuthority, DefaultChampionId, _defaultCardIds);
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
        if (_deckChoose == null) return;
        _deckChoose.OnAuthoritativeStateReceived(new GameplayDeckChooseStateData
        {
            IsReady = IsReady,
            SelectedDeckId = SelectedDeckId.ToString()
        });
    }
}
