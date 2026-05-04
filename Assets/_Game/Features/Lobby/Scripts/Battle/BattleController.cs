using System;
using System.Threading.Tasks;
using Zenject;

internal class BattleController : IBattleController
{
    [Inject] private readonly IDebugLogger _debugLogger;
    [Inject] private readonly IBattleModel _model;
    [Inject] private readonly IHttpServiceSubsystem _httpService;

    public void Initialize() { }
    public void Dispose() { }

    public async Task InitializeBattleSetup()
    {
        try
        {
            _debugLogger.Log("Battle: Initializing battle setup");
            var setup = await _httpService.Get<BattleSetupResponse>("https://api.example.com/battle/setup");

            if (setup != null)
            {
                _model.SetOpponentName(setup.opponentName);
                _model.SetOpponentLevel(setup.opponentLevel);
                _model.SetPlayerHP(setup.playerHP);
                _model.SetOpponentHP(setup.opponentHP);
                _model.SetPlayerMaxHP(setup.playerMaxHP);
                _model.SetOpponentMaxHP(setup.opponentMaxHP);
                _debugLogger.Log($"Battle: Setup loaded. Opponent: {setup.opponentName} (Lvl {setup.opponentLevel})");
            }
            else
            {
                _debugLogger.LogError("Battle: Failed to load battle setup");
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"Battle: InitializeBattleSetup failed: {ex.Message}");
        }
    }

    public void SetIsReady(bool isReady)
    {
        _model.SetIsReady(isReady);
    }
}

[System.Serializable]
internal class BattleSetupResponse
{
    public string opponentName;
    public int opponentLevel;
    public int playerHP;
    public int opponentHP;
    public int playerMaxHP;
    public int opponentMaxHP;
}
