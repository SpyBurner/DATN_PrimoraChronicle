using Fusion;
using Zenject;
using System.Collections.Generic;

public class HandNetworkView : NetworkBehaviour, IHandNetworkBridge
{
    [Inject] private readonly IHandSubsystem _subsystem;

    private ChangeDetector _changeDetector;

    [Networked, Capacity(10)] public NetworkArray<NetworkString<_16>> NetworkedCards { get; }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority)
            _subsystem.RegisterNetworkBridge(this);

        PushState();
    }

    public override void OnDestroy()
    {
        if (HasInputAuthority)
            _subsystem.RegisterNetworkBridge(null);
    }

    // ── IHandNetworkBridge (upstream: client → server) ────────────────────

    public void SendPlayCardRpc(string cardId) => Rpc_RequestPlayCard(cardId);

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestPlayCard(string cardId)
    {
        for (int i = 0; i < NetworkedCards.Length; i++)
        {
            if (NetworkedCards[i].ToString() == cardId)
            {
                NetworkedCards.Set(i, default);
                break;
            }
        }
    }

    // ── Downstream: server → all clients ────────────────────────────────

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(NetworkedCards))
            {
                PushState();
                break;
            }
        }
    }

    private void PushState()
    {
        var list = new List<string>();
        for (int i = 0; i < NetworkedCards.Length; i++)
        {
            var cardId = NetworkedCards[i].ToString();
            if (!string.IsNullOrEmpty(cardId))
                list.Add(cardId);
        }

        _subsystem.OnAuthoritativeStateReceived(new HandStateData
        {
            Cards = list
        });
    }
}
