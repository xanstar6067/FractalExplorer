using FractalExplorer.Engines;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Windows.Forms;
using System.Text.Json;
using System.Linq;
using FractalExplorer.SelectorsForms;
using System.Collections.Generic;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes; // Добавлено для List<>

namespace FractalExplorer
{
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

        private SerpinskyPaletteManager _paletteManager;
        private ColorConfigurationSerpinskyForm _colorConfigForm;
        #endregion

        #region Constructor
        public FractalSerpinsky()
        {
            InitializeComponent();
            _engine = new FractalSerpinskyEngine();
            _paletteManager = new SerpinskyPaletteManager();
            InitializeCustomComponents();
        }
        #endregion

        #region UI Initialization
        private void InitializeCustomComponents()
        {
            renderTimer = new System.Windows.Forms.Timer { Interval = 300 };
            renderTimer.Tick += RenderTimer_Tick;

            int cores = Environment.ProcessorCount;
            cbCPUThreads.Items.Clear();
            for (int i = 1; i <= cores; i++)
            {
                cbCPUThreads.Items.Add(i);
            }
            cbCPUThreads.Items.Add("Auto");
            cbCPUThreads.SelectedItem = "Auto";

            Load += (s, e) =>
            {
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
        private void FractalType_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox activeCheckBox = sender as CheckBox;
            if (activeCheckBox == null || !activeCheckBox.Checked) return;

            if (activeCheckBox == FractalTypeIsGeometry)
            {
                FractalTypeIsChaos.Checked = false;
                nudIterations.Maximum = 20;
                nudIterations.Minimum = 0;
                if (nudIterations.Value >= 20 || nudIterations.Value < 0) nudIterations.Value = 8;
            }
            else // FractalTypeIsChaos
            {
                FractalTypeIsGeometry.Checked = false;
                nudIterations.Maximum = int.MaxValue;
                nudIterations.Minimum = 1000;
                if (nudIterations.Value < 1000) nudIterations.Value = 50000;
            }
            ScheduleRender();
        }

        private void color_configurations_Click(object sender, EventArgs e)
        {
            if (_colorConfigForm == null || _colorConfigForm.IsDisposed)
            {
                _colorConfigForm = new ColorConfigurationSerpinskyForm(_paletteManager);
                _colorConfigForm.PaletteApplied += OnPaletteApplied;
                _colorConfigForm.FormClosed += (s, args) => _colorConfigForm = null;
                _colorConfigForm.Show(this);
            }
            else
            {
                _colorConfigForm.Activate();
            }
        }

        private void OnPaletteApplied(object sender, EventArgs e)
        {
            ApplyActivePalette();
            ScheduleRender();
        }

        private void btnRender_Click(object sender, EventArgs e)
        {
            previewRenderCts?.Cancel();
            renderTimer.Stop();
            RenderTimer_Tick(sender, e);
        }

        private void btnStateManager_Click(object sender, EventArgs e)
        {
            using (var dialog = new Forms.SaveLoadDialogForm(this))
            {
                dialog.ShowDialog(this);
            }
        }

        private void abortRender_Click(object sender, EventArgs e)
        {
            if (isRenderingPreview) previewRenderCts?.Cancel();
            if (isHighResRendering) highResRenderCts?.Cancel();
        }
        #endregion

        #region Rendering Logic
        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            if (sender == nudZoom)
            {
                currentZoom = (double)nudZoom.Value;
            }
            ScheduleRender();
        }

        private void ScheduleRender()
        {
            if (isHighResRendering || WindowState == FormWindowState.Minimized) return;
            previewRenderCts?.Cancel();
            renderTimer.Stop();
            renderTimer.Start();
        }

