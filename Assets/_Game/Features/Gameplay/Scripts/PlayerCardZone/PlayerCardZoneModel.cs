using System;
using System.Collections.Generic;
using Fusion;

internal class PlayerCardZoneModel : IPlayerCardZoneModel
{
    private readonly Dictionary<PlayerRef, PlayerCardZoneData> _zones = new();

    public event Action<PlayerRef, int> HPChanged;
    public event Action<PlayerRef, IReadOnlyList<string>> HandChanged;
    public event Action<PlayerRef, int> DeckCountChanged;
    public event Action<PlayerRef, int> DiscardCountChanged;
    public event Action<PlayerRef, string> PlayerNameChanged;

    public IReadOnlyList<string> GetHand(PlayerRef player)
        => _zones.TryGetValue(player, out var z) && z.Hand != null ? z.Hand : new List<string>();

    public int GetDeckCount(PlayerRef player)
        => _zones.TryGetValue(player, out var z) ? z.DeckCount : 0;

    public int GetDiscardCount(PlayerRef player)
        => _zones.TryGetValue(player, out var z) ? z.DiscardCount : 0;

    public int GetHP(PlayerRef player)
        => _zones.TryGetValue(player, out var z) ? z.HP : 0;

    public string GetPlayerName(PlayerRef player)
        => _zones.TryGetValue(player, out var z) ? z.PlayerName ?? string.Empty : string.Empty;

    public void Initialize() { }

    public void Dispose() => _zones.Clear();

    public void ApplyState(PlayerCardZoneData data)
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

        if (!exists || prev.PlayerName != data.PlayerName)
            PlayerNameChanged?.Invoke(data.Owner, data.PlayerName ?? string.Empty);
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
