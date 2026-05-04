using UnityEngine;
using Zenject;

public class MatchResultPanel : MonoBehaviour {
    private IMatchResultSubsystem _subsystem;
    
    [Inject]
    public void Construct(IMatchResultSubsystem subsystem) {
        _subsystem = subsystem;
    }
}
