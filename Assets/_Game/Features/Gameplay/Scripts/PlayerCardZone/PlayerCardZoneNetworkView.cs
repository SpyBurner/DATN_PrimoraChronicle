using System.Collections.Generic;
using Core.GDS;
using Fusion;
using UnityEngine;
using Zenject;

public class PlayerCardZoneNetworkView : NetworkBehaviour, IPlayerCardZoneNetworkBridge
{
    [Inject(Optional = true)] private IPlayerCardZoneSubsystem _subsystem;
    [Inject(Optional = true)] private IGameStateSubsystem _gameState;
    [Inject(Optional = true)] private ICardLoadingManagerSubsystem _cardLoading;
    [Inject(Optional = true)] private IProfileSubsystem _profileSubsystem;
    [Inject(Optional = true)] private IBehaviorRegistrySubsystem _behaviorRegistry;
    [Inject(Optional = true)] private IUnitSubsystem _unitSubsystem;
    [Inject(Optional = true)] private IBoardSubsystem _boardSubsystem;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public int HP { get; set; }
    [Networked] public int MaxHP { get; set; }
    [Networked] public int HandCount { get; set; }
    [Networked] public int DeckCount { get; set; }
    [Networked] public int DiscardCount { get; set; }
    [Networked] public NetworkBool IsSetup { get; set; }
    [Networked] public NetworkString<_32> ChampionId { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }
    [Networked] public int DrawPhaseNewCards { get; set; }
    [Networked] public NetworkBool DrawPhaseConfirmed { get; set; }

    [Networked, Capacity(9)] public NetworkArray<NetworkString<_32>> Hand { get; }
    [Networked, Capacity(40)] public NetworkArray<NetworkString<_32>> Deck { get; }
    [Networked, Capacity(60)] public NetworkArray<NetworkString<_32>> Discard { get; }

    private ChangeDetector _changeDetector;

    private const int ChampionSlot = 0;
    private const int SupportCardCount = 6;
    private const int HandMax = 7;
    private const int HandArrayCapacity = 9;
    private const int DeckCapacity = 40;
    private const int DiscardCapacity = 60;
    private const int DefaultHP = 100;
    private const int OpeningHandSize = 6;
    private const int DrawPhaseDrawCount = 2;

    public override void Spawned()
    {
        if (_subsystem == null)
        {
            var ctx = FindFirstObjectByType<SceneContext>();
            _subsystem = ctx?.Container.Resolve<IPlayerCardZoneSubsystem>();
            _gameState = ctx?.Container.Resolve<IGameStateSubsystem>();
            _cardLoading = ctx?.Container.Resolve<ICardLoadingManagerSubsystem>();
            _profileSubsystem = ctx?.Container.TryResolve<IProfileSubsystem>();
            _behaviorRegistry = ctx?.Container.Resolve<IBehaviorRegistrySubsystem>();
            _unitSubsystem = ctx?.Container.Resolve<IUnitSubsystem>();
            _boardSubsystem = ctx?.Container.Resolve<IBoardSubsystem>();
            _logger = ctx?.Container.Resolve<IDebugLogger>();
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority)
        {
            _subsystem?.RegisterNetworkBridge(this);
            if (Object.HasStateAuthority && _profileSubsystem != null)
                PlayerName = _profileSubsystem.Username ?? string.Empty;
            else if (HasInputAuthority && _profileSubsystem != null)
                Rpc_SetPlayerName(Runner.LocalPlayer, _profileSubsystem.Username ?? string.Empty);
        }

        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
            _subsystem?.RegisterNetworkBridge(null);
    }

    // ── Server-Side API (called by DeckChoose or Coordinator) ────────────

    public void ServerSetupDeckForMatch(string championId, string[] supportCardIds)
    {
        if (!Object.HasStateAuthority) return;

        Owner = Object.InputAuthority;
        ChampionId = championId;

        int championHP = DefaultHP;
        List<CardGrantRef> grants = null;

        if (_cardLoading != null && _cardLoading.TryGetCardData(championId, out CardData champData))
        {
            if (champData.hp > 0) championHP = champData.hp;
            grants = champData.grants_cards;
        }

        HP = championHP;
        MaxHP = championHP;

        // Champion card always at hand[0]
        Hand.Set(ChampionSlot, championId);
        HandCount = 1;

        int deckIndex = 0;
        foreach (var cardId in supportCardIds)
        {
            if (deckIndex >= DeckCapacity) break;
            Deck.Set(deckIndex, cardId);
            deckIndex++;
        }

        if (grants != null)
        {
            foreach (var grant in grants)
            {
                for (int i = 0; i < grant.quantity; i++)
                {
                    if (deckIndex >= DeckCapacity) break;
                    Deck.Set(deckIndex, grant.string_id);
                    deckIndex++;
                }
            }
        }

        DeckCount = deckIndex;
        DiscardCount = 0;
        IsSetup = true;

        ShuffleDeck();
        ServerDraw(OpeningHandSize);

        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator != null)
        {
            var roster = coordinator.GetPlayerRosterView(Owner);
            roster?.SendHPChangedRpc(Owner, HP);
        }

