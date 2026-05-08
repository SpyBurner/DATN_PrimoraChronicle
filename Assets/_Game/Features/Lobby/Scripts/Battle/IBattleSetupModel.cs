using UnityObservables;

public interface IBattleSetupModel : IModel
{
    Observable<bool> IsOffline { get; }
    Observable<int> BotCount { get; }
    Observable<int> PlayerCount { get; }
    Observable<string> ErrorMessage { get; }

    void SetOffline(bool isOffline);
    void SetBotCount(int count);
    void SetPlayerCount(int count);
    void SetErrorMessage(string message);
}
