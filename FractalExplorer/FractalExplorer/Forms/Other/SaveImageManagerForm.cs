using FractalExplorer.Properties;
using FractalExplorer.Utilities.RenderUtilities;
using FractalExplorer.Utilities.Imaging.Filters;
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

        // Коэффициент, на который уменьшается исходный рендер для последующего увеличения.
        // 1.5 означает, что рендер будет в 1.5 раза меньше целевого разрешения.
        private const double BICUBIC_UPSCALE_FACTOR = 1.5;

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
            LoadSettings();
            UpdateJpgQualityUI();
            UpdateEffectControls(); // Обновляем состояние элементов управления эффектами
            _lastStatusMessage = "Готово";
            lblStatus.Text = _lastStatusMessage;
        }

        private void SaveImageManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isRendering)
            {
                _cts?.Cancel();
            }
            else
            {
                SaveSettings();
            }
            _uiUpdateTimer?.Dispose();
        }

        #region Settings Management

        private void LoadSettings()
        {
            var settings = Settings.Default;
            nudWidth.Value = Math.Max(nudWidth.Minimum, Math.Min(nudWidth.Maximum, settings.SaveForm_Width));
            nudHeight.Value = Math.Max(nudHeight.Minimum, Math.Min(nudHeight.Maximum, settings.SaveForm_Height));
            cbFormat.SelectedIndex = Math.Max(0, Math.Min(cbFormat.Items.Count - 1, settings.SaveForm_FormatIndex));
            cbSSAA.SelectedIndex = Math.Max(0, Math.Min(cbSSAA.Items.Count - 1, settings.SaveForm_SsaaIndex));
            trackBarJpgQuality.Value = Math.Max(trackBarJpgQuality.Minimum, Math.Min(trackBarJpgQuality.Maximum, settings.SaveForm_JpgQuality));
            chkApplyBicubic.Checked = settings.SaveForm_ApplyBicubic;

            lblJpgQualityValue.Text = $"{trackBarJpgQuality.Value}%";
        }

        private void SaveSettings()
        {
            var settings = Settings.Default;
            settings.SaveForm_Width = nudWidth.Value;
            settings.SaveForm_Height = nudHeight.Value;
            settings.SaveForm_FormatIndex = cbFormat.SelectedIndex;
            settings.SaveForm_SsaaIndex = cbSSAA.SelectedIndex;
            settings.SaveForm_JpgQuality = trackBarJpgQuality.Value;
            settings.SaveForm_ApplyBicubic = chkApplyBicubic.Checked;
            settings.Save();
        }

        #endregion

        #region UI Event Handlers

        private void cbFormat_SelectedIndexChanged(object sender, EventArgs e) => UpdateJpgQualityUI();

        private void trackBarJpgQuality_Scroll(object sender, EventArgs e)
        {
            lblJpgQualityValue.Text = $"{trackBarJpgQuality.Value}%";
        }

        private void chkApplyBicubic_CheckedChanged(object sender, EventArgs e)
        {
            UpdateEffectControls();
        }

        // --- Обработчики событий для пресетов и поворота ---
        private void btnPreset720p_Click(object sender, EventArgs e) { nudWidth.Value = 1280; nudHeight.Value = 720; }
        private void btnPresetFHD_Click(object sender, EventArgs e) { nudWidth.Value = 1920; nudHeight.Value = 1080; }
        private void btnPreset2K_Click(object sender, EventArgs e) { nudWidth.Value = 2560; nudHeight.Value = 1440; }
        private void btnPreset4K_Click(object sender, EventArgs e) { nudWidth.Value = 3840; nudHeight.Value = 2160; }
        private void btnPreset8K_Click(object sender, EventArgs e) { nudWidth.Value = 7680; nudHeight.Value = 4320; }
        private void btnRotate_Click(object sender, EventArgs e) { (nudWidth.Value, nudHeight.Value) = (nudHeight.Value, nudWidth.Value); }

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
                    int targetWidth = (int)nudWidth.Value;
                    int targetHeight = (int)nudHeight.Value;
                    ImageFormat imageFormat = GetImageFormat(format);
                    int jpgQuality = trackBarJpgQuality.Value;

                    int renderWidth, renderHeight, ssaaFactor;
                    bool useBicubicUpscale = chkApplyBicubic.Checked;

                    if (useBicubicUpscale)
                    {
                        // Режим апскейла: рендерим в меньшем разрешении
                        renderWidth = (int)(targetWidth / BICUBIC_UPSCALE_FACTOR);
                        renderHeight = (int)(targetHeight / BICUBIC_UPSCALE_FACTOR);
                        ssaaFactor = 1; // SSAA не используется в этом режиме
                    }
                    else
                    {
                        // Стандартный режим SSAA
                        renderWidth = targetWidth;
                        renderHeight = targetHeight;
                        ssaaFactor = GetSsaaFactor();
                    }

                    // 1. Основной рендеринг (с параметрами, зависящими от режима)
                    renderedBitmap = await _renderSource.RenderHighResolutionAsync(_renderState, renderWidth, renderHeight, ssaaFactor, progress, _cts.Token);
                    _cts.Token.ThrowIfCancellationRequested();

                    // 2. Этап пост-обработки (увеличиваем, если нужно)
                    finalBitmap = await ApplyPostProcessingAsync(renderedBitmap, useBicubicUpscale, targetWidth, targetHeight, progress, _cts.Token);
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

        #endregion

        #region Helper Methods

        private async Task<Bitmap> ApplyPostProcessingAsync(Bitmap sourceBitmap, bool useBicubic, int targetWidth, int targetHeight, IProgress<RenderProgress> progress, CancellationToken token)
        {
            if (!useBicubic)
            {
                return sourceBitmap; // Если режим не выбран, просто возвращаем исходное изображение
            }

            progress.Report(new RenderProgress { Status = $"Бикубическое увеличение до {targetWidth}x{targetHeight}...", Percentage = 95 });

            var bicubicFilter = new BicubicResizeFilter(targetWidth, targetHeight);
            Bitmap filteredBitmap = await Task.Run(() => bicubicFilter.Apply(sourceBitmap), token);

            return filteredBitmap;
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

        private void UpdateJpgQualityUI()
        {
            bool isJpg = cbFormat.SelectedItem?.ToString() == "JPG";
            lblJpgQuality.Visible = isJpg;
            trackBarJpgQuality.Visible = isJpg;
            lblJpgQualityValue.Visible = isJpg;
        }

        /// <summary>
        /// Включает/выключает контролы SSAA в зависимости от галочки бикубической интерполяции.
        /// </summary>
        private void UpdateEffectControls()
        {
            bool bicubicMode = chkApplyBicubic.Checked;
            // Просто делаем контролы SSAA неактивными, не меняя их значение
            lblSsaa.Enabled = !bicubicMode;
            cbSSAA.Enabled = !bicubicMode;
        }

        private int GetSsaaFactor()
        {
            switch (cbSSAA.SelectedIndex)
            {
                case 1: return 2;
                case 2: return 4;
                case 3: return 8;
                case 4: return 10;
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

        #endregion
    }
}