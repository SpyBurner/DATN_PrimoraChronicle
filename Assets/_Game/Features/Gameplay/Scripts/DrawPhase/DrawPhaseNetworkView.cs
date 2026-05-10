using Fusion;
using Zenject;

public class DrawPhaseNetworkView : NetworkBehaviour, IDrawPhaseNetworkBridge
{
    [Inject] private readonly IDrawPhaseSubsystem _subsystem;

    private ChangeDetector _changeDetector;

    [Networked] public int NetworkedCardsToDraw { get; set; }
    [Networked] public NetworkBool NetworkedIsDrawing { get; set; }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasInputAuthority)
            _subsystem.RegisterNetworkBridge(this);

        PushState();
    }

    public void OnDestroy()
    {
        if (HasInputAuthority)
            _subsystem.RegisterNetworkBridge(null);
    }

    // ── IDrawPhaseNetworkBridge (upstream: client → server) ───────────────

    public void SendStartDrawRpc(int count) => Rpc_RequestStartDraw(count);
    public void SendCompleteDrawRpc() => Rpc_RequestCompleteDraw();

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestStartDraw(int count)
    {
        NetworkedCardsToDraw = count;
        NetworkedIsDrawing = true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestCompleteDraw()
    {
        NetworkedIsDrawing = false;
        NetworkedCardsToDraw = 0;
    }

    // ── Downstream: server → all clients ────────────────────────────────

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var _ in _changeDetector.DetectChanges(this))
        {
            PushState();
            break;
        }
    }

    private void PushState()
    {
        _subsystem.OnAuthoritativeStateReceived(new DrawPhaseStateData
        {
            CardsToDraw = NetworkedCardsToDraw,
            IsDrawing = NetworkedIsDrawing
        });
    }
}
