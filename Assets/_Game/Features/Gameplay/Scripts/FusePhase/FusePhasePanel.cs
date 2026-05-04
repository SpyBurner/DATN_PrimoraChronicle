using UnityEngine;
using Zenject;

public class FusePhasePanel : MonoBehaviour {
    private IFusePhaseSubsystem _subsystem;
    
    [Inject]
    public void Construct(IFusePhaseSubsystem subsystem) {
        _subsystem = subsystem;
    }
}
