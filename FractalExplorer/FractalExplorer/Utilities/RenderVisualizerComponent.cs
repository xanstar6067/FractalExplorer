// RenderVisualizerComponent.cs
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;
using System.Collections.Generic; // Для List<Rectangle>
using System; // Для Math

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

    // НОВОЕ: Список всех ЗАВЕРШЕННЫХ плиток в текущей сессии
    private readonly object _completedTilesListLock = new object();
    private List<Rectangle> _completedTilesList = new List<Rectangle>();

    public RenderVisualizerComponent()
    {
        _tileBorderPen = new Pen(_tileBorderColor, _tileBorderThickness);
        // Для _sessionBorderPen можно попробовать PenAlignment, если линии будут выглядеть смещенными
        // _sessionBorderPen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset; // или Center
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
        // Если меняем толщину, нужно пересоздать перо или обновить его Alignment, если используется
        // _sessionBorderPen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
    }

    public void NotifyRenderSessionStart()
    {
        _isRenderSessionActive = true;
        ClearActiveTiles();
        lock (_completedTilesListLock)
        {
            _completedTilesList.Clear(); // Очищаем список завершенных плиток
        }
    }

    public void NotifyRenderSessionComplete()
    {
        _isRenderSessionActive = false;
        ClearActiveTiles();
    }

    public void NotifyTileRenderStart(Rectangle tileBounds)
    {
        if (_isRenderSessionActive)
        {
            _activeTiles.TryAdd(tileBounds, 0);
        }
    }

    public void NotifyTileRenderComplete(Rectangle tileBounds)
    {
        _activeTiles.TryRemove(tileBounds, out _);

        if (_isRenderSessionActive)
        {
            lock (_completedTilesListLock)
            {
                // Проверяем, нет ли уже такой плитки (на всякий случай, хотя не должно быть)
                if (!_completedTilesList.Contains(tileBounds))
                {
                    _completedTilesList.Add(tileBounds);
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
        List<Rectangle> currentCompletedTilesSnapshot;
        lock (_completedTilesListLock)
        {
            currentCompletedTilesSnapshot = new List<Rectangle>(_completedTilesList); // Создаем копию для безопасной итерации
        }

        // 1. Рисуем "ступенчатую" красную рамку для объединенной области ЗАВЕРШЕННЫХ плиток
        if (_isRenderSessionActive && _sessionBorderPen != null && currentCompletedTilesSnapshot.Any())
        {
            // Установка качества рендеринга может помочь с линиями
            // g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach (var currentTile in currentCompletedTilesSnapshot)
            {
                // Координаты текущей плитки
                int rLeft = currentTile.Left;
                int rTop = currentTile.Top;
                int rRight = currentTile.Right;
                int rBottom = currentTile.Bottom;

                // Проверка верхней стороны currentTile
                bool hasAdjacentTop = currentCompletedTilesSnapshot.Any(other =>
                    other != currentTile && // Не сама плитка
                    other.Bottom == rTop && // Нижняя сторона другой плитки совпадает с верхней текущей
                    Math.Max(other.Left, rLeft) < Math.Min(other.Right, rRight) // Горизонтальное перекрытие
                );
                if (!hasAdjacentTop)
                {
                    g.DrawLine(_sessionBorderPen, rLeft, rTop, rRight, rTop);
                }

                // Проверка нижней стороны currentTile
                bool hasAdjacentBottom = currentCompletedTilesSnapshot.Any(other =>
                    other != currentTile &&
                    other.Top == rBottom && // Верхняя сторона другой плитки совпадает с нижней текущей
                    Math.Max(other.Left, rLeft) < Math.Min(other.Right, rRight) // Горизонтальное перекрытие
                );
                if (!hasAdjacentBottom)
                {
                    g.DrawLine(_sessionBorderPen, rLeft, rBottom, rRight, rBottom);
                }

                // Проверка левой стороны currentTile
                bool hasAdjacentLeft = currentCompletedTilesSnapshot.Any(other =>
                    other != currentTile &&
                    other.Right == rLeft && // Правая сторона другой плитки совпадает с левой текущей
                    Math.Max(other.Top, rTop) < Math.Min(other.Bottom, rBottom) // Вертикальное перекрытие
                );
                if (!hasAdjacentLeft)
                {
                    g.DrawLine(_sessionBorderPen, rLeft, rTop, rLeft, rBottom);
                }

                // Проверка правой стороны currentTile
                bool hasAdjacentRight = currentCompletedTilesSnapshot.Any(other =>
                    other != currentTile &&
                    other.Left == rRight && // Левая сторона другой плитки совпадает с правой текущей
                    Math.Max(other.Top, rTop) < Math.Min(other.Bottom, rBottom) // Вертикальное перекрытие
                );
                if (!hasAdjacentRight)
                {
                    g.DrawLine(_sessionBorderPen, rRight, rTop, rRight, rBottom);
                }
            }
        }

        // 2. Рисуем зеленые рамки для активных плиток (обрабатываемых в данный момент)
        if (!_activeTiles.IsEmpty && _tileBorderPen != null)
        {
            var tilesToDraw = _activeTiles.Keys.ToList();

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