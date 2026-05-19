using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class SceneLoaderController : ISceneLoaderController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IUIManagerSubsystem _uiManager;
    [Inject] private readonly ISceneLoaderModel _sceneModel;
    [Inject] private readonly INetworkManagerSubsystem _networkManager;

    public void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        _networkManager.IsSceneLoadingChanged += HandleIsSceneLoadingChanged;
    }

    public void Dispose()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _networkManager.IsSceneLoadingChanged -= HandleIsSceneLoadingChanged;
    }

    public async Task LoadScene(string sceneName)
    {
        if (_sceneModel.IsLoading.Value)
        {
            _debugLogger.LogWarning($"Scene load already in progress. Ignoring request to load scene '{sceneName}'.");
            return;
        }

        _sceneModel.IsLoading.Value = true;

        _debugLogger.Log($"Starting to load scene '{sceneName}'.");
        await _uiManager.FadeOut();
        _debugLogger.Log($"Fade out completed. Loading scene '{sceneName}'.");

        var operation = SceneManager.LoadSceneAsync(sceneName);
        _sceneModel.CurrentLoad.Value = operation;

        while (!operation.isDone)
        {
            _debugLogger.Log($"Loading scene '{sceneName}'... {operation.progress * 100f}%");
            await Task.Yield();
        }

        _debugLogger.Log($"Scene '{sceneName}' loaded successfully.");
        _sceneModel.CurrentLoad.Value = null;

        await _uiManager.FadeIn();
        _debugLogger.Log($"Fade in completed for scene '{sceneName}'.");
        
        // Show the default screen for the newly loaded scene
        await _uiManager.ShowDefaultScreenForScene(sceneName);

        _sceneModel.IsLoading.Value = false;
    }

    public async Task LoadNetworkedScene(Fusion.NetworkRunner runner, string sceneName)
    {
        if (_sceneModel.IsLoading.Value)
        {
            _debugLogger.LogWarning($"Scene load already in progress. Ignoring request to load networked scene '{sceneName}'.");
            return;
        }

        _sceneModel.IsLoading.Value = true;

        _debugLogger.Log($"Starting to load networked scene '{sceneName}'.");
        await _uiManager.FadeOut();
        _debugLogger.Log($"Fade out completed. Loading networked scene '{sceneName}'.");

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.name != sceneName)
        {
            _debugLogger.Log($"Unloading previous local scene '{activeScene.name}' before loading networked scene '{sceneName}'.");
            try
            {
                var unloadOp = SceneManager.UnloadSceneAsync(activeScene);
                if (unloadOp != null)
                {
                    while (!unloadOp.isDone)
                    {
                        await Task.Yield();
                    }
                }
            }
            catch (Exception ex)
            {
                _debugLogger.LogWarning($"Failed to unload active scene '{activeScene.name}': {ex.Message}");
            }
        }

        // Only the authority triggers Fusion's networked scene load.
        // Fusion propagates it to all connected clients automatically.
        // Non-authority clients still perform the local unload above and
        // then wait here for Fusion to push the new scene via callbacks.
        bool isAuthority = runner.IsServer || runner.IsSharedModeMasterClient;
        if (isAuthority)
        {
            _debugLogger.Log($"[SceneLoader] Authority confirmed. Calling runner.LoadScene('{sceneName}').");
            await runner.LoadScene(sceneName);
        }
        else
        {
            _debugLogger.Log($"[SceneLoader] Not authority — skipping runner.LoadScene. Waiting for Fusion to push scene '{sceneName}'.");
        }

        while (_sceneModel.IsLoading.Value)
        {
            await Task.Yield();
        }

        _debugLogger.Log($"Scene '{sceneName}' loaded successfully.");

        await _uiManager.FadeIn();
        _debugLogger.Log($"Fade in completed for networked scene '{sceneName}'.");
        
        // Show the default screen for the newly loaded scene
        await _uiManager.ShowDefaultScreenForScene(sceneName);
    }

    private void HandleIsSceneLoadingChanged(bool isLoading)
    {
        _debugLogger.Log($"IsSceneLoading changed to {isLoading}");
        _sceneModel.IsLoading.Value = isLoading;
    }

    public Task ReloadScene()
    {
        if (_sceneModel.IsLoading.Value)
        {
            _debugLogger.LogWarning("Scene load already in progress. Ignoring request to reload current scene.");
            return Task.CompletedTask;
        }
        _debugLogger.Log("Reloading current scene.");
        var currentScene = SceneManager.GetActiveScene();
        return LoadScene(currentScene.name);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _debugLogger.Log($"Scene '{scene.name}' loaded with mode '{mode}'. Default screen will be handled by UIRoot/UIManager.");
    }
}