using System.Threading.Tasks;

public interface IBattleController : IController
{
    Task InitializeBattleSetup();
    void SetIsReady(bool isReady);
}
