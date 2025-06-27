using FractalExplorer.Engines;
using FractalExplorer.Resources;
using FractalExplorer.SelectorsForms;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Diagnostics; // Добавлено для Stopwatch
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace FractalExplorer
{
    /// <summary>
    /// Представляет основную форму для отображения и взаимодействия с фракталом Серпинского.
    /// Реализует интерфейс <see cref="ISaveLoadCapableFractal"/> для сохранения и загрузки состояний фрактала.
    /// </summary>
    public partial class FractalSerpinsky : Form, ISaveLoadCapableFractal
    {
        #region Fields
        private readonly FractalSerpinskyEngine _engine;
        private Bitmap canvasBitmap;
        private volatile bool isRenderingPreview = false;
        private volatile bool isHighResRendering = false;
        private CancellationTokenSource previewRenderCts;
        private CancellationTokenSource highResRenderCts;
        private System.Windows.Forms.Timer renderTimer;
        private double currentZoom = 1.0;
        private double centerX = 0.0;
        private double centerY = 0.0;
        private double renderedZoom = 1.0;
        private double renderedCenterX = 0.0;
        private double renderedCenterY = 0.0;
        private Point panStart;
        private bool panning = false;

        private string _baseTitle; // Поле для хранения базового заголовка окна
        private SerpinskyPaletteManager _paletteManager;
        private ColorConfigurationSerpinskyForm _colorConfigForm;
        #endregion

        #region Constructor
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FractalSerpinsky"/>.
        /// Создает экземпляр движка фрактала и менеджера палитр.
        /// </summary>
        public FractalSerpinsky()
        {
            InitializeComponent();
            _engine = new FractalSerpinskyEngine();
            _paletteManager = new SerpinskyPaletteManager();
            InitializeCustomComponents();
        }
        #endregion

        #region UI Initialization
        /// <summary>
        /// Инициализирует пользовательские компоненты и подписывается на события UI.
        /// Настраивает таймер рендеринга, элементы управления потоками ЦП и обработчики событий мыши/UI.
        /// </summary>
        private void InitializeCustomComponents()
        {
            renderTimer = new System.Windows.Forms.Timer { Interval = 300 };
            renderTimer.Tick += RenderTimer_Tick;

            // Заполняем выпадающий список количеством потоков ЦП, включая опцию "Авто".
            int cores = Environment.ProcessorCount;
            cbCPUThreads.Items.Clear();
            for (int i = 1; i <= cores; i++)
            {
                cbCPUThreads.Items.Add(i);
            }
            cbCPUThreads.Items.Add("Auto");
            cbCPUThreads.SelectedItem = "Auto";

            // Подписываемся на события формы и элементов управления.
            Load += (s, e) =>
            {
                _baseTitle = this.Text; // Сохраняем исходный заголовок
                renderedCenterX = centerX;
                renderedCenterY = centerY;
                renderedZoom = currentZoom;
                ApplyActivePalette();
                ScheduleRender();
            };
            canvasSerpinsky.Paint += CanvasSerpinsky_Paint;
            canvasSerpinsky.MouseWheel += CanvasSerpinsky_MouseWheel;
            canvasSerpinsky.MouseDown += CanvasSerpinsky_MouseDown;
            canvasSerpinsky.MouseMove += CanvasSerpinsky_MouseMove;
            canvasSerpinsky.MouseUp += CanvasSerpinsky_MouseUp;
            Resize += (s, e) => { if (WindowState != FormWindowState.Minimized) ScheduleRender(); };
            canvasSerpinsky.Resize += (s, e) => { if (WindowState != FormWindowState.Minimized) ScheduleRender(); };

            nudZoom.ValueChanged += ParamControl_Changed;
            nudIterations.ValueChanged += ParamControl_Changed;
            cbCPUThreads.SelectedIndexChanged += ParamControl_Changed;
            FractalTypeIsGeometry.CheckedChanged += FractalType_CheckedChanged;
            FractalTypeIsChaos.CheckedChanged += FractalType_CheckedChanged;

            FractalTypeIsGeometry.Checked = true;
            UpdateAbortButtonState();
        }
        #endregion

        #region UI Event Handlers
        /// <summary>
        /// Обрабатывает изменение выбранного типа фрактала (геометрический или хаотический).
        /// Обновляет ограничения для количества итераций в зависимости от выбранного типа.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void FractalType_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCheckBox = sender as CheckBox;
            // Проверяем, что событие вызвано активным чекбоксом, чтобы избежать двойного вызова или обработки неактивного.
            if (activeCheckBox == null || !activeCheckBox.Checked)
            {
                return;
            }

            if (activeCheckBox == FractalTypeIsGeometry)
            {
                FractalTypeIsChaos.Checked = false;
                nudIterations.Maximum = 20;
                nudIterations.Minimum = 0;
                // Устанавливаем разумное значение по умолчанию, если текущее выходит за новые границы.
                if (nudIterations.Value >= 20 || nudIterations.Value < 0)
                {
                    nudIterations.Value = 8;
                }
            }
            else // FractalTypeIsChaos
            {
                FractalTypeIsGeometry.Checked = false;
                nudIterations.Maximum = int.MaxValue;
                nudIterations.Minimum = 1000;
                // Устанавливаем разумное значение по умолчанию, если текущее выходит за новые границы.
                if (nudIterations.Value < 1000)
                {
                    nudIterations.Value = 50000;
                }
            }
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Настройки цвета".
        /// Открывает или активирует форму настроек цвета для фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void color_configurations_Click(object sender, EventArgs e)
        {
            // Создаем новую форму, если она еще не существует или была закрыта.
            if (_colorConfigForm == null || _colorConfigForm.IsDisposed)
            {
                _colorConfigForm = new ColorConfigurationSerpinskyForm(_paletteManager);
                _colorConfigForm.PaletteApplied += OnPaletteApplied;
                // Обнуляем ссылку на форму при ее закрытии, чтобы можно было создать новую.
                _colorConfigForm.FormClosed += (s, args) => _colorConfigForm = null;
                _colorConfigForm.Show(this);
            }
            else
            {
                // Если форма уже открыта, просто активируем ее.
                _colorConfigForm.Activate();
            }
        }

        /// <summary>
        /// Обрабатывает событие применения новой палитры цветов.
        /// Применяет выбранную палитру и перерисовывает фрактал.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void OnPaletteApplied(object sender, EventArgs e)
        {
            ApplyActivePalette();
            ScheduleRender();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Рендеринг".
        /// Отменяет текущий предпросмотр и немедленно запускает новый рендеринг.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void btnRender_Click(object sender, EventArgs e)
        {
            previewRenderCts?.Cancel();
            renderTimer.Stop();
            // Вызываем обработчик таймера напрямую, чтобы начать рендеринг немедленно.
            RenderTimer_Tick(sender, e);
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Менеджер состояний".
        /// Открывает диалоговое окно для сохранения и загрузки состояний фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void btnStateManager_Click(object sender, EventArgs e)
        {
            using (var dialog = new Forms.SaveLoadDialogForm(this))
            {
                dialog.ShowDialog(this);
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Отмена рендеринга".
        /// Отменяет любой текущий рендеринг (предпросмотр или высококачественный).
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void abortRender_Click(object sender, EventArgs e)
        {
            if (isRenderingPreview)
            {
                previewRenderCts?.Cancel();
            }
            if (isHighResRendering)
            {
                highResRenderCts?.Cancel();
            }
        }
        #endregion

        #region Rendering Logic
        /// <summary>
        /// Обрабатывает изменение значений элементов управления, влияющих на параметры фрактала.
        /// Если происходит изменение масштаба, обновляет внутреннее значение масштаба.
        /// Запускает планирование нового рендеринга.
        /// </summary>
        /// <param name="sender">Источник события (например, NumericUpDown).</param>
        /// <param name="e">Данные события.</param>
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            // Игнорируем изменения, если в данный момент идет высококачественный рендеринг,
            // чтобы избежать конфликтов и нестабильности.
            if (isHighResRendering)
            {
                return;
            }
            if (sender == nudZoom)
            {
                currentZoom = (double)nudZoom.Value;
            }
            ScheduleRender();
        }

        /// <summary>
        /// Планирует рендеринг фрактала.
        /// Отменяет любой текущий рендеринг предпросмотра и запускает таймер для задержки рендеринга,
        /// чтобы избежать чрезмерной перерисовки при быстрых изменениях параметров.
        /// </summary>
        private void ScheduleRender()
        {
            // Не планируем рендеринг, если идет высококачественный рендеринг или форма свернута.
            if (isHighResRendering || WindowState == FormWindowState.Minimized)
            {
                return;
            }
            previewRenderCts?.Cancel();
            renderTimer.Stop();
            renderTimer.Start();
        }

        /// <summary>
        /// Обрабатывает событие тика таймера рендеринга, инициируя процесс рендеринга предпросмотра.
        /// Выполняет рендеринг в фоновом потоке, обновляет индикатор прогресса и отображает результат.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private async void RenderTimer_Tick(object sender, EventArgs e)
        {
            renderTimer.Stop();
            // Предотвращаем запуск нового рендеринга, если какой-либо рендеринг уже активен.
            if (isHighResRendering || isRenderingPreview)
            {
                return;
            }

            var stopwatch = Stopwatch.StartNew(); // Запуск секундомера

            isRenderingPreview = true;
            SetMainControlsEnabled(false);
            UpdateAbortButtonState();

            previewRenderCts?.Dispose();
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;

            UpdateEngineParameters();
            int numThreads = GetThreadCount();
            int renderWidth = canvasSerpinsky.Width;
            int renderHeight = canvasSerpinsky.Height;

            // Не рендерим, если размеры области для рисования некорректны.
            if (renderWidth <= 0 || renderHeight <= 0)
            {
                isRenderingPreview = false;
                SetMainControlsEnabled(true);
                UpdateAbortButtonState();
                return;
            }

            try
            {
                var bitmap = new Bitmap(renderWidth, renderHeight, PixelFormat.Format32bppArgb);
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                var pixelBuffer = new byte[bitmapData.Stride * renderHeight];

                // Асинхронно запускаем рендеринг в фоновом потоке, чтобы не блокировать UI.
                await Task.Run(() => _engine.RenderToBuffer(
                    pixelBuffer, renderWidth, renderHeight, bitmapData.Stride, 4,
                    numThreads, token, (progress) => UpdateProgressBar(progressBarSerpinsky, progress)), token);

                // Проверяем, был ли запрос на отмену во время рендеринга.
                token.ThrowIfCancellationRequested();
                Marshal.Copy(pixelBuffer, 0, bitmapData.Scan0, pixelBuffer.Length);
                bitmap.UnlockBits(bitmapData);

                // Обновляем текущий битмап и сохраняем параметры, при которых он был отрендерен,
                // чтобы корректно масштабировать и позиционировать его при перерисовке.
                Bitmap oldImage = canvasBitmap;
                canvasBitmap = bitmap;
                renderedZoom = currentZoom;
                renderedCenterX = centerX;
                renderedCenterY = centerY;
                canvasSerpinsky.Invalidate(); // Запрашиваем перерисовку элемента управления.
                oldImage?.Dispose(); // Освобождаем ресурсы старого битмапа.

                stopwatch.Stop(); // Остановка секундомера после успешного рендеринга
                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                this.Text = $"{_baseTitle} - Время последнего рендера: {elapsedSeconds:F3} сек."; // Обновление заголовка
            }
            catch (OperationCanceledException)
            {
                // Игнорируем исключение отмены, так как это ожидаемое поведение при отмене рендеринга.
                // Не обновляем заголовок, если рендеринг был отменен.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isRenderingPreview = false;
                // Включаем основные элементы управления, только если нет активного высококачественного рендеринга.
                if (!isHighResRendering)
                {
                    SetMainControlsEnabled(true);
                }
                UpdateAbortButtonState();
                UpdateProgressBar(progressBarSerpinsky, 0);
            }
        }
        #endregion

        #region Canvas Interaction
        /// <summary>
        /// Обрабатывает событие рисования на канвасе фрактала.
        /// Очищает фон и отрисовывает отрендеренный битмап, масштабируя его относительно текущего положения и масштаба.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события рисования.</param>
        private void CanvasSerpinsky_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(_engine.BackgroundColor);
            // Если битмап еще не отрендерен или размеры канваса некорректны, ничего не отрисовываем.
            if (canvasBitmap == null || canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0)
            {
                return;
            }

            // Вычисляем мировые координаты и размеры области, которая была отрендерена в canvasBitmap.
            double renderedAspectRatio = (double)canvasBitmap.Width / canvasBitmap.Height;
            double renderedViewHeightWorld = 1.0 / renderedZoom;
            double renderedViewWidthWorld = renderedViewHeightWorld * renderedAspectRatio;
            double renderedMinReal = renderedCenterX - renderedViewWidthWorld / 2.0;
            double renderedMaxImaginary = renderedCenterY + renderedViewHeightWorld / 2.0;

            // Вычисляем мировые координаты и размеры области, которую нужно отобразить сейчас (основываясь на текущем зуме и центре).
            double currentAspectRatio = (double)canvasSerpinsky.Width / canvasSerpinsky.Height;
            double currentViewHeightWorld = 1.0 / currentZoom;
            double currentViewWidthWorld = currentViewHeightWorld * currentAspectRatio;
            double currentMinReal = centerX - currentViewWidthWorld / 2.0;
            double currentMaxImaginary = centerY + currentViewHeightWorld / 2.0;

            // Вычисляем смещение и масштабирование для отрисовки отрендеренного битмапа
            // таким образом, чтобы он соответствовал текущему масштабу и положению.
            float offsetX = (float)(((renderedMinReal - currentMinReal) / currentViewWidthWorld) * canvasSerpinsky.Width);
            float offsetY = (float)(((currentMaxImaginary - renderedMaxImaginary) / currentViewHeightWorld) * canvasSerpinsky.Height);
            float scaleWidth = (float)((renderedViewWidthWorld / currentViewWidthWorld) * canvasSerpinsky.Width);
            float scaleHeight = (float)((renderedViewHeightWorld / currentViewHeightWorld) * canvasSerpinsky.Height);

            // Используем режим интерполяции NearestNeighbor для быстрого и простого масштабирования пикселей,
            // что подходит для фракталов и предпросмотров.
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(canvasBitmap, new RectangleF(offsetX, offsetY, scaleWidth, scaleHeight));
        }

        /// <summary>
        /// Обрабатывает событие прокрутки колеса мыши для масштабирования фрактала.
        /// Изменяет текущий масштаб и центр фрактала, чтобы масштабирование происходило относительно точки под курсором.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
        private void CanvasSerpinsky_MouseWheel(object sender, MouseEventArgs e)
        {
            // Игнорируем событие, если идет высококачественный рендеринг или канвас имеет нулевую ширину.
            if (isHighResRendering || canvasSerpinsky.Width <= 0)
            {
                return;
            }
            double zoomFactor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
            PointF worldPosition = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            currentZoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, currentZoom * zoomFactor));
            PointF newWorldPosition = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            // Корректируем центр, чтобы точка под курсором оставалась на месте после масштабирования.
            centerX += worldPosition.X - newWorldPosition.X;
            centerY += worldPosition.Y - newWorldPosition.Y;
            canvasSerpinsky.Invalidate(); // Запрашиваем перерисовку для визуального обновления.
            // Обновляем значение NumericUpDown, если оно отличается, или планируем рендеринг, если нет.
            if (nudZoom.Value != (decimal)currentZoom)
            {
                nudZoom.Value = (decimal)currentZoom;
            }
            else
            {
                ScheduleRender();
            }
        }

        /// <summary>
        /// Обрабатывает событие нажатия кнопки мыши на канвасе для начала панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
        private void CanvasSerpinsky_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panning = true;
                panStart = e.Location;
            }
        }

        /// <summary>
        /// Обрабатывает событие перемещения мыши по канвасу для выполнения панорамирования.
        /// Изменяет центр фрактала в соответствии с перемещением мыши.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
        private void CanvasSerpinsky_MouseMove(object sender, MouseEventArgs e)
        {
            if (!panning)
            {
                return;
            }
            // Преобразуем начальную и текущую позицию курсора в мировые координаты,
            // чтобы корректно рассчитать смещение центра.
            PointF worldBefore = ScreenToWorld(panStart, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            PointF worldAfter = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            centerX += worldBefore.X - worldAfter.X;
            centerY += worldBefore.Y - newWorldPosition.Y;
            panStart = e.Location; // Обновляем начальную точку панорамирования для следующего шага.
            canvasSerpinsky.Invalidate(); // Запрашиваем перерисовку для визуального обновления.
            ScheduleRender(); // Планируем новый рендеринг с учетом нового центра.
        }

        /// <summary>
        /// Обрабатывает событие отпускания кнопки мыши на канвасе для завершения панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события мыши.</param>
        private void CanvasSerpinsky_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panning = false;
            }
        }

        /// <summary>
        /// Преобразует координаты точки на экране в координаты в мировой системе фрактала.
        /// </summary>
        /// <param name="screenPoint">Координаты точки на экране.</param>
        /// <param name="screenWidth">Ширина экрана (или области отрисовки).</param>
        /// <param name="screenHeight">Высота экрана (или области отрисовки).</param>
        /// <param name="zoom">Текущий масштаб фрактала.</param>
        /// <param name="centerX">Мировая координата X центра фрактала.</param>
        /// <param name="centerY">Мировая координата Y центра фрактала.</param>
        /// <returns>Координаты точки в мировой системе фрактала (реальная и мнимая части).</returns>
        private PointF ScreenToWorld(Point screenPoint, int screenWidth, int screenHeight, double zoom, double centerX, double centerY)
        {
            double aspectRatio = (double)screenWidth / screenHeight;
            double viewHeightWorld = 1.0 / zoom;
            double viewWidthWorld = viewHeightWorld * aspectRatio;
            double minReal = centerX - viewWidthWorld / 2.0;
            double maxImaginary = centerY + viewHeightWorld / 2.0;
            float worldX = (float)(minReal + (screenPoint.X / (double)screenWidth) * viewWidthWorld);
            float worldY = (float)(maxImaginary - (screenPoint.Y / (double)screenHeight) * viewHeightWorld);
            return new PointF(worldX, worldY);
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Обновляет параметры движка рендеринга фрактала, используя текущие значения из элементов управления UI.
        /// </summary>
        private void UpdateEngineParameters()
        {
            _engine.RenderMode = FractalTypeIsGeometry.Checked ? SerpinskyRenderMode.Geometric : SerpinskyRenderMode.Chaos;
            _engine.Iterations = (int)nudIterations.Value;
            _engine.Zoom = currentZoom;
            _engine.CenterX = centerX;
            _engine.CenterY = centerY;
            ApplyActivePalette(); // Применяем активную палитру, так как она тоже является параметром движка.
        }

        /// <summary>
        /// Применяет активную палитру цветов из менеджера палитр к движку рендеринга.
        /// </summary>
        private void ApplyActivePalette()
        {
            // Проверяем, что движок и активная палитра доступны.
            if (_engine == null || _paletteManager?.ActivePalette == null)
            {
                return;
            }
            var activePalette = _paletteManager.ActivePalette;
            _engine.ColorMode = SerpinskyColorMode.CustomColor;
            _engine.FractalColor = activePalette.FractalColor;
            _engine.BackgroundColor = activePalette.BackgroundColor;
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Сохранить PNG".
        /// Запускает асинхронный процесс рендеринга фрактала в высоком разрешении и сохранения его в PNG файл.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private async void btnSavePNG_Click(object sender, EventArgs e)
        {
            // Отменяем текущий предпросмотр, чтобы избежать конфликтов при рендеринге в высоком разрешении.
            if (isRenderingPreview)
            {
                previewRenderCts?.Cancel();
            }
            // Не запускаем сохранение, если уже идет высококачественный рендеринг.
            if (isHighResRendering)
            {
                return;
            }

            int outputWidth = (int)nudW2.Value;
            int outputHeight = (int)nudH2.Value;

            using (var saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"serpinski_{DateTime.Now:yyyyMMdd_HHmmss}.png" })
            {
                if (saveDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                isHighResRendering = true;
                SetMainControlsEnabled(false);
                UpdateAbortButtonState();
                progressPNGSerpinsky.Visible = true;
                UpdateProgressBar(progressPNGSerpinsky, 0);

                highResRenderCts = new CancellationTokenSource();
                CancellationToken token = highResRenderCts.Token;

                // Создаем новый экземпляр движка для рендеринга в высоком разрешении,
                // чтобы не конфликтовать с текущим состоянием основного движка.
                var renderEngine = new FractalSerpinskyEngine();
                UpdateEngineParameters(); // Получаем текущие параметры из UI.
                                          // Копируем параметры из основного движка в новый для высококачественного рендеринга.
                renderEngine.RenderMode = _engine.RenderMode;
                renderEngine.ColorMode = _engine.ColorMode;
                renderEngine.Iterations = _engine.Iterations;
                renderEngine.Zoom = _engine.Zoom;
                renderEngine.CenterX = _engine.CenterX;
                renderEngine.CenterY = _engine.CenterY;
                renderEngine.FractalColor = _engine.FractalColor;
                renderEngine.BackgroundColor = _engine.BackgroundColor;

                int numThreads = GetThreadCount();

                try
                {
                    var stopwatch = Stopwatch.StartNew(); // Запуск секундомера
                    Bitmap highResBitmap = await Task.Run(() =>
                    {
                        var bitmap = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb);
                        var bmpData = bitmap.LockBits(new Rectangle(0, 0, outputWidth, outputHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        var pixelBuffer = new byte[bmpData.Stride * outputHeight];
                        renderEngine.RenderToBuffer(
                            pixelBuffer, outputWidth, outputHeight, bmpData.Stride, 4,
                            numThreads, token, (progress) => UpdateProgressBar(progressPNGSerpinsky, progress));
                        token.ThrowIfCancellationRequested(); // Проверяем отмену после завершения рендеринга.
                        Marshal.Copy(pixelBuffer, 0, bmpData.Scan0, pixelBuffer.Length);
                        bitmap.UnlockBits(bmpData);
                        return bitmap;
                    }, token);
                    stopwatch.Stop(); // Остановка секундомера

                    highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                    double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                    MessageBox.Show($"Изображение сохранено!\nВремя рендеринга: {elapsedSeconds:F3} сек.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    highResBitmap.Dispose();
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show("Сохранение было отменено.", "Отмена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    isHighResRendering = false;
                    SetMainControlsEnabled(true);
                    UpdateAbortButtonState();
                    progressPNGSerpinsky.Visible = false;
                    highResRenderCts.Dispose();
                }
            }
        }

        /// <summary>
        /// Определяет количество потоков ЦП для рендеринга на основе выбора пользователя.
        /// Если выбрано "Авто", возвращает количество логических процессоров системы.
        /// </summary>
        /// <returns>Количество потоков ЦП для использования в рендеринге.</returns>
        private int GetThreadCount()
        {
            return cbCPUThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbCPUThreads.SelectedItem);
        }

        /// <summary>
        /// Включает или отключает основные элементы управления UI на панели.
        /// Используется для предотвращения изменения параметров во время рендеринга.
        /// Кнопка отмены рендеринга остается активной.
        /// </summary>
        /// <param name="enabled">Значение, указывающее, следует ли включить (true) или отключить (false) элементы управления.</param>
        private void SetMainControlsEnabled(bool enabled)
        {
            foreach (Control ctrl in panel1.Controls)
            {
                // Кнопка отмены всегда должна быть доступна, если рендеринг активен.
                if (ctrl != abortRender)
                {
                    ctrl.Enabled = enabled;
                }
            }
            UpdateAbortButtonState(); // Обновляем состояние кнопки отмены после изменения состояния других контролов.
        }

        /// <summary>
        /// Обновляет состояние кнопки "Отмена рендеринга" в зависимости от того, активен ли какой-либо рендеринг.
        /// </summary>
        private void UpdateAbortButtonState()
        {
            // Используем Invoke, так как метод может быть вызван из фонового потока.
            if (IsHandleCreated)
            {
                Invoke((Action)(() => abortRender.Enabled = isRenderingPreview || isHighResRendering));
            }
        }

        /// <summary>
        /// Обновляет значение индикатора прогресса.
        /// </summary>
        /// <param name="progressBar">Элемент управления ProgressBar для обновления.</param>
        /// <param name="percentage">Процент выполнения (от 0 до 100).</param>
        private void UpdateProgressBar(ProgressBar progressBar, int percentage)
        {
            // Используем Invoke, так как метод может быть вызван из фонового потока,
            // а доступ к UI-элементам должен быть в UI-потоке.
            if (progressBar.IsHandleCreated)
            {
                progressBar.Invoke((Action)(() => progressBar.Value = Math.Min(100, Math.Max(0, percentage))));
            }
        }

        /// <summary>
        /// Переопределенный метод, вызываемый при закрытии формы.
        /// Отменяет все активные операции рендеринга и освобождает связанные ресурсы.
        /// </summary>
        /// <param name="e">Данные события закрытия формы.</param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            previewRenderCts?.Cancel();
            previewRenderCts?.Dispose();
            highResRenderCts?.Cancel();
            highResRenderCts?.Dispose();
            renderTimer?.Stop();
            renderTimer?.Dispose();
            canvasBitmap?.Dispose();
            base.OnFormClosed(e);
        }
        #endregion

        #region ISaveLoadCapableFractal Implementation
        /// <summary>
        /// Получает строковый идентификатор типа фрактала.
        /// </summary>
        /// <value>Идентификатор типа фрактала, используемый для сохранения/загрузки.</value>
        public string FractalTypeIdentifier => "Serpinsky";

        /// <summary>
        /// Получает конкретный тип состояния сохранения для данного фрактала.
        /// </summary>
        /// <value>Тип состояния сохранения, который должен использоваться для сериализации/десериализации.</value>
        public Type ConcreteSaveStateType => typeof(SerpinskySaveState);

        /// <summary>
        /// Представляет параметры, используемые для рендеринга миниатюры (предпросмотра) фрактала Серпинского.
        /// </summary>
        public class SerpinskyPreviewParams
        {
            /// <summary>
            /// Получает или задает режим рендеринга фрактала (Геометрический или Хаотический).
            /// </summary>
            public SerpinskyRenderMode RenderMode { get; set; }

            /// <summary>
            /// Получает или задает количество итераций для рендеринга.
            /// </summary>
            public int Iterations { get; set; }

            /// <summary>
            /// Получает или задает текущий уровень масштабирования.
            /// </summary>
            public double Zoom { get; set; }

            /// <summary>
            /// Получает или задает координату X центра фрактала.
            /// </summary>
            public double CenterX { get; set; }

            /// <summary>
            /// Получает или задает координату Y центра фрактала.
            /// </summary>
            public double CenterY { get; set; }

            /// <summary>
            /// Получает или задает цвет фрактала.
            /// </summary>
            public Color FractalColor { get; set; }

            /// <summary>
            /// Получает или задает цвет фона.
            /// </summary>
            public Color BackgroundColor { get; set; }
        }

        /// <summary>
        /// Получает текущее состояние фрактала для сохранения.
        /// </summary>
        /// <param name="saveName">Имя, присвоенное сохраняемому состоянию.</param>
        /// <returns>Объект <see cref="FractalSaveStateBase"/>, содержащий текущие параметры фрактала.</returns>
        public FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            var state = new SerpinskySaveState(this.FractalTypeIdentifier)
            {
                SaveName = saveName,
                Timestamp = DateTime.Now,
                RenderMode = FractalTypeIsGeometry.Checked ? SerpinskyRenderMode.Geometric : SerpinskyRenderMode.Chaos,
                Iterations = (int)nudIterations.Value,
                Zoom = this.currentZoom,
                CenterX = this.centerX,
                CenterY = this.centerY,
                FractalColor = _engine.FractalColor,
                BackgroundColor = _engine.BackgroundColor
            };

            // Создаем укороченные параметры для быстрого рендеринга превью,
            // чтобы предпросмотр загружался быстрее.
            var previewParams = new SerpinskyPreviewParams
            {
                RenderMode = state.RenderMode,
                Zoom = state.Zoom,
                CenterX = state.CenterX,
                CenterY = state.CenterY,
                FractalColor = state.FractalColor,
                BackgroundColor = state.BackgroundColor,
                // Используем меньшее количество итераций для превью, чтобы ускорить его генерацию.
                Iterations = (state.RenderMode == SerpinskyRenderMode.Geometric)
                    ? Math.Min(state.Iterations, 5)
                    : Math.Min(state.Iterations, 20000)
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
            if (stateBase is SerpinskySaveState state)
            {
                // Останавливаем текущие рендеры, чтобы избежать конфликтов при загрузке нового состояния.
                previewRenderCts?.Cancel();
                renderTimer.Stop();

                // Создаем временный объект палитры из загруженного состояния.
                // Имя палитры генерируется из имени сохранения для удобства.
                var loadedPalette = new SerpinskyColorPalette
                {
                    Name = $"Загружено: {state.SaveName}",
                    FractalColor = state.FractalColor,
                    BackgroundColor = state.BackgroundColor,
                    IsBuiltIn = false // Важно, чтобы палитра рассматривалась как временная/пользовательская.
                };

                // Устанавливаем эту палитру как активную в менеджере.
                // Она не добавляется в общий список, чтобы не засорять его, а просто становится текущей.
                _paletteManager.ActivePalette = loadedPalette;

                // Устанавливаем RenderMode, чтобы обновить ограничения nudIterations до установки значения.
                if (state.RenderMode == SerpinskyRenderMode.Geometric)
                {
                    FractalTypeIsGeometry.Checked = true;
                }
                else
                {
                    FractalTypeIsChaos.Checked = true;
                }

                // Безопасно устанавливаем значения контролов, учитывая их минимальные и максимальные значения.
                decimal safeIterations = Math.Max(nudIterations.Minimum, Math.Min(nudIterations.Maximum, state.Iterations));
                nudIterations.Value = safeIterations;

                this.centerX = state.CenterX;
                this.centerY = state.CenterY;
                this.currentZoom = state.Zoom;
                decimal safeZoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, (decimal)state.Zoom));
                nudZoom.Value = safeZoom;

                // Вызываем UpdateEngineParameters, который теперь также вызовет ApplyActivePalette
                // и применит цвета из нашей новой, только что установленной `loadedPalette`.
                UpdateEngineParameters();

                // Запускаем рендер нового состояния.
                ScheduleRender();
            }
            else
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Эти поля используются для кеширования предварительных изображений.
        // `_cachedPreviewBitmap` хранит отрендеренный битмап для текущего превью.
        private static Bitmap _cachedPreviewBitmap;
        // `_cachedPreviewStateIdentifier` хранит уникальный идентификатор состояния, для которого был отрендерен _cachedPreviewBitmap.
        private static string _cachedPreviewStateIdentifier;
        // `_previewCacheLock` используется для синхронизации доступа к кешу превью,
        // чтобы избежать состояния гонки при параллельном запросе на рендеринг плиток.
        private static readonly object _previewCacheLock = new object();

        /// <summary>
        /// Асинхронно рендерит плитку для предварительного просмотра.
        /// Использует механизм кеширования для избежания многократного рендеринга одного и того же полного изображения превью.
        /// </summary>
        /// <param name="state">Состояние фрактала для рендеринга.</param>
        /// <param name="tile">Информация о запрашиваемой плитке.</param>
        /// <param name="totalWidth">Общая ширина изображения предварительного просмотра.</param>
        /// <param name="totalHeight">Общая высота изображения предварительного просмотра.</param>
        /// <param name="tileSize">Размер одной плитки (для вывода).</param>
        /// <returns>Массив байтов, представляющий данные пикселей для запрошенной плитки.</returns>
        public async Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase state, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            // Используем уникальный идентификатор состояния для определения,
            // соответствует ли кешированное изображение текущему запросу.
            string currentStateIdentifier = $"{state.SaveName}_{state.Timestamp}";

            lock (_previewCacheLock)
            {
                // Проверяем, существует ли уже кешированное изображение для текущего состояния.
                if (_cachedPreviewBitmap != null && _cachedPreviewStateIdentifier == currentStateIdentifier)
                {
                    // Битмап уже есть, ничего не делаем в этом блоке.
                }
                else
                {
                    // Битмапа нет или он устарел, сбрасываем кеш. Рендер произойдет ниже, вне блокировки.
                    _cachedPreviewBitmap?.Dispose();
                    _cachedPreviewBitmap = null;
                }
            }

            // Если битмапа не было (или он был сброшен), рендерим его.
            // Этот код может выполниться несколько раз параллельно для одного и того же состояния,
            // но только первый успешно отрендеренный результат попадет в кеш благодаря повторной блокировке ниже.
            if (_cachedPreviewBitmap == null)
            {
                var newBitmap = await Task.Run(() => RenderPreview(state, totalWidth, totalHeight));
                lock (_previewCacheLock) // Повторно блокируем для безопасного обновления кеша.
                {
                    // Еще раз проверяем, вдруг кто-то уже создал битмап, пока мы рендерили,
                    // чтобы избежать перезаписи и утечки памяти.
                    if (_cachedPreviewBitmap == null)
                    {
                        _cachedPreviewBitmap = newBitmap;
                        _cachedPreviewStateIdentifier = currentStateIdentifier;
                    }
                    else
                    {
                        newBitmap?.Dispose(); // Наш рендер оказался лишним, освобождаем ресурсы.
                    }
                }
            }

            // Теперь, когда у нас точно есть кешированный битмап, вырезаем из него запрошенную плитку.
            var tileBuffer = new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
            using (var tileBmp = new Bitmap(tile.Bounds.Width, tile.Bounds.Height, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(tileBmp))
                {
                    lock (_previewCacheLock) // Блокируем доступ к битмапу на время чтения, чтобы избежать конфликтов.
                    {
                        if (_cachedPreviewBitmap != null)
                        {
                            g.DrawImage(_cachedPreviewBitmap, new Rectangle(0, 0, tile.Bounds.Width, tile.Bounds.Height), tile.Bounds, GraphicsUnit.Pixel);
                        }
                    }
                }

                // Копируем данные пикселей плитки в буфер.
                var bmpData = tileBmp.LockBits(new Rectangle(0, 0, tileBmp.Width, tileBmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(bmpData.Scan0, tileBuffer, 0, tileBuffer.Length);
                tileBmp.UnlockBits(bmpData);
            }

            return tileBuffer;
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
            // Обрабатываем случай, если параметры предпросмотра отсутствуют в состоянии.
            if (string.IsNullOrEmpty(state.PreviewParametersJson))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError))
                {
                    g.Clear(Color.DarkGray);
                    TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                return bmpError;
            }

            SerpinskyPreviewParams previewParams;
            try
            {
                // Десериализуем параметры предпросмотра из JSON.
                var jsonOptions = new JsonSerializerOptions();
                jsonOptions.Converters.Add(new Utilities.JsonConverters.JsonColorConverter());
                previewParams = JsonSerializer.Deserialize<SerpinskyPreviewParams>(state.PreviewParametersJson, jsonOptions);
            }
            catch (Exception)
            {
                // Обрабатываем ошибку десериализации параметров.
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError))
                {
                    g.Clear(Color.DarkRed);
                    TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                return bmpError;
            }

            // Инициализируем новый движок фрактала с параметрами для предпросмотра.
            var previewEngine = new FractalSerpinskyEngine
            {
                RenderMode = previewParams.RenderMode,
                Iterations = previewParams.Iterations,
                Zoom = previewParams.Zoom,
                CenterX = previewParams.CenterX,
                CenterY = previewParams.CenterY,
                FractalColor = previewParams.FractalColor,
                BackgroundColor = previewParams.BackgroundColor,
                ColorMode = SerpinskyColorMode.CustomColor
            };

            Bitmap bmp = new Bitmap(previewWidth, previewHeight, PixelFormat.Format32bppArgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, previewWidth, previewHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte[] buffer = new byte[bmpData.Stride * previewHeight];

            // Рендерим изображение в буфер. Для предпросмотра достаточно одного потока и без отмены.
            previewEngine.RenderToBuffer(buffer, previewWidth, previewHeight, bmpData.Stride, 4, 1, CancellationToken.None, progress => { });

            // Копируем данные из буфера в битмап.
            Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        /// <summary>
        /// Загружает все сохраненные состояния фрактала для данного типа.
        /// </summary>
        /// <returns>Список базовых объектов состояний фрактала.</returns>
        public List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<SerpinskySaveState>(this.FractalTypeIdentifier);
            // Приводим список к базовому типу для соответствия интерфейсу.
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        /// <summary>
        /// Сохраняет список состояний фрактала для данного типа.
        /// </summary>
        /// <param name="saves">Список базовых объектов состояний фрактала для сохранения.</param>
        public void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<SerpinskySaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }
        #endregion
    }
}