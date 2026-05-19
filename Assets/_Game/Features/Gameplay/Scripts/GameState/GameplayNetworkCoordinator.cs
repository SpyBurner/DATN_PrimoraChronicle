using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Zenject;

public class GameplayNetworkCoordinator : NetworkBehaviour
{
    [Inject(Optional = true)] private INetworkManagerSubsystem _networkManager;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Header("Manager Prefabs")]
    [SerializeField] private NetworkPrefabRef _gameStateManagerPrefab;
    [SerializeField] private NetworkPrefabRef _boardManagerPrefab;
    [SerializeField] private NetworkPrefabRef _playerStatePrefab;
    [SerializeField] private NetworkPrefabRef _deckChooseViewPrefab;

    [Header("Player Piece Prefabs")]
    [SerializeField] private NetworkPrefabRef _player1PiecePrefab;
    [SerializeField] private NetworkPrefabRef _player2PiecePrefab;

    [Networked, Capacity(4)] public NetworkArray<NetworkId> PlayerStates { get; }
    [Networked] public int PlayerCount { get; set; }

    private GameStateNetworkView _gameStateView;
    private BoardNetworkView _boardView;
    private readonly Dictionary<PlayerRef, NetworkObject> _playerPieces = new();

    public static GameplayNetworkCoordinator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void Spawned()
    {
        if (!Object.HasStateAuthority) return;

        _logger?.Log("[GameplayNetworkCoordinator] Spawned as StateAuthority. Initializing match...");

        SpawnGameStateManager();
        SpawnBoard();
        SpawnExistingPlayers();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
        _playerPieces.Clear();
    }

    private void Start()
    {
        if (_networkManager != null)
            _networkManager.PlayerJoined += HandlePlayerJoined;
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
            _networkManager.PlayerJoined -= HandlePlayerJoined;
        if (Instance == this) Instance = null;
    }

    // ── Spawning ─────────────────────────────────────────────────────────

    private void SpawnGameStateManager()
    {
        if (!_gameStateManagerPrefab.IsValid)
        {
            _logger?.LogWarning("[GameplayNetworkCoordinator] GameState prefab not assigned.");
            return;
        }

        var obj = Runner.Spawn(_gameStateManagerPrefab, Vector3.zero, Quaternion.identity);
        _gameStateView = obj.GetComponent<GameStateNetworkView>();
        _logger?.Log("[GameplayNetworkCoordinator] Spawned GameStateManager.");
    }

    private void SpawnBoard()
    {
        if (!_boardManagerPrefab.IsValid)
        {
            _logger?.LogWarning("[GameplayNetworkCoordinator] Board prefab not assigned.");
            return;
        }

        var obj = Runner.Spawn(_boardManagerPrefab, Vector3.zero, Quaternion.identity);
        _boardView = obj.GetComponent<BoardNetworkView>();
        _logger?.Log("[GameplayNetworkCoordinator] Spawned BoardManager.");
    }

    private void SpawnExistingPlayers()
    {
        foreach (var player in Runner.ActivePlayers)
        {
            SpawnPlayerState(player);
        }
    }

    private void HandlePlayerJoined(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (_playerPieces.ContainsKey(player)) return;
        SpawnPlayerState(player);
    }

    private void SpawnPlayerState(PlayerRef player)
    {
        if (_playerStatePrefab.IsValid)
        {
            var stateObj = Runner.Spawn(_playerStatePrefab, Vector3.zero, Quaternion.identity, player);
            RegisterPlayerState(stateObj.Id);
            _logger?.Log($"[GameplayNetworkCoordinator] Spawned PlayerState for {player}.");
        }

        if (_deckChooseViewPrefab.IsValid)
        {
            Runner.Spawn(_deckChooseViewPrefab, Vector3.zero, Quaternion.identity, player);
            _logger?.Log($"[GameplayNetworkCoordinator] Spawned DeckChooseView for {player}.");
        }

        int playerIndex = GetPlayerIndex(player);
        var piecePrefab = playerIndex == 0 ? _player1PiecePrefab : _player2PiecePrefab;

        if (piecePrefab.IsValid && _boardView != null)
        {
            var spawnPos = _boardView.GetDeployWorldPosition(playerIndex);
            var spawnRot = _boardView.GetDeployRotation(playerIndex);
            var pieceObj = Runner.Spawn(piecePrefab, spawnPos, spawnRot, player);
            _playerPieces[player] = pieceObj;
            _logger?.Log($"[GameplayNetworkCoordinator] Spawned player piece for {player} at index {playerIndex}.");
        }
    }

    private void RegisterPlayerState(NetworkId stateId)
    {
        for (int i = 0; i < PlayerStates.Length; i++)
        {
            if (!PlayerStates.Get(i).IsValid)
            {
                PlayerStates.Set(i, stateId);
                PlayerCount++;
                return;
            }
        }
    }

    private int GetPlayerIndex(PlayerRef player)
    {
        int index = 0;
        foreach (var p in Runner.ActivePlayers)
        {
            if (p == player) return index;
            index++;
        }
        return 0;
    }

    // ── Public API for other subsystems ──────────────────────────────────

    public GameStateNetworkView GameStateView => _gameStateView;
    public BoardNetworkView BoardView => _boardView;
}
