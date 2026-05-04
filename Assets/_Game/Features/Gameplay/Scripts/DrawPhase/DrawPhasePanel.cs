using UnityEngine;
using Zenject;

public class DrawPhasePanel : MonoBehaviour {
    private IDrawPhaseSubsystem _subsystem;
    
    [Inject]
    public void Construct(IDrawPhaseSubsystem subsystem) {
        _subsystem = subsystem;
    }
}
