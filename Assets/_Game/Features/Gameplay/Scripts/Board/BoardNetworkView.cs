using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Zenject;

public class BoardNetworkView : NetworkBehaviour, IBoardNetworkBridge
{
    [Inject(Optional = true)] private IBoardSubsystem _board;
    [Inject(Optional = true)] private IDebugLogger _logger;

    [Header("Prefabs")]
    [SerializeField] private NetworkPrefabRef _hexTilePrefab;

    [Header("Grid Settings")]
    [SerializeField] private float _horizontalSpacing = 1.732f;
    [SerializeField] private float _verticalSpacing = 1.5f;
    [SerializeField] private bool _autoMeasureSpacing = true;

    [Networked] public NetworkBool IsGenerated { get; set; }

    private ChangeDetector _changeDetector;
    private readonly List<HexTile> _tiles = new();
    private readonly Dictionary<HexCoord, Vector3> _tilePositions = new();

    private static readonly HexCoord DeployAreaPlayer1 = new(4, -4);
    private static readonly HexCoord DeployAreaPlayer2 = new(-4, 4);

    public override void Spawned()
    {
        if (_board == null)
        {
            var ctx = FindObjectOfType<SceneContext>();
            _board = ctx?.Container.Resolve<IBoardSubsystem>();
        }
        if (_logger == null)
        {
            var ctx = FindObjectOfType<SceneContext>();
            _logger = ctx?.Container.Resolve<IDebugLogger>();
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _board?.RegisterNetworkBridge(this);

        if (Object.HasStateAuthority)
        {
            GenerateBoard();
        }
        else
        {
            RebuildTileRegistryFromChildren();
        }

        PushState();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _board?.RegisterNetworkBridge(null);
        _tiles.Clear();
        _tilePositions.Clear();
    }

    // ── IBoardNetworkBridge ──────────────────────────────────────────────

    public void SendBoardGeneratedRpc() { }

    // ── Board generation (StateAuthority only) ───────────────────────────

    private void GenerateBoard()
    {
        if (!Object.HasStateAuthority) return;

        if (_autoMeasureSpacing && _hexTilePrefab.IsValid)
        {
            MeasureTileSpacing();
        }

        Quaternion tileRotation = Quaternion.Euler(270f, 330f, 0f);
        int spawnedCount = 0;

        for (int r = -4; r <= 4; r++)
        {
            int numCols = 9 - Math.Abs(r);
            for (int c = 0; c < numCols; c++)
            {
                float x = (c - (numCols - 1) / 2f) * _horizontalSpacing;
                float z = r * _verticalSpacing;

                Vector3 localPos = new Vector3(x, 0f, z);
                Vector3 spawnPos = transform.position + transform.rotation * localPos;
                spawnPos.y = transform.position.y;

                int p = -r;
                int q = c - 4 + Mathf.Max(0, r);

                NetworkObject tileObj = Runner.Spawn(_hexTilePrefab, spawnPos, tileRotation);
                if (tileObj != null)
                {
                    tileObj.transform.SetParent(transform);

                    var hexTile = tileObj.GetComponent<HexTile>();
                    if (hexTile == null)
                        hexTile = tileObj.gameObject.AddComponent<HexTile>();

                    hexTile.SetCoordinates(p, q);
                    RegisterTileLocally(hexTile);
                    spawnedCount++;
                }
            }
        }

        IsGenerated = true;
        _logger?.Log($"[BoardNetworkView] Generated board with {spawnedCount} hex tiles.");
        RegisterWithSubsystem();
    }

    private void MeasureTileSpacing()
    {
        NetworkObject tempTile = Runner.Spawn(_hexTilePrefab, Vector3.zero, Quaternion.Euler(270f, 0f, 0f));
        if (tempTile == null) return;

        var rend = tempTile.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            float zSize = rend.bounds.size.z;
            float inradius = zSize / 2f;
            _horizontalSpacing = 2f * inradius;
            _verticalSpacing = Mathf.Sqrt(3f) * inradius;
            _logger?.Log($"[BoardNetworkView] Measured spacing from tile bounds: H={_horizontalSpacing:F3}, V={_verticalSpacing:F3}");
        }

        Runner.Despawn(tempTile);
    }

    private void RebuildTileRegistryFromChildren()
    {
        _tiles.Clear();
        _tilePositions.Clear();

        var childTiles = GetComponentsInChildren<HexTile>();
        foreach (var tile in childTiles)
        {
            RegisterTileLocally(tile);
        }

        if (_tiles.Count > 0)
        {
            RegisterWithSubsystem();
            _logger?.Log($"[BoardNetworkView] Rebuilt tile registry from {_tiles.Count} children (client).");
        }
    }

    private void RegisterTileLocally(HexTile tile)
    {
        _tiles.Add(tile);
        var coord = new HexCoord(tile.p, tile.q);
        Vector3 pos = tile.transform.childCount > 0
            ? tile.transform.GetChild(0).position
            : tile.transform.position;
        _tilePositions[coord] = pos;
    }

    private void RegisterWithSubsystem()
    {
        if (_board == null) return;

        var coords = new List<HexCoord>(_tiles.Count);
        foreach (var tile in _tiles)
            coords.Add(new HexCoord(tile.p, tile.q));

        var boardSub = _board as BoardSubsystem;
        if (boardSub != null)
        {
            boardSub.RegisterTiles(coords, _tilePositions);
            RegisterDeployAreas(boardSub);
        }
    }

    private void RegisterDeployAreas(BoardSubsystem boardSub)
    {
        int playerIndex = 0;
        foreach (var player in Runner.ActivePlayers)
        {
            var deployCoord = playerIndex == 0 ? DeployAreaPlayer1 : DeployAreaPlayer2;
            boardSub.RegisterDeployArea(player, deployCoord);
            playerIndex++;
            if (playerIndex >= 2) break;
        }
    }

    // ── State push ───────────────────────────────────────────────────────

    public override void Render()
    {
        if (_changeDetector == null) return;
        foreach (var _ in _changeDetector.DetectChanges(this))
        {
            if (IsGenerated && _tiles.Count == 0)
                RebuildTileRegistryFromChildren();

            PushState();
            break;
        }
    }

    private void PushState()
    {
        if (_board == null) return;
        _board.OnAuthoritativeStateReceived(new BoardStateData
        {
            IsGenerated = IsGenerated
        });
    }

    // ── Public accessors for other NetworkViews ──────────────────────────

    public HexTile FindTile(int p, int q)
    {
        foreach (var tile in _tiles)
            if (tile.p == p && tile.q == q) return tile;
        return null;
    }

    public Vector3 ResolveCoordinateToPosition(int p, int q)
    {
        var coord = new HexCoord(p, q);
        return _tilePositions.TryGetValue(coord, out var pos) ? pos : Vector3.zero;
    }

    public Vector3 GetDeployWorldPosition(int playerIndex)
    {
        var coord = playerIndex == 0 ? DeployAreaPlayer1 : DeployAreaPlayer2;
        return _tilePositions.TryGetValue(coord, out var pos) ? pos : Vector3.zero;
    }

    public Quaternion GetDeployRotation(int playerIndex)
    {
        float yRotation = playerIndex == 0 ? 210f : 30f;
        Vector3 facing = Quaternion.Euler(0f, yRotation, 0f) * Vector3.forward;
        return Quaternion.LookRotation(Vector3.up, facing);
    }
}
