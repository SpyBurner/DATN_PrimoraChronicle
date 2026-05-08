using UnityEngine;
using Zenject;

public class HandPanel : UIPanel {
    private IHandSubsystem _subsystem;
    
    [Inject]
    public void Construct(IHandSubsystem subsystem) {
        _subsystem = subsystem;
    }
}
