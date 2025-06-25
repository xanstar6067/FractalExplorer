﻿using FractalExplorer.Engines; // Предполагается, что здесь есть ComplexDecimal
using FractalExplorer.Resources; // Предполагается, что здесь есть TileInfo
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms; // Явное добавление для Form, PictureBox, etc.

namespace FractalExplorer.Forms.SelectorsForms.Selector
{
    /// <summary>
    /// Форма для выбора комплексного параметра 'c' (для фрактала Жюлиа "Пылающий корабль")
    /// путем интерактивного клика по изображению множества "Пылающий корабль" (Мандельброт-версия).
    /// Также поддерживает панорамирование и масштабирование отображаемого множества.
    /// </summary>
    public class BurningShipCSelectorForm : Form
    {
        #region Fields

        /// <summary>
        /// Ссылка на главную форму, которая является владельцем этого окна и реализует IFractalForm.
        /// </summary>
        private readonly IFractalForm ownerForm;

        /// <summary>
        /// Элемент PictureBox для отображения множества "Пылающий корабль".
        /// </summary>
        private PictureBox displayPictureBox;

        /// <summary>
        /// Bitmap с текущим отрисованным изображением множества.
        /// </summary>
        private Bitmap renderedBitmap;

        /// <summary>
        /// Выбранные координаты на множестве (в системе координат комплексной плоскости).
        /// </summary>
        private PointF selectedComplexCoords = new PointF(float.NaN, float.NaN);

        /// <summary>
        /// Флаг, указывающий, идет ли в данный момент рендеринг изображения.
        /// </summary>
        private volatile bool isRendering = false;

        /// <summary>
        /// Текущая минимальная действительная часть отображаемой области на комплексной плоскости.
        /// </summary>
        private double currentMinRe = INITIAL_MIN_RE;

        /// <summary>
        /// Текущая максимальная действительная часть отображаемой области на комплексной плоскости.
        /// </summary>
        private double currentMaxRe = INITIAL_MAX_RE;

        /// <summary>
        /// Текущая минимальная мнимая часть отображаемой области на комплексной плоскости.
        /// </summary>
        private double currentMinIm = INITIAL_MIN_IM;

        /// <summary>
        /// Текущая максимальная мнимая часть отображаемой области на комплексной плоскости.
        /// </summary>
        private double currentMaxIm = INITIAL_MAX_IM;

        /// <summary>
        /// Начальная точка курсора мыши при панорамировании.
        /// </summary>
        private Point panStart;

        /// <summary>
        /// Флаг, указывающий, находится ли пользователь в режиме панорамирования.
        /// </summary>
        private bool panning = false;

        /// <summary>
        /// Начальная минимальная действительная часть для отображения по умолчанию.
        /// </summary>
        private const double INITIAL_MIN_RE = -2.0;

        /// <summary>
        /// Начальная максимальная действительная часть для отображения по умолчанию.
        /// </summary>
        private const double INITIAL_MAX_RE = 1.5;

        /// <summary>
        /// Начальная минимальная мнимая часть для отображения по умолчанию.
        /// </summary>
        private const double INITIAL_MIN_IM = -1.0;

        /// <summary>
        /// Начальная максимальная мнимая часть для отображения по умолчанию.
        /// </summary>
        private const double INITIAL_MAX_IM = 1.5;

        /// <summary>
        /// Количество итераций для рендеринга множества "Пылающий корабль" в селекторе.
        /// </summary>
        private const int ITERATIONS = 200;

        /// <summary>
        /// Минимальная действительная часть, по которой был отрисован текущий <see cref="renderedBitmap"/>.
        /// </summary>
        private double renderedMinRe = INITIAL_MIN_RE;

        /// <summary>
        /// Максимальная действительная часть, по которой был отрисован текущий <see cref="renderedBitmap"/>.
        /// </summary>
        private double renderedMaxRe = INITIAL_MAX_RE;

        /// <summary>
        /// Минимальная мнимая часть, по которой был отрисован текущий <see cref="renderedBitmap"/>.
        /// </summary>
        private double renderedMinIm = INITIAL_MIN_IM;

        /// <summary>
        /// Максимальная мнимая часть, по которой был отрисован текущий <see cref="renderedBitmap"/>.
        /// </summary>
        private double renderedMaxIm = INITIAL_MAX_IM;

