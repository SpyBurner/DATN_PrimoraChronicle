using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using Zenject;

public class NetworkSpawner : NetworkBehaviour
{
    [Inject] private readonly INetworkManagerSubsystem _networkManager;
    [Inject] private readonly IDebugLogger _debugLogger;

    [Header("Prefabs")]
    public NetworkPrefabRef playerPiecePrefab; // Fallback prefab
    public NetworkPrefabRef player1PiecePrefab;
    public NetworkPrefabRef player2PiecePrefab;
    public NetworkPrefabRef player3PiecePrefab; // Future addition
    public NetworkPrefabRef deckChooseViewPrefab;
    public NetworkPrefabRef hexTilePrefab;
    public NetworkPrefabRef boardPrefab; // Optional networked board parent prefab

    public NetworkPrefabRef playerStatePrefab;
    public NetworkGameplayManager gameplayManagerPrefab;


    [Header("Grid Settings")]
    public float horizontalSpacing = 1.732f;
    public float verticalSpacing = 1.5f;

    private GameObject _boardParent;
    private bool _boardReady = false;

#if FUSION_SHARED_TEST
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPieces = new Dictionary<PlayerRef, NetworkObject>();
#endif

    private NetworkPrefabRef GetPlayerPiecePrefab(PlayerRef player)
    {
        int playerNum = player.PlayerId;
        
        if (Runner != null)
        {
            int index = 0;
            foreach (var activePlayer in Runner.ActivePlayers)
            {
                if (activePlayer == player)
                {
                    playerNum = index + 1;
                    break;
                }
                index++;
            }
        }

        if (playerNum == 1 && player1PiecePrefab.IsValid) return player1PiecePrefab;
        if (playerNum == 2 && player2PiecePrefab.IsValid) return player2PiecePrefab;
        if (playerNum == 3 && player3PiecePrefab.IsValid) return player3PiecePrefab;

        return playerPiecePrefab;
    }

    private Quaternion GetPlayerSpawnRotation(PlayerRef player, Vector3 spawnPos)
    {
        float yRotation = 210f; // Default facing center for P1
        bool isP2 = false;

        if (player.PlayerId == 2)
        {
            isP2 = true;
        }
        else if (Runner != null)
        {
            int index = 0;
            foreach (var activePlayer in Runner.ActivePlayers)
            {
                if (activePlayer == player)
                {
                    if (index == 1) isP2 = true;
                    break;
                }
                index++;
            }
        }

        if (isP2)
        {
            yRotation = 30f; // Symmetrical facing direction for P2
        }
        else
        {
            // Dynamically calculate look-at-center rotation for other players (like P3)
            Vector3 directionToCenter = (transform.position - spawnPos);
            directionToCenter.y = 0f;
            if (directionToCenter.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToCenter);
                yRotation = lookRotation.eulerAngles.y;
            }
        }

