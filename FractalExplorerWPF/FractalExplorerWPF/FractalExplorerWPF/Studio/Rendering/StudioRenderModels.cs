using System.Numerics;

namespace FractalExplorerWPF.Studio.Rendering;

public readonly record struct StudioTile(int X, int Y, int Width, int Height);

public readonly record struct StudioRenderProgress(
    StudioLayerFrame Frame,
    StudioTile Tile,
    int CompletedTiles,
    int TotalTiles);

public sealed class StudioLayerFrame
{
    public StudioLayerFrame(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        Width = width;
        Height = height;
        Pixels = new Vector4[checked(width * height)];
    }

    public int Width { get; }
    public int Height { get; }
    public Vector4[] Pixels { get; }

    public Vector4 this[int x, int y]
    {
        get => Pixels[y * Width + x];
        set => Pixels[y * Width + x] = value;
    }
}

public static class StudioTilePlanner
{
    public static IReadOnlyList<StudioTile> Create(int width, int height, int tileSize = 64)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        tileSize = Math.Clamp(tileSize, 16, 256);
        double centerX = width / 2d;
        double centerY = height / 2d;
        var tiles = new List<StudioTile>();
        for (int y = 0; y < height; y += tileSize)
        for (int x = 0; x < width; x += tileSize)
            tiles.Add(new StudioTile(
                x,
                y,
                Math.Min(tileSize, width - x),
                Math.Min(tileSize, height - y)));
        return tiles
            .OrderBy(tile =>
            {
                double dx = tile.X + tile.Width / 2d - centerX;
                double dy = tile.Y + tile.Height / 2d - centerY;
                return dx * dx + dy * dy;
            })
            .ToArray();
    }
}
