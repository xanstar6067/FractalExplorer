using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FractalExplorer.Engines; // Для PhoenixEngine и ComplexDecimal
using FractalExplorer.Resources; // Для IFractalForm (если понадобится для LoupeZoom)

namespace FractalExplorer.SelectorsForms
{
    public partial class PhoenixCSelectorForm : Form
    {
        #region Fields
        private readonly FractalExplorer.Forms.FractalPhoenixForm _ownerForm; // Ссылка на основную форму

        // Параметры для среза по P (Q фиксировано)
        private Bitmap _slicePBitmap;
        private double _slicePMinRe = -2.0;
        private double _slicePMaxRe = 2.0;
        private double _slicePMinIm = -2.0;
        private double _slicePMaxIm = 2.0;
        private double _renderedSlicePMinRe, _renderedSlicePMaxRe, _renderedSlicePMinIm, _renderedSlicePMaxIm;
        private Point _panStartSliceP;
        private bool _panningSliceP = false;
        private CancellationTokenSource _ctsSliceP;
        private volatile bool _isRenderingSliceP = false;
        private System.Windows.Forms.Timer _renderDebounceTimerSliceP;

        // Параметры для среза по Q (P фиксировано)
        private Bitmap _sliceQBitmap;
        private double _sliceQMinRe = -2.0;
        private double _sliceQMaxRe = 2.0;
        private double _sliceQMinIm = -2.0;
        private double _sliceQMaxIm = 2.0;
        private double _renderedSliceQMinRe, _renderedSliceQMaxRe, _renderedSliceQMinIm, _renderedSliceQMaxIm;
        private Point _panStartSliceQ;
        private bool _panningSliceQ = false;
        private CancellationTokenSource _ctsSliceQ;
        private volatile bool _isRenderingSliceQ = false;
        private System.Windows.Forms.Timer _renderDebounceTimerSliceQ;


        private ComplexDecimal _currentC1_P; // Текущий P = (nudPReal, nudPImaginary)
        private ComplexDecimal _currentC1_Q; // Текущий Q = (nudQReal, nudQImaginary)
        // C2 берем из основной формы и не меняем в селекторе
        private ComplexDecimal _fixedC2;

        private const int SLICE_ITERATIONS = 75; // Итерации для рендера срезов
        private const int RENDER_DEBOUNCE_MILLISECONDS = 300;

        // Движок для рендеринга срезов
        private readonly PhoenixEngine _sliceRenderEngine;
        #endregion

        #region Events
        public event Action<ComplexDecimal, ComplexDecimal> ParametersSelected; // C1, C2
        #endregion

        #region Constructor
        public PhoenixCSelectorForm(FractalExplorer.Forms.FractalPhoenixForm owner, ComplexDecimal initialC1, ComplexDecimal initialC2)
        {
            InitializeComponent();
            _ownerForm = owner;
            _currentC1_P = new ComplexDecimal(initialC1.Real, 0); // Для среза P, P.Im будет второй осью
            _currentC1_Q = new ComplexDecimal(0, initialC1.Imaginary); // Для среза Q, Q.Re будет второй осью
            // Более точно:
            // _currentC1_P - это будет сам параметр P, который мы выбираем на sliceCanvasP. Его Re и Im - это оси.
            // _currentC1_Q - это будет сам параметр Q, который мы выбираем на sliceCanvasQ. Его Re и Im - это оси.

            // Инициализируем NumericUpDowns из initialC1
            nudPReal.Value = initialC1.Real;
            nudPImaginary.Value = 0; // Это будет ось Y для среза P
            nudQReal.Value = 0;      // Это будет ось X для среза Q
            nudQImaginary.Value = initialC1.Imaginary;

            _fixedC2 = initialC2;

            _sliceRenderEngine = new PhoenixEngine
            {
                MaxIterations = SLICE_ITERATIONS,
                ThresholdSquared = 4.0m, // Стандартный порог
                // Палитру установим при рендере
                C2 = _fixedC2 // C2 фиксирован для рендера срезов
            };

            SetupSliceCanvas(sliceCanvasP, ref _renderedSlicePMinRe, ref _renderedSlicePMaxRe, ref _renderedSlicePMinIm, ref _renderedSlicePMaxIm, progressBarSliceP);
            SetupSliceCanvas(sliceCanvasQ, ref _renderedSliceQMinRe, ref _renderedSliceQMaxRe, ref _renderedSliceQMinIm, ref _renderedSliceQMaxIm, progressBarSliceQ);

            _renderDebounceTimerSliceP = new System.Windows.Forms.Timer { Interval = RENDER_DEBOUNCE_MILLISECONDS };
            _renderDebounceTimerSliceP.Tick += (s, e) => ScheduleRenderSliceP();
            _renderDebounceTimerSliceQ = new System.Windows.Forms.Timer { Interval = RENDER_DEBOUNCE_MILLISECONDS };
            _renderDebounceTimerSliceQ.Tick += (s, e) => ScheduleRenderSliceQ();


            this.Load += SelectorForm_Load;

            // Подписки для NumericUpDowns
            nudPReal.ValueChanged += NudValues_Changed;
            nudPImaginary.ValueChanged += NudValues_Changed; // Это будет виртуальная ось Y среза P
            nudQReal.ValueChanged += NudValues_Changed;      // Это будет виртуальная ось X среза Q
            nudQImaginary.ValueChanged += NudValues_Changed;

            UpdateFixedValueLabels();
        }

