// --- START OF FILE BurningShipCSelectorForm.cs ---

using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace FractalDraving
{
    /// <summary>
    /// Форма для выбора комплексного параметра 'c' (для фрактала Жюлиа "Горящий Корабль")
    /// путем интерактивного клика по изображению множества "Горящий Корабль" (Мандельброт-версия).
    /// Также поддерживает панорамирование и масштабирование отображаемого множества.
    /// </summary>
    public class BurningShipCSelectorForm : Form
    {
        // Ссылка на главную форму, которая является владельцем этого окна и реализует IFractalForm.
        private readonly IFractalForm ownerForm;
        // Элемент PictureBox для отображения множества.
        private PictureBox displayPictureBox;
        // Bitmap с текущим изображением множества.
        private Bitmap renderedBitmap;
        // Выбранные координаты на множестве (в системе координат комплексной плоскости)
        private PointF selectedComplexCoords = new PointF(float.NaN, float.NaN);
        // Флаг, указывающий, идет ли в данный момент рендеринг.
        private volatile bool isRendering = false;

        // Текущие границы отображаемой области на комплексной плоскости.
        // Используем значения, подходящие для Горящего Корабля
        private double currentMinRe = INITIAL_MIN_RE;
        private double currentMaxRe = INITIAL_MAX_RE;
        private double currentMinIm = INITIAL_MIN_IM;
        private double currentMaxIm = INITIAL_MAX_IM;

        // Поля для реализации панорамирования изображения мышью.
        private Point panStart;
        private bool panning = false;

        // Начальные константы, определяющие границы отображения по умолчанию.
        // Эти значения взяты из вашего burningShipComplex.cs для mandelbrotCanvas1
        private const double INITIAL_MIN_RE = -2.0;
        private const double INITIAL_MAX_RE = 1.5;
        private const double INITIAL_MIN_IM = -1.5;
        private const double INITIAL_MAX_IM = 1.0;
        private const int ITERATIONS = 200; // Можно настроить, 200 должно быть нормально для селектора

        /// <summary>
        /// Событие, возникающее при выборе координат (параметра 'c')
        /// </summary>
        public event Action<double, double> CoordinatesSelected;

        // Поля для хранения параметров, с которыми был отрисован текущий renderedBitmap
        private double renderedMinRe = INITIAL_MIN_RE;
        private double renderedMaxRe = INITIAL_MAX_RE;
        private double renderedMinIm = INITIAL_MIN_IM;
        private double renderedMaxIm = INITIAL_MAX_IM;

        // Таймер для отложенного рендеринга
        private System.Windows.Forms.Timer renderDebounceTimer;
        private const int RENDER_DEBOUNCE_MILLISECONDS = 300; // Задержка для рендеринга


        /// <summary>
        /// Конструктор формы выбора параметра 'c'.
        /// </summary>
        public BurningShipCSelectorForm(IFractalForm owner, double initialRe = double.NaN, double initialIm = double.NaN)
        {
            this.ownerForm = owner ?? throw new ArgumentNullException(nameof(owner));
            this.Text = "Выбор точки C (Множество Горящий Корабль)"; // <--- ИЗМЕНЕНО
            this.Size = new Size(800, 700);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            displayPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            this.Controls.Add(displayPictureBox);

            // Подписка на события.
            this.Load += SelectorForm_Load;
            displayPictureBox.Paint += DisplayPictureBox_Paint;
            displayPictureBox.MouseClick += DisplayPictureBox_MouseClick;
            displayPictureBox.MouseWheel += DisplayPictureBox_MouseWheel;
            displayPictureBox.MouseDown += DisplayPictureBox_MouseDown;
            displayPictureBox.MouseMove += DisplayPictureBox_MouseMove;
            displayPictureBox.MouseUp += DisplayPictureBox_MouseUp;
            displayPictureBox.Resize += DisplayPictureBox_Resize;

            renderDebounceTimer = new System.Windows.Forms.Timer { Interval = RENDER_DEBOUNCE_MILLISECONDS };
            renderDebounceTimer.Tick += RenderDebounceTimer_Tick;

            renderedMinRe = currentMinRe;
            renderedMaxRe = currentMaxRe;
            renderedMinIm = currentMinIm;
            renderedMaxIm = currentMaxIm;

            SetSelectedCoordinates(initialRe, initialIm, true);
        }

        public void SetSelectedCoordinates(double re, double im, bool raiseEvent = true)
        {
            re = Math.Max(currentMinRe, Math.Min(currentMaxRe, re));
            im = Math.Max(currentMinIm, Math.Min(currentMaxIm, im));

            bool changed = false;

            if (double.IsNaN(re) || double.IsNaN(im))
            {
                if (!float.IsNaN(selectedComplexCoords.X))
                {
                    changed = true;
                }
                selectedComplexCoords = new PointF(float.NaN, float.NaN);
            }
            else if (selectedComplexCoords.X != (float)re || selectedComplexCoords.Y != (float)im)
            {
                selectedComplexCoords = new PointF((float)re, (float)im);
                changed = true;
            }

            if (changed && displayPictureBox.IsHandleCreated && !displayPictureBox.IsDisposed)
            {
                displayPictureBox.Invalidate();
            }

            if (raiseEvent && !float.IsNaN(selectedComplexCoords.X))
            {
                CoordinatesSelected?.Invoke(selectedComplexCoords.X, selectedComplexCoords.Y);
            }
        }

        private async void SelectorForm_Load(object sender, EventArgs e)
        {
            await RenderSetAsync();
        }

        private async void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            renderDebounceTimer.Stop();
            if (this.IsHandleCreated && !this.IsDisposed && !this.Disposing)
            {
                await RenderSetAsync();
            }
        }

        private void ScheduleDelayedRender()
        {
            if (this.IsHandleCreated && !this.IsDisposed && !this.Disposing)
            {
                renderDebounceTimer.Stop();
                renderDebounceTimer.Start();
            }
        }

        private void DisplayPictureBox_Resize(object sender, EventArgs e)
        {
            if (displayPictureBox.Width > 0 && displayPictureBox.Height > 0)
            {
                ScheduleDelayedRender();
            }
        }

        private async Task RenderSetAsync()
        {
            if (isRendering) return;
            if (displayPictureBox.Width <= 0 || displayPictureBox.Height <= 0 ||
                !displayPictureBox.IsHandleCreated || displayPictureBox.IsDisposed || this.Disposing) return;

            isRendering = true;

            int currentWidth = displayPictureBox.Width;
            int currentHeight = displayPictureBox.Height;
            double minRe_capture = currentMinRe;
            double maxRe_capture = currentMaxRe;
            double minIm_capture = currentMinIm;
            double maxIm_capture = currentMaxIm;

            Bitmap newRenderedBitmap = null;

            try
            {
                newRenderedBitmap = await Task.Run(() =>
                    RenderBurningShipSetInternal(currentWidth, currentHeight, ITERATIONS,
                                                   minRe_capture, maxRe_capture, minIm_capture, maxIm_capture));

                if (displayPictureBox.IsHandleCreated && !displayPictureBox.IsDisposed && !this.Disposing)
                {
                    displayPictureBox.Invoke((Action)(() =>
                    {
                        if (displayPictureBox.IsHandleCreated && !displayPictureBox.IsDisposed && !this.Disposing)
                        {
                            Bitmap oldOwnedBitmap = renderedBitmap;
                            renderedBitmap = newRenderedBitmap;

                            renderedMinRe = minRe_capture;
                            renderedMaxRe = maxRe_capture;
                            renderedMinIm = minIm_capture;
                            renderedMaxIm = maxIm_capture;

                            if (oldOwnedBitmap != null && oldOwnedBitmap != renderedBitmap)
                            {
                                oldOwnedBitmap.Dispose();
                            }
                            displayPictureBox.Invalidate();
                        }
                        else
                        {
                            newRenderedBitmap?.Dispose();
                        }
                    }));
                }
                else
                {
                    newRenderedBitmap?.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering Burning Ship set for selector: {ex.Message}");
                newRenderedBitmap?.Dispose();
            }
            finally
            {
                isRendering = false;
            }
        }

        // <--- ГЛАВНОЕ ИЗМЕНЕНИЕ ЗДЕСЬ: ЛОГИКА РЕНДЕРИНГА ---
        private Bitmap RenderBurningShipSetInternal(int canvasWidth, int canvasHeight, int maxIter,
                                                      double minRe, double maxRe, double minIm, double maxIm)
        {
            Bitmap bmp = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, canvasWidth, canvasHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            int bytes = Math.Abs(stride) * canvasHeight;
            byte[] buffer = new byte[bytes];

            double reRange = maxRe - minRe;
            double imRange = maxIm - minIm;

            if (reRange <= 0 || imRange <= 0)
            {
                for (int i = 0; i < buffer.Length; i++) buffer[i] = 0;
                Marshal.Copy(buffer, 0, scan0, bytes);
                bmp.UnlockBits(bmpData);
                return bmp;
            }

            Parallel.For(0, canvasHeight, y_coord =>
            {
                int rowOffset = y_coord * stride;
                for (int x_coord = 0; x_coord < canvasWidth; x_coord++)
                {
                    double c_re = minRe + (x_coord / (double)canvasWidth) * reRange;
                    double c_im = maxIm - (y_coord / (double)canvasHeight) * imRange; // Y инвертирован для стандартного отображения
                    Complex c0 = new Complex(c_re, c_im);
                    Complex z = Complex.Zero;
                    int iter = 0;

                    // Формула Горящего Корабля (Мандельброт-версия)
                    while (iter < maxIter && z.Magnitude < 2.0) // Порог 2.0 для Мандельброт-подобных
                    {
                        z = new Complex(Math.Abs(z.Real), -Math.Abs(z.Imaginary)); // Взять абсолютные значения
                        z = z * z + c0;                                          // Стандартная итерация
                        iter++;
                    }

                    byte r_val, g_val, b_val;
                    if (iter == maxIter)
                    {
                        r_val = g_val = b_val = 0; // Внутри множества - черный
                    }
                    else
                    {
                        // Та же палитра, что и в MandelbrotSelectorForm
                        double t = (double)iter / maxIter;
                        if (t < 0.5)
                        {
                            r_val = (byte)(t * 2 * 200);
                            g_val = (byte)(t * 2 * 50);
                            b_val = (byte)(t * 2 * 30);
                        }
                        else
                        {
                            t = (t - 0.5) * 2;
                            r_val = (byte)(200 + t * 55);
                            g_val = (byte)(50 + t * 205);
                            b_val = (byte)(30 + t * 225);
                        }
                    }
                    int index = rowOffset + x_coord * 3;
                    buffer[index] = b_val;
                    buffer[index + 1] = g_val;
                    buffer[index + 2] = r_val;
                }
            });

            Marshal.Copy(buffer, 0, scan0, bytes);
            bmp.UnlockBits(bmpData);
            return bmp;
        }
        // --- КОНЕЦ ГЛАВНОГО ИЗМЕНЕНИЯ ---

        private void DisplayPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || displayPictureBox.Width <= 0 || displayPictureBox.Height <= 0)
            {
                return;
            }

            double reRange = currentMaxRe - currentMinRe;
            double imRange = currentMaxIm - currentMinIm;

            if (reRange <= 0 || imRange <= 0) return;

            double re = currentMinRe + (e.X / (double)displayPictureBox.Width) * reRange;
            double im = currentMaxIm - (e.Y / (double)displayPictureBox.Height) * imRange;

            SetSelectedCoordinates(re, im, true);
        }

        private void DisplayPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (renderedBitmap == null || displayPictureBox.Width <= 0 || displayPictureBox.Height <= 0)
            {
                e.Graphics.Clear(Color.Black);
                DrawMarker(e.Graphics);
                return;
            }

            double rMinR_param = renderedMinRe;
            double rMaxR_param = renderedMaxRe;
            double rMinI_param = renderedMinIm;
            double rMaxI_param = renderedMaxIm;

            double cMinR_param = currentMinRe;
            double cMaxR_param = currentMaxRe;
            double cMinI_param = currentMinIm;
            double cMaxI_param = currentMaxIm;

            double renderedComplexWidth = rMaxR_param - rMinR_param;
            double renderedComplexHeight = rMaxI_param - rMinI_param;

            double currentComplexWidth = cMaxR_param - cMinR_param;
            double currentComplexHeight = cMaxI_param - cMinI_param;

            if (renderedComplexWidth <= 0 || renderedComplexHeight <= 0 || currentComplexWidth <= 0 || currentComplexHeight <= 0)
            {
                e.Graphics.Clear(Color.Black);
                if (renderedBitmap != null)
                    e.Graphics.DrawImageUnscaled(renderedBitmap, Point.Empty);
                DrawMarker(e.Graphics);
                return;
            }

            float p1_X = (float)(((rMinR_param - cMinR_param) / currentComplexWidth) * displayPictureBox.Width);
            float p1_Y = (float)(((cMaxI_param - rMaxI_param) / currentComplexHeight) * displayPictureBox.Height);

            float destWidthPixels = (float)((renderedComplexWidth / currentComplexWidth) * displayPictureBox.Width);
            float destHeightPixels = (float)((renderedComplexHeight / currentComplexHeight) * displayPictureBox.Height);

            PointF destPoint1 = new PointF(p1_X, p1_Y);
            PointF destPoint2 = new PointF(p1_X + destWidthPixels, p1_Y);
            PointF destPoint3 = new PointF(p1_X, p1_Y + destHeightPixels);

            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            if (destWidthPixels > 0 && destHeightPixels > 0)
            {
                try
                {
                    e.Graphics.DrawImage(renderedBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                }
                catch (ArgumentException)
                {
                    if (renderedBitmap != null)
                        e.Graphics.DrawImageUnscaled(renderedBitmap, Point.Empty);
                }
            }
            else
            {
                if (renderedBitmap != null)
                    e.Graphics.DrawImageUnscaled(renderedBitmap, Point.Empty);
            }

            DrawMarker(e.Graphics);
        }

        private void DrawMarker(Graphics g)
        {
            if (!float.IsNaN(selectedComplexCoords.X) && displayPictureBox.Width > 0 && displayPictureBox.Height > 0)
            {
                double reRange = currentMaxRe - currentMinRe;
                double imRange = currentMaxIm - currentMinIm;

                if (reRange > 0 && imRange > 0)
                {
                    try
                    {
                        checked
                        {
                            int markerX = (int)(((selectedComplexCoords.X - currentMinRe) / reRange) * displayPictureBox.Width);
                            int markerY = (int)(((currentMaxIm - selectedComplexCoords.Y) / imRange) * displayPictureBox.Height);

                            int markerSize = 9;
                            using (Pen markerPen = new Pen(Color.FromArgb(220, Color.Green), 2f))
                            {
                                g.DrawLine(markerPen, markerX - markerSize, markerY, markerX + markerSize, markerY);
                                g.DrawLine(markerPen, markerX, markerY - markerSize, markerX, markerY + markerSize);
                            }
                        }
                    }
                    catch (OverflowException ex)
                    {
                        string errorMessage = $"Переполнение: {ex.Message}";
                        if (this.IsHandleCreated && !this.IsDisposed && !this.Disposing)
                        {
                            this.Invoke((Action)(() => { this.Text = errorMessage; }));
                        }
                    }
                }
            }
        }

        private void DisplayPictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (displayPictureBox.Width <= 0 || displayPictureBox.Height <= 0) return;

            double zoomFactor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
            double oldReRange = currentMaxRe - currentMinRe;
            double oldImRange = currentMaxIm - currentMinIm;

            if (oldReRange <= 0 || oldImRange <= 0) return;

            double mouseRe = currentMinRe + (e.X / (double)displayPictureBox.Width) * oldReRange;
            double mouseIm = currentMaxIm - (e.Y / (double)displayPictureBox.Height) * oldImRange;

            double newReRange = oldReRange / zoomFactor;
            double newImRange = oldImRange / zoomFactor;

            const double minRangeAllowed = 1e-13;
            const double maxRangeAllowed = 100.0;
            const double minImageWidthRe = 1e-9;
            const double minImageHeightIm = 1e-9;

            if (newReRange < minRangeAllowed || newImRange < minRangeAllowed ||
                newReRange > maxRangeAllowed || newImRange > maxRangeAllowed ||
                newReRange < minImageWidthRe || newImRange < minImageHeightIm) // Проверка на слишком маленький диапазон
            {
                return;
            }

            double reRatio = e.X / (double)displayPictureBox.Width;
            double imRatio = e.Y / (double)displayPictureBox.Height;

            currentMinRe = mouseRe - reRatio * newReRange;
            currentMaxRe = currentMinRe + newReRange;
            currentMinIm = mouseIm - (1.0 - imRatio) * newImRange;
            currentMaxIm = currentMinIm + newImRange;

            displayPictureBox.Invalidate();
            ScheduleDelayedRender();
        }

        private void DisplayPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panning = true;
                panStart = e.Location;
            }
        }

        private void DisplayPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (panning)
            {
                if (displayPictureBox.Width <= 0 || displayPictureBox.Height <= 0) return;

                double reRange = currentMaxRe - currentMinRe;
                double imRange = currentMaxIm - currentMinIm;

                if (reRange <= 0 || imRange <= 0) return;

                double dx_pixels = panStart.X - e.X;
                double dy_pixels = panStart.Y - e.Y;

                double dx_complex = dx_pixels * (reRange / displayPictureBox.Width);
                double dy_complex = dy_pixels * (imRange / displayPictureBox.Height);

                currentMinRe += dx_complex;
                currentMaxRe += dx_complex;
                currentMinIm -= dy_complex;
                currentMaxIm -= dy_complex;

                panStart = e.Location;

                displayPictureBox.Invalidate();
                ScheduleDelayedRender();
            }
        }

        private void DisplayPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panning = false;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            renderDebounceTimer?.Stop();
            renderDebounceTimer?.Dispose();
            renderDebounceTimer = null;

            renderedBitmap?.Dispose();
            renderedBitmap = null;

            base.OnFormClosed(e);
        }
    }
}
// --- END OF FILE BurningShipCSelectorForm.cs ---