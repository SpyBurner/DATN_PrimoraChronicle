using UnityEngine;
using Zenject;

public class BoardPanel : MonoBehaviour {
    private IBoardSubsystem _subsystem;
    
    [Inject]
    public void Construct(IBoardSubsystem subsystem) {
        _subsystem = subsystem;
    }
}
