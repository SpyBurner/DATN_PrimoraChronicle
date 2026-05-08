using Zenject;

public class DeckBuildInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<DeckBuildModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<DeckBuildController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<DeckBuildSubsystem>()
            .AsSingle().NonLazy();
    }
}
