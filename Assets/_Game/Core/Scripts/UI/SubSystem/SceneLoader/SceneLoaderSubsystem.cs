using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityObservables;
using Zenject;

public class SceneLoaderSubsystem : ISceneLoaderSubsystem
{
    [Inject] private readonly ISceneLoaderController _controller;
    [Inject] private readonly ISceneLoaderModel _sceneModel;

    public event UnityAction<bool> IsLoadingChanged;
    public event UnityAction<AsyncOperation> CurrentLoadChanged;
    public event UnityAction<CancellationTokenSource> SceneTokenChanged;

    public void Initialize()
    {
        if (_sceneModel?.IsLoading != null)
            _sceneModel.IsLoading.OnChanged += HandleIsLoadingChanged;

        if (_sceneModel?.CurrentLoad != null)
            _sceneModel.CurrentLoad.OnChanged += HandleCurrentLoadChanged;

        if (_sceneModel?.SceneToken != null)
            _sceneModel.SceneToken.OnChanged += HandleSceneTokenChanged;
    }

    public void Dispose()
    {
        if (_sceneModel?.IsLoading != null)
            _sceneModel.IsLoading.OnChanged -= HandleIsLoadingChanged;

        if (_sceneModel?.CurrentLoad != null)
            _sceneModel.CurrentLoad.OnChanged -= HandleCurrentLoadChanged;

        if (_sceneModel?.SceneToken != null)
            _sceneModel.SceneToken.OnChanged -= HandleSceneTokenChanged;
    }

    // Controller surface - forward calls to the controller
    public Task LoadScene(string sceneName) => _controller.LoadScene(sceneName);

    public Task LoadNetworkedScene(Fusion.NetworkRunner runner, string sceneName) => _controller.LoadNetworkedScene(runner, sceneName);

    public Task ReloadScene() => _controller.ReloadScene();

    // Local handlers that forward the model state to subscribers via UnityAction events
    private void HandleIsLoadingChanged()
    {
        try
        {
            IsLoadingChanged?.Invoke(_sceneModel.IsLoading.Value);
        }
        catch (Exception)
        {
            // swallow to avoid breaking sender
        }
    }

    private void HandleCurrentLoadChanged()
    {
        try
        {
            CurrentLoadChanged?.Invoke(_sceneModel.CurrentLoad.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }

    private void HandleSceneTokenChanged()
    {
        try
        {
            SceneTokenChanged?.Invoke(_sceneModel.SceneToken.Value);
        }
        catch (Exception)
        {
            // swallow
        }
    }
}