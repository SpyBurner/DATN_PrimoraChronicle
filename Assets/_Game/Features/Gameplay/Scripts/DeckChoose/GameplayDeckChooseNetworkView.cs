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
    [Inject] private readonly IGameplayDeckChooseSubsystem _deckChoose;

    [Networked] public NetworkBool IsReady { get; set; }
    [Networked] public NetworkString<_64> SelectedDeckId { get; set; }

    private ChangeDetector _changeDetector;

    private static readonly string[] _defaultCardIds =
        { "troop_scout", "troop_warrior", "equip_sword", "spell_fireball" };
    private const string DefaultChampionId = "champ_hero";
    private const int DefaultInitialHP = 100;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority)
            _deckChoose.RegisterNetworkBridge(this);

        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
            _deckChoose.RegisterNetworkBridge(null);
    }

    // ── IGameplayDeckChooseNetworkBridge ──────────────────────────────────

    public void SendConfirmRpc(string championId, string cardIdsJoined, int playerIndex)
        => Rpc_ConfirmDeckSelection(championId, cardIdsJoined, playerIndex);

    public void SendAutoConfirmRpc(int playerIndex)
        => Rpc_AutoConfirmDeckSelection(playerIndex);

    // ── RPCs (client → server) ────────────────────────────────────────────

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_ConfirmDeckSelection(string championId, string cardIdsJoined, int playerIndex)
    {
        string[] cardIds = cardIdsJoined.Split(',');
        SetupPlayerDeck(Object.InputAuthority, championId, cardIds, playerIndex);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_AutoConfirmDeckSelection(int playerIndex)
    {
        SetupPlayerDeck(Object.InputAuthority, DefaultChampionId, _defaultCardIds, playerIndex);
    }

    private void SetupPlayerDeck(PlayerRef playerRef, string championId, string[] cardIds, int playerIndex)
    {
        if (NetworkGameplayManager.Instance == null) return;

        for (int i = 0; i < NetworkGameplayManager.Instance.PlayerStates.Length; i++)
        {
            var stateId = NetworkGameplayManager.Instance.PlayerStates.Get(i);
            if (!stateId.IsValid) continue;

            if (Runner.TryFindObject(stateId, out var stateObj))
            {
                var ps = stateObj.GetComponent<NetworkPlayerState>();
                if (ps != null && ps.Player == playerRef)
                {
                    ps.SetupDeck(championId, cardIds, DefaultInitialHP, playerIndex);
                    ps.DrawCards(6);
                    break;
                }
            }
        }

        IsReady = true;
        SelectedDeckId = string.Empty;
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
        _deckChoose.OnAuthoritativeStateReceived(new GameplayDeckChooseStateData
        {
            IsReady = IsReady,
            SelectedDeckId = SelectedDeckId.ToString()
        });
    }
}
