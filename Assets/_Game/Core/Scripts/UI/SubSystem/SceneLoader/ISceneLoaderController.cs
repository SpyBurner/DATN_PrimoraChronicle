using System.Threading.Tasks;

public interface ISceneLoaderController : IController
{
    Task LoadScene(string sceneName);
    Task LoadNetworkedScene(Fusion.NetworkRunner runner, string sceneName);
    Task ReloadScene();
}