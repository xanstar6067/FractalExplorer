using FractalExplorer.Engines;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Windows.Forms;

namespace FractalExplorer
{
    /// <summary>
    /// Форма для отображения и управления рендерингом фрактала Серпинского.
    /// Предоставляет пользовательский интерфейс для настройки параметров фрактала,
    /// режимов рендеринга и цветовой схемы.
    /// </summary>
    public partial class FractalSerpinsky : Form
    {
        #region Fields

        /// <summary>
        /// Экземпляр движка для рендеринга фрактала Серпинского.
        /// </summary>
        private readonly SerpinskyFractalEngine _engine;

        /// <summary>
        /// Битмап, в который рендерится фрактал для отображения на канвасе.
        /// </summary>
        private Bitmap canvasBitmap;

        /// <summary>
        /// Флаг, указывающий, выполняется ли сейчас рендеринг предпросмотра.
        /// </summary>
        private volatile bool isRenderingPreview = false;

        /// <summary>
        /// Флаг, указывающий, выполняется ли сейчас рендеринг в высоком разрешении для сохранения.
        /// </summary>
        private volatile bool isHighResRendering = false;

        /// <summary>
        /// Токен отмены для операций рендеринга предпросмотра.
        /// </summary>
        private CancellationTokenSource previewRenderCts;

        /// <summary>
        /// Токен отмены для операций рендеринга в высоком разрешении.
        /// </summary>
        private CancellationTokenSource highResRenderCts;

        /// <summary>
        /// Таймер для отложенного запуска рендеринга, чтобы избежать частых перерисовок.
        /// </summary>
        private System.Windows.Forms.Timer renderTimer;

        /// <summary>
        /// Текущий коэффициент масштабирования фрактала.
        /// </summary>
        private double currentZoom = 1.0;

        /// <summary>
        /// Текущая координата X центра видимой области фрактала.
        /// </summary>
        private double centerX = 0.0;

        /// <summary>
        /// Текущая координата Y центра видимой области фрактала.
        /// </summary>
        private double centerY = 0.0;

        /// <summary>
        /// Коэффициент масштабирования, по которому был отрисован текущий <see cref="canvasBitmap"/>.
        /// </summary>
        private double renderedZoom = 1.0;

        /// <summary>
        /// Координата X центра, по которой был отрисован текущий <see cref="canvasBitmap"/>.
        /// </summary>
        private double renderedCenterX = 0.0;

        /// <summary>
        /// Координата Y центра, по которой был отрисован текущий <see cref="canvasBitmap"/>.
        /// </summary>
        private double renderedCenterY = 0.0;

        /// <summary>
        /// Начальная позиция курсора мыши при панорамировании.
        /// </summary>
        private Point panStart;

        /// <summary>
        /// Флаг, указывающий, находится ли пользователь в режиме панорамирования.
        /// </summary>
        private bool panning = false;

        /// <summary>
        /// Диалог выбора цвета для настройки пользовательских цветов фрактала.
        /// </summary>
        private ColorDialog colorDialog;

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FractalSerpinsky"/>.
        /// </summary>
        public FractalSerpinsky()
        {
            InitializeComponent();
            _engine = new SerpinskyFractalEngine();
            InitializeCustomComponents();
        }

        #endregion

        #region UI Initialization

        /// <summary>
        /// Выполняет дополнительную инициализацию элементов управления формы и обработчиков событий.
        /// </summary>
        private void InitializeCustomComponents()
        {
            renderTimer = new System.Windows.Forms.Timer { Interval = 300 };
            renderTimer.Tick += RenderTimer_Tick;

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++)
            {
                cbCPUThreads.Items.Add(i);
            }
            cbCPUThreads.Items.Add("Auto");
            cbCPUThreads.SelectedItem = "Auto";

            colorDialog = new ColorDialog();

