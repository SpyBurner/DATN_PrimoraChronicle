using UnityObservables;

public class CombatModel : ICombatModel
{
    private Observable<string> _currentAttackerId = new("");
    public Observable<string> CurrentAttackerId => _currentAttackerId;

    private Observable<string> _currentDefenderId = new("");
    public Observable<string> CurrentDefenderId => _currentDefenderId;

    private Observable<string> _combatLog = new("");
    public Observable<string> CombatLog => _combatLog;

    public void Initialize() { }

    public void Dispose()
    {
        _currentAttackerId.Value = "";
        _currentDefenderId.Value = "";
        _combatLog.Value = "";
    }

    public void ApplyState(CombatStateData data)
    {
        _currentAttackerId.Value = data.CurrentAttackerId.ToString();
        _currentDefenderId.Value = data.CurrentDefenderId.ToString();
        _combatLog.Value = data.CombatLog.ToString();
    }
}