        _logger?.Log($"[PlayerCardZoneNetworkView] Setup for {Owner}: champion={championId}, deckSize={DeckCount}, HP={HP}");
    }

    public void ServerDraw(int count)
    {
        if (!Object.HasStateAuthority) return;

        for (int i = 0; i < count; i++)
        {
            if (DeckCount == 0)
                ReshuffleDiscard();

            if (DeckCount > 0 && HandCount < HandMax)
            {
                string drawn = Deck.Get(DeckCount - 1).ToString();
                Deck.Set(DeckCount - 1, string.Empty);
                DeckCount--;

                Hand.Set(HandCount, drawn);
                HandCount++;
            }
        }
    }

    public void ServerDiscardFromHand(int handIndex)
    {
        if (!Object.HasStateAuthority) return;
        if (handIndex < 0 || handIndex >= HandCount) return;

        string card = Hand.Get(handIndex).ToString();

        for (int i = handIndex; i < HandCount - 1; i++)
            Hand.Set(i, Hand.Get(i + 1));
        Hand.Set(HandCount - 1, string.Empty);
        HandCount--;

        if (DiscardCount < DiscardCapacity)
        {
            Discard.Set(DiscardCount, card);
            DiscardCount++;
        }
    }

    public void ServerKeepCards(List<string> keep)
    {
        if (!Object.HasStateAuthority) return;

        if (keep.Count > SupportCardCount)
            keep = keep.GetRange(0, SupportCardCount);

        for (int i = HandCount - 1; i > ChampionSlot; i--)
        {
            string card = Hand.Get(i).ToString();
            if (!keep.Contains(card))
                ServerDiscardFromHand(i);
        }

        DrawPhaseConfirmed = true;
        DrawPhaseNewCards = 0;
    }

    public void ServerApplyDamage(int amount)
    {
        if (!Object.HasStateAuthority) return;
        HP = Mathf.Max(0, HP - amount);

        var coordinator = GameplayNetworkCoordinator.Instance;
        if (coordinator != null)
        {
            var roster = coordinator.GetPlayerRosterView(Owner);
            roster?.SendHPChangedRpc(Owner, HP);
        }
    }

    public void ServerDiscardFusionCard(string cardId)
    {
        if (!Object.HasStateAuthority) return;
        if (string.IsNullOrEmpty(cardId)) return;
        if (DiscardCount >= DiscardCapacity) return;
        Discard.Set(DiscardCount, cardId);
        DiscardCount++;
    }

    // ── Draw Phase Server API ────────────────────────────────────────────

    public void ServerStartDrawPhase()
    {
        if (!Object.HasStateAuthority) return;

        DrawPhaseConfirmed = false;
        DrawPhaseNewCards = 0;

        int drawn = 0;
        for (int i = 0; i < DrawPhaseDrawCount; i++)
        {
            if (DeckCount == 0)
                ReshuffleDiscard();

            if (DeckCount > 0 && HandCount < HandArrayCapacity)
            {
                string card = Deck.Get(DeckCount - 1).ToString();
                Deck.Set(DeckCount - 1, string.Empty);
                DeckCount--;

                Hand.Set(HandCount, card);
                HandCount++;
                drawn++;
            }
        }

        DrawPhaseNewCards = drawn;
        _logger?.Log($"[PlayerCardZoneNetworkView] DrawPhase: {Owner} drew {drawn} cards. Hand={HandCount}");
    }

    public void ServerAutoKeepOnTimeout()
    {
        if (!Object.HasStateAuthority) return;
        if (DrawPhaseConfirmed) return;

        if (HandCount > HandMax)
        {
            for (int i = HandCount - 1; i >= HandMax; i--)
            {
                string card = Hand.Get(i).ToString();
                Hand.Set(i, string.Empty);
                HandCount--;

                if (DiscardCount < DiscardCapacity)
                {
                    Discard.Set(DiscardCount, card);
                    DiscardCount++;
                }
            }
        }

        DrawPhaseConfirmed = true;
        DrawPhaseNewCards = 0;
        _logger?.Log($"[PlayerCardZoneNetworkView] DrawPhase auto-keep for {Owner}. Hand={HandCount}");
    }

    // ── IPlayerCardZoneNetworkBridge ─────────────────────────────────────

    public void SendDrawRpc(PlayerRef player, int count)
        => Rpc_RequestDraw(Runner.LocalPlayer, count);

    public void SendKeepCardsRpc(PlayerRef player, string cardIdsJoined)
        => Rpc_RequestKeepCards(Runner.LocalPlayer, cardIdsJoined);

    public void SendPlayMainPhaseSpellRpc(string cardId, HexCoord target)
        => Rpc_RequestPlayMainPhaseSpell(Runner.LocalPlayer, cardId, target);

    // ── RPCs (client → server) ───────────────────────────────────────────

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_SetPlayerName(PlayerRef sender, string name, RpcInfo info = default)
    {
        PlayerName = name;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestDraw(PlayerRef sender, int count, RpcInfo info = default)
    {
        ServerDraw(count);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestKeepCards(PlayerRef sender, string cardIdsJoined, RpcInfo info = default)
    {
        var keep = new List<string>(cardIdsJoined.Split(','));
        ServerKeepCards(keep);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestPlayMainPhaseSpell(PlayerRef sender, string cardId, HexCoord target, RpcInfo info = default)
    {
        if (_gameState != null && _gameState.IsReady(sender))
        {
            _logger?.LogWarning($"[PlayerCardZone] MainPhaseSpell rejected: player {sender} already confirmed fusion.");
            return;
        }

        bool found = false;
        for (int i = 0; i < HandCount; i++)
        {
            if (Hand.Get(i).ToString() == cardId)
            {
                ServerDiscardFromHand(i);
                found = true;
                break;
            }
        }

        if (!found)
        {
            _logger?.LogWarning($"[PlayerCardZone] MainPhaseSpell rejected: card '{cardId}' not in hand.");
            return;
        }

        ExecuteMainPhaseSpell(cardId, target);
    }

    private void ExecuteMainPhaseSpell(string cardId, HexCoord target)
    {
        if (_cardLoading == null || _behaviorRegistry == null) return;

        if (!_cardLoading.TryGetCardData(cardId, out CardData cardData))
        {
            _logger?.LogWarning($"[PlayerCardZone] No card data for '{cardId}'.");
            return;
        }

        if (string.IsNullOrEmpty(cardData.main_phase_spell_behavior_id))
        {
            _logger?.LogWarning($"[PlayerCardZone] Card '{cardId}' has no main_phase_spell_behavior_id.");
            return;
        }

        if (!_behaviorRegistry.TryGetMainPhaseSpellBehavior(cardData.main_phase_spell_behavior_id, out var behaviorSO))
        {
            _logger?.LogWarning($"[PlayerCardZone] Behavior '{cardData.main_phase_spell_behavior_id}' not found in registry.");
            return;
        }

        var spellBehavior = behaviorSO as MainPhaseSpellBehaviorSO;
        if (spellBehavior == null)
        {
            _logger?.LogWarning($"[PlayerCardZone] Behavior '{cardData.main_phase_spell_behavior_id}' is not a MainPhaseSpellBehaviorSO.");
            return;
        }

        spellBehavior.Execute(Owner, target, _unitSubsystem, _boardSubsystem, _cardLoading, _logger);
        _logger?.Log($"[PlayerCardZone] Executed main phase spell '{cardData.main_phase_spell_behavior_id}' at ({target.P},{target.Q}).");
    }

    // ── State push (server → all clients) ────────────────────────────────

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

        var hand = new List<string>();
        for (int i = 0; i < HandCount; i++)
        {
            var s = Hand.Get(i).ToString();
            if (!string.IsNullOrEmpty(s)) hand.Add(s);
        }

        _subsystem.OnAuthoritativeStateReceived(new PlayerCardZonePrivateData
        {
            Owner = Owner,
            HP = HP,
            Hand = hand,
            DeckCount = DeckCount,
            DiscardCount = DiscardCount,
            DrawPhaseNewCards = DrawPhaseNewCards,
            IsDrawPhaseConfirmed = DrawPhaseConfirmed
        });
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void ShuffleDeck()
    {
        var rand = new System.Random();
        for (int i = DeckCount - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            string temp = Deck.Get(i).ToString();
            Deck.Set(i, Deck.Get(j));
            Deck.Set(j, temp);
        }
    }

    private void ReshuffleDiscard()
    {
        if (DiscardCount == 0) return;

        var cards = new List<string>();
        for (int i = 0; i < DiscardCount; i++)
        {
            cards.Add(Discard.Get(i).ToString());
            Discard.Set(i, string.Empty);
        }
        DiscardCount = 0;

        var rand = new System.Random();
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }

        for (int i = 0; i < cards.Count && i < DeckCapacity; i++)
        {
            Deck.Set(i, cards[i]);
        }
        DeckCount = cards.Count;
    }
}
