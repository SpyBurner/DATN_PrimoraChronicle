using UnityEngine;
using Zenject;

public class FusePhasePanel : UIPanel {
    private IFusePhaseSubsystem _subsystem;
    
    [Inject]
    public void Construct(IFusePhaseSubsystem subsystem) {
        _subsystem = subsystem;
    }

    public override void Show()
    {
        base.Show();
        // Subscribe to model observables via subsystem
    }

    public override void Hide()
    {
        base.Hide();
        // Unsubscribe
    }
}
