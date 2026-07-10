using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Core.Rendering;

public static class MandelbrotTileScheduler
{
    public static IReadOnlyList<MandelbrotRenderTile> Create(
        int width,
        int height,
        int tileSize,
        TileSchedulingStrategy strategy)
    {
        int columns = (width + tileSize - 1) / tileSize;
        int rows = (height + tileSize - 1) / tileSize;
        var tiles = new List<MandelbrotRenderTile>(columns * rows);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int x = column * tileSize;
                int y = row * tileSize;
                tiles.Add(new MandelbrotRenderTile(
                    x, y, Math.Min(tileSize, width - x), Math.Min(tileSize, height - y), column, row));
            }
        }

        return strategy switch
        {
            TileSchedulingStrategy.Linear => tiles,
            TileSchedulingStrategy.Classic => tiles.OrderBy(tile => DistanceSquared(tile, columns, rows)).ToList(),
            TileSchedulingStrategy.Spiral => BuildSpiral(tiles, columns, rows),
            TileSchedulingStrategy.Randomized => Shuffle(tiles),
            TileSchedulingStrategy.Checkerboard => tiles.OrderBy(tile => (tile.Column + tile.Row) & 1)
                .ThenBy(tile => tile.Row).ThenBy(tile => tile.Column).ToList(),
            TileSchedulingStrategy.Diagonal => tiles.OrderBy(tile => tile.Column + tile.Row)
                .ThenBy(tile => tile.Row).ToList(),
            TileSchedulingStrategy.EdgesInward => tiles.OrderByDescending(tile => DistanceSquared(tile, columns, rows)).ToList(),
            TileSchedulingStrategy.MortonCurve => tiles.OrderBy(tile => MortonCode((uint)tile.Column, (uint)tile.Row)).ToList(),
            _ => tiles
        };
    }

    private static double DistanceSquared(MandelbrotRenderTile tile, int columns, int rows)
    {
        double dx = tile.Column - (columns - 1) / 2.0;
        double dy = tile.Row - (rows - 1) / 2.0;
        return dx * dx + dy * dy;
    }

    private static IReadOnlyList<MandelbrotRenderTile> BuildSpiral(
        List<MandelbrotRenderTile> tiles, int columns, int rows)
    {
        var lookup = tiles.ToDictionary(tile => (tile.Column, tile.Row));
        var result = new List<MandelbrotRenderTile>(tiles.Count);
        var visited = new HashSet<(int Column, int Row)>();
        int centerColumn = (columns - 1) / 2;
        int centerRow = (rows - 1) / 2;
        int maxRadius = Math.Max(columns, rows);

        for (int radius = 0; radius <= maxRadius && result.Count < tiles.Count; radius++)
        {
            int left = centerColumn - radius;
            int right = centerColumn + radius;
            int top = centerRow - radius;
            int bottom = centerRow + radius;
            for (int column = left; column <= right; column++) Add(column, top);
            for (int row = top + 1; row <= bottom; row++) Add(right, row);
            for (int column = right - 1; column >= left; column--) Add(column, bottom);
            for (int row = bottom - 1; row > top; row--) Add(left, row);
        }
        return result;

        void Add(int column, int row)
        {
            if (column < 0 || column >= columns || row < 0 || row >= rows ||
                !visited.Add((column, row))) return;
            if (lookup.TryGetValue((column, row), out MandelbrotRenderTile tile)) result.Add(tile);
        }
    }

    private static IReadOnlyList<MandelbrotRenderTile> Shuffle(List<MandelbrotRenderTile> source)
    {
        var result = source.ToList();
        for (int index = result.Count - 1; index > 0; index--)
        {
            int other = Random.Shared.Next(index + 1);
            (result[index], result[other]) = (result[other], result[index]);
        }
        return result;
    }

    private static ulong MortonCode(uint x, uint y)
    {
        ulong result = 0;
        for (int bit = 0; bit < 32; bit++)
        {
            result |= ((ulong)(x >> bit) & 1UL) << (bit * 2);
            result |= ((ulong)(y >> bit) & 1UL) << (bit * 2 + 1);
        }
        return result;
    }
}
