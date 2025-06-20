// RenderVisualizerComponent.cs
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;

public class RenderVisualizerComponent : IDisposable
{
    // Для зеленых рамок отдельных плиток
    private readonly ConcurrentDictionary<Rectangle, byte> _activeTiles = new ConcurrentDictionary<Rectangle, byte>();
    private Pen _tileBorderPen;
    private Color _tileBorderColor = Color.FromArgb(180, 77, 255, 77); // Полупрозрачный зеленый
    private float _tileBorderThickness = 1.5f;

    // Для красной рамки всей сессии рендеринга
    private Pen _sessionBorderPen;
    private Color _sessionBorderColor = Color.FromArgb(180, 255, 0, 0); // Полупрозрачный красный
    private float _sessionBorderThickness = 2f; // Сделаем ее чуть толще для заметности
    private bool _isRenderSessionActive = false; // Флаг активности общей сессии рендеринга

    public RenderVisualizerComponent()
    {
        _tileBorderPen = new Pen(_tileBorderColor, _tileBorderThickness);
        _sessionBorderPen = new Pen(_sessionBorderColor, _sessionBorderThickness);
    }

    /// <summary>
    /// Устанавливает внешний вид для рамок отдельных плиток.
    /// </summary>
    public void SetTileAppearance(Color borderColor, float thickness)
    {
        _tileBorderColor = borderColor;
        _tileBorderThickness = thickness;
        _tileBorderPen?.Dispose();
        _tileBorderPen = new Pen(_tileBorderColor, _tileBorderThickness);
    }

    /// <summary>
    /// Устанавливает внешний вид для рамки всей сессии рендеринга.
    /// </summary>
    public void SetSessionBorderAppearance(Color borderColor, float thickness)
    {
        _sessionBorderColor = borderColor;
        _sessionBorderThickness = thickness;
        _sessionBorderPen?.Dispose();
        _sessionBorderPen = new Pen(_sessionBorderColor, _sessionBorderThickness);
    }

    /// <summary>
    /// Уведомляет визуализатор о начале общей сессии рендеринга.
    /// Активирует отрисовку красной рамки.
    /// </summary>
    public void NotifyRenderSessionStart()
    {
        _isRenderSessionActive = true;
        ClearActiveTiles(); // Очищаем отдельные плитки от предыдущей сессии, если были
    }

    /// <summary>
    /// Уведомляет визуализатор о завершении или отмене общей сессии рендеринга.
    /// Деактивирует отрисовку красной рамки.
    /// </summary>
    public void NotifyRenderSessionComplete()
    {
        _isRenderSessionActive = false;
        ClearActiveTiles(); // Также очищаем отдельные плитки
    }

    public void NotifyTileRenderStart(Rectangle tileBounds)
    {
        // Добавляем плитку для зеленой рамки, только если общая сессия активна
        if (_isRenderSessionActive)
        {
            _activeTiles.TryAdd(tileBounds, 0);
        }
    }

    public void NotifyTileRenderComplete(Rectangle tileBounds)
    {
        _activeTiles.TryRemove(tileBounds, out _);
    }

    public void ClearActiveTiles()
    {
        _activeTiles.Clear();
    }

    /// <summary>
    /// Отрисовывает все визуализации: красную рамку сессии и зеленые рамки активных плиток.
    /// </summary>
    /// <param name="g">Объект Graphics для рисования.</param>
    /// <param name="canvasBounds">Полные границы холста, на котором происходит рендеринг.
    /// Используется для отрисовки красной рамки сессии.</param>
    public void DrawVisualization(Graphics g, Rectangle canvasBounds)
    {
        // 1. Рисуем красную рамку для всей сессии рендеринга, если она активна
        if (_isRenderSessionActive && _sessionBorderPen != null && canvasBounds.Width > 0 && canvasBounds.Height > 0)
        {
            // Рисуем рамку так, чтобы она была чуть внутри границ холста, чтобы не обрезалась
            RectangleF visualSessionBorder = canvasBounds;
            visualSessionBorder.Inflate(-_sessionBorderThickness / 2, -_sessionBorderThickness / 2);
            if (visualSessionBorder.Width > 0 && visualSessionBorder.Height > 0)
            {
                g.DrawRectangle(_sessionBorderPen, visualSessionBorder.X, visualSessionBorder.Y, visualSessionBorder.Width, visualSessionBorder.Height);
            }
        }

        // 2. Рисуем зеленые рамки для активных плиток
        if (!_activeTiles.IsEmpty && _tileBorderPen != null)
        {
            var tilesToDraw = _activeTiles.Keys.ToList(); // Копируем для безопасной итерации

            foreach (var tileBounds in tilesToDraw)
            {
                Rectangle visualTileBounds = tileBounds;
                // Уменьшаем немного, чтобы рамка была четко видна и не затиралась соседними плитками
                visualTileBounds.Inflate(-(int)Math.Ceiling(_tileBorderThickness), -(int)Math.Ceiling(_tileBorderThickness));
                if (visualTileBounds.Width > 0 && visualTileBounds.Height > 0)
                {
                    g.DrawRectangle(_tileBorderPen, visualTileBounds);
                }
            }
        }
    }

    public void Dispose()
    {
        _tileBorderPen?.Dispose();
        _sessionBorderPen?.Dispose();
        GC.SuppressFinalize(this);
    }
}