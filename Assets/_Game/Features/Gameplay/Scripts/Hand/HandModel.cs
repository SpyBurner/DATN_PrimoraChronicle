using Fusion;
using System.Collections.Generic;
using UnityObservables;

public class HandModel : NetworkBehaviour, IHandModel 
{
    private ChangeDetector _changeDetector;

    [Networked, Capacity(7)] 
    public NetworkArray<NetworkString<_16>> NetworkedCards { get; }

    private Observable<List<string>> _cards = new(new List<string>());
    public Observable<List<string>> Cards { get => _cards; }

    public void Initialize() { }
    public void Dispose() 
    {
        _cards.Value = new List<string>();
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        SyncCards();
    }

    public override void Render()
    {
        if (_changeDetector == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(NetworkedCards))
            {
                SyncCards();
            }
        }
    }

    private void SyncCards()
    {
        var list = new List<string>();
        for (int i = 0; i < NetworkedCards.Length; i++)
        {
            string cardId = NetworkedCards[i].ToString();
            if (!string.IsNullOrEmpty(cardId))
            {
                list.Add(cardId);
            }
        }
        _cards.Value = list;
    }
}
