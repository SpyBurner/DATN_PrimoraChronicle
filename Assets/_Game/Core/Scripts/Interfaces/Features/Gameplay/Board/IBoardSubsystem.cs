using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

public interface IBoardSubsystem : ISubsystem
{
    event UnityAction<bool> IsGeneratedChanged;
    event UnityAction<System.Collections.Generic.IReadOnlyList<HexCoord>> TilesChanged;
    event UnityAction<HexCoord, string> TileOccupantChanged;
    event UnityAction<HexCoord, string> TileEffectChanged;

    bool IsGenerated { get; }
    IReadOnlyList<HexCoord> AllTiles { get; }

    Vector3 GetWorldPosition(HexCoord coord);
    bool TryResolveWorldToHex(Vector3 world, out HexCoord coord);
    int Distance(HexCoord a, HexCoord b);
    bool IsEmpty(HexCoord coord);
    HexCoord GetDeployArea(PlayerRef owner);
    IReadOnlyList<HexCoord> GetNeighbors(HexCoord coord);
    IReadOnlyList<HexCoord> GetTilesInRange(HexCoord center, int range);
    IReadOnlyList<HexCoord> FindPath(HexCoord from, HexCoord to, int maxDistance);
    void SetOccupant(HexCoord coord, string unitId);

    void RegisterNetworkBridge(IBoardNetworkBridge bridge);
    void OnAuthoritativeStateReceived(BoardStateData data);
}
