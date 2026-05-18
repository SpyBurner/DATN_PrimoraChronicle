using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using Zenject;

public class NetworkSpawner : SimulationBehaviour
{
    [Inject] private readonly INetworkManagerSubsystem _networkManager;

    [Header("Prefabs")]
    public NetworkPrefabRef playerPiecePrefab;
    public GameObject hexTilePrefab;

    private GameObject _boardParent;

    private void Awake()
    {
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
            _boardParent = new GameObject("Board");
            _boardParent.transform.position = transform.position;
            _boardParent.transform.rotation = transform.rotation;
        }

        if (hexTilePrefab != null)
        {
            GameObject tile = Instantiate(hexTilePrefab, transform.position, Quaternion.identity, _boardParent.transform);
            tile.name = "HexTile_Pivot";
        }
    }

    private void HandlePlayerJoined(PlayerRef player)
    {
        NetworkRunner runner = _networkManager.Runner;
        if (runner != null)
        {
            // Only the Host has the authority to spawn networked objects
            if (runner.IsServer)
            {
                Vector3 spawnPos = new Vector3(0, 1, 0); // Replace with your grid entry point
                
                // Spawn the piece and assign Input Authority to the joining player
                runner.Spawn(playerPiecePrefab, spawnPos, Quaternion.identity, player);
            }
        }
    }
}
