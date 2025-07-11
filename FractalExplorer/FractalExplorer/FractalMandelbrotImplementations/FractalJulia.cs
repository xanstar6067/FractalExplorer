using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Engines.EngineImplementations;
using FractalExplorer.Engines.EngineInterfaces;
using FractalExplorer.Forms.SelectorsForms.Selector;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Drawing.Imaging;
using System.Globalization;

namespace FractalExplorer.Projects
{
    public partial class FractalJulia : FractalMandelbrotFamilyForm
    {
        private const decimal MANDELBROT_MIN_RE = -2.0m;
        private const decimal MANDELBROT_MAX_RE = 1.0m;
        private const decimal MANDELBROT_MIN_IM = -1.2m;
        private const decimal MANDELBROT_MAX_IM = 1.2m;
        private const int MANDELBROT_PREVIEW_ITERATIONS = 75;
        private JuliaMandelbrotSelectorForm _mandelbrotCSelectorWindow;

        public FractalJulia()
        {
            Text = "Фрактал Жюлиа";
        }

        protected override FractalType GetFractalType()
        {
            return FractalType.Julia;
        }

        protected override BigDecimal BaseScale => new BigDecimal(4.0m);
        protected override decimal InitialCenterX => 0.0m;
        protected override decimal InitialCenterY => 0.0m;

        protected override void OnPostInitialize()
        {
            mandelbrotPreviewPanel.Visible = true;
            lblRe.Visible = true;
            nudRe.Visible = true;
            lblIm.Visible = true;
            nudIm.Visible = true;

            var previewCanvas = Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault() as PictureBox;
            if (previewCanvas != null)
            {
                previewCanvas.Click += mandelbrotCanvas_Click;
                previewCanvas.Paint += mandelbrotCanvas_Paint;
                Task.Run(() => RenderAndDisplayMandelbrotSet());
            }
        }

        protected override string GetSaveFileNameDetails()
        {
            string reString = nudRe.Value.ToString("F15", CultureInfo.InvariantCulture).Replace(".", "_");
            string imString = nudIm.Value.ToString("F15", CultureInfo.InvariantCulture).Replace(".", "_");
            return $"julia_re{reString}_im{imString}";
        }

        private void RenderAndDisplayMandelbrotSet()
        {
            var previewCanvas = Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault() as PictureBox;
            if (previewCanvas == null || previewCanvas.Width <= 0 || previewCanvas.Height <= 0) return;

            Bitmap mandelbrotImage = RenderMandelbrotSetInternal(previewCanvas.Width, previewCanvas.Height);

            if (previewCanvas.IsHandleCreated && !previewCanvas.IsDisposed)
            {
                previewCanvas.Invoke(() =>
                {
                    previewCanvas.Image?.Dispose();
                    previewCanvas.Image = mandelbrotImage;
                });
            }
            else
            {
                mandelbrotImage?.Dispose();
            }
        }

        private Bitmap RenderMandelbrotSetInternal(int canvasWidth, int canvasHeight)
        {
            // --- Используем новый движок для рендера превью ---
            var engine = new EngineDecimal();
            engine.MaxIterations = MANDELBROT_PREVIEW_ITERATIONS;
            engine.MaxColorIterations = MANDELBROT_PREVIEW_ITERATIONS;
            engine.Palette = GetPaletteMandelbrotClassicColor;

            var options = new RenderOptions
            {
                Width = canvasWidth,
                Height = canvasHeight,
                FractalType = FractalType.Mandelbrot,
                CenterX = ((MANDELBROT_MAX_RE + MANDELBROT_MIN_RE) / 2).ToString(CultureInfo.InvariantCulture),
                CenterY = ((MANDELBROT_MAX_IM + MANDELBROT_MIN_IM) / 2).ToString(CultureInfo.InvariantCulture),
                Scale = (MANDELBROT_MAX_RE - MANDELBROT_MIN_RE).ToString(CultureInfo.InvariantCulture),
                NumThreads = Environment.ProcessorCount
            };

            return engine.Render(options, CancellationToken.None);
        }

        private void mandelbrotCanvas_Paint(object sender, PaintEventArgs e)
        {
            var previewCanvas = sender as PictureBox;
            if (previewCanvas?.Image == null) return;
            decimal reRange = MANDELBROT_MAX_RE - MANDELBROT_MIN_RE;
            decimal imRange = MANDELBROT_MAX_IM - MANDELBROT_MIN_IM;
            if (reRange <= 0 || imRange <= 0) return;
            decimal currentCReal = nudRe.Value;
            decimal currentCImaginary = nudIm.Value;
            if (currentCReal >= MANDELBROT_MIN_RE && currentCReal <= MANDELBROT_MAX_RE && currentCImaginary >= MANDELBROT_MIN_IM && currentCImaginary <= MANDELBROT_MAX_IM)
            {
                int markerX = (int)((currentCReal - MANDELBROT_MIN_RE) / reRange * previewCanvas.Width);
                int markerY = (int)((MANDELBROT_MAX_IM - currentCImaginary) / imRange * previewCanvas.Height);
                using (Pen markerPen = new Pen(Color.FromArgb(200, Color.LimeGreen), 1.5f))
                {
                    e.Graphics.DrawLine(markerPen, 0, markerY, previewCanvas.Width, markerY);
                    e.Graphics.DrawLine(markerPen, markerX, 0, markerX, previewCanvas.Height);
                }
            }
        }

        private void mandelbrotCanvas_Click(object sender, EventArgs e)
        {
            double initialReal = (double)nudRe.Value;
            double initialImaginary = (double)nudIm.Value;

            if (_mandelbrotCSelectorWindow == null || _mandelbrotCSelectorWindow.IsDisposed)
            {
                _mandelbrotCSelectorWindow = new JuliaMandelbrotSelectorForm(this, initialReal, initialImaginary);
                _mandelbrotCSelectorWindow.CoordinatesSelected += (re, im) =>
                {
                    nudRe.Value = (decimal)re;
                    nudIm.Value = (decimal)im;
                };
                _mandelbrotCSelectorWindow.FormClosed += (s, args) => { _mandelbrotCSelectorWindow = null; };
                _mandelbrotCSelectorWindow.Show(this);
            }
            else
            {
                _mandelbrotCSelectorWindow.Activate();
                _mandelbrotCSelectorWindow.SetSelectedCoordinates(initialReal, initialImaginary, true);
            }
        }

        private Color GetPaletteMandelbrotClassicColor(int iter, int maxIter, int maxColorIterations)
        {
            if (iter == maxIter) return Color.Black;
            double tClassic = (double)iter / maxIter;
            byte r, g, b;
            if (tClassic < 0.5) { double t = tClassic * 2; r = (byte)(t * 200); g = (byte)(t * 50); b = (byte)(t * 30); }
            else { double t = (tClassic - 0.5) * 2; r = (byte)(200 + t * 55); g = (byte)(50 + t * 205); b = (byte)(30 + t * 225); }
            return Color.FromArgb(r, g, b);
        }

        #region ISaveLoadCapableFractal Overrides
        public override string FractalTypeIdentifier => "Julia";
        public override Type ConcreteSaveStateType => typeof(JuliaFamilySaveState);
        public override List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<JuliaFamilySaveState>(this.FractalTypeIdentifier);
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }
        public override void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<JuliaFamilySaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }
        #endregion
    }
}