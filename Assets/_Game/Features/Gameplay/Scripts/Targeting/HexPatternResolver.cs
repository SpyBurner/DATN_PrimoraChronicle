using System.Collections.Generic;
using Core.GDS;

public static class HexPatternResolver
{
    // Returns all board tiles matched by one HexCoordinate pattern entry relative to pivot.
    // Dispatch logic per {n, p, q} discriminator table in F4-targeting-hexpattern.md.
    public static List<HexCoord> Resolve(HexCoord pivot, HexCoordinate pattern, IBoardSubsystem board)
    {
        int n = pattern.n, p = pattern.p, q = pattern.q;
        var result = new List<HexCoord>();

        if (n == 0 && p == 0 && q == 0)
        {
            result.Add(pivot);
            return result;
        }

        if (n > 0 && p == 0 && q == 0)
        {
            foreach (var tile in board.AllTiles)
                if (board.Distance(pivot, tile) <= n) result.Add(tile);
            return result;
        }

        if (n == -1 && p == 0 && q == 0)
        {
            result.AddRange(board.AllTiles);
            return result;
        }

        if (n == -1)
        {
            var current = new HexCoord(pivot.P + p, pivot.Q + q);
            while (board.ContainsTile(current))
            {
                result.Add(current);
                current = new HexCoord(current.P + p, current.Q + q);
            }
            return result;
        }

        for (int k = 1; k <= n; k++)
        {
            var stepped = new HexCoord(pivot.P + k * p, pivot.Q + k * q);
            if (board.ContainsTile(stepped)) result.Add(stepped);
        }
        return result;
    }

    // Returns the union of all Resolve() results for every pattern entry.
    public static List<HexCoord> ResolveAll(HexCoord pivot, List<HexCoordinate> patterns, IBoardSubsystem board)
    {
        if (patterns == null || patterns.Count == 0) return new List<HexCoord> { pivot };
        var result = new List<HexCoord>();
        var seen = new HashSet<HexCoord>();
        foreach (var pattern in patterns)
        {
            foreach (var tile in Resolve(pivot, pattern, board))
                if (seen.Add(tile)) result.Add(tile);
        }
        return result;
    }

    // Returns the effective max range from a target_pattern list for range-ring display.
    // Returns int.MaxValue for infinite-line patterns — GetTilesInRange handles this by returning all tiles.
    public static int GetRange(List<HexCoordinate> patterns)
    {
        if (patterns == null || patterns.Count == 0) return 1;
        int max = 0;
        foreach (var entry in patterns)
        {
            if (entry.n == -1) return int.MaxValue;
            if (entry.n > max) max = entry.n;
        }
        return max > 0 ? max : 1;
    }
}
