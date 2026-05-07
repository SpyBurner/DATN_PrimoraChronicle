    public void SetBotCount(int count) => _model.SetBotCount(count);
    public void SetPlayerCount(int count) => _model.SetPlayerCount(count);

    public async Task StartMatchmaking()
    {
        _debugLogger.Log($"BattleSetup: Starting matchmaking (Offline: {_model.IsOffline.Value}, Bots: {_model.BotCount.Value})");
        // Logic to transition to MatchMaking subsystem or start game directly
        await Task.Yield();
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
