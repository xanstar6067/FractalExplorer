using FractalDraving;
using FractalExplorer.Resources;
using FractalExplorer.Selectors;
using System.Drawing.Imaging;

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

        protected override FractalEngineBase CreateEngine()
        {
            return new JuliaEngine();
        }

        protected override decimal BaseScale => 4.0m;
        protected override decimal InitialCenterX => 0.0m;
        protected override decimal InitialCenterY => 0.0m;

        protected override void OnPostInitialize()
        {
            // Скрываем ненужные для Жюлиа контролы
            mondelbrotClassicBox.Visible = false;

            // Настраиваем видимость специфичных для Жюлиа контролов
            mandelbrotPreviewPanel.Visible = true;
            lblRe.Visible = true;
            nudRe.Visible = true;
            lblIm.Visible = true;
            nudIm.Visible = true;

            // Подписываемся на события превью
            mandelbrotPreviewCanvas.Click += mandelbrotCanvas_Click;
            mandelbrotPreviewCanvas.Paint += mandelbrotCanvas_Paint;

            // Запускаем рендер превью
            Task.Run(() => RenderAndDisplayMandelbrotSet());
        }

        protected override void UpdateEngineSpecificParameters()
        {
            // Передаем константу C в движок
            _fractalEngine.C = new ComplexDecimal(nudRe.Value, nudIm.Value);

            // Обновляем маркер на превью, если он есть
            if (mandelbrotPreviewCanvas.IsHandleCreated && !mandelbrotPreviewCanvas.IsDisposed)
            {
                mandelbrotPreviewCanvas.Invalidate();
            }
        }

        #region C-Selector Logic

        private void RenderAndDisplayMandelbrotSet()
        {
            if (mandelbrotPreviewCanvas == null || mandelbrotPreviewCanvas.Width <= 0 || mandelbrotPreviewCanvas.Height <= 0) return;
            Bitmap mandelbrotImage = RenderMandelbrotSetInternal(mandelbrotPreviewCanvas.Width, mandelbrotPreviewCanvas.Height, MANDELBROT_PREVIEW_ITERATIONS);
            if (mandelbrotPreviewCanvas.IsHandleCreated && !mandelbrotPreviewCanvas.IsDisposed)
            {
                mandelbrotPreviewCanvas.Invoke(() =>
                {
                    mandelbrotPreviewCanvas.Image?.Dispose();
                    mandelbrotPreviewCanvas.Image = mandelbrotImage;
                });
            }
            else
            {
                mandelbrotImage?.Dispose();
            }
        }

        private Bitmap RenderMandelbrotSetInternal(int canvasWidth, int canvasHeight, int iterationsLimit)
        {
            Bitmap bmp = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format24bppRgb);

            // Создаем движок специально для этого превью
            var engine = new MandelbrotEngine
            {
                MaxIterations = iterationsLimit,
                ThresholdSquared = 4m,
                Palette = GetPaletteMandelbrotClassicColor,
                Scale = MANDELBROT_MAX_RE - MANDELBROT_MIN_RE,
                CenterX = (MANDELBROT_MAX_RE + MANDELBROT_MIN_RE) / 2,
                CenterY = (MANDELBROT_MAX_IM + MANDELBROT_MIN_IM) / 2
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

            decimal reRange = MANDELBROT_MAX_RE - MANDELBROT_MIN_RE;
            decimal imRange = MANDELBROT_MAX_IM - MANDELBROT_MIN_IM;
            decimal currentCRe = nudRe.Value;
            decimal currentCIm = nudIm.Value;

            if (reRange > 0 && imRange > 0 && currentCRe >= MANDELBROT_MIN_RE && currentCRe <= MANDELBROT_MAX_RE &&
                currentCIm >= MANDELBROT_MIN_IM && currentCIm <= MANDELBROT_MAX_IM)
            {
                int markerX = (int)((currentCRe - MANDELBROT_MIN_RE) / reRange * mandelbrotPreviewCanvas.Width);
                int markerY = (int)((MANDELBROT_MAX_IM - currentCIm) / imRange * mandelbrotPreviewCanvas.Height);

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
            // Форматируем Re и Im для имени файла
            string reStr = nudRe.Value.ToString("F15", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            string imStr = nudIm.Value.ToString("F15", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            return $"julia_re{reStr}_im{imStr}";
        }
        #endregion
    }
}