using UnityObservables;

internal class BattleSetupModel : IBattleSetupModel
{
    private Observable<bool> _isOffline = new(false);
    private Observable<int> _botCount = new(0);
    private Observable<int> _playerCount = new(2);
    private Observable<string> _errorMessage = new(string.Empty);

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
