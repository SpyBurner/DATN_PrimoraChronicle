using System.Threading.Tasks;

public interface IBattleSetupController : IController
{
    void SetOffline(bool isOffline);
    void SetBotCount(int count);
    void SetPlayerCount(int count);
    Task StartMatchmaking();
}