        /// <summary>
        /// Таймер для отложенного запуска рендеринга, чтобы избежать частых перерисовок.
        /// </summary>
        private System.Windows.Forms.Timer renderDebounceTimer;

        /// <summary>
        /// Задержка в миллисекундах для таймера отложенного рендеринга.
        /// </summary>
        private const int RENDER_DEBOUNCE_MILLISECONDS = 300;

        #endregion

        #region Events

        /// <summary>
        /// Событие, возникающее при выборе новых координат (параметра 'c').
        /// </summary>
        public event Action<double, double> CoordinatesSelected;

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор формы выбора параметра 'c'.
        /// </summary>
        /// <param name="owner">Главная форма, являющаяся владельцем этого окна.</param>
        /// <param name="initialRe">Начальное значение действительной части для выбранных координат (по умолчанию NaN).</param>
        /// <param name="initialIm">Начальное значение мнимой части для выбранных координат (по умолчанию NaN).</param>
        public BurningShipCSelectorForm(IFractalForm owner, double initialRe = double.NaN, double initialIm = double.NaN)
        {
            ownerForm = owner ?? throw new ArgumentNullException(nameof(owner));
            Text = "Выбор точки C (Множество Горящий Корабль)";
            Size = new Size(800, 700);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            displayPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            Controls.Add(displayPictureBox);

            // Подписка на события.
            Load += SelectorForm_Load;
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

        #endregion

        #region Public Methods

        /// <summary>
        /// Устанавливает выбранные координаты параметра 'c' и при необходимости вызывает событие.
        /// </summary>
        /// <param name="re">Действительная часть комплексного числа.</param>
        /// <param name="im">Мнимая часть комплексного числа.</param>
        /// <param name="raiseEvent">Если True, вызывает событие <see cref="CoordinatesSelected"/>.</param>
        public void SetSelectedCoordinates(double re, double im, bool raiseEvent = true)
        {
            // Ограничиваем координаты в пределах текущего отображаемого диапазона.
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
                displayPictureBox.Invalidate(); // Запрашиваем перерисовку маркера
            }

            if (raiseEvent && !float.IsNaN(selectedComplexCoords.X))
            {
                CoordinatesSelected?.Invoke(selectedComplexCoords.X, selectedComplexCoords.Y);
            }
        }

        #endregion

        #region Rendering Logic

        /// <summary>
        /// Обработчик события загрузки формы. Запускает асинхронный рендеринг множества.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private async void SelectorForm_Load(object sender, EventArgs e)
        {
            await RenderSetAsync();
        }

        /// <summary>
        /// Обработчик события тика таймера отложенного рендеринга.
        /// Запускает асинхронный рендеринг множества.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private async void RenderDebounceTimer_Tick(object sender, EventArgs e)
        {
            renderDebounceTimer.Stop();
            if (IsHandleCreated && !IsDisposed && !Disposing)
            {
                await RenderSetAsync();
            }
        }

        /// <summary>
        /// Планирует отложенный рендеринг множества. Останавливает текущий таймер и запускает его снова.
        /// </summary>
        private void ScheduleDelayedRender()
        {
            if (IsHandleCreated && !IsDisposed && !Disposing)
            {
                renderDebounceTimer.Stop();
                renderDebounceTimer.Start();
            }
        }

        /// <summary>
        /// Обработчик события изменения размера PictureBox.
        /// Планирует отложенный рендеринг для обновления изображения.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void DisplayPictureBox_Resize(object sender, EventArgs e)
        {
            if (displayPictureBox.Width > 0 && displayPictureBox.Height > 0)
            {
                ScheduleDelayedRender();
            }
        }