        private void SelectorForm_Load(object sender, EventArgs e)
        {
            // Устанавливаем начальные значения для рендеринга (копируем из текущих)
            _renderedSlicePMinRe = _slicePMinRe; _renderedSlicePMaxRe = _slicePMaxRe;
            _renderedSlicePMinIm = _slicePMinIm; _renderedSlicePMaxIm = _slicePMaxIm;
            _renderedSliceQMinRe = _sliceQMinRe; _renderedSliceQMaxRe = _sliceQMaxRe;
            _renderedSliceQMinIm = _sliceQMinIm; _renderedSliceQMaxIm = _sliceQMaxIm;

            Task.Run(() => RenderSlicePAsync());
            Task.Run(() => RenderSliceQAsync());
        }

        private void SetupSliceCanvas(PictureBox canvas, ref double minRe, ref double maxRe, ref double minIm, ref double maxIm, ProgressBar pb)
        {
            canvas.Paint += (s, e) => SliceCanvas_Paint(s, e, canvas == sliceCanvasP);
            canvas.MouseClick += (s, e) => SliceCanvas_MouseClick(s, e, canvas == sliceCanvasP);
            canvas.MouseWheel += (s, e) => SliceCanvas_MouseWheel(s, e, canvas == sliceCanvasP);
            canvas.MouseDown += (s, e) => SliceCanvas_MouseDown(s, e, canvas == sliceCanvasP);
            canvas.MouseMove += (s, e) => SliceCanvas_MouseMove(s, e, canvas == sliceCanvasP);
            canvas.MouseUp += (s, e) => SliceCanvas_MouseUp(s, e, canvas == sliceCanvasP);
            canvas.Resize += (s, e) => {
                if (canvas.Width > 0 && canvas.Height > 0)
                {
                    if (canvas == sliceCanvasP) ScheduleRenderSliceP(true); else ScheduleRenderSliceQ(true);
                }
            };
            pb.Visible = false;
        }
        #endregion

        #region UI Update and Value Handling
        private void NudValues_Changed(object sender, EventArgs e)
        {
            UpdateFixedValueLabels();

            // Перерисовываем ОБА канваса, чтобы обновить маркеры
            sliceCanvasP.Invalidate();
            sliceCanvasQ.Invalidate();

            // Если изменилось значение P (nudPReal), которое используется как фиксированное для среза Q,
            // то инициируем перерендер среза Q.
            if (sender == nudPReal)
            {
                ScheduleRenderSliceQ(true); // true для немедленного рендера
            }
            // Если изменилось значение Q (nudQImaginary), которое используется как фиксированное для среза P,
            // то инициируем перерендер среза P.
            else if (sender == nudQImaginary)
            {
                ScheduleRenderSliceP(true);
            }
            // Если пользователь вручную изменил nudPImaginary или nudQReal,
            // которые влияют на z0 при рендеринге соответствующего среза,
            // также нужно перерендерить этот срез.
            else if (sender == nudPImaginary) // nudPImaginary влияет на z0 для среза P
            {
                ScheduleRenderSliceP(true);
            }
            else if (sender == nudQReal) // nudQReal в текущей реализации не влияет на z0 для среза Q, но если бы влиял, то:
            {
                // ScheduleRenderSliceQ(true); // Если бы nudQReal влиял на z0 для среза Q
            }
        }

