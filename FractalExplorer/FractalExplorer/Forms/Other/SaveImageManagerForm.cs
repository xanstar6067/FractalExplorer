using FractalExplorer.Properties;
using FractalExplorer.Utilities.Imaging.Filters;
using FractalExplorer.Utilities.RenderUtilities;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FractalExplorer.Forms.Other
{
    /// <summary>
    /// Форма для управления процессом сохранения изображения в высоком разрешении.
    /// Предоставляет пользователю опции выбора разрешения, формата файла,
    /// применения сглаживания (SSAA) или бикубического апскейла.
    /// </summary>
    public partial class SaveImageManagerForm : Form
    {
        #region Поля

        /// <summary>
        /// Источник для рендеринга изображения в высоком разрешении.
        /// </summary>
        private readonly IHighResRenderable _renderSource;

        /// <summary>
        /// Состояние рендеринга (параметры фрактала), которое будет сохранено.
        /// </summary>
        private readonly HighResRenderState _renderState;

        /// <summary>
        /// Источник токенов для отмены асинхронной операции рендеринга.
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// Флаг, указывающий, выполняется ли в данный момент процесс рендеринга.
        /// </summary>
        private bool _isRendering = false;

        /// <summary>
        /// Секундомер для измерения времени, затраченного на рендеринг.
        /// </summary>
        private readonly Stopwatch _renderStopwatch;

        /// <summary>
        /// Таймер для периодического обновления пользовательского интерфейса (например, времени рендеринга).
        /// </summary>
        private readonly System.Windows.Forms.Timer _uiUpdateTimer;

        /// <summary>
        /// Последнее сообщение о статусе для отображения пользователю.
        /// </summary>
        private string _lastStatusMessage;

        #endregion

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SaveImageManagerForm"/>.
        /// </summary>
        /// <param name="renderSource">Источник, предоставляющий данные и метод для рендеринга.</param>
        public SaveImageManagerForm(IHighResRenderable renderSource)
        {
            InitializeComponent();
            _renderSource = renderSource ?? throw new ArgumentNullException(nameof(renderSource));
            _renderState = _renderSource.GetRenderState();
            _renderStopwatch = new Stopwatch();
            _uiUpdateTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
        }

        #region Обработчики событий формы и таймера

        /// <summary>
        /// Обрабатывает событие тика таймера для обновления статусной строки.
        /// </summary>
        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (lblStatus.IsHandleCreated && !lblStatus.IsDisposed)
            {
                this.Invoke((Action)(() => lblStatus.Text = $"{_lastStatusMessage} [{_renderStopwatch.Elapsed:mm\\:ss\\.f}]"));
            }
        }

        /// <summary>
        /// Обрабатывает событие загрузки формы.
        /// </summary>
        private void SaveImageManagerForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
            UpdateJpgQualityUI();
            UpdateEffectControls();
            _lastStatusMessage = "Готово";
            lblStatus.Text = _lastStatusMessage;
        }

        /// <summary>
        /// Обрабатывает событие закрытия формы.
        /// Если рендеринг не выполняется, сохраняет настройки.
        /// Если рендеринг активен, отменяет его.
        /// </summary>
        private void SaveImageManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isRendering)
            {
                SaveSettings();
            }
            else
            {
                _cts?.Cancel();
            }
            _uiUpdateTimer?.Dispose();
        }

        #endregion

        #region Управление настройками

        /// <summary>
        /// Загружает настройки формы из пользовательских настроек приложения.
        /// </summary>
        private void LoadSettings()
        {
            var settings = Settings.Default;
            nudWidth.Value = Math.Max(nudWidth.Minimum, Math.Min(nudWidth.Maximum, settings.SaveForm_Width));
            nudHeight.Value = Math.Max(nudHeight.Minimum, Math.Min(nudHeight.Maximum, settings.SaveForm_Height));
            cbFormat.SelectedIndex = Math.Max(0, Math.Min(cbFormat.Items.Count - 1, settings.SaveForm_FormatIndex));
            cbSSAA.SelectedIndex = Math.Max(0, Math.Min(cbSSAA.Items.Count - 1, settings.SaveForm_SsaaIndex));
            trackBarJpgQuality.Value = Math.Max(trackBarJpgQuality.Minimum, Math.Min(trackBarJpgQuality.Maximum, settings.SaveForm_JpgQuality));

            // MODIFIED: Загрузка настроек для новых элементов
            chkApplyBicubic.Checked = settings.SaveForm_ApplyBicubic;
            cbBicubicFactor.SelectedIndex = Math.Max(0, Math.Min(cbBicubicFactor.Items.Count - 1, settings.SaveForm_BicubicFactorIndex));
            chkApplyLanczos.Checked = settings.SaveForm_ApplyLanczos;
            cbLanczosFactor.SelectedIndex = Math.Max(0, Math.Min(cbLanczosFactor.Items.Count - 1, settings.SaveForm_LanczosFactorIndex));

            lblJpgQualityValue.Text = $"{trackBarJpgQuality.Value}%";
        }

        /// <summary>
        /// Сохраняет текущие настройки формы в пользовательские настройки приложения.
        /// </summary>
        private void SaveSettings()
        {
            var settings = Settings.Default;
            settings.SaveForm_Width = nudWidth.Value;
            settings.SaveForm_Height = nudHeight.Value;
            settings.SaveForm_FormatIndex = cbFormat.SelectedIndex;
            settings.SaveForm_SsaaIndex = cbSSAA.SelectedIndex;
            settings.SaveForm_JpgQuality = trackBarJpgQuality.Value;

            // MODIFIED: Сохранение настроек для новых элементов
            settings.SaveForm_ApplyBicubic = chkApplyBicubic.Checked;
            settings.SaveForm_BicubicFactorIndex = cbBicubicFactor.SelectedIndex;
            settings.SaveForm_ApplyLanczos = chkApplyLanczos.Checked;
            settings.SaveForm_LanczosFactorIndex = cbLanczosFactor.SelectedIndex;

            settings.Save();
        }

        #endregion

        #region Обработчики событий элементов UI

        private void cbFormat_SelectedIndexChanged(object sender, EventArgs e) => UpdateJpgQualityUI();
        private void trackBarJpgQuality_Scroll(object sender, EventArgs e) => lblJpgQualityValue.Text = $"{trackBarJpgQuality.Value}%";

        // MODIFIED: Обработчики для взаимного исключения чекбоксов
        private void chkApplyBicubic_CheckedChanged(object sender, EventArgs e)
        {
            if (chkApplyBicubic.Checked)
            {
                chkApplyLanczos.Checked = false;
            }
            UpdateEffectControls();
        }

        // NEW: Обработчик для нового чекбокса
        private void chkApplyLanczos_CheckedChanged(object sender, EventArgs e)
        {
            if (chkApplyLanczos.Checked)
            {
                chkApplyBicubic.Checked = false;
            }
            UpdateEffectControls();
        }

        private void btnPreset720p_Click(object sender, EventArgs e) { nudWidth.Value = 1280; nudHeight.Value = 720; }
        private void btnPresetFHD_Click(object sender, EventArgs e) { nudWidth.Value = 1920; nudHeight.Value = 1080; }
        private void btnPreset2K_Click(object sender, EventArgs e) { nudWidth.Value = 2560; nudHeight.Value = 1440; }
        private void btnPreset4K_Click(object sender, EventArgs e) { nudWidth.Value = 3840; nudHeight.Value = 2160; }
        private void btnPreset8K_Click(object sender, EventArgs e) { nudWidth.Value = 7680; nudHeight.Value = 4320; }
        private void btnRotate_Click(object sender, EventArgs e) => (nudWidth.Value, nudHeight.Value) = (nudHeight.Value, nudWidth.Value);

        /// <summary>
        /// Обрабатывает нажатие на кнопку "Отмена" или "Закрыть".
        /// Если идет рендеринг, отменяет его. В противном случае закрывает форму.
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_isRendering && _cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
            else
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        /// <summary>
        /// Запускает процесс рендеринга и сохранения изображения.
        /// </summary>
        private async void btnSave_Click(object sender, EventArgs e)
        {
            string format = cbFormat.SelectedItem.ToString();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"{_renderState.FileNameDetails}_{timestamp}.{format.ToLower()}";

            using (var sfd = new SaveFileDialog
            {
                Filter = $"{format.ToUpper()} Files|*.{format.ToLower()}|All files|*.*",
                FileName = suggestedFileName,
                Title = "Сохранить изображение"
            })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                _isRendering = true;
                SetUiState(false);
                _cts = new CancellationTokenSource();
                _renderStopwatch.Restart();
                _uiUpdateTimer.Start();

                IProgress<RenderProgress> progress = new Progress<RenderProgress>(p =>
                {
                    _lastStatusMessage = p.Status;
                    if (progressBar.IsHandleCreated && !progressBar.IsDisposed)
                    {
                        progressBar.Invoke((Action)(() => progressBar.Value = p.Percentage));
                    }
                });

                Bitmap renderedBitmap = null;
                Bitmap finalBitmap = null;

                try
                {
                    int targetWidth = (int)nudWidth.Value;
                    int targetHeight = (int)nudHeight.Value;
                    ImageFormat imageFormat = GetImageFormat(format);
                    int jpgQuality = trackBarJpgQuality.Value;

                    // MODIFIED: Определяем, какой режим используется
                    bool useBicubicUpscale = chkApplyBicubic.Checked;
                    bool useLanczos = chkApplyLanczos.Checked;
                    int renderWidth, renderHeight, ssaaFactor;

                    if (useBicubicUpscale)
                    {
                        // Режим с бикубическим апскейлом: рендерим в меньшем разрешении.
                        double upscaleFactor = GetBicubicFactor();
                        renderWidth = (int)Math.Max(1, targetWidth / upscaleFactor);
                        renderHeight = (int)Math.Max(1, targetHeight / upscaleFactor);
                        ssaaFactor = 1; // SSAA не используется в этом режиме.
                    }
                    // NEW: Логика для режима Ланцоша
                    else if (useLanczos)
                    {
                        // Режим с Ланцошем: рендерим в разрешении, зависящем от коэффициента.
                        double scaleFactor = GetLanczosFactor();
                        renderWidth = (int)Math.Max(1, Math.Round(targetWidth * scaleFactor));
                        renderHeight = (int)Math.Max(1, Math.Round(targetHeight * scaleFactor));
                        ssaaFactor = 1; // SSAA не используется, т.к. Ланцош выполняет свою форму суперсемплинга.
                    }
                    else
                    {
                        // Стандартный режим с SSAA: рендерим в целевом разрешении.
                        renderWidth = targetWidth;
                        renderHeight = targetHeight;
                        ssaaFactor = GetSsaaFactor();
                    }

                    renderedBitmap = await _renderSource.RenderHighResolutionAsync(_renderState, renderWidth, renderHeight, ssaaFactor, progress, _cts.Token);
                    _cts.Token.ThrowIfCancellationRequested();

                    // MODIFIED: Передаем флаг useLanczos в постобработку
                    finalBitmap = await ApplyPostProcessingAsync(renderedBitmap, useBicubicUpscale, useLanczos, targetWidth, targetHeight, progress, _cts.Token);
                    _cts.Token.ThrowIfCancellationRequested();

                    if (renderedBitmap != finalBitmap)
                    {
                        renderedBitmap.Dispose();
                        renderedBitmap = null;
                    }

                    _renderStopwatch.Stop();
                    _uiUpdateTimer.Stop();
                    TimeSpan totalTime = _renderStopwatch.Elapsed;
                    lblStatus.Text = $"Сохранение файла... (Заняло {totalTime:mm\\:ss})";
                    progressBar.Value = 100;

                    await Task.Run(() => SaveBitmap(finalBitmap, sfd.FileName, imageFormat, jpgQuality), _cts.Token);

                    string elapsedTimeString = totalTime.TotalMinutes >= 1 ? $"{totalTime:m' мин 's' сек'}" : $"{totalTime:s\\.fff' сек'}";
                    MessageBox.Show($"Изображение успешно сохранено!\n\nОбщее время: {elapsedTimeString}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (OperationCanceledException)
                {
                    lblStatus.Text = "Операция отменена.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка при сохранении: {ex.Message}\n\n{ex.StackTrace}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    renderedBitmap?.Dispose();
                    finalBitmap?.Dispose();
                    _renderStopwatch.Stop();
                    _uiUpdateTimer.Stop();
                    _isRendering = false;
                    SetUiState(true);
                    _cts?.Dispose();
                    _cts = null;
                }
            }
        }

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Применяет постобработку к отрендеренному изображению.
        /// В данный момент используется для бикубического апскейла или масштабирования Ланцошем.
        /// </summary>
        /// <param name="sourceBitmap">Исходное изображение.</param>
        /// <param name="useBicubic">Флаг, указывающий, нужно ли применять апскейл.</param>
        /// <param name="useLanczos">Флаг, указывающий, нужно ли применять масштабирование Ланцошем.</param>
        /// <param name="targetWidth">Целевая ширина изображения.</param>
        /// <param name="targetHeight">Целевая высота изображения.</param>
        /// <param name="progress">Объект для отчета о прогрессе.</param>
        /// <param name="token">Токен отмены.</param>
        /// <returns>Обработанное изображение. Если обработка не требовалась, возвращает исходное.</returns>
        private async Task<Bitmap> ApplyPostProcessingAsync(Bitmap sourceBitmap, bool useBicubic, bool useLanczos, int targetWidth, int targetHeight, IProgress<RenderProgress> progress, CancellationToken token)
        {
            bool isResizeNeeded = sourceBitmap.Width != targetWidth || sourceBitmap.Height != targetHeight;

            if (useBicubic && isResizeNeeded)
            {
                progress.Report(new RenderProgress { Status = $"Бикубический апскейл до {targetWidth}x{targetHeight}...", Percentage = 95 });
                var bicubicFilter = new BicubicResizeFilter(targetWidth, targetHeight);
                return await Task.Run(() => bicubicFilter.Apply(sourceBitmap), token);
            }
            else if (useLanczos && isResizeNeeded)
            {
                progress.Report(new RenderProgress { Status = $"Масштабирование (Ланцош) до {targetWidth}x{targetHeight}...", Percentage = 95 });
                var lanczosFilter = new LanczosResizeFilter(targetWidth, targetHeight); // a=3 по умолчанию
                return await Task.Run(() => lanczosFilter.Apply(sourceBitmap), token);
            }

            return sourceBitmap; // Если обработка не нужна, возвращаем оригинал
        }

        /// <summary>
        /// Сохраняет изображение в файл с заданными параметрами.
        /// </summary>
        /// <param name="bitmap">Изображение для сохранения.</param>
        /// <param name="filePath">Путь к файлу.</param>
        /// <param name="format">Формат изображения.</param>
        /// <param name="jpgQuality">Качество для формата JPG (0-100).</param>
        private void SaveBitmap(Bitmap bitmap, string filePath, ImageFormat format, int jpgQuality)
        {
            if (format == ImageFormat.Jpeg)
            {
                var qualityEncoder = Encoder.Quality;
                var encoderParameters = new EncoderParameters(1) { Param = { [0] = new EncoderParameter(qualityEncoder, (long)jpgQuality) } };
                ImageCodecInfo jpgEncoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

                if (jpgEncoder != null)
                {
                    bitmap.Save(filePath, jpgEncoder, encoderParameters);
                }
            }
            else
            {
                bitmap.Save(filePath, format);
            }
        }

        /// <summary>
        /// Устанавливает состояние элементов UI в зависимости от того, идет ли рендеринг.
        /// </summary>
        /// <param name="enabled">True, если элементы должны быть активны; иначе False.</param>
        private void SetUiState(bool enabled)
        {
            if (this.IsDisposed) return;
            this.Invoke((Action)(() =>
            {
                if (this.IsDisposed) return;
                pnlMain.Enabled = enabled;
                btnSave.Enabled = enabled;
                btnCancel.Text = enabled ? "Закрыть" : "Отмена";

                if (enabled)
                {
                    progressBar.Value = 0;
                    _lastStatusMessage = "Готово";
                    lblStatus.Text = _lastStatusMessage;
                }
            }));
        }

        /// <summary>
        /// Обновляет видимость элементов управления качеством JPG.
        /// </summary>
        private void UpdateJpgQualityUI()
        {
            bool isJpg = cbFormat.SelectedItem?.ToString() == "JPG";
            lblJpgQuality.Visible = isJpg;
            trackBarJpgQuality.Visible = isJpg;
            lblJpgQualityValue.Visible = isJpg;
        }

        /// <summary>
        /// Обновляет состояние элементов управления эффектами (SSAA / Bicubic / Lanczos).
        /// </summary>
        private void UpdateEffectControls()
        {
            bool bicubicMode = chkApplyBicubic.Checked;
            bool lanczosMode = chkApplyLanczos.Checked;

            // SSAA доступен только если не выбран ни один из фильтров
            lblSsaa.Enabled = !bicubicMode && !lanczosMode;
            cbSSAA.Enabled = !bicubicMode && !lanczosMode;

            // Управление для бикубического фильтра
            lblBicubicFactor.Visible = bicubicMode;
            cbBicubicFactor.Visible = bicubicMode;

            // NEW: Управление для фильтра Ланцоша
            lblLanczosFactor.Visible = lanczosMode;
            cbLanczosFactor.Visible = lanczosMode;
        }

        /// <summary>
        /// Получает множитель SSAA из выбранного элемента ComboBox.
        /// </summary>
        /// <returns>Целочисленный множитель (1, 2, 4, 8, 10).</returns>
        private int GetSsaaFactor()
        {
            return cbSSAA.SelectedIndex switch
            {
                1 => 2,
                2 => 4,
                3 => 8,
                4 => 10,
                _ => 1,
            };
        }

        /// <summary>
        /// Получает множитель для бикубического апскейла из ComboBox.
        /// </summary>
        /// <returns>Множитель апскейла (например, 1.5).</returns>
        private double GetBicubicFactor()
        {
            return cbBicubicFactor.SelectedIndex switch
            {
                0 => 1.1,
                1 => 1.2,
                2 => 1.3,
                3 => 1.4,
                4 => 1.5,
                5 => 2.0,
                6 => 2.5,
                _ => 1.5,
            };
        }

        /// <summary>
        /// NEW: Получает множитель для масштабирования методом Ланцоша.
        /// > 1.0 - суперсемплинг (уменьшение), < 1.0 - апскейл (увеличение).
        /// </summary>
        /// <returns>Множитель масштабирования.</returns>
        private double GetLanczosFactor()
        {
            return cbLanczosFactor.SelectedIndex switch
            {
                0 => 4.0,   // x4.0 (Суперсемплинг, Ultra)
                1 => 2.0,   // x2.0 (Суперсемплинг, High)
                2 => 1.5,   // x1.5 (Суперсемплинг, Medium)
                3 => 0.75,  // x0.75 (Апскейл, Quality)
                4 => 0.5,   // x0.5 (Апскейл, Balanced)
                5 => 0.25,  // x0.25 (Апскейл, Performance)
                _ => 1.0,   // По умолчанию без изменений
            };
        }

        /// <summary>
        /// Преобразует строковое представление формата в объект <see cref="ImageFormat"/>.
        /// </summary>
        /// <param name="format">Строка с названием формата ("JPG", "BMP", "PNG").</param>
        /// <returns>Соответствующий объект ImageFormat. По умолчанию PNG.</returns>
        private ImageFormat GetImageFormat(string format)
        {
            return format.ToUpper() switch
            {
                "JPG" => ImageFormat.Jpeg,
                "BMP" => ImageFormat.Bmp,
                _ => ImageFormat.Png,
            };
        }

        #endregion
    }
}