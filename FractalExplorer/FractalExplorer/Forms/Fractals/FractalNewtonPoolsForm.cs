using FractalExplorer.Engines;
using FractalExplorer.Resources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;

namespace FractalExplorer
{
    /// <summary>
    /// Форма для отображения и взаимодействия с фракталами Ньютона.
    /// Позволяет задавать комплексные функции, находить их корни и визуализировать
    /// бассейны притяжения этих корней с различными настройками палитры.
    /// </summary>
    public partial class NewtonPools : Form, ISaveLoadCapableFractal
    {
        #region Fields

        /// <summary>
        /// Экземпляр движка для рендеринга фрактала Ньютона.
        /// </summary>
        private readonly FractalNewtonEngine _engine;

        /// <summary>
        /// Таймер для отложенного запуска рендеринга, чтобы избежать частых перерисовок.
        /// </summary>
        private readonly System.Windows.Forms.Timer _renderDebounceTimer;

        /// <summary>
        /// Компонент для визуализации процесса рендеринга плиток.
        /// </summary>
        private RenderVisualizerComponent _renderVisualizer;

        /// <summary>
        /// Менеджер палитр, специфичный для фракталов Ньютона.
        /// </summary>
        private NewtonPaletteManager _paletteManager;

        /// <summary>
        /// Форма для настройки цветовой палитры фрактала Ньютона.
        /// </summary>
        private ColorConfigurationNewtonPoolsForm _colorSettingsForm;

        /// <summary>
        /// Базовый масштаб для преобразования мировых координат в экранные.
        /// </summary>
        private const double BASE_SCALE = 3.0;

        /// <summary>
        /// Размер одной плитки (тайла) для пошагового рендеринга.
        /// </summary>
        private const int TILE_SIZE = 32;

        /// <summary>
        /// Объект для блокировки доступа к битмапам во время рендеринга.
        /// </summary>
        private readonly object _bitmapLock = new object();

        /// <summary>
        /// Битмап, содержащий отрисованное изображение для предпросмотра.
        /// </summary>
        private Bitmap _previewBitmap;

        /// <summary>
        /// Битмап, в который текущий момент происходит рендеринг плиток.
        /// </summary>
        private Bitmap _currentRenderingBitmap;

        /// <summary>
        /// Токен отмены для операций рендеринга предпросмотра.
        /// </summary>
        private CancellationTokenSource _previewRenderCts;

        /// <summary>
        /// Флаг, указывающий, выполняется ли сейчас рендеринг в высоком разрешении.
        /// </summary>
        private volatile bool _isHighResRendering = false;

        /// <summary>
        /// Флаг, указывающий, выполняется ли сейчас рендеринг предпросмотра.
        /// </summary>
        private volatile bool _isRenderingPreview = false;

        /// <summary>
        /// Текущий коэффициент масштабирования фрактала.
        /// </summary>
        private double _zoom = 1.0;

        /// <summary>
        /// Текущая координата X центра видимой области фрактала.
        /// </summary>
        private double _centerX = 0.0;

        /// <summary>
        /// Текущая координата Y центра видимой области фрактала.
        /// </summary>
        private double _centerY = 0.0;

        /// <summary>
        /// Координата X центра, по которой был отрисован _previewBitmap.
        /// Используется для интерполяции при панорамировании/масштабировании.
        /// </summary>
        private double _renderedCenterX;

        /// <summary>
        /// Координата Y центра, по которой был отрисован _previewBitmap.
        /// Используется для интерполяции при панорамировании/масштабировании.
        /// </summary>
        private double _renderedCenterY;

        /// <summary>
        /// Коэффициент масштабирования, по которому был отрисован _previewBitmap.
        /// Используется для интерполяции при панорамировании/масштабировании.
        /// </summary>
        private double _renderedZoom;

        /// <summary>
        /// Начальная позиция курсора мыши при панорамировании.
        /// </summary>
        private Point _panStart;

        /// <summary>
        /// Флаг, указывающий, находится ли пользователь в режиме панорамирования.
        /// </summary>
        private bool _panning = false;

