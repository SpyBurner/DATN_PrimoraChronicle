public interface IBoardSubsystem : ISubsystem
{
    IBoardModel Model { get; }
    IBoardController Controller { get; }
}