        /// <summary>
        /// Асинхронно рендерит множество "Пылающий корабль" в фоновом потоке и обновляет изображение на PictureBox.
        /// </summary>
        /// <returns>Задача, представляющая асинхронную операцию рендеринга.</returns>
        private async Task RenderSetAsync()
        {
            if (isRendering)
            {
                return;
            }
            if (displayPictureBox.Width <= 0 || displayPictureBox.Height <= 0 ||
                !displayPictureBox.IsHandleCreated || displayPictureBox.IsDisposed || Disposing)
            {
                return;
            }

            isRendering = true;

            // Захватываем текущие параметры диапазона для рендеринга в отдельном потоке.
            int currentWidth = displayPictureBox.Width;
            int currentHeight = displayPictureBox.Height;
            double minReCapture = currentMinRe;
            double maxReCapture = currentMaxRe;
            double minImCapture = currentMinIm;
            double maxImCapture = currentMaxIm;

            Bitmap newRenderedBitmap = null;

            try
            {
                newRenderedBitmap = await Task.Run(() =>
                    RenderBurningShipSetInternal(currentWidth, currentHeight, ITERATIONS,
                                                   minReCapture, maxReCapture, minImCapture, maxImCapture));

                // Обновляем UI только если форма все еще активна и не закрывается.
                if (displayPictureBox.IsHandleCreated && !displayPictureBox.IsDisposed && !Disposing)
                {
                    displayPictureBox.Invoke(() =>
                    {
                        if (displayPictureBox.IsHandleCreated && !displayPictureBox.IsDisposed && !Disposing)
                        {
                            Bitmap oldOwnedBitmap = renderedBitmap;
                            renderedBitmap = newRenderedBitmap;

                            // Сохраняем параметры, по которым был отрисован текущий renderedBitmap.
                            renderedMinRe = minReCapture;
                            renderedMaxRe = maxReCapture;
                            renderedMinIm = minImCapture;
                            renderedMaxIm = maxImCapture;

                            // Освобождаем старый Bitmap, если он отличается от нового.
                            if (oldOwnedBitmap != null && oldOwnedBitmap != renderedBitmap)
                            {
                                oldOwnedBitmap.Dispose();
                            }
                            displayPictureBox.Invalidate(); // Запрашиваем перерисовку
                        }
                        else
                        {
                            newRenderedBitmap?.Dispose();
                        }
                    });
                }
                else
                {
                    newRenderedBitmap?.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка рендеринга множества 'Пылающий корабль' для селектора: {ex.Message}");
                newRenderedBitmap?.Dispose();
            }
            finally
            {
                isRendering = false;
            }
        }

        /// <summary>
        /// Рендерит множество "Пылающий корабль" Мандельброта в Bitmap.
        /// </summary>
        /// <param name="canvasWidth">Ширина канваса.</param>
        /// <param name="canvasHeight">Высота канваса.</param>
        /// <param name="maxIter">Максимальное количество итераций.</param>
        /// <param name="minRe">Минимальная действительная часть диапазона.</param>
        /// <param name="maxRe">Максимальная действительная часть диапазона.</param>
        /// <param name="minIm">Минимальная мнимая часть диапазона.</param>
        /// <param name="maxIm">Максимальная мнимая часть диапазона.</param>
        /// <returns>Отрисованный Bitmap.</returns>
        private Bitmap RenderBurningShipSetInternal(int canvasWidth, int canvasHeight, int maxIter,
                                                      double minRe, double maxRe, double minIm, double maxIm)
        {
            Bitmap bitmap = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format24bppRgb);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, canvasWidth, canvasHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            int stride = bitmapData.Stride;
            nint scan0 = bitmapData.Scan0;
            int bufferSize = Math.Abs(stride) * canvasHeight;
            byte[] pixelBuffer = new byte[bufferSize];

            double realRange = maxRe - minRe;
            double imaginaryRange = maxIm - minIm;

            // Заполняем буфер черным цветом, если диапазон некорректен
            if (realRange <= 0 || imaginaryRange <= 0)
            {
                for (int i = 0; i < pixelBuffer.Length; i++)
                {
                    pixelBuffer[i] = 0;
                }
                Marshal.Copy(pixelBuffer, 0, scan0, bufferSize);
                bitmap.UnlockBits(bitmapData);
                return bitmap;
            }

            Parallel.For(0, canvasHeight, yCoord =>
            {
                int rowOffset = yCoord * stride;
                for (int xCoord = 0; xCoord < canvasWidth; xCoord++)
                {
                    // Преобразуем пиксельные координаты в комплексные
                    double cReal = minRe + xCoord / (double)canvasWidth * realRange;
                    double cImaginary = maxIm - yCoord / (double)canvasHeight * imaginaryRange; // Ось Y инвертирована для стандартного отображения фракталов
                    Complex c0 = new Complex(cReal, cImaginary);
                    Complex z = Complex.Zero;
                    int iteration = 0;

                    // Формула итерации для фрактала "Пылающий корабль" (Мандельброт-версия)
                    // Z_n+1 = (|Re(Z_n)| + i * |-Im(Z_n)|)^2 + C
                    while (iteration < maxIter && z.Magnitude < 2.0) // Порог 2.0 является стандартным для Мандельброт-подобных множеств
                    {
                        // Примечание: данная реализация использует -Math.Abs(z.Imaginary),
                        // что соответствует инвертированному фракталу "Пылающий корабль".
                        // Стандартный "Пылающий корабль" обычно использует Math.Abs(z.Imaginary).
                        z = new Complex(Math.Abs(z.Real), -Math.Abs(z.Imaginary));
                        z = z * z + c0;
                        iteration++;
                    }

                    byte redValue, greenValue, blueValue;
                    if (iteration == maxIter)
                    {
                        redValue = greenValue = blueValue = 0; // Внутри множества - черный цвет
                    }
                    else
                    {
                        // Применяем ту же классическую палитру, что и в MandelbrotSelectorForm, для согласованности.
                        double t = (double)iteration / maxIter;
                        if (t < 0.5)
                        {
                            redValue = (byte)(t * 2 * 200);
                            greenValue = (byte)(t * 2 * 50);
                            blueValue = (byte)(t * 2 * 30);
                        }
                        else
                        {
                            t = (t - 0.5) * 2;
                            redValue = (byte)(200 + t * 55);
                            greenValue = (byte)(50 + t * 205);
                            blueValue = (byte)(30 + t * 225);
                        }
                    }
                    int index = rowOffset + xCoord * 3; // Предполагаем 3 байта на пиксель (24bppRgb)
                    pixelBuffer[index] = blueValue;
                    pixelBuffer[index + 1] = greenValue;
                    pixelBuffer[index + 2] = redValue;
                }
            });

            Marshal.Copy(pixelBuffer, 0, scan0, bufferSize);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        #endregion

        #region Canvas Interaction

        /// <summary>
        /// Обработчик события клика мыши по PictureBox.
        /// Выбирает комплексные координаты 'c' в соответствии с позицией клика.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void DisplayPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || displayPictureBox.Width <= 0 || displayPictureBox.Height <= 0)
            {
                return;
            }

            double realRange = currentMaxRe - currentMinRe;
            double imaginaryRange = currentMaxIm - currentMinIm;

            if (realRange <= 0 || imaginaryRange <= 0)
            {
                return;
            }

            // Преобразуем пиксельные координаты клика в комплексные.
            double selectedReal = currentMinRe + e.X / (double)displayPictureBox.Width * realRange;
            double selectedImaginary = currentMaxIm - e.Y / (double)displayPictureBox.Height * imaginaryRange;

            SetSelectedCoordinates(selectedReal, selectedImaginary, true);
        }

