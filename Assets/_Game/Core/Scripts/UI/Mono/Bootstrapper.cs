using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Core
{
    public class Bootstrapper : MonoBehaviour
    {
        [Inject] private readonly ISceneLoaderSubsystem _sceneLoader;
        [Inject] private readonly IUIManagerSubsystem _uiManager;

        [SerializeField] private string _nextSceneName = SceneNames.ACCOUNT;

        private async void Start()
        {
            Debug.Log("Bootstrapper starting...");
            await Initialize();
            // Load the initial scene (e.g., Account Scene)
            //await Task.Delay(500);
            await _sceneLoader.LoadScene(_nextSceneName);
            // Ensure the default UI for the loaded scene is shown
            await _uiManager.ShowDefaultScreenForScene(_nextSceneName);
        }

        private async Task Initialize()
        {
            await Task.Yield(); // Ensure this runs after all Awake() methods
            /**
                * Any additional initialization logic can be placed here.
                * For example, you could preload certain UI panels or resources if needed.
                * However, in this architecture, panels are typically instantiated when their scenes load,
                * so we can keep this method focused on any global setup that might be necessary.
                */
            Debug.Log("Bootstrapper initialization complete.");
        }
    }
}
