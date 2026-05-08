using Zenject;

public class CardDetailInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<CardDetailModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<CardDetailController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<CardDetailSubsystem>()
            .AsSingle().NonLazy();
    }
}
