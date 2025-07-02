using FractalExplorer.Utilities.RenderUtilities;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

        public SaveImageManagerForm(IHighResRenderable renderSource)
        {
            InitializeComponent();
            _renderSource = renderSource ?? throw new ArgumentNullException(nameof(renderSource));
            _renderState = _renderSource.GetRenderState();
        }

        private void SaveImageManagerForm_Load(object sender, EventArgs e)
        {
            // Инициализация UI
            cbFormat.SelectedIndex = 0; // PNG по умолчанию
            cbSSAA.SelectedIndex = 1;   // Низкое (2x) по умолчанию
            UpdateJpgQualityUI();

            // Загрузка превью
            try
            {
                previewPictureBox.Image = _renderSource.RenderPreview(_renderState, previewPictureBox.Width, previewPictureBox.Height);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить предпросмотр: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cbFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateJpgQualityUI();
        }

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
            int width = (int)nudWidth.Value;
            int height = (int)nudHeight.Value;
            string format = cbFormat.SelectedItem.ToString();
            int ssaaFactor = GetSsaaFactor();

            ImageFormat imageFormat = GetImageFormat(format);

            // ИСПРАВЛЕНИЕ: Формируем имя файла, используя данные из RenderState
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"{_renderState.FileNameDetails}_{timestamp}.{format.ToLower()}";

            using (var sfd = new SaveFileDialog
            {
                Filter = $"{format.ToUpper()} Files|*.{format.ToLower()}|All files|*.*",
                FileName = suggestedFileName, // Используем новое осмысленное имя
                Title = "Сохранить изображение"
            })
            {
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                SetUiState(false); // Блокируем UI
                _cts = new CancellationTokenSource();

                // Объявляем переменную как интерфейс и добавляем InvokeRequired для потокобезопасности
                IProgress<RenderProgress> progress = new Progress<RenderProgress>(p =>
                {
                    if (progressBar.IsHandleCreated && !progressBar.IsDisposed)
                    {
                        progressBar.Invoke((Action)(() => progressBar.Value = p.Percentage));
                    }
                    if (lblStatus.IsHandleCreated && !lblStatus.IsDisposed)
                    {
                        lblStatus.Invoke((Action)(() => lblStatus.Text = p.Status));
                    }
                });

                try
                {
                    // Передаем progress в метод рендеринга
                    Bitmap resultBitmap = await _renderSource.RenderHighResolutionAsync(_renderState, width, height, ssaaFactor, progress, _cts.Token);

                    _cts.Token.ThrowIfCancellationRequested();

                    // Вызываем Report, чтобы обновить UI уже из этого потока
                    progress.Report(new RenderProgress { Percentage = 100, Status = "Сохранение файла..." });
                    await Task.Run(() => SaveBitmap(resultBitmap, sfd.FileName, imageFormat), _cts.Token);
                    resultBitmap.Dispose();

                    MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                catch (OperationCanceledException)
                {
                    if (IsHandleCreated && !IsDisposed)
                    {
                        this.Invoke((Action)(() => {
                            lblStatus.Text = "Операция отменена";
                            progressBar.Value = 0;
                        }));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка при сохранении: {ex.Message}\n\n{ex.StackTrace}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    SetUiState(true); // Разблокируем UI
                    _cts?.Dispose();
                    _cts = null;
                }
            }
        }

        private void SaveBitmap(Bitmap bitmap, string filePath, ImageFormat format)
        {
            if (format == ImageFormat.Jpeg)
            {
                var qualityEncoder = Encoder.Quality;
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(qualityEncoder, (long)trackBarJpgQuality.Value);
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
            if (_cts != null && !_cts.IsCancellationRequested)
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
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        private void SetUiState(bool enabled)
        {
            pnlMain.Enabled = enabled;
            btnSave.Enabled = enabled;
            btnCancel.Text = enabled ? "Закрыть" : "Отмена";
            if (enabled)
            {
                progressBar.Value = 0;
                lblStatus.Text = "Готово";
            }
        }

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