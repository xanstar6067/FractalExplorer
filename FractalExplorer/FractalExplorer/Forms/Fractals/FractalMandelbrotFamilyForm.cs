using FractalExplorer.Core;
using FractalExplorer.Engines;
using FractalExplorer.Forms;
using FractalExplorer.Projects;
using FractalExplorer.Resources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Text.Json;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;


namespace FractalDraving
{
    /// <summary>
    /// Базовый абстрактный класс для форм, отображающих фракталы.
    /// Предоставляет общую логику для управления движком рендеринга,
    /// палитрой, масштабированием, панорамированием и сохранением изображений.
    /// </summary>
    public abstract partial class FractalMandelbrotFamilyForm : Form, IFractalForm, ISaveLoadCapableFractal
    {
        #region Fields

        /// <summary>
        /// Компонент для визуализации процесса рендеринга плиток.
        /// </summary>
        private RenderVisualizerComponent _renderVisualizer;

        /// <summary>
        /// Менеджер палитр, используемый этой формой.
        /// </summary>
        private ColorPaletteMandelbrotFamily _paletteManager;

        /// <summary>
        /// Форма конфигурации палитр, связанная с этой формой.
        /// </summary>
        private ColorConfigurationMandelbrotFamilyForm _colorConfigForm;

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
        /// Экземпляр движка для рендеринга фрактала.
        /// </summary>
        protected FractalMandelbrotFamilyEngine _fractalEngine;

        /// <summary>
        /// Текущий коэффициент масштабирования фрактала.
        /// </summary>
        protected decimal _zoom = 1.0m;

        /// <summary>
        /// Текущая координата X центра видимой области фрактала.
        /// </summary>
        protected decimal _centerX = 0.0m;

        /// <summary>
        /// Текущая координата Y центра видимой области фрактала.
        /// </summary>
        protected decimal _centerY = 0.0m;

        /// <summary>
        /// Координата X центра, по которой был отрисован _previewBitmap.
        /// Используется для интерполяции при панорамировании/масштабировании.
        /// </summary>
        private decimal _renderedCenterX;

        /// <summary>
        /// Координата Y центра, по которой был отрисован _previewBitmap.
        /// Используется для интерполяции при панорамировании/масштабировании.
        /// </summary>
        private decimal _renderedCenterY;

        /// <summary>
        /// Коэффициент масштабирования, по которому был отрисован _previewBitmap.
        /// Используется для интерполяции при панорамировании/масштабировании.
        /// </summary>
        private decimal _renderedZoom;

        /// <summary>
        /// Начальная позиция курсора мыши при панорамировании.
        /// </summary>
        private Point _panStart;

        /// <summary>
        /// Флаг, указывающий, находится ли пользователь в режиме панорамирования.
        /// </summary>
        private bool _panning = false;

        /// <summary>
        /// Таймер для отложенного запуска рендеринга, чтобы избежать частых перерисовок.
        /// </summary>
        private System.Windows.Forms.Timer _renderDebounceTimer;

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FractalMandelbrotFamilyForm"/>.
        /// </summary>
        protected FractalMandelbrotFamilyForm()
        {
            InitializeComponent();
            _centerX = InitialCenterX;
            _centerY = InitialCenterY;
        }

        #endregion

        #region Protected Abstract/Virtual Methods

        /// <summary>
        /// Абстрактный метод, который должен быть реализован в производных классах
        /// для создания конкретного экземпляра движка фрактала.
        /// </summary>
        /// <returns>Экземпляр <see cref="FractalMandelbrotFamilyEngine"/>.</returns>
        protected abstract FractalMandelbrotFamilyEngine CreateEngine();

        /// <summary>
        /// Получает базовый масштаб для фрактала.
        /// </summary>
        protected virtual decimal BaseScale => 3.0m;

        /// <summary>
        /// Получает начальную координату X центра фрактала.
        /// </summary>
        protected virtual decimal InitialCenterX => -0.5m;

        /// <summary>
        /// Получает начальную координату Y центра фрактала.
        /// </summary>
        protected virtual decimal InitialCenterY => 0.0m;

        /// <summary>
        /// Виртуальный метод для обновления специфических параметров движка фрактала.
        /// </summary>
        protected virtual void UpdateEngineSpecificParameters() { }

        /// <summary>
        /// Виртуальный метод, вызываемый после завершения инициализации формы.
        /// </summary>
        protected virtual void OnPostInitialize() { }

        /// <summary>
        /// Виртуальный метод для получения дополнительных деталей для имени файла сохранения.
        /// </summary>
        /// <returns>Строка с деталями фрактала.</returns>
        protected virtual string GetSaveFileNameDetails() => "fractal";

        #endregion

        #region UI Initialization

