using UnityEngine;
using Zenject;

public class AccountSceneController : MonoBehaviour
{
    [Inject] private readonly DiContainer _container;
    [Inject] private readonly IUIManagerSubsystem _uiManager;
    [Inject] private readonly IDebugLogger _debugLogger;

    private async void Start()
    {
        _debugLogger.Log("[Account] Instantiating login panel...");

        var prefab = _uiManager.GetPrefab(UIIdentifier.ACCOUNT_LOGIN);
        var screenLayer = GameObject.Find("Screen Layer");
        _container.InstantiatePrefab(prefab, screenLayer != null ? screenLayer.transform : transform);

        _debugLogger.Log("[Account] Showing login screen...");
        await _uiManager.ShowScreen<AccountLoginPanel>();
    }
}
