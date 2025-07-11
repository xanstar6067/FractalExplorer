using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Engines.EngineImplementations;
using FractalExplorer.Engines.EngineInterfaces;
using FractalExplorer.Forms.SelectorsForms.Selector;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Globalization;

namespace FractalExplorer.Projects
{
    public partial class FractalJuliaBurningShip : FractalMandelbrotFamilyForm
    {
        private const decimal BURNING_SHIP_MIN_REAL = -2.0m;
        private const decimal BURNING_SHIP_MAX_REAL = 1.5m;
        private const decimal BURNING_SHIP_MIN_IMAGINARY = -1.0m;
        private const decimal BURNING_SHIP_MAX_IMAGINARY = 1.5m;
        private const int BURNING_SHIP_PREVIEW_ITERATIONS = 75;
        private BurningShipCSelectorForm _burningShipCSelectorWindow;

        public FractalJuliaBurningShip()
        {
            Text = "Фрактал Горящий Корабль (Жюлиа)";
        }

        protected override FractalType GetFractalType()
        {
            return FractalType.JuliaBurningShip;
        }

        protected override BigDecimal BaseScale => new BigDecimal(4.0m);
        protected override decimal InitialCenterX => 0.0m;
        protected override decimal InitialCenterY => 0.0m;

        protected override void OnPostInitialize()
        {
            mandelbrotPreviewPanel.Visible = true;
            lblRe.Visible = true; nudRe.Visible = true;
            lblIm.Visible = true; nudIm.Visible = true;
            nudRe.Value = -1.7551867961883m;
            nudIm.Value = 0.01068m;

            var previewCanvas = Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault() as PictureBox;
            if (previewCanvas != null)
            {
                previewCanvas.Click += mandelbrotCanvas_Click;
                previewCanvas.Paint += mandelbrotCanvas_Paint;
                Task.Run(() => RenderAndDisplayBurningShipSet());
            }
        }

        protected override string GetSaveFileNameDetails()
        {
            string reString = nudRe.Value.ToString("F15", CultureInfo.InvariantCulture).Replace(".", "_");
            string imString = nudIm.Value.ToString("F15", CultureInfo.InvariantCulture).Replace(".", "_");
            return $"burningship_julia_re{reString}_im{imString}";
        }

        private void RenderAndDisplayBurningShipSet()
        {
            var previewCanvas = Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault() as PictureBox;
            if (previewCanvas == null || previewCanvas.Width <= 0 || previewCanvas.Height <= 0) return;

            Bitmap burningShipImage = RenderBurningShipSetInternal(previewCanvas.Width, previewCanvas.Height);

            if (previewCanvas.IsHandleCreated && !previewCanvas.IsDisposed)
            {
                previewCanvas.Invoke(() =>
                {
                    previewCanvas.Image?.Dispose();
                    previewCanvas.Image = burningShipImage;
                });
            }
            else
            {
                burningShipImage?.Dispose();
            }
        }

        private Bitmap RenderBurningShipSetInternal(int canvasWidth, int canvasHeight)
        {
            var engine = new EngineDecimal();
            engine.MaxIterations = BURNING_SHIP_PREVIEW_ITERATIONS;
            engine.MaxColorIterations = BURNING_SHIP_PREVIEW_ITERATIONS;
            engine.Palette = GetPaletteMandelbrotClassicColor;

            var options = new RenderOptions
            {
                Width = canvasWidth,
                Height = canvasHeight,
                FractalType = FractalType.MandelbrotBurningShip,
                CenterX = ((BURNING_SHIP_MAX_REAL + BURNING_SHIP_MIN_REAL) / 2).ToString(CultureInfo.InvariantCulture),
                CenterY = ((BURNING_SHIP_MAX_IMAGINARY + BURNING_SHIP_MIN_IMAGINARY) / 2).ToString(CultureInfo.InvariantCulture),
                Scale = (BURNING_SHIP_MAX_REAL - BURNING_SHIP_MIN_REAL).ToString(CultureInfo.InvariantCulture),
                NumThreads = Environment.ProcessorCount
            };

            return engine.Render(options, CancellationToken.None);
        }

        private void mandelbrotCanvas_Paint(object sender, PaintEventArgs e)
        {
            var previewCanvas = sender as PictureBox;
            if (previewCanvas?.Image == null) return;
            decimal realRange = BURNING_SHIP_MAX_REAL - BURNING_SHIP_MIN_REAL;
            decimal imaginaryRange = BURNING_SHIP_MAX_IMAGINARY - BURNING_SHIP_MIN_IMAGINARY;
            if (realRange <= 0 || imaginaryRange <= 0) return;
            decimal currentCReal = nudRe.Value;
            decimal currentCImaginary = nudIm.Value;

            if (currentCReal >= BURNING_SHIP_MIN_REAL && currentCReal <= BURNING_SHIP_MAX_REAL &&
                currentCImaginary >= BURNING_SHIP_MIN_IMAGINARY && currentCImaginary <= BURNING_SHIP_MAX_IMAGINARY)
            {
                int markerX = (int)((currentCReal - BURNING_SHIP_MIN_REAL) / realRange * previewCanvas.Width);
                int markerY = (int)((BURNING_SHIP_MAX_IMAGINARY - currentCImaginary) / imaginaryRange * previewCanvas.Height);
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

            if (_burningShipCSelectorWindow == null || _burningShipCSelectorWindow.IsDisposed)
            {
                _burningShipCSelectorWindow = new BurningShipCSelectorForm(this, initialReal, initialImaginary);
                _burningShipCSelectorWindow.CoordinatesSelected += (re, im) =>
                {
                    nudRe.Value = (decimal)re;
                    nudIm.Value = (decimal)im;
                };
                _burningShipCSelectorWindow.FormClosed += (s, args) => { _burningShipCSelectorWindow = null; };
                _burningShipCSelectorWindow.Show(this);
            }
            else
            {
                _burningShipCSelectorWindow.Activate();
                _burningShipCSelectorWindow.SetSelectedCoordinates(initialReal, initialImaginary, true);
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
        public override string FractalTypeIdentifier => "JuliaBurningShip";
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