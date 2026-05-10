using System.Threading.Tasks;
using Zenject;

public class BattleSetupController : IBattleSetupController
{
    [Inject] private readonly IBattleSetupModel _model;
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly INetworkManagerSubsystem _networkManager;
    [Inject] private readonly UIManagerSubsystem _uiManager;

    public void Initialize() { }
    public void Dispose() { }

    public void SetOffline(bool isOffline) => _model.SetOffline(isOffline);
    public void SetBotCount(int count) => _model.SetBotCount(count);
    public void SetPlayerCount(int count) => _model.SetPlayerCount(count);

    public async Task StartMatchmaking()
    {
        _debugLogger.Log($"BattleSetup: Starting matchmaking (Offline: {_model.IsOffline.Value}, Bots: {_model.BotCount.Value})");
        await _uiManager.Show<MatchMakingPanel>();
        await _networkManager.StartSession(
            new Fusion.StartGameArgs{
                PlayerCount = _model.PlayerCount.Value,
                GameMode = Fusion.GameMode.AutoHostOrClient,
                SessionName = "BattleSession",
            });
    }
}