        /// <summary>
        /// Обработчик события отрисовки PictureBox.
        /// Рисует отрисованное множество, масштабируя его при необходимости,
        /// а затем рисует маркер выбранной точки 'c'.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события рисования.</param>
        private void DisplayPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (renderedBitmap == null || displayPictureBox.Width <= 0 || displayPictureBox.Height <= 0)
            {
                e.Graphics.Clear(Color.Black); // Очищаем фон, если нет битмапа.
                DrawMarker(e.Graphics); // Всегда рисуем маркер, если есть выбранные координаты.
                return;
            }

            double renderedMinRealParam = renderedMinRe;
            double renderedMaxRealParam = renderedMaxRe;
            double renderedMinImaginaryParam = renderedMinIm;
            double renderedMaxImaginaryParam = renderedMaxIm;

            double currentMinRealParam = currentMinRe;
            double currentMaxRealParam = currentMaxRe;
            double currentMinImaginaryParam = currentMinIm;
            double currentMaxImaginaryParam = currentMaxIm;

            double renderedComplexWidth = renderedMaxRealParam - renderedMinRealParam;
            double renderedComplexHeight = renderedMaxImaginaryParam - renderedMinImaginaryParam;

            double currentComplexWidth = currentMaxRealParam - currentMinRealParam;
            double currentComplexHeight = currentMaxImaginaryParam - currentMinImaginaryParam;

