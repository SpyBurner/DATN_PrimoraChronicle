using System;
using System.Collections.Generic;

internal class PlayerCardZoneModel : IPlayerCardZoneModel
{
    private List<string> _hand = new();

    public event Action<IReadOnlyList<string>> OwnHandChanged;

    public IReadOnlyList<string> GetOwnHand() => _hand;

    public void Initialize() { }

    public void Dispose() => _hand.Clear();

    public void ApplyState(PlayerCardZonePrivateData data)
    {
        _hand = data.Hand ?? new List<string>();
        OwnHandChanged?.Invoke(_hand);
    }
}
