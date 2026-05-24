using System;
using System.Collections.Generic;
using Fusion;

internal class PlayerCardZoneModel : IPlayerCardZoneModel
{
    private readonly Dictionary<PlayerRef, PlayerCardZonePrivateData> _zones = new();

    public event Action<PlayerRef, int> HPChanged;
    public event Action<PlayerRef, IReadOnlyList<string>> HandChanged;
    public event Action<PlayerRef, int> DeckCountChanged;
    public event Action<PlayerRef, int> DiscardCountChanged;
    public event Action<PlayerRef, int> DrawPhaseNewCardsChanged;
    public event Action<PlayerRef, bool> DrawPhaseConfirmedChanged;

    public IReadOnlyList<string> GetHand(PlayerRef player)
        => _zones.TryGetValue(player, out var z) ? z.Hand ?? new List<string>() : new List<string>();

    public int GetDeckCount(PlayerRef player)
        => _zones.TryGetValue(player, out var z) ? z.DeckCount : 0;

    public int GetDiscardCount(PlayerRef player)
        => _zones.TryGetValue(player, out var z) ? z.DiscardCount : 0;

    public int GetHP(PlayerRef player)
        => _zones.TryGetValue(player, out var z) ? z.HP : 0;

    public int GetDrawPhaseNewCards(PlayerRef player)
        => _zones.TryGetValue(player, out var z) ? z.DrawPhaseNewCards : 0;

    public bool GetDrawPhaseConfirmed(PlayerRef player)
        => _zones.TryGetValue(player, out var z) && z.IsDrawPhaseConfirmed;

    public void Initialize() { }

    public void Dispose() => _zones.Clear();

    public void ApplyState(PlayerCardZonePrivateData data)
    {
        bool exists = _zones.TryGetValue(data.Owner, out var prev);
        _zones[data.Owner] = data;

        if (!exists || prev.HP != data.HP)
            HPChanged?.Invoke(data.Owner, data.HP);

        if (!exists || !ListsEqual(prev.Hand, data.Hand))
            HandChanged?.Invoke(data.Owner, data.Hand ?? new List<string>());

        if (!exists || prev.DeckCount != data.DeckCount)
            DeckCountChanged?.Invoke(data.Owner, data.DeckCount);

        if (!exists || prev.DiscardCount != data.DiscardCount)
            DiscardCountChanged?.Invoke(data.Owner, data.DiscardCount);

        if (!exists || prev.DrawPhaseNewCards != data.DrawPhaseNewCards)
            DrawPhaseNewCardsChanged?.Invoke(data.Owner, data.DrawPhaseNewCards);

        if (!exists || prev.IsDrawPhaseConfirmed != data.IsDrawPhaseConfirmed)
            DrawPhaseConfirmedChanged?.Invoke(data.Owner, data.IsDrawPhaseConfirmed);
    }

    private static bool ListsEqual(List<string> a, List<string> b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}