            // Если какой-либо диапазон некорректен, очищаем и рисуем только маркер.
            if (renderedComplexWidth <= 0 || renderedComplexHeight <= 0 || currentComplexWidth <= 0 || currentComplexHeight <= 0)
            {
                e.Graphics.Clear(Color.Black);
                if (renderedBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(renderedBitmap, Point.Empty); // Fallback для отображения, если масштабирование невозможно
                }
                DrawMarker(e.Graphics);
                return;
            }

            // Вычисляем положение и размер для отрисовки исходного битмапа с учетом нового зума и панорамирования.
            float offsetX = (float)((renderedMinRealParam - currentMinRealParam) / currentComplexWidth * displayPictureBox.Width);
            float offsetY = (float)((currentMaxImaginaryParam - renderedMaxImaginaryParam) / currentComplexHeight * displayPictureBox.Height);

            float destinationWidthPixels = (float)(renderedComplexWidth / currentComplexWidth * displayPictureBox.Width);
            float destinationHeightPixels = (float)(renderedComplexHeight / currentComplexHeight * displayPictureBox.Height);

            PointF destinationPoint1 = new PointF(offsetX, offsetY);
            PointF destinationPoint2 = new PointF(offsetX + destinationWidthPixels, offsetY);
            PointF destinationPoint3 = new PointF(offsetX, offsetY + destinationHeightPixels);

            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half; // Для лучшего качества при масштабировании

            if (destinationWidthPixels > 0 && destinationHeightPixels > 0)
            {
                try
                {
                    e.Graphics.DrawImage(renderedBitmap, new PointF[] { destinationPoint1, destinationPoint2, destinationPoint3 });
                }
                catch (ArgumentException)
                {
                    // Если произошла ошибка (например, из-за слишком экстремальных значений),
                    // рисуем битмап без масштабирования как запасной вариант.
                    if (renderedBitmap != null)
                    {
                        e.Graphics.DrawImageUnscaled(renderedBitmap, Point.Empty);
                    }
                }
            }
            else
            {
                // Если новые размеры некорректны, просто рисуем без масштабирования.
                if (renderedBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(renderedBitmap, Point.Empty);
                }
            }

            DrawMarker(e.Graphics); // Рисуем маркер выбранной точки.
        }

