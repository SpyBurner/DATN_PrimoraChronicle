    public Observable<bool> IsOffline { get => _isOffline; }
    public Observable<int> BotCount { get => _botCount; }
    public Observable<int> PlayerCount { get => _playerCount; }
    public Observable<string> ErrorMessage { get => _errorMessage; }

    public void Initialize() { }

    public void Dispose()
    {
        _isOffline.Value = false;
        _botCount.Value = 0;
        _playerCount.Value = 2;
        _errorMessage.Value = string.Empty;
    }

    public void SetOffline(bool isOffline) => _isOffline.Value = isOffline;
    public void SetBotCount(int count) => _botCount.Value = count;
    public void SetPlayerCount(int count) => _playerCount.Value = count;
    public void SetErrorMessage(string message) => _errorMessage.Value = message;
}
