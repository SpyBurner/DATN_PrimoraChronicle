internal interface IDrawPhaseController : IController
{
    void StartDraw(int count);
    void CompleteDraw();
    void RegisterBridge(IDrawPhaseNetworkBridge bridge);
    void OnAuthoritativeStateReceived(DrawPhaseStateData data);
}

