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
    public GameObject hexTilePrefab;
    public GameObject boardPrefab; // Optional networked board parent prefab

    [Header("Grid Settings")]
    public float horizontalSpacing = 1.732f;
    public float verticalSpacing = 1.5f;

    private GameObject _boardParent;

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

        // Try to resolve position via BoardManager
        if (_boardParent != null)
        {
            var boardManager = _boardParent.GetComponent<BoardManager>();
            if (boardManager != null)
            {
                Vector3 resolvedPos = boardManager.ResolveCoordinateToPosition(p, q);
                if (resolvedPos != Vector3.zero)
                {
                    return resolvedPos;
                }
            }
        }

        // Fallback calculations
        float x = (c - (9 - Math.Abs(r) - 1) / 2f) * horizontalSpacing;
        float z = r * verticalSpacing;

        // Spawn piece 1 unit above the hex tile flat surface
        Vector3 localPos = new Vector3(x, 1f, z);
        return transform.position + transform.rotation * localPos;
    }

    public override void Spawned()
    {
#if FUSION_SHARED_TEST
        if (!Runner.IsSharedModeMasterClient) return;
#else
        if (!Runner.IsServer) return;
#endif
        GenerateBoard();
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
            if (boardPrefab != null)
            {
                NetworkObject boardObj = Runner.Spawn(boardPrefab, transform.position, transform.rotation);
                _boardParent = boardObj.gameObject;
            }
            else
            {
                _boardParent = new GameObject("Board");
                _boardParent.transform.position = transform.position;
                _boardParent.transform.rotation = transform.rotation;
                _boardParent.AddComponent<BoardManager>();
            }
        }

        if (hexTilePrefab == null) return;

        // Instantiate temporary tile at Euler(270, 0, 0) to query the bounding box on the Z axis
        GameObject tempTile = Instantiate(hexTilePrefab, Vector3.zero, Quaternion.Euler(270f, 0f, 0f));
        float zSize = 1f;
        var rComponent = tempTile.GetComponentInChildren<Renderer>();
        if (rComponent != null)
        {
            zSize = rComponent.bounds.size.z;
        }

        Destroy(tempTile);

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
    }

    private void HandlePlayerJoined(PlayerRef player)
    {
        NetworkRunner runner = _networkManager.Runner;
        if (runner != null)
        {
            NetworkPrefabRef piecePrefab = GetPlayerPiecePrefab(player);
            Vector3 spawnPos = GetPlayerSpawnPosition(player);
            Quaternion spawnRot = GetPlayerSpawnRotation(player, spawnPos);

#if FUSION_SHARED_TEST
            if (player != runner.LocalPlayer) return;
            
            // Spawn the piece and assign Input Authority to the joining player
            NetworkObject spawnedObj = runner.Spawn(piecePrefab, spawnPos, spawnRot, player);
            _spawnedPieces[player] = spawnedObj;
#else
            // Only the Host has the authority to spawn networked objects
            if (runner.IsServer)
            {
                // Spawn the piece and assign Input Authority to the joining player
                runner.Spawn(piecePrefab, spawnPos, spawnRot, player);
            }
#endif
        }
    }
}
