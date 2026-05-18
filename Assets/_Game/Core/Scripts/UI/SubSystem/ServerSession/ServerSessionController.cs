using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using Zenject;

public class ServerSessionController : IServerSessionController
{
    [Inject] private readonly IServerSessionModel _model;
    [Inject] private readonly INetworkManagerSubsystem _networkManager;
    [Inject] private readonly IBackendBridgeSubsystem _backendBridge;
    [Inject] private readonly IDebugLogger _logger;

    private StartSessionCommand _currentCommand;
    private readonly Dictionary<PlayerRef, string> _playerMap = new();
    private DateTime? _matchStartTime;
    private bool _isMatchOver;

    public void Initialize()
    {
        if (!Application.isBatchMode) return;

        _backendBridge.StartSessionReceived += StartSession;
        _networkManager.PlayerJoined += OnPlayerJoined;
        _networkManager.PlayerLeft += OnPlayerLeft;

        _logger.Log("[ServerSession] Controller initialized.");
    }

    public void Dispose()
    {
        if (Application.isBatchMode)
        {
            _backendBridge.StartSessionReceived -= StartSession;
            _networkManager.PlayerJoined -= OnPlayerJoined;
            _networkManager.PlayerLeft -= OnPlayerLeft;
        }
        _ = ShutdownSession();
    }

    public async void StartSession(StartSessionCommand cmd)
    {
        if (!Application.isBatchMode) return;
        
        _currentCommand = cmd;
        _playerMap.Clear();
        _matchStartTime = null;
        _isMatchOver = false;

        _logger.Log($"[ServerSession] Starting session: {cmd.SessionName}");

        var args = new StartGameArgs
        {
            GameMode = GameMode.Server,
            SessionName = cmd.SessionName,
            PlayerCount = 2,
            IsVisible = false,
            IsOpen = true,
        };

        bool success = await _networkManager.StartSession(args);
        
        if (success)
        {
            await _backendBridge.NotifyMatchCreatedAsync(cmd.SessionName, cmd.Player1UserId, cmd.Player2UserId);

            _model.ApplyState(new ServerSessionStateData
            {
                ActiveSessionName = cmd.SessionName,
                IsRunning = true,
                LastJoinedPlayer = PlayerRef.None,
                LastLeftPlayer = PlayerRef.None
            });
            _logger.Log($"[ServerSession] Session ready: {cmd.SessionName}");
        }
        else
        {
            _logger.LogError($"[ServerSession] Failed to start session: {_networkManager.ErrorMessage}");
        }
    }

    public void OnPlayerJoined(PlayerRef player)
    {
        if (!Application.isBatchMode) return;
        if (!_model.IsRunning.Value) return;

        _logger.Log($"[ServerSession] Player joined Fusion: {player}");

        if (!_playerMap.ContainsKey(player))
        {
            if (_playerMap.Count == 0)
            {
                _playerMap[player] = _currentCommand.Player1UserId;
                _logger.Log($"[ServerSession] Mapped {player} to Player 1: {_currentCommand.Player1UserId}");
            }
            else if (_playerMap.Count == 1)
            {
                _playerMap[player] = _currentCommand.Player2UserId;
                _logger.Log($"[ServerSession] Mapped {player} to Player 2: {_currentCommand.Player2UserId}");
            }
        }

        _model.ApplyState(new ServerSessionStateData
        {
            ActiveSessionName = _model.ActiveSessionName.Value,
            IsRunning = _model.IsRunning.Value,
            LastJoinedPlayer = player,
            LastLeftPlayer = _model.LastLeftPlayer.Value
        });

        if (_playerMap.Count == 2 && _matchStartTime == null)
        {
            _matchStartTime = DateTime.UtcNow;
            _logger.Log("[ServerSession] Both players connected. Match officially started!");
        }
    }

    public async void OnPlayerLeft(PlayerRef player)
    {
        if (!Application.isBatchMode) return;
        if (!_model.IsRunning.Value || _isMatchOver) return;

        _logger.Log($"[ServerSession] Player left Fusion: {player}");

        _model.ApplyState(new ServerSessionStateData
        {
            ActiveSessionName = _model.ActiveSessionName.Value,
            IsRunning = _model.IsRunning.Value,
            LastJoinedPlayer = _model.LastJoinedPlayer.Value,
            LastLeftPlayer = player
        });

        if (_playerMap.ContainsKey(player))
        {
            var leavingUserId = _playerMap[player];
            _logger.LogWarning($"[ServerSession] Active player left: {leavingUserId}. Treating as disconnect loss.");

            _isMatchOver = true;

            await _backendBridge.ReportPlayerDisconnectedAsync(leavingUserId);

            string winnerUserId = string.Empty;
            string loserUserId = leavingUserId;

            foreach (var kvp in _playerMap)
            {
                if (kvp.Value != leavingUserId)
                {
                    winnerUserId = kvp.Value;
                    break;
                }
            }

            int duration = _matchStartTime.HasValue ? (int)(DateTime.UtcNow - _matchStartTime.Value).TotalSeconds : 0;
            var result = new MatchResultData
            {
                SessionName = _model.ActiveSessionName.Value,
                WinnerUserId = winnerUserId,
                LoserUserId = loserUserId,
                DurationSeconds = duration,
                EndReason = "Disconnect"
            };

            await _backendBridge.ReportMatchResultAsync(result);
            await ShutdownSession();
        }
        else if (_networkManager.PlayerCount == 0)
        {
            // If no players remain, treat as disconnect-triggered end just in case.
            OnMatchEnded(new MatchResultData
            {
                SessionName = _model.ActiveSessionName.Value,
                EndReason = "Disconnect",
            });
        }
    }
    
    private async void OnMatchEnded(MatchResultData result)
    {
        if (!Application.isBatchMode) return;
        if (_networkManager.Runner == null || !_networkManager.Runner.IsServer) return;

        _logger.Log($"[ServerSession] Match ended. Reporting to BE...");

        await _backendBridge.ReportMatchResultAsync(result);

        await _networkManager.ShutdownRunner();

        _model.ApplyState(new ServerSessionStateData { IsRunning = false });
    }

    public async void EndMatch(string winnerUserId, string loserUserId, string endReason)
    {
        if (_isMatchOver) return;
        _isMatchOver = true;

        _logger.Log($"[ServerSession] Ending match normally. Winner: {winnerUserId}, Loser: {loserUserId}, Reason: {endReason}");

        int duration = _matchStartTime.HasValue ? (int)(DateTime.UtcNow - _matchStartTime.Value).TotalSeconds : 0;
        var result = new MatchResultData
        {
            SessionName = _model.ActiveSessionName.Value,
            WinnerUserId = winnerUserId,
            LoserUserId = loserUserId,
            DurationSeconds = duration,
            EndReason = endReason
        };

        OnMatchEnded(result);
    }

    private async Task ShutdownSession()
    {
        _logger.Log("[ServerSession] Shutting down Fusion session...");
        await _networkManager.ShutdownRunner();

        _model.ApplyState(new ServerSessionStateData
        {
            ActiveSessionName = string.Empty,
            IsRunning = false,
            LastJoinedPlayer = PlayerRef.None,
            LastLeftPlayer = PlayerRef.None
        });

        _playerMap.Clear();
        _matchStartTime = null;
        _isMatchOver = false;
        _currentCommand = null;
    }
}
