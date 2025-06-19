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
        private const decimal BS_MIN_IM = -1.0m; // Новый низ (старый верх с инвертированным знаком)
        private const decimal BS_MAX_IM = 1.5m;  // Новый верх (старый низ с инвертированным знаком)
        private const int BS_PREVIEW_ITERATIONS = 75;

        private BurningShipCSelectorForm _burningShipCSelectorWindow;

        public FractalJuliaBurningShip()
        {
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

            nudRe.Value = -1.7551867961883m;
            nudIm.Value = 0.01068m;

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

            // Создаем движок специально для превью "Горящего Корабля"
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

            // --- НАЧАЛО ИЗМЕНЕНИЙ ---

            // Создаем один буфер для всего изображения превью
            int bytes = Math.Abs(bmpData.Stride) * canvasHeight;
            byte[] buffer = new byte[bytes];
            int bytesPerPixel = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;

            // Создаем одну большую плитку, покрывающую все превью
            var tile = new TileInfo(0, 0, canvasWidth, canvasHeight);

            // Вызываем новый, исправленный RenderTile
            engine.RenderTile(buffer, bmpData.Stride, bytesPerPixel, tile, canvasWidth, canvasHeight);

            // Копируем готовый буфер в битмап
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, bytes);

            // --- КОНЕЦ ИЗМЕНЕНИЙ ---

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

        protected override string GetSaveFileNameDetails()
        {
            // Форматируем Re и Im для имени файла
            string reStr = nudRe.Value.ToString("F15", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            string imStr = nudIm.Value.ToString("F15", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            return $"burningship_julia_re{reStr}_im{imStr}";
        }
        #endregion
    }
}