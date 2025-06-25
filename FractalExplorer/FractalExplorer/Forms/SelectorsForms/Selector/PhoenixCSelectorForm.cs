using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FractalExplorer.Engines;
using FractalExplorer.Resources;

namespace FractalExplorer.SelectorsForms
{
    public partial class PhoenixCSelectorForm : Form
    {
        #region Fields
        private readonly FractalExplorer.Forms.FractalPhoenixForm _ownerForm;

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

        private ComplexDecimal _fixedC2;

        private const int SLICE_ITERATIONS = 275;
        private const int RENDER_DEBOUNCE_MILLISECONDS = 300;
        private readonly PhoenixEngine _sliceRenderEngine;
        #endregion

        #region Events
        public event Action<ComplexDecimal, ComplexDecimal> ParametersSelected;
        #endregion

        #region Constructor
        public PhoenixCSelectorForm(FractalExplorer.Forms.FractalPhoenixForm owner, ComplexDecimal initialC1, ComplexDecimal initialC2)
        {
            InitializeComponent();
            _ownerForm = owner;

            nudPReal.Value = initialC1.Real;
            nudPImaginary.Value = 0;
            nudQReal.Value = 0;
            nudQImaginary.Value = initialC1.Imaginary;

            _fixedC2 = initialC2;

            _sliceRenderEngine = new PhoenixEngine
            {
                MaxIterations = SLICE_ITERATIONS,
                ThresholdSquared = 4.0m,
                C2 = _fixedC2,
                Palette = GetSliceRenderPalette(), // Устанавливаем палитру здесь
                MaxColorIterations = SLICE_ITERATIONS // Для серой палитры это будет использоваться как maxClrIter
            };

            SetupSliceCanvasEvents(sliceCanvasP, true);
            SetupSliceCanvasEvents(sliceCanvasQ, false);

            _renderDebounceTimerSliceP = new System.Windows.Forms.Timer { Interval = RENDER_DEBOUNCE_MILLISECONDS };
            _renderDebounceTimerSliceP.Tick += RenderDebounceTimerSliceP_Tick;
            _renderDebounceTimerSliceQ = new System.Windows.Forms.Timer { Interval = RENDER_DEBOUNCE_MILLISECONDS };
            _renderDebounceTimerSliceQ.Tick += RenderDebounceTimerSliceQ_Tick;

            this.Load += SelectorForm_Load;

            nudPReal.ValueChanged += NudValues_Changed;
            nudPImaginary.ValueChanged += NudValues_Changed;
            nudQReal.ValueChanged += NudValues_Changed;
            nudQImaginary.ValueChanged += NudValues_Changed;

            UpdateFixedValueLabels();
        }

        private void SelectorForm_Load(object sender, EventArgs e)
        {
            _renderedSlicePMinRe = _slicePMinRe; _renderedSlicePMaxRe = _slicePMaxRe;
            _renderedSlicePMinIm = _slicePMinIm; _renderedSlicePMaxIm = _slicePMaxIm;
            _renderedSliceQMinRe = _sliceQMinRe; _renderedSliceQMaxRe = _sliceQMaxRe;
            _renderedSliceQMinIm = _sliceQMinIm; _renderedSliceQMaxIm = _sliceQMaxIm;

            _renderDebounceTimerSliceP.Start();
            _renderDebounceTimerSliceQ.Start();
        }

        private void SetupSliceCanvasEvents(PictureBox canvas, bool isPSliceTarget)
        {
            canvas.Paint += (s, e) => SliceCanvas_Paint(s, e, isPSliceTarget);
            canvas.MouseClick += (s, e) => SliceCanvas_MouseClick(s, e, isPSliceTarget);
            canvas.MouseWheel += (s, e) => SliceCanvas_MouseWheel(s, e, isPSliceTarget);
            canvas.MouseDown += (s, e) => SliceCanvas_MouseDown(s, e, isPSliceTarget);
            canvas.MouseMove += (s, e) => SliceCanvas_MouseMove(s, e, isPSliceTarget);
            canvas.MouseUp += (s, e) => SliceCanvas_MouseUp(s, e, isPSliceTarget);
            canvas.Resize += (s, e) => {
                if (canvas.Width > 0 && canvas.Height > 0)
                {
                    var timer = isPSliceTarget ? _renderDebounceTimerSliceP : _renderDebounceTimerSliceQ;
                    timer.Stop();
                    timer.Start();
                }
            };
            var pb = isPSliceTarget ? progressBarSliceP : progressBarSliceQ;
            pb.Visible = false;
        }
        #endregion

