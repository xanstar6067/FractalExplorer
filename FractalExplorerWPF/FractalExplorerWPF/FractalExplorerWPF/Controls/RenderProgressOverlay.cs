using System.Windows;
using System.Windows.Media;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Controls;

public sealed class RenderProgressOverlay : FrameworkElement
{
    private readonly Dictionary<(int Column, int Row), MandelbrotRenderTile> _completed = [];
    private readonly Dictionary<(int Column, int Row), MandelbrotRenderTile> _active = [];
    private int _renderWidth = 1;
    private int _renderHeight = 1;

    public void BeginSession(int renderWidth, int renderHeight)
    {
        _renderWidth = Math.Max(1, renderWidth);
        _renderHeight = Math.Max(1, renderHeight);
        _completed.Clear();
        _active.Clear();
        InvalidateVisual();
    }

    public void StartTile(MandelbrotRenderTile tile)
    {
        _active[(tile.Column, tile.Row)] = tile;
    }

    public void CompleteTile(MandelbrotRenderTile tile)
    {
        _active.Remove((tile.Column, tile.Row));
        _completed[(tile.Column, tile.Row)] = tile;
    }

    public void EndSession()
    {
        _completed.Clear();
        _active.Clear();
        InvalidateVisual();
    }

    public void Refresh() => InvalidateVisual();

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        double scaleX = ActualWidth / _renderWidth;
        double scaleY = ActualHeight / _renderHeight;
        var completedPen = new Pen(new SolidColorBrush(Color.FromArgb(190, 255, 54, 54)), 1.5);
        var activePen = new Pen(new SolidColorBrush(Color.FromArgb(220, 85, 255, 105)), 1.5);
        completedPen.Freeze();
        activePen.Freeze();

        foreach (((int column, int row), MandelbrotRenderTile tile) in _completed)
        {
            double left = tile.X * scaleX;
            double top = tile.Y * scaleY;
            double right = (tile.X + tile.Width) * scaleX;
            double bottom = (tile.Y + tile.Height) * scaleY;
            if (!_completed.ContainsKey((column, row - 1))) drawingContext.DrawLine(completedPen, new Point(left, top), new Point(right, top));
            if (!_completed.ContainsKey((column, row + 1))) drawingContext.DrawLine(completedPen, new Point(left, bottom), new Point(right, bottom));
            if (!_completed.ContainsKey((column - 1, row))) drawingContext.DrawLine(completedPen, new Point(left, top), new Point(left, bottom));
            if (!_completed.ContainsKey((column + 1, row))) drawingContext.DrawLine(completedPen, new Point(right, top), new Point(right, bottom));
        }

        foreach (MandelbrotRenderTile tile in _active.Values)
        {
            var rect = new Rect(tile.X * scaleX, tile.Y * scaleY,
                Math.Max(0, tile.Width * scaleX - 1), Math.Max(0, tile.Height * scaleY - 1));
            drawingContext.DrawRectangle(null, activePen, rect);
        }
    }
}
