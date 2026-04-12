using Core;
using Zenject;

internal class CoreInstaller : MonoInstaller
{
    public UIMappingSO UIMapping;
    public override void InstallBindings()
    {
        Container.BindInstance(UIMapping).AsSingle();

        // Bind Helpers - concrete implementation must exist (e.g. DebugLogger)
        Container.BindInterfacesAndSelfTo<DebugLogger>()
            .AsSingle().NonLazy();

        // UIManager
        Container.BindInterfacesAndSelfTo<UIManagerModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<UIManagerController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<UIManagerSubsystem>()
            .AsSingle().NonLazy();

        // SceneLoader (use the concrete names you implemented)
        Container.BindInterfacesAndSelfTo<SceneLoaderController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<SceneLoaderModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<SceneLoaderSubsystem>()
            .AsSingle().NonLazy();
    }
}