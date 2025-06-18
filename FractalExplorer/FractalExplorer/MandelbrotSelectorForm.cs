// --- START OF FILE MandelbrotSelectorForm.cs ---

using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace FractalDraving
{
    /// <summary>
    /// Форма для выбора комплексного параметра 'c' (для фрактала Жюлиа)
    /// путем интерактивного клика по изображению множества Мандельброта.
    /// Также поддерживает панорамирование и масштабирование отображаемого множества Мандельброта.
    /// </summary>
    public class MandelbrotSelectorForm : Form
    {
        // Ссылка на главную форму, которая является владельцем этого окна и реализует IFractalForm.
        private readonly IFractalForm ownerForm; // <--- ИЗМЕНЕНИЕ ЗДЕСЬ
        // Элемент PictureBox для отображения множества Мандельброта.
        private PictureBox mandelbrotDisplay;
        // Bitmap с текущим изображением множества Мандельброта.
        private Bitmap mandelbrotBitmap;
        // Выбранные координаты на множестве Мандельброта (в системе координат комплексной плоскости)
        private PointF selectedMandelbrotCoords = new PointF(float.NaN, float.NaN);
        // Флаг, указывающий, идет ли в данный момент рендеринг множества Мандельброта.
        private volatile bool isRendering = false;

        // Текущие границы отображаемой области множества Мандельброта на комплексной плоскости.
        private double currentMinRe = MIN_RE;
        private double currentMaxRe = MAX_RE;
        private double currentMinIm = MIN_IM;
        private double currentMaxIm = MAX_IM;

        // Поля для реализации панорамирования изображения мышью.
        private Point panStart;
        private bool panning = false;

        // Начальные константы, определяющие границы отображения множества Мандельброта по умолчанию.
        private const double MIN_RE = -2.0;
        private const double MAX_RE = 1.0;
        private const double MIN_IM = -1.2;
        private const double MAX_IM = 1.2;
        private const int ITERATIONS = 200; //75

        /// <summary>
        /// Событие, возникающее при выборе координат (параметра 'c') на множестве Мандельброта
        /// </summary>
        public event Action<double, double> CoordinatesSelected;

        // Поля для хранения параметров, с которыми был отрисован текущий mandelbrotBitmap
        private double renderedMinRe = MIN_RE;
        private double renderedMaxRe = MAX_RE;
        private double renderedMinIm = MIN_IM;
        private double renderedMaxIm = MAX_IM;

        // Таймер для отложенного рендеринга
        private System.Windows.Forms.Timer renderDebounceTimer;
        private const int RENDER_DEBOUNCE_MILLISECONDS = 300; // Задержка для рендеринга


        /// <summary>
        /// Конструктор формы выбора параметра 'c' (MandelbrotSelectorForm).
        /// </summary>
        public MandelbrotSelectorForm(IFractalForm owner, double initialRe = double.NaN, double initialIm = double.NaN) // <--- И ИЗМЕНЕНИЕ ЗДЕСЬ
        {
            this.ownerForm = owner ?? throw new ArgumentNullException(nameof(owner));
            this.Text = "Выбор точки C (Множество Мандельброта)";
            this.Size = new Size(800, 700);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            mandelbrotDisplay = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage // Важно для начального отображения, но мы рисуем сами
            };
            this.Controls.Add(mandelbrotDisplay);

            // Подписка на события.
            this.Load += MandelbrotSelectorForm_Load;
            mandelbrotDisplay.Paint += MandelbrotDisplay_Paint;
            mandelbrotDisplay.MouseClick += MandelbrotDisplay_MouseClick;
            mandelbrotDisplay.MouseWheel += MandelbrotDisplay_MouseWheel;
            mandelbrotDisplay.MouseDown += MandelbrotDisplay_MouseDown;
            mandelbrotDisplay.MouseMove += MandelbrotDisplay_MouseMove;
            mandelbrotDisplay.MouseUp += MandelbrotDisplay_MouseUp;
            mandelbrotDisplay.Resize += MandelbrotDisplay_Resize;

            // Инициализация таймера
            renderDebounceTimer = new System.Windows.Forms.Timer { Interval = RENDER_DEBOUNCE_MILLISECONDS };
            renderDebounceTimer.Tick += RenderDebounceTimer_Tick;

            // Установка начальных rendered параметров
            renderedMinRe = currentMinRe;
            renderedMaxRe = currentMaxRe;
            renderedMinIm = currentMinIm;
            renderedMaxIm = currentMaxIm;

            SetSelectedCoordinates(initialRe, initialIm, true);
        }

        /// <summary>
        /// Устанавливает выбранные координаты параметра 'c' (Re, Im) и обновляет отображение (маркер).
        /// </summary>
        public void SetSelectedCoordinates(double re, double im, bool raiseEvent = true)
        {
            re = Math.Max(currentMinRe, Math.Min(currentMaxRe, re));
            im = Math.Max(currentMinIm, Math.Min(currentMaxIm, im));

            bool changed = false;

            if (double.IsNaN(re) || double.IsNaN(im))
            {
                if (!float.IsNaN(selectedMandelbrotCoords.X))
                {
                    changed = true;
                }
                selectedMandelbrotCoords = new PointF(float.NaN, float.NaN);
            }
            else if (selectedMandelbrotCoords.X != (float)re || selectedMandelbrotCoords.Y != (float)im)
            {
                selectedMandelbrotCoords = new PointF((float)re, (float)im);
                changed = true;
            }

            if (changed && mandelbrotDisplay.IsHandleCreated && !mandelbrotDisplay.IsDisposed)
            {
                mandelbrotDisplay.Invalidate();
            }

            if (raiseEvent && !float.IsNaN(selectedMandelbrotCoords.X))
            {
                CoordinatesSelected?.Invoke(selectedMandelbrotCoords.X, selectedMandelbrotCoords.Y);
            }
        }

        private async void MandelbrotSelectorForm_Load(object sender, EventArgs e)
        {
            await RenderMandelbrotAsync();
        }

        private async void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            renderDebounceTimer.Stop();
            if (this.IsHandleCreated && !this.IsDisposed && !this.Disposing)
            {
                await RenderMandelbrotAsync();
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

        private void MandelbrotDisplay_Resize(object sender, EventArgs e)
        {
            if (mandelbrotDisplay.Width > 0 && mandelbrotDisplay.Height > 0)
            {
                // При изменении размера контрола всегда нужен полный перерендер,
                // так как размер bitmap должен измениться.
                // И также нужно обновить rendered* параметры, если bitmap был,
                // но он не соответствует новому размеру PictureBox.
                // RenderMandelbrotAsync сам позаботится об обновлении rendered* после рендера.
                ScheduleDelayedRender();
            }
        }

        private async Task RenderMandelbrotAsync()
        {
            if (isRendering) return;
            if (mandelbrotDisplay.Width <= 0 || mandelbrotDisplay.Height <= 0 ||
                !mandelbrotDisplay.IsHandleCreated || mandelbrotDisplay.IsDisposed || this.Disposing) return;

            isRendering = true;

            int currentWidth = mandelbrotDisplay.Width;
            int currentHeight = mandelbrotDisplay.Height;
            // Захватываем ТЕКУЩИЕ целевые параметры для рендера
            double minRe_capture = currentMinRe;
            double maxRe_capture = currentMaxRe;
            double minIm_capture = currentMinIm;
            double maxIm_capture = currentMaxIm;

            Bitmap newRenderedBitmap = null;

            try
            {
                newRenderedBitmap = await Task.Run(() =>
                    RenderMandelbrotSetForSelector(currentWidth, currentHeight, ITERATIONS,
                                                   minRe_capture, maxRe_capture, minIm_capture, maxIm_capture));

                if (mandelbrotDisplay.IsHandleCreated && !mandelbrotDisplay.IsDisposed && !this.Disposing)
                {
                    // Выполняем обновление UI в основном (UI) потоке
                    mandelbrotDisplay.Invoke((Action)(() =>
                    {
                        if (mandelbrotDisplay.IsHandleCreated && !mandelbrotDisplay.IsDisposed && !this.Disposing)
                        {
                            Bitmap oldOwnedBitmap = mandelbrotBitmap;
                            mandelbrotBitmap = newRenderedBitmap;
                            // mandelbrotDisplay.Image = mandelbrotBitmap; // PictureBox может использовать это, но мы рисуем сами в Paint

                            // Сохраняем параметры, с которыми был сделан ЭТОТ рендер
                            renderedMinRe = minRe_capture;
                            renderedMaxRe = maxRe_capture;
                            renderedMinIm = minIm_capture;
                            renderedMaxIm = maxIm_capture;

                            if (oldOwnedBitmap != null && oldOwnedBitmap != mandelbrotBitmap)
                            {
                                oldOwnedBitmap.Dispose();
                            }
                            // Запросить перерисовку (теперь уже без трансформации, т.к. current == rendered,
                            // или с минимальной, если пользователь успел еще что-то изменить)
                            mandelbrotDisplay.Invalidate();
                        }
                        else
                        {
                            newRenderedBitmap?.Dispose(); // Контрол был уничтожен
                        }
                    }));
                }
                else
                {
                    newRenderedBitmap?.Dispose(); // Форма или PictureBox были уничтожены
                }
            }
            catch (Exception ex)
            {
                // В реальном приложении здесь могло бы быть логирование ошибки.
                System.Diagnostics.Debug.WriteLine($"Error rendering Mandelbrot for selector: {ex.Message}");
                newRenderedBitmap?.Dispose();
            }
            finally
            {
                isRendering = false;
            }
        }


        private Bitmap RenderMandelbrotSetForSelector(int canvasWidth, int canvasHeight, int maxIter,
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
                for (int i = 0; i < buffer.Length; i++) buffer[i] = 0; // Заполнить черным.
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
                    double c_im = maxIm - (y_coord / (double)canvasHeight) * imRange;
                    Complex c0 = new Complex(c_re, c_im);
                    Complex z = Complex.Zero;
                    int iter = 0;

                    while (iter < maxIter && z.Magnitude < 2.0)
                    {
                        z = z * z + c0;
                        iter++;
                    }

                    byte r_val, g_val, b_val;
                    if (iter == maxIter)
                    {
                        r_val = g_val = b_val = 0;
                    }
                    else
                    {
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

        private void MandelbrotDisplay_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || mandelbrotDisplay.Width <= 0 || mandelbrotDisplay.Height <= 0)
            {
                return;
            }

            double reRange = currentMaxRe - currentMinRe;
            double imRange = currentMaxIm - currentMinIm;

            if (reRange <= 0 || imRange <= 0) return;

            double re = currentMinRe + (e.X / (double)mandelbrotDisplay.Width) * reRange;
            double im = currentMaxIm - (e.Y / (double)mandelbrotDisplay.Height) * imRange;

            SetSelectedCoordinates(re, im, true);
        }

        private void MandelbrotDisplay_Paint(object sender, PaintEventArgs e)
        {
            if (mandelbrotBitmap == null || mandelbrotDisplay.Width <= 0 || mandelbrotDisplay.Height <= 0)
            {
                e.Graphics.Clear(Color.Black);
                DrawMarker(e.Graphics); // Попытка нарисовать маркер даже на черном фоне
                return;
            }

            // Параметры, с которыми был создан mandelbrotBitmap
            double rMinR_param = renderedMinRe;
            double rMaxR_param = renderedMaxRe;
            double rMinI_param = renderedMinIm;
            double rMaxI_param = renderedMaxIm;

            // Текущие целевые параметры отображения
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
                if (mandelbrotBitmap != null)
                    e.Graphics.DrawImageUnscaled(mandelbrotBitmap, Point.Empty); // Fallback
                DrawMarker(e.Graphics);
                return;
            }

            float p1_X = (float)(((rMinR_param - cMinR_param) / currentComplexWidth) * mandelbrotDisplay.Width);
            float p1_Y = (float)(((cMaxI_param - rMaxI_param) / currentComplexHeight) * mandelbrotDisplay.Height);

            float destWidthPixels = (float)((renderedComplexWidth / currentComplexWidth) * mandelbrotDisplay.Width);
            float destHeightPixels = (float)((renderedComplexHeight / currentComplexHeight) * mandelbrotDisplay.Height);

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
                    e.Graphics.DrawImage(mandelbrotBitmap, new PointF[] { destPoint1, destPoint2, destPoint3 });
                }
                catch (ArgumentException)
                {
                    if (mandelbrotBitmap != null)
                        e.Graphics.DrawImageUnscaled(mandelbrotBitmap, Point.Empty);
                }
            }
            else
            {
                if (mandelbrotBitmap != null)
                    e.Graphics.DrawImageUnscaled(mandelbrotBitmap, Point.Empty);
            }

            DrawMarker(e.Graphics);
        }

        private void DrawMarker(Graphics g)
        {
            if (!float.IsNaN(selectedMandelbrotCoords.X) && mandelbrotDisplay.Width > 0 && mandelbrotDisplay.Height > 0)
            {
                double reRange = currentMaxRe - currentMinRe;
                double imRange = currentMaxIm - currentMinIm;

                if (reRange > 0 && imRange > 0)
                {
                    try
                    {
                        checked
                        {
                            int markerX = (int)(((selectedMandelbrotCoords.X - currentMinRe) / reRange) * mandelbrotDisplay.Width);
                            int markerY = (int)(((currentMaxIm - selectedMandelbrotCoords.Y) / imRange) * mandelbrotDisplay.Height);

                            int markerSize = 9;
                            using (Pen markerPen = new Pen(Color.FromArgb(220, Color.Green), 2f))
                            {
                                g.DrawLine(markerPen, markerX - markerSize, markerY, markerX + markerSize, markerY);
                                g.DrawLine(markerPen, markerX, markerY - markerSize, markerX, markerY + markerSize);
                            }
                        }
                    }
                    catch (OverflowException ex) //так делать хреново но мне лень нормально ограничить это дело
                    {
                        // Обработка исключения переполнения.
                        string errorMessage = $"Переполнение: {ex.Message}";

                        // Выводим сообщение в отладку
                        //System.Diagnostics.Debug.WriteLine(errorMessage);

                        // Выводим сообщение в заголовок окна.  Обратите внимание, что нужно
                        // использовать Invoke, чтобы обновить UI из другого потока (если это необходимо).
                        if (this.IsHandleCreated && !this.IsDisposed && !this.Disposing)
                        {
                            this.Invoke((Action)(() =>
                            {
                                this.Text = errorMessage;
                            }));
                        }

                        // Можно предпринять другие действия, например, не рисовать маркер.
                    }
                }
            }
        }

        private void MandelbrotDisplay_MouseWheel(object sender, MouseEventArgs e)
        {
            if (mandelbrotDisplay.Width <= 0 || mandelbrotDisplay.Height <= 0) return;

            double zoomFactor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
            double oldReRange = currentMaxRe - currentMinRe;
            double oldImRange = currentMaxIm - currentMinIm;

            if (oldReRange <= 0 || oldImRange <= 0) return;

            double mouseRe = currentMinRe + (e.X / (double)mandelbrotDisplay.Width) * oldReRange;
            double mouseIm = currentMaxIm - (e.Y / (double)mandelbrotDisplay.Height) * oldImRange;

            double newReRange = oldReRange / zoomFactor;
            double newImRange = oldImRange / zoomFactor;

            const double minRangeAllowed = 1e-13;
            const double maxRangeAllowed = 100.0;

            // Добавляем константу для минимальной ширины/высоты.  Этот параметр можно настроить.
            const double minImageWidthRe = 1e-9; // Минимальная ширина по Re
            const double minImageHeightIm = 1e-9; // Минимальная высота по Im

            if (newReRange < minRangeAllowed || newImRange < minRangeAllowed ||
                newReRange > maxRangeAllowed || newImRange > maxRangeAllowed)
            {
                return;
            }

            double reRatio = e.X / (double)mandelbrotDisplay.Width;
            double imRatio = e.Y / (double)mandelbrotDisplay.Height;

            // Вычисляем новые границы, но пока не присваиваем.
            double proposedMinRe = mouseRe - reRatio * newReRange;
            double proposedMaxRe = proposedMinRe + newReRange;

            double proposedMinIm = mouseIm - (1.0 - imRatio) * newImRange;
            double proposedMaxIm = proposedMinIm + newImRange;


            // Проверяем, не стали ли размеры слишком маленькими.
            if (proposedMaxRe - proposedMinRe < minImageWidthRe || proposedMaxIm - proposedMinIm < minImageHeightIm)
            {
                return; // Не масштабируем, если размеры слишком малы.
            }


            // Если все проверки пройдены, применяем изменения.
            currentMinRe = proposedMinRe;
            currentMaxRe = proposedMaxRe;
            currentMinIm = proposedMinIm;
            currentMaxIm = proposedMaxIm;


            mandelbrotDisplay.Invalidate();
            ScheduleDelayedRender();
        }

        private void MandelbrotDisplay_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panning = true;
                panStart = e.Location;
            }
        }

        private void MandelbrotDisplay_MouseMove(object sender, MouseEventArgs e)
        {
            if (panning)
            {
                if (mandelbrotDisplay.Width <= 0 || mandelbrotDisplay.Height <= 0) return;

                double reRange = currentMaxRe - currentMinRe;
                double imRange = currentMaxIm - currentMinIm;

                if (reRange <= 0 || imRange <= 0) return;

                double dx_pixels = panStart.X - e.X;
                double dy_pixels = panStart.Y - e.Y;

                double dx_complex = dx_pixels * (reRange / mandelbrotDisplay.Width);
                double dy_complex = dy_pixels * (imRange / mandelbrotDisplay.Height);

                currentMinRe += dx_complex;
                currentMaxRe += dx_complex;
                currentMinIm -= dy_complex; // ИЗМЕНЕНО: вычитаем dy_complex
                currentMaxIm -= dy_complex; // ИЗМЕНЕНО: вычитаем dy_complex

                panStart = e.Location;

                mandelbrotDisplay.Invalidate();
                ScheduleDelayedRender();
            }
        }

        private void MandelbrotDisplay_MouseUp(object sender, MouseEventArgs e)
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

            mandelbrotBitmap?.Dispose();
            mandelbrotBitmap = null;

            base.OnFormClosed(e);
        }
    }
}