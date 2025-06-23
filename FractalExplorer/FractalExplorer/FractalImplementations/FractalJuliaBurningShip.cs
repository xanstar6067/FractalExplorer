using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Resources;
using FractalExplorer.Selectors;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FractalExplorer.Projects
{
    public partial class FractalJuliaBurningShip : FractalFormBase
    {
        private const decimal BS_MIN_RE = -2.0m;
        private const decimal BS_MAX_RE = 1.5m;
        private const decimal BS_MIN_IM = -1.0m;
        private const decimal BS_MAX_IM = 1.5m;
        private const int BS_PREVIEW_ITERATIONS = 75;

        private BurningShipCSelectorForm _burningShipCSelectorWindow;

        public FractalJuliaBurningShip()
        {
            Text = "Фрактал Горящий Корабль (Жюлиа)";
        }

        protected override FractalMondelbrotBaseEngine CreateEngine()
        {
            return new JuliaBurningShipEngine();
        }

        protected override decimal BaseScale => 4.0m;
        protected override decimal InitialCenterX => 0.0m;
        protected override decimal InitialCenterY => 0.0m;

        protected override void OnPostInitialize()
        {
            var classicBox = this.Controls.Find("mondelbrotClassicBox", true).FirstOrDefault();
            if (classicBox != null) classicBox.Visible = false;

            var previewPanel = this.Controls.Find("mandelbrotPreviewPanel", true).FirstOrDefault();
            if (previewPanel != null) previewPanel.Visible = true;

            this.Controls.Find("lblRe", true).FirstOrDefault()?.Show();
            nudRe.Visible = true;
            this.Controls.Find("lblIm", true).FirstOrDefault()?.Show();
            nudIm.Visible = true;

            nudRe.Value = -1.7551867961883m;
            nudIm.Value = 0.01068m;

            var previewCanvas = this.Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault() as PictureBox;
            if (previewCanvas != null)
            {
                previewCanvas.Click += mandelbrotCanvas_Click;
                previewCanvas.Paint += mandelbrotCanvas_Paint;
                Task.Run(() => RenderAndDisplayBurningShipSet());
            }
        }

        protected override void UpdateEngineSpecificParameters()
        {
            _fractalEngine.C = new ComplexDecimal(nudRe.Value, nudIm.Value);
            var previewCanvas = this.Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault();
            if (previewCanvas != null && previewCanvas.IsHandleCreated && !previewCanvas.IsDisposed)
            {
                previewCanvas.Invalidate();
            }
        }

        #region C-Selector Logic

        private void RenderAndDisplayBurningShipSet()
        {
            var previewCanvas = this.Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault() as PictureBox;
            if (previewCanvas == null || previewCanvas.Width <= 0 || previewCanvas.Height <= 0) return;
            Bitmap bsImage = RenderBurningShipSetInternal(previewCanvas.Width, previewCanvas.Height, BS_PREVIEW_ITERATIONS);
            if (previewCanvas.IsHandleCreated && !previewCanvas.IsDisposed)
            {
                previewCanvas.Invoke((Action)(() =>
                {
                    previewCanvas.Image?.Dispose();
                    previewCanvas.Image = bsImage;
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
                // ИСПОЛЬЗУЕМ ЛОКАЛЬНУЮ ВЕРСИЮ МЕТОДА
                Palette = GetPaletteMandelbrotClassicColor,
                Scale = BS_MAX_RE - BS_MIN_RE,
                CenterX = (BS_MAX_RE + BS_MIN_RE) / 2,
                CenterY = (BS_MAX_IM + BS_MIN_IM) / 2
            };

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, canvasWidth, canvasHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int bytes = Math.Abs(bmpData.Stride) * canvasHeight;
            byte[] buffer = new byte[bytes];
            int bytesPerPixel = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            var tile = new TileInfo(0, 0, canvasWidth, canvasHeight);

            engine.RenderTile(buffer, bmpData.Stride, bytesPerPixel, tile, canvasWidth, canvasHeight);

            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, bytes);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private void mandelbrotCanvas_Paint(object sender, PaintEventArgs e)
        {
            var previewCanvas = sender as PictureBox;
            if (previewCanvas?.Image == null) return;

            decimal reRange = BS_MAX_RE - BS_MIN_RE;
            decimal imRange = BS_MAX_IM - BS_MIN_IM;
            decimal currentCRe = nudRe.Value;
            decimal currentCIm = nudIm.Value;

            if (reRange > 0 && imRange > 0 && currentCRe >= BS_MIN_RE && currentCRe <= BS_MAX_RE &&
                currentCIm >= BS_MIN_IM && currentCIm <= BS_MAX_IM)
            {
                int markerX = (int)((currentCRe - BS_MIN_RE) / reRange * previewCanvas.Width);
                int markerY = (int)((BS_MAX_IM - currentCIm) / imRange * previewCanvas.Height);

                using (Pen markerPen = new Pen(Color.FromArgb(200, Color.LimeGreen), 1.5f))
                {
                    e.Graphics.DrawLine(markerPen, 0, markerY, previewCanvas.Width, markerY);
                    e.Graphics.DrawLine(markerPen, markerX, 0, markerX, previewCanvas.Height);
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

        protected override string GetSaveFileNameDetails()
        {
            string reStr = nudRe.Value.ToString("F15", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            string imStr = nudIm.Value.ToString("F15", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            return $"burningship_julia_re{reStr}_im{imStr}";
        }

        // НОВЫЙ МЕТОД: Локальная копия функции цвета для превью
        private Color GetPaletteMandelbrotClassicColor(int iter, int maxIter, int maxClrIter)
        {
            if (iter == maxIter) return Color.Black;
            double t_classic = (double)iter / maxIter;
            byte r, g, b;
            if (t_classic < 0.5)
            {
                double t = t_classic * 2;
                r = (byte)(t * 200);
                g = (byte)(t * 50);
                b = (byte)(t * 30);
            }
            else
            {
                double t = (t_classic - 0.5) * 2;
                r = (byte)(200 + t * 55);
                g = (byte)(50 + t * 205);
                b = (byte)(30 + t * 225);
            }
            return Color.FromArgb(r, g, b);
        }

        #endregion
    }
}