        private void UpdateFixedValueLabels()
        {
            // Для среза P (оси P.Re, P.Im), Q фиксирован значениями из nudQReal и nudQImaginary
            ComplexDecimal fixedQ = new ComplexDecimal(nudQReal.Value, nudQImaginary.Value);
            lblFixedQForPSlice.Text = $"(Q = {fixedQ.Real:F4} ; {fixedQ.Imaginary:F4}i)";

            // Для среза Q (оси Q.Re, Q.Im), P фиксирован значениями из nudPReal и nudPImaginary
            ComplexDecimal fixedP = new ComplexDecimal(nudPReal.Value, nudPImaginary.Value);
            lblFixedPForQSlice.Text = $"(P = {fixedP.Real:F4} ; {fixedP.Imaginary:F4}i)";
        }


        public void SetSelectedParameters(ComplexDecimal c1)
        {
            bool changed = false;
            if (nudPReal.Value != c1.Real) { nudPReal.Value = c1.Real; changed = true; }
            if (nudQImaginary.Value != c1.Imaginary) { nudQImaginary.Value = c1.Imaginary; changed = true; }

            // nudPImaginary и nudQReal - это оси срезов, они не должны меняться этим методом,
            // они меняются кликом по срезу или ручным вводом.
            // Если они тоже должны меняться, то нужно передавать и "осевые" значения.

            if (changed)
            {
                _currentC1_P = new ComplexDecimal(nudPReal.Value, nudPImaginary.Value); // Обновляем P
                _currentC1_Q = new ComplexDecimal(nudQReal.Value, nudQImaginary.Value); // Обновляем Q
                UpdateFixedValueLabels();
                sliceCanvasP.Invalidate();
                sliceCanvasQ.Invalidate();
            }
        }

        #endregion

        #region Rendering Slices

        private Func<int, int, int, Color> GetClassicPalette()
        {
            // Палитра "Классика" из ColorPaletteMandelbrotFamily
            var classicColors = new List<Color> { Color.FromArgb(0, 0, 0), Color.FromArgb(200, 50, 30), Color.FromArgb(255, 255, 255) };
            int colorCount = classicColors.Count;

            return (iter, maxIter, maxColorIterParam) => // maxColorIterParam здесь будет SLICE_ITERATIONS
            {
                if (iter == maxIter) return Color.Black;
                if (maxColorIterParam <= 1) return classicColors[0];

                double t = (double)Math.Min(iter, maxColorIterParam - 1) / (maxColorIterParam - 1);
                double scaledT = t * (colorCount - 1);
                int index1 = (int)Math.Floor(scaledT);
                int index2 = Math.Min(index1 + 1, colorCount - 1);
                double localT = scaledT - index1;
                index1 = Math.Max(0, Math.Min(index1, colorCount - 1));
                index2 = Math.Max(0, Math.Min(index2, colorCount - 1));
                return LerpColor(classicColors[index1], classicColors[index2], localT);
            };
        }
        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }


        private void ScheduleRenderSliceP(bool immediate = false)
        {
            _renderDebounceTimerSliceP.Stop();
            if (immediate) RenderSlicePAsync(); else _renderDebounceTimerSliceP.Start();
        }
        private void ScheduleRenderSliceQ(bool immediate = false)
        {
            _renderDebounceTimerSliceQ.Stop();
            if (immediate) RenderSliceQAsync(); else _renderDebounceTimerSliceQ.Start();
        }

