using Zenject;

public class GameplayInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // GameState Subsystem
        // Note: GameStateModel is a NetworkBehaviour, so it must be bound from the hierarchy or a prefab factory.
        Container.BindInterfacesAndSelfTo<GameStateModel>()
            .FromComponentInHierarchy().AsSingle().NonLazy();
            
        Container.BindInterfacesAndSelfTo<GameStateController>()
            .AsSingle().NonLazy();
            
        Container.BindInterfacesAndSelfTo<GameStateSubsystem>()
            .AsSingle().NonLazy();

        // Hand Subsystem (NetworkBehaviour)
        Container.BindInterfacesAndSelfTo<HandModel>()
            .FromComponentInHierarchy().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<HandController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<HandSubsystem>().AsSingle().NonLazy();

        // FusePhase Subsystem
        Container.BindInterfacesAndSelfTo<FusePhaseModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<FusePhaseController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<FusePhaseSubsystem>().AsSingle().NonLazy();

        // Board Subsystem (NetworkBehaviour)
        Container.BindInterfacesAndSelfTo<BoardModel>()
            .FromComponentInHierarchy().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BoardController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BoardSubsystem>().AsSingle().NonLazy();

        // Combat Subsystem (NetworkBehaviour)
        Container.BindInterfacesAndSelfTo<CombatModel>()
            .FromComponentInHierarchy().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<CombatController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<CombatSubsystem>().AsSingle().NonLazy();

        // DrawPhase Subsystem
        Container.BindInterfacesAndSelfTo<DrawPhaseModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<DrawPhaseController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<DrawPhaseSubsystem>().AsSingle().NonLazy();

        // MatchResult Subsystem
        Container.BindInterfacesAndSelfTo<MatchResultModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<MatchResultController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<MatchResultSubsystem>().AsSingle().NonLazy();

    }
}
