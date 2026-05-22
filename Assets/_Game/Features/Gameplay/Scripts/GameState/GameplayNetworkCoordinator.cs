using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Zenject;

public class GameplayNetworkCoordinator : NetworkBehaviour
{
    [Inject] private INetworkManagerSubsystem _networkManager;
    [Inject] private IDebugLogger _logger;

    [Header("Manager Prefabs")]
    [SerializeField] private NetworkPrefabRef _gameStateManagerPrefab;
    [SerializeField] private NetworkPrefabRef _boardManagerPrefab;
    [SerializeField] private NetworkPrefabRef _playerStatePrefab;
    [SerializeField] private NetworkPrefabRef _deckChooseViewPrefab;
    [SerializeField] private NetworkPrefabRef _playerCardZoneViewPrefab;
    [SerializeField] private NetworkPrefabRef _playerRosterPublicViewPrefab;
    [SerializeField] private NetworkPrefabRef _matchRewardsPrivateViewPrefab;
    [SerializeField] private NetworkPrefabRef _fusionViewPrefab;
    [SerializeField] private NetworkPrefabRef _unitPrefab;
    [SerializeField] private NetworkPrefabRef _combatCoordinatorPrefab;
    [SerializeField] private NetworkPrefabRef _matchResultCoordinatorPrefab;

    [Header("Player Piece Prefabs")]
    [SerializeField] private NetworkPrefabRef _player1PiecePrefab;
    [SerializeField] private NetworkPrefabRef _player2PiecePrefab;

    [Networked, Capacity(4)] public NetworkArray<NetworkId> PlayerStates { get; }
    [Networked, Capacity(4)] public NetworkArray<NetworkId> PlayerCardZoneIds { get; }
    [Networked] public int PlayerCount { get; set; }

    private GameStateNetworkView _gameStateView;
    private BoardNetworkView _boardView;
    private CombatNetworkView _combatView;
    private MatchResultNetworkView _matchResultView;
    private readonly Dictionary<PlayerRef, PlayerCardZoneNetworkView> _playerCardZones = new();
    private readonly Dictionary<PlayerRef, GameplayDeckChooseNetworkView> _deckChooseViews = new();
    private readonly Dictionary<PlayerRef, PlayerRosterPublicNetworkView> _rosterViews = new();
    private readonly Dictionary<PlayerRef, MatchRewardsPrivateNetworkView> _rewardsViews = new();
    private readonly Dictionary<PlayerRef, FusionNetworkView> _fusionViews = new();
    private readonly Dictionary<PlayerRef, NetworkObject> _playerPieces = new();
    private readonly HashSet<PlayerRef> _spawnedPlayers = new();

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
        SpawnCombatCoordinator();
        SpawnMatchResultCoordinator();
        SpawnExistingPlayers();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
        _spawnedPlayers.Clear();
        _playerPieces.Clear();
        _playerCardZones.Clear();
        _deckChooseViews.Clear();
        _rosterViews.Clear();
        _rewardsViews.Clear();
        _fusionViews.Clear();
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

        var obj = Runner.Spawn(_gameStateManagerPrefab, transform.position, transform.rotation);
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

