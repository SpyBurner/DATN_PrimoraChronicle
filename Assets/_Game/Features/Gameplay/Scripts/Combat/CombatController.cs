using Zenject;

public class CombatController : ICombatController
{
    [Inject] private readonly ICombatModel _model;
    [Inject] private readonly IDebugLogger _debugLogger;

    public void Initialize() { }
    public void Dispose() { }

    public void ExecuteTurn()
    {
        _debugLogger.Log("CombatController: ExecuteTurn");
    }

    public void SkipCombat()
    {
        _debugLogger.Log("CombatController: SkipCombat");
    }
}
