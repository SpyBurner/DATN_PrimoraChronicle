using Zenject;

public class LobbyInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // LobbyMain
        Container.BindInterfacesAndSelfTo<LobbyMainModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<LobbyMainController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<LobbyMainSubsystem>()
            .AsSingle().NonLazy();

        // Profile
        Container.BindInterfacesAndSelfTo<ProfileModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<ProfileController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<ProfileSubsystem>()
            .AsSingle().NonLazy();

        // Battle
        Container.BindInterfacesAndSelfTo<BattleModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BattleController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BattleSubsystem>()
            .AsSingle().NonLazy();

        // Deck
        Container.BindInterfacesAndSelfTo<DeckModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<DeckController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<DeckSubsystem>()
            .AsSingle().NonLazy();

        // Shop
        Container.BindInterfacesAndSelfTo<ShopModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<ShopController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<ShopSubsystem>()
            .AsSingle().NonLazy();

        // Setting
        Container.BindInterfacesAndSelfTo<SettingModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<SettingController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<SettingSubsystem>()
            .AsSingle().NonLazy();

        // MatchHistory
        Container.BindInterfacesAndSelfTo<MatchHistoryModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<MatchHistoryController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<MatchHistorySubsystem>()
            .AsSingle().NonLazy();
    }
}
