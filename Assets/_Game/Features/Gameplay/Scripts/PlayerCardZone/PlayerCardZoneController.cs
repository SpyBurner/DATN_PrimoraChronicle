using System.Collections.Generic;
using Fusion;
using Zenject;

internal class PlayerCardZoneController : IPlayerCardZoneController
{
    [Inject] private readonly IPlayerCardZoneModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private IPlayerCardZoneNetworkBridge _bridge;

    public void Initialize() { }

    public void Dispose() => _bridge = null;

    public void RegisterBridge(IPlayerCardZoneNetworkBridge bridge)
    {
        _bridge = bridge;
        _logger.Log($"[PlayerCardZone] Bridge {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(PlayerCardZoneData data) => _model.ApplyState(data);

    public void RequestDraw(PlayerRef player, int count)
    {
        if (_bridge != null) _bridge.SendDrawRpc(player, count);
    }

    public void RequestKeepCards(PlayerRef player, IReadOnlyList<string> keep)
    {
        if (_bridge != null) _bridge.SendKeepCardsRpc(player, string.Join(",", keep));
    }

    public void RequestPlayMainPhaseSpell(string cardId, HexCoord target)
    {
        if (_bridge != null) _bridge.SendPlayMainPhaseSpellRpc(cardId, target);
    }
}
