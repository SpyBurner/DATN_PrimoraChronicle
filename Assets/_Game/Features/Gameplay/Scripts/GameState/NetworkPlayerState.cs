using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayerState : NetworkBehaviour
{
    [Networked] public PlayerRef Player { get; set; }
    [Networked] public int HP { get; set; }
    [Networked] public int MaxHP { get; set; }
    [Networked] public NetworkBool IsAlive { get; set; }
    [Networked] public NetworkBool IsReady { get; set; }
    [Networked] public NetworkBool IsAI { get; set; }

    [Networked, Capacity(6)] public NetworkArray<NetworkString<_16>> Hand { get; }
    [Networked, Capacity(40)] public NetworkArray<NetworkString<_16>> Deck { get; }
    [Networked, Capacity(40)] public NetworkArray<NetworkString<_16>> Discard { get; }

    [Networked] public int HandCount { get; set; }
    [Networked] public int DeckCount { get; set; }
    [Networked] public int DiscardCount { get; set; }

    [Networked] public NetworkString<_16> ChampionID { get; set; }
    [Networked] public int DeployAreaP { get; set; }
    [Networked] public int DeployAreaQ { get; set; }

    public void SetupDeck(string championId, string[] cardIds, int initialHP)
    {
        if (!Object.HasStateAuthority) return;

        ChampionID = championId;
        HP = initialHP;
        MaxHP = initialHP;
        IsAlive = true;
        IsReady = true;

        // Populate deck
        int index = 0;
        foreach (var cardId in cardIds)
        {
            if (index < 40)
            {
                Deck.Set(index, cardId);
                index++;
            }
        }
        DeckCount = index;
        HandCount = 0;
        DiscardCount = 0;
    }

    public void DrawCards(int count)
    {
        if (!Object.HasStateAuthority) return;

        for (int i = 0; i < count; i++)
        {
            if (DeckCount == 0)
            {
                ReshuffleDiscard();
            }

            if (DeckCount > 0 && HandCount < 6)
            {
                string drawnCard = Deck.Get(DeckCount - 1).ToString();
                Deck.Set(DeckCount - 1, string.Empty);
                DeckCount--;

                Hand.Set(HandCount, drawnCard);
                HandCount++;
            }
        }
    }

    public void DiscardCard(int handIndex)
    {
        if (!Object.HasStateAuthority) return;
        if (handIndex < 0 || handIndex >= HandCount) return;

        string discardedCard = Hand.Get(handIndex).ToString();
        
        // Shift remaining hand cards left
        for (int i = handIndex; i < HandCount - 1; i++)
        {
            Hand.Set(i, Hand.Get(i + 1));
        }
        Hand.Set(HandCount - 1, string.Empty);
        HandCount--;

        if (DiscardCount < 40)
        {
            Discard.Set(DiscardCount, discardedCard);
            DiscardCount++;
        }
    }

    private void ReshuffleDiscard()
    {
        if (!Object.HasStateAuthority) return;
        if (DiscardCount == 0) return;

        // Shuffle cards from discard to deck
        System.Random rand = new System.Random();
        List<string> tempCards = new List<string>();
        for (int i = 0; i < DiscardCount; i++)
        {
            tempCards.Add(Discard.Get(i).ToString());
            Discard.Set(i, string.Empty);
        }
        DiscardCount = 0;

        // Simple Fisher-Yates shuffle
        for (int i = tempCards.Count - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            string temp = tempCards[i];
            tempCards[i] = tempCards[j];
            tempCards[j] = temp;
        }

        int index = 0;
        foreach (var card in tempCards)
        {
            Deck.Set(index, card);
            index++;
        }
        DeckCount = index;
    }
}
