public struct DrawPhaseStateData
{
    public int CardsToDraw;
    public bool IsDrawing;
}

public interface IDrawPhaseNetworkBridge
{
    void SendStartDrawRpc(int count);
    void SendCompleteDrawRpc();
}