        /// <summary>
        /// Массив предустановленных полиномиальных формул для выбора.
        /// </summary>
        private readonly string[] presetPolynomials =
        {
            "z^3-1",
            "z^4-1",
            "z^5-1",
            "z^6-1",
            "z^3-2*z+2",
            "z^5 - z^2 + 1",
            "z^6 + 3*z^3 - 2",
            "z^4 - 4*z^2 + 4",
            "z^7 + z^4 - z + 1",
            "z^8 + 15*z^4 - 16",
            "z^4 + z^3 + z^2 + z + 1",
            "z^2 - i",
            "(z^2-1)*(z-2*i)",
            "(1+2*i)*z^2+z-1",
            "0.5*z^3 - 1.25*z + 2",
            "(2+i)*z^3 - (1-2*i)*z + 1",
            "i*z^4 + z - 1",
            "(1+0.5*i)*z^2 - z + (2-3*i)",
            "(0.3+1.7*i)*z^3 + (1-i)",
            "(2-i)*z^5 + (3+2*i)*z^2 - 1",
            "-2*z^3 + 0.75*z^2 - 1",
            "z^6 - 1.5*z^3 + 0.25",
            "-0.1*z^4 + z - 2",
            "(1/2)*z^3 + (3/4)*z - 1",
            "(2+3*i)*(z^2) - (1-i)*z + 4",
            "(z^2-1)/(z^2+1)",
            "(z^3-1)/(z^3+1)",
            "z^2 / (z-1)^2",
            "(z^4-1)/(z*z-2*z+1)"
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NewtonPools"/>.
        /// </summary>
        public NewtonPools()
        {
            InitializeComponent();
            _engine = new FractalNewtonEngine();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _paletteManager = new NewtonPaletteManager();
            InitializeForm();
        }

        #endregion

        #region UI Initialization

        /// <summary>
        /// Выполняет инициализацию элементов управления формы и подписку на события.
        /// </summary>
        private void InitializeForm()
        {
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;

            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            cbSelector.Items.AddRange(presetPolynomials);
            cbSelector.SelectedIndex = 0;
            richTextInput.Text = cbSelector.SelectedItem.ToString();

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++)
            {
                cbThreads.Items.Add(i);
            }
            cbThreads.Items.Add("Auto");
            cbThreads.SelectedItem = "Auto";

            nudZoom.Minimum = 0.001M;
            nudZoom.Maximum = 1_000_000_000_000M;
            nudZoom.DecimalPlaces = 4;
            nudZoom.Value = (decimal)_zoom;

            btnConfigurePalette.Click += btnConfigurePalette_Click;

            nudIterations.ValueChanged += (s, e) => ScheduleRender();
            cbThreads.SelectedIndexChanged += (s, e) => ScheduleRender();
            nudZoom.ValueChanged += (s, e) => { _zoom = (double)nudZoom.Value; ScheduleRender(); };
            cbSelector.SelectedIndexChanged += cbSelector_SelectedIndexChanged;
            richTextInput.TextChanged += (s, e) => ScheduleRender();
            btnRender.Click += (s, e) => ScheduleRender();
            btnSave.Click += btnSave_Click;

            fractal_bitmap.MouseWheel += Canvas_MouseWheel;
            fractal_bitmap.MouseDown += Canvas_MouseDown;
            fractal_bitmap.MouseMove += Canvas_MouseMove;
            fractal_bitmap.MouseUp += Canvas_MouseUp;
            fractal_bitmap.Paint += Canvas_Paint;
            fractal_bitmap.Resize += (s, e) =>
            {
                if (WindowState != FormWindowState.Minimized)
                {
                    ScheduleRender();
                }
            };

            // Инициализация отображаемых параметров для корректной интерполяции при первом рендеринге.
            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            ApplyActivePalette();
            ScheduleRender();
        }

        #endregion

        #region Palette Management

        /// <summary>
        /// Обработчик события клика по кнопке "Configure Palette".
        /// Открывает форму настройки палитры для фрактала Ньютона.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnConfigurePalette_Click(object sender, EventArgs e)
        {
            // Сначала пытаемся установить формулу, чтобы получить количество корней.
            // Это необходимо для корректного отображения количества цветов в форме настройки палитры.
            if (!_engine.SetFormula(richTextInput.Text, out string _))
            {
                MessageBox.Show("Сначала введите корректную формулу, чтобы определить количество корней.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_colorSettingsForm == null || _colorSettingsForm.IsDisposed)
            {
                _colorSettingsForm = new ColorConfigurationNewtonPoolsForm(_paletteManager);
                _colorSettingsForm.PaletteChanged += (s, palette) =>
                {
                    _paletteManager.ActivePalette = palette;
                    ApplyActivePalette();
                    ScheduleRender();
                };
            }
            _colorSettingsForm.ShowWithRootCount(_engine.Roots.Count);
        }

        /// <summary>
        /// Применяет активную палитру из менеджера палитр к движку фрактала.
        /// </summary>
        private void ApplyActivePalette()
        {
            var palette = _paletteManager.ActivePalette;
            if (palette == null)
            {
                return;
            }

            // Если в палитре нет заданных цветов для корней, генерируем гармонические цвета по умолчанию.
            // Это обеспечивает базовое отображение, даже если пользователь не настроил цвета.
            if (palette.RootColors == null || palette.RootColors.Count == 0)
            {
                _engine.RootColors = ColorConfigurationNewtonPoolsForm.GenerateHarmonicColors(_engine.Roots.Count).ToArray();
            }
            else
            {
                _engine.RootColors = palette.RootColors.ToArray();
            }

            _engine.BackgroundColor = palette.BackgroundColor;
            _engine.UseGradient = palette.IsGradient;
        }

        #endregion

        #region Rendering Logic

        /// <summary>
        /// Обработчик события, когда визуализатор рендеринга запрашивает перерисовку канваса.
        /// </summary>
        private void OnVisualizerNeedsRedraw()
        {
            // Используем BeginInvoke, чтобы перерисовка происходила в UI-потоке,
            // поскольку этот метод может быть вызван из фонового потока.
            if (fractal_bitmap.IsHandleCreated && !fractal_bitmap.IsDisposed)
            {
                fractal_bitmap.BeginInvoke((Action)(() => fractal_bitmap.Invalidate()));
            }
        }

        /// <summary>
        /// Планирует рендеринг предпросмотра, используя таймер задержки.
        /// Это предотвращает избыточные рендеринги при частых изменениях параметров (например, при вводе текста).
        /// </summary>
        private void ScheduleRender()
        {
            // Не планируем рендеринг, если идет высококачественный рендеринг или форма свернута,
            // чтобы избежать лишних вычислений и мерцания.
            if (_isHighResRendering || WindowState == FormWindowState.Minimized)
            {
                return;
            }
            if (_isRenderingPreview)
            {
                _previewRenderCts?.Cancel(); // Отменяем текущий предпросмотр, если он активен, в пользу нового.
            }
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
        }

        /// <summary>
        /// Обработчик события тика таймера задержки рендеринга.
        /// Запускает рендеринг предпросмотра, если нет других активных рендерингов.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private async void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimer.Stop();
            // Если уже что-то рендерится, откладываем новый рендеринг, чтобы избежать конфликтов.
            if (_isHighResRendering || _isRenderingPreview)
            {
                ScheduleRender();
                return;
            }
            await StartPreviewRender();
        }

        /// <summary>
        /// Запускает процесс рендеринга предпросмотра фрактала в фоновом потоке.
        /// Рендеринг выполняется по плиткам с отображением прогресса.
        /// </summary>
        /// <returns>Задача, представляющая асинхронную операцию рендеринга.</returns>
        private async Task StartPreviewRender()
        {
            // Не рендерим, если область для рисования некорректна.
            if (fractal_bitmap.Width <= 0 || fractal_bitmap.Height <= 0)
            {
                return;
            }

            _isRenderingPreview = true;
            _previewRenderCts?.Cancel(); // Отменяем предыдущий рендеринг, если он еще активен, чтобы начать новый.
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;

            _renderVisualizer?.NotifyRenderSessionStart(); // Уведомляем визуализатор о начале сессии рендеринга.

            // Устанавливаем формулу и проверяем на ошибки парсинга.
            // Если формула некорректна, отображаем отладочную информацию и сбрасываем битмап.
            if (!_engine.SetFormula(richTextInput.Text, out string debugInfo))
            {
                richTextDebugOutput.Text = debugInfo;
                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = null;
                    _currentRenderingBitmap?.Dispose();
                    _currentRenderingBitmap = null;
                }
                if (fractal_bitmap.IsHandleCreated && !fractal_bitmap.IsDisposed)
                {
                    fractal_bitmap.Invalidate(); // Очищаем канвас, если формула некорректна.
                }
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete();
                return;
            }
            richTextDebugOutput.Text = debugInfo;

            // Создаем новый битмап для текущего рендеринга, чтобы избежать модификации уже отображаемого.
            var newRenderingBitmap = new Bitmap(fractal_bitmap.Width, fractal_bitmap.Height, PixelFormat.Format32bppArgb);
            lock (_bitmapLock)
            {
                _currentRenderingBitmap?.Dispose(); // Освобождаем старый текущий битмап перед заменой.
                _currentRenderingBitmap = newRenderingBitmap;
            }

            UpdateEngineParameters(); // Обновляем параметры движка перед рендерингом, используя значения из UI.

            // Сохраняем текущие параметры вида, чтобы знать, для какой области рендерился битмап.
            // Это необходимо для корректной интерполяции при перерисовке канваса.
            double currentRenderedCenterX = _centerX;
            double currentRenderedCenterY = _centerY;
            double currentRenderedZoom = _zoom;

            // Генерируем плитки для рендеринга, сортируя их от центра к краям.
            // Это улучшает визуальное восприятие прогресса, так как центр, как правило, наиболее важен.
            var tiles = GenerateTiles(fractal_bitmap.Width, fractal_bitmap.Height);
            var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount());

