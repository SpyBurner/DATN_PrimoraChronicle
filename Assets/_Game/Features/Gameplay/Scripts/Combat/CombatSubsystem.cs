using System;

public class CombatSubsystem : ICombatSubsystem {
    public ICombatModel Model { get; }
    public ICombatController Controller { get; }
    
    public CombatSubsystem(ICombatModel model, ICombatController controller) {
        Model = model;
        Controller = controller;
    }
    
    public void Dispose() {
        Model?.Dispose();
        Controller?.Dispose();
    }
}
