using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

public class BoardSubsystem : IBoardSubsystem
{
    [Inject] private readonly IBoardController _controller;
    [Inject] private readonly IBoardModel _model;

    public event UnityAction<bool> IsGeneratedChanged;
    public event UnityAction<System.Collections.Generic.IReadOnlyList<HexCoord>> TilesChanged;
    public event UnityAction<HexCoord, string> TileOccupantChanged;
    public event UnityAction<HexCoord, string> TileEffectChanged;

    public bool IsGenerated => _model.IsGenerated.Value;

    // Track A populates this at board-generation time via the NetworkView
    public IReadOnlyList<HexCoord> AllTiles { get; private set; } = new List<HexCoord>();

    // World-position registry populated by BoardNetworkView after generation
    private readonly Dictionary<HexCoord, Vector3> _tilePositions = new();
    private readonly Dictionary<HexCoord, string> _occupants = new();
    private readonly Dictionary<int, HexCoord> _deployAreas = new();

    public void Initialize()
    {
        _model.IsGenerated.OnChanged += HandleIsGeneratedChanged;
        _controller.Initialize();
    }

    public void Dispose()
    {
        _model.IsGenerated.OnChanged -= HandleIsGeneratedChanged;
        _tilePositions.Clear();
        _occupants.Clear();
        _deployAreas.Clear();
        _controller.Dispose();
        _model.Dispose();
    }

    public void RegisterNetworkBridge(IBoardNetworkBridge bridge) => _controller.RegisterBridge(bridge);
    public void OnAuthoritativeStateReceived(BoardStateData data) => _controller.OnAuthoritativeStateReceived(data);

    // Called by BoardNetworkView after grid is built
    public void RegisterTiles(IReadOnlyList<HexCoord> tiles, Dictionary<HexCoord, Vector3> positions)
    {
        AllTiles = tiles;
        _tilePositions.Clear();
        foreach (var kvp in positions) _tilePositions[kvp.Key] = kvp.Value;
        try { TilesChanged?.Invoke(tiles); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    public void RegisterDeployArea(PlayerRef owner, HexCoord coord) => _deployAreas[owner.RawEncoded] = coord;

    public Vector3 GetWorldPosition(HexCoord coord)
        => _tilePositions.TryGetValue(coord, out var pos) ? pos : Vector3.zero;

    public bool TryResolveWorldToHex(Vector3 world, out HexCoord coord)
    {
        const float threshold = 2f;
        foreach (var kvp in _tilePositions)
        {
            if (Vector3.Distance(kvp.Value, world) < threshold)
            {
                coord = kvp.Key;
                return true;
            }
        }
        coord = HexCoord.Invalid;
        return false;
    }

    public int Distance(HexCoord a, HexCoord b)
        => (Math.Abs(a.P - b.P) + Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R)) / 2;

    public bool IsEmpty(HexCoord coord) => !_occupants.ContainsKey(coord);

    public HexCoord GetDeployArea(PlayerRef owner)
        => _deployAreas.TryGetValue(owner.RawEncoded, out var c) ? c : HexCoord.Invalid;

    public IReadOnlyList<HexCoord> GetNeighbors(HexCoord coord)
    {
        var dirs = new[] {
            new HexCoord(1,-1), new HexCoord(1,0), new HexCoord(0,1),
            new HexCoord(-1,1), new HexCoord(-1,0), new HexCoord(0,-1)
        };
        var result = new List<HexCoord>();
        foreach (var d in dirs)
        {
            var n = new HexCoord(coord.P + d.P, coord.Q + d.Q);
            if (_tilePositions.ContainsKey(n)) result.Add(n);
        }
        return result;
    }

    public IReadOnlyList<HexCoord> GetTilesInRange(HexCoord center, int range)
    {
        var result = new List<HexCoord>();
        foreach (var tile in AllTiles)
            if (Distance(center, tile) <= range) result.Add(tile);
        return result;
    }

    public IReadOnlyList<HexCoord> FindPath(HexCoord from, HexCoord to, int maxDistance)
    {
        if (!IsEmpty(to)) return new List<HexCoord>();
        if (Distance(from, to) > maxDistance) return new List<HexCoord>();

        // BFS pathfinding through empty tiles only
        var visited = new HashSet<HexCoord> { from };
        var queue = new Queue<(HexCoord pos, List<HexCoord> path)>();
        queue.Enqueue((from, new List<HexCoord>()));

        while (queue.Count > 0)
        {
            var (current, path) = queue.Dequeue();

            if (path.Count >= maxDistance) continue;

            foreach (var neighbor in GetNeighbors(current))
            {
                if (visited.Contains(neighbor)) continue;

                var newPath = new List<HexCoord>(path) { neighbor };

                if (neighbor == to)
                    return newPath;

                if (IsEmpty(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue((neighbor, newPath));
                }
            }
        }

        return new List<HexCoord>();
    }

    // Called by UnitNetworkView when a unit moves onto/off a tile
    public void SetOccupant(HexCoord coord, string unitId)
    {
        if (string.IsNullOrEmpty(unitId)) _occupants.Remove(coord);
        else _occupants[coord] = unitId;
        try { TileOccupantChanged?.Invoke(coord, unitId); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }

    private void HandleIsGeneratedChanged()
    {
        try { IsGeneratedChanged?.Invoke(_model.IsGenerated.Value); }
        catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
    }
}
