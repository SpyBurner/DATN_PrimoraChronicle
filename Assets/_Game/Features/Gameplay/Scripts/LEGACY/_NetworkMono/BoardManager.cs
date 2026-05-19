using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class BoardManager : NetworkBehaviour
{
    [Header("Spawned Tiles")]
    [SerializeField] private List<HexTile> tiles = new List<HexTile>();

    public override void Spawned()
    {
        RefreshTileList();
    }

    public void RegisterTile(HexTile tile)
    {
        if (tile != null && !tiles.Contains(tile))
        {
            tiles.Add(tile);
        }
    }

    public void RefreshTileList()
    {
        tiles.Clear();
        tiles.AddRange(GetComponentsInChildren<HexTile>());
    }

    /// <summary>
    /// Resolves axial coordinates (p, q) to the world position of the tile's player placement child.
    /// Returns Vector3.zero if not found.
    /// </summary>
    public Vector3 ResolveCoordinateToPosition(int p, int q)
    {
        HexTile tile = FindTile(p, q);
        if (tile != null)
        {
            // Each tile has a single child that is the player placement position.
            if (tile.transform.childCount > 0)
            {
                return tile.transform.GetChild(0).position;
            }
            return tile.transform.position; // Fallback to tile origin
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Resolves a world position to the axial coordinates (p, q) of the closest tile.
    /// Returns true if a tile is found within a 2-unit threshold.
    /// </summary>
    public bool ResolvePositionToCoordinate(Vector3 position, out int p, out int q)
    {
        p = 0;
        q = 0;
        HexTile closestTile = null;
        float minDistance = float.MaxValue;

        if (tiles.Count == 0)
        {
            RefreshTileList();
        }

        foreach (var tile in tiles)
        {
            if (tile == null) continue;

            // Use the placement child position if available, or the tile position itself
            Vector3 tilePos = (tile.transform.childCount > 0) ? tile.transform.GetChild(0).position : tile.transform.position;
            float dist = Vector3.Distance(position, tilePos);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestTile = tile;
            }
        }

        if (closestTile != null && minDistance < 2f)
        {
            p = closestTile.p;
            q = closestTile.q;
            return true;
        }

        return false;
    }

    public HexTile FindTile(int p, int q)
    {
        if (tiles.Count == 0)
        {
            RefreshTileList();
        }

        foreach (var tile in tiles)
        {
            if (tile != null && tile.p == p && tile.q == q)
            {
                return tile;
            }
        }
        return null;
    }
}
