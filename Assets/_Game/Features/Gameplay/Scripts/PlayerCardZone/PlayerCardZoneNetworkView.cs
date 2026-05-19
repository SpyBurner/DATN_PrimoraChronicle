using System.Collections.Generic;
using Core.GDS;
using Fusion;
using UnityEngine;
using Zenject;

public class PlayerCardZoneNetworkView : NetworkBehaviour, IPlayerCardZoneNetworkBridge
{
    [Inject(Optional = true)] private IPlayerCardZoneSubsystem _subsystem;
    [Inject(Optional = true)] private ICardLoadingManagerSubsystem _cardLoading;
    [Inject(Optional = true)] private IProfileSubsystem _profileSubsystem;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Networked] public PlayerRef Owner { get; set; }
    [Networked] public int HP { get; set; }
    [Networked] public int MaxHP { get; set; }
    [Networked] public int HandCount { get; set; }
    [Networked] public int DeckCount { get; set; }
    [Networked] public int DiscardCount { get; set; }
    [Networked] public NetworkBool IsSetup { get; set; }
    [Networked] public NetworkString<_16> ChampionId { get; set; }
    [Networked] public NetworkString<_32> PlayerName { get; set; }

    [Networked, Capacity(6)] public NetworkArray<NetworkString<_32>> Hand { get; }
    [Networked, Capacity(40)] public NetworkArray<NetworkString<_32>> Deck { get; }
    [Networked, Capacity(60)] public NetworkArray<NetworkString<_32>> Discard { get; }

    private ChangeDetector _changeDetector;

    private const int HandMax = 6;
    private const int DeckCapacity = 40;
    private const int DiscardCapacity = 60;
    private const int DefaultHP = 100;
    private const int OpeningHandSize = 6;

    public override void Spawned()
    {
        if (_subsystem == null)
        {
            var ctx = FindObjectOfType<SceneContext>();
            _subsystem = ctx?.Container.Resolve<IPlayerCardZoneSubsystem>();
            _cardLoading = ctx?.Container.Resolve<ICardLoadingManagerSubsystem>();
            _profileSubsystem = ctx?.Container.TryResolve<IProfileSubsystem>();
            _logger = ctx?.Container.Resolve<IDebugLogger>();
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority)
        {
            _subsystem?.RegisterNetworkBridge(this);
            if (Object.HasStateAuthority && _profileSubsystem != null)
                PlayerName = _profileSubsystem.Username ?? string.Empty;
            else if (HasInputAuthority && _profileSubsystem != null)
                Rpc_SetPlayerName(_profileSubsystem.Username ?? string.Empty);
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
        HandCount = 0;
        DiscardCount = 0;
        IsSetup = true;

        ShuffleDeck();
        ServerDraw(OpeningHandSize);

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

        for (int i = HandCount - 1; i >= 0; i--)
        {
            string card = Hand.Get(i).ToString();
            if (!keep.Contains(card))
                ServerDiscardFromHand(i);
        }

    }

    public void ServerApplyDamage(int amount)
    {
        if (!Object.HasStateAuthority) return;
        HP = Mathf.Max(0, HP - amount);
    }

    // ── IPlayerCardZoneNetworkBridge ─────────────────────────────────────

    public void SendDrawRpc(PlayerRef player, int count)
        => Rpc_RequestDraw(count);

    public void SendKeepCardsRpc(PlayerRef player, string cardIdsJoined)
        => Rpc_RequestKeepCards(cardIdsJoined);

    public void SendPlayMainPhaseSpellRpc(string cardId, HexCoord target)
        => Rpc_RequestPlayMainPhaseSpell(cardId, target);

    // ── RPCs (client → server) ───────────────────────────────────────────

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_SetPlayerName(string name)
    {
        PlayerName = name;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestDraw(int count)
    {
        ServerDraw(count);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestKeepCards(string cardIdsJoined)
    {
        var keep = new List<string>(cardIdsJoined.Split(','));
        ServerKeepCards(keep);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestPlayMainPhaseSpell(string cardId, HexCoord target)
    {
        for (int i = 0; i < HandCount; i++)
        {
            if (Hand.Get(i).ToString() == cardId)
            {
                ServerDiscardFromHand(i);
                break;
            }
        }
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

        _subsystem.OnAuthoritativeStateReceived(new PlayerCardZoneData
        {
            Owner = Owner,
            HP = HP,
            Hand = hand,
            DeckCount = DeckCount,
            DiscardCount = DiscardCount,
            PlayerName = PlayerName.ToString()
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