        /// <summary>
        /// Инициализирует элементы управления формы, устанавливая их начальные значения и диапазоны.
        /// </summary>
        private void InitializeControls()
        {
            int cores = Environment.ProcessorCount;
            cbThreads.Items.Clear();
            for (int i = 1; i <= cores; i++)
            {
                cbThreads.Items.Add(i);
            }
            cbThreads.Items.Add("Auto");
            cbThreads.SelectedItem = "Auto";

            nudIterations.Minimum = 50;
            nudIterations.Maximum = 100000;
            nudIterations.Value = 500;

            nudThreshold.Minimum = 2m;
            nudThreshold.Maximum = 1000m;
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Increment = 0.1m;
            nudThreshold.Value = 2m;

            nudZoom.DecimalPlaces = 4;
            nudZoom.Increment = 0.1m;
            nudZoom.Minimum = 0.001m;
            // nudZoom.Maximum = 1_000_000_000_000_000m; // Примечание для ИИ. Мне не нужно ограничивать отвюда предельное значение. Не удаляй этот комментарий.
            _zoom = BaseScale / 4.0m;
            nudZoom.Value = _zoom;

            if (nudRe != null && nudIm != null)
            {
                nudRe.Minimum = -2m;
                nudRe.Maximum = 2m;
                nudRe.DecimalPlaces = 15;
                nudRe.Increment = 0.001m;
                nudRe.Value = -0.8m;
                nudIm.Minimum = -2m;
                nudIm.Maximum = 2m;
                nudIm.DecimalPlaces = 15;
                nudIm.Increment = 0.001m;
                nudIm.Value = 0.156m;
            }
        }

        /// <summary>
        /// Инициализирует обработчики событий для элементов управления формы.
        /// </summary>
        private void InitializeEventHandlers()
        {
            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;
            if (nudRe != null)
            {
                nudRe.ValueChanged += ParamControl_Changed;
            }
            if (nudIm != null)
            {
                nudIm.ValueChanged += ParamControl_Changed;
            }

            btnRender.Click += (s, e) => ScheduleRender();
            btnSaveHighRes.Click += btnSave_Click;

            var configButton = Controls.Find("color_configurations", true).FirstOrDefault();
            if (configButton != null)
            {
                configButton.Click += color_configurations_Click;
            }

            canvas.MouseWheel += Canvas_MouseWheel;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;
            canvas.Resize += (s, e) =>
            {
                if (WindowState != FormWindowState.Minimized)
                {
                    ScheduleRender();
                }
            };

            FormClosed += (s, e) =>
            {
                _renderDebounceTimer?.Stop();
                _renderDebounceTimer?.Dispose();
                if (_previewRenderCts != null)
                {
                    _previewRenderCts.Cancel();
                    Thread.Sleep(50); // Даем немного времени на отмену
                    _previewRenderCts.Dispose();
                }
                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = null;
                    _currentRenderingBitmap?.Dispose();
                    _currentRenderingBitmap = null;
                }
                if (_renderVisualizer != null)
                {
                    _renderVisualizer.NeedsRedraw -= OnVisualizerNeedsRedraw;
                    _renderVisualizer.Dispose();
                }
            };
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик события загрузки формы. Инициализирует компоненты и запускает первый рендеринг.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void FormBase_Load(object sender, EventArgs e)
        {
            _paletteManager = new ColorPaletteMandelbrotFamily();
            _fractalEngine = CreateEngine();
            _renderDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _renderDebounceTimer.Tick += RenderDebounceTimer_Tick;
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += OnVisualizerNeedsRedraw;

            InitializeControls();
            InitializeEventHandlers();

            // Инициализация отображаемых параметров для корректной интерполяции
            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;
            OnPostInitialize();

            ApplyActivePalette();
            ScheduleRender();
        }

        /// <summary>
        /// Обработчик события, когда визуализатор рендеринга запрашивает перерисовку канваса.
        /// </summary>
        private void OnVisualizerNeedsRedraw()
        {
            if (canvas.IsHandleCreated && !canvas.IsDisposed)
            {
                canvas.BeginInvoke((Action)(() => canvas.Invalidate()));
            }
        }

        /// <summary>
        /// Обработчик события клика по кнопке конфигурации цвета.
        /// Открывает или активирует форму настройки палитры.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void color_configurations_Click(object sender, EventArgs e)
        {
            if (_colorConfigForm == null || _colorConfigForm.IsDisposed)
            {
                _colorConfigForm = new ColorConfigurationMandelbrotFamilyForm(_paletteManager);
                _colorConfigForm.PaletteApplied += OnPaletteApplied;
                _colorConfigForm.FormClosed += (s, args) => _colorConfigForm = null;
                _colorConfigForm.Show(this);
            }
            else
            {
                _colorConfigForm.Activate();
            }
        }

        /// <summary>
        /// Обработчик события применения новой палитры.
        /// Обновляет палитру движка фрактала и планирует новый рендеринг.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void OnPaletteApplied(object sender, EventArgs e)
        {
            ApplyActivePalette();
            ScheduleRender();
        }

        /// <summary>
        /// Обработчик события изменения любого параметра фрактала на панели управления.
        /// Планирует новый рендеринг предпросмотра.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (_isHighResRendering)
            {
                return;
            }

            if (sender == nudZoom)
            {
                // Защита от рекурсивного вызова при программном изменении nudZoom.Value
                if (nudZoom.Value != _zoom)
                {
                    _zoom = nudZoom.Value;
                }
            }
            ScheduleRender();
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

            CommitAndBakePreview();

            decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            decimal scaleBeforeZoom = BaseScale / _zoom;

