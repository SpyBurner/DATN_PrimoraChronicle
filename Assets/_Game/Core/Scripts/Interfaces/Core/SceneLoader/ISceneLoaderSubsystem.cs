using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public interface ISceneLoaderSubsystem : ISubsystem
{
    // Model change notifications (expose only UnityAction events)
    event UnityAction<bool> IsLoadingChanged;
    event UnityAction<AsyncOperation> CurrentLoadChanged;
    event UnityAction<CancellationTokenSource> SceneTokenChanged;

    // Controller methods (forwarded by the subsystem)
    Task LoadScene(string sceneName);
    Task LoadNetworkedScene(Fusion.NetworkRunner runner, string sceneName);
    Task ReloadScene();
}