        #region UI Update and Value Handling
        private void NudValues_Changed(object sender, EventArgs e)
        {
            UpdateFixedValueLabels();
            sliceCanvasP.Invalidate();
            sliceCanvasQ.Invalidate();

            if (sender == nudPReal)
            {
                _renderDebounceTimerSliceQ.Stop(); _renderDebounceTimerSliceQ.Start();
            }
            else if (sender == nudQImaginary)
            {
                _renderDebounceTimerSliceP.Stop(); _renderDebounceTimerSliceP.Start();
            }
            else if (sender == nudPImaginary)
            {
                _renderDebounceTimerSliceP.Stop(); _renderDebounceTimerSliceP.Start();
            }
            else if (sender == nudQReal)
            {
                // Если nudQReal используется для z0.Im среза Q (в RenderSliceQAsync сейчас используется qIm_val - Y-ось канваса)
                // Если бы он влиял, то: _renderDebounceTimerSliceQ.Stop(); _renderDebounceTimerSliceQ.Start();
            }
        }

        private void UpdateFixedValueLabels()
        {
            lblFixedQForPSlice.Text = $"(Q фикс. = {nudQImaginary.Value:F4})";
            lblFixedPForQSlice.Text = $"(P фикс. = {nudPReal.Value:F4})";
        }

        public void SetSelectedParameters(ComplexDecimal c1FromOwner)
        {
            bool triggerRenderP = false;
            bool triggerRenderQ = false;

            if (nudPReal.Value != c1FromOwner.Real)
            {
                nudPReal.Value = c1FromOwner.Real;
                triggerRenderQ = true;
            }
            if (nudQImaginary.Value != c1FromOwner.Imaginary)
            {
                nudQImaginary.Value = c1FromOwner.Imaginary;
                triggerRenderP = true;
            }

            UpdateFixedValueLabels();
            sliceCanvasP.Invalidate();
            sliceCanvasQ.Invalidate();

            if (triggerRenderP) { _renderDebounceTimerSliceP.Stop(); _renderDebounceTimerSliceP.Start(); }
            if (triggerRenderQ) { _renderDebounceTimerSliceQ.Stop(); _renderDebounceTimerSliceQ.Start(); }
        }
        #endregion

        #region Rendering Slices
        private Func<int, int, int, Color> GetSliceRenderPalette()
        {
            return (iter, maxIter, maxClrIter) =>
            {
                if (iter == maxIter) return Color.Black;
                if (maxClrIter <= 0) return Color.Gray;

                double tLog = Math.Log(Math.Min(iter, maxClrIter) + 1) / Math.Log(maxClrIter + 1);
                int cValRaw = (int)(255.0 * (1 - tLog));
                int cVal = ClampColorComponent(cValRaw);
                return Color.FromArgb(cVal, cVal, cVal);
            };
        }
        private static int ClampColorComponent(int component)
        {
            if (component < 0) return 0;
            if (component > 255) return 255;
            return component;
        }

