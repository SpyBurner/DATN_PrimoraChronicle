using UnityEngine;
using Zenject;

public class MatchResultPanel : UIPanel {
    private IMatchResultSubsystem _subsystem;
    
    [Inject]
    public void Construct(IMatchResultSubsystem subsystem) {
        _subsystem = subsystem;
    }
}
