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

    public void OnAuthoritativeStateReceived(PlayerCardZonePrivateData data) => _model.ApplyState(data);

    public void RequestDraw(PlayerRef p, int count) => _bridge?.SendDrawRpc(p, count);

    public void RequestKeepCards(PlayerRef p, IReadOnlyList<string> keep)
        => _bridge?.SendKeepCardsRpc(p, string.Join(",", keep));

    public void RequestPlayMainPhaseSpell(string cardId, HexCoord target)
        => _bridge?.SendPlayMainPhaseSpellRpc(cardId, target);
}
