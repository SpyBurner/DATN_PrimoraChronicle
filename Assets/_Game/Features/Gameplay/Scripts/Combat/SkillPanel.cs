using UnityEngine;
using Zenject;

public class SkillPanel : UIPanel {
    private ICombatSubsystem _subsystem;
    
    [Inject]
    public void Construct(ICombatSubsystem subsystem) {
        _subsystem = subsystem;
    }
}
