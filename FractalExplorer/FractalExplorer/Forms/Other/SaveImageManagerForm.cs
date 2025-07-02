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
        private bool _isRendering = false;

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
                    int width = (int)nudWidth.Value;
                    int height = (int)nudHeight.Value;
                    int ssaaFactor = GetSsaaFactor();
                    ImageFormat imageFormat = GetImageFormat(format);

                    Bitmap resultBitmap = await _renderSource.RenderHighResolutionAsync(_renderState, width, height, ssaaFactor, progress, _cts.Token);

                    _cts.Token.ThrowIfCancellationRequested();

                    progress.Report(new RenderProgress { Percentage = 100, Status = "Сохранение файла..." });
                    await Task.Run(() => SaveBitmap(resultBitmap, sfd.FileName, imageFormat), _cts.Token);
                    resultBitmap.Dispose();

                    MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                // ИСПРАВЛЕНИЕ: Ловим AggregateException
                catch (AggregateException ae)
                {
                    // Проверяем, что все внутренние исключения - это исключения отмены.
                    // Если это так, то считаем это штатной отменой операции.
                    ae.Handle(ex => ex is OperationCanceledException);
                }
                catch (OperationCanceledException)
                {
                    // Этот блок по-прежнему нужен для случаев, когда отмена происходит
                    // не внутри Parallel.For (например, между рендерингом и сохранением).
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка при сохранении: {ex.Message}\n\n{ex.StackTrace}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    _isRendering = false;
                    SetUiState(true);
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
            if (_isRendering && _cts != null && !_cts.IsCancellationRequested)
            {
                // Если идет рендер, просто отменяем его.
                // async-метод btnSave_Click сам обработает исключение и восстановит UI.
                _cts.Cancel();
            }
            else
            {
                // Если рендер не идет, кнопка работает как "Закрыть".
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void SaveImageManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // ИСПРАВЛЕНИЕ: Если рендер еще идет в момент закрытия формы, отменяем его.
            if (_isRendering && _cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        // ИСПРАВЛЕНИЕ: Самая важная часть. Делаем метод "пуленепробиваемым".
        private void SetUiState(bool enabled)
        {
            // Проверяем, не уничтожена ли форма, перед доступом к контролам.
            if (this.IsDisposed) return;

            // Invoke нужен на случай, если этот метод будет вызван не из UI потока.
            // В нашем случае это избыточно, но является хорошей практикой.
            this.Invoke((Action)(() => {
                if (this.IsDisposed) return; // Повторная проверка на всякий случай

                pnlMain.Enabled = enabled;
                btnSave.Enabled = enabled;

                // Кнопка "Отмена" меняет свою функцию
                btnCancel.Text = enabled ? "Закрыть" : "Отмена";

                if (enabled)
                {
                    progressBar.Value = 0;
                    lblStatus.Text = "Готово";
                }
            }));
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