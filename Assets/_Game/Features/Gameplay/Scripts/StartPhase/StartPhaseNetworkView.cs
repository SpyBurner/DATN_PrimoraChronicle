using Fusion;
using Zenject;
using System.Collections.Generic;

public class StartPhaseNetworkView : NetworkBehaviour, IStartPhaseNetworkBridge
{
    [Inject] private readonly IStartPhaseSubsystem _subsystem;

    private ChangeDetector _changeDetector;

    [Networked, Capacity(3)] public NetworkArray<int> NetworkedChampions { get; }
    [Networked] public NetworkBool NetworkedIsReady { get; set; }
    [Networked] public NetworkString<_32> NetworkedStatus { get; set; }

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

    // ── IStartPhaseNetworkBridge (upstream: client → server) ───────────────

    public void SendSetIsReadyRpc(bool ready) => Rpc_RequestSetIsReady(ready);
    public void SendAddChampionRpc(int championId) => Rpc_RequestAddChampion(championId);
    public void SendRemoveChampionRpc(int championId) => Rpc_RequestRemoveChampion(championId);

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestSetIsReady(bool ready)
    {
        NetworkedIsReady = ready;
        NetworkedStatus = ready ? "Waiting for opponent..." : "Selecting champions...";
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestAddChampion(int championId)
    {
        for (int i = 0; i < NetworkedChampions.Length; i++)
        {
            if (NetworkedChampions[i] == 0)
            {
                NetworkedChampions.Set(i, championId);
                break;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestRemoveChampion(int championId)
    {
        for (int i = 0; i < NetworkedChampions.Length; i++)
        {
            if (NetworkedChampions[i] == championId)
            {
                NetworkedChampions.Set(i, 0);
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
            PushState();
            break;
        }
    }

    private void PushState()
    {
        var list = new List<int>();
        for (int i = 0; i < NetworkedChampions.Length; i++)
        {
            if (NetworkedChampions[i] != 0)
                list.Add(NetworkedChampions[i]);
        }

        _subsystem.OnAuthoritativeStateReceived(new StartPhaseStateData
        {
            SelectedChampions = list,
            IsReady = NetworkedIsReady,
            Status = NetworkedStatus.ToString()
        });
    }
}