            Load += (s, e) =>
            {
                renderedCenterX = centerX;
                renderedCenterY = centerY;
                renderedZoom = currentZoom;
                ScheduleRender();
            };
            canvasSerpinsky.Paint += CanvasSerpinsky_Paint;
            canvasSerpinsky.MouseWheel += CanvasSerpinsky_MouseWheel;
            canvasSerpinsky.MouseDown += CanvasSerpinsky_MouseDown;
            canvasSerpinsky.MouseMove += CanvasSerpinsky_MouseMove;
            canvasSerpinsky.MouseUp += CanvasSerpinsky_MouseUp;
            Resize += (s, e) => ScheduleRender();
            canvasSerpinsky.Resize += (s, e) => ScheduleRender();

            nudZoom.ValueChanged += ParamControl_Changed;
            nudIterations.ValueChanged += ParamControl_Changed;
            cbCPUThreads.SelectedIndexChanged += ParamControl_Changed;

            nudIterations.Minimum = 0;
            nudIterations.Maximum = 20;
            nudIterations.Value = 8;

            nudZoom.Minimum = 0.01m;
            nudZoom.Maximum = 10000000m;
            nudZoom.Value = 1m;
            nudZoom.DecimalPlaces = 2;

            FractalTypeIsGeometry.CheckedChanged += FractalType_CheckedChanged;
            FractalTypeIsChaos.CheckedChanged += FractalType_CheckedChanged;
            renderBW.CheckedChanged += ColorMode_CheckedChanged;
            colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;
            colorColor.CheckedChanged += ColorChoiceMode_CheckedChanged;
            colorBackground.CheckedChanged += ColorTarget_CheckedChanged;
            colorFractal.CheckedChanged += ColorTarget_CheckedChanged;

            FractalTypeIsGeometry.Checked = true;
            colorGrayscale.Checked = true;
            UpdatePaletteCanvas();
            UpdateAbortButtonState();
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// Обработчик события изменения выбранного режима рендеринга фрактала (Геометрический или Хаос).
        /// Обновляет параметры итераций в соответствии с выбранным режимом.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void FractalType_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCheckBox = sender as CheckBox;
            if (activeCheckBox == null || !activeCheckBox.Checked)
            {
                return;
            }

            if (activeCheckBox == FractalTypeIsGeometry)
            {
                FractalTypeIsChaos.Checked = false;
                nudIterations.Maximum = 20;
                nudIterations.Minimum = 0;
                // Если текущее значение выходит за новые пределы, сбрасываем на значение по умолчанию.
                if (nudIterations.Value > 20)
                {
                    nudIterations.Value = 8;
                }
            }
            else // FractalTypeIsChaos
            {
                FractalTypeIsGeometry.Checked = false;
                nudIterations.Maximum = int.MaxValue;
                nudIterations.Minimum = 1000;
                // Если текущее значение слишком мало для режима "Хаос", сбрасываем на адекватное.
                if (nudIterations.Value < 1000)
                {
                    nudIterations.Value = 50000;
                }
            }
            ScheduleRender();
        }

        /// <summary>
        /// Обработчик события изменения выбора пользовательского режима цвета.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void ColorChoiceMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox currentCheckBox = sender as CheckBox;
            if (currentCheckBox == null)
            {
                return;
            }

            // Временно отписываемся от событий других чекбоксов, чтобы избежать рекурсии
            renderBW.CheckedChanged -= ColorMode_CheckedChanged;
            colorGrayscale.CheckedChanged -= ColorMode_CheckedChanged;

            if (currentCheckBox.Checked)
            {
                if (renderBW.Checked)
                {
                    renderBW.Checked = false;
                }
                if (colorGrayscale.Checked)
                {
                    colorGrayscale.Checked = false;
                }
                // Если выбран режим "Custom Color", и ни один из под-выборов (Fractal/Background) не активен, активируем Fractal.
                if (!colorFractal.Checked && !colorBackground.Checked)
                {
                    colorFractal.CheckedChanged -= ColorTarget_CheckedChanged;
                    colorFractal.Checked = true;
                    colorFractal.CheckedChanged += ColorTarget_CheckedChanged;
                }
            }
            else if (!renderBW.Checked && !colorGrayscale.Checked)
            {
                // Если "Custom Color" отключен, и ни один из базовых режимов не активен, включаем "Grayscale" по умолчанию.
                colorGrayscale.Checked = true;
            }

