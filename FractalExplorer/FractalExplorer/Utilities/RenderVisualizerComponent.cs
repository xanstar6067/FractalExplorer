using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;

public class RenderVisualizerComponent : IDisposable
{
    private readonly ConcurrentDictionary<Rectangle, byte> _activeTiles = new ConcurrentDictionary<Rectangle, byte>();
    private Pen _borderPen;
    private Color _borderColor = Color.FromArgb(180, 77, 255, 77); // Полупрозрачный зеленый
    private float _borderThickness = 1.5f;

    public RenderVisualizerComponent()
    {
        _borderPen = new Pen(_borderColor, _borderThickness);
    }

    /// <summary>
    /// Устанавливает цвет и толщину рамки.
    /// </summary>
    public void SetAppearance(Color borderColor, float thickness)
    {
        _borderColor = borderColor;
        _borderThickness = thickness;
        _borderPen?.Dispose();
        _borderPen = new Pen(_borderColor, _borderThickness);
    }

    /// <summary>
    /// Уведомляет визуализатор о начале рендеринга плитки.
    /// </summary>
    /// <param name="tileBounds">Границы плитки в экранных координатах.</param>
    public void NotifyTileRenderStart(Rectangle tileBounds)
    {
        _activeTiles.TryAdd(tileBounds, 0);
    }

    /// <summary>
    /// Уведомляет визуализатор о завершении рендеринга плитки.
    /// </summary>
    /// <param name="tileBounds">Границы плитки в экранных координатах.</param>
    public void NotifyTileRenderComplete(Rectangle tileBounds)
    {
        _activeTiles.TryRemove(tileBounds, out _);
    }

    /// <summary>
    /// Очищает все активные плитки (например, при отмене рендера или старте нового).
    /// </summary>
    public void ClearActiveTiles()
    {
        _activeTiles.Clear();
    }

    /// <summary>
    /// Отрисовывает рамки для всех активных плиток.
    /// Вызывается в методе Paint холста.
    /// </summary>
    /// <param name="g">Объект Graphics для рисования.</param>
    public void DrawVisualization(Graphics g)
    {
        if (_activeTiles.IsEmpty || _borderPen == null)
            return;

        // Копируем ключи для безопасной итерации
        var tilesToDraw = _activeTiles.Keys.ToList();

        foreach (var tileBounds in tilesToDraw)
        {
            // Рисуем рамку так, чтобы она была внутри плитки и не обрезалась
            Rectangle visualBounds = tileBounds;
            // Уменьшаем немного, чтобы рамка была четко видна и не затиралась соседними плитками при Invalidate
            visualBounds.Inflate(-(int)Math.Ceiling(_borderThickness), -(int)Math.Ceiling(_borderThickness));
            if (visualBounds.Width > 0 && visualBounds.Height > 0)
            {
                g.DrawRectangle(_borderPen, visualBounds);
            }
        }
    }

    public void Dispose()
    {
        _borderPen?.Dispose();
        GC.SuppressFinalize(this);
    }
}