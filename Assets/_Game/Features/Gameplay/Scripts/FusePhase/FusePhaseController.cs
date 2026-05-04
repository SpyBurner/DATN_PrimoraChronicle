using System;

public class FusePhaseController : IFusePhaseController {
    private readonly IFusePhaseModel _model;
    public FusePhaseController(IFusePhaseModel model) {
        _model = model;
    }
    public void Initialize() { }
    public void Dispose() { }
}
