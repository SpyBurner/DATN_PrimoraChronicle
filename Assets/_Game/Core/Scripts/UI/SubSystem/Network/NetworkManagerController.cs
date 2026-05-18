using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using Zenject;

internal class NetworkManagerController : INetworkManagerController, INetworkRunnerCallbacks
{
    [Inject] private readonly INetworkManagerModel _model;
    [Inject] private readonly IDebugLogger _debugLogger;
    public NetworkRunner Runner { get; private set; }

    public void Initialize() { }

    public void Dispose()
    {
        _ = ShutdownRunner();
    }

    public async Task<bool> StartSession(StartGameArgs args)
    {
        try
        {
            _debugLogger.Log($"[NetworkController] Starting session — Mode: {args.GameMode}");
            _model.SetErrorMessage(string.Empty);

            if (Runner == null)
            {
                var go = new GameObject("[NetworkRunner]");
                GameObject.DontDestroyOnLoad(go);
                Runner = go.AddComponent<NetworkRunner>();
                go.AddComponent<NetworkSceneManagerDefault>();
                Runner.ProvideInput = !Application.isBatchMode;

                Runner.AddCallbacks(this);
            }
            
            var result = await Runner.StartGame(args);

            if (result.Ok)
            {
                _debugLogger.Log($"[NetworkController] Session started: {Runner.SessionInfo.Name}");
                _model.SetSessionName(Runner.SessionInfo.Name);
                _model.SetRegion(Runner.SessionInfo.Region);
                _model.SetMaxPlayers(Runner.SessionInfo.MaxPlayers);
                _model.SetPlayerCount(Runner.SessionInfo.PlayerCount);
                _model.SetRunnerState(NetworkRunner.States.Running);
                return true;
            }
            else
            {
                var err = result.ShutdownReason.ToString();
                _debugLogger.LogError($"[NetworkController] StartGame failed: {err}");
                _model.SetErrorMessage(err);
                _model.SetRunnerState(NetworkRunner.States.Shutdown);
                return false;
            }
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"[NetworkController] StartSession exception: {ex.Message}");
            _model.SetErrorMessage(ex.Message);
            _model.SetRunnerState(NetworkRunner.States.Shutdown);
            return false;
        }
    }

    public async Task ShutdownRunner()
    {
        if (Runner == null) return;
        try
        {
            _debugLogger.Log("[NetworkController] Shutting down runner.");
            await Runner.Shutdown();
        }
        catch (Exception ex)
        {
            _debugLogger.LogError($"[NetworkController] ShutdownRunner exception: {ex.Message}");
        }
        finally
        {
            if (Runner != null && Runner.gameObject != null)
                GameObject.Destroy(Runner.gameObject);
            Runner = null;
            _model.SetRunnerState(NetworkRunner.States.Shutdown);
            _model.SetSessionName(string.Empty);
            _model.SetPlayerCount(0);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        _model.SetPlayerCount(runner.SessionInfo.PlayerCount);
        _model.SetLastJoinedPlayer(player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _model.SetPlayerCount(runner.SessionInfo.PlayerCount);
        _model.SetLastLeftPlayer(player);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        _model.SetRunnerState(NetworkRunner.States.Shutdown);
        _model.SetSessionName(string.Empty);
        _model.SetPlayerCount(0);
        if (shutdownReason != ShutdownReason.Ok) _model.SetErrorMessage(shutdownReason.ToString());
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        _model.SetErrorMessage(reason.ToString());
        _model.SetRunnerState(NetworkRunner.States.Shutdown);
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        _model.SetErrorMessage(reason.ToString());
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnObjectWordsChanged(NetworkRunner runner, NetworkObject obj, HashSet<PlayerRef> changedWordsPlayers) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
