using UnityEngine;
using Zenject;

internal class CoreInstaller : MonoInstaller
{
    [SerializeField] private UIPrefabRegistrySO _uiPrefabRegistry;

    public override void InstallBindings()
    {
        // Bind Helpers - concrete implementation must exist (e.g. DebugLogger)
        Container.BindInterfacesAndSelfTo<DebugLogger>()
            .AsSingle().NonLazy();

        // UI Prefab Registry
        Container.BindInstance(_uiPrefabRegistry).AsSingle();

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