            // Инициализируем прогресс-бар на UI потоке.
            if (progressBar.IsHandleCreated && !progressBar.IsDisposed)
            {
                progressBar.Invoke((Action)(() =>
                {
                    progressBar.Value = 0;
                    progressBar.Maximum = tiles.Count;
                }));
            }
            int progress = 0;

            try
            {
                // Запускаем асинхронный рендеринг плиток, позволяя отменять операцию.
                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested(); // Проверка на отмену в начале обработки плитки.

                    _renderVisualizer?.NotifyTileRenderStart(tile.Bounds); // Уведомляем визуализатор о начале рендеринга плитки.

                    var tileBuffer = _engine.RenderSingleTile(tile, fractal_bitmap.Width, fractal_bitmap.Height, out int bytesPerPixel);

                    ct.ThrowIfCancellationRequested(); // Проверка на отмену после рендеринга плитки, перед записью.

                    lock (_bitmapLock)
                    {
                        // Если рендеринг был отменен или запущен новый (т.е. _currentRenderingBitmap уже другой),
                        // не записываем данные в устаревший битмап.
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap)
                        {
                            return;
                        }

                        // Записываем данные плитки в основной битмап, используя LockBits для прямого доступа к пикселям.
                        BitmapData bitmapData = _currentRenderingBitmap.LockBits(tile.Bounds, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
                        int tileWidthInBytes = tile.Bounds.Width * bytesPerPixel;

                        for (int y = 0; y < tile.Bounds.Height; y++)
                        {
                            IntPtr destinationPointer = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride);
                            int sourceOffset = y * tileWidthInBytes;
                            Marshal.Copy(tileBuffer, sourceOffset, destinationPointer, tileWidthInBytes);
                        }
                        _currentRenderingBitmap.UnlockBits(bitmapData);
                    }

                    _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds); // Уведомляем визуализатор о завершении рендеринга плитки.