        // The local axis of the player models are Y forward, Z up.
        // This maps local Z to world up and local Y to the horizontal facing direction.
        Vector3 facingDirection = Quaternion.Euler(0f, yRotation, 0f) * Vector3.forward;
        return Quaternion.LookRotation(Vector3.up, facingDirection);
    }

    private Vector3 GetPlayerSpawnPosition(PlayerRef player)
    {
        // P1: P4 Q-4 -> r = -4, c = 0
        // P2: P-4 Q4 -> r = 4, c = 4
        bool isP2 = false;

        if (player.PlayerId == 2)
        {
            isP2 = true;
        }
        else if (Runner != null)
        {
            int index = 0;
            foreach (var activePlayer in Runner.ActivePlayers)
            {
                if (activePlayer == player)
                {
                    if (index == 1) isP2 = true;
                    break;
                }
                index++;
            }
        }

        int r = isP2 ? 4 : -4;
        int c = isP2 ? 4 : 0;

        int p = -r;
        int q = c - 4 + Mathf.Max(0, r);

        // Try to resolve position via BoardManager and get surface height from tile child
        if (_boardParent != null)
        {
            if (_boardParent.TryGetComponent<BoardManager>(out var boardManager))
            {
                Vector3 resolvedPos = boardManager.ResolveCoordinateToPosition(p, q);
                if (resolvedPos != Vector3.zero)
                {
                    HexTile spawnTile = boardManager.FindTile(p, q);
                    if (spawnTile != null && spawnTile.transform.childCount > 0)
                    {
                        Vector3 childWorldPos = spawnTile.transform.GetChild(0).position;
                        _debugLogger.Log($"[NetworkSpawner] Resolved player {player} spawn from tile child world pos: P={p}, Q={q} -> {childWorldPos}");
                        return childWorldPos;
                    }

                    _debugLogger.Log($"[NetworkSpawner] Resolved player {player} position from BoardManager: P={p}, Q={q} -> {resolvedPos}");
                    return resolvedPos;
                }
                else
                {
                    _debugLogger.Log($"[NetworkSpawner] WARNING: BoardManager returned zero for P={p}, Q={q}. Tile may not be registered.");
                }
            }
        }

        // Fallback calculations - use the SAME coordinate calculation as tile spawning
        // Tiles are spawned relative to transform, so we must use the same formula
        float x = (c - (9 - Math.Abs(r) - 1) / 2f) * horizontalSpacing;
        float z = r * verticalSpacing;

        Vector3 localPos = new Vector3(x, 0f, z);
        Vector3 fallbackPos = _boardParent.transform.position + _boardParent.transform.rotation * localPos;

        _debugLogger.Log($"[NetworkSpawner] Using fallback spawn position for player {player}: P={p}, Q={q}, localPos={localPos} -> world={fallbackPos}");
        return fallbackPos;
    }

    public override void Spawned()
    {
        if (!Runner.IsSharedModeMasterClient && !Runner.IsServer) return;
        SpawnManagers();
        GenerateBoard();
        SpawnAIPlayers();
    }

    private void SpawnManagers()
    {
        if (gameplayManagerPrefab != null)
        {
            Runner.Spawn(gameplayManagerPrefab, Vector3.zero, Quaternion.identity);
            _debugLogger.Log("[NetworkSpawner] Spawned NetworkGameplayManager.");
        }
        else
        {
            _debugLogger.Log("[NetworkSpawner] WARNING: gameplayManagerPrefab not assigned. Please assign a NetworkGameplayManager prefab to ensure proper game state management.");
        }
    }

    private void SpawnAIPlayers()
    {
        if (Runner == null || !Runner.IsServer || !Runner.IsSharedModeMasterClient) return;

        int aiCount = 0;
        if (Runner.SessionInfo.Properties != null && Runner.SessionInfo.Properties.TryGetValue("ai_count", out var val))
        {
            aiCount = (int)val;
        }

        _debugLogger.Log($"[NetworkSpawner] Spawning {aiCount} AI Players based on SessionProperties.");

        int humanPlayerCount = 0;
        if (Runner != null)
        {
            foreach (var _ in Runner.ActivePlayers)
                humanPlayerCount++;
        }

        for (int i = 0; i < aiCount; i++)
        {
            int aiPlayerIndex = humanPlayerCount + i;

            if (playerStatePrefab.IsValid)
            {
                var stateObj = Runner.Spawn(playerStatePrefab, Vector3.zero, Quaternion.identity);
                var playerState = stateObj.GetComponent<NetworkPlayerState>();
                if (playerState != null)
                {
                    playerState.Player = PlayerRef.None;
                    playerState.IsAI = true;
                    playerState.SetupDeck("AI_Champion", new string[] { "card_strike", "card_defend" }, 100, aiPlayerIndex, "AI");

                    if (NetworkGameplayManager.Instance != null)
                        NetworkGameplayManager.Instance.RegisterPlayerState(playerState);
                }
            }
        }
    }

    private void Start()
    {
        if (_networkManager != null)
        {
            _networkManager.PlayerJoined += HandlePlayerJoined;
        }
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
        {
            _networkManager.PlayerJoined -= HandlePlayerJoined;
        }
    }

    private void GenerateBoard()
    {
        if (_boardParent == null)
        {
            if (boardPrefab != null && boardPrefab.IsValid)
            {
                NetworkObject boardObj = Runner.Spawn(boardPrefab, transform.position, transform.rotation);
                _boardParent = boardObj.gameObject;
                _debugLogger.Log("[NetworkSpawner] Spawned networked board from prefab.");
            }
            else
            {
                if (boardPrefab == null || !boardPrefab.IsValid)
                {
                    _debugLogger.Log("[NetworkSpawner] WARNING: boardPrefab not assigned. Please assign a networked board prefab to ensure proper synchronization of child hex tiles.");
                }

                _boardParent = new GameObject("Board");
                _boardParent.transform.position = transform.position;
                _boardParent.transform.rotation = transform.rotation;
                _boardParent.AddComponent<BoardManager>();

                _boardParent.AddComponent<NetworkObject>();
                _debugLogger.Log("[NetworkSpawner] Created networked board fallback with NetworkObject component.");
            }
        }

        if (hexTilePrefab == null) return;

        // Instantiate temporary tile at Euler(270, 0, 0) to query the bounding box on the Z axis
        NetworkObject tempTile = Runner.Spawn(hexTilePrefab, Vector3.zero, Quaternion.Euler(270f, 0f, 0f));
        float zSize = 1f;
        var rComponent = tempTile.GetComponentInChildren<Renderer>();
        if (rComponent != null)
        {
            zSize = rComponent.bounds.size.z;
        }

        Runner.Despawn(tempTile);

        // Inradius (distance from center to the center of an edge).
        // Note: The forward direction (Z-axis) of this transform is the pointy top direction of the board.
        float inradius = zSize / 2f;
        horizontalSpacing = 2f * inradius;
        verticalSpacing = Mathf.Sqrt(3f) * inradius;

        _debugLogger.Log($"[NetworkSpawner] Measured Z-bounds size: {zSize}. Auto-configured horizontalSpacing: {horizontalSpacing}, verticalSpacing: {verticalSpacing}");

        int spawnedCount = 0;
        Quaternion tileRotation = Quaternion.Euler(270f, 330f, 0f);
        var boardManager = _boardParent.GetComponent<BoardManager>();

        for (int r = -4; r <= 4; r++)
        {
            int numCols = 9 - Math.Abs(r);
            for (int c = 0; c < numCols; c++)
            {
                float x = (c - (numCols - 1) / 2f) * horizontalSpacing;
                float z = r * verticalSpacing;

                Vector3 localPos = new Vector3(x, 0f, z);
                Vector3 spawnPos = transform.position + transform.rotation * localPos;
                spawnPos.y = _boardParent.transform.position.y;

                NetworkObject tileObj = Runner.Spawn(hexTilePrefab, spawnPos, tileRotation);
                if (tileObj != null)
                {
                    tileObj.transform.SetParent(_boardParent.transform);

                    int p = -r;
                    int q = c - 4 + Mathf.Max(0, r);

                    var hexTile = tileObj.GetComponent<HexTile>();
                    if (hexTile == null)
                    {
                        hexTile = tileObj.gameObject.AddComponent<HexTile>();
                    }
                    hexTile.SetCoordinates(p, q);

                    if (boardManager != null)
                    {
                        boardManager.RegisterTile(hexTile);
                    }
                }
                spawnedCount++;
            }
        }
        _debugLogger.Log($"[NetworkSpawner] Generated board with {spawnedCount} hex tiles on state authority.");
        _boardReady = true;
    }

    private void HandlePlayerJoined(PlayerRef player)
    {
        NetworkRunner runner = _networkManager.Runner;
        if (runner == null) return;
        SpawnPlayerPiece(player, runner);
    }

    private System.Collections.IEnumerator SpawnPlayerWhenTileReady(PlayerRef player, NetworkRunner runner)
    {
        while (!IsSpawnTileReady(player, runner))
            yield return null;

        SpawnPlayerPiece(player, runner);
    }

    private bool IsSpawnTileReady(PlayerRef player, NetworkRunner runner)
    {
        if (!_boardReady || _boardParent == null) return false;
        if (!_boardParent.TryGetComponent<BoardManager>(out var boardManager)) return false;

        bool isP2 = player.PlayerId == 2;
        if (!isP2 && runner != null)
        {
            int idx = 0;
            foreach (var ap in runner.ActivePlayers)
            {
                if (ap == player) { if (idx == 1) isP2 = true; break; }
                idx++;
            }
        }

        int r = isP2 ? 4 : -4;
        int c = isP2 ? 4 : 0;
        int coordP = -r;
        int coordQ = c - 4 + Mathf.Max(0, r);
        HexTile tile = boardManager.FindTile(coordP, coordQ);
        return tile != null && tile.transform.childCount > 0;
    }

    private void SpawnPlayerPiece(PlayerRef player, NetworkRunner runner)
    {
        if (!runner.IsServer && !runner.IsSharedModeMasterClient) return;

        int playerIndex = 0;
        if (runner != null)
        {
            int index = 0;
            foreach (var activePlayer in runner.ActivePlayers)
            {
                if (activePlayer == player) { playerIndex = index; break; }
                index++;
            }
        }

        if (playerStatePrefab != null)
        {
            var stateObj = runner.Spawn(playerStatePrefab, Vector3.zero, Quaternion.identity, player);
            if (stateObj.TryGetComponent<NetworkPlayerState>(out var playerState))
            {
                playerState.Player = player;
                playerState.IsAI = false;

                if (NetworkGameplayManager.Instance != null)
                    NetworkGameplayManager.Instance.RegisterPlayerState(playerState);
            }
        }

        if (deckChooseViewPrefab != null)
            runner.Spawn(deckChooseViewPrefab, Vector3.zero, Quaternion.identity, player);
    }
}