        /// <summary>
        /// Рисует маркер выбранных координат 'c' на графическом контексте.
        /// </summary>
        /// <param name="graphics">Графический контекст для рисования.</param>
        private void DrawMarker(Graphics graphics)
        {
            if (!float.IsNaN(selectedComplexCoords.X) && displayPictureBox.Width > 0 && displayPictureBox.Height > 0)
            {
                double realRange = currentMaxRe - currentMinRe;
                double imaginaryRange = currentMaxIm - currentMinIm;

                if (realRange > 0 && imaginaryRange > 0)
                {
                    try
                    {
                        checked // Включаем проверку на переполнение для целочисленных преобразований.
                        {
                            // Преобразуем комплексные координаты маркера в пиксельные.
                            int markerX = (int)((selectedComplexCoords.X - currentMinRe) / realRange * displayPictureBox.Width);
                            int markerY = (int)((currentMaxIm - selectedComplexCoords.Y) / imaginaryRange * displayPictureBox.Height);

                            int markerSize = 9;
                            using (Pen markerPen = new Pen(Color.FromArgb(220, Color.Green), 2f))
                            {
                                graphics.DrawLine(markerPen, markerX - markerSize, markerY, markerX + markerSize, markerY);
                                graphics.DrawLine(markerPen, markerX, markerY - markerSize, markerX, markerY + markerSize);
                            }
                        }
                    }
                    catch (OverflowException ex)
                    {
                        // Если происходит переполнение при вычислении координат маркера,
                        // выводим сообщение об ошибке (например, в заголовок формы).
                        string errorMessage = $"Переполнение при отрисовке маркера: {ex.Message}";
                        if (IsHandleCreated && !IsDisposed && !Disposing)
                        {
                            Invoke(() => { Text = errorMessage; });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик события прокрутки колеса мыши.
        /// Изменяет масштаб отображаемого множества.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void DisplayPictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (displayPictureBox.Width <= 0 || displayPictureBox.Height <= 0)
            {
                return;
            }

            double zoomFactor = e.Delta > 0 ? 1.2 : 1.0 / 1.2; // Коэффициент зума
            double oldReRange = currentMaxRe - currentMinRe;
            double oldImRange = currentMaxIm - currentMinIm;

            if (oldReRange <= 0 || oldImRange <= 0)
            {
                return;
            }

            // Вычисляем мировые координаты точки под курсором до изменения масштаба.
            double mouseReal = currentMinRe + e.X / (double)displayPictureBox.Width * oldReRange;
            double mouseImaginary = currentMaxIm - e.Y / (double)displayPictureBox.Height * oldImRange;

            double newReRange = oldReRange / zoomFactor;
            double newImRange = oldImRange / zoomFactor;

            // Ограничения для диапазона масштабирования, чтобы избежать слишком больших/маленьких значений.
            const double MIN_RANGE_ALLOWED = 1e-13;
            const double MAX_RANGE_ALLOWED = 100.0;
            // Дополнительные проверки на минимальные размеры изображения
            const double MIN_IMAGE_WIDTH_REAL = 1e-9;
            const double MIN_IMAGE_HEIGHT_IMAGINARY = 1e-9;


            if (newReRange < MIN_RANGE_ALLOWED || newImRange < MIN_RANGE_ALLOWED ||
                newReRange > MAX_RANGE_ALLOWED || newImRange > MAX_RANGE_ALLOWED ||
                newReRange < MIN_IMAGE_WIDTH_REAL || newImRange < MIN_IMAGE_HEIGHT_IMAGINARY)
            {
                return;
            }

            double reRatio = e.X / (double)displayPictureBox.Width;
            double imRatio = e.Y / (double)displayPictureBox.Height;

            // Пересчитываем новый диапазон так, чтобы точка под курсором осталась на месте.
            currentMinRe = mouseReal - reRatio * newReRange;
            currentMaxRe = currentMinRe + newReRange;
            currentMinIm = mouseImaginary - (1.0 - imRatio) * newImRange;
            currentMaxIm = currentMinIm + newImRange;

            displayPictureBox.Invalidate(); // Запрашиваем перерисовку для немедленного отображения зума.
            ScheduleDelayedRender(); // Планируем новый рендеринг для высокой четкости.
        }

        /// <summary>
        /// Обработчик события нажатия кнопки мыши.
        /// Инициирует режим панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void DisplayPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panning = true;
                panStart = e.Location;
            }
        }

        /// <summary>
        /// Обработчик события перемещения мыши.
        /// Выполняет панорамирование отображаемого множества.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void DisplayPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (panning)
            {
                if (displayPictureBox.Width <= 0 || displayPictureBox.Height <= 0)
                {
                    return;
                }

                double realRange = currentMaxRe - currentMinRe;
                double imaginaryRange = currentMaxIm - currentMinIm;

                if (realRange <= 0 || imaginaryRange <= 0)
                {
                    return;
                }

                // Вычисляем смещение в пикселях.
                double deltaXPixels = panStart.X - e.X;
                double deltaYPixels = panStart.Y - e.Y;

                // Преобразуем смещение из пикселей в комплексные координаты.
                double deltaXComplex = deltaXPixels * (realRange / displayPictureBox.Width);
                double deltaYComplex = deltaYPixels * (imaginaryRange / displayPictureBox.Height);

                // Смещаем диапазон отображения.
                currentMinRe += deltaXComplex;
                currentMaxRe += deltaXComplex;
                currentMinIm -= deltaYComplex; // Ось Y на экране инвертирована относительно мнимой оси.
                currentMaxIm -= deltaYComplex;

                panStart = e.Location; // Обновляем начальную точку панорамирования.

                displayPictureBox.Invalidate(); // Запрашиваем перерисовку для плавного панорамирования.
                ScheduleDelayedRender(); // Планируем новый рендеринг для высокой четкости.
            }
        }

        /// <summary>
        /// Обработчик события отпускания кнопки мыши.
        /// Завершает режим панорамирования.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события мыши.</param>
        private void DisplayPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panning = false;
            }
        }

        #endregion

        #region Form Lifecycle

        /// <summary>
        /// Обработчик события закрытия формы.
        /// Освобождает ресурсы и отключает таймер.
        /// </summary>
        /// <param name="e">Аргументы события.</param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            renderDebounceTimer?.Stop();
            renderDebounceTimer?.Dispose();
            renderDebounceTimer = null;

            renderedBitmap?.Dispose();
            renderedBitmap = null;

            base.OnFormClosed(e);
        }

        #endregion
    }
}