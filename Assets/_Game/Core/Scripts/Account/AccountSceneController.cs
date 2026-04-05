using UnityEngine;
using Zenject;

public class AccountSceneController : MonoBehaviour
{
    [Inject] private readonly DiContainer _container;
    [Inject] private readonly IUIManagerSubsystem _uiManager;
    [Inject] private readonly IDebugLogger _debugLogger;

    private async void Start()
    {
        _debugLogger.Log("[Account] Instantiating account panels...");

        var screenLayer = GameObject.Find("Screen Layer");
        var parent = screenLayer != null ? screenLayer.transform : transform;

        _container.InstantiatePrefab(_uiManager.GetPrefab(UIIdentifier.ACCOUNT_LOGIN), parent);
        _container.InstantiatePrefab(_uiManager.GetPrefab(UIIdentifier.ACCOUNT_REGISTER), parent);

        _debugLogger.Log("[Account] Showing login screen...");
        await _uiManager.ShowScreen<AccountLoginPanel>();
    }
}