        private async Task RenderSlicePAsync()
        {
            if (_isRenderingSliceP || sliceCanvasP.Width <= 0 || sliceCanvasP.Height <= 0) return;
            _isRenderingSliceP = true;
            _ctsSliceP?.Cancel();
            _ctsSliceP = new CancellationTokenSource();
            var token = _ctsSliceP.Token;

            var pb = progressBarSliceP;
            if (pb.IsHandleCreated && !pb.IsDisposed) pb.Invoke((Action)(() => { pb.Value = 0; pb.Visible = true; }));

            int w = sliceCanvasP.Width;
            int h = sliceCanvasP.Height;
            double minR_axis = _slicePMinRe;
            double maxR_axis = _slicePMaxRe;
            double minI_axis = _slicePMinIm;
            double maxI_axis = _slicePMaxIm;

            decimal fixedQ_scalar = nudQImaginary.Value;

            // Палитра и MaxColorIterations уже установлены в _sliceRenderEngine в конструкторе

            Bitmap newBitmap = null;
            try
            {
                newBitmap = await Task.Run(() =>
                {
                    Bitmap bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, bmp.PixelFormat);
                    byte[] buffer = new byte[Math.Abs(bmpData.Stride) * h];
                    long renderedLines = 0;

                    Parallel.For(0, h, new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = Environment.ProcessorCount }, y_pixel =>
                    {
                        if (token.IsCancellationRequested) return;
                        for (int x_pixel = 0; x_pixel < w; x_pixel++)
                        {
                            decimal p_scalar_for_engine = (decimal)(minR_axis + x_pixel * (maxR_axis - minR_axis) / w);
                            decimal z0_im_for_slice_visual = (decimal)(maxI_axis - y_pixel * (maxI_axis - minI_axis) / h);

                            ComplexDecimal c1_engine_param = new ComplexDecimal(p_scalar_for_engine, fixedQ_scalar);
                            ComplexDecimal z0_for_slice = new ComplexDecimal(0, z0_im_for_slice_visual);

                            int iter = _sliceRenderEngine.CalculateIterations(z0_for_slice, ComplexDecimal.Zero, c1_engine_param, _fixedC2);
                            Color c = _sliceRenderEngine.Palette(iter, SLICE_ITERATIONS, SLICE_ITERATIONS); // Используем палитру из движка
                            int idx = y_pixel * bmpData.Stride + x_pixel * 3;
                            buffer[idx] = c.B; buffer[idx + 1] = c.G; buffer[idx + 2] = c.R;
                        }
                        long currentProgress = Interlocked.Increment(ref renderedLines);
                        if (pb.IsHandleCreated && !pb.IsDisposed)
                            pb.Invoke((Action)(() => pb.Value = (int)(100.0 * currentProgress / h)));
                    });
                    token.ThrowIfCancellationRequested();
                    Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                    bmp.UnlockBits(bmpData);
                    return bmp;
                }, token);

