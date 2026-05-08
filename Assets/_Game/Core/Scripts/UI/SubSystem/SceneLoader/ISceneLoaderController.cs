using System.Threading.Tasks;

public interface ISceneLoaderController : IController
{
    Task LoadScene(string sceneName);
    Task ReloadScene();
}