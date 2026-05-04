using UnityEngine;
using Zenject;

public class CombatPanel : MonoBehaviour {
    private ICombatSubsystem _subsystem;
    
    [Inject]
    public void Construct(ICombatSubsystem subsystem) {
        _subsystem = subsystem;
    }
}
