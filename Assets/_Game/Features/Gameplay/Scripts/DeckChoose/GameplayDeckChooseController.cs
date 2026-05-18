using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

internal class GameplayDeckChooseController : IGameplayDeckChooseController
{
    [Inject] private readonly IGameplayDeckChooseModel _model;
    [Inject] private readonly IDebugLogger _logger;
    [Inject] private readonly IHttpServiceSubsystem _httpService;
    [Inject] private readonly IAuthSessionModel _authSession;

    private IGameplayDeckChooseNetworkBridge _bridge;
    private DeckSummaryData _stagedSummary;

    private static readonly string[] _defaultCardIds =
        { "troop_scout", "troop_warrior", "equip_sword", "spell_fireball" };
    private const string DefaultChampionId = "champ_hero";
    private const int DefaultInitialHP = 100;

    public void Initialize() { }
    public void Dispose() { _bridge = null; }

    public void StageSelection(DeckSummaryData summary)
    {
        _stagedSummary = summary;
        _logger.Log($"[GameplayDeckChoose] Staged deck: {summary.id}");
    }

    public async Task ConfirmSelection()
    {
        if (string.IsNullOrEmpty(_stagedSummary.id))
        {
            _logger.LogWarning("[GameplayDeckChoose] ConfirmSelection called with no staged deck.");
            return;
        }

        DeckDetailData detail = await FetchDeckDetail(_stagedSummary.id);
        if (detail == null)
        {
            _logger.LogError($"[GameplayDeckChoose] Failed to fetch detail for deck {_stagedSummary.id}.");
            return;
        }

        string cardIdsJoined = string.Join(",", detail.cardIds);
        int playerIndex = ResolvePlayerIndex();

        if (_bridge != null)
        {
            _bridge.SendConfirmRpc(detail.championStringID, cardIdsJoined, playerIndex);
        }
        else
        {
            // Offline / editor path — apply directly
            _model.ApplyState(new GameplayDeckChooseStateData
            {
                IsReady = true,
                SelectedDeckId = detail.id
            });
        }
    }

    public async Task AutoConfirmLastDeck()
    {
        int playerIndex = ResolvePlayerIndex();

        if (!string.IsNullOrEmpty(_stagedSummary.id))
        {
            // A deck was browsed but not confirmed — confirm it now
            await ConfirmSelection();
            return;
        }

        // No deck selected — fall back to defaults
        _logger.Log("[GameplayDeckChoose] Auto-confirming with default deck.");
        string cardIdsJoined = string.Join(",", _defaultCardIds);

        if (_bridge != null)
            _bridge.SendAutoConfirmRpc(playerIndex);
        else
            _model.ApplyState(new GameplayDeckChooseStateData { IsReady = true, SelectedDeckId = string.Empty });
    }

    public void RegisterBridge(IGameplayDeckChooseNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log($"[GameplayDeckChoose] Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(GameplayDeckChooseStateData data)
    {
        _model.ApplyState(data);
    }

    private async Task<DeckDetailData> FetchDeckDetail(string deckId)
    {
        try
        {
            string encoded = Uri.EscapeDataString(deckId);
            return await _httpService.Get<DeckDetailData>($"/api/decks/{encoded}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"[GameplayDeckChoose] FetchDeckDetail exception: {ex.Message}");
            return null;
        }
    }

    private int ResolvePlayerIndex()
    {
        if (NetworkGameplayManager.Instance == null) return 0;

        string userId = _authSession?.CurrentUserId?.Value ?? string.Empty;
        for (int i = 0; i < NetworkGameplayManager.Instance.PlayerStates.Length; i++)
        {
            var stateId = NetworkGameplayManager.Instance.PlayerStates.Get(i);
            if (!stateId.IsValid) continue;
            if (NetworkGameplayManager.Instance.Runner.TryFindObject(stateId, out var obj))
            {
                var ps = obj.GetComponent<NetworkPlayerState>();
                if (ps != null && ps.Player.PlayerId == i)
                    return i;
            }
        }
        return 0;
    }
}
