using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

internal class SceneLoaderController : ISceneLoaderController, IInitializable, IDisposable
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IUIManagerSubsystem _uiManager;
    [Inject] private readonly ISceneLoaderModel _sceneModel;

    public void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void Dispose()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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

        _sceneModel.IsLoading.Value = false;
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

    async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _debugLogger.Log($"Scene '{scene.name}' loaded with mode '{mode}'.");
        await Task.Yield(); // Ensure this runs after all Awake() methods in the new scene
        await _uiManager.ShowDefaultScreenForScene(scene.name);
    }
}