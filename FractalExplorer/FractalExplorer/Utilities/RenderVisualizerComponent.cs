// RenderVisualizerComponent.cs
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq; // Потребуется для ToList()
using System.Collections.Generic; // Для List<Rectangle>

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
    private float _sessionBorderThickness = 2f;
    private bool _isRenderSessionActive = false;

    // НОВОЕ: Для отслеживания области, объединяющей ЗАВЕРШЕННЫЕ плитки в текущей сессии
    private readonly object _completedAreaLock = new object();
    private Rectangle _completedTilesUnionArea = Rectangle.Empty;

    public RenderVisualizerComponent()
    {
        _tileBorderPen = new Pen(_tileBorderColor, _tileBorderThickness);
        _sessionBorderPen = new Pen(_sessionBorderColor, _sessionBorderThickness);
    }

    public void SetTileAppearance(Color borderColor, float thickness)
    {
        _tileBorderColor = borderColor;
        _tileBorderThickness = thickness;
        _tileBorderPen?.Dispose();
        _tileBorderPen = new Pen(_tileBorderColor, _tileBorderThickness);
    }

    public void SetSessionBorderAppearance(Color borderColor, float thickness)
    {
        _sessionBorderColor = borderColor;
        _sessionBorderThickness = thickness;
        _sessionBorderPen?.Dispose();
        _sessionBorderPen = new Pen(_sessionBorderColor, _sessionBorderThickness);
    }

    public void NotifyRenderSessionStart()
    {
        _isRenderSessionActive = true; // Устанавливаем флаг до очистки
        ClearActiveTiles(); // Очищаем отдельные активные плитки от предыдущей сессии
        lock (_completedAreaLock)
        {
            _completedTilesUnionArea = Rectangle.Empty; // Сбрасываем общую область завершенных плиток
        }
    }

    public void NotifyRenderSessionComplete()
    {
        _isRenderSessionActive = false;
        ClearActiveTiles(); // Также очищаем активные плитки при завершении
        // _completedTilesUnionArea не нужно сбрасывать здесь, она сбрасывается при следующем Start
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
        _activeTiles.TryRemove(tileBounds, out _); // Убираем из активных (зеленая рамка исчезнет)

        // Обновляем общую область завершенных плиток для красной рамки
        if (_isRenderSessionActive)
        {
            lock (_completedAreaLock)
            {
                if (_completedTilesUnionArea.IsEmpty)
                {
                    _completedTilesUnionArea = tileBounds;
                }
                else
                {
                    _completedTilesUnionArea = Rectangle.Union(_completedTilesUnionArea, tileBounds);
                }
            }
        }
    }

    public void ClearActiveTiles()
    {
        _activeTiles.Clear();
    }

    public void DrawVisualization(Graphics g, Rectangle canvasBounds)
    {
        Rectangle currentCompletedArea;
        lock (_completedAreaLock)
        {
            currentCompletedArea = _completedTilesUnionArea;
        }

        // 1. Рисуем красную рамку для объединенной области ЗАВЕРШЕННЫХ плиток
        if (_isRenderSessionActive && _sessionBorderPen != null && !currentCompletedArea.IsEmpty)
        {
            RectangleF visualSessionBorder = currentCompletedArea;
            // Слегка уменьшаем прямоугольник, чтобы линия пера была нарисована по его границе
            // и внешний край пера совпадал с границей currentCompletedArea.
            visualSessionBorder.Inflate(-_sessionBorderThickness / 2f, -_sessionBorderThickness / 2f);
            if (visualSessionBorder.Width > 0 && visualSessionBorder.Height > 0)
            {
                g.DrawRectangle(_sessionBorderPen, visualSessionBorder.X, visualSessionBorder.Y, visualSessionBorder.Width, visualSessionBorder.Height);
            }
        }

        // 2. Рисуем зеленые рамки для активных плиток (обрабатываемых в данный момент)
        if (!_activeTiles.IsEmpty && _tileBorderPen != null)
        {
            var tilesToDraw = _activeTiles.Keys.ToList(); // Копируем для безопасной итерации

            foreach (var tileRect in tilesToDraw)
            {
                RectangleF visualTileBounds = tileRect;
                visualTileBounds.Inflate(-_tileBorderThickness / 2f, -_tileBorderThickness / 2f);
                if (visualTileBounds.Width > 0 && visualTileBounds.Height > 0)
                {
                    g.DrawRectangle(_tileBorderPen, visualTileBounds.X, visualTileBounds.Y, visualTileBounds.Width, visualTileBounds.Height);
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