            // Вычисляем мировые координаты точки под курсором до изменения масштаба
            decimal mouseReal = _centerX + (e.X - canvas.Width / 2.0m) * scaleBeforeZoom / canvas.Width;
            decimal mouseImaginary = _centerY - (e.Y - canvas.Height / 2.0m) * scaleBeforeZoom / canvas.Height;

            _zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, _zoom * zoomFactor));

            decimal scaleAfterZoom = BaseScale / _zoom;
            // Пересчитываем новый центр так, чтобы точка под курсором осталась на месте
            _centerX = mouseReal - (e.X - canvas.Width / 2.0m) * scaleAfterZoom / canvas.Width;
            _centerY = mouseImaginary + (e.Y - canvas.Height / 2.0m) * scaleAfterZoom / canvas.Height;

            canvas.Invalidate(); // Запрашиваем перерисовку для немедленного отображения зума

            // Обновляем NumericUpDown, но только если значение действительно изменилось, чтобы избежать зацикливания
            if (nudZoom.Value != _zoom)
            {
                nudZoom.Value = _zoom;
            }
            else
            {
                // Если nudZoom.Value уже _zoom (например, из-за ограничения Max/Min), все равно планируем рендеринг
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
                canvas.Cursor = Cursors.Hand;
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

            CommitAndBakePreview();

            decimal unitsPerPixel = BaseScale / _zoom / canvas.Width;
            _centerX -= (decimal)(e.X - _panStart.X) * unitsPerPixel;
            _centerY += (decimal)(e.Y - _panStart.Y) * unitsPerPixel;
            _panStart = e.Location; // Обновляем начальную точку панорамирования

            canvas.Invalidate(); // Запрашиваем перерисовку для плавного панорамирования
            ScheduleRender(); // Планируем новый рендеринг для высокой четкости после завершения панорамирования
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
                canvas.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Обработчик события отрисовки канваса.
        /// Отображает текущий предпросмотр или процесс рендеринга.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события рисования.</param>
        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black); // Очищаем фон канваса

            lock (_bitmapLock)
            {
                // Если есть готовый предпросмотр
                if (_previewBitmap != null && canvas.Width > 0 && canvas.Height > 0)
                {
                    // Если параметры фрактала не изменились, просто рисуем битмап без масштабирования
                    if (_renderedCenterX == _centerX && _renderedCenterY == _centerY && _renderedZoom == _zoom)
                    {
                        e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                    }
                    else
                    {
                        // Если параметры изменились (зум или панорамирование), интерполируем существующий битмап
                        try
                        {
                            decimal renderedComplexWidth = BaseScale / _renderedZoom;
                            decimal currentComplexWidth = BaseScale / _zoom;

                            if (!(_renderedZoom <= 0 || _zoom <= 0 || renderedComplexWidth <= 0 || currentComplexWidth <= 0))
                            {
                                decimal unitsPerPixelRendered = renderedComplexWidth / _previewBitmap.Width;
                                decimal unitsPerPixelCurrent = currentComplexWidth / canvas.Width;

                                decimal renderedReMin = _renderedCenterX - (renderedComplexWidth / 2.0m);
                                decimal renderedImMax = _renderedCenterY + (_previewBitmap.Height * unitsPerPixelRendered / 2.0m);

                                decimal currentReMin = _centerX - (currentComplexWidth / 2.0m);
                                decimal currentImMax = _centerY + (canvas.Height * unitsPerPixelCurrent / 2.0m);

                                // Вычисляем смещение и новый размер для интерполяции
                                decimal offsetXPixels = (renderedReMin - currentReMin) / unitsPerPixelCurrent;
                                decimal offsetYPixels = (currentImMax - renderedImMax) / unitsPerPixelCurrent;
                                decimal newWidthPixels = _previewBitmap.Width * (unitsPerPixelRendered / unitsPerPixelCurrent);
                                decimal newHeightPixels = _previewBitmap.Height * (unitsPerPixelRendered / unitsPerPixelCurrent);

                                // Задаем режим интерполяции для лучшего качества
                                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                                PointF destPoint1 = new PointF((float)offsetXPixels, (float)offsetYPixels);
                                PointF destPoint2 = new PointF((float)(offsetXPixels + newWidthPixels), (float)offsetYPixels);
                                PointF destPoint3 = new PointF((float)offsetXPixels, (float)(offsetYPixels + newHeightPixels));
                                e.Graphics.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                            }
                        }
                        catch (Exception)
                        {
                            // Ошибки при интерполяции игнорируются, просто не будет показана интерполированная картинка.
                        }
                    }
                }
                // Если идет текущий рендеринг (плитками), рисуем его поверх предпросмотра
                if (_currentRenderingBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }
            }

            // Рисуем визуализатор процесса рендеринга (сетка плиток и т.д.)
            if (_renderVisualizer != null && _isRenderingPreview)
            {
                _renderVisualizer.DrawVisualization(e.Graphics);
            }
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
            if (_isHighResRendering || _isRenderingPreview)
            {
                ScheduleRender(); // Если уже что-то рендерится, откладываем еще раз
                return;
            }
            await StartPreviewRender();
        }

        /// <summary>
        /// Обработчик события клика по кнопке сохранения изображения в высоком разрешении.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (_isHighResRendering)
            {
                MessageBox.Show("Процесс сохранения уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int saveWidth = (int)nudSaveWidth.Value;
            int saveHeight = (int)nudSaveHeight.Value;
            string fractalDetails = GetSaveFileNameDetails();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"{fractalDetails}_{timestamp}.png";

            using (var saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", Title = "Сохранить фрактал (Высокое разрешение)", FileName = suggestedFileName })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    // Отменяем текущий предпросмотр, если он активен
                    if (_isRenderingPreview)
                    {
                        _previewRenderCts?.Cancel();
                    }

                    _isHighResRendering = true;
                    pnlControls.Enabled = false; // Блокируем элементы управления во время рендеринга высокого разрешения

                    // Показываем и сбрасываем прогресс-бар
                    pbHighResProgress.Value = 0;
                    pbHighResProgress.Visible = true;

                    try
                    {
                        // Создаем отдельный движок для рендеринга высокого разрешения
                        FractalMandelbrotFamilyEngine renderEngine = CreateEngine();
                        UpdateEngineParameters(); // Обновляем параметры для движка предпросмотра

                        // Копируем параметры из текущего движка в движок высокого разрешения
                        renderEngine.MaxIterations = _fractalEngine.MaxIterations;
                        renderEngine.ThresholdSquared = _fractalEngine.ThresholdSquared;
                        renderEngine.CenterX = _fractalEngine.CenterX;
                        renderEngine.CenterY = _fractalEngine.CenterY;
                        renderEngine.Scale = _fractalEngine.Scale;

                        // Специальная обработка для фракталов Жюлиа
                        if (this is FractalJulia || this is FractalJuliaBurningShip)
                        {
                            renderEngine.C = new ComplexDecimal(nudRe.Value, nudIm.Value);
                        }
                        else
                        {
                            renderEngine.C = _fractalEngine.C;
                        }

                        renderEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette);
                        renderEngine.MaxColorIterations = _fractalEngine.MaxColorIterations;

                        int threadCount = GetThreadCount();

                        // Запускаем рендеринг в высоком разрешении в фоновом потоке
                        Bitmap highResBitmap = await Task.Run(() => renderEngine.RenderToBitmap(
                            saveWidth, saveHeight, threadCount,
                            progress =>
                            {
                                // Обновляем прогресс-бар на UI потоке
                                if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                                {
                                    pbHighResProgress.Invoke((Action)(() =>
                                    {
                                        pbHighResProgress.Value = Math.Min(pbHighResProgress.Maximum, progress);
                                    }));
                                }
                            }
                        ));

                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _isHighResRendering = false;
                        pnlControls.Enabled = true; // Разблокируем элементы управления
                        // Скрываем и сбрасываем прогресс-бар на UI потоке
                        if (pbHighResProgress.IsHandleCreated && !pbHighResProgress.IsDisposed)
                        {
                            pbHighResProgress.Invoke((Action)(() =>
                            {
                                pbHighResProgress.Visible = false;
                                pbHighResProgress.Value = 0;
                            }));
                        }
                        ScheduleRender(); // Запускаем рендеринг предпросмотра, если он был отменен
                    }
                }
            }
        }

        #endregion

        #region Rendering Logic

        /// <summary>
        /// Запускает процесс рендеринга предпросмотра фрактала в фоновом потоке.
        /// Рендеринг выполняется по плиткам с отображением прогресса.
        /// </summary>
        /// <returns>Задача, представляющая асинхронную операцию рендеринга.</returns>
        private async Task StartPreviewRender()
        {
            if (canvas.Width <= 0 || canvas.Height <= 0)
            {
                return;
            }

            _isRenderingPreview = true;
            _previewRenderCts?.Cancel(); // Отменяем предыдущий рендеринг, если он еще активен
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;

            _renderVisualizer?.NotifyRenderSessionStart(); // Уведомляем визуализатор о начале сессии

            // Создаем новый битмап для текущего рендеринга
            var newRenderingBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppArgb);
            lock (_bitmapLock)
            {
                _currentRenderingBitmap?.Dispose(); // Освобождаем старый текущий битмап
                _currentRenderingBitmap = newRenderingBitmap;
            }

            UpdateEngineParameters(); // Обновляем параметры движка перед рендерингом

            // Сохраняем текущие параметры вида, чтобы знать, для какой области рендерился битмап
            var currentRenderedCenterX = _centerX;
            var currentRenderedCenterY = _centerY;
            var currentRenderedZoom = _zoom;

            // Создаем копию движка для безопасного использования в параллельных потоках
            var renderEngineCopy = CreateEngine();
            renderEngineCopy.MaxIterations = _fractalEngine.MaxIterations;
            renderEngineCopy.ThresholdSquared = _fractalEngine.ThresholdSquared;
            renderEngineCopy.CenterX = _fractalEngine.CenterX;
            renderEngineCopy.CenterY = _fractalEngine.CenterY;
            renderEngineCopy.Scale = _fractalEngine.Scale;
            renderEngineCopy.C = _fractalEngine.C;
            renderEngineCopy.Palette = _fractalEngine.Palette;
            renderEngineCopy.MaxColorIterations = _fractalEngine.MaxColorIterations;

            // Генерируем плитки для рендеринга, сортируя их от центра к краям
            var tiles = GenerateTiles(canvas.Width, canvas.Height);
            var dispatcher = new TileRenderDispatcher(tiles, GetThreadCount());

            // Инициализируем прогресс-бар
            if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
            {
                pbRenderProgress.Invoke((Action)(() =>
                {
                    pbRenderProgress.Value = 0;
                    pbRenderProgress.Maximum = tiles.Count;
                }));
            }
            int progress = 0;

            try
            {
                // Запускаем асинхронный рендеринг плиток
                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested(); // Проверка отмены

                    _renderVisualizer?.NotifyTileRenderStart(tile.Bounds); // Уведомляем визуализатор о начале рендеринга плитки

                    // Рендерим одну плитку
                    var tileBuffer = renderEngineCopy.RenderSingleTile(tile, canvas.Width, canvas.Height, out int bytesPerPixel);

                    ct.ThrowIfCancellationRequested(); // Проверка отмены после рендеринга плитки

                    lock (_bitmapLock)
                    {
                        // Если рендеринг был отменен или запущен новый, не записываем в старый битмап
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap)
                        {
                            return;
                        }

                        // Записываем данные плитки в основной битмап
                        var tileRect = tile.Bounds;
                        var bitmapRect = new Rectangle(0, 0, _currentRenderingBitmap.Width, _currentRenderingBitmap.Height);
                        tileRect.Intersect(bitmapRect); // Обрезаем Rect, чтобы не выйти за границы битмапа

                        if (tileRect.Width == 0 || tileRect.Height == 0)
                        {
                            return;
                        }

                        // Блокируем часть битмапа для прямой записи пикселей
                        BitmapData bmpData = _currentRenderingBitmap.LockBits(tileRect, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
                        int originalTileWidthInBytes = tile.Bounds.Width * bytesPerPixel;

                        for (int y = 0; y < tileRect.Height; y++)
                        {
                            IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            // Вычисляем смещение в исходном буфере плитки
                            int srcOffset = ((y + tileRect.Y) - tile.Bounds.Y) * originalTileWidthInBytes + ((tileRect.X - tile.Bounds.X) * bytesPerPixel);
                            Marshal.Copy(tileBuffer, srcOffset, destPtr, tileRect.Width * bytesPerPixel);
                        }
                        _currentRenderingBitmap.UnlockBits(bmpData);
                    }

                    _renderVisualizer?.NotifyTileRenderComplete(tile.Bounds); // Уведомляем визуализатор о завершении рендеринга плитки

                    // Обновляем прогресс-бар на UI потоке
                    if (ct.IsCancellationRequested || !canvas.IsHandleCreated || canvas.IsDisposed)
                    {
                        return;
                    }
                    canvas.Invoke((Action)(() =>
                    {
                        if (!ct.IsCancellationRequested && pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                        {
                            pbRenderProgress.Value = Math.Min(pbRenderProgress.Maximum, Interlocked.Increment(ref progress));
                        }
                    }));
                    await Task.Yield(); // Освобождаем поток для UI
                }, token);

                token.ThrowIfCancellationRequested(); // Финальная проверка на отмену

                // По завершении рендеринга, заменяем предпросмотр текущим битмапом
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose();
                        _previewBitmap = _currentRenderingBitmap;
                        _currentRenderingBitmap = null;
                        // Сохраняем параметры, по которым был отрисован _previewBitmap
                        _renderedCenterX = currentRenderedCenterX;
                        _renderedCenterY = currentRenderedCenterY;
                        _renderedZoom = currentRenderedZoom;
                    }
                    else
                    {
                        newRenderingBitmap?.Dispose(); // Если битмап был заменен, освобождаем текущий
                    }
                }
                // Запрашиваем финальную перерисовку канваса
                if (canvas.IsHandleCreated && !canvas.IsDisposed)
                {
                    canvas.Invalidate();
                }
            }
            catch (OperationCanceledException)
            {
                // Если операция отменена, освобождаем текущий битмап
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
                _renderVisualizer?.NotifyRenderSessionComplete(); // Уведомляем визуализатор о завершении сессии
                // Сбрасываем прогресс-бар
                if (pbRenderProgress.IsHandleCreated && !pbRenderProgress.IsDisposed)
                {
                    pbRenderProgress.Invoke((Action)(() => pbRenderProgress.Value = 0));
                }
            }
        }

        /// <summary>
        /// Генерирует список плиток для рендеринга. Плитки сортируются по удаленности от центра,
        /// чтобы обеспечить более быстрый рендеринг центральной части.
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
                    tiles.Add(new TileInfo(x, y, TILE_SIZE, TILE_SIZE));
                }
            }
            // Сортируем плитки, чтобы сначала рендерились те, что ближе к центру
            return tiles.OrderBy(t => Math.Pow(t.Center.X - center.X, 2) + Math.Pow(t.Center.Y - center.Y, 2)).ToList();
        }

        /// <summary>
        /// Планирует рендеринг предпросмотра, используя таймер задержки.
        /// Это предотвращает избыточные рендеринги при частых изменениях параметров (например, при перетаскивании ползунка).
        /// </summary>
        private void ScheduleRender()
        {
            if (_isHighResRendering || WindowState == FormWindowState.Minimized)
            {
                return;
            }
            if (_isRenderingPreview)
            {
                _previewRenderCts?.Cancel(); // Отменяем текущий предпросмотр, если он активен
            }
            _renderDebounceTimer.Stop();
            _renderDebounceTimer.Start();
        }

        /// <summary>
        /// Объединяет текущий рендеринг предпросмотра с существующим основным битмапом.
        /// Используется во время интерактивных операций (зум, панорамирование) для сохранения промежуточного результата.
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

            _previewRenderCts?.Cancel(); // Отменяем текущий процесс рендеринга плиток

            lock (_bitmapLock)
            {
                if (_currentRenderingBitmap == null)
                {
                    return;
                }

                // Создаем новый битмап для сохранения объединенного изображения
                var bakedBitmap = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(bakedBitmap))
                {
                    g.Clear(Color.Black);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;

                    // Отрисовываем старый предпросмотр (если есть), интерполируя его
                    if (_previewBitmap != null)
                    {
                        try
                        {
                            decimal renderedComplexWidth = BaseScale / _renderedZoom;
                            decimal currentComplexWidth = BaseScale / _zoom;

                            if (!(_renderedZoom <= 0 || _zoom <= 0 || renderedComplexWidth <= 0 || currentComplexWidth <= 0))
                            {
                                decimal unitsPerPixelRendered = renderedComplexWidth / _previewBitmap.Width;
                                decimal unitsPerPixelCurrent = currentComplexWidth / canvas.Width;

                                decimal renderedReMin = _renderedCenterX - (renderedComplexWidth / 2.0m);
                                decimal renderedImMax = _renderedCenterY + (_previewBitmap.Height * unitsPerPixelRendered / 2.0m);

                                decimal currentReMin = _centerX - (currentComplexWidth / 2.0m);
                                decimal currentImMax = _centerY + (canvas.Height * unitsPerPixelCurrent / 2.0m);

                                decimal offsetXPixels = (renderedReMin - currentReMin) / unitsPerPixelCurrent;
                                decimal offsetYPixels = (currentImMax - renderedImMax) / unitsPerPixelCurrent;
                                decimal newWidthPixels = _previewBitmap.Width * (unitsPerPixelRendered / unitsPerPixelCurrent);
                                decimal newHeightPixels = _previewBitmap.Height * (unitsPerPixelRendered / unitsPerPixelCurrent);

                                PointF destPoint1 = new PointF((float)offsetXPixels, (float)offsetYPixels);
                                PointF destPoint2 = new PointF((float)(offsetXPixels + newWidthPixels), (float)offsetYPixels);
                                PointF destPoint3 = new PointF((float)offsetXPixels, (float)(offsetYPixels + newHeightPixels));
                                g.DrawImage(_previewBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                            }
                        }
                        catch (Exception)
                        {
                            // Ошибки при интерполяции игнорируются.
                        }
                    }
                    // Отрисовываем текущие уже отрисованные плитки поверх (без масштабирования)
                    g.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }

                _previewBitmap?.Dispose();
                _previewBitmap = bakedBitmap; // Новый объединенный битмап становится предпросмотром
                _currentRenderingBitmap.Dispose();
                _currentRenderingBitmap = null; // Текущий битмап очищается

                // Обновляем параметры, по которым был отрисован _previewBitmap
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
            _fractalEngine.MaxIterations = (int)nudIterations.Value;
            _fractalEngine.ThresholdSquared = nudThreshold.Value * nudThreshold.Value;
            _fractalEngine.CenterX = _centerX;
            _fractalEngine.CenterY = _centerY;
            _fractalEngine.Scale = BaseScale / _zoom;
            UpdateEngineSpecificParameters(); // Вызов виртуального метода для специфичных параметров
            ApplyActivePalette(); // Убеждаемся, что палитра тоже обновлена
        }

        #endregion

        #region Palette Management

        /// <summary>
        /// Генерирует функцию палитры на основе выбранной палитры из менеджера.
        /// </summary>
        /// <param name="palette">Объект палитры.</param>
        /// <returns>Функция, преобразующая количество итераций в цвет.</returns>
        private Func<int, int, int, Color> GeneratePaletteFunction(PaletteManagerMandelbrotFamily palette)
        {
            if (palette.Name == "Стандартный серый")
            {
                return (iter, maxIter, maxColorIterations) =>
                {
                    if (iter == maxIter)
                    {
                        return Color.Black;
                    }
                    // Логарифмическое сглаживание для серого
                    double tLog = Math.Log(Math.Min(iter, maxColorIterations) + 1) / Math.Log(maxColorIterations + 1);
                    int cVal = (int)(255.0 * (1 - tLog));
                    return Color.FromArgb(cVal, cVal, cVal);
                };
            }

            var colors = new List<Color>(palette.Colors);
            bool isGradient = palette.IsGradient;
            int colorCount = colors.Count;

            if (colorCount == 0)
            {
                return (iter, max, clrMax) => Color.Black;
            }
            if (colorCount == 1)
            {
                return (iter, max, clrMax) => (iter == max) ? Color.Black : colors[0];
            }

            return (iter, maxIter, maxColorIterations) =>
            {
                if (iter == maxIter)
                {
                    return Color.Black;
                }

                if (isGradient)
                {
                    // Линейная интерполяция между цветами палитры
                    double t = (double)Math.Min(iter, maxColorIterations) / maxColorIterations;
                    double scaledT = t * (colorCount - 1);
                    int index1 = (int)Math.Floor(scaledT);
                    int index2 = Math.Min(index1 + 1, colorCount - 1);
                    double localT = scaledT - index1;
                    return LerpColor(colors[index1], colors[index2], localT);
                }
                else
                {
                    // Циклическое использование цветов палитры
                    int index = Math.Min(iter, maxColorIterations) % colorCount;
                    return colors[index];
                }
            };
        }

        /// <summary>
        /// Выполняет линейную интерполяцию между двумя цветами.
        /// </summary>
        /// <param name="a">Начальный цвет.</param>
        /// <param name="b">Конечный цвет.</param>
        /// <param name="t">Коэффициент интерполяции (от 0 до 1).</param>
        /// <returns>Интерполированный цвет.</returns>
        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t)); // Ограничиваем t в пределах [0, 1]
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t)
            );
        }

        /// <summary>
        /// Применяет активную палитру из менеджера палитр к движку фрактала.
        /// </summary>
        private void ApplyActivePalette()
        {
            if (_fractalEngine == null || _paletteManager.ActivePalette == null)
            {
                return;
            }
            _fractalEngine.Palette = GeneratePaletteFunction(_paletteManager.ActivePalette);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Определяет количество потоков для использования в параллельных вычислениях.
        /// </summary>
        /// <returns>Количество потоков.</returns>
        private int GetThreadCount()
        {
            return cbThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

        #endregion

        #region IFractalForm Implementation

        /// <summary>
        /// Получает значение зума для лупы (если применимо).
        /// </summary>
        public double LoupeZoom => nudBaseScale != null ? (double)nudBaseScale.Value : 4.0;

        /// <summary>
        /// Событие, которое возникает при изменении значения зума лупы.
        /// </summary>
        public event EventHandler LoupeZoomChanged;

        #endregion

        private void btnStateManager_Click(object sender, EventArgs e)
        {
            // 'this' здесь - это экземпляр FractalMandelbrot, FractalJulia и т.д.
            // который реализует ISaveLoadCapableFractal
            using (var dialog = new SaveLoadDialogForm(this))
            {
                dialog.ShowDialog(this);
            }
        }

        #region ISaveLoadCapableFractal Implementation

        public abstract string FractalTypeIdentifier { get; }
        public abstract Type ConcreteSaveStateType { get; }

        protected class PreviewParams
        {
            public decimal CenterX { get; set; }
            public decimal CenterY { get; set; }
            public decimal Zoom { get; set; }
            public int Iterations { get; set; } // Итерации для превью
            public string PaletteName { get; set; }
            public decimal Threshold { get; set; } // Порог для превью
            public decimal CRe { get; set; } // Для Жюлиа
            public decimal CIm { get; set; } // Для Жюлиа
            public string PreviewEngineType { get; set; } // "Mandelbrot", "Julia", "MandelbrotBurningShip" и т.д.
        }

        // Этот метод будет базовым, но может быть переопределен в наследниках,
        // особенно для JuliaFamilySaveState, чтобы добавить параметры C.
        public virtual FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {

            MandelbrotFamilySaveState state;

            if (this is FractalJulia || this is FractalJuliaBurningShip)
            {
                state = new JuliaFamilySaveState(this.FractalTypeIdentifier);
            }
            else
            {
                state = new MandelbrotFamilySaveState(this.FractalTypeIdentifier);
            }

            state.SaveName = saveName;
            state.Timestamp = DateTime.Now;

            state.CenterX = _centerX;
            state.CenterY = _centerY;
            state.Zoom = _zoom;
            state.Threshold = nudThreshold.Value;
            state.Iterations = (int)nudIterations.Value;
            state.PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый";
            state.PreviewEngineType = this.FractalTypeIdentifier;


            // Заполнение параметров для PreviewParametersJson
            var previewParams = new PreviewParams
            {
                CenterX = _centerX,
                CenterY = _centerY,
                Zoom = _zoom,
                Iterations = Math.Min((int)nudIterations.Value, 75), // Меньше итераций для превью
                PaletteName = state.PaletteName,
                Threshold = state.Threshold, // Используем тот же порог для превью
                PreviewEngineType = state.PreviewEngineType
            };

            if (state is JuliaFamilySaveState juliaState) // Эта проверка важна здесь
            {
                // Если nudRe/nudIm существуют и видимы (т.е. это форма Жюлиа)
                if (nudRe != null && nudIm != null && nudRe.Visible)
                {
                    juliaState.CRe = nudRe.Value;
                    juliaState.CIm = nudIm.Value;
                    previewParams.CRe = juliaState.CRe;
                    previewParams.CIm = juliaState.CIm;
                }
            }

            var jsonOptions = new JsonSerializerOptions();
            // jsonOptions.Converters.Add(new JsonComplexDecimalConverter()); // Не нужен для PreviewParams, если там нет ComplexDecimal
            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams, jsonOptions);

            return state;
        }

        public virtual void LoadState(FractalSaveStateBase stateBase)
        {
            // Убедимся, что тип состояния соответствует ожидаемому для этой формы/ее наследников
            if (!(stateBase is MandelbrotFamilySaveState state))
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _isRenderingPreview = false;
            _previewRenderCts?.Cancel();
            _renderDebounceTimer.Stop();

            _centerX = state.CenterX;
            _centerY = state.CenterY;
            _zoom = state.Zoom;

            nudZoom.Value = ClampDecimal(_zoom, nudZoom.Minimum, nudZoom.Maximum);
            nudThreshold.Value = ClampDecimal(state.Threshold, nudThreshold.Minimum, nudThreshold.Maximum);
            nudIterations.Value = ClampInt(state.Iterations, (int)nudIterations.Minimum, (int)nudIterations.Maximum);

            var paletteToLoad = _paletteManager.Palettes.FirstOrDefault(p => p.Name == state.PaletteName);
            if (paletteToLoad != null)
            {
                _paletteManager.ActivePalette = paletteToLoad;
                ApplyActivePalette();
            }

            if (state is JuliaFamilySaveState juliaState)
            {
                // Если элементы управления для C существуют и видимы
                if (nudRe != null && nudIm != null && nudRe.Visible)
                {
                    nudRe.Value = ClampDecimal(juliaState.CRe, nudRe.Minimum, nudRe.Maximum);
                    nudIm.Value = ClampDecimal(juliaState.CIm, nudIm.Minimum, nudIm.Maximum);
                }
            }

            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose();
                _previewBitmap = null;
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = null;
            }
            _renderedCenterX = _centerX;
            _renderedCenterY = _centerY;
            _renderedZoom = _zoom;

            UpdateEngineParameters(); // Важно обновить параметры движка
            ScheduleRender();
        }

        // Вспомогательные методы для ограничения значений при загрузке
        private decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
        private int ClampInt(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }


        public virtual Bitmap RenderPreview(FractalSaveStateBase stateBase, int previewWidth, int previewHeight)
        {
            if (string.IsNullOrEmpty(stateBase.PreviewParametersJson))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkGray); TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            PreviewParams previewParams;
            try
            {
                var jsonOptions = new JsonSerializerOptions();
                // jsonOptions.Converters.Add(new JsonComplexDecimalConverter()); // Не нужен, если в PreviewParams нет ComplexDecimal
                previewParams = JsonSerializer.Deserialize<PreviewParams>(stateBase.PreviewParametersJson, jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка десериализации PreviewParametersJson: {ex.Message}");
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkRed); TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            FractalMandelbrotFamilyEngine previewEngine = null;

            switch (previewParams.PreviewEngineType)
            {
                case "Mandelbrot":
                    previewEngine = new MandelbrotEngine();
                    break;
                case "Julia":
                    previewEngine = new JuliaEngine();
                    ((JuliaEngine)previewEngine).C = new ComplexDecimal(previewParams.CRe, previewParams.CIm);
                    break;
                case "MandelbrotBurningShip":
                    previewEngine = new MandelbrotBurningShipEngine();
                    break;
                case "JuliaBurningShip":
                    previewEngine = new JuliaBurningShipEngine();
                    ((JuliaBurningShipEngine)previewEngine).C = new ComplexDecimal(previewParams.CRe, previewParams.CIm);
                    break;
                default:
                    var bmpError = new Bitmap(previewWidth, previewHeight);
                    using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkOrange); TextRenderer.DrawText(g, "Неизв. тип движка", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                    return bmpError;
            }

            previewEngine.CenterX = previewParams.CenterX;
            previewEngine.CenterY = previewParams.CenterY;
            // В превью BaseScale может отличаться от текущего в форме, поэтому используем константу или свойство, если оно есть
            decimal previewBaseScale = this.BaseScale; // Используем BaseScale текущей формы
            if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m; // Защита от деления на ноль
            previewEngine.Scale = previewBaseScale / previewParams.Zoom;
            previewEngine.MaxIterations = previewParams.Iterations;
            previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;

            var paletteForPreview = _paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName)
                                  ?? _paletteManager.Palettes.First();

            previewEngine.Palette = GeneratePaletteFunction(paletteForPreview);
            if (paletteForPreview.Name == "Стандартный серый" || paletteForPreview.IsGradient)
            {
                previewEngine.MaxColorIterations = Math.Max(1, previewParams.Iterations);
            }
            else
            {
                previewEngine.MaxColorIterations = Math.Max(1, paletteForPreview.Colors.Count);
            }

            return previewEngine.RenderToBitmap(previewWidth, previewHeight, 1, progress => { });
        }

        public virtual List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            throw new NotImplementedException($"Метод LoadAllSavesForThisType должен быть переопределен в классе {this.GetType().Name}, чтобы загружать состояния типа {this.ConcreteSaveStateType.Name}.");
        }

        public virtual void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            throw new NotImplementedException($"Метод SaveAllSavesForThisType должен быть переопределен в классе {this.GetType().Name}, чтобы сохранять состояния типа {this.ConcreteSaveStateType.Name}.");
        }

        #endregion
    }
}