using Fusion;
using UnityEngine.Events;
using Zenject;

public class ServerSessionSubsystem : IServerSessionSubsystem
{
    [Inject] private readonly IServerSessionController _controller;
    [Inject] private readonly IServerSessionModel _model;

    public event UnityAction<string> SessionStarted;
    public event UnityAction<PlayerRef> PlayerJoined;
    public event UnityAction<PlayerRef> PlayerLeft;
    public event UnityAction MatchEnded;

    public void Initialize()
    {
        _model.ActiveSessionName.OnChanged += HandleActiveSessionNameChanged;
        
        _model.LastJoinedPlayer.OnChanged += HandleLastJoinedPlayerChanged;
        _model.LastLeftPlayer.OnChanged += HandleLastLeftPlayerChanged;
        
        _model.IsRunning.OnChanged += HandleIsRunningChanged;

        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.ActiveSessionName.OnChanged -= HandleActiveSessionNameChanged;
        
        _model.LastJoinedPlayer.OnChanged -= HandleLastJoinedPlayerChanged;
        _model.LastLeftPlayer.OnChanged -= HandleLastLeftPlayerChanged;
        
        _model.IsRunning.OnChanged -= HandleIsRunningChanged;

        _controller.Dispose();
    }

    private void HandleActiveSessionNameChanged()
    {
        if (!string.IsNullOrEmpty(_model.ActiveSessionName.Value))
        {
            SessionStarted?.Invoke(_model.ActiveSessionName.Value);
        }
    }

    private void HandleLastJoinedPlayerChanged()
    {
        PlayerJoined?.Invoke(_model.LastJoinedPlayer.Value);
    }

    private void HandleLastLeftPlayerChanged()
    {
        PlayerLeft?.Invoke(_model.LastLeftPlayer.Value);
    }
    
    private void HandleIsRunningChanged()
    {
        if (!_model.IsRunning.Value) 
        {
            MatchEnded?.Invoke();
        }
    }

    public void EndMatch(string winnerUserId, string loserUserId, string endReason)
    {
        _controller.EndMatch(winnerUserId, loserUserId, endReason);
    }
}
