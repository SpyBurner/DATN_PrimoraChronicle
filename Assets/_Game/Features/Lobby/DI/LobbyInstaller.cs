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

        // BattleSetup
        Container.BindInterfacesAndSelfTo<BattleSetupModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BattleSetupController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BattleSetupSubsystem>()
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

        // MatchMaking
        Container.BindInterfacesAndSelfTo<MatchMakingModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<MatchMakingController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<MatchMakingSubsystem>()
            .AsSingle().NonLazy();

        // Popups
        Container.BindInterfacesAndSelfTo<PopupSubsystemModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<PopupSubsystemController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<PopupSubsystem>()
            .AsSingle().NonLazy();
    }
}
