public interface IMatchResultController : IController
{
    void ShowResult(bool victory, int gold, int rank);
    void BackToLobby();
}
