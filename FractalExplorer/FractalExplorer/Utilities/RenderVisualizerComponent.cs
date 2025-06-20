// RenderVisualizerComponent.cs
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq; // Потребуется для ToList()
// System.Collections.Generic не нужен явно для этого изменения, но часто полезен

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

    // НОВОЕ: Для отслеживания области, затронутой рендерингом в текущей сессии
    private readonly object _sessionAreaLock = new object(); // Для потокобезопасного обновления _currentSessionRenderedArea
    private Rectangle _currentSessionRenderedArea = Rectangle.Empty;

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
        _isRenderSessionActive = true;
        ClearActiveTiles(); // Очищаем отдельные плитки от предыдущей сессии
        lock (_sessionAreaLock)
        {
            _currentSessionRenderedArea = Rectangle.Empty; // Сбрасываем общую область для новой сессии
        }
    }

    public void NotifyRenderSessionComplete()
    {
        _isRenderSessionActive = false;
        ClearActiveTiles(); // Также очищаем активные плитки при завершении
        // _currentSessionRenderedArea не нужно сбрасывать здесь, она сбрасывается при следующем Start
    }

    public void NotifyTileRenderStart(Rectangle tileBounds)
    {
        if (_isRenderSessionActive)
        {
            _activeTiles.TryAdd(tileBounds, 0); // Добавляем для зеленой рамки

            // Обновляем общую область рендеринга для красной рамки
            lock (_sessionAreaLock)
            {
                if (_currentSessionRenderedArea.IsEmpty)
                {
                    _currentSessionRenderedArea = tileBounds;
                }
                else
                {
                    _currentSessionRenderedArea = Rectangle.Union(_currentSessionRenderedArea, tileBounds);
                }
            }
        }
    }

    public void NotifyTileRenderComplete(Rectangle tileBounds)
    {
        _activeTiles.TryRemove(tileBounds, out _);
        // Не изменяем _currentSessionRenderedArea здесь, т.к. плитка всё ещё является частью отрендеренной области сессии
    }

    public void ClearActiveTiles()
    {
        _activeTiles.Clear();
    }

    /// <summary>
    /// Отрисовывает все визуализации.
    /// Красная рамка сессии теперь рисуется вокруг _currentSessionRenderedArea.
    /// Зеленые рамки рисуются для _activeTiles.
    /// </summary>
    /// <param name="g">Объект Graphics для рисования.</param>
    /// <param name="canvasBounds">Полные границы холста. Используется как запасной вариант или для других визуализаций.</param>
    public void DrawVisualization(Graphics g, Rectangle canvasBounds) // canvasBounds пока остается в сигнатуре
    {
        Rectangle sessionAreaSnapshot;
        lock (_sessionAreaLock)
        {
            sessionAreaSnapshot = _currentSessionRenderedArea;
        }

        // 1. Рисуем красную рамку для текущей фактически отрендеренной области сессии
        if (_isRenderSessionActive && _sessionBorderPen != null && !sessionAreaSnapshot.IsEmpty)
        {
            RectangleF visualSessionBorder = sessionAreaSnapshot;
            // Уменьшаем немного, чтобы линия была внутри фактических границ
            visualSessionBorder.Inflate(-_sessionBorderThickness / 2f, -_sessionBorderThickness / 2f);
            if (visualSessionBorder.Width > 0 && visualSessionBorder.Height > 0)
            {
                g.DrawRectangle(_sessionBorderPen, visualSessionBorder.X, visualSessionBorder.Y, visualSessionBorder.Width, visualSessionBorder.Height);
            }
        }

        // 2. Рисуем зеленые рамки для активных плиток (обрабатываемых в данный момент)
        if (!_activeTiles.IsEmpty && _tileBorderPen != null)
        {
            // Копируем ключи для безопасной итерации, если _activeTiles может измениться из другого потока
            var tilesToDraw = _activeTiles.Keys.ToList();

            foreach (var tileBounds in tilesToDraw)
            {
                RectangleF visualTileBounds = tileBounds;
                // Уменьшаем немного, чтобы рамка была четко видна
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