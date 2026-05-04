using System;

public class HandController : IHandController {
    private readonly IHandModel _model;
    public HandController(IHandModel model) {
        _model = model;
    }
    public void Initialize() { }
    public void Dispose() { }
}