            // Подписываемся обратно на события
            renderBW.CheckedChanged += ColorMode_CheckedChanged;
            colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;

            UpdatePaletteCanvas();
            ScheduleRender();
        }

        /// <summary>
        /// Обработчик события изменения выбора режима цвета (Черно-белый, Оттенки серого).
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void ColorMode_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCheckBox = sender as CheckBox;
            if (activeCheckBox == null)
            {
                return;
            }

            // Временно отписываемся от событий других чекбоксов
            if (colorColor != null)
            {
                colorColor.CheckedChanged -= ColorChoiceMode_CheckedChanged;
            }

            if (activeCheckBox.Checked)
            {
                if (colorColor != null && colorColor.Checked)
                {
                    colorColor.Checked = false;
                }

                // Обеспечиваем, что только один из BW/Grayscale активен
                if (activeCheckBox == renderBW && colorGrayscale.Checked)
                {
                    colorGrayscale.CheckedChanged -= ColorMode_CheckedChanged;
                    colorGrayscale.Checked = false;
                    colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;
                }
                else if (activeCheckBox == colorGrayscale && renderBW.Checked)
                {
                    renderBW.CheckedChanged -= ColorMode_CheckedChanged;
                    renderBW.Checked = false;
                    renderBW.CheckedChanged += ColorMode_CheckedChanged;
                }
            }
            else if (!renderBW.Checked && !colorGrayscale.Checked && (colorColor == null || !colorColor.Checked))
            {
                // Если все режимы цвета отключены, возвращаем к "Grayscale" по умолчанию.
                colorGrayscale.CheckedChanged -= ColorMode_CheckedChanged;
                colorGrayscale.Checked = true;
                colorGrayscale.CheckedChanged += ColorMode_CheckedChanged;
            }

            // Подписываемся обратно на события
            if (colorColor != null)
            {
                colorColor.CheckedChanged += ColorChoiceMode_CheckedChanged;
            }

            UpdatePaletteCanvas();
            ScheduleRender();
        }

        /// <summary>
        /// Обработчик события изменения выбранной цели для пользовательского цвета (Фрактал или Фон).
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void ColorTarget_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCheckBox = sender as CheckBox;
            if (activeCheckBox == null || !activeCheckBox.Checked)
            {
                // Если ни одна из целей цвета не выбрана в режиме CustomColor, по умолчанию выбираем Fractal
                if ((colorColor != null && colorColor.Checked) && !colorBackground.Checked && !colorFractal.Checked)
                {
                    colorFractal.CheckedChanged -= ColorTarget_CheckedChanged;
                    colorFractal.Checked = true;
                    colorFractal.CheckedChanged += ColorTarget_CheckedChanged;
                }
                UpdatePaletteCanvas();
                return;
            }

            if (activeCheckBox == colorBackground)
            {
                colorFractal.Checked = false;
            }
            else if (activeCheckBox == colorFractal)
            {
                colorBackground.Checked = false;
            }
            UpdatePaletteCanvas();
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Render".
        /// Отменяет любой текущий рендеринг и немедленно запускает новый.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnRender_Click(object sender, EventArgs e)
        {
            // Отменяем любой текущий рендер и останавливаем таймер, чтобы избежать двойного запуска
            previewRenderCts?.Cancel();
            renderTimer.Stop();

            // Немедленно запускаем логику рендеринга, которая находится в RenderTimer_Tick,
            // минуя 300-миллисекундную задержку таймера.
            RenderTimer_Tick(sender, e);
        }

        #endregion

        #region Rendering Logic

        /// <summary>
        /// Обработчик события изменения числовых параметров управления (зум, итерации и т.д.).
        /// Запускает отложенный рендеринг.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void ParamControl_Changed(object sender, EventArgs e)
        {
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
        /// Планирует рендеринг предпросмотра, используя таймер задержки.
        /// Это предотвращает избыточные рендеринги при частых изменениях параметров.
        /// </summary>
        private void ScheduleRender()
        {
            if (isHighResRendering)
            {
                return;
            }
            previewRenderCts?.Cancel(); // Отменяем текущий предпросмотр, если он активен
            renderTimer.Stop();
            renderTimer.Start();
        }

        /// <summary>
        /// Обработчик события тика таймера рендеринга.
        /// Запускает процесс рендеринга фрактала в буфер.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private async void RenderTimer_Tick(object sender, EventArgs e)
        {
            renderTimer.Stop();
            // Если уже идет рендеринг высокого разрешения или предпросмотра, выходим.
            if (isHighResRendering || isRenderingPreview)
            {
                return;
            }

            isRenderingPreview = true;
            SetMainControlsEnabled(false);
            UpdateAbortButtonState();

            previewRenderCts?.Dispose(); // Освобождаем старый токен
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;

            UpdateEngineParameters(); // Обновляем параметры движка из UI

            int numThreads = GetThreadCount(); // Получаем количество потоков

            int renderWidth = canvasSerpinsky.Width;
            int renderHeight = canvasSerpinsky.Height;
            if (renderWidth <= 0 || renderHeight <= 0)
            {
                // Если размеры нулевые, завершаем рендеринг
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

                await Task.Run(() => _engine.RenderToBuffer(
                    pixelBuffer, renderWidth, renderHeight, bitmapData.Stride, 4,
                    numThreads,
                    token,
                    (progress) => UpdateProgressBar(progressBarSerpinsky, progress)),
                token);

                token.ThrowIfCancellationRequested(); // Проверка отмены после завершения рендеринга

                Marshal.Copy(pixelBuffer, 0, bitmapData.Scan0, pixelBuffer.Length);
                bitmap.UnlockBits(bitmapData);

                // Заменяем старый битмап новым и освобождаем ресурсы старого
                Bitmap oldImage = canvasBitmap;
                canvasBitmap = bitmap;
                renderedZoom = currentZoom;
                renderedCenterX = centerX;
                renderedCenterY = centerY;
                canvasSerpinsky.Invalidate(); // Запрашиваем перерисовку
                oldImage?.Dispose();
            }
            catch (OperationCanceledException)
            {
                // Игнорируем исключение, если операция была отменена
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isRenderingPreview = false;
                // Восстанавливаем элементы управления, если не идет рендеринг высокого разрешения
                if (!isHighResRendering)
                {
                    SetMainControlsEnabled(true);
                }
                UpdateAbortButtonState();
                UpdateProgressBar(progressBarSerpinsky, 0); // Сбрасываем прогресс-бар
            }
        }

        #endregion

        #region Canvas Interaction

        /// <summary>
        /// Обработчик события отрисовки канваса.
        /// Рисует текущий битмап фрактала на канвасе, интерполируя его при изменении масштаба или центра.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события рисования.</param>
        private void CanvasSerpinsky_Paint(object sender, PaintEventArgs e)
        {
            bool isBlackAndWhiteMode = renderBW.Checked;
            bool isGrayscaleMode = colorGrayscale.Checked;

            Color effectiveBackgroundColor;
            if (isBlackAndWhiteMode || isGrayscaleMode)
            {
                effectiveBackgroundColor = Color.White;
            }
            else
            {
                effectiveBackgroundColor = _engine.BackgroundColor;
            }

            e.Graphics.Clear(effectiveBackgroundColor); // Очищаем фон канваса

            if (canvasBitmap == null || canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0)
            {
                return;
            }

            // Параметры отрисованного битмапа
            double renderedAspectRatio = (double)canvasBitmap.Width / canvasBitmap.Height;
            double renderedViewHeightWorld = 1.0 / renderedZoom;
            double renderedViewWidthWorld = renderedViewHeightWorld * renderedAspectRatio;
            double renderedMinReal = renderedCenterX - renderedViewWidthWorld / 2.0;
            double renderedMaxImaginary = renderedCenterY + renderedViewHeightWorld / 2.0;

            // Текущие параметры канваса
            double currentAspectRatio = (double)canvasSerpinsky.Width / canvasSerpinsky.Height;
            double currentViewHeightWorld = 1.0 / currentZoom;
            double currentViewWidthWorld = currentViewHeightWorld * currentAspectRatio;
            double currentMinReal = centerX - currentViewWidthWorld / 2.0;
            double currentMaxImaginary = centerY + currentViewHeightWorld / 2.0;

            // Вычисляем положение и размер для отрисовки исходного битмапа с учетом нового зума и панорамирования
            float offsetX = (float)(((renderedMinReal - currentMinReal) / currentViewWidthWorld) * canvasSerpinsky.Width);
            float offsetY = (float)(((currentMaxImaginary - renderedMaxImaginary) / currentViewHeightWorld) * canvasSerpinsky.Height);
            float scaleWidth = (float)((renderedViewWidthWorld / currentViewWidthWorld) * canvasSerpinsky.Width);
            float scaleHeight = (float)((renderedViewHeightWorld / currentViewHeightWorld) * canvasSerpinsky.Height);

            // Используем интерполяцию NearestNeighbor для четкого пиксельного масштабирования
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(canvasBitmap, new RectangleF(offsetX, offsetY, scaleWidth, scaleHeight));
        }

        /// <summary>
        /// Обработчик события прокрутки колеса мыши над канвасом.
        /// Изменяет масштаб фрактала и обновляет центр, чтобы сохранить точку под курсором.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события колеса мыши.</param>
        private void CanvasSerpinsky_MouseWheel(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || canvasSerpinsky.Width <= 0)
            {
                return;
            }

            double zoomFactor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
            // Вычисляем мировые координаты точки под курсором до изменения масштаба
            PointF worldPosition = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);

            currentZoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, currentZoom * zoomFactor));

            // Пересчитываем новый центр так, чтобы точка под курсором осталась на месте
            PointF newWorldPosition = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            centerX += worldPosition.X - newWorldPosition.X;
            centerY += worldPosition.Y - newWorldPosition.Y;

            canvasSerpinsky.Invalidate(); // Запрашиваем перерисовку для немедленного отображения зума

            // Обновляем NumericUpDown, но только если значение действительно изменилось, чтобы избежать зацикливания
            if (nudZoom.Value != (decimal)currentZoom)
            {
                nudZoom.Value = (decimal)currentZoom;
            }
            else
            {
                // Если nudZoom.Value уже currentZoom (например, из-за ограничения Max/Min), все равно планируем рендеринг
                ScheduleRender();
            }
        }

        /// <summary>
        /// Обработчик события нажатия кнопки мыши над канвасом.
        /// Инициирует режим панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void CanvasSerpinsky_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panning = true;
                panStart = e.Location;
            }
        }

        /// <summary>
        /// Обработчик события перемещения мыши над канвасом.
        /// Выполняет панорамирование фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void CanvasSerpinsky_MouseMove(object sender, MouseEventArgs e)
        {
            if (!panning)
            {
                return;
            }

            // Вычисляем мировые координаты точки, откуда началось панорамирование
            PointF worldBefore = ScreenToWorld(panStart, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            // Вычисляем мировые координаты текущей позиции мыши
            PointF worldAfter = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);

            // Смещаем центр, чтобы точка, за которую тянули, оставалась под курсором
            centerX += worldBefore.X - worldAfter.X;
            centerY += worldBefore.Y - worldAfter.Y;

            panStart = e.Location; // Обновляем начальную точку панорамирования

            canvasSerpinsky.Invalidate(); // Запрашиваем перерисовку для плавного панорамирования
            ScheduleRender(); // Планируем новый рендеринг для высокой четкости после завершения панорамирования
        }

        /// <summary>
        /// Обработчик события отпускания кнопки мыши над канвасом.
        /// Завершает режим панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void CanvasSerpinsky_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panning = false;
            }
        }

        /// <summary>
        /// Преобразует координаты из экранных пиксельных координат в мировое пространство фрактала.
        /// </summary>
        /// <param name="screenPoint">Точка в экранных пиксельных координатах.</param>
        /// <param name="screenWidth">Ширина экрана/области рендеринга в пикселях.</param>
        /// <param name="screenHeight">Высота экрана/области рендеринга в пикселях.</param>
        /// <param name="zoom">Коэффициент масштабирования фрактала.</param>
        /// <param name="centerX">Координата X центра видимой области фрактала.</param>
        /// <param name="centerY">Координата Y центра видимой области фрактала.</param>
        /// <returns>Точка в мировых координатах фрактала.</returns>
        private PointF ScreenToWorld(Point screenPoint, int screenWidth, int screenHeight, double zoom, double centerX, double centerY)
        {
            double aspectRatio = (double)screenWidth / screenHeight;
            double viewHeightWorld = 1.0 / zoom;
            double viewWidthWorld = viewHeightWorld * aspectRatio;

            double minReal = centerX - viewWidthWorld / 2.0;
            double maxImaginary = centerY + viewHeightWorld / 2.0;

            float worldX = (float)(minReal + (screenPoint.X / (double)screenWidth) * viewWidthWorld);
            // Ось Y в экранных координатах обычно направлена вниз, поэтому (maxImaginary - worldPoint.Y)
            float worldY = (float)(maxImaginary - (screenPoint.Y / (double)screenHeight) * viewHeightWorld);
            return new PointF(worldX, worldY);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Обновляет параметры движка фрактала на основе текущих значений элементов управления UI.
        /// </summary>
        private void UpdateEngineParameters()
        {
            _engine.RenderMode = FractalTypeIsGeometry.Checked ? SerpinskyRenderMode.Geometric : SerpinskyRenderMode.Chaos;

            if (renderBW.Checked)
            {
                _engine.ColorMode = SerpinskyColorMode.BlackAndWhite;
            }
            else if (colorGrayscale.Checked)
            {
                _engine.ColorMode = SerpinskyColorMode.Grayscale;
            }
            else
            {
                _engine.ColorMode = SerpinskyColorMode.CustomColor;
            }

            _engine.Iterations = (int)nudIterations.Value;
            _engine.Zoom = currentZoom;
            _engine.CenterX = centerX;
            _engine.CenterY = centerY;
            // Цвета _engine.FractalColor и BackgroundColor обновляются через cancasPalette_Click
        }

        /// <summary>
        /// Обработчик события клика по кнопке сохранения изображения в PNG.
        /// Рендерит фрактал в высоком разрешении и сохраняет его в файл.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private async void btnSavePNG_Click(object sender, EventArgs e)
        {
            if (isRenderingPreview)
            {
                previewRenderCts?.Cancel();
            }
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

                // Создаем отдельный движок для сохранения, чтобы не изменять параметры текущего движка
                var renderEngine = new SerpinskyFractalEngine();
                UpdateEngineParameters(); // Обновляем параметры, чтобы скопировать их
                renderEngine.RenderMode = _engine.RenderMode;
                renderEngine.ColorMode = _engine.ColorMode;
                renderEngine.Iterations = _engine.Iterations;
                renderEngine.Zoom = _engine.Zoom;
                renderEngine.CenterX = _engine.CenterX;
                renderEngine.CenterY = _engine.CenterY;
                renderEngine.FractalColor = _engine.FractalColor;
                renderEngine.BackgroundColor = _engine.BackgroundColor;

                int numThreads = GetThreadCount(); // Получаем количество потоков

                try
                {
                    Bitmap highResBitmap = await Task.Run(() =>
                    {
                        var bitmap = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb);
                        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, outputWidth, outputHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        var pixelBuffer = new byte[bitmapData.Stride * outputHeight];

                        renderEngine.RenderToBuffer(
                            pixelBuffer, outputWidth, outputHeight, bitmapData.Stride, 4,
                            numThreads,
                            token,
                            (progress) => UpdateProgressBar(progressPNGSerpinsky, progress));

                        token.ThrowIfCancellationRequested();
                        Marshal.Copy(pixelBuffer, 0, bitmapData.Scan0, pixelBuffer.Length);
                        bitmap.UnlockBits(bitmapData);
                        return bitmap;
                    }, token);

                    highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                    MessageBox.Show("Изображение сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        /// Определяет количество потоков для использования в параллельных вычислениях.
        /// </summary>
        /// <returns>Количество потоков.</returns>
        private int GetThreadCount()
        {
            return cbCPUThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbCPUThreads.SelectedItem);
        }

        /// <summary>
        /// Обработчик события клика по канвасу для выбора цвета.
        /// Открывает диалог выбора цвета и применяет выбранный цвет к фракталу или фону.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void cancasPalette_Click(object sender, EventArgs e)
        {
            if (!colorColor.Checked)
            {
                return;
            }
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                if (colorFractal.Checked)
                {
                    _engine.FractalColor = colorDialog.Color;
                }
                else if (colorBackground.Checked)
                {
                    _engine.BackgroundColor = colorDialog.Color;
                }
                UpdatePaletteCanvas();
                ScheduleRender();
            }
        }

        /// <summary>
        /// Обновляет отображение текущего выбранного цвета или градиента на канвасе палитры.
        /// </summary>
        private void UpdatePaletteCanvas()
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            bool isColorModeActive = colorColor.Checked;
            bool areMainControlsActive = nudZoom.Enabled;

            colorFractal.Enabled = areMainControlsActive && isColorModeActive;
            colorBackground.Enabled = areMainControlsActive && isColorModeActive;
            canvasPalette.Enabled = areMainControlsActive && isColorModeActive;

            using (Graphics g = canvasPalette.CreateGraphics())
            {
                if (canvasPalette.Enabled)
                {
                    if (isColorModeActive)
                    {
                        Color previewColor = colorFractal.Checked ? _engine.FractalColor : _engine.BackgroundColor;
                        g.Clear(previewColor);
                    }
                    else if (renderBW.Checked)
                    {
                        g.Clear(Color.White);
                        g.FillRectangle(Brushes.Black, 0, 0, canvasPalette.Width / 2, canvasPalette.Height);
                    }
                    else if (colorGrayscale.Checked)
                    {
                        using (var linearGradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(canvasPalette.ClientRectangle, Color.Gainsboro, Color.DarkSlateGray, 0f))
                        {
                            g.FillRectangle(linearGradientBrush, canvasPalette.ClientRectangle);
                        }
                    }
                }
                else
                {
                    g.Clear(SystemColors.ControlDark);
                }
            }
        }

        /// <summary>
        /// Включает или отключает основные элементы управления формы.
        /// Кнопка "Отмена" управляется отдельно.
        /// </summary>
        /// <param name="enabled">True для включения, False для отключения.</param>
        private void SetMainControlsEnabled(bool enabled)
        {
            // Проходимся по всем контролам на панели
            foreach (Control ctrl in panel1.Controls)
            {
                // Если это НЕ кнопка отмены, то меняем ее состояние
                if (ctrl != abortRender)
                {
                    ctrl.Enabled = enabled;
                }
            }
            // Состояние самой кнопки отмены будет управляться отдельно
            // методом UpdateAbortButtonState(), который вызывается следом.
            UpdateAbortButtonState();
        }

        /// <summary>
        /// Обновляет состояние кнопки "Отмена" рендеринга.
        /// Кнопка активна, если идет рендеринг предпросмотра или высокого разрешения.
        /// </summary>
        private void UpdateAbortButtonState()
        {
            if (IsHandleCreated)
            {
                Invoke((Action)(() => abortRender.Enabled = isRenderingPreview || isHighResRendering));
            }
        }

        /// <summary>
        /// Обновляет значение прогресс-бара.
        /// </summary>
        /// <param name="progressBar">Прогресс-бар для обновления.</param>
        /// <param name="percentage">Процент выполнения (от 0 до 100).</param>
        private void UpdateProgressBar(ProgressBar progressBar, int percentage)
        {
            if (progressBar.IsHandleCreated)
            {
                progressBar.Invoke((Action)(() => progressBar.Value = Math.Min(100, Math.Max(0, percentage))));
            }
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Отмена рендеринга".
        /// Отменяет активные операции рендеринга.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
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

        /// <summary>
        /// Обработчик события закрытия формы.
        /// Освобождает ресурсы и отменяет любые активные операции.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            previewRenderCts?.Cancel();
            previewRenderCts?.Dispose();
            highResRenderCts?.Cancel();
            highResRenderCts?.Dispose();
            renderTimer?.Stop();
            renderTimer?.Dispose();
            canvasBitmap?.Dispose();
            colorDialog?.Dispose();
            base.OnFormClosed(e);
        }

        #endregion
    }
}