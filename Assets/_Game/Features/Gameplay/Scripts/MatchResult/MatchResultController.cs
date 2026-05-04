using System;

public class MatchResultController : IMatchResultController {
    private readonly IMatchResultModel _model;
    public MatchResultController(IMatchResultModel model) {
        _model = model;
    }
    public void Initialize() { }
    public void Dispose() { }
}
