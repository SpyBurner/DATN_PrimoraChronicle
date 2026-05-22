using System;
using System.Collections.Generic;
using Fusion;

internal class PlayerRosterModel : IPlayerRosterModel
{
    public event Action<PlayerRef, int> HPChanged;
    public event Action<PlayerRef, string> NameChanged;
    public event Action<PlayerRef, string> UserIdChanged;

    private readonly Dictionary<int, int> _hp = new();
    private readonly Dictionary<int, string> _names = new();
    private readonly Dictionary<int, string> _userIds = new();
    private readonly List<PlayerRef> _allPlayers = new();

    public IReadOnlyList<PlayerRef> AllPlayers => _allPlayers;

    public int GetHP(PlayerRef p) => _hp.TryGetValue(p.RawEncoded, out var v) ? v : 0;
    public string GetName(PlayerRef p) => _names.TryGetValue(p.RawEncoded, out var v) ? v : string.Empty;
    public string GetUserId(PlayerRef p) => _userIds.TryGetValue(p.RawEncoded, out var v) ? v : string.Empty;

    public void Initialize() { }

    public void Dispose()
    {
        _hp.Clear();
        _names.Clear();
        _userIds.Clear();
        _allPlayers.Clear();
    }

    public void ApplyState(PlayerRosterPublicData data)
    {
        int key = data.Owner.RawEncoded;

        if (!_allPlayers.Contains(data.Owner))
            _allPlayers.Add(data.Owner);

        // HP
        bool hpChanged = !_hp.TryGetValue(key, out int prevHp) || prevHp != data.HP;
        _hp[key] = data.HP;
        if (hpChanged) HPChanged?.Invoke(data.Owner, data.HP);

        // Name
        bool nameChanged = !_names.TryGetValue(key, out string prevName) || prevName != data.PlayerName;
        _names[key] = data.PlayerName ?? string.Empty;
        if (nameChanged && !string.IsNullOrEmpty(data.PlayerName))
            NameChanged?.Invoke(data.Owner, data.PlayerName);

        // UserId
        bool uidChanged = !_userIds.TryGetValue(key, out string prevUid) || prevUid != data.UserId;
        _userIds[key] = data.UserId ?? string.Empty;
        if (uidChanged && !string.IsNullOrEmpty(data.UserId))
            UserIdChanged?.Invoke(data.Owner, data.UserId);
    }
}
