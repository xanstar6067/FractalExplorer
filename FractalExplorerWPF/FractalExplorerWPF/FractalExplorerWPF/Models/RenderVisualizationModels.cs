using System.Threading;

namespace FractalExplorerWPF.Models;

public enum TileSchedulingStrategy
{
    Classic,
    Linear,
    Spiral,
    Randomized,
    Checkerboard,
    Diagonal,
    EdgesInward,
    MortonCurve
}

public static class RenderPatternSettings
{
    private static int _selectedPattern;

    public static TileSchedulingStrategy SelectedPattern
    {
        get => (TileSchedulingStrategy)Volatile.Read(ref _selectedPattern);
        set => Volatile.Write(ref _selectedPattern, (int)value);
    }
}

public readonly record struct MandelbrotRenderTile(
    int X,
    int Y,
    int Width,
    int Height,
    int Column,
    int Row);