                if (this.IsHandleCreated && !this.IsDisposed && sliceCanvasP.IsHandleCreated && !sliceCanvasP.IsDisposed)
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
            catch (Exception ex) { newBitmap?.Dispose(); if (this.IsHandleCreated && !this.IsDisposed) MessageBox.Show($"Ошибка рендера среза P: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally
            {
                _isRenderingSliceP = false;
                if (pb.IsHandleCreated && !pb.IsDisposed)
                    pb.Invoke((Action)(() => { pb.Visible = false; pb.Value = 0; }));
            }
        }

        private async Task RenderSliceQAsync()
        {
            if (_isRenderingSliceQ || sliceCanvasQ.Width <= 0 || sliceCanvasQ.Height <= 0) return;
            _isRenderingSliceQ = true;
            _ctsSliceQ?.Cancel();
            _ctsSliceQ = new CancellationTokenSource();
            var token = _ctsSliceQ.Token;

            var pb = progressBarSliceQ;
            if (pb.IsHandleCreated && !pb.IsDisposed) pb.Invoke((Action)(() => { pb.Value = 0; pb.Visible = true; }));

            int w = sliceCanvasQ.Width;
            int h = sliceCanvasQ.Height;
            double minR_axis = _sliceQMinRe;
            double maxR_axis = _sliceQMaxRe;
            double minI_axis = _sliceQMinIm;
            double maxI_axis = _sliceQMaxIm;

            decimal fixedP_scalar = nudPReal.Value;

            // Палитра и MaxColorIterations уже установлены в _sliceRenderEngine

            Bitmap newBitmap = null;
            try
            {
                newBitmap = await Task.Run(() =>
                {
                    Bitmap bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, bmp.PixelFormat);
                    byte[] buffer = new byte[Math.Abs(bmpData.Stride) * h];
                    long renderedLines = 0;

                    Parallel.For(0, h, new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = Environment.ProcessorCount }, y_pixel =>
                    {
                        if (token.IsCancellationRequested) return;
                        for (int x_pixel = 0; x_pixel < w; x_pixel++)
                        {
                            decimal q_scalar_for_engine = (decimal)(minR_axis + x_pixel * (maxR_axis - minR_axis) / w);
                            decimal z0_im_for_slice_visual = (decimal)(maxI_axis - y_pixel * (maxI_axis - minI_axis) / h);

                            ComplexDecimal c1_engine_param = new ComplexDecimal(fixedP_scalar, q_scalar_for_engine);
                            ComplexDecimal z0_for_slice = new ComplexDecimal(0, z0_im_for_slice_visual);

                            int iter = _sliceRenderEngine.CalculateIterations(z0_for_slice, ComplexDecimal.Zero, c1_engine_param, _fixedC2);
                            Color c = _sliceRenderEngine.Palette(iter, SLICE_ITERATIONS, SLICE_ITERATIONS); // Используем палитру из движка
                            int idx = y_pixel * bmpData.Stride + x_pixel * 3;
                            buffer[idx] = c.B; buffer[idx + 1] = c.G; buffer[idx + 2] = c.R;
                        }
                        long currentProgress = Interlocked.Increment(ref renderedLines);
                        if (pb.IsHandleCreated && !pb.IsDisposed)
                            pb.Invoke((Action)(() => pb.Value = (int)(100.0 * currentProgress / h)));
                    });
                    token.ThrowIfCancellationRequested();
                    Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
                    bmp.UnlockBits(bmpData);
                    return bmp;
                }, token);

                if (this.IsHandleCreated && !this.IsDisposed && sliceCanvasQ.IsHandleCreated && !sliceCanvasQ.IsDisposed)
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
            catch (Exception ex) { newBitmap?.Dispose(); if (this.IsHandleCreated && !this.IsDisposed) MessageBox.Show($"Ошибка рендера среза Q: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally
            {
                _isRenderingSliceQ = false;
                if (pb.IsHandleCreated && !pb.IsDisposed)
                    pb.Invoke((Action)(() => { pb.Visible = false; pb.Value = 0; }));
            }
        }
        #endregion

        #region Canvas Interaction
        private void SliceCanvas_Paint(object sender, PaintEventArgs e, bool isPSliceTarget)
        {
            PictureBox canvas = sender as PictureBox;
            Bitmap bmpToDraw = isPSliceTarget ? _slicePBitmap : _sliceQBitmap;

            double renderedMinRe = isPSliceTarget ? _renderedSlicePMinRe : _renderedSliceQMinRe;
            double renderedMaxRe = isPSliceTarget ? _renderedSlicePMaxRe : _renderedSliceQMaxRe;
            double renderedMinIm = isPSliceTarget ? _renderedSlicePMinIm : _renderedSliceQMinIm;
            double renderedMaxIm = isPSliceTarget ? _renderedSlicePMaxIm : _renderedSliceQMaxIm;

            double currentMinRe = isPSliceTarget ? _slicePMinRe : _sliceQMinRe;
            double currentMaxRe = isPSliceTarget ? _slicePMaxRe : _sliceQMaxRe;
            double currentMinIm = isPSliceTarget ? _slicePMinIm : _sliceQMinIm;
            double currentMaxIm = isPSliceTarget ? _slicePMaxIm : _sliceQMaxIm;

            e.Graphics.Clear(Color.DimGray);
            if (bmpToDraw == null || canvas.Width <= 0 || canvas.Height <= 0)
            {
                DrawMarker(e.Graphics, canvas, isPSliceTarget);
                return;
            }

            double renderedComplexWidth = renderedMaxRe - renderedMinRe;
            double renderedComplexHeight = renderedMaxIm - renderedMinIm;
            double currentComplexWidth = currentMaxRe - currentMinRe;
            double currentComplexHeight = currentMaxIm - currentMinIm;

            if (renderedComplexWidth <= 0 || renderedComplexHeight <= 0 || currentComplexWidth <= 0 || currentComplexHeight <= 0)
            {
                if (bmpToDraw != null) e.Graphics.DrawImageUnscaled(bmpToDraw, Point.Empty);
                DrawMarker(e.Graphics, canvas, isPSliceTarget);
                return;
            }

            float offsetX = (float)((renderedMinRe - currentMinRe) / currentComplexWidth * canvas.Width);
            float offsetY = (float)((currentMaxIm - renderedMaxIm) / currentComplexHeight * canvas.Height);
            float destWidthPixels = (float)(renderedComplexWidth / currentComplexWidth * canvas.Width);
            float destHeightPixels = (float)(renderedComplexHeight / currentComplexHeight * canvas.Height);

            if (destWidthPixels > 0 && destHeightPixels > 0)
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.DrawImage(bmpToDraw, new RectangleF(offsetX, offsetY, destWidthPixels, destHeightPixels));
            }
            else
            {
                if (bmpToDraw != null) e.Graphics.DrawImageUnscaled(bmpToDraw, Point.Empty);
            }
            DrawMarker(e.Graphics, canvas, isPSliceTarget);
        }

        private void DrawMarker(Graphics g, PictureBox canvas, bool isPSliceTarget)
        {
            decimal valX_axis;
            decimal valY_axis_z0_Im;

            double minX_map, maxX_map;
            double minY_map, maxY_map;


            if (isPSliceTarget)
            {
                valX_axis = nudPReal.Value;
                valY_axis_z0_Im = nudPImaginary.Value;
                minX_map = _slicePMinRe; maxX_map = _slicePMaxRe;
                minY_map = _slicePMinIm; maxY_map = _slicePMaxIm;
            }
            else
            {
                valX_axis = nudQImaginary.Value;
                valY_axis_z0_Im = nudQReal.Value;    // Здесь nudQReal используется для z0.Im среза Q
                minX_map = _sliceQMinRe; maxX_map = _sliceQMaxRe;
                minY_map = _sliceQMinIm; maxY_map = _sliceQMaxIm;
            }

            double xRange_map = maxX_map - minX_map;
            double yRange_map = maxY_map - minY_map;

            if (xRange_map > 0 && yRange_map > 0 && canvas.Width > 0 && canvas.Height > 0)
            {
                int markerX_pixel = (int)(((double)valX_axis - minX_map) / xRange_map * canvas.Width);
                int markerY_pixel = (int)((maxY_map - (double)valY_axis_z0_Im) / yRange_map * canvas.Height);

                int size = 7;
                using (Pen p = new Pen(Color.LimeGreen, 2))
                {
                    g.DrawLine(p, markerX_pixel - size, markerY_pixel, markerX_pixel + size, markerY_pixel);
                    g.DrawLine(p, markerX_pixel, markerY_pixel - size, markerX_pixel, markerY_pixel + size);
                }
            }
        }

        private void SliceCanvas_MouseClick(object sender, MouseEventArgs e, bool isPSliceTarget)
        {
            PictureBox canvas = sender as PictureBox;
            if (e.Button != MouseButtons.Left || canvas.Width <= 0 || canvas.Height <= 0) return;

            double currentMinRe_map = isPSliceTarget ? _slicePMinRe : _sliceQMinRe;
            double currentMaxRe_map = isPSliceTarget ? _slicePMaxRe : _sliceQMaxRe;
            double currentMinIm_map = isPSliceTarget ? _slicePMinIm : _sliceQMinIm;
            double currentMaxIm_map = isPSliceTarget ? _slicePMaxIm : _sliceQMaxIm;

            double xRange_map = currentMaxRe_map - currentMinRe_map;
            double yRange_map = currentMaxIm_map - currentMinIm_map;

            if (xRange_map <= 0 || yRange_map <= 0) return;

            decimal selectedValueOnXAxis = ClampDecimal((decimal)(currentMinRe_map + e.X / (double)canvas.Width * xRange_map),
                                                       isPSliceTarget ? nudPReal.Minimum : nudQImaginary.Minimum,
                                                       isPSliceTarget ? nudPReal.Maximum : nudQImaginary.Maximum);

            decimal selectedValueOnYAxis_for_z0Im = ClampDecimal((decimal)(currentMaxIm_map - e.Y / (double)canvas.Height * yRange_map),
                                                                isPSliceTarget ? nudPImaginary.Minimum : nudQReal.Minimum,
                                                                isPSliceTarget ? nudPImaginary.Maximum : nudQReal.Maximum);

            if (isPSliceTarget)
            {
                nudPReal.Value = selectedValueOnXAxis;
                nudPImaginary.Value = selectedValueOnYAxis_for_z0Im;
            }
            else
            {
                nudQImaginary.Value = selectedValueOnXAxis;
                nudQReal.Value = selectedValueOnYAxis_for_z0Im;
            }
        }

        private void SliceCanvas_MouseWheel(object sender, MouseEventArgs e, bool isPSliceTarget)
        {
            PictureBox canvas = sender as PictureBox;
            if (canvas.Width <= 0 || canvas.Height <= 0) return;

            double zoomFactor = e.Delta > 0 ? 1.25 : 1.0 / 1.25;
            ref double minRe_map = ref (isPSliceTarget ? ref _slicePMinRe : ref _sliceQMinRe);
            ref double maxRe_map = ref (isPSliceTarget ? ref _slicePMaxRe : ref _sliceQMaxRe);
            ref double minIm_map = ref (isPSliceTarget ? ref _slicePMinIm : ref _sliceQMinIm);
            ref double maxIm_map = ref (isPSliceTarget ? ref _slicePMaxIm : ref _sliceQMaxIm);

            double oldReRange_map = maxRe_map - minRe_map;
            double oldImRange_map = maxIm_map - minIm_map;
            if (oldReRange_map <= 0 || oldImRange_map <= 0) return;

            double mouseX_canvas = e.X;
            double mouseY_canvas = e.Y;

            double mouseRe_complex_map = minRe_map + (mouseX_canvas / canvas.Width) * oldReRange_map;
            double mouseIm_complex_map = maxIm_map - (mouseY_canvas / canvas.Height) * oldImRange_map;

            double newReRange_map = oldReRange_map / zoomFactor;
            double newImRange_map = oldImRange_map / zoomFactor;

            const double MIN_ALLOWED_RANGE = 1e-12;
            const double MAX_ALLOWED_RANGE = 1e4;
            if (newReRange_map < MIN_ALLOWED_RANGE || newImRange_map < MIN_ALLOWED_RANGE ||
                newReRange_map > MAX_ALLOWED_RANGE || newImRange_map > MAX_ALLOWED_RANGE)
            {
                return;
            }

            minRe_map = mouseRe_complex_map - (mouseX_canvas / canvas.Width) * newReRange_map;
            maxRe_map = minRe_map + newReRange_map;

            minIm_map = mouseIm_complex_map - (1.0 - (mouseY_canvas / canvas.Height)) * newImRange_map;
            maxIm_map = minIm_map + newImRange_map;

            canvas.Invalidate();

            var timer = isPSliceTarget ? _renderDebounceTimerSliceP : _renderDebounceTimerSliceQ;
            timer.Stop();
            timer.Start();
        }

        private void SliceCanvas_MouseDown(object sender, MouseEventArgs e, bool isPSliceTarget)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isPSliceTarget) { _panningSliceP = true; _panStartSliceP = e.Location; }
                else { _panningSliceQ = true; _panStartSliceQ = e.Location; }
                (sender as PictureBox).Cursor = Cursors.Hand;
            }
        }
        private void SliceCanvas_MouseMove(object sender, MouseEventArgs e, bool isPSliceTarget)
        {
            PictureBox canvas = sender as PictureBox;
            bool isPanning = isPSliceTarget ? _panningSliceP : _panningSliceQ;
            if (!isPanning || canvas.Width <= 0 || canvas.Height <= 0) return;

            ref Point panStart = ref (isPSliceTarget ? ref _panStartSliceP : ref _panStartSliceQ);
            ref double minRe_map = ref (isPSliceTarget ? ref _slicePMinRe : ref _sliceQMinRe);
            ref double maxRe_map = ref (isPSliceTarget ? ref _slicePMaxRe : ref _sliceQMaxRe);
            ref double minIm_map = ref (isPSliceTarget ? ref _slicePMinIm : ref _sliceQMinIm);
            ref double maxIm_map = ref (isPSliceTarget ? ref _slicePMaxIm : ref _sliceQMaxIm);

            double rRange_map = maxRe_map - minRe_map;
            double iRange_map = maxIm_map - minIm_map;
            if (rRange_map <= 0 || iRange_map <= 0) return;

            double deltaXPixels = e.X - panStart.X;
            double deltaYPixels = e.Y - panStart.Y;

            double deltaRe_map = deltaXPixels * (rRange_map / canvas.Width);
            double deltaIm_map = deltaYPixels * (iRange_map / canvas.Height);

            minRe_map -= deltaRe_map;
            maxRe_map -= deltaRe_map;
            minIm_map += deltaIm_map;
            maxIm_map += deltaIm_map;

            panStart = e.Location;
            canvas.Invalidate();
        }
        private void SliceCanvas_MouseUp(object sender, MouseEventArgs e, bool isPSliceTarget)
        {
            bool wasPanning;
            var timerToStart = isPSliceTarget ? _renderDebounceTimerSliceP : _renderDebounceTimerSliceQ;

            if (isPSliceTarget) { wasPanning = _panningSliceP; _panningSliceP = false; }
            else { wasPanning = _panningSliceQ; _panningSliceQ = false; }

            (sender as PictureBox).Cursor = Cursors.Default;

            if (wasPanning)
            {
                timerToStart.Stop();
                timerToStart.Start();
            }
        }

        #endregion

        #region Timer Tick Handlers
        private async void RenderDebounceTimerSliceP_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimerSliceP.Stop();
            if (this.IsHandleCreated && !this.IsDisposed && !this.Disposing)
            {
                await RenderSlicePAsync();
            }
        }

        private async void RenderDebounceTimerSliceQ_Tick(object sender, EventArgs e)
        {
            _renderDebounceTimerSliceQ.Stop();
            if (this.IsHandleCreated && !this.IsDisposed && !this.Disposing)
            {
                await RenderSliceQAsync();
            }
        }
        #endregion

        #region Form Actions
        private void btnApply_Click(object sender, EventArgs e)
        {
            ComplexDecimal c1Result = new ComplexDecimal(nudPReal.Value, nudQImaginary.Value);
            ParametersSelected?.Invoke(c1Result, _fixedC2);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Form Closing
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _ctsSliceP?.Cancel(); _ctsSliceP?.Dispose(); _ctsSliceP = null;
            _ctsSliceQ?.Cancel(); _ctsSliceQ?.Dispose(); _ctsSliceQ = null;

            _renderDebounceTimerSliceP?.Stop(); _renderDebounceTimerSliceP?.Dispose(); _renderDebounceTimerSliceP = null;
            _renderDebounceTimerSliceQ?.Stop(); _renderDebounceTimerSliceQ?.Dispose(); _renderDebounceTimerSliceQ = null;

            _slicePBitmap?.Dispose(); _slicePBitmap = null;
            _sliceQBitmap?.Dispose(); _sliceQBitmap = null;

            base.OnFormClosed(e);
        }
        #endregion
        private decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}