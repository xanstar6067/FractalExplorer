using FractalExplorer.Utilities.RenderUtilities;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FractalExplorer.Forms.Other
{
    /// <summary>
    /// Форма для управления процессом рендеринга и сохранения изображений фракталов в высоком разрешении.
    /// </summary>
    public partial class SaveImageManagerForm : Form
    {
        /// <summary>
        /// Источник для рендеринга изображения.
        /// </summary>
        private readonly IHighResRenderable _renderSource;

        /// <summary>
        /// Состояние рендеринга, содержащее параметры для отрисовки.
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
        /// Секундомер для измерения времени рендеринга.
        /// </summary>
        private readonly Stopwatch _renderStopwatch;

        /// <summary>
        /// Таймер для обновления пользовательского интерфейса.
        /// </summary>
        private readonly System.Windows.Forms.Timer _uiUpdateTimer;

        /// <summary>
        /// Последнее сообщение о статусе для отображения.
        /// </summary>
        private string _lastStatusMessage;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SaveImageManagerForm"/>.
        /// </summary>
        /// <param name="renderSource">Источник, способный выполнить рендеринг изображения в высоком разрешении.</param>
        public SaveImageManagerForm(IHighResRenderable renderSource)
        {
            InitializeComponent();
            _renderSource = renderSource ?? throw new ArgumentNullException(nameof(renderSource));
            _renderState = _renderSource.GetRenderState();

            _renderStopwatch = new Stopwatch();
            _uiUpdateTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
        }

        /// <summary>
        /// Обрабатывает событие Tick таймера обновления UI для обновления метки статуса.
        /// </summary>
        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            this.Invoke((Action)(() =>
            {
                if (lblStatus.IsHandleCreated && !lblStatus.IsDisposed)
                {
                    lblStatus.Text = $"{_lastStatusMessage} [{_renderStopwatch.Elapsed:mm\\:ss\\.f}]";
                }
            }));
        }

        /// <summary>
        /// Обрабатывает событие Load формы, инициализируя значения по умолчанию.
        /// </summary>
        private void SaveImageManagerForm_Load(object sender, EventArgs e)
        {
            cbFormat.SelectedIndex = 0;
            cbSSAA.SelectedIndex = 1;
            UpdateJpgQualityUI();
            _lastStatusMessage = "Готово";
            lblStatus.Text = _lastStatusMessage;
        }

        /// <summary>
        /// Обрабатывает событие SelectedIndexChanged для выпадающего списка форматов.
        /// </summary>
        private void cbFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateJpgQualityUI();
        }

        /// <summary>
        /// Показывает или скрывает элементы управления качеством JPG в зависимости от выбранного формата изображения.
        /// </summary>
        private void UpdateJpgQualityUI()
        {
            bool isJpg = cbFormat.SelectedItem?.ToString() == "JPG";
            lblJpgQuality.Visible = isJpg;
            trackBarJpgQuality.Visible = isJpg;
            lblJpgQualityValue.Visible = isJpg;
        }

        /// <summary>
        /// Обрабатывает событие Scroll ползунка качества JPG, обновляя метку со значением.
        /// </summary>
        private void trackBarJpgQuality_Scroll(object sender, EventArgs e)
        {
            lblJpgQualityValue.Text = $"{trackBarJpgQuality.Value}%";
        }

        /// <summary>
        /// Устанавливает разрешение Full HD (1920x1080).
        /// </summary>
        private void btnPresetFHD_Click(object sender, EventArgs e)
        {
            nudWidth.Value = 1920;
            nudHeight.Value = 1080;
        }

        /// <summary>
        /// Устанавливает разрешение 4K UHD (3840x2160).
        /// </summary>
        private void btnPreset4K_Click(object sender, EventArgs e)
        {
            nudWidth.Value = 3840;
            nudHeight.Value = 2160;
        }

        /// <summary>
        /// Обрабатывает нажатие на кнопку "Сохранить", запуская процесс рендеринга и сохранения.
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

                _lastStatusMessage = "Подготовка к рендерингу...";
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

                try
                {
                    int width = (int)nudWidth.Value;
                    int height = (int)nudHeight.Value;
                    int ssaaFactor = GetSsaaFactor();
                    ImageFormat imageFormat = GetImageFormat(format);
                    int jpgQuality = trackBarJpgQuality.Value;

                    Bitmap resultBitmap = await _renderSource.RenderHighResolutionAsync(_renderState, width, height, ssaaFactor, progress, _cts.Token);

                    _cts.Token.ThrowIfCancellationRequested();

                    _renderStopwatch.Stop();
                    _uiUpdateTimer.Stop();
                    TimeSpan renderTime = _renderStopwatch.Elapsed;

                    lblStatus.Text = $"Сохранение файла... (Заняло {renderTime:mm\\:ss})";
                    progressBar.Value = 100;

                    await Task.Run(() => SaveBitmap(resultBitmap, sfd.FileName, imageFormat, jpgQuality), _cts.Token);
                    resultBitmap.Dispose();

                    string elapsedTimeString;
                    if (renderTime.TotalMinutes >= 1)
                        elapsedTimeString = $"{renderTime:m' мин 's' сек'}";
                    else
                        elapsedTimeString = $"{renderTime:s\\.fff' сек'}";

                    MessageBox.Show($"Изображение успешно сохранено!\n\nВремя рендеринга: {elapsedTimeString}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                catch (AggregateException ae)
                {
                    ae.Handle(ex => ex is OperationCanceledException);
                    lblStatus.Text = "Операция отменена.";
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
                    _renderStopwatch.Stop();
                    _uiUpdateTimer.Stop();
                    _isRendering = false;
                    SetUiState(true);
                    _cts?.Dispose();
                    _cts = null;
                }
            }
        }

        /// <summary>
        /// Сохраняет указанное изображение в файл с заданным форматом и настройками качества.
        /// </summary>
        /// <param name="bitmap">Изображение для сохранения.</param>
        /// <param name="filePath">Путь к выходному файлу.</param>
        /// <param name="format">Формат изображения.</param>
        /// <param name="jpgQuality">Настройка качества для формата JPG (0-100).</param>
        private void SaveBitmap(Bitmap bitmap, string filePath, ImageFormat format, int jpgQuality)
        {
            if (format == ImageFormat.Jpeg)
            {
                var qualityEncoder = Encoder.Quality;
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(qualityEncoder, (long)jpgQuality);

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
        /// Обрабатывает нажатие на кнопку "Отмена"/"Закрыть".
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
        /// Обрабатывает событие закрытия формы для гарантированной отмены выполняющегося рендеринга.
        /// </summary>
        private void SaveImageManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isRendering && _cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
            _uiUpdateTimer?.Dispose();
        }

        /// <summary>
        /// Включает или отключает элементы управления пользовательского интерфейса в зависимости от состояния рендеринга.
        /// </summary>
        /// <param name="enabled">True для включения элементов, false для отключения.</param>
        private void SetUiState(bool enabled)
        {
            if (this.IsDisposed) return;

            this.Invoke((Action)(() => {
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
        /// Получает коэффициент Super-Sampling Anti-Aliasing (SSAA) из выбора в UI.
        /// </summary>
        /// <returns>Целочисленный коэффициент SSAA.</returns>
        private int GetSsaaFactor()
        {
            switch (cbSSAA.SelectedIndex)
            {
                case 1: return 2; // Низкое
                case 2: return 4; // Высокое
                case 3: return 8; // Ультра
                default: return 1; // Выкл
            }
        }

        /// <summary>
        /// Преобразует строковое имя формата в соответствующий объект <see cref="ImageFormat"/>.
        /// </summary>
        /// <param name="format">Имя формата (например, "JPG", "PNG").</param>
        /// <returns>Соответствующий <see cref="ImageFormat"/>.</returns>
        private ImageFormat GetImageFormat(string format)
        {
            switch (format.ToUpper())
            {
                case "JPG": return ImageFormat.Jpeg;
                case "BMP": return ImageFormat.Bmp;
                default: return ImageFormat.Png;
            }
        }
    }
}