        private async void RenderTimer_Tick(object sender, EventArgs e)
        {
            renderTimer.Stop();
            if (isHighResRendering || isRenderingPreview) return;

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

                await Task.Run(() => _engine.RenderToBuffer(
                    pixelBuffer, renderWidth, renderHeight, bitmapData.Stride, 4,
                    numThreads, token, (progress) => UpdateProgressBar(progressBarSerpinsky, progress)), token);

                token.ThrowIfCancellationRequested();
                Marshal.Copy(pixelBuffer, 0, bitmapData.Scan0, pixelBuffer.Length);
                bitmap.UnlockBits(bitmapData);

                Bitmap oldImage = canvasBitmap;
                canvasBitmap = bitmap;
                renderedZoom = currentZoom;
                renderedCenterX = centerX;
                renderedCenterY = centerY;
                canvasSerpinsky.Invalidate();
                oldImage?.Dispose();
            }
            catch (OperationCanceledException) { /* Ignore */ }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isRenderingPreview = false;
                if (!isHighResRendering) SetMainControlsEnabled(true);
                UpdateAbortButtonState();
                UpdateProgressBar(progressBarSerpinsky, 0);
            }
        }
        #endregion

        #region Canvas Interaction
        private void CanvasSerpinsky_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(_engine.BackgroundColor);
            if (canvasBitmap == null || canvasSerpinsky.Width <= 0 || canvasSerpinsky.Height <= 0) return;

            double renderedAspectRatio = (double)canvasBitmap.Width / canvasBitmap.Height;
            double renderedViewHeightWorld = 1.0 / renderedZoom;
            double renderedViewWidthWorld = renderedViewHeightWorld * renderedAspectRatio;
            double renderedMinReal = renderedCenterX - renderedViewWidthWorld / 2.0;
            double renderedMaxImaginary = renderedCenterY + renderedViewHeightWorld / 2.0;

            double currentAspectRatio = (double)canvasSerpinsky.Width / canvasSerpinsky.Height;
            double currentViewHeightWorld = 1.0 / currentZoom;
            double currentViewWidthWorld = currentViewHeightWorld * currentAspectRatio;
            double currentMinReal = centerX - currentViewWidthWorld / 2.0;
            double currentMaxImaginary = centerY + currentViewHeightWorld / 2.0;

            float offsetX = (float)(((renderedMinReal - currentMinReal) / currentViewWidthWorld) * canvasSerpinsky.Width);
            float offsetY = (float)(((currentMaxImaginary - renderedMaxImaginary) / currentViewHeightWorld) * canvasSerpinsky.Height);
            float scaleWidth = (float)((renderedViewWidthWorld / currentViewWidthWorld) * canvasSerpinsky.Width);
            float scaleHeight = (float)((renderedViewHeightWorld / currentViewHeightWorld) * canvasSerpinsky.Height);

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(canvasBitmap, new RectangleF(offsetX, offsetY, scaleWidth, scaleHeight));
        }

        private void CanvasSerpinsky_MouseWheel(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || canvasSerpinsky.Width <= 0) return;
            double zoomFactor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
            PointF worldPosition = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            currentZoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, currentZoom * zoomFactor));
            PointF newWorldPosition = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            centerX += worldPosition.X - newWorldPosition.X;
            centerY += worldPosition.Y - newWorldPosition.Y;
            canvasSerpinsky.Invalidate();
            if (nudZoom.Value != (decimal)currentZoom) nudZoom.Value = (decimal)currentZoom;
            else ScheduleRender();
        }

        private void CanvasSerpinsky_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { panning = true; panStart = e.Location; }
        }

        private void CanvasSerpinsky_MouseMove(object sender, MouseEventArgs e)
        {
            if (!panning) return;
            PointF worldBefore = ScreenToWorld(panStart, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            PointF worldAfter = ScreenToWorld(e.Location, canvasSerpinsky.Width, canvasSerpinsky.Height, currentZoom, centerX, centerY);
            centerX += worldBefore.X - worldAfter.X;
            centerY += worldBefore.Y - worldAfter.Y;
            panStart = e.Location;
            canvasSerpinsky.Invalidate();
            ScheduleRender();
        }

        private void CanvasSerpinsky_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) panning = false;
        }

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
        private void UpdateEngineParameters()
        {
            _engine.RenderMode = FractalTypeIsGeometry.Checked ? SerpinskyRenderMode.Geometric : SerpinskyRenderMode.Chaos;
            _engine.Iterations = (int)nudIterations.Value;
            _engine.Zoom = currentZoom;
            _engine.CenterX = centerX;
            _engine.CenterY = centerY;
            ApplyActivePalette();
        }

        private void ApplyActivePalette()
        {
            if (_engine == null || _paletteManager?.ActivePalette == null) return;
            var activePalette = _paletteManager.ActivePalette;
            _engine.ColorMode = SerpinskyColorMode.CustomColor;
            _engine.FractalColor = activePalette.FractalColor;
            _engine.BackgroundColor = activePalette.BackgroundColor;
        }

        private async void btnSavePNG_Click(object sender, EventArgs e)
        {
            if (isRenderingPreview) previewRenderCts?.Cancel();
            if (isHighResRendering) return;
            int outputWidth = (int)nudW2.Value;
            int outputHeight = (int)nudH2.Value;
            using (var saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"serpinski_{DateTime.Now:yyyyMMdd_HHmmss}.png" })
            {
                if (saveDialog.ShowDialog() != DialogResult.OK) return;
                isHighResRendering = true;
                SetMainControlsEnabled(false);
                UpdateAbortButtonState();
                progressPNGSerpinsky.Visible = true;
                UpdateProgressBar(progressPNGSerpinsky, 0);
                highResRenderCts = new CancellationTokenSource();
                CancellationToken token = highResRenderCts.Token;
                var renderEngine = new FractalSerpinskyEngine();
                UpdateEngineParameters();
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
                    Bitmap highResBitmap = await Task.Run(() =>
                    {
                        var bitmap = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb);
                        var bmpData = bitmap.LockBits(new Rectangle(0, 0, outputWidth, outputHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        var pixelBuffer = new byte[bmpData.Stride * outputHeight];
                        renderEngine.RenderToBuffer(
                            pixelBuffer, outputWidth, outputHeight, bmpData.Stride, 4,
                            numThreads, token, (progress) => UpdateProgressBar(progressPNGSerpinsky, progress));
                        token.ThrowIfCancellationRequested();
                        Marshal.Copy(pixelBuffer, 0, bmpData.Scan0, pixelBuffer.Length);
                        bitmap.UnlockBits(bmpData);
                        return bitmap;
                    }, token);
                    highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                    MessageBox.Show("Изображение сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    highResBitmap.Dispose();
                }
                catch (OperationCanceledException) { MessageBox.Show("Сохранение было отменено.", "Отмена", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                catch (Exception ex) { MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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

        private int GetThreadCount()
        {
            return cbCPUThreads.SelectedItem?.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbCPUThreads.SelectedItem);
        }

        private void SetMainControlsEnabled(bool enabled)
        {
            foreach (Control ctrl in panel1.Controls)
            {
                if (ctrl != abortRender) ctrl.Enabled = enabled;
            }
            UpdateAbortButtonState();
        }

        private void UpdateAbortButtonState()
        {
            if (IsHandleCreated)
            {
                Invoke((Action)(() => abortRender.Enabled = isRenderingPreview || isHighResRendering));
            }
        }

        private void UpdateProgressBar(ProgressBar progressBar, int percentage)
        {
            if (progressBar.IsHandleCreated)
            {
                progressBar.Invoke((Action)(() => progressBar.Value = Math.Min(100, Math.Max(0, percentage))));
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            previewRenderCts?.Cancel(); previewRenderCts?.Dispose();
            highResRenderCts?.Cancel(); highResRenderCts?.Dispose();
            renderTimer?.Stop(); renderTimer?.Dispose();
            canvasBitmap?.Dispose();
            base.OnFormClosed(e);
        }
        #endregion

        #region ISaveLoadCapableFractal Implementation
        public string FractalTypeIdentifier => "Serpinsky";
        public Type ConcreteSaveStateType => typeof(SerpinskySaveState);
        protected class SerpinskyPreviewParams
        {
            public SerpinskyRenderMode RenderMode { get; set; }
            public int Iterations { get; set; }
            public double Zoom { get; set; }
            public double CenterX { get; set; }
            public double CenterY { get; set; }
            public Color FractalColor { get; set; }
            public Color BackgroundColor { get; set; }
        }

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

            var previewParams = new SerpinskyPreviewParams
            {
                RenderMode = state.RenderMode,
                Zoom = state.Zoom,
                CenterX = state.CenterX,
                CenterY = state.CenterY,
                FractalColor = state.FractalColor,
                BackgroundColor = state.BackgroundColor,
                Iterations = (state.RenderMode == SerpinskyRenderMode.Geometric)
                    ? Math.Min(state.Iterations, 5)
                    : Math.Min(state.Iterations, 20000)
            };

            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.Converters.Add(new Core.JsonColorConverter());
            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams, jsonOptions);

            return state;
        }

        public void LoadState(FractalSaveStateBase stateBase)
        {
            if (stateBase is SerpinskySaveState state)
            {
                // Останавливаем текущие рендеры
                previewRenderCts?.Cancel();
                renderTimer.Stop();

                // 1. Создаем временный объект палитры из загруженного состояния.
                // Имя палитры можно составить из имени сохранения, чтобы было понятно.
                var loadedPalette = new SerpinskyColorPalette
                {
                    Name = $"Загружено: {state.SaveName}",
                    FractalColor = state.FractalColor,
                    BackgroundColor = state.BackgroundColor,
                    IsBuiltIn = false // Важно, чтобы ее можно было рассматривать как временную/пользовательскую
                };

                // 2. Устанавливаем эту палитру как активную в менеджере.
                // Мы не добавляем ее в общий список, чтобы не засорять его,
                // просто делаем ее текущей.
                _paletteManager.ActivePalette = loadedPalette;

                // 3. Устанавливаем RenderMode, чтобы обновить ограничения nudIterations
                if (state.RenderMode == SerpinskyRenderMode.Geometric)
                {
                    FractalTypeIsGeometry.Checked = true;
                }
                else
                {
                    FractalTypeIsChaos.Checked = true;
                }

                // 4. Безопасно устанавливаем значения контролов
                decimal safeIterations = Math.Max(nudIterations.Minimum, Math.Min(nudIterations.Maximum, state.Iterations));
                nudIterations.Value = safeIterations;

                this.centerX = state.CenterX;
                this.centerY = state.CenterY;
                this.currentZoom = state.Zoom;
                decimal safeZoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, (decimal)state.Zoom));
                nudZoom.Value = safeZoom;

                // 5. Вызываем UpdateEngineParameters, который теперь вызовет ApplyActivePalette
                // и применит цвета из нашей новой, только что установленной `loadedPalette`.
                UpdateEngineParameters();

                // 6. Запускаем рендер.
                ScheduleRender();
            }
            else
            {
                MessageBox.Show("Несовместимый тип состояния для загрузки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public Bitmap RenderPreview(FractalSaveStateBase state, int previewWidth, int previewHeight)
        {
            if (string.IsNullOrEmpty(state.PreviewParametersJson))
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkGray); TextRenderer.DrawText(g, "Нет данных", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

            SerpinskyPreviewParams previewParams;
            try
            {
                var jsonOptions = new JsonSerializerOptions();
                jsonOptions.Converters.Add(new Core.JsonColorConverter());
                previewParams = JsonSerializer.Deserialize<SerpinskyPreviewParams>(state.PreviewParametersJson, jsonOptions);
            }
            catch (Exception)
            {
                var bmpError = new Bitmap(previewWidth, previewHeight);
                using (var g = Graphics.FromImage(bmpError)) { g.Clear(Color.DarkRed); TextRenderer.DrawText(g, "Ошибка параметров", Font, new Rectangle(0, 0, previewWidth, previewHeight), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
                return bmpError;
            }

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

            previewEngine.RenderToBuffer(buffer, previewWidth, previewHeight, bmpData.Stride, 4, 1, CancellationToken.None, progress => { });

            Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        public List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<SerpinskySaveState>(this.FractalTypeIdentifier);
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        public void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<SerpinskySaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }
        #endregion
    }
}