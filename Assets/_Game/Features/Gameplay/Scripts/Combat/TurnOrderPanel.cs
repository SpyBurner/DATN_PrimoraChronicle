using UnityEngine;
using Zenject;

public class TurnOrderPanel : UIPanel {
    private ICombatSubsystem _subsystem;
    
    [Inject]
    public void Construct(ICombatSubsystem subsystem) {
        _subsystem = subsystem;
    }
}
