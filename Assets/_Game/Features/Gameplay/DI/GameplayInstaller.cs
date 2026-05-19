using Zenject;

public class GameplayInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GameplayDeckSubsystem>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<GameplayDeckChooseModel>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<GameplayDeckChooseController>().AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<GameplayDeckChooseSubsystem>().AsSingle().NonLazy();
    }
}
