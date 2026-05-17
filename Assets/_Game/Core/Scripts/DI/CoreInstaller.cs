using Core;
using Core.Config;
using Zenject;

internal class CoreInstaller : MonoInstaller
{
    public UIMappingSO UIMapping;
    public ServerConfig ServerConfig;

    public override void InstallBindings()
    {
        // Create MainThreadDispatcher
        var dispatcherGo = new UnityEngine.GameObject("MainThreadDispatcher");
        UnityEngine.Object.DontDestroyOnLoad(dispatcherGo);
        dispatcherGo.AddComponent<MainThreadDispatcher>();

        Container.BindInstance(UIMapping).AsSingle();
        
        if (ServerConfig != null)
        {
            Container.BindInstance(ServerConfig).AsSingle().NonLazy();
        }

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

        // CardLoadingManager
        Container.BindInterfacesAndSelfTo<CardLoadingManagerModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<CardLoadingManagerController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<CardLoadingManagerSubsystem>()
            .AsSingle().NonLazy();

        // SceneLoader (use the concrete names you implemented)
        Container.BindInterfacesAndSelfTo<SceneLoaderController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<SceneLoaderModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<SceneLoaderSubsystem>()
            .AsSingle().NonLazy();

        // HttpService
        Container.BindInterfacesAndSelfTo<HttpServiceModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<HttpServiceController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<HttpServiceSubsystem>()
            .AsSingle().NonLazy();

        // AuthSession
        Container.BindInterfacesAndSelfTo<AuthSessionModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<AuthSessionController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<AuthSessionSubsystem>()
            .AsSingle().NonLazy();

        // AudioManager
        Container.BindInterfacesAndSelfTo<AudioManagerModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<AudioManagerController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<AudioManagerSubsystem>()
            .AsSingle().NonLazy();

        // Network
        Container.BindInterfacesAndSelfTo<NetworkManagerModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<NetworkManagerController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<NetworkManagerSubsystem>()
            .AsSingle().NonLazy();

        // BackendBridge
        Container.BindInterfacesAndSelfTo<BackendBridgeModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BackendBridgeController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<BackendBridgeSubsystem>()
            .AsSingle().NonLazy();

        // ServerSession
        Container.BindInterfacesAndSelfTo<ServerSessionModel>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<ServerSessionController>()
            .AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<ServerSessionSubsystem>()
            .AsSingle().NonLazy();
    }
}