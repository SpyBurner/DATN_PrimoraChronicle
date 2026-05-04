using System;

public class CombatController : ICombatController {
    private readonly ICombatModel _model;
    public CombatController(ICombatModel model) {
        _model = model;
    }
    public void Initialize() { }
    public void Dispose() { }
}
