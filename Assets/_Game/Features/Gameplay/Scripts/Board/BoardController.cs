using System;

public class BoardController : IBoardController {
    private readonly IBoardModel _model;
    public BoardController(IBoardModel model) {
        _model = model;
    }
    public void Initialize() { }
    public void Dispose() { }
}
