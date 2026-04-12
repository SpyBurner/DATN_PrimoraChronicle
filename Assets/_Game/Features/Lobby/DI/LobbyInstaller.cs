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
    }
}
