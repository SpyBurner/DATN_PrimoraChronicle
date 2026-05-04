using System;

public class BoardSubsystem : IBoardSubsystem {
    public IBoardModel Model { get; }
    public IBoardController Controller { get; }
    
    public BoardSubsystem(IBoardModel model, IBoardController controller) {
        Model = model;
        Controller = controller;
    }
    
    public void Dispose() {
        Model?.Dispose();
        Controller?.Dispose();
    }
}
