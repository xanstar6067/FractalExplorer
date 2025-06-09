using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FractalExplorer
{
    public partial class NewtonPpools : Form
    {
        private System.Windows.Forms.Timer renderTimer;
        private int maxIterations;
        private double threshold;
        private int threadCount;
        private int width, height;
        private Point panStart;
        private bool panning = false;
        private double zoom = 1.0;
        private double centerX = 0.0;
        private double centerY = 0.0;
        private const double BASE_SCALE = 3.0;
        private bool isHighResRendering = false;
        private volatile bool isRenderingPreview = false;
        private CancellationTokenSource previewRenderCts;
        private double renderedCenterX;
        private double renderedCenterY;
        private double renderedZoom;
        private string[] presetPolynomials = { "z^3-1", "z^4-1", "z^3-2z+2" };

        public NewtonPpools()
        {
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeForm()
        {
            width = fractal_bitmap.Width;
            height = fractal_bitmap.Height;

            renderTimer = new System.Windows.Forms.Timer { Interval = 300 };
            renderTimer.Tick += RenderTimer_Tick;

            foreach (string poly in presetPolynomials)
            {
                cbSelector.Items.Add(poly);
            }
            cbSelector.SelectedIndex = 0;

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++)
            {
                cbThreads.Items.Add(i);
            }
            cbThreads.Items.Add("Auto");
            cbThreads.SelectedItem = "Auto";

            nudIterations.ValueChanged += ParamControl_Changed;
            nudThreshold.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;
            cbSelector.SelectedIndexChanged += cbSelector_SelectedIndexChanged;

            fractal_bitmap.MouseWheel += Canvas_MouseWheel;
            fractal_bitmap.MouseDown += Canvas_MouseDown;
            fractal_bitmap.MouseMove += Canvas_MouseMove;
            fractal_bitmap.MouseUp += Canvas_MouseUp;
            fractal_bitmap.Paint += Canvas_Paint;

            this.Resize += Form_Resize;
            fractal_bitmap.Resize += Canvas_Resize;

            renderedCenterX = centerX;
            renderedCenterY = centerY;
            renderedZoom = zoom;

            ScheduleRender();
        }

        private void cbSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbSelector.SelectedIndex >= 0)
            {
                richTextBox1.Text = cbSelector.SelectedItem.ToString();
                ScheduleRender();
            }
        }

        private void Form_Resize(object sender, EventArgs e) => ResizeCanvas();
        private void Canvas_Resize(object sender, EventArgs e) => ResizeCanvas();

        private void ResizeCanvas()
        {
            if (isHighResRendering) return;
            if (fractal_bitmap.Width <= 0 || fractal_bitmap.Height <= 0) return;
            width = fractal_bitmap.Width;
            height = fractal_bitmap.Height;
            ScheduleRender();
        }

        private void ParamControl_Changed(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            if (sender == nudZoom)
            {
                zoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, (double)nudZoom.Value));
                if (nudZoom.Value != (decimal)zoom)
                {
                    nudZoom.Value = (decimal)zoom;
                }
            }
            ScheduleRender();
        }

        private void ScheduleRender()
        {
            if (isHighResRendering) return;
            previewRenderCts?.Cancel();
            renderTimer.Stop();
            renderTimer.Start();
        }

        private async void RenderTimer_Tick(object sender, EventArgs e)
        {
            renderTimer.Stop();
            if (isHighResRendering || isRenderingPreview) return;

            isRenderingPreview = true;
            previewRenderCts?.Dispose();
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;

            UpdateParameters();
            double currentRenderCenterX = centerX;
            double currentRenderCenterY = centerY;
            double currentRenderZoom = zoom;
            string polyStr = string.Empty;

            if (richTextBox1.InvokeRequired)
            {
                polyStr = (string)richTextBox1.Invoke(new Func<string>(() => richTextBox1.Text));
            }
            else
            {
                polyStr = richTextBox1.Text;
            }

            try
            {
                await Task.Run(() => RenderFractal(token, currentRenderCenterX, currentRenderCenterY, currentRenderZoom, polyStr), token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isRenderingPreview = false;
            }
        }

        private void UpdateParameters()
        {
            maxIterations = (int)nudIterations.Value;
            threshold = (double)nudThreshold.Value;
            threadCount = cbThreads.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (fractal_bitmap.Image == null || width <= 0 || height <= 0)
            {
                e.Graphics.Clear(Color.Black);
                return;
            }

            double scaleRendered = BASE_SCALE / renderedZoom;
            double scaleCurrent = BASE_SCALE / zoom;

            if (renderedZoom <= 0 || zoom <= 0 || scaleRendered <= 0 || scaleCurrent <= 0)
            {
                e.Graphics.Clear(Color.Black);
                e.Graphics.DrawImageUnscaled(fractal_bitmap.Image, Point.Empty);
                return;
            }

            double complex_half_width_rendered = (BASE_SCALE / renderedZoom) / 2.0;
            double complex_half_height_rendered = (BASE_SCALE / renderedZoom) / 2.0;
            double complex_half_width_current = (BASE_SCALE / zoom) / 2.0;
            double complex_half_height_current = (BASE_SCALE / zoom) / 2.0;

            double renderedImage_re_min = renderedCenterX - complex_half_width_rendered;
            double renderedImage_im_min = renderedCenterY - complex_half_height_rendered;
            double currentView_re_min = centerX - complex_half_width_current;
            double currentView_im_min = centerY - complex_half_height_current;

            float p1_X = (float)((renderedImage_re_min - currentView_re_min) / (complex_half_width_current * 2.0) * width);
            float p1_Y = (float)((renderedImage_im_min - currentView_im_min) / (complex_half_height_current * 2.0) * height);

            float w_prime = (float)(width * ((BASE_SCALE / renderedZoom) / (BASE_SCALE / zoom)));
            float h_prime = (float)(height * ((BASE_SCALE / renderedZoom) / (BASE_SCALE / zoom)));

            PointF destPoint1 = new PointF(p1_X, p1_Y);
            PointF destPoint2 = new PointF(p1_X + w_prime, p1_Y);
            PointF destPoint3 = new PointF(p1_X, p1_Y + h_prime);

            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            if (w_prime > 0 && h_prime > 0)
            {
                try
                {
                    e.Graphics.DrawImage(fractal_bitmap.Image, new PointF[] { destPoint1, destPoint2, destPoint3 });
                }
                catch (ArgumentException)
                {
                    e.Graphics.DrawImageUnscaled(fractal_bitmap.Image, Point.Empty);
                }
            }
            else
            {
                e.Graphics.DrawImageUnscaled(fractal_bitmap.Image, Point.Empty);
            }
        }

        private void RenderFractal(CancellationToken token, double renderCenterX, double renderCenterY, double renderZoom, string polyStr)
        {
            if (token.IsCancellationRequested || isHighResRendering || fractal_bitmap.Width <= 0 || fractal_bitmap.Height <= 0) return;

            Polynomial p;
            try
            {
                p = ParsePolynomial(polyStr);
            }
            catch (Exception ex)
            {
                if (fractal_bitmap.IsHandleCreated && !fractal_bitmap.IsDisposed)
                {
                    fractal_bitmap.Invoke((Action)(() =>
                    {
                        MessageBox.Show($"Ошибка парсинга полинома: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                return;
            }

            List<Complex> roots = FindRoots(p);
            if (roots.Count == 0)
            {
                if (fractal_bitmap.IsHandleCreated && !fractal_bitmap.IsDisposed)
                {
                    fractal_bitmap.Invoke((Action)(() =>
                    {
                        MessageBox.Show("Не удалось найти корни полинома.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
                return;
            }

            Color[] rootColors = new Color[roots.Count];
            for (int i = 0; i < roots.Count; i++)
            {
                int shade = 255 * (i + 1) / (roots.Count + 1);
                rootColors[i] = Color.FromArgb(shade, shade, shade);
            }

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
                double epsilon = threshold * 1e-3;

                double scale_factor_w = (BASE_SCALE / renderZoom) / width;
                double scale_factor_h = (BASE_SCALE / renderZoom) / height;

                Parallel.For(0, height, po, y =>
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        double c_re = renderCenterX + (x - width / 2.0) * scale_factor_w;
                        double c_im = renderCenterY + (y - height / 2.0) * scale_factor_h;
                        Complex z = new Complex(c_re, c_im);
                        Polynomial pDeriv = p.Derivative();

                        int iter = 0;
                        while (iter < maxIterations)
                        {
                            Complex pz = p.Evaluate(z);
                            if (pz.Magnitude < epsilon) break;
                            Complex pDz = pDeriv.Evaluate(z);
                            if (pDz.Magnitude < epsilon) break;
                            z = z - pz / pDz;
                            iter++;
                        }

                        int rootIndex = -1;
                        double minDist = double.MaxValue;
                        for (int r = 0; r < roots.Count; r++)
                        {
                            double dist = (z - roots[r]).Magnitude;
                            if (dist < minDist)
                            {
                                minDist = dist;
                                rootIndex = r;
                            }
                        }

                        Color pixelColor = (rootIndex >= 0 && minDist < epsilon) ? rootColors[rootIndex] : Color.Black;
                        int index = rowOffset + x * 3;
                        buffer[index] = pixelColor.B;
                        buffer[index + 1] = pixelColor.G;
                        buffer[index + 2] = pixelColor.R;
                    }

                    int progress = Interlocked.Increment(ref done);
                    if (!token.IsCancellationRequested && progressBar.IsHandleCreated && !progressBar.IsDisposed)
                    {
                        progressBar.BeginInvoke((Action)(() =>
                        {
                            if (progressBar.Value <= progressBar.Maximum)
                                progressBar.Value = Math.Min(progressBar.Maximum, (int)(100.0 * progress / height));
                        }));
                    }
                });

                token.ThrowIfCancellationRequested();
                Marshal.Copy(buffer, 0, scan0, buffer.Length);
                bmp.UnlockBits(bmpData);
                bmpData = null;

                if (fractal_bitmap.IsHandleCreated && !fractal_bitmap.IsDisposed)
                {
                    Bitmap oldImage = null;
                    fractal_bitmap.Invoke((Action)(() =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            bmp?.Dispose();
                            return;
                        }
                        oldImage = fractal_bitmap.Image as Bitmap;
                        fractal_bitmap.Image = bmp;
                        renderedCenterX = renderCenterX;
                        renderedCenterY = renderCenterY;
                        renderedZoom = renderZoom;
                        bmp = null;
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
                if (bmpData != null && bmp != null) bmp.UnlockBits(bmpData);
                bmp?.Dispose();
            }
        }

        private Bitmap RenderFractalToBitmap(int renderWidth, int renderHeight, double currentCenterX, double currentCenterY,
                                            double currentZoom, int currentMaxIterations, double currentThreshold,
                                            int numThreads, Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0) return new Bitmap(1, 1);

            string polyStr = string.Empty;
            if (richTextBox1.InvokeRequired)
            {
                polyStr = (string)richTextBox1.Invoke(new Func<string>(() => richTextBox1.Text));
            }
            else
            {
                polyStr = richTextBox1.Text;
            }

            Polynomial p = ParsePolynomial(polyStr);
            List<Complex> roots = FindRoots(p);
            Color[] rootColors = new Color[roots.Count];
            for (int i = 0; i < roots.Count; i++)
            {
                int shade = 255 * (i + 1) / (roots.Count + 1);
                rootColors[i] = Color.FromArgb(shade, shade, shade);
            }

            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            byte[] buffer = new byte[Math.Abs(stride) * renderHeight];

            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;
            double epsilon = currentThreshold * 1e-3;

            double scale_factor_w = (BASE_SCALE / currentZoom) / renderWidth;
            double scale_factor_h = (BASE_SCALE / currentZoom) / renderHeight;

            Parallel.For(0, renderHeight, po, y =>
            {
                int rowOffset = y * stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    double c_re = currentCenterX + (x - renderWidth / 2.0) * scale_factor_w;
                    double c_im = currentCenterY + (y - renderHeight / 2.0) * scale_factor_h;
                    Complex z = new Complex(c_re, c_im);
                    Polynomial pDeriv = p.Derivative();

                    int iter = 0;
                    while (iter < currentMaxIterations)
                    {
                        Complex pz = p.Evaluate(z);
                        if (pz.Magnitude < epsilon) break;
                        Complex pDz = pDeriv.Evaluate(z);
                        if (pDz.Magnitude < epsilon) break;
                        z = z - pz / pDz;
                        iter++;
                    }

                    int rootIndex = -1;
                    double minDist = double.MaxValue;
                    for (int r = 0; r < roots.Count; r++)
                    {
                        double dist = (z - roots[r]).Magnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            rootIndex = r;
                        }
                    }

                    Color pixelColor = (rootIndex >= 0 && minDist < epsilon) ? rootColors[rootIndex] : Color.Black;
                    int index = rowOffset + x * 3;
                    buffer[index] = pixelColor.B;
                    buffer[index + 1] = pixelColor.G;
                    buffer[index + 2] = pixelColor.R;
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
            if (isHighResRendering) return;

            double zoomFactor = e.Delta > 0 ? 1.5 : 1.0 / 1.5;
            double oldZoom = zoom;

            double scaleBeforeZoomX = BASE_SCALE / oldZoom / width;
            double scaleBeforeZoomY = BASE_SCALE / oldZoom / height;

            double mouseRe = centerX + (e.X - width / 2.0) * scaleBeforeZoomX;
            double mouseIm = centerY + (e.Y - height / 2.0) * scaleBeforeZoomY;

            zoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, zoom * zoomFactor));

            double scaleAfterZoomX = BASE_SCALE / zoom / width;
            double scaleAfterZoomY = BASE_SCALE / zoom / height;

            centerX = mouseRe - (e.X - width / 2.0) * scaleAfterZoomX;
            centerY = mouseIm - (e.Y - height / 2.0) * scaleAfterZoomY;

            fractal_bitmap.Invalidate();
            if (nudZoom.Value != (decimal)zoom) nudZoom.Value = (decimal)zoom;
            else ScheduleRender();
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                panning = true;
                panStart = e.Location;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || !panning) return;

            double scaleX = BASE_SCALE / zoom / width;
            double scaleY = BASE_SCALE / zoom / height;

            centerX -= (e.X - panStart.X) * scaleX;
            centerY -= (e.Y - panStart.Y) * scaleY;
            panStart = e.Location;

            fractal_bitmap.Invalidate();
            ScheduleRender();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left) panning = false;
        }

        private void btnRender_Click(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            ScheduleRender();
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            if (isHighResRendering)
            {
                MessageBox.Show("Процесс сохранения уже запущен.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int saveWidth = (int)nudW.Value;
            int saveHeight = (int)nudH.Value;

            if (saveWidth <= 0 || saveHeight <= 0)
            {
                MessageBox.Show("Ширина и высота должны быть больше 0.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string suggestedFileName = $"newton_pools_{timestamp}.png";

            using (SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Сохранить фрактал Ньютона",
                FileName = suggestedFileName
            })
            {
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    isHighResRendering = true;
                    btnSave.Enabled = false;
                    SetMainControlsEnabled(false);
                    progressPNG.Value = 0;
                    progressPNG.Visible = true;

                    try
                    {
                        UpdateParameters();
                        int currentMaxIterations = this.maxIterations;
                        double currentThreshold = this.threshold;
                        double currentZoom = this.zoom;
                        double currentCenterX = this.centerX;
                        double currentCenterY = this.centerY;
                        int currentThreadCount = this.threadCount;

                        Bitmap highResBitmap = await Task.Run(() => RenderFractalToBitmap(
                            saveWidth, saveHeight, currentCenterX, currentCenterY, currentZoom,
                            currentMaxIterations, currentThreshold, currentThreadCount,
                            progressPercentage =>
                            {
                                if (progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
                                {
                                    progressPNG.Invoke((Action)(() =>
                                    {
                                        if (progressPNG.Value <= progressPNG.Maximum)
                                            progressPNG.Value = Math.Min(progressPNG.Maximum, progressPercentage);
                                    }));
                                }
                            }
                        ));
                        highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                        highResBitmap.Dispose();
                        MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        isHighResRendering = false;
                        btnSave.Enabled = true;
                        SetMainControlsEnabled(true);
                        if (progressPNG.IsHandleCreated && !progressPNG.IsDisposed)
                        {
                            progressPNG.Invoke((Action)(() => { progressPNG.Visible = false; progressPNG.Value = 0; }));
                        }
                    }
                }
            }
        }

        private void SetMainControlsEnabled(bool enabled)
        {
            Action action = () =>
            {
                btnRender.Enabled = enabled;
                nudIterations.Enabled = enabled;
                nudThreshold.Enabled = enabled;
                cbThreads.Enabled = enabled;
                nudZoom.Enabled = enabled;
                nudW.Enabled = enabled;
                nudH.Enabled = enabled;
                cbSelector.Enabled = enabled;
                richTextBox1.Enabled = enabled;
            };

            if (this.InvokeRequired) this.Invoke(action);
            else action();
        }

        private Polynomial ParsePolynomial(string polyStr)
        {
            polyStr = polyStr.Replace(" ", "");
            string[] terms = Regex.Split(polyStr, @"(?=[+-])");
            Dictionary<int, double> coeffsDict = new Dictionary<int, double>();

            foreach (string term in terms)
            {
                if (string.IsNullOrEmpty(term)) continue;

                // Определяем знак и убираем его из терма
                char sign = term[0] == '-' ? '-' : '+';
                string termWithoutSign = term[0] == '+' || term[0] == '-' ? term.Substring(1) : term;

                // Обработка членов вида az^b, z^b или z
                Match match = Regex.Match(termWithoutSign, @"^((\d*\.?\d+)?z)(\^(\d+))?$");
                if (match.Success)
                {
                    string coeffStr = match.Groups[2].Value;
                    string powerStr = match.Groups[4].Value;
                    double coeff = string.IsNullOrEmpty(coeffStr) ? 1.0 : double.Parse(coeffStr);
                    int power = string.IsNullOrEmpty(powerStr) ? 1 : int.Parse(powerStr);
                    if (sign == '-') coeff = -coeff;

                    coeffsDict[power] = coeffsDict.ContainsKey(power) ? coeffsDict[power] + coeff : coeff;
                }
                else
                {
                    // Обработка констант
                    match = Regex.Match(termWithoutSign, @"^\d+$");
                    if (match.Success)
                    {
                        double coeff = double.Parse(termWithoutSign);
                        if (sign == '-') coeff = -coeff;
                        coeffsDict[0] = coeffsDict.ContainsKey(0) ? coeffsDict[0] + coeff : coeff;
                    }
                    else
                    {
                        throw new ArgumentException($"Неверный формат слагаемого: {term}");
                    }
                }
            }

            int maxPower = coeffsDict.Keys.Any() ? coeffsDict.Keys.Max() : 0;
            List<double> coefficients = new List<double>();
            for (int i = 0; i <= maxPower; i++)
            {
                coefficients.Add(coeffsDict.ContainsKey(i) ? coeffsDict[i] : 0.0);
            }
            return new Polynomial(coefficients);
        }

        private List<Complex> FindRoots(Polynomial p, int maxIter = 100, double epsilon = 1e-6)
        {
            List<Complex> roots = new List<Complex>();
            Polynomial pDeriv = p.Derivative();
            double[] reValues = { -1.0, 0.0, 1.0 };
            double[] imValues = { -1.0, 0.0, 1.0 };

            foreach (double re in reValues)
            {
                foreach (double im in imValues)
                {
                    Complex z = new Complex(re, im);
                    for (int i = 0; i < maxIter; i++)
                    {
                        Complex pz = p.Evaluate(z);
                        Complex pDz = pDeriv.Evaluate(z);
                        if (pDz.Magnitude < epsilon) break;
                        Complex zNext = z - pz / pDz;
                        if ((zNext - z).Magnitude < epsilon)
                        {
                            bool isNew = true;
                            foreach (Complex root in roots)
                            {
                                if ((zNext - root).Magnitude < epsilon)
                                {
                                    isNew = false;
                                    break;
                                }
                            }
                            if (isNew) roots.Add(zNext);
                            break;
                        }
                        z = zNext;
                    }
                }
            }
            return roots;
        }

        private class Polynomial
        {
            public List<double> coefficients;

            public Polynomial(List<double> coeffs)
            {
                coefficients = coeffs;
            }

            public Complex Evaluate(Complex z)
            {
                Complex result = Complex.Zero;
                for (int i = 0; i < coefficients.Count; i++)
                {
                    result += coefficients[i] * Complex.Pow(z, i);
                }
                return result;
            }

            public Polynomial Derivative()
            {
                if (coefficients.Count <= 1) return new Polynomial(new List<double> { 0 });
                List<double> derivCoeffs = new List<double>();
                for (int i = 1; i < coefficients.Count; i++)
                {
                    derivCoeffs.Add(i * coefficients[i]);
                }
                return new Polynomial(derivCoeffs);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            renderTimer?.Stop();
            previewRenderCts?.Cancel();
            previewRenderCts?.Dispose();
            renderTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}