using System.Threading.Tasks;
using Zenject;

public interface ISceneLoaderController : IInitializable
{
    Task LoadScene(string sceneName);
    Task ReloadScene();
}