using FractalExplorer.Utilities.RenderUtilities;
using FractalExplorer.Utilities.Imaging.Filters; // <-- Добавлен using для фильтров
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
    public partial class SaveImageManagerForm : Form
    {
        private readonly IHighResRenderable _renderSource;
        private readonly HighResRenderState _renderState;
        private CancellationTokenSource _cts;
        private bool _isRendering = false;
        private readonly Stopwatch _renderStopwatch;
        private readonly System.Windows.Forms.Timer _uiUpdateTimer;
        private string _lastStatusMessage;

        public SaveImageManagerForm(IHighResRenderable renderSource)
        {
            InitializeComponent();
            _renderSource = renderSource ?? throw new ArgumentNullException(nameof(renderSource));
            _renderState = _renderSource.GetRenderState();
            _renderStopwatch = new Stopwatch();
            _uiUpdateTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;
        }

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

        private void SaveImageManagerForm_Load(object sender, EventArgs e)
        {
            cbFormat.SelectedIndex = 0;
            cbSSAA.SelectedIndex = 1;
            UpdateJpgQualityUI();
            _lastStatusMessage = "Готово";
            lblStatus.Text = _lastStatusMessage;
        }

        private void cbFormat_SelectedIndexChanged(object sender, EventArgs e) => UpdateJpgQualityUI();

        private void UpdateJpgQualityUI()
        {
            bool isJpg = cbFormat.SelectedItem?.ToString() == "JPG";
            lblJpgQuality.Visible = isJpg;
            trackBarJpgQuality.Visible = isJpg;
            lblJpgQualityValue.Visible = isJpg;
        }

        private void trackBarJpgQuality_Scroll(object sender, EventArgs e)
        {
            lblJpgQualityValue.Text = $"{trackBarJpgQuality.Value}%";
        }

        private void btnPresetFHD_Click(object sender, EventArgs e)
        {
            nudWidth.Value = 1920;
            nudHeight.Value = 1080;
        }

        private void btnPreset4K_Click(object sender, EventArgs e)
        {
            nudWidth.Value = 3840;
            nudHeight.Value = 2160;
        }

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

                Bitmap renderedBitmap = null;
                Bitmap finalBitmap = null;

                try
                {
                    int width = (int)nudWidth.Value;
                    int height = (int)nudHeight.Value;
                    int ssaaFactor = GetSsaaFactor();
                    ImageFormat imageFormat = GetImageFormat(format);
                    int jpgQuality = trackBarJpgQuality.Value;

                    // 1. Основной рендеринг
                    renderedBitmap = await _renderSource.RenderHighResolutionAsync(_renderState, width, height, ssaaFactor, progress, _cts.Token);
                    _cts.Token.ThrowIfCancellationRequested();

                    // 2. Этап пост-обработки (применение фильтров)
                    finalBitmap = await ApplyPostProcessingAsync(renderedBitmap, width, height, progress, _cts.Token);
                    _cts.Token.ThrowIfCancellationRequested();

                    // Если фильтр создал новый битмап, старый нужно освободить
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

                    // 3. Сохранение итогового изображения
                    await Task.Run(() => SaveBitmap(finalBitmap, sfd.FileName, imageFormat, jpgQuality), _cts.Token);

                    string elapsedTimeString = totalTime.TotalMinutes >= 1 ?
                        $"{totalTime:m' мин 's' сек'}" :
                        $"{totalTime:s\\.fff' сек'}";

                    MessageBox.Show($"Изображение успешно сохранено!\n\nОбщее время: {elapsedTimeString}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                catch (AggregateException ae) when (ae.InnerException is OperationCanceledException)
                {
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

        /// <summary>
        /// Асинхронно применяет выбранные фильтры пост-обработки к изображению.
        /// </summary>
        private async Task<Bitmap> ApplyPostProcessingAsync(Bitmap sourceBitmap, int targetWidth, int targetHeight, IProgress<RenderProgress> progress, CancellationToken token)
        {
            Bitmap currentBitmap = sourceBitmap;

            // Проверяем, нужно ли применять бикубическую интерполяцию
            if (chkApplyBicubic.Checked)
            {
                progress.Report(new RenderProgress { Status = "Применение бикубической интерполяции...", Percentage = 95 });

                var bicubicFilter = new BicubicResizeFilter(targetWidth, targetHeight);

                // Запускаем ресурсоемкую задачу в фоновом потоке, чтобы не блокировать UI
                Bitmap filteredBitmap = await Task.Run(() => bicubicFilter.Apply(currentBitmap), token);

                // Если currentBitmap не был исходным (вдруг будет цепочка фильтров), его нужно освободить
                if (currentBitmap != sourceBitmap)
                {
                    currentBitmap.Dispose();
                }
                currentBitmap = filteredBitmap;
            }

            // Здесь можно будет добавить другие фильтры:
            // if (chkSharpen.Checked) { ... }

            return currentBitmap;
        }

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

        private void SaveImageManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isRendering && _cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
            _uiUpdateTimer?.Dispose();
        }

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

        private int GetSsaaFactor()
        {
            switch (cbSSAA.SelectedIndex)
            {
                case 1: return 2;
                case 2: return 4;
                case 3: return 8;
                default: return 1;
            }
        }

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