        private async Task RenderSlicePAsync()
        {
            if (_isRenderingSliceP || sliceCanvasP.Width <= 0 || sliceCanvasP.Height <= 0) return;
            _isRenderingSliceP = true;
            _ctsSliceP?.Cancel();
            _ctsSliceP = new CancellationTokenSource();
            var token = _ctsSliceP.Token;

            if (progressBarSliceP.IsHandleCreated && !progressBarSliceP.IsDisposed)
                progressBarSliceP.Invoke((Action)(() => { progressBarSliceP.Value = 0; progressBarSliceP.Visible = true; }));

            int w = sliceCanvasP.Width;
            int h = sliceCanvasP.Height;
            double minR_axis = _slicePMinRe; // Диапазон для Re(P) - ось X
            double maxR_axis = _slicePMaxRe;
            double minI_axis = _slicePMinIm; // Диапазон для Im(P) - ось Y
            double maxI_axis = _slicePMaxIm;

            // Q фиксировано: берем компоненты из соответствующих NumericUpDown
            ComplexDecimal fixedQ_for_slice = new ComplexDecimal(nudQReal.Value, nudQImaginary.Value);

            _sliceRenderEngine.Palette = GetClassicPalette();
            _sliceRenderEngine.MaxColorIterations = SLICE_ITERATIONS;

            Bitmap newBitmap = null;
            try
            {
                newBitmap = await Task.Run(() =>
                {
                    Bitmap bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, bmp.PixelFormat);
                    byte[] buffer = new byte[Math.Abs(bmpData.Stride) * h];
                    long renderedPixels = 0;

                    Parallel.For(0, h, new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = Environment.ProcessorCount }, y_pixel =>
                    {
                        if (token.IsCancellationRequested) return;
                        for (int x_pixel = 0; x_pixel < w; x_pixel++)
                        {
                            // Преобразуем пиксельные координаты в значения для P
                            decimal pRe_val = (decimal)(minR_axis + x_pixel * (maxR_axis - minR_axis) / w);
                            decimal pIm_val = (decimal)(maxI_axis - y_pixel * (maxI_axis - minI_axis) / h); // Y инвертирован

                            ComplexDecimal currentP_param = new ComplexDecimal(pRe_val, pIm_val);

                            // Формируем C1 для движка: C1.Real = P.Re, C1.Imaginary = Q.Im (если Q скаляр, или Q.Re если P.Im=0)
                            // В нашем движке C1.Real = P, C1.Imaginary = Q.
                            // P у нас комплексное (currentP_param). Q у нас комплексное (fixedQ_for_slice).
                            // Это не соответствует формуле с скалярными P и Q.
                            // Формула в движке: x_next = z_curr.Re^2 - z_curr.Im^2 + C1.Real + C1.Imaginary * z_prev.Re;
                            //                                                        P_scalar  Q_scalar
                            // Значит, C1.Real должен быть P, а C1.Imaginary должен быть Q.
                            // Если мы рендерим срез по P (currentP_param), то P должен быть скаляром pRe_val (X-ось).
                            // А pIm_val (Y-ось) может влиять на z0 или быть проигнорирован для выбора P.
                            // Для простоты, пусть P = pRe_val, а Q = fixedQ_for_slice.Imaginary (скаляр).
                            // Если мы хотим 2D срез для P, то P само должно быть комплексным в формуле.

                            // ИСПРАВЛЕНИЕ ЛОГИКИ:
                            // Для среза P, мы перебираем pRe_val (ось X) и pIm_val (ось Y).
                            // Эти два значения вместе формируют *скаляр P* и *скаляр Q* для передачи в движок.
                            // Например: P = pRe_val, Q = pIm_val. Это один вариант.
                            // Или: P = pRe_val, а Q фиксировано (из nudQImaginary.Value). pIm_val (ось Y) влияет на z0.Im.
                            //
                            // Давай придерживаться идеи, что sliceCanvasP выбирает P (скаляр) и sliceCanvasQ выбирает Q (скаляр).
                            // Ось X канваса P = значение P. Ось Y канваса P = Im(z0).
                            // Ось X канваса Q = значение Q. Ось Y канваса Q = Im(z0).

                            decimal p_scalar_for_engine = pRe_val; // P берем с X-оси среза P
                            decimal q_scalar_for_engine = fixedQ_for_slice.Imaginary; // Q берем из фиксированного значения (компонента Q)

                            ComplexDecimal c1_engine_param = new ComplexDecimal(p_scalar_for_engine, q_scalar_for_engine);
                            ComplexDecimal z0_for_slice = new ComplexDecimal(0, pIm_val); // Y-ось среза P влияет на Im(z0)

                            int iter = _sliceRenderEngine.CalculateIterations(z0_for_slice, ComplexDecimal.Zero, c1_engine_param, _fixedC2);
                            Color c = _sliceRenderEngine.Palette(iter, SLICE_ITERATIONS, SLICE_ITERATIONS);
                            int idx = y_pixel * bmpData.Stride + x_pixel * 3;
                            buffer[idx] = c.B; buffer[idx + 1] = c.G; buffer[idx + 2] = c.R;
                        }
                        long currentProgress = Interlocked.Increment(ref renderedPixels);
                        if (progressBarSliceP.IsHandleCreated && !progressBarSliceP.IsDisposed)
                            progressBarSliceP.Invoke((Action)(() => progressBarSliceP.Value = (int)(100.0 * currentProgress / h)));
                    });
                    token.ThrowIfCancellationRequested();
                    Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                    bmp.UnlockBits(bmpData);
                    return bmp;
                }, token);

