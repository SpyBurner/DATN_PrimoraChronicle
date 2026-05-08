public interface IGameStateSubsystem : ISubsystem
{
    IGameStateModel Model { get; }
    IGameStateController Controller { get; }
}