        var obj = Runner.Spawn(_boardManagerPrefab, transform.position, transform.rotation);
        _boardView = obj.GetComponent<BoardNetworkView>();
        _logger?.Log("[GameplayNetworkCoordinator] Spawned BoardManager.");
    }

    private void SpawnCombatCoordinator()
    {
        if (!_combatCoordinatorPrefab.IsValid)
        {
            _logger?.LogWarning("[GameplayNetworkCoordinator] CombatCoordinator prefab not assigned.");
            return;
        }

        var obj = Runner.Spawn(_combatCoordinatorPrefab, Vector3.zero, Quaternion.identity);
        _combatView = obj.GetComponent<CombatNetworkView>();
        _logger?.Log("[GameplayNetworkCoordinator] Spawned CombatCoordinator.");
    }

    private void SpawnMatchResultCoordinator()
    {
        if (!_matchResultCoordinatorPrefab.IsValid)
        {
            _logger?.LogWarning("[GameplayNetworkCoordinator] MatchResultCoordinator prefab not assigned.");
            return;
        }

        var obj = Runner.Spawn(_matchResultCoordinatorPrefab, Vector3.zero, Quaternion.identity);
        _matchResultView = obj.GetComponent<MatchResultNetworkView>();
        _logger?.Log("[GameplayNetworkCoordinator] Spawned MatchResultCoordinator.");
    }

    private void SpawnExistingPlayers()
    {
        foreach (var player in Runner.ActivePlayers)
        {
            if (_spawnedPlayers.Contains(player)) continue;
            SpawnPlayerState(player);
        }
    }

    private void HandlePlayerJoined(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (_spawnedPlayers.Contains(player)) return;
        SpawnPlayerState(player);
    }

    // Per-player spawning logic
    private void SpawnPlayerState(PlayerRef player)
    {
        _spawnedPlayers.Add(player);
        if (_playerCardZoneViewPrefab.IsValid)
        {
            var pczObj = Runner.Spawn(_playerCardZoneViewPrefab, transform.position, transform.rotation, player);
            var pczView = pczObj.GetComponent<PlayerCardZoneNetworkView>();
            if (pczView != null)
            {
                _playerCardZones[player] = pczView;
                RegisterPlayerCardZone(pczObj.Id);
            }
            _logger?.Log($"[GameplayNetworkCoordinator] Spawned PlayerCardZoneView for {player}.");
        }

        if (_deckChooseViewPrefab.IsValid)
        {
            var dcObj = Runner.Spawn(_deckChooseViewPrefab, transform.position, transform.rotation, player, (runner, o) => {
                // DeckChoose doesn't strictly need Owner, but good practice if it has one
            });
            var dcView = dcObj.GetComponent<GameplayDeckChooseNetworkView>();
            if (dcView != null)
                _deckChooseViews[player] = dcView;
            _logger?.Log($"[GameplayNetworkCoordinator] Spawned DeckChooseView for {player}.");
        }

        if (_playerRosterPublicViewPrefab.IsValid)
        {
            var rObj = Runner.Spawn(_playerRosterPublicViewPrefab, transform.position, transform.rotation, player, (runner, o) => {
                var rView = o.GetComponent<PlayerRosterPublicNetworkView>();
                if (rView != null) {
                    rView.Owner = player;
                    rView.HP = 100;
                }
            });
            var rView = rObj.GetComponent<PlayerRosterPublicNetworkView>();
            if (rView != null)
            {
                _rosterViews[player] = rView;
            }
            _logger?.Log($"[GameplayNetworkCoordinator] Spawned PlayerRosterPublicView for {player}.");
        }

        if (_matchRewardsPrivateViewPrefab.IsValid)
        {
            var mObj = Runner.Spawn(_matchRewardsPrivateViewPrefab, transform.position, transform.rotation, player, (runner, o) => {
                var mView = o.GetComponent<MatchRewardsPrivateNetworkView>();
                // Match rewards initialization if needed
            });
            var mView = mObj.GetComponent<MatchRewardsPrivateNetworkView>();
            if (mView != null)
            {
                mView.ServerInitialize(player);
                _rewardsViews[player] = mView;
            }
            _logger?.Log($"[GameplayNetworkCoordinator] Spawned MatchRewardsPrivateView for {player}.");
        }

        if (_fusionViewPrefab.IsValid)
        {
            var fusionObj = Runner.Spawn(_fusionViewPrefab, Vector3.zero, Quaternion.identity, player);
            var fusionView = fusionObj.GetComponent<FusionNetworkView>();
            if (fusionView != null)
                _fusionViews[player] = fusionView;
            _logger?.Log($"[GameplayNetworkCoordinator] Spawned FusionView for {player}.");
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

    private void RegisterPlayerCardZone(NetworkId zoneId)
    {
        for (int i = 0; i < PlayerCardZoneIds.Length; i++)
        {
            if (!PlayerCardZoneIds.Get(i).IsValid)
            {
                PlayerCardZoneIds.Set(i, zoneId);
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
    public CombatNetworkView CombatView => _combatView;
    public MatchResultNetworkView MatchResultView => _matchResultView;

    public PlayerCardZoneNetworkView GetPlayerCardZoneView(PlayerRef player)
    {
        _playerCardZones.TryGetValue(player, out var view);
        return view;
    }

    public GameplayDeckChooseNetworkView GetDeckChooseView(PlayerRef player)
    {
        _deckChooseViews.TryGetValue(player, out var view);
        return view;
    }

    public FusionNetworkView GetFusionView(PlayerRef player)
    {
        _fusionViews.TryGetValue(player, out var view);
        return view;
    }

    public IEnumerable<PlayerRef> GetAllPlayers() => _playerCardZones.Keys;

    public void ResetFusionViewsForNewTurn()
    {
        foreach (var kvp in _fusionViews)
            kvp.Value?.ServerResetForNewTurn();
    }

    public string[] GetFusionUsedCards(PlayerRef player)
    {
        if (_fusionViews.TryGetValue(player, out var view) && view != null)
            return view.GetUsedCardIds();
        return System.Array.Empty<string>();
    }
}
