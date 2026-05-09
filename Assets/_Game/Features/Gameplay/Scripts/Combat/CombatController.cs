using Zenject;

internal class CombatController : ICombatController
{
    private readonly ICombatModel _model;
    private readonly IDebugLogger _debugLogger;
    private ICombatNetworkBridge _bridge;

    public CombatController(ICombatModel model, IDebugLogger debugLogger)
    {
        _model = model;
        _debugLogger = debugLogger;
    }

    public void Initialize() { }
    public void Dispose() { }

    public void RegisterBridge(ICombatNetworkBridge bridge)
    {
        _bridge = bridge;
        _debugLogger.Log($"[CombatController] Bridge {(_bridge == null ? "unregistered" : "registered")}.");
    }

    public void ExecuteTurn()
    {
        if (_bridge != null)
        {
            _bridge.SendExecuteTurnRpc();
        }
        else
        {
            _debugLogger.Log("CombatController: ExecuteTurn (Local)");
            // Local path implementation would go here
        }
    }

    public void SkipCombat()
    {
        if (_bridge != null)
        {
            _bridge.SendSkipCombatRpc();
        }
        else
        {
            _debugLogger.Log("CombatController: SkipCombat (Local)");
            // Local path implementation would go here
        }
    }

    public void OnAuthoritativeStateReceived(CombatStateData data)
    {
        _model.ApplyState(data);
    }
}
