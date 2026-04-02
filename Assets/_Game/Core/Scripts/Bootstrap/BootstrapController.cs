using System.Threading.Tasks;
using UnityEngine;
using Zenject;

public class BootstrapController : MonoBehaviour
{
    [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;
    [Inject] private readonly IDebugLogger _debugLogger;

    private async void Start()
    {
        _debugLogger.Log("[Bootstrap] Initializing...");

        await InitializeSystems();

        _debugLogger.Log("[Bootstrap] Initialization complete. Loading Account scene...");
        await _sceneLoader.LoadScene(SceneNames.ACCOUNT);
    }

    private async Task InitializeSystems()
    {
        // Allow one frame for all Zenject bindings to fully resolve
        await Task.Yield();

        // TODO: Add any one-time initialization here, for example:
        // - Load player preferences / saved data
        // - Initialize analytics or crash reporting
        // - Check network connectivity
        // - Authenticate with backend services
        // - Warm up object pools
        // - Preload commonly used assets

        _debugLogger.Log("[Bootstrap] All systems initialized.");
    }
}
