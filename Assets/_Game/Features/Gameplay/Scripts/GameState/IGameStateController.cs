public interface IGameStateController : IController
{
    void StartMatch();
    void EndTurn();
    void SetPhase(string phase);
}
