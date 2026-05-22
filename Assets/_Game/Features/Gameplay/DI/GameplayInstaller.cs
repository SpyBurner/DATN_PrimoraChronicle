using Zenject;

public class GameplayInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // DeckChoose (existing)
        Container.BindInterfacesAndSelfTo<GameplayDeckSubsystem>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<GameplayDeckChooseModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<GameplayDeckChooseController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<GameplayDeckChooseSubsystem>().AsSingle().NonLazy();

        // GameState
        Container.BindInterfacesAndSelfTo<GameStateModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<GameStateController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<GameStateSubsystem>().AsSingle().NonLazy();

        // Board
        Container.BindInterfacesAndSelfTo<BoardModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BoardController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BoardSubsystem>().AsSingle().NonLazy();

        // Unit
        Container.BindInterfacesAndSelfTo<UnitModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<UnitController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<UnitSubsystem>().AsSingle().NonLazy();

        // PlayerRoster
        Container.BindInterfacesAndSelfTo<PlayerRosterModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<PlayerRosterController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<PlayerRosterSubsystem>().AsSingle().NonLazy();

        // MatchRewards
        Container.BindInterfacesAndSelfTo<MatchRewardsModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<MatchRewardsController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<MatchRewardsSubsystem>().AsSingle().NonLazy();

        // Combat
        Container.BindInterfacesAndSelfTo<CombatModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<CombatController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<CombatSubsystem>().AsSingle().NonLazy();

        // PlayerCardZone
        Container.BindInterfacesAndSelfTo<PlayerCardZoneModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<PlayerCardZoneController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<PlayerCardZoneSubsystem>().AsSingle().NonLazy();

        // Fusion
        Container.BindInterfacesAndSelfTo<FusionModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<FusionController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<FusionSubsystem>().AsSingle().NonLazy();

        // TileEffect
        Container.BindInterfacesAndSelfTo<TileEffectModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<TileEffectController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<TileEffectSubsystem>().AsSingle().NonLazy();

        // DamagePipeline
        Container.BindInterfacesAndSelfTo<DamagePipelineSubsystem>().AsSingle().NonLazy();

        // BehaviorRegistry
        Container.BindInterfacesAndSelfTo<BehaviorRegistryModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BehaviorRegistryController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BehaviorRegistrySubsystem>().AsSingle().NonLazy();

        // MatchResult
        Container.BindInterfacesAndSelfTo<MatchResultModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<MatchResultController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<MatchResultSubsystem>().AsSingle().NonLazy();

        // Targeting (client-side only, no Model/Controller)
        Container.BindInterfacesAndSelfTo<TargetingSubsystem>().AsSingle().NonLazy();
    }
}
