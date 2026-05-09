using Zenject;

public class GameplayInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // GameState Subsystem
        Container.Bind<IGameStateModel>().To<GameStateModel>().AsSingle();
        Container.Bind<IGameStateController>().To<GameStateController>().AsSingle();
        Container.Bind<IGameStateSubsystem>().To<GameStateSubsystem>().AsSingle();

        // Hand Subsystem
        Container.Bind<IHandModel>().To<HandModel>().AsSingle();
        Container.Bind<IHandController>().To<HandController>().AsSingle();
        Container.Bind<IHandSubsystem>().To<HandSubsystem>().AsSingle();

        // FusePhase Subsystem
        Container.Bind<IFusePhaseModel>().To<FusePhaseModel>().AsSingle();
        Container.Bind<IFusePhaseController>().To<FusePhaseController>().AsSingle();
        Container.Bind<IFusePhaseSubsystem>().To<FusePhaseSubsystem>().AsSingle();

        // Board Subsystem
        Container.Bind<IBoardModel>().To<BoardModel>().AsSingle();
        Container.Bind<IBoardController>().To<BoardController>().AsSingle();
        Container.Bind<IBoardSubsystem>().To<BoardSubsystem>().AsSingle();

        // Combat Subsystem
        Container.Bind<ICombatModel>().To<CombatModel>().AsSingle();
        Container.Bind<ICombatController>().To<CombatController>().AsSingle();
        Container.Bind<ICombatSubsystem>().To<CombatSubsystem>().AsSingle();

        // DrawPhase Subsystem
        Container.Bind<IDrawPhaseModel>().To<DrawPhaseModel>().AsSingle();
        Container.Bind<IDrawPhaseController>().To<DrawPhaseController>().AsSingle();
        Container.Bind<IDrawPhaseSubsystem>().To<DrawPhaseSubsystem>().AsSingle();

        // MatchResult Subsystem
        Container.Bind<IMatchResultModel>().To<MatchResultModel>().AsSingle();
        Container.Bind<IMatchResultController>().To<MatchResultController>().AsSingle();
        Container.Bind<IMatchResultSubsystem>().To<MatchResultSubsystem>().AsSingle();

        // StartPhase Subsystem
        Container.Bind<IStartPhaseModel>().To<StartPhaseModel>().AsSingle();
        Container.Bind<IStartPhaseController>().To<StartPhaseController>().AsSingle();
        Container.Bind<IStartPhaseSubsystem>().To<StartPhaseSubsystem>().AsSingle();
    }
}
