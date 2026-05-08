using UnityEngine;
using Zenject;

public class CombatPanel : UIPanel {
    private ICombatSubsystem _subsystem;
    
    [Inject]
    public void Construct(ICombatSubsystem subsystem) {
        _subsystem = subsystem;
    }
}
