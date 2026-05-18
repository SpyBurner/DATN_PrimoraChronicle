using Fusion;

public interface IServerSessionController : IController
{
    void StartSession(StartSessionCommand cmd);
    void OnPlayerJoined(PlayerRef player);
    void OnPlayerLeft(PlayerRef player);
    void EndMatch(string winnerUserId, string loserUserId, string endReason);
}
