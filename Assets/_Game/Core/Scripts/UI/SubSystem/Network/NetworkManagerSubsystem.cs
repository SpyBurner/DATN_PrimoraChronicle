using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine.Events;
using Zenject;

public class NetworkManagerSubsystem : INetworkManagerSubsystem
{
    [Inject] private readonly INetworkManagerController _controller;
    [Inject] private readonly INetworkManagerModel _model;

    public event UnityAction<NetworkRunner.States> RunnerStateChanged;
    public event UnityAction<string> SessionNameChanged;
    public event UnityAction<int> PlayerCountChanged;
    public event UnityAction<string> ErrorMessageChanged;

    public NetworkRunner.States RunnerState => _model.RunnerState.Value;
    public string SessionName => _model.SessionName.Value;
    public string Region => _model.Region.Value;
    public int PlayerCount => _model.PlayerCount.Value;
    public int MaxPlayers => _model.MaxPlayers.Value;
    public string ErrorMessage => _model.ErrorMessage.Value;
    public NetworkRunner Runner => _controller.Runner;

    public void Initialize()
    {
        _model.RunnerState.OnChanged += HandleRunnerStateChanged;
        _model.SessionName.OnChanged += HandleSessionNameChanged;
        _model.PlayerCount.OnChanged += HandlePlayerCountChanged;
        _model.ErrorMessage.OnChanged += HandleErrorMessageChanged;
    }

    public void Dispose()
    {
        _model.RunnerState.OnChanged -= HandleRunnerStateChanged;
        _model.SessionName.OnChanged -= HandleSessionNameChanged;
        _model.PlayerCount.OnChanged -= HandlePlayerCountChanged;
        _model.ErrorMessage.OnChanged -= HandleErrorMessageChanged;
    }

    public Task<bool> StartSession(StartGameArgs args) => _controller.StartSession(args);
    public Task ShutdownRunner() => _controller.ShutdownRunner();

    private void HandleRunnerStateChanged() => RunnerStateChanged?.Invoke(_model.RunnerState.Value);
    private void HandleSessionNameChanged() => SessionNameChanged?.Invoke(_model.SessionName.Value);
    private void HandlePlayerCountChanged() => PlayerCountChanged?.Invoke(_model.PlayerCount.Value);
    private void HandleErrorMessageChanged() => ErrorMessageChanged?.Invoke(_model.ErrorMessage.Value);
}
