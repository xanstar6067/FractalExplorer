using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Resources;
using FractalExplorer.Selectors;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace FractalExplorer.Projects
{
    public partial class FractalJulia : FractalFormBase
    {
        // Константы для превью Мандельброта
        private const decimal MANDELBROT_MIN_RE = -2.0m;
        private const decimal MANDELBROT_MAX_RE = 1.0m;
        private const decimal MANDELBROT_MIN_IM = -1.2m;
        private const decimal MANDELBROT_MAX_IM = 1.2m;
        private const int MANDELBROT_PREVIEW_ITERATIONS = 75;

        private MandelbrotSelectorForm _mandelbrotCSelectorWindow;

        public FractalJulia()
        {
            Text = "Фрактал Жюлиа";
        }

        protected override FractalMondelbrotBaseEngine CreateEngine()
        {
            return new JuliaEngine();
        }

        protected override decimal BaseScale => 4.0m;
        protected override decimal InitialCenterX => 0.0m;
        protected override decimal InitialCenterY => 0.0m;

        protected override void OnPostInitialize()
        {
            // Отображаем нужные для Жюлиа контролы
            mandelbrotPreviewPanel.Visible = true;
            lblRe.Visible = true;
            nudRe.Visible = true;
            lblIm.Visible = true;
            nudIm.Visible = true;

            // Настраиваем превью множества Мандельброта
            var previewCanvas = this.Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault() as PictureBox;
            if (previewCanvas != null)
            {
                previewCanvas.Click += mandelbrotCanvas_Click;
                previewCanvas.Paint += mandelbrotCanvas_Paint;
                Task.Run(() => RenderAndDisplayMandelbrotSet());
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

        private void RenderAndDisplayMandelbrotSet()
        {
            var previewCanvas = this.Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault() as PictureBox;
            if (previewCanvas == null || previewCanvas.Width <= 0 || previewCanvas.Height <= 0) return;
            Bitmap mandelbrotImage = RenderMandelbrotSetInternal(previewCanvas.Width, previewCanvas.Height, MANDELBROT_PREVIEW_ITERATIONS);
            if (previewCanvas.IsHandleCreated && !previewCanvas.IsDisposed)
            {
                previewCanvas.Invoke((System.Action)(() =>
                {
                    previewCanvas.Image?.Dispose();
                    previewCanvas.Image = mandelbrotImage;
                }));
            }
            else
            {
                mandelbrotImage?.Dispose();
            }
        }

        private Bitmap RenderMandelbrotSetInternal(int canvasWidth, int canvasHeight, int iterationsLimit)
        {
            Bitmap bmp = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format24bppRgb);

            var engine = new MandelbrotEngine
            {
                MaxIterations = iterationsLimit,
                ThresholdSquared = 4m,
                // ИСПОЛЬЗУЕМ ЛОКАЛЬНУЮ ВЕРСИЮ МЕТОДА
                Palette = GetPaletteMandelbrotClassicColor,
                Scale = MANDELBROT_MAX_RE - MANDELBROT_MIN_RE,
                CenterX = (MANDELBROT_MAX_RE + MANDELBROT_MIN_RE) / 2,
                CenterY = (MANDELBROT_MAX_IM + MANDELBROT_MIN_IM) / 2
            };

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, canvasWidth, canvasHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int bytes = System.Math.Abs(bmpData.Stride) * canvasHeight;
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

            decimal reRange = MANDELBROT_MAX_RE - MANDELBROT_MIN_RE;
            decimal imRange = MANDELBROT_MAX_IM - MANDELBROT_MIN_IM;
            decimal currentCRe = nudRe.Value;
            decimal currentCIm = nudIm.Value;

            if (reRange > 0 && imRange > 0 && currentCRe >= MANDELBROT_MIN_RE && currentCRe <= MANDELBROT_MAX_RE &&
                currentCIm >= MANDELBROT_MIN_IM && currentCIm <= MANDELBROT_MAX_IM)
            {
                int markerX = (int)((currentCRe - MANDELBROT_MIN_RE) / reRange * previewCanvas.Width);
                int markerY = (int)((MANDELBROT_MAX_IM - currentCIm) / imRange * previewCanvas.Height);

                using (Pen markerPen = new Pen(Color.FromArgb(200, Color.LimeGreen), 1.5f))
                {
                    e.Graphics.DrawLine(markerPen, 0, markerY, previewCanvas.Width, markerY);
                    e.Graphics.DrawLine(markerPen, markerX, 0, markerX, previewCanvas.Height);
                }
            }
        }

        private void mandelbrotCanvas_Click(object sender, System.EventArgs e)
        {
            double initialRe = (double)nudRe.Value;
            double initialIm = (double)nudIm.Value;

            if (_mandelbrotCSelectorWindow == null || _mandelbrotCSelectorWindow.IsDisposed)
            {
                _mandelbrotCSelectorWindow = new MandelbrotSelectorForm(this, initialRe, initialIm);
                _mandelbrotCSelectorWindow.CoordinatesSelected += (re, im) => {
                    nudRe.Value = (decimal)re;
                    nudIm.Value = (decimal)im;
                };
                _mandelbrotCSelectorWindow.FormClosed += (s, args) => { _mandelbrotCSelectorWindow = null; };
                _mandelbrotCSelectorWindow.Show(this);
            }
            else
            {
                _mandelbrotCSelectorWindow.Activate();
                _mandelbrotCSelectorWindow.SetSelectedCoordinates(initialRe, initialIm, true);
            }
        }

        protected override string GetSaveFileNameDetails()
        {
            string reStr = nudRe.Value.ToString("F15", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            string imStr = nudIm.Value.ToString("F15", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            return $"julia_re{reStr}_im{imStr}";
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