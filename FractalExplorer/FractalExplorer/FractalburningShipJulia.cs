// --- START OF FILE FractalburningShipJulia.cs ---

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FractalDraving
{
    public partial class FractalburningShipJulia : Form, IFractalForm
    {
        private System.Windows.Forms.Timer renderTimer;
        private ComplexDecimal c;
        private int maxIterations;
        private decimal thresholdSquared;
        private int threadCount;
        private int width, height;

        private Point panStart;
        private bool panning = false;

        private decimal zoom = 1.0m;
        private decimal centerX = 0.0m;
        private decimal centerY = 0.0m;
        private const decimal BASE_SCALE = 1.0m;

        private bool isHighResRendering = false;
        private volatile bool isRenderingPreview = false;
        private CancellationTokenSource previewRenderCts;

        private CheckBox[] paletteCheckBoxes;
        private CheckBox lastSelectedPaletteCheckBox = null;

        private const double MANDELBROT_MIN_RE_D = -2.0;
        private const double MANDELBROT_MAX_RE_D = 1.5;
        private const double MANDELBROT_MIN_IM_D = -1.5;
        private const double MANDELBROT_MAX_IM_D = 1.0;
        private const int MANDELBROT_PREVIEW_ITERATIONS = 75;

        private BurningShipCSelectorForm mandelbrotCSelectorWindow;

        private decimal renderedCenterX;
        private decimal renderedCenterY;
        private decimal renderedZoom;

        public double LoupeZoom => (double)nudBaseScale.Value;
        public event EventHandler LoupeZoomChanged;

        public FractalburningShipJulia()
        {
            InitializeComponent();
            this.Text = "Фрактал Горящий Корабль (Жюлиа)";
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            width = canvas1.Width;
            height = canvas1.Height;

            renderTimer = new System.Windows.Forms.Timer { Interval = 300 };
            renderTimer.Tick += RenderTimer_Tick;

            paletteCheckBoxes = new CheckBox[] {
                colorBox, oldRenderBW,
                Controls.Find("checkBox1", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox2", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox3", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox4", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox5", true).FirstOrDefault() as CheckBox,
                Controls.Find("checkBox6", true).FirstOrDefault() as CheckBox
            };

            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null))
            {
                cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            }

            nudRe1.ValueChanged += ParamControl_Changed;
            nudIm1.ValueChanged += ParamControl_Changed;
            nudIterations1.ValueChanged += ParamControl_Changed;
            nudThreshold1.ValueChanged += ParamControl_Changed;
            cbThreads1.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;
            nudBaseScale.ValueChanged += NudBaseScale_ValueChanged;

            canvas1.MouseWheel += Canvas_MouseWheel;
            canvas1.MouseDown += Canvas_MouseDown;
            canvas1.MouseMove += Canvas_MouseMove;
            canvas1.MouseUp += Canvas_MouseUp;
            canvas1.Paint += Canvas_Paint;

            if (mandelbrotCanvas1 != null)
            {
                mandelbrotCanvas1.Click += mandelbrotCanvas_Click;
                mandelbrotCanvas1.Paint += mandelbrotCanvas_Paint;
                await Task.Run(() => RenderAndDisplayMandelbrotSet());
            }

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++)
            {
                cbThreads1.Items.Add(i);
            }
            cbThreads1.Items.Add("Auto");
            cbThreads1.SelectedItem = "Auto";

            nudRe1.Minimum = -2m;
            nudRe1.Maximum = 2m;
            nudRe1.DecimalPlaces = 25;
            nudRe1.Increment = 0.000000000000001m;

            nudIm1.Minimum = -2m;
            nudIm1.Maximum = 2m;
            nudIm1.DecimalPlaces = 25;
            nudIm1.Increment = 0.000000000000001m;

            nudIterations1.Minimum = 50;
            nudIterations1.Maximum = 100000;
            nudIterations1.Value = 600;

            nudThreshold1.Minimum = 2m;
            nudThreshold1.Maximum = 10000m;
            nudThreshold1.DecimalPlaces = 1;
            nudThreshold1.Increment = 0.1m;
            nudThreshold1.Value = 2m;

            nudZoom.DecimalPlaces = 0;
            nudZoom.Increment = 1m;
            nudZoom.Minimum = 1m;
            nudZoom.Maximum = decimal.MaxValue / 1000;
            nudZoom.Value = this.zoom;

            nudBaseScale.Minimum = 1m;
            nudBaseScale.Maximum = 10m;
            nudBaseScale.DecimalPlaces = 1;
            nudBaseScale.Increment = 0.1m;
            nudBaseScale.Value = 4m; // Пример значения, можно настроить

            this.Resize += Form1_Resize;
            canvas1.Resize += Canvas_Resize;

            renderedCenterX = this.centerX;
            renderedCenterY = this.centerY;
            renderedZoom = this.zoom;

            HandleColorBoxEnableState();
            UpdateParameters();
            ScheduleRender();
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas1.Image == null || width <= 0 || height <= 0)
            {
                e.Graphics.Clear(Color.Black);
                return;
            }

            decimal currentScaleRendered = BASE_SCALE / this.renderedZoom;
            decimal currentScaleCurrent = BASE_SCALE / this.zoom;
            decimal aspectRatio = (width > 0 && height > 0) ? (decimal)height / (decimal)width : 1.0m;

            if (this.renderedZoom <= 0m || this.zoom <= 0m || currentScaleRendered <= 0m || currentScaleCurrent <= 0m)
            {
                e.Graphics.Clear(Color.Black);
                if (canvas1.Image != null) e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
                return;
            }

            decimal rendered_complex_half_width = currentScaleRendered / 2.0m;
            decimal rendered_complex_half_height = currentScaleRendered * aspectRatio / 2.0m;

            decimal current_complex_half_width = currentScaleCurrent / 2.0m;
            decimal current_complex_half_height = currentScaleCurrent * aspectRatio / 2.0m;

            decimal renderedImage_re_min = this.renderedCenterX - rendered_complex_half_width;
            decimal renderedImage_im_min = this.renderedCenterY - rendered_complex_half_height;

            decimal currentView_re_min = this.centerX - current_complex_half_width;
            decimal currentView_im_min = this.centerY - current_complex_half_height;

            float p1_X = (float)(((renderedImage_re_min - currentView_re_min) / (current_complex_half_width * 2.0m)) * (decimal)width);
            float p1_Y = (float)(((renderedImage_im_min - currentView_im_min) / (current_complex_half_height * 2.0m)) * (decimal)height);

            decimal zoomRatio = currentScaleRendered / currentScaleCurrent;
            float w_prime = (float)((decimal)width * zoomRatio);
            float h_prime = (float)((decimal)height * zoomRatio);

            PointF destPoint1 = new PointF(p1_X, p1_Y);
            PointF destPoint2 = new PointF(p1_X + w_prime, p1_Y);
            PointF destPoint3 = new PointF(p1_X, p1_Y + h_prime);

            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            if (w_prime > 0 && h_prime > 0 && canvas1.Image != null)
            {
                try
                {
                    e.Graphics.DrawImage(canvas1.Image, new PointF[] { destPoint1, destPoint2, destPoint3 });
                }
                catch (ArgumentException)
                {
                    if (canvas1.Image != null) e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
                }
            }
            else
            {
                if (canvas1.Image != null) e.Graphics.DrawImageUnscaled(canvas1.Image, Point.Empty);
            }
        }

        private void NudBaseScale_ValueChanged(object sender, EventArgs e)
        {
            LoupeZoomChanged?.Invoke(this, EventArgs.Empty);
            ScheduleRender();
        }

        private void RenderAndDisplayMandelbrotSet()
        {
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0) return;
            Bitmap mandelbrotImage = RenderMandelbrotSetInternal(mandelbrotCanvas1.Width, mandelbrotCanvas1.Height, MANDELBROT_PREVIEW_ITERATIONS);
            if (mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
            {
                mandelbrotCanvas1.Invoke((Action)(() =>
                {
                    mandelbrotCanvas1.Image?.Dispose();
                    mandelbrotCanvas1.Image = mandelbrotImage;
                    mandelbrotCanvas1.Invalidate();
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
            if (canvasWidth <= 0 || canvasHeight <= 0) return bmp;

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, canvasWidth, canvasHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            int bytes = Math.Abs(stride) * canvasHeight;
            byte[] buffer = new byte[bytes];

            double reRange = MANDELBROT_MAX_RE_D - MANDELBROT_MIN_RE_D;
            double imRange = MANDELBROT_MAX_IM_D - MANDELBROT_MIN_IM_D;
            decimal previewThresholdSquared = 4m;

            Parallel.For(0, canvasHeight, y_coord =>
            {
                int rowOffset = y_coord * stride;
                for (int x_coord = 0; x_coord < canvasWidth; x_coord++)
                {
                    double c_re_d = MANDELBROT_MIN_RE_D + (x_coord / (double)canvasWidth) * reRange;
                    double c_im_d = MANDELBROT_MAX_IM_D - (y_coord / (double)canvasHeight) * imRange;

                    ComplexDecimal c0 = new ComplexDecimal((decimal)c_re_d, (decimal)c_im_d);
                    ComplexDecimal z = ComplexDecimal.Zero;
                    int iter = 0;

                    while (iter < iterationsLimit && z.MagnitudeSquared < previewThresholdSquared)
                    {
                        z = new ComplexDecimal(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                        z = z * z + c0;
                        iter++;
                    }

                    byte r_val, g_val, b_val;
                    if (iter == iterationsLimit)
                    {
                        r_val = g_val = b_val = 0;
                    }
                    else
                    {
                        double t = (double)iter / iterationsLimit;
                        if (t < 0.5)
                        {
                            r_val = (byte)(t * 2 * 200); g_val = (byte)(t * 2 * 50); b_val = (byte)(t * 2 * 30);
                        }
                        else
                        {
                            t = (t - 0.5) * 2;
                            r_val = (byte)(200 + t * 55); g_val = (byte)(50 + t * 205); b_val = (byte)(30 + t * 225);
                        }
                    }
                    int index = rowOffset + x_coord * 3;
                    buffer[index] = b_val; buffer[index + 1] = g_val; buffer[index + 2] = r_val;
                }
            });

            Marshal.Copy(buffer, 0, scan0, bytes);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private void MandelbrotCSelectorWindow_CoordinatesSelected(double re, double im)
        {
            decimal re_d = (decimal)Math.Max((double)nudRe1.Minimum, Math.Min((double)nudRe1.Maximum, re));
            decimal im_d = (decimal)Math.Max((double)nudIm1.Minimum, Math.Min((double)nudIm1.Maximum, im));
            bool changed = false;
            if (nudRe1.Value != re_d) { nudRe1.Value = re_d; changed = true; }
            if (nudIm1.Value != im_d) { nudIm1.Value = im_d; changed = true; }
            if (changed && mandelbrotCanvas1 != null && mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
            {
                mandelbrotCanvas1.Invoke((Action)(() => mandelbrotCanvas1.Invalidate()));
            }
        }

        private void mandelbrotCanvas_Paint(object sender, PaintEventArgs e)
        {
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0) return;
            double reRange = MANDELBROT_MAX_RE_D - MANDELBROT_MIN_RE_D;
            double imRange = MANDELBROT_MAX_IM_D - MANDELBROT_MIN_IM_D;
            double currentCRe = (double)nudRe1.Value;
            double currentCIm = (double)nudIm1.Value;
            if (currentCRe >= MANDELBROT_MIN_RE_D && currentCRe <= MANDELBROT_MAX_RE_D &&
                currentCIm >= MANDELBROT_MIN_IM_D && currentCIm <= MANDELBROT_MAX_IM_D)
            {
                int markerX = (int)((currentCRe - MANDELBROT_MIN_RE_D) / reRange * mandelbrotCanvas1.Width);
                int markerY = (int)((MANDELBROT_MAX_IM_D - currentCIm) / imRange * mandelbrotCanvas1.Height);
                using (Pen markerPen = new Pen(Color.FromArgb(200, Color.Green), 1.5f))
                {
                    e.Graphics.DrawLine(markerPen, 0, markerY, mandelbrotCanvas1.Width, markerY);
                    e.Graphics.DrawLine(markerPen, markerX, 0, markerX, mandelbrotCanvas1.Height);
                }
            }
        }

        private void mandelbrotCanvas_Click(object sender, EventArgs e)
        {
            if (mandelbrotCanvas1 == null || mandelbrotCanvas1.Width <= 0 || mandelbrotCanvas1.Height <= 0) return;
            MouseEventArgs mouseArgs = e as MouseEventArgs;
            if (mouseArgs == null)
            {
                Point clientPoint = mandelbrotCanvas1.PointToClient(Control.MousePosition);
                if ((Control.MouseButtons & MouseButtons.Left) != MouseButtons.Left) return;
                mouseArgs = new MouseEventArgs(MouseButtons.Left, 1, clientPoint.X, clientPoint.Y, 0);
            }
            if (mouseArgs.Button != MouseButtons.Left) return;
            double initialRe = (double)nudRe1.Value;
            double initialIm = (double)nudIm1.Value;
            if (mandelbrotCSelectorWindow == null || mandelbrotCSelectorWindow.IsDisposed)
            {
                mandelbrotCSelectorWindow = new BurningShipCSelectorForm(this, initialRe, initialIm);
                mandelbrotCSelectorWindow.CoordinatesSelected += MandelbrotCSelectorWindow_CoordinatesSelected;
                mandelbrotCSelectorWindow.FormClosed += (s, args) => { mandelbrotCSelectorWindow = null; };
                mandelbrotCSelectorWindow.Show(this);
            }
            else
            {
                mandelbrotCSelectorWindow.Activate();
                mandelbrotCSelectorWindow.SetSelectedCoordinates(initialRe, initialIm, true);
            }
            if (mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed)
            {
                mandelbrotCanvas1.Invalidate();
            }
        }

        private void PaletteCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox currentCb = sender as CheckBox; if (currentCb == null) return;
            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null)) cb.CheckedChanged -= PaletteCheckBox_CheckedChanged;
            if (currentCb.Checked)
            {
                lastSelectedPaletteCheckBox = currentCb;
                foreach (var cb in paletteCheckBoxes.Where(cb => cb != null && cb != currentCb)) cb.Checked = false;
            }
            else
            {
                lastSelectedPaletteCheckBox = paletteCheckBoxes.FirstOrDefault(cb => cb != null && cb.Checked);
            }
            foreach (var cb in paletteCheckBoxes.Where(cb => cb != null)) cb.CheckedChanged += PaletteCheckBox_CheckedChanged;
            HandleColorBoxEnableState();
            ScheduleRender();
        }

        private void HandleColorBoxEnableState()
        {
            if (paletteCheckBoxes == null || colorBox == null || oldRenderBW == null) return;
            bool isAnyNewPaletteCbChecked = paletteCheckBoxes.Skip(2).Any(cb => cb != null && cb.Checked);
            if (isAnyNewPaletteCbChecked) colorBox.Enabled = true;
            else if (colorBox.Checked && !oldRenderBW.Checked) colorBox.Enabled = true;
            else colorBox.Enabled = !oldRenderBW.Checked;
        }

        private void Form1_Resize(object sender, EventArgs e) => ResizeCanvas();
        private void Canvas_Resize(object sender, EventArgs e) => ResizeCanvas();

        private void ResizeCanvas()
        {
            if (isHighResRendering) return;
            if (canvas1.Width <= 0 || canvas1.Height <= 0) return;
            width = canvas1.Width; height = canvas1.Height;
            ScheduleRender();
        }

        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            if (sender == nudZoom)
            {
                this.zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, nudZoom.Value));
                if (nudZoom.Value != this.zoom)
                {
                    nudZoom.Value = this.zoom;
                }
            }
            if (sender == nudThreshold1 || sender == nudIterations1 || sender == nudRe1 || sender == nudIm1 || sender == cbThreads1)
            {
                UpdateParameters();
            }

            if (mandelbrotCanvas1 != null && (sender == nudRe1 || sender == nudIm1))
            {
                if (mandelbrotCanvas1.IsHandleCreated && !mandelbrotCanvas1.IsDisposed) mandelbrotCanvas1.Invalidate();
            }
            ScheduleRender();
        }

        private void UpdateParameters()
        {
            c = new ComplexDecimal(nudRe1.Value, nudIm1.Value);
            maxIterations = (int)nudIterations1.Value;
            decimal thresholdValue = nudThreshold1.Value;
            thresholdSquared = thresholdValue * thresholdValue;
            threadCount = cbThreads1.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads1.SelectedItem);
        }

        private void ScheduleRender()
        {
            if (isHighResRendering) return;
            previewRenderCts?.Cancel();
            renderTimer.Stop(); renderTimer.Start();
        }

        private async void RenderTimer_Tick(object sender, EventArgs e)
        {
            renderTimer.Stop();
            if (isHighResRendering) return;
            if (isRenderingPreview) { renderTimer.Start(); return; }
            isRenderingPreview = true;
            previewRenderCts?.Dispose();
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;

            decimal currentRenderCenterX = this.centerX;
            decimal currentRenderCenterY = this.centerY;
            decimal currentRenderZoom = this.zoom;

            try
            {
                await Task.Run(() => RenderFractal(token, currentRenderCenterX, currentRenderCenterY, currentRenderZoom), token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            finally { isRenderingPreview = false; }
        }

        private void RenderFractal(CancellationToken token, decimal renderCenterX, decimal renderCenterY, decimal renderZoom)
        {
            if (token.IsCancellationRequested) return;
            if (isHighResRendering || width <= 0 || height <= 0) return;

            Bitmap bmp = null;
            BitmapData bmpData = null;

            try
            {
                bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                token.ThrowIfCancellationRequested();
                Rectangle rect = new Rectangle(0, 0, width, height);
                bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
                token.ThrowIfCancellationRequested();
                int stride = bmpData.Stride;
                IntPtr scan0 = bmpData.Scan0;
                byte[] buffer = new byte[Math.Abs(stride) * height];
                ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = threadCount, CancellationToken = token };
                int done = 0;
                const int currentMaxColorIter = 1000;

                decimal currentBaseScale = BASE_SCALE;
                decimal scaleX = currentBaseScale / renderZoom;
                decimal scaleY = scaleX;
                if (width > 0 && height > 0) // Защита от деления на ноль
                {
                    scaleY = scaleX * ((decimal)height / (decimal)width);
                }


                Parallel.For(0, height, po, y_coord =>
                {
                    if (token.IsCancellationRequested) return;
                    int rowOffset = y_coord * stride;
                    for (int x_coord = 0; x_coord < width; x_coord++)
                    {
                        decimal re = renderCenterX + ((decimal)x_coord - (decimal)width / 2.0m) * scaleX / (decimal)width;
                        decimal im = renderCenterY - ((decimal)y_coord - (decimal)height / 2.0m) * scaleY / (decimal)height;

                        ComplexDecimal z_val = new ComplexDecimal(re, im);
                        int iter_val = 0;

                        while (iter_val < maxIterations && z_val.MagnitudeSquared <= thresholdSquared)
                        {
                            z_val = new ComplexDecimal(Math.Abs(z_val.Real), Math.Abs(z_val.Imaginary));
                            z_val = z_val * z_val + c;
                            iter_val++;
                        }

                        Color pixelColor = GetPixelColor(iter_val, maxIterations, currentMaxColorIter);
                        int index = rowOffset + x_coord * 3;
                        buffer[index] = pixelColor.B;
                        buffer[index + 1] = pixelColor.G;
                        buffer[index + 2] = pixelColor.R;
                    }

                    int progress = Interlocked.Increment(ref done);
                    if (!token.IsCancellationRequested && progressBar1.IsHandleCreated && !progressBar1.IsDisposed && height > 0)
                    {
                        try { progressBar1.BeginInvoke((Action)(() => { if (progressBar1.IsHandleCreated && !progressBar1.IsDisposed && progressBar1.Value <= progressBar1.Maximum) progressBar1.Value = Math.Min(progressBar1.Maximum, (int)(100.0 * progress / height)); })); } catch (InvalidOperationException) { }
                    }
                });

                token.ThrowIfCancellationRequested();
                Marshal.Copy(buffer, 0, scan0, buffer.Length);
                bmp.UnlockBits(bmpData);
                bmpData = null;
                token.ThrowIfCancellationRequested();

                if (canvas1.IsHandleCreated && !canvas1.IsDisposed)
                {
                    Bitmap oldImage = null;
                    canvas1.Invoke((Action)(() =>
                    {
                        if (token.IsCancellationRequested) { bmp?.Dispose(); return; }
                        oldImage = canvas1.Image as Bitmap;
                        canvas1.Image = bmp;
                        this.renderedCenterX = renderCenterX;
                        this.renderedCenterY = renderCenterY;
                        this.renderedZoom = renderZoom;
                        bmp = null;
                        canvas1.Invalidate();
                    }));
                    oldImage?.Dispose();
                }
                else
                {
                    bmp?.Dispose();
                }
            }
            finally
            {
                if (bmpData != null && bmp != null) { try { bmp.UnlockBits(bmpData); } catch { } }
                if (bmp != null) bmp.Dispose();
            }
        }

        private Bitmap RenderFractalToBitmap(int renderWidth, int renderHeight,
                                             decimal currentCenterX_param, decimal currentCenterY_param, decimal currentZoom_param,
                                             decimal currentBaseScale_param,
                                             ComplexDecimal currentC_param,
                                             int currentMaxIterations_param, decimal currentThresholdSquared_param_local,
                                             int numThreads,
                                             Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0) return new Bitmap(1, 1);
            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            byte[] buffer = new byte[Math.Abs(stride) * renderHeight];
            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;
            const int currentMaxColorIter_param = 1000;

            decimal scaleX = currentBaseScale_param / currentZoom_param;
            decimal scaleY = scaleX;
            if (renderWidth > 0 && renderHeight > 0)
            {
                scaleY = scaleX * ((decimal)renderHeight / (decimal)renderWidth);
            }

            Parallel.For(0, renderHeight, po, y_coord =>
            {
                int rowOffset = y_coord * stride;
                for (int x_coord = 0; x_coord < renderWidth; x_coord++)
                {
                    decimal re = currentCenterX_param + ((decimal)x_coord - (decimal)renderWidth / 2.0m) * scaleX / (decimal)renderWidth;
                    decimal im = currentCenterY_param - ((decimal)y_coord - (decimal)renderHeight / 2.0m) * scaleY / (decimal)renderHeight;

                    ComplexDecimal z_val = new ComplexDecimal(re, im);
                    int iter_val = 0;

                    while (iter_val < currentMaxIterations_param && z_val.MagnitudeSquared <= currentThresholdSquared_param_local)
                    {
                        z_val = new ComplexDecimal(Math.Abs(z_val.Real), Math.Abs(z_val.Imaginary));
                        z_val = z_val * z_val + currentC_param;
                        iter_val++;
                    }

                    Color pixelColor = GetPixelColor(iter_val, currentMaxIterations_param, currentMaxColorIter_param);
                    int index = rowOffset + x_coord * 3;
                    buffer[index] = pixelColor.B; buffer[index + 1] = pixelColor.G; buffer[index + 2] = pixelColor.R;
                }
                long currentDone = Interlocked.Increment(ref done);
                if (renderHeight > 0) reportProgressCallback((int)(100.0 * currentDone / renderHeight));
            });

            Marshal.Copy(buffer, 0, scan0, buffer.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || width <= 0 || height <= 0) return;

            decimal zoomFactor = e.Delta > 0 ? 1.5m : 1.0m / 1.5m;
            decimal oldZoom = this.zoom;

            decimal mouseX = e.X;
            decimal mouseY = e.Y;

            decimal aspectRatio = (decimal)height / (decimal)width;

            decimal currentPixelScaleX = (BASE_SCALE / oldZoom) / (decimal)width;
            decimal currentPixelScaleY = (BASE_SCALE / oldZoom * aspectRatio) / (decimal)height;

            decimal mouseRe = this.centerX + (mouseX - (decimal)width / 2.0m) * currentPixelScaleX;
            decimal mouseIm = this.centerY - (mouseY - (decimal)height / 2.0m) * currentPixelScaleY;

            this.zoom = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, this.zoom * zoomFactor));

            decimal newPixelScaleX = (BASE_SCALE / this.zoom) / (decimal)width;
            decimal newPixelScaleY = (BASE_SCALE / this.zoom * aspectRatio) / (decimal)height;

            this.centerX = mouseRe - (mouseX - (decimal)width / 2.0m) * newPixelScaleX;
            this.centerY = mouseIm + (mouseY - (decimal)height / 2.0m) * newPixelScaleY;

            canvas1.Invalidate();
            if (nudZoom.Value != this.zoom)
            {
                nudZoom.Value = this.zoom;
            }
            else
            {
                ScheduleRender();
            }
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left) { panning = true; panStart = e.Location; }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || !panning || width <= 0 || height <= 0) return;

            decimal aspectRatio = (decimal)height / (decimal)width;
            decimal currentPixelScaleX = (BASE_SCALE / this.zoom) / (decimal)width;
            decimal currentPixelScaleY = (BASE_SCALE / this.zoom * aspectRatio) / (decimal)height;

            decimal dx = e.X - panStart.X;
            decimal dy = e.Y - panStart.Y;

            this.centerX -= dx * currentPixelScaleX;
            this.centerY += dy * currentPixelScaleY;

            panStart = e.Location;
            canvas1.Invalidate();
            ScheduleRender();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left) panning = false;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (isHighResRendering)
            {
                MessageBox.Show("Идет сохранение в высоком разрешении. Пожалуйста, подождите.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new SaveFileDialog { Filter = "PNG Image|*.png" })
            {
                dlg.FileName = $"fractal_burningship_julia_preview_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (canvas1.Image != null) canvas1.Image.Save(dlg.FileName, ImageFormat.Png);
                    else MessageBox.Show("Нет изображения для сохранения!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnRender_Click(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            ScheduleRender();
        }

        private void SetMainControlsEnabled(bool enabled)
        {
            Action action = () =>
            {
                if (btnRender1 != null) btnRender1.Enabled = enabled;
                nudRe1.Enabled = enabled;
                nudIm1.Enabled = enabled;
                nudIterations1.Enabled = enabled;
                nudThreshold1.Enabled = enabled;
                cbThreads1.Enabled = enabled;
                nudZoom.Enabled = enabled;
                nudBaseScale.Enabled = enabled;
                if (nudW != null) nudW.Enabled = enabled;
                if (nudH != null) nudH.Enabled = enabled;
                foreach (var cb_item in paletteCheckBoxes.Where(cb_item => cb_item != null)) cb_item.Enabled = enabled;
                if (enabled) HandleColorBoxEnableState();
                else if (colorBox != null) colorBox.Enabled = false;
            };
            if (this.InvokeRequired) this.Invoke(action);
            else action();
        }

        private async void btnSave_Click_1(object sender, EventArgs e)
        {
            if (isHighResRendering) { MessageBox.Show("Процесс сохранения в высоком разрешении уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            int saveWidth = (int)nudW.Value; int saveHeight = (int)nudH.Value;
            if (saveWidth <= 0 || saveHeight <= 0) { MessageBox.Show("Ширина и высота изображения для сохранения должны быть больше 0.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            string reValueString = nudRe1.Value.ToString("F25", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            string imValueString = nudIm1.Value.ToString("F25", System.Globalization.CultureInfo.InvariantCulture).Replace(".", "_");
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"fractal_burningship_julia_re{reValueString}_im{imValueString}_{timestamp}.png";

            using (SaveFileDialog saveDialog = new SaveFileDialog { Filter = "PNG Image|*.png", Title = "Сохранить фрактал Горящий Корабль (Жюлиа - Высокое разрешение)", FileName = suggestedFileName })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    Button currentActionSaveButton = sender as Button; isHighResRendering = true; if (currentActionSaveButton != null) currentActionSaveButton.Enabled = false; SetMainControlsEnabled(false);
                    if (progressPNG != null) { progressPNG.Value = 0; progressPNG.Visible = true; }
                    try
                    {
                        UpdateParameters();

                        ComplexDecimal currentC_Capture = this.c;
                        int currentMaxIterations_Capture = this.maxIterations;
                        decimal currentThresholdSquared_Capture = this.thresholdSquared;
                        decimal currentZoom_Capture = this.zoom;
                        decimal currentCenterX_Capture = this.centerX;
                        decimal currentCenterY_Capture = this.centerY;
                        int currentThreadCount_Capture = this.threadCount;
                        decimal currentBaseScale_Capture = BASE_SCALE;

                        Bitmap highResBitmap = await Task.Run(() => RenderFractalToBitmap(
                            saveWidth, saveHeight,
                            currentCenterX_Capture, currentCenterY_Capture, currentZoom_Capture,
                            currentBaseScale_Capture,
                            currentC_Capture,
                            currentMaxIterations_Capture, currentThresholdSquared_Capture,
                            currentThreadCount_Capture,
                            progressPercentage =>
                            {
                                if (progressPNG != null && progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
                                {
                                    try { progressPNG.Invoke((Action)(() => { if (progressPNG.Maximum > 0 && progressPNG.Value <= progressPNG.Maximum) progressPNG.Value = Math.Min(progressPNG.Maximum, progressPercentage); })); }
                                    catch (ObjectDisposedException) { }
                                    catch (InvalidOperationException) { }
                                }
                            }));
                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png); highResBitmap.Dispose();
                        MessageBox.Show("Изображение успешно сохранено в высоком разрешении!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show($"Ошибка при сохранении изображения: {ex.Message}\n{ex.StackTrace}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    finally
                    {
                        isHighResRendering = false; if (currentActionSaveButton != null) currentActionSaveButton.Enabled = true; SetMainControlsEnabled(true);
                        if (progressPNG != null && progressPNG.IsHandleCreated && !progressPNG.IsDisposed) { try { progressPNG.Invoke((Action)(() => { progressPNG.Visible = false; progressPNG.Value = 0; })); } catch (ObjectDisposedException) { } catch (InvalidOperationException) { } }
                    }
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            renderTimer?.Stop();
            previewRenderCts?.Cancel();
            previewRenderCts?.Dispose();
            renderTimer?.Dispose();
            mandelbrotCSelectorWindow?.Close();
            base.OnFormClosed(e);
        }

        #region Palettes
        private delegate Color PaletteFunction(double t, int iter, int maxIterations, int maxColorIter);

        private Color GetPixelColor(int iter, int currentMaxIterations, int currentMaxColorIter)
        {
            if (iter == currentMaxIterations) return (lastSelectedPaletteCheckBox?.Name == "checkBox6") ? Color.FromArgb(50, 50, 50) : Color.Black;
            double t_capped = (double)Math.Min(iter, currentMaxColorIter) / currentMaxColorIter;
            double t_log = Math.Log(Math.Min(iter, currentMaxColorIter) + 1) / Math.Log(currentMaxColorIter + 1);
            PaletteFunction selectedPaletteFunc = GetDefaultPaletteColor;
            if (lastSelectedPaletteCheckBox != null)
            {
                if (lastSelectedPaletteCheckBox == colorBox) selectedPaletteFunc = GetPaletteColorBoxColor;
                else if (lastSelectedPaletteCheckBox == oldRenderBW) selectedPaletteFunc = GetPaletteOldBWColor;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox1") selectedPaletteFunc = GetPalette1Color;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox2") selectedPaletteFunc = GetPalette2Color;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox3") selectedPaletteFunc = GetPalette3Color;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox4") selectedPaletteFunc = GetPalette4Color;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox5") selectedPaletteFunc = GetPalette5Color;
                else if (lastSelectedPaletteCheckBox.Name == "checkBox6") selectedPaletteFunc = GetPalette6Color;
            }
            double t_param = (selectedPaletteFunc == GetDefaultPaletteColor) ? t_log : t_capped;
            return selectedPaletteFunc(t_param, iter, currentMaxIterations, currentMaxColorIter);
        }

        private Color GetDefaultPaletteColor(double t_log, int iter, int maxIter, int maxClrIter) { int cVal = (int)(255.0 * (1 - t_log)); return Color.FromArgb(cVal, cVal, cVal); }
        private Color GetPaletteColorBoxColor(double t_capped, int iter, int maxIter, int maxClrIter) { return ColorFromHSV(360.0 * t_capped, 0.6, 1.0); }
        private Color GetPaletteOldBWColor(double t_capped, int iter, int maxIter, int maxClrIter) { int cVal = 255 - (int)(255.0 * t_capped); return Color.FromArgb(cVal, cVal, cVal); }
        private Color LerpColor(Color a, Color b, double t) { t = Math.Max(0, Math.Min(1, t)); return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
        private Color GetPalette1Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.Black, c2 = Color.FromArgb(200, 0, 0), c3 = Color.FromArgb(255, 100, 0), c4 = Color.FromArgb(255, 255, 100), c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        private Color GetPalette2Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.Black, c2 = Color.FromArgb(0, 0, 100), c3 = Color.FromArgb(0, 120, 200), c4 = Color.FromArgb(170, 220, 255), c5 = Color.White; if (t < 0.25) return LerpColor(c1, c2, t / 0.25); if (t < 0.50) return LerpColor(c2, c3, (t - 0.25) / 0.25); if (t < 0.75) return LerpColor(c3, c4, (t - 0.50) / 0.25); return LerpColor(c4, c5, (t - 0.75) / 0.25); }
        private Color GetPalette3Color(double t, int iter, int maxIter, int maxClrIter) { double r = Math.Sin(t * Math.PI * 3.0 + 0.5) * 0.45 + 0.5, g = Math.Sin(t * Math.PI * 3.0 + Math.PI * 2.0 / 3.0 + 0.5) * 0.45 + 0.5, b = Math.Sin(t * Math.PI * 3.0 + Math.PI * 4.0 / 3.0 + 0.5) * 0.45 + 0.5; return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255)); }
        private Color GetPalette4Color(double t, int iter, int maxIter, int maxClrIter) { Color c1 = Color.FromArgb(10, 0, 20), c2 = Color.FromArgb(255, 0, 255), c3 = Color.FromArgb(0, 255, 255), c4 = Color.FromArgb(230, 230, 250); if (t < 0.1) return LerpColor(c1, c2, t / 0.1); if (t < 0.4) return LerpColor(c2, c1, (t - 0.1) / 0.3); if (t < 0.5) return LerpColor(c1, c3, (t - 0.4) / 0.1); if (t < 0.8) return LerpColor(c3, c1, (t - 0.5) / 0.3); return LerpColor(c1, c4, (t - 0.8) / 0.2); }
        private Color GetPalette5Color(double t, int iter, int maxIter, int maxClrIter) { int g_val = 50 + (int)(t * 150); double s_val = Math.Sin(t * Math.PI * 5); int f_val = Math.Max(0, Math.Min(255, g_val + (int)(s_val * 40))); return Color.FromArgb(f_val, f_val, Math.Min(255, f_val + (int)(t * 25))); }
        private Color GetPalette6Color(double t, int iter, int maxIter, int maxClrIter) { double h = (t * 200.0 + 180.0) % 360.0, s = Math.Max(0.2, Math.Min(0.6, 0.35 + (Math.Sin(t * Math.PI * 2) * 0.1))), v = Math.Max(0.7, Math.Min(0.95, 0.80 + (Math.Cos(t * Math.PI * 2.5) * 0.15))); return ColorFromHSV(h, s, v); }
        private Color ColorFromHSV(double hue, double saturation, double value) { hue = (hue % 360 + 360) % 360; int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6; double f = hue / 60 - Math.Floor(hue / 60); value = Math.Max(0, Math.Min(1, value)); saturation = Math.Max(0, Math.Min(1, saturation)); int v_comp = Convert.ToInt32(value * 255); int p_comp = Convert.ToInt32(v_comp * (1 - saturation)); int q_comp = Convert.ToInt32(v_comp * (1 - f * saturation)); int t_comp = Convert.ToInt32(v_comp * (1 - (1 - f) * saturation)); switch (hi) { case 0: return Color.FromArgb(v_comp, t_comp, p_comp); case 1: return Color.FromArgb(q_comp, v_comp, p_comp); case 2: return Color.FromArgb(p_comp, v_comp, t_comp); case 3: return Color.FromArgb(p_comp, q_comp, v_comp); case 4: return Color.FromArgb(t_comp, p_comp, v_comp); default: return Color.FromArgb(v_comp, p_comp, q_comp); } }
        #endregion
    }
}
// --- END OF FILE FractalburningShipJulia.cs ---