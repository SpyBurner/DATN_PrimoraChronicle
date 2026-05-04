using System;

public class DrawPhaseController : IDrawPhaseController {
    private readonly IDrawPhaseModel _model;
    public DrawPhaseController(IDrawPhaseModel model) {
        _model = model;
    }
    public void Initialize() { }
    public void Dispose() { }
}
