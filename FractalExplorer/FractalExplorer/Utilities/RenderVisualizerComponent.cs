// RenderVisualizerComponent.cs (Оптимизированная версия)
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

public class RenderVisualizerComponent : IDisposable
{
    // --- Конфигурация ---
    private readonly int _tileSize; // Размер плитки для быстрой калькуляции координат

    // --- Состояние ---
    private readonly ConcurrentDictionary<Rectangle, byte> _activeTiles = new ConcurrentDictionary<Rectangle, byte>();
    private readonly object _completedTilesListLock = new object();
    private readonly List<Rectangle> _completedTilesList = new List<Rectangle>();
    private volatile bool _isRenderSessionActive = false;

    // --- Объекты для рисования ---
    private readonly Pen _tileBorderPen;
    private readonly Pen _sessionBorderPen;

    // --- Логика асинхронного обновления ---
    private readonly System.Threading.Timer _redrawTimer;
    private volatile bool _isDirty = false;
    public event Action NeedsRedraw;

    /// <summary>
    /// Конструктор теперь требует размер плитки для оптимизации.
    /// </summary>
    public RenderVisualizerComponent(int tileSize)
    {
        _tileSize = tileSize > 0 ? tileSize : 32; // Проверка на корректность

        _tileBorderPen = new Pen(Color.FromArgb(180, 77, 255, 77), 1.5f);
        _sessionBorderPen = new Pen(Color.FromArgb(180, 255, 0, 0), 2f);

        // Таймер запустится при старте сессии рендеринга
        _redrawTimer = new System.Threading.Timer(OnRedrawTimerTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Метод, вызываемый таймером. Проверяет, были ли изменения, и запрашивает перерисовку.
    /// </summary>
    private void OnRedrawTimerTick(object state)
    {
        if (_isDirty)
        {
            _isDirty = false;
            NeedsRedraw?.Invoke();
        }
    }

    public void NotifyRenderSessionStart()
    {
        _isRenderSessionActive = true;
        _activeTiles.Clear();
        lock (_completedTilesListLock)
        {
            _completedTilesList.Clear();
        }
        _isDirty = true;
        // Запускаем таймер с интервалом 33 мс (~30 FPS)
        // Запускаем таймер с интервалом 16 мс (~60 FPS)
        _redrawTimer.Change(0, 33);
    }

    public void NotifyRenderSessionComplete()
    {
        _isRenderSessionActive = false;
        // Останавливаем таймер
        _redrawTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _isDirty = true;
        OnRedrawTimerTick(null); // Запрашиваем финальную перерисовку, чтобы очистить холст
    }

    public void NotifyTileRenderStart(Rectangle tileBounds)
    {
        if (_isRenderSessionActive)
        {
            _activeTiles.TryAdd(tileBounds, 0);
            _isDirty = true; // Отмечаем, что состояние изменилось
        }
    }

    public void NotifyTileRenderComplete(Rectangle tileBounds)
    {
        _activeTiles.TryRemove(tileBounds, out _);
        if (_isRenderSessionActive)
        {
            lock (_completedTilesListLock)
            {
                _completedTilesList.Add(tileBounds);
            }
            _isDirty = true; // Отмечаем, что состояние изменилось
        }
    }

    /// <summary>
    /// Оптимизированный метод отрисовки. Вызывается из события Paint формы.
    /// </summary>
    public void DrawVisualization(Graphics g)
    {
        if (!_isRenderSessionActive && _activeTiles.IsEmpty) return;

        List<Rectangle> currentCompletedTilesSnapshot;
        lock (_completedTilesListLock)
        {
            currentCompletedTilesSnapshot = new List<Rectangle>(_completedTilesList);
        }

        // 1. Отрисовка красной рамки завершённых плиток (Алгоритм O(N))
        if (_sessionBorderPen != null && currentCompletedTilesSnapshot.Any())
        {
            // Создаём карту для быстрого (O(1)) поиска соседей
            var tileMap = new HashSet<Point>();
            foreach (var rect in currentCompletedTilesSnapshot)
            {
                tileMap.Add(new Point(rect.X / _tileSize, rect.Y / _tileSize));
            }

            foreach (var tile in currentCompletedTilesSnapshot)
            {
                var tileCoord = new Point(tile.X / _tileSize, tile.Y / _tileSize);

                // Рисуем верхнюю грань, если сверху нет соседа
                if (!tileMap.Contains(new Point(tileCoord.X, tileCoord.Y - 1)))
                    g.DrawLine(_sessionBorderPen, tile.Left, tile.Top, tile.Right, tile.Top);

                // Рисуем нижнюю грань, если снизу нет соседа
                if (!tileMap.Contains(new Point(tileCoord.X, tileCoord.Y + 1)))
                    g.DrawLine(_sessionBorderPen, tile.Left, tile.Bottom, tile.Right, tile.Bottom);

                // Рисуем левую грань, если слева нет соседа
                if (!tileMap.Contains(new Point(tileCoord.X - 1, tileCoord.Y)))
                    g.DrawLine(_sessionBorderPen, tile.Left, tile.Top, tile.Left, tile.Bottom);

                // Рисуем правую грань, если справа нет соседа
                if (!tileMap.Contains(new Point(tileCoord.X + 1, tileCoord.Y)))
                    g.DrawLine(_sessionBorderPen, tile.Right, tile.Top, tile.Right, tile.Bottom);
            }
        }

        // 2. Отрисовка зелёных рамок для активных (обрабатываемых) плиток
        if (!_activeTiles.IsEmpty && _tileBorderPen != null)
        {
            foreach (var tileRect in _activeTiles.Keys)
            {
                g.DrawRectangle(_tileBorderPen, tileRect.X, tileRect.Y, tileRect.Width - 1, tileRect.Height - 1);
            }
        }
    }

    public void Dispose()
    {
        _redrawTimer?.Dispose();
        _tileBorderPen?.Dispose();
        _sessionBorderPen?.Dispose();
        GC.SuppressFinalize(this);
    }
}