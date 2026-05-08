using Fusion;
using UnityObservables;

public class CombatModel : NetworkBehaviour, ICombatModel
{
    private ChangeDetector _changeDetector;

    [Networked] public NetworkString<_16> NetworkedAttacker { get; set; }
    [Networked] public NetworkString<_16> NetworkedDefender { get; set; }
    [Networked] public NetworkString<_64> NetworkedLog { get; set; }

    private Observable<string> _currentAttackerId = new("");
    public Observable<string> CurrentAttackerId => _currentAttackerId;

    private Observable<string> _currentDefenderId = new("");
    public Observable<string> CurrentDefenderId => _currentDefenderId;

    private Observable<string> _combatLog = new("");
    public Observable<string> CombatLog => _combatLog;

    public void Initialize() { }
    public void Dispose() { }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        SyncState();
    }

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            SyncState();
            break;
        }
    }

    private void SyncState()
    {
        _currentAttackerId.Value = NetworkedAttacker.ToString();
        _currentDefenderId.Value = NetworkedDefender.ToString();
        _combatLog.Value = NetworkedLog.ToString();
    }
}