                    // Обновляем прогресс-бар на UI потоке.
                    if (ct.IsCancellationRequested || !fractal_bitmap.IsHandleCreated || fractal_bitmap.IsDisposed)
                    {
                        return;
                    }
                    fractal_bitmap.Invoke((Action)(() =>
                    {
                        if (!ct.IsCancellationRequested && progressBar.IsHandleCreated && !progressBar.IsDisposed)
                        {
                            progressBar.Value = Math.Min(progressBar.Maximum, Interlocked.Increment(ref progress));
                        }
                    }));
                    await Task.Yield(); // Освобождаем поток для UI-ответов.
                }, token);

                token.ThrowIfCancellationRequested(); // Финальная проверка на отмену после завершения всех плиток.

                // По завершении рендеринга, заменяем предпросмотр текущим битмапом.
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose();
                        _previewBitmap = new Bitmap(_currentRenderingBitmap); // Создаем копию для _previewBitmap.
                        _currentRenderingBitmap.Dispose();
                        _currentRenderingBitmap = null;
                        // Сохраняем параметры, по которым был отрисован _previewBitmap, для корректной интерполяции.
                        _renderedCenterX = currentRenderedCenterX;
                        _renderedCenterY = currentRenderedCenterY;
                        _renderedZoom = currentRenderedZoom;
                    }
                    else
                    {
                        newRenderingBitmap?.Dispose(); // Если битмап был заменен другим рендерингом, освобождаем наш.
                    }
                }
                // Запрашиваем финальную перерисовку канваса для отображения полностью отрендеренного изображения.
                if (fractal_bitmap.IsHandleCreated && !fractal_bitmap.IsDisposed)
                {
                    fractal_bitmap.Invalidate();
                }
            }
            catch (OperationCanceledException)
            {
                // Если операция отменена, освобождаем ресурсы текущего рендеринга.
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _currentRenderingBitmap?.Dispose();
                        _currentRenderingBitmap = null;
                    }
                }
                newRenderingBitmap?.Dispose();
            }
            catch (Exception ex)
            {
                newRenderingBitmap?.Dispose();
                if (IsHandleCreated && !IsDisposed)
                {
                    MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer?.NotifyRenderSessionComplete(); // Уведомляем визуализатор о завершении сессии.
                // Сбрасываем прогресс-бар.
                if (progressBar.IsHandleCreated && !progressBar.IsDisposed)
                {
                    progressBar.Invoke((Action)(() => progressBar.Value = 0));
                }
            }
        }

        /// <summary>
        /// Генерирует список плиток для рендеринга. Плитки сортируются по удаленности от центра,
        /// чтобы обеспечить более быстрый рендеринг центральной части и улучшить визуальное восприятие прогресса.
        /// </summary>
        /// <param name="width">Общая ширина области рендеринга.</param>
        /// <param name="height">Общая высота области рендеринга.</param>
        /// <returns>Список объектов <see cref="TileInfo"/>.</returns>
        private List<TileInfo> GenerateTiles(int width, int height)
        {
            var tiles = new List<TileInfo>();
            Point center = new Point(width / 2, height / 2);

            for (int y = 0; y < height; y += TILE_SIZE)
            {
                for (int x = 0; x < width; x += TILE_SIZE)
                {
                    int tileWidth = Math.Min(TILE_SIZE, width - x);
                    int tileHeight = Math.Min(TILE_SIZE, height - y);
                    tiles.Add(new TileInfo(x, y, tileWidth, tileHeight));
                }
            }
            // Сортируем плитки, чтобы сначала рендерились те, что ближе к центру.
            return tiles.OrderBy(t => Math.Pow(t.Center.X - center.X, 2) + Math.Pow(t.Center.Y - center.Y, 2)).ToList();
        }

        #endregion

        #region Canvas Interaction

        /// <summary>
        /// Обработчик события отрисовки канваса.
        /// Отображает текущий предпросмотр или процесс рендеринга, интерполируя его при необходимости.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события рисования.</param>
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black); // Очищаем фон канваса.
            e.Graphics.InterpolationMode = InterpolationMode.Bilinear; // Используем билинейную интерполяцию для плавного масштабирования.

            lock (_bitmapLock)
            {
                if (_previewBitmap != null && fractal_bitmap.Width > 0 && fractal_bitmap.Height > 0)
                {
                    try
                    {
                        // Вычисляем параметры масштабирования и смещения для интерполяции.
                        // Это позволяет отображать старый, частично отрендеренный битмап,
                        // корректно масштабируя его под текущий зум и панорамирование.
                        double renderedScale = BASE_SCALE / _renderedZoom;
                        double currentScale = BASE_SCALE / _zoom;
                        float drawScaleRatio = (float)(renderedScale / currentScale);

                        float newWidth = fractal_bitmap.Width * drawScaleRatio;
                        float newHeight = fractal_bitmap.Height * drawScaleRatio;

                        double deltaReal = _renderedCenterX - _centerX;
                        double deltaImaginary = _renderedCenterY - _centerY;

                        // Вычисляем смещение в пикселях, чтобы точка обзора оставалась на месте.
                        float offsetX = (float)(deltaReal / currentScale * fractal_bitmap.Width);
                        float offsetY = (float)(deltaImaginary / currentScale * fractal_bitmap.Width);

                        // Вычисляем позицию отрисовки.
                        float drawX = (fractal_bitmap.Width - newWidth) / 2.0f + offsetX;
                        float drawY = (fractal_bitmap.Height - newHeight) / 2.0f + offsetY;

                        var destinationRectangle = new RectangleF(drawX, drawY, newWidth, newHeight);
                        e.Graphics.DrawImage(_previewBitmap, destinationRectangle);
                    }
                    catch (Exception)
                    {
                        // Игнорируем ошибки при интерполяции, так как это не критично для функциональности,
                        // и просто предотвращает отображение интерполированной картинки.
                    }
                }
                // Если идет текущий рендеринг (плитками), рисуем его поверх предпросмотра,
                // чтобы отображать прогресс в реальном времени.
                if (_currentRenderingBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }
                // Рисуем визуализатор процесса рендеринга (сетка плиток и т.д.).
                if (_renderVisualizer != null && _isRenderingPreview)
                {
                    _renderVisualizer.DrawVisualization(e.Graphics);
                }
            }
        }

        /// <summary>
        /// Обработчик события прокрутки колеса мыши над канвасом.
        /// Изменяет масштаб фрактала и обновляет центр, чтобы сохранить точку под курсором.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события колеса мыши.</param>
        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering)
            {
                return;
            }

            // Сохраняем текущий прогресс рендеринга в _previewBitmap.
            // Это позволяет плавно масштабировать уже отрендеренную часть.
            CommitAndBakePreview();

            double zoomFactor = e.Delta > 0 ? 1.5 : 1.0 / 1.5;
            // Вычисляем масштаб на пиксель до зума, чтобы корректно преобразовать экранные координаты в мировые.
            double scaleBefore = BASE_SCALE / _zoom / fractal_bitmap.Width;

            // Вычисляем мировые координаты точки под курсором до изменения масштаба.
            double mouseReal = _centerX + (e.X - fractal_bitmap.Width / 2.0) * scaleBefore;
            double mouseImaginary = _centerY + (e.Y - fractal_bitmap.Height / 2.0) * scaleBefore;

            _zoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, _zoom * zoomFactor));

            // Вычисляем масштаб на пиксель после зума.
            double scaleAfter = BASE_SCALE / _zoom / fractal_bitmap.Width;
            // Пересчитываем новый центр так, чтобы точка под курсором осталась на месте после масштабирования.
            _centerX = mouseReal - (e.X - fractal_bitmap.Width / 2.0) * scaleAfter;
            _centerY = mouseImaginary - (e.Y - fractal_bitmap.Height / 2.0) * scaleAfter;

            fractal_bitmap.Invalidate(); // Запрашиваем немедленную перерисовку для плавного отображения зума.

            // Обновляем NumericUpDown, но только если значение действительно изменилось, чтобы избежать зацикливания.
            if (nudZoom.Value != (decimal)_zoom)
            {
                nudZoom.Value = (decimal)_zoom;
            }
            else
            {
                // Если nudZoom.Value уже _zoom (например, из-за ограничения Max/Min), все равно планируем рендеринг,
                // так как центр изменился.
                ScheduleRender();
            }
        }

        /// <summary>
        /// Обработчик события нажатия кнопки мыши над канвасом.
        /// Инициирует режим панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering)
            {
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                _panning = true;
                _panStart = e.Location;
            }
        }

        /// <summary>
        /// Обработчик события перемещения мыши над канвасом.
        /// Выполняет панорамирование фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering || !_panning)
            {
                return;
            }

            // Сохраняем текущий прогресс рендеринга в _previewBitmap.
            // Это позволяет плавно панорамировать уже отрендеренную часть изображения.
            CommitAndBakePreview();

            // Вычисляем масштаб на пиксель, чтобы преобразовать движение мыши в мировые координаты.
            double scale = BASE_SCALE / _zoom / fractal_bitmap.Width;
            _centerX -= (e.X - _panStart.X) * scale;
            _centerY -= (e.Y - _panStart.Y) * scale;
            _panStart = e.Location; // Обновляем начальную точку панорамирования для следующего шага.

            fractal_bitmap.Invalidate(); // Запрашиваем немедленную перерисовку для плавного панорамирования.
            ScheduleRender(); // Планируем новый рендеринг для высокой четкости после завершения панорамирования.
        }

        /// <summary>
        /// Обработчик события отпускания кнопки мыши над канвасом.
        /// Завершает режим панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isHighResRendering)
            {
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                _panning = false;
            }
        }

        /// <summary>
        /// Обработчик события изменения выбранного элемента в выпадающем списке формул.
        /// Обновляет текстовое поле ввода формулы выбранной формулой.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void cbSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbSelector.SelectedIndex >= 0)
            {
                richTextInput.Text = cbSelector.SelectedItem.ToString();
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Объединяет текущий рендеринг предпросмотра с существующим основным битмапом.
        /// Используется во время интерактивных операций (зум, панорамирование) для сохранения промежуточного результата.
        /// Это предотвращает мерцание и потерю уже отрендеренных частей изображения при изменении масштаба/положения.
        /// </summary>
        private void CommitAndBakePreview()
        {
            lock (_bitmapLock)
            {
                if (!_isRenderingPreview || _currentRenderingBitmap == null)
                {
                    return;
                }
            }

            // Отменяем текущий процесс рендеринга плиток, так как мы создаем "снимок" того, что уже есть.
            _previewRenderCts?.Cancel();

            lock (_bitmapLock)
            {
                if (_currentRenderingBitmap == null)
                {
                    return;
                }

                // Создаем новый битмап для сохранения объединенного изображения.
                // Перерисовываем содержимое канваса (который включает _previewBitmap и _currentRenderingBitmap)
                // в этот новый "запеченный" битмап.
                var bakedBitmap = new Bitmap(fractal_bitmap.Width, fractal_bitmap.Height, PixelFormat.Format24bppRgb);
                using (var graphics = Graphics.FromImage(bakedBitmap))
                {
                    var currentRectangle = fractal_bitmap.ClientRectangle;
                    var paintEventArgs = new PaintEventArgs(graphics, currentRectangle);
                    Canvas_Paint(this, paintEventArgs); // Повторно используем логику Canvas_Paint для отрисовки.
                }

                _previewBitmap?.Dispose();
                _previewBitmap = bakedBitmap; // Новый объединенный битмап становится предпросмотром.
                _currentRenderingBitmap.Dispose();
                _currentRenderingBitmap = null; // Текущий битмап очищается, так как его содержимое теперь в _previewBitmap.

                // Обновляем параметры, по которым был отрисован _previewBitmap, для будущей интерполяции.
                _renderedCenterX = _centerX;
                _renderedCenterY = _centerY;
                _renderedZoom = _zoom;
            }
        }

        /// <summary>
        /// Обновляет параметры движка фрактала на основе текущих значений элементов управления.
        /// </summary>
        private void UpdateEngineParameters()
        {
            _engine.MaxIterations = (int)nudIterations.Value;
            _engine.CenterX = _centerX;
            _engine.CenterY = _centerY;
            _engine.Scale = BASE_SCALE / _zoom;
            ApplyActivePalette(); // Убеждаемся, что палитра тоже обновлена и применена к движку.
        }

        /// <summary>
        /// Определяет количество потоков для использования в параллельных вычислениях на основе выбора пользователя.
        /// Если выбрано "Auto", возвращает количество логических процессоров системы.
        /// </summary>
        /// <returns>Количество потоков.</returns>
        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Save".
        /// Рендерит фрактал в высоком разрешении и сохраняет его в файл PNG.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (_isHighResRendering)
            {
                return;
            }
            _isHighResRendering = true;
            SetMainControlsEnabled(false);

            int saveWidth = (int)nudW.Value;
            int saveHeight = (int)nudH.Value;

            using (var saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"newton_pools_{DateTime.Now:yyyyMMdd_HHmmss}.png" })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    progressBar.Value = 0;
                    progressBar.Visible = true;

                    // Создаем отдельный движок для сохранения, чтобы не изменять параметры текущего движка,
                    // который может быть занят рендерингом предпросмотра.
                    var saveEngine = new FractalNewtonEngine();
                    if (!saveEngine.SetFormula(richTextInput.Text, out _))
                    {
                        MessageBox.Show("Ошибка в формуле, сохранение отменено.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        FinalizeSave();
                        return;
                    }

                    int threadCount = GetThreadCount();

                    // Копируем параметры из текущего движка в движок для сохранения,
                    // чтобы изображение было отрендерено с актуальными настройками.
                    saveEngine.MaxIterations = (int)nudIterations.Value;
                    saveEngine.CenterX = _centerX;
                    saveEngine.CenterY = _centerY;
                    saveEngine.Scale = BASE_SCALE / _zoom;

                    // Копируем параметры палитры.
                    ApplyActivePalette(); // Убеждаемся, что _engine.RootColors и другие параметры актуальны.
                    saveEngine.RootColors = _engine.RootColors;
                    saveEngine.BackgroundColor = _engine.BackgroundColor;
                    saveEngine.UseGradient = _engine.UseGradient;

                    try
                    {
                        Bitmap highResBitmap = await Task.Run(() => saveEngine.RenderToBitmap(
                            saveWidth, saveHeight,
                            threadCount,
                            progress =>
                            {
                                // Обновляем прогресс-бар на UI потоке, так как это UI-операция.
                                if (progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
                                {
                                    progressPNG.Invoke((Action)(() => progressPNG.Value = Math.Min(100, progress)));
                                }
                            }
                        ));

                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        MessageBox.Show("Изображение сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            FinalizeSave(); // Завершаем процесс сохранения независимо от результата диалога.
        }

        /// <summary>
        /// Выполняет финальные действия после завершения сохранения изображения,
        /// такие как сброс флагов состояния и скрытие прогресс-бара.
        /// </summary>
        private void FinalizeSave()
        {
            _isHighResRendering = false;
            SetMainControlsEnabled(true);
            if (progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
            {
                progressPNG.Invoke((Action)(() => { progressPNG.Visible = false; progressPNG.Value = 0; }));
            }
        }

        /// <summary>
        /// Включает или отключает основные элементы управления формы.
        /// Используется для предотвращения изменения параметров во время рендеринга в высоком разрешении.
        /// </summary>
        /// <param name="enabled">True для включения, False для отключения.</param>
        private void SetMainControlsEnabled(bool enabled)
        {
            Action action = () =>
            {
                panel1.Enabled = enabled; // Предполагаем, что panel1 содержит основные элементы управления.
                btnSave.Enabled = enabled; // Кнопка сохранения управляется отдельно.
            };
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }

        #endregion

        #region Form Lifecycle

        /// <summary>
        /// Обработчик события закрытия формы.
        /// Освобождает ресурсы и отменяет любые активные операции рендеринга для предотвращения утечек памяти.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e); // Вызов базового метода для стандартной обработки закрытия формы.
            _renderDebounceTimer?.Stop();
            _previewRenderCts?.Cancel();
            _previewRenderCts?.Dispose();
            _renderDebounceTimer?.Dispose();
            _colorSettingsForm?.Close(); // Закрываем форму настроек цвета, если она открыта.
            _colorSettingsForm?.Dispose();
            if (_renderVisualizer != null)
            {
                _renderVisualizer.NeedsRedraw -= OnVisualizerNeedsRedraw; // Отписываемся от события во избежание утечек.
                _renderVisualizer.Dispose();
            }
        }

        #endregion

        #region ISaveLoadCapableFractal Implementation

        /// <summary>
        /// Получает строковый идентификатор типа фрактала.
        /// </summary>
        /// <value>Идентификатор типа фрактала, используемый для сохранения/загрузки.</value>
        public string FractalTypeIdentifier => "NewtonPools";

        /// <summary>
        /// Получает конкретный тип состояния сохранения для данного фрактала.
        /// </summary>
        /// <value>Тип состояния сохранения, который должен использоваться для сериализации/десериализации.</value>
        public Type ConcreteSaveStateType => typeof(NewtonSaveState);

        /// <summary>
        /// Вспомогательный класс для параметров превью фрактала Ньютона.
        /// Используется для сериализации и десериализации параметров, необходимых для быстрого рендеринга миниатюр.
        /// </summary>
        public class NewtonPreviewParams
        {
            /// <summary>
            /// Получает или задает формулу фрактала.
            /// </summary>
            public string Formula { get; set; }

            /// <summary>
            /// Получает или задает координату X центра фрактала.
            /// </summary>
            public decimal CenterX { get; set; }

            /// <summary>
            /// Получает или задает координату Y центра фрактала.
            /// </summary>
            public decimal CenterY { get; set; }

            /// <summary>
            /// Получает или задает коэффициент масштабирования фрактала.
            /// </summary>
            public decimal Zoom { get; set; }

            /// <summary>
            /// Получает или задает количество итераций для рендеринга.
            /// Уменьшенные итерации используются для ускорения генерации превью.
            /// </summary>
            public int Iterations { get; set; }

            /// <summary>
            /// Получает или задает снимок палитры, используемой для рендеринга.
            /// </summary>
            public NewtonColorPalette PaletteSnapshot { get; set; }
        }

        /// <summary>
        /// Получает текущее состояние фрактала для сохранения.
        /// </summary>
        /// <param name="saveName">Имя, присвоенное сохраняемому состоянию.</param>
        /// <returns>Объект <see cref="FractalSaveStateBase"/>, содержащий текущие параметры фрактала.</returns>
        public FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            // Важно: Убедимся, что текущая активная палитра в менеджере синхронизирована
            // с цветами, отображаемыми на UI. Это гарантирует, что при сохранении будет записана
            // актуальная палитра, даже если пользователь изменил цвета без сохранения в менеджере палитр.
            _paletteManager.ActivePalette.BackgroundColor = _engine.BackgroundColor;
            _paletteManager.ActivePalette.IsGradient = _engine.UseGradient;
            _paletteManager.ActivePalette.RootColors = new List<Color>(_engine.RootColors);

            var state = new NewtonSaveState(this.FractalTypeIdentifier)
            {
                SaveName = saveName,
                Timestamp = DateTime.Now,
                Formula = richTextInput.Text,
                CenterX = (decimal)_centerX, // Приводим double к decimal для сохранения.
                CenterY = (decimal)_centerY,
                Zoom = (decimal)_zoom,
                Iterations = (int)nudIterations.Value,
                // Сохраняем "снимок" текущей активной палитры, чтобы ее можно было восстановить при загрузке.
                PaletteSnapshot = _paletteManager.ActivePalette
            };

            var previewParams = new NewtonPreviewParams
            {
                Formula = state.Formula,
                CenterX = state.CenterX,
                CenterY = state.CenterY,
                Zoom = state.Zoom,
                // Уменьшаем количество итераций для превью, чтобы ускорить его генерацию.
                Iterations = Math.Min(state.Iterations, 50),
                PaletteSnapshot = state.PaletteSnapshot
            };

            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.Converters.Add(new Utilities.JsonConverters.JsonColorConverter());
            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams, jsonOptions);

            return state;
        }

        /// <summary>
        /// Загружает состояние фрактала из предоставленного объекта состояния.
        /// Обновляет параметры UI и запускает новый рендеринг.
        /// </summary>
        /// <param name="stateBase">Базовый объект состояния фрактала для загрузки.</param>
        public void LoadState(FractalSaveStateBase stateBase)
        {
            if (stateBase is NewtonSaveState state)
            {
                _isRenderingPreview = false;
                _previewRenderCts?.Cancel();
                _renderDebounceTimer.Stop();

                // Устанавливаем параметры фрактала из загруженного состояния.
                _centerX = (double)state.CenterX;
                _centerY = (double)state.CenterY;
                _zoom = (double)state.Zoom;

                // Обновляем элементы UI для отображения загруженных параметров.
                richTextInput.Text = state.Formula;
                nudZoom.Value = (decimal)_zoom;
                nudIterations.Value = state.Iterations;

                // Загружаем палитру. Поскольку палитра сохранена целиком как снимок,
                // мы можем просто установить ее как активную в менеджере палитр.
                _paletteManager.ActivePalette = state.PaletteSnapshot;

                // Применяем параметры к движку (UpdateEngineParameters сделает это),
                // включая обновленную палитру.
                UpdateEngineParameters();

                // Сбрасываем текущие битмапы рендеринга, чтобы новый рендеринг начался с чистого листа.
                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = null;
                    _currentRenderingBitmap?.Dispose();
                    _currentRenderingBitmap = null;
                }
                // Обновляем параметры, по которым будет отображаться предпросмотр, для корректной интерполяции.
                _renderedCenterX = _centerX;
                _renderedCenterY = _centerY;
                _renderedZoom = _zoom;

                ScheduleRender(); // Запускаем рендеринг нового состояния.
            }
            else
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Асинхронно рендерит плитку для предварительного просмотра.
        /// Этот метод используется системой сохранения/загрузки для отображения миниатюр состояний.
        /// </summary>
        /// <param name="state">Состояние фрактала для рендеринга.</param>
        /// <param name="tile">Информация о запрашиваемой плитке.</param>
        /// <param name="totalWidth">Общая ширина изображения предварительного просмотра.</param>
        /// <param name="totalHeight">Общая высота изображения предварительного просмотра.</param>
        /// <param name="tileSize">Размер одной плитки (для вывода).</param>
        /// <returns>Массив байтов, представляющий данные пикселей для запрошенной плитки.</returns>
        public async Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase state, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            return await Task.Run(() =>
            {
                // Если параметры предпросмотра отсутствуют, возвращаем пустой буфер.
                if (string.IsNullOrEmpty(state.PreviewParametersJson))
                {
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

                NewtonPreviewParams previewParams;
                try
                {
                    // Десериализуем параметры предпросмотра из JSON.
                    var jsonOptions = new JsonSerializerOptions();
                    jsonOptions.Converters.Add(new Utilities.JsonConverters.JsonColorConverter());
                    previewParams = JsonSerializer.Deserialize<NewtonPreviewParams>(state.PreviewParametersJson, jsonOptions);
                }
                catch
                {
                    // В случае ошибки десериализации, возвращаем пустой буфер, чтобы не прерывать процесс.
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

                var previewEngine = new FractalNewtonEngine();
                // Если формула невалидна, возвращаем пустой буфер.
                if (!previewEngine.SetFormula(previewParams.Formula, out _))
                {
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

                previewEngine.CenterX = (double)previewParams.CenterX;
                previewEngine.CenterY = (double)previewParams.CenterY;
                previewEngine.Scale = 3.0 / (double)previewParams.Zoom; // BaseScale = 3.0 для Ньютона.
                previewEngine.MaxIterations = 150; // Используем повышенное количество итераций для более качественного превью.

                // Настраиваем палитру движка из снимка, сохраненного в параметрах превью.
                var palette = previewParams.PaletteSnapshot;
                previewEngine.BackgroundColor = palette.BackgroundColor;
                previewEngine.UseGradient = palette.IsGradient;
                // Если в снимке нет цветов для корней, генерируем их на основе найденных корней.
                previewEngine.RootColors = (palette.RootColors != null && palette.RootColors.Any()) ? palette.RootColors.ToArray() : ColorConfigurationNewtonPoolsForm.GenerateHarmonicColors(previewEngine.Roots.Count).ToArray();

                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }

        /// <summary>
        /// Рендерит полное изображение предварительного просмотра фрактала на основе заданного состояния.
        /// </summary>
        /// <param name="state">Состояние фрактала, содержащее параметры для рендеринга.</param>
        /// <param name="previewWidth">Желаемая ширина изображения предварительного просмотра.</param>
        /// <param name="previewHeight">Желаемая высота изображения предварительного просмотра.</param>
        /// <returns>Объект <see cref="Bitmap"/>, содержащий отрендеренное изображение предварительного просмотра.</returns>
        public Bitmap RenderPreview(FractalSaveStateBase state, int previewWidth, int previewHeight)
        {
            // Если параметры предпросмотра отсутствуют, возвращаем изображение с сообщением об ошибке.
            if (string.IsNullOrEmpty(state.PreviewParametersJson))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkGray); TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            NewtonPreviewParams previewParams;
            try
            {
                // Десериализуем параметры предпросмотра из JSON.
                var jsonOptions = new JsonSerializerOptions();
                jsonOptions.Converters.Add(new Utilities.JsonConverters.JsonColorConverter());
                previewParams = JsonSerializer.Deserialize<NewtonPreviewParams>(state.PreviewParametersJson, jsonOptions);
            }
            catch (Exception)
            {
                // В случае ошибки десериализации, возвращаем изображение с сообщением об ошибке.
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkRed); TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            var previewEngine = new FractalNewtonEngine();
            // Если формула невалидна в сохраненном состоянии, возвращаем изображение с сообщением об ошибке.
            if (!previewEngine.SetFormula(previewParams.Formula, out _))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkRed); TextRenderer.DrawText(g, "Ошибка формулы", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            previewEngine.CenterX = (double)previewParams.CenterX;
            previewEngine.CenterY = (double)previewParams.CenterY;
            // Предотвращаем деление на ноль, если зум оказался равен 0.
            if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
            previewEngine.Scale = 3.0 / (double)previewParams.Zoom; // Для Ньютона BaseScale = 3.0.
            previewEngine.MaxIterations = previewParams.Iterations;

            // Настраиваем палитру движка из снимка, сохраненного в параметрах.
            var palette = previewParams.PaletteSnapshot;
            previewEngine.BackgroundColor = palette.BackgroundColor;
            previewEngine.UseGradient = palette.IsGradient;
            if (palette.RootColors != null && palette.RootColors.Count > 0)
            {
                previewEngine.RootColors = palette.RootColors.ToArray();
            }
            else
            {
                // Если в снимке нет цветов, генерируем их на основе найденных корней фрактала.
                previewEngine.RootColors = ColorConfigurationNewtonPoolsForm.GenerateHarmonicColors(previewEngine.Roots.Count).ToArray();
            }

            // Рендерим изображение предварительного просмотра с использованием одного потока для эффективности.
            return previewEngine.RenderToBitmap(previewWidth, previewHeight, 1, progress => { });
        }

        /// <summary>
        /// Загружает все сохраненные состояния фрактала, специфичные для данного типа.
        /// </summary>
        /// <returns>Список базовых объектов состояний фрактала.</returns>
        public List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<NewtonSaveState>(this.FractalTypeIdentifier);
            // Приводим список к базовому типу FractalSaveStateBase для соответствия интерфейсу.
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        /// <summary>
        /// Сохраняет список состояний фрактала, специфичных для данного типа.
        /// </summary>
        /// <param name="saves">Список базовых объектов состояний фрактала для сохранения.</param>
        public void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<NewtonSaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Менеджер состояний".
        /// Открывает диалоговое окно для сохранения и загрузки состояний фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnStateManager_Click(object sender, EventArgs e)
        {
            // 'this' здесь - это экземпляр NewtonPools, реализующий ISaveLoadCapableFractal,
            // что позволяет диалогу взаимодействовать с текущей формой для сохранения/загрузки.
            using (var dialog = new Forms.SaveLoadDialogForm(this))
            {
                dialog.ShowDialog(this);
            }
        }

        #endregion
    }
}