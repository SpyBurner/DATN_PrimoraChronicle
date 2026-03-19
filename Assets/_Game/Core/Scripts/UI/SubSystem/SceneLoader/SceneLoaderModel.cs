using System;
using System.Threading;
using UnityEngine;
using UnityObservables;
using Zenject;

internal class SceneLoaderModel : ISceneLoaderModel
{
    private Observable<bool> _isLoading = new(new());
    private Observable<AsyncOperation> _currentLoad = new(new());
    private Observable<CancellationTokenSource> _sceneToken = new(new());

    public Observable<bool> IsLoading { get => _isLoading; }
    public Observable<AsyncOperation> CurrentLoad { get => _currentLoad; }
    public Observable<CancellationTokenSource> SceneToken { get => _sceneToken; }

    public void Initialize()
    {
    }

    public void Dispose()
    {
        // Reset values and dispose token if present
        _isLoading.Value = false;
        _currentLoad.Value = null;

        var token = _sceneToken.Value;
        if (token != null)
        {
            try { token.Cancel(); } catch { /* swallow */ }
            try { token.Dispose(); } catch { /* swallow */ }
        }

        _sceneToken.Value = null;
    }
}