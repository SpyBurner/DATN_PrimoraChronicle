using System.Threading.Tasks;
using UnityEngine.Events;

public interface IBattleSetupSubsystem : ISubsystem
{
    event UnityAction<bool> IsOfflineChanged;
    event UnityAction<int> BotCountChanged;
    event UnityAction<int> PlayerCountChanged;

    void SetOffline(bool isOffline);
    void SetBotCount(int count);
    void SetPlayerCount(int count);
    Task StartMatchmaking();
}
