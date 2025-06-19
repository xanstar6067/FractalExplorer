using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FractalDraving
{
    public partial class FractalJuliaBurningShip : FractalFormBase
    {
        private const decimal BS_MIN_RE = -2.0m;
        private const decimal BS_MAX_RE = 1.5m;
        private const decimal BS_MIN_IM = -1.5m;
        private const decimal BS_MAX_IM = 1.0m;
        private const int BS_PREVIEW_ITERATIONS = 75;

        private BurningShipCSelectorForm _burningShipCSelectorWindow;

        public FractalJuliaBurningShip()
        {
            InitializeComponent();
            this.Text = "Фрактал Горящий Корабль (Жюлиа)";
        }

        protected override FractalEngineBase CreateEngine()
        {
            return new JuliaBurningShipEngine();
        }

        protected override decimal BaseScale => 4.0m;
        protected override decimal InitialCenterX => 0.0m;
        protected override decimal InitialCenterY => 0.0m;

        protected override void OnPostInitialize()
        {
            mondelbrotClassicBox.Visible = false;
            mandelbrotPreviewPanel.Visible = true;
            lblRe.Visible = true;
            nudRe.Visible = true;
            lblIm.Visible = true;
            nudIm.Visible = true;

            mandelbrotPreviewCanvas.Click += mandelbrotCanvas_Click;
            mandelbrotPreviewCanvas.Paint += mandelbrotCanvas_Paint;

            Task.Run(() => RenderAndDisplayBurningShipSet());
        }

        protected override void UpdateEngineSpecificParameters()
        {
            _fractalEngine.C = new ComplexDecimal(nudRe.Value, nudIm.Value);
            if (mandelbrotPreviewCanvas.IsHandleCreated && !mandelbrotPreviewCanvas.IsDisposed)
            {
                mandelbrotPreviewCanvas.Invalidate();
            }
        }

        #region C-Selector Logic

        private void RenderAndDisplayBurningShipSet()
        {
            if (mandelbrotPreviewCanvas == null || mandelbrotPreviewCanvas.Width <= 0 || mandelbrotPreviewCanvas.Height <= 0) return;
            Bitmap bsImage = RenderBurningShipSetInternal(mandelbrotPreviewCanvas.Width, mandelbrotPreviewCanvas.Height, BS_PREVIEW_ITERATIONS);
            if (mandelbrotPreviewCanvas.IsHandleCreated && !mandelbrotPreviewCanvas.IsDisposed)
            {
                mandelbrotPreviewCanvas.Invoke((Action)(() =>
                {
                    mandelbrotPreviewCanvas.Image?.Dispose();
                    mandelbrotPreviewCanvas.Image = bsImage;
                }));
            }
            else
            {
                bsImage?.Dispose();
            }
        }

        private Bitmap RenderBurningShipSetInternal(int canvasWidth, int canvasHeight, int iterationsLimit)
        {
            Bitmap bmp = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format24bppRgb);
            var engine = new MandelbrotBurningShipEngine
            {
                MaxIterations = iterationsLimit,
                ThresholdSquared = 4m,
                Palette = GetPaletteMandelbrotClassicColor,
                Scale = (BS_MAX_RE - BS_MIN_RE),
                CenterX = (BS_MAX_RE + BS_MIN_RE) / 2,
                CenterY = (BS_MAX_IM + BS_MIN_IM) / 2
            };

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, canvasWidth, canvasHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            var tile = new TileInfo(0, 0, canvasWidth, canvasHeight);
            engine.RenderTile(bmpData, tile, canvasWidth, canvasHeight);
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        private void mandelbrotCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (mandelbrotPreviewCanvas?.Image == null) return;

            decimal reRange = BS_MAX_RE - BS_MIN_RE;
            decimal imRange = BS_MAX_IM - BS_MIN_IM;
            decimal currentCRe = nudRe.Value;
            decimal currentCIm = nudIm.Value;

            if (reRange > 0 && imRange > 0 && currentCRe >= BS_MIN_RE && currentCRe <= BS_MAX_RE &&
                currentCIm >= BS_MIN_IM && currentCIm <= BS_MAX_IM)
            {
                int markerX = (int)((currentCRe - BS_MIN_RE) / reRange * mandelbrotPreviewCanvas.Width);
                int markerY = (int)((BS_MAX_IM - currentCIm) / imRange * mandelbrotPreviewCanvas.Height);

                using (Pen markerPen = new Pen(Color.FromArgb(200, Color.LimeGreen), 1.5f))
                {
                    e.Graphics.DrawLine(markerPen, 0, markerY, mandelbrotPreviewCanvas.Width, markerY);
                    e.Graphics.DrawLine(markerPen, markerX, 0, markerX, mandelbrotPreviewCanvas.Height);
                }
            }
        }

        private void mandelbrotCanvas_Click(object sender, EventArgs e)
        {
            double initialRe = (double)nudRe.Value;
            double initialIm = (double)nudIm.Value;

            if (_burningShipCSelectorWindow == null || _burningShipCSelectorWindow.IsDisposed)
            {
                _burningShipCSelectorWindow = new BurningShipCSelectorForm(this, initialRe, initialIm);
                _burningShipCSelectorWindow.CoordinatesSelected += (re, im) => {
                    nudRe.Value = (decimal)re;
                    nudIm.Value = (decimal)im;
                };
                _burningShipCSelectorWindow.FormClosed += (s, args) => { _burningShipCSelectorWindow = null; };
                _burningShipCSelectorWindow.Show(this);
            }
            else
            {
                _burningShipCSelectorWindow.Activate();
                _burningShipCSelectorWindow.SetSelectedCoordinates(initialRe, initialIm, true);
            }
        }
        #endregion
    }
}