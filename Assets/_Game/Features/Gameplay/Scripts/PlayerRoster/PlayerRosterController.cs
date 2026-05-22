using System.Collections.Generic;
using Fusion;
using Zenject;

internal class PlayerRosterController : IPlayerRosterController
{
    [Inject] private readonly IPlayerRosterModel _model;
    [Inject] private readonly IDebugLogger _logger;

    private readonly Dictionary<PlayerRef, IPlayerRosterNetworkBridge> _bridges = new();

    public void Initialize() { }

    public void Dispose() => _bridges.Clear();

    public void RegisterBridge(PlayerRef owner, IPlayerRosterNetworkBridge bridge)
    {
        if (bridge == null)
            _bridges.Remove(owner);
        else
            _bridges[owner] = bridge;

        _logger.Log($"[PlayerRoster] Bridge for {owner} {(bridge == null ? "unregistered" : "registered")}.");
    }

    public void OnAuthoritativeStateReceived(PlayerRosterPublicData data) => _model.ApplyState(data);
}