                if (sliceCanvasP.IsHandleCreated && !sliceCanvasP.IsDisposed)
                {
                    sliceCanvasP.Invoke((Action)(() => {
                        _slicePBitmap?.Dispose();
                        _slicePBitmap = newBitmap;
                        _renderedSlicePMinRe = minR_axis; _renderedSlicePMaxRe = maxR_axis;
                        _renderedSlicePMinIm = minI_axis; _renderedSlicePMaxIm = maxI_axis;
                        sliceCanvasP.Invalidate();
                    }));
                }
                else { newBitmap?.Dispose(); }
            }
            catch (OperationCanceledException) { newBitmap?.Dispose(); }
            catch (Exception ex) { newBitmap?.Dispose(); MessageBox.Show($"Ошибка рендера среза P: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally
            {
                _isRenderingSliceP = false;
                if (progressBarSliceP.IsHandleCreated && !progressBarSliceP.IsDisposed)
                    progressBarSliceP.Invoke((Action)(() => { progressBarSliceP.Visible = false; progressBarSliceP.Value = 0; }));
            }
        }

        private async Task RenderSliceQAsync()
        {
            if (_isRenderingSliceQ || sliceCanvasQ.Width <= 0 || sliceCanvasQ.Height <= 0) return;
            _isRenderingSliceQ = true;
            _ctsSliceQ?.Cancel();
            _ctsSliceQ = new CancellationTokenSource();
            var token = _ctsSliceQ.Token;

            if (progressBarSliceQ.IsHandleCreated && !progressBarSliceQ.IsDisposed)
                progressBarSliceQ.Invoke((Action)(() => { progressBarSliceQ.Value = 0; progressBarSliceQ.Visible = true; }));

            int w = sliceCanvasQ.Width;
            int h = sliceCanvasQ.Height;
            double minR_axis = _sliceQMinRe; // Диапазон для Re(Q) - ось X
            double maxR_axis = _sliceQMaxRe;
            double minI_axis = _sliceQMinIm; // Диапазон для Im(Q) - ось Y
            double maxI_axis = _sliceQMaxIm;

            // P фиксировано: берем компоненты из соответствующих NumericUpDown
            ComplexDecimal fixedP_for_slice = new ComplexDecimal(nudPReal.Value, nudPImaginary.Value);

            _sliceRenderEngine.Palette = GetClassicPalette();
            _sliceRenderEngine.MaxColorIterations = SLICE_ITERATIONS;

            Bitmap newBitmap = null;
            try
            {
                newBitmap = await Task.Run(() =>
                {
                    Bitmap bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, bmp.PixelFormat);
                    byte[] buffer = new byte[Math.Abs(bmpData.Stride) * h];
                    long renderedPixels = 0;

                    Parallel.For(0, h, new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = Environment.ProcessorCount }, y_pixel =>
                    {
                        if (token.IsCancellationRequested) return;
                        for (int x_pixel = 0; x_pixel < w; x_pixel++)
                        {
                            // Преобразуем пиксельные координаты в значения для Q
                            decimal qRe_val = (decimal)(minR_axis + x_pixel * (maxR_axis - minR_axis) / w);
                            decimal qIm_val = (decimal)(maxI_axis - y_pixel * (maxI_axis - minI_axis) / h); // Y инвертирован

                            // ИСПРАВЛЕНИЕ ЛОГИКИ:
                            // P = fixedP_for_slice.Real (скаляр)
                            // Q = qRe_val (скаляр, с X-оси среза Q)
                            // Y-ось среза Q (qIm_val) влияет на z0.Im
                            decimal p_scalar_for_engine = fixedP_for_slice.Real;
                            decimal q_scalar_for_engine = qRe_val;

                            ComplexDecimal c1_engine_param = new ComplexDecimal(p_scalar_for_engine, q_scalar_for_engine);
                            ComplexDecimal z0_for_slice = new ComplexDecimal(0, qIm_val); // Y-ось среза Q влияет на Im(z0)

                            int iter = _sliceRenderEngine.CalculateIterations(z0_for_slice, ComplexDecimal.Zero, c1_engine_param, _fixedC2);
                            Color c = _sliceRenderEngine.Palette(iter, SLICE_ITERATIONS, SLICE_ITERATIONS);
                            int idx = y_pixel * bmpData.Stride + x_pixel * 3;
                            buffer[idx] = c.B; buffer[idx + 1] = c.G; buffer[idx + 2] = c.R;
                        }
                        long currentProgress = Interlocked.Increment(ref renderedPixels);
                        if (progressBarSliceQ.IsHandleCreated && !progressBarSliceQ.IsDisposed)
                            progressBarSliceQ.Invoke((Action)(() => progressBarSliceQ.Value = (int)(100.0 * currentProgress / h)));
                    });
                    token.ThrowIfCancellationRequested();
                    Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                    bmp.UnlockBits(bmpData);
                    return bmp;
                }, token);

                if (sliceCanvasQ.IsHandleCreated && !sliceCanvasQ.IsDisposed)
                {
                    sliceCanvasQ.Invoke((Action)(() => {
                        _sliceQBitmap?.Dispose();
                        _sliceQBitmap = newBitmap;
                        _renderedSliceQMinRe = minR_axis; _renderedSliceQMaxRe = maxR_axis;
                        _renderedSliceQMinIm = minI_axis; _renderedSliceQMaxIm = maxI_axis;
                        sliceCanvasQ.Invalidate();
                    }));
                }
                else { newBitmap?.Dispose(); }
            }
            catch (OperationCanceledException) { newBitmap?.Dispose(); }
            catch (Exception ex) { newBitmap?.Dispose(); MessageBox.Show($"Ошибка рендера среза Q: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally
            {
                _isRenderingSliceQ = false;
                if (progressBarSliceQ.IsHandleCreated && !progressBarSliceQ.IsDisposed)
                    progressBarSliceQ.Invoke((Action)(() => { progressBarSliceQ.Visible = false; progressBarSliceQ.Value = 0; }));
            }
        }


        #endregion

        #region Canvas Interaction (Common for both slices)

        private void SliceCanvas_Paint(object sender, PaintEventArgs e, bool isPSlice)
        {
            PictureBox canvas = sender as PictureBox;
            Bitmap bmp = isPSlice ? _slicePBitmap : _sliceQBitmap;
            double rMin = isPSlice ? _renderedSlicePMinRe : _renderedSliceQMinRe;
            double rMax = isPSlice ? _renderedSlicePMaxRe : _renderedSliceQMaxRe;
            double iMin = isPSlice ? _renderedSlicePMinIm : _renderedSliceQMinIm;
            double iMax = isPSlice ? _renderedSlicePMaxIm : _renderedSliceQMaxIm;
            double cMinRe = isPSlice ? _slicePMinRe : _sliceQMinRe;
            double cMaxRe = isPSlice ? _slicePMaxRe : _sliceQMaxRe;
            double cMinIm = isPSlice ? _slicePMinIm : _sliceQMinIm;
            double cMaxIm = isPSlice ? _slicePMaxIm : _sliceQMaxIm;

            e.Graphics.Clear(Color.DimGray);
            if (bmp == null || canvas.Width <= 0 || canvas.Height <= 0)
            {
                DrawMarker(e.Graphics, canvas, isPSlice);
                return;
            }

            double renderedW = rMax - rMin; double renderedH = iMax - iMin;
            double currentW = cMaxRe - cMinRe; double currentH = cMaxIm - cMinIm;

            if (renderedW <= 0 || renderedH <= 0 || currentW <= 0 || currentH <= 0)
            {
                e.Graphics.DrawImageUnscaled(bmp, Point.Empty); DrawMarker(e.Graphics, canvas, isPSlice); return;
            }

            float offX = (float)((rMin - cMinRe) / currentW * canvas.Width);
            float offY = (float)((cMaxIm - iMax) / currentH * canvas.Height);
            float destW = (float)(renderedW / currentW * canvas.Width);
            float destH = (float)(renderedH / currentH * canvas.Height);

            if (destW > 0 && destH > 0)
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.DrawImage(bmp, new RectangleF(offX, offY, destW, destH));
            }
            else
            {
                e.Graphics.DrawImageUnscaled(bmp, Point.Empty);
            }
            DrawMarker(e.Graphics, canvas, isPSlice);
        }

        private void DrawMarker(Graphics g, PictureBox canvas, bool isPSlice)
        {
            decimal valRe, valIm;
            double minRe, maxRe, minIm, maxIm;

            if (isPSlice)
            {
                valRe = nudPReal.Value; // Ось X среза P - это P.Re
                valIm = nudPImaginary.Value; // Ось Y среза P - это P.Im
                minRe = _slicePMinRe; maxRe = _slicePMaxRe;
                minIm = _slicePMinIm; maxIm = _slicePMaxIm;
            }
            else // QSlice
            {
                valRe = nudQReal.Value; // Ось X среза Q - это Q.Re
                valIm = nudQImaginary.Value; // Ось Y среза Q - это Q.Im
                minRe = _sliceQMinRe; maxRe = _sliceQMaxRe;
                minIm = _sliceQMinIm; maxIm = _sliceQMaxIm;
            }

            double rRange = maxRe - minRe;
            double iRange = maxIm - minIm;

            if (rRange > 0 && iRange > 0 && canvas.Width > 0 && canvas.Height > 0)
            {
                int markerX = (int)(((double)valRe - minRe) / rRange * canvas.Width);
                int markerY = (int)((maxIm - (double)valIm) / iRange * canvas.Height); // Y инвертирован
                int size = 7;
                using (Pen p = new Pen(Color.LimeGreen, 2))
                {
                    g.DrawLine(p, markerX - size, markerY, markerX + size, markerY);
                    g.DrawLine(p, markerX, markerY - size, markerX, markerY + size);
                }
            }
        }


        private void SliceCanvas_MouseClick(object sender, MouseEventArgs e, bool isPSlice)
        {
            PictureBox canvas = sender as PictureBox;
            if (e.Button != MouseButtons.Left || canvas.Width <= 0 || canvas.Height <= 0) return;

            double currentMinRe = isPSlice ? _slicePMinRe : _sliceQMinRe;
            double currentMaxRe = isPSlice ? _slicePMaxRe : _sliceQMaxRe;
            double currentMinIm = isPSlice ? _slicePMinIm : _sliceQMinIm;
            double currentMaxIm = isPSlice ? _slicePMaxIm : _sliceQMaxIm;

            double realRange = currentMaxRe - currentMinRe;
            double imaginaryRange = currentMaxIm - currentMinIm;

            if (realRange <= 0 || imaginaryRange <= 0) return;

            decimal selectedRe = (decimal)(currentMinRe + e.X / (double)canvas.Width * realRange);
            decimal selectedIm = (decimal)(currentMaxIm - e.Y / (double)canvas.Height * imaginaryRange);

            if (isPSlice)
            {
                nudPReal.Value = selectedRe;
                nudPImaginary.Value = selectedIm; // P.Im - это Y-ось среза
            }
            else // QSlice
            {
                nudQReal.Value = selectedRe;
                nudQImaginary.Value = selectedIm; // Q.Im - это Y-ось среза
            }
            // NudValues_Changed вызовется автоматически и обновит _currentC1_P/_Q и лейблы
        }


        private void SliceCanvas_MouseWheel(object sender, MouseEventArgs e, bool isPSlice)
        {
            PictureBox canvas = sender as PictureBox;
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            double zoomFactor = e.Delta > 0 ? 1.25 : 1.0 / 1.25;
            ref double minRe = ref (isPSlice ? ref _slicePMinRe : ref _sliceQMinRe);
            ref double maxRe = ref (isPSlice ? ref _slicePMaxRe : ref _sliceQMaxRe);
            ref double minIm = ref (isPSlice ? ref _slicePMinIm : ref _sliceQMinIm);
            ref double maxIm = ref (isPSlice ? ref _slicePMaxIm : ref _sliceQMaxIm);

            double oldReRange = maxRe - minRe;
            double oldImRange = maxIm - minIm;
            if (oldReRange <= 0 || oldImRange <= 0) return;

            double mouseReal = minRe + e.X / (double)canvas.Width * oldReRange;
            double mouseImaginary = maxIm - e.Y / (double)canvas.Height * oldImRange;

            double newReRange = oldReRange / zoomFactor;
            double newImRange = oldImRange / zoomFactor;

            const double MIN_ALLOWED_RANGE = 1e-9;
            if (newReRange < MIN_ALLOWED_RANGE || newImRange < MIN_ALLOWED_RANGE) return;

            minRe = mouseReal - (e.X / (double)canvas.Width) * newReRange;
            maxRe = minRe + newReRange;
            minIm = mouseImaginary - (1.0 - e.Y / (double)canvas.Height) * newImRange;
            maxIm = minIm + newImRange;

            canvas.Invalidate();
            if (isPSlice) ScheduleRenderSliceP(); else ScheduleRenderSliceQ();
        }

        private void SliceCanvas_MouseDown(object sender, MouseEventArgs e, bool isPSlice)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isPSlice) { _panningSliceP = true; _panStartSliceP = e.Location; }
                else { _panningSliceQ = true; _panStartSliceQ = e.Location; }
                (sender as PictureBox).Cursor = Cursors.Hand;
            }
        }
        private void SliceCanvas_MouseMove(object sender, MouseEventArgs e, bool isPSlice)
        {
            PictureBox canvas = sender as PictureBox;
            bool isPanning = isPSlice ? _panningSliceP : _panningSliceQ;
            if (!isPanning || canvas.Width <= 0 || canvas.Height <= 0) return;

            ref Point panStart = ref (isPSlice ? ref _panStartSliceP : ref _panStartSliceQ);
            ref double minRe = ref (isPSlice ? ref _slicePMinRe : ref _sliceQMinRe);
            ref double maxRe = ref (isPSlice ? ref _slicePMaxRe : ref _sliceQMaxRe);
            ref double minIm = ref (isPSlice ? ref _slicePMinIm : ref _sliceQMinIm);
            ref double maxIm = ref (isPSlice ? ref _slicePMaxIm : ref _sliceQMaxIm);

            double rRange = maxRe - minRe; double iRange = maxIm - minIm;
            if (rRange <= 0 || iRange <= 0) return;

            double dx = (e.X - panStart.X) * (rRange / canvas.Width);
            double dy = (e.Y - panStart.Y) * (iRange / canvas.Height);

            minRe -= dx; maxRe -= dx;
            minIm += dy; maxIm += dy; // Y-ось инвертирована
            panStart = e.Location;

            canvas.Invalidate();
            if (isPSlice) ScheduleRenderSliceP(); else ScheduleRenderSliceQ();
        }
        private void SliceCanvas_MouseUp(object sender, MouseEventArgs e, bool isPSlice)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isPSlice) _panningSliceP = false; else _panningSliceQ = false;
                (sender as PictureBox).Cursor = Cursors.Default;
            }
        }

        #endregion

        #region Form Actions
        private void btnApply_Click(object sender, EventArgs e)
        {
            // Собираем C1 из nudPReal (для P.Re) и nudQImaginary (для Q.Im)
            // P.Im и Q.Re - это выбранные значения на срезах.
            ComplexDecimal c1 = new ComplexDecimal(nudPReal.Value, nudQImaginary.Value);
            // C2 остается тем, что пришло из основной формы (_fixedC2)
            ParametersSelected?.Invoke(c1, _fixedC2);
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Form Closing
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _ctsSliceP?.Cancel(); _ctsSliceP?.Dispose();
            _ctsSliceQ?.Cancel(); _ctsSliceQ?.Dispose();
            _renderDebounceTimerSliceP?.Stop(); _renderDebounceTimerSliceP?.Dispose();
            _renderDebounceTimerSliceQ?.Stop(); _renderDebounceTimerSliceQ?.Dispose();
            _slicePBitmap?.Dispose();
            _sliceQBitmap?.Dispose();
            base.OnFormClosed(e);
        }
        #endregion
    }
}