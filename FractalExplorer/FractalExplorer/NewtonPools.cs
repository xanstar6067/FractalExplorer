using MathNet.Numerics;
using MathNet.Symbolics;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Expr = MathNet.Symbolics.Expression;

namespace FractalExplorer
{
    public class Polynomial
    {
        public List<Complex> coefficients;

        public Polynomial(List<Complex> coeffs)
        {
            coefficients = coeffs ?? new List<Complex>();
        }

        public Complex Evaluate(Complex z)
        {
            Complex result = Complex.Zero;
            for (int i = 0; i < coefficients.Count; i++)
            {
                if (coefficients[i] != Complex.Zero)
                {
                    result += coefficients[i] * Complex.Pow(z, i);
                }
            }
            return result;
        }

        public Polynomial Derivative()
        {
            if (coefficients.Count <= 1)
            {
                return new Polynomial(new List<Complex> { Complex.Zero });
            }
            List<Complex> derivCoeffs = new List<Complex>();
            for (int i = 1; i < coefficients.Count; i++)
            {
                derivCoeffs.Add(i * coefficients[i]);
            }
            if (!derivCoeffs.Any() || derivCoeffs.All(c => c == Complex.Zero))
            {
                return new Polynomial(new List<Complex> { Complex.Zero });
            }
            return new Polynomial(derivCoeffs);
        }
    }

    public static class PolynomialParser
    {
        private static Complex EvaluateConstantExpression(Expr expression)
        {
            if (expression == Expr.I)
            {
                return Complex.ImaginaryOne;
            }
            else if (expression == Expr.Zero)
            {
                return Complex.Zero;
            }
            else if (expression == Expr.One)
            {
                return Complex.One;
            }
            else if (expression.IsNumber)
            {
                string exprStr = expression.ToString();
                if (double.TryParse(exprStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double real))
                {
                    return new Complex(real, 0);
                }
                else if (exprStr.Contains("I"))
                {
                    exprStr = exprStr.Replace("I", "");
                    if (string.IsNullOrEmpty(exprStr) || exprStr == "-")
                    {
                        return new Complex(0, exprStr == "-" ? -1 : 1);
                    }
                    if (double.TryParse(exprStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double imag))
                    {
                        return new Complex(0, imag);
                    }
                }
                throw new InvalidOperationException($"Невозможно распознать константу '{exprStr}'.");
            }
            throw new InvalidOperationException($"Выражение '{expression.ToString()}' не является константой.");
        }

        public static Polynomial ParsePolynomialWithMathNet(string polyStr)
        {
            if (string.IsNullOrWhiteSpace(polyStr))
            {
                return new Polynomial(new List<Complex> { Complex.Zero });
            }

            string processedPolyStr = polyStr.Replace(" ", "");
            processedPolyStr = Regex.Replace(processedPolyStr, @"(\d)([a-zA-Z\(])", "$1*$2");
            processedPolyStr = Regex.Replace(processedPolyStr, @"(\))([a-zA-Z\d\(])", "$1*$2");
            processedPolyStr = Regex.Replace(processedPolyStr, @"([a-zA-Z])([\d\(])(?<!sin|cos|tan|ln|log|exp|sqrt)", "$1*$2");
            processedPolyStr = Regex.Replace(processedPolyStr, @"(?<=[^a-zA-Z_]|^)i(?=z|\()", "i*");

            Expr symbolicExpr;
            var parseResult = Infix.Parse(processedPolyStr);
            if (parseResult.IsError)
            {
                throw new ArgumentException($"Ошибка парсинга полинома '{polyStr}': {parseResult.ErrorValue}");
            }
            symbolicExpr = parseResult.ResultValue;

            var z_symbol = Expr.Symbol("z");
            Expr expandedExpr = Algebraic.Expand(symbolicExpr);

            List<Complex> coeffs = new List<Complex>();
            Expr currentExpr = expandedExpr;
            long factorial = 1;
            int maxDegreeToTry = 20;

            for (int k = 0; k < maxDegreeToTry; k++)
            {
                Complex termValue;
                try
                {
                    termValue = EvaluateConstantExpression(currentExpr);
                }
                catch (InvalidOperationException)
                {
                    if (k == 0 && !polyStr.Contains("z"))
                    {
                        try
                        {
                            coeffs.Add(EvaluateConstantExpression(expandedExpr));
                            goto EndLoop;
                        }
                        catch { }
                    }
                    if (k > 0 && coeffs.All(c => c == Complex.Zero)) break;
                    termValue = Complex.Zero;
                }

                coeffs.Add(termValue / factorial);

                if (currentExpr.Equals(Expr.Zero))
                    break;

                currentExpr = Calculus.Differentiate(currentExpr, z_symbol);
                if (k < maxDegreeToTry - 1)
                {
                    factorial *= (k + 1);
                    if (factorial <= 0) factorial = long.MaxValue;
                }

                if (currentExpr.Equals(Expr.Zero) && coeffs.Any(c => c != Complex.Zero))
                    break;
            }
        EndLoop:;

            while (coeffs.Count > 1 && coeffs.Last().Magnitude < 1e-9)
            {
                coeffs.RemoveAt(coeffs.Count - 1);
            }
            if (coeffs.Count == 0) coeffs.Add(Complex.Zero);

            return new Polynomial(coeffs);
        }
    }

    public partial class NewtonPools : Form
    {
        private System.Windows.Forms.Timer renderTimer;
        private int maxIterations;
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
        private string[] presetPolynomials = {
            "z^3-1", "z^4-1", "z^3-2*z+2", "(1+2*i)*z^2+z-1", "(0.5-0.3*i)*z^3+2",
            "z^5-z^2+1", "z^6+3*z^3-2", "z^4-4*z^2+4", "0.5*z^3-1.25*z+2", "z^7+z^4-z+1",
            "(2+i)*z^3-(1-2*i)*z+1", "i*z^4+z-1", "(1+0.5*i)*z^2-z+(2-3*i)",
            "(0.3+1.7*i)*z^3+(1-i)", "(2-i)*z^5+(3+2*i)*z^2-1", "-2*z^3+0.75*z^2-1",
            "z^6-1.5*z^3+0.25", "-0.1*z^4+z-2", "(1/2)*z^3+(3/4)*z-1", "(2+3*i)*z^2-(1-i)*z+4"
        };

        public NewtonPools()
        {
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeForm()
        {
            width = fractal_bitmap.Width > 0 ? fractal_bitmap.Width : 1;
            height = fractal_bitmap.Height > 0 ? fractal_bitmap.Height : 1;

            renderTimer = new System.Windows.Forms.Timer { Interval = 300 };
            renderTimer.Tick += RenderTimer_Tick;

            foreach (string poly in presetPolynomials) cbSelector.Items.Add(poly);
            if (cbSelector.Items.Count > 0) cbSelector.SelectedIndex = 0;
            else richTextBox1.Text = "z^3-1";

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++) cbThreads.Items.Add(i);
            cbThreads.Items.Add("Auto");
            cbThreads.SelectedItem = "Auto";

            nudIterations.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;
            cbSelector.SelectedIndexChanged += cbSelector_SelectedIndexChanged;
            oldRenderBW.CheckedChanged += ParamControl_Changed;
            colorBox0.CheckedChanged += ColorBox_Changed;
            colorBox1.CheckedChanged += ColorBox_Changed;
            colorBox2.CheckedChanged += ColorBox_Changed;
            colorBox3.CheckedChanged += ColorBox_Changed;
            colorBox4.CheckedChanged += ColorBox_Changed;

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
            }
            ScheduleRender();
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
                decimal val = nudZoom.Value;
                double newZoom = Convert.ToDouble(val);
                zoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, newZoom));

                if (nudZoom.Value != (decimal)zoom)
                {
                    nudZoom.ValueChanged -= ParamControl_Changed;
                    nudZoom.Value = (decimal)zoom;
                    nudZoom.ValueChanged += ParamControl_Changed;
                }
            }
            ScheduleRender();
        }

        private void ColorBox_Changed(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            CheckBox currentCb = sender as CheckBox;
            if (currentCb != null && currentCb.Checked)
            {
                foreach (var cb in new[] { colorBox0, colorBox1, colorBox2, colorBox3, colorBox4 })
                {
                    if (cb != null && cb != currentCb) cb.Checked = false;
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
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    MessageBox.Show($"Ошибка рендеринга: {ex.Message}\n\nПодробности: {ex.InnerException?.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                isRenderingPreview = false;
            }
        }

        private void UpdateParameters()
        {
            maxIterations = (int)nudIterations.Value;
            if (cbThreads.SelectedItem != null)
            {
                threadCount = cbThreads.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
            }
            else
            {
                threadCount = Environment.ProcessorCount;
            }
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (fractal_bitmap.Image == null || width <= 0 || height <= 0)
            {
                e.Graphics.Clear(Color.Black);
                return;
            }

            float currentPPU_X = (float)(width / (BASE_SCALE / zoom));
            float currentPPU_Y = (float)(height / (BASE_SCALE / zoom));

            float renderedPPU_X = (float)(fractal_bitmap.Image.Width / (BASE_SCALE / renderedZoom));
            float renderedPPU_Y = (float)(fractal_bitmap.Image.Height / (BASE_SCALE / renderedZoom));

            double renderedImgTopLeftRe = renderedCenterX - (fractal_bitmap.Image.Width / 2.0) / renderedPPU_X;
            double renderedImgTopLeftIm = renderedCenterY + (fractal_bitmap.Image.Height / 2.0) / renderedPPU_Y;

            double currentViewTopLeftRe = centerX - (width / 2.0) / currentPPU_X;
            double currentViewTopLeftIm = centerY + (height / 2.0) / currentPPU_Y;

            float offsetX = (float)(renderedImgTopLeftRe - currentViewTopLeftRe) * currentPPU_X;
            float offsetY = (float)(currentViewTopLeftIm - renderedImgTopLeftIm) * currentPPU_Y;

            float newWidth = (float)(fractal_bitmap.Image.Width * (currentPPU_X / renderedPPU_X));
            float newHeight = (float)(fractal_bitmap.Image.Height * (currentPPU_Y / renderedPPU_Y));

            PointF destPoint1 = new PointF(offsetX, offsetY);
            PointF destPoint2 = new PointF(offsetX + newWidth, offsetY);
            PointF destPoint3 = new PointF(offsetX, offsetY + newHeight);

            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            if (newWidth > 0 && newHeight > 0 && float.IsFinite(offsetX) && float.IsFinite(offsetY) && float.IsFinite(newWidth) && float.IsFinite(newHeight))
            {
                try
                {
                    e.Graphics.DrawImage(fractal_bitmap.Image, new PointF[] { destPoint1, destPoint2, destPoint3 });
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Canvas_Paint DrawImage error: {ex.Message}");
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
            int currentWidth = this.width;
            int currentHeight = this.height;
            if (token.IsCancellationRequested || isHighResRendering || currentWidth <= 0 || currentHeight <= 0) return;

            Polynomial p;
            try
            {
                p = PolynomialParser.ParsePolynomialWithMathNet(polyStr);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка парсинга полинома: {ex.Message}", ex);
            }

            List<Complex> roots = FindRoots(p);
            if (token.IsCancellationRequested) return;

            if (roots.Count == 0 && !(p.coefficients.Count == 1 && p.coefficients[0] == Complex.Zero))
            {
                if (p.coefficients.Any(c => c != Complex.Zero))
                    Console.WriteLine($"Предупреждение: Не удалось найти корни для полинома '{polyStr}'. Отображение может быть черным.");
            }

            Color[] rootColors = new Color[Math.Max(1, roots.Count)];
            bool useBlackWhite = false;
            bool useGradient = false;
            bool usePastel = false;
            bool useContrast = false;
            bool useFire = false;
            bool useContrasting = false;

            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() =>
                {
                    useBlackWhite = oldRenderBW.Checked;
                    useGradient = colorBox0.Checked;
                    usePastel = colorBox1.Checked;
                    useContrast = colorBox2.Checked;
                    useFire = colorBox3.Checked;
                    useContrasting = colorBox4.Checked;
                }));
            }
            else
            {
                useBlackWhite = oldRenderBW.Checked;
                useGradient = colorBox0.Checked;
                usePastel = colorBox1.Checked;
                useContrast = colorBox2.Checked;
                useFire = colorBox3.Checked;
                useContrasting = colorBox4.Checked;
            }

            int numColorsToGenerate = roots.Count > 0 ? roots.Count : 1;
            for (int i = 0; i < numColorsToGenerate; i++)
            {
                if (useGradient)
                {
                    double t = (numColorsToGenerate > 1) ? (double)i / (numColorsToGenerate - 1) : 0.5;
                    if (t < 0.5) rootColors[i] = Color.FromArgb((int)(139 * t / 0.5), 0, 0);
                    else { double t2 = (t - 0.5) / 0.5; rootColors[i] = Color.FromArgb((int)(139 + (255 - 139) * t2), (int)(215 * t2), 0); }
                }
                else if (usePastel)
                {
                    Color[] pastelColors = { Color.FromArgb(255, 182, 193), Color.FromArgb(173, 216, 230), Color.FromArgb(189, 252, 201), Color.FromArgb(255, 223, 186), Color.FromArgb(186, 255, 201) };
                    rootColors[i] = pastelColors[i % pastelColors.Length];
                }
                else if (useContrast)
                {
                    Color[] contrastColors = { Color.Red, Color.Yellow, Color.Blue, Color.Lime, Color.Magenta, Color.Cyan };
                    rootColors[i] = contrastColors[i % contrastColors.Length];
                }
                else if (useFire)
                {
                    Color[] fireColors = { Color.FromArgb(200, 0, 0), Color.FromArgb(255, 100, 0), Color.FromArgb(255, 200, 50), Color.FromArgb(255, 255, 150) };
                    rootColors[i] = fireColors[i % fireColors.Length];
                }
                else if (useContrasting)
                {
                    Color[] contrastingColors = { Color.FromArgb(10, 0, 20), Color.Purple, Color.Cyan, Color.FromArgb(20, 0, 120), Color.Fuchsia, Color.Aqua };
                    rootColors[i] = contrastingColors[i % contrastingColors.Length];
                }
                else if (useBlackWhite)
                {
                    rootColors[i] = Color.White;
                }
                else
                {
                    int shade = (numColorsToGenerate > 0) ? (255 * (i + 1) / (numColorsToGenerate + 1)) : 128;
                    rootColors[i] = Color.FromArgb(shade, shade, shade);
                }
            }

            Bitmap bmp = null;
            BitmapData bmpData = null;

            try
            {
                bmp = new Bitmap(currentWidth, currentHeight, PixelFormat.Format24bppRgb);
                token.ThrowIfCancellationRequested();

                Rectangle rect = new Rectangle(0, 0, currentWidth, currentHeight);
                bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
                token.ThrowIfCancellationRequested();

                int stride = bmpData.Stride;
                IntPtr scan0 = bmpData.Scan0;
                byte[] buffer = new byte[Math.Abs(stride) * currentHeight];

                ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = threadCount, CancellationToken = token };
                int done = 0;
                double epsilon = 1e-7;

                double scale_factor_w = (BASE_SCALE / renderZoom) / currentWidth;
                double scale_factor_h = (BASE_SCALE / renderZoom) / currentHeight;

                Polynomial pDeriv = p.Derivative();

                Parallel.For(0, currentHeight, po, y =>
                {
                    if (token.IsCancellationRequested) return;
                    int rowOffset = y * stride;
                    for (int x = 0; x < currentWidth; x++)
                    {
                        double c_re = renderCenterX + (x - currentWidth / 2.0) * scale_factor_w;
                        double c_im = renderCenterY + (currentHeight / 2.0 - y) * scale_factor_h;
                        Complex z = new Complex(c_re, c_im);

                        int iter = 0;
                        while (iter < maxIterations)
                        {
                            Complex pz = p.Evaluate(z);
                            if (pz.Magnitude < epsilon) break;
                            Complex pDz = pDeriv.Evaluate(z);
                            if (pDz.Magnitude < epsilon * 1e-3) break;
                            Complex step = pz / pDz;
                            if (double.IsNaN(step.Real) || double.IsNaN(step.Imaginary) || double.IsInfinity(step.Real) || double.IsInfinity(step.Imaginary)) break;
                            z = z - step;
                            iter++;
                        }

                        int rootIndex = -1;
                        if (roots.Count > 0)
                        {
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
                        }

                        Color pixelColor;
                        if (rootIndex != -1 && roots.Count > 0 && (z - roots[rootIndex]).Magnitude < epsilon * 50)
                        {
                            if (useGradient && !useBlackWhite)
                            {
                                double t = Math.Min(1.0, (double)iter / Math.Max(1, maxIterations * 0.75));
                                int baseHue = (int)(((double)rootIndex / Math.Max(1, roots.Count)) * 330) % 360;
                                int finalHue = (baseHue + (int)(t * 30)) % 360;
                                double saturation = 0.7 + 0.3 * Math.Sin(t * Math.PI);
                                double value = 0.6 + 0.4 * Math.Cos(t * Math.PI * 0.5);
                                pixelColor = HsvToRgb(finalHue, saturation, value);
                            }
                            else
                            {
                                pixelColor = rootColors[rootIndex % rootColors.Length];
                                if (!useBlackWhite)
                                {
                                    double factor = Math.Max(0.2, 1.0 - 0.9 * Math.Sqrt(Math.Min(1.0, (double)iter / maxIterations)));
                                    pixelColor = Color.FromArgb(
                                        pixelColor.A,
                                        (int)(pixelColor.R * factor),
                                        (int)(pixelColor.G * factor),
                                        (int)(pixelColor.B * factor)
                                    );
                                }
                            }
                        }
                        else
                        {
                            double darkness = Math.Min(1.0, (double)iter / maxIterations);
                            int darkShade = (int)(30 * (1.0 - darkness));
                            pixelColor = (usePastel && !useBlackWhite) ? Color.FromArgb(darkShade, darkShade, darkShade + 10) : Color.Black;
                        }

                        int bufferIndex = rowOffset + x * 3;
                        buffer[bufferIndex] = pixelColor.B;
                        buffer[bufferIndex + 1] = pixelColor.G;
                        buffer[bufferIndex + 2] = pixelColor.R;
                    }

                    int progress = Interlocked.Increment(ref done);
                    if (!token.IsCancellationRequested && progressBar.IsHandleCreated && !progressBar.IsDisposed)
                    {
                        progressBar.BeginInvoke((Action)(() =>
                        {
                            if (progressBar.Maximum > 0 && currentHeight > 0)
                                progressBar.Value = Math.Min(progressBar.Maximum, (int)(100.0 * progress / currentHeight));
                        }));
                    }
                });

                token.ThrowIfCancellationRequested();
                Marshal.Copy(buffer, 0, scan0, buffer.Length);
            }
            finally
            {
                if (bmpData != null && bmp != null)
                {
                    bmp.UnlockBits(bmpData);
                }

                if (token.IsCancellationRequested)
                {
                    bmp?.Dispose();
                }
                else if (bmp != null)
                {
                    if (fractal_bitmap.IsHandleCreated && !fractal_bitmap.IsDisposed)
                    {
                        Bitmap oldImage = null;
                        fractal_bitmap.Invoke((Action)(() =>
                        {
                            oldImage = fractal_bitmap.Image as Bitmap;
                            fractal_bitmap.Image = bmp;
                            renderedCenterX = renderCenterX;
                            renderedCenterY = renderCenterY;
                            renderedZoom = renderZoom;
                        }));
                        oldImage?.Dispose();
                    }
                    else
                    {
                        bmp.Dispose();
                    }
                }
                if (progressBar.IsHandleCreated && !progressBar.IsDisposed)
                {
                    progressBar.BeginInvoke((Action)(() => { if (progressBar.Value != 0 && progressBar.Value != 100) progressBar.Value = 0; }));
                }
            }
        }

        private Color HsvToRgb(double hue, double saturation, double value)
        {
            hue = hue % 360;
            if (hue < 0) hue += 360;

            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = Math.Max(0, Math.Min(1, value));
            saturation = Math.Max(0, Math.Min(1, saturation));

            int v = Convert.ToInt32(value * 255);
            int p = Convert.ToInt32(value * (1 - saturation) * 255);
            int q = Convert.ToInt32(value * (1 - f * saturation) * 255);
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation) * 255);

            if (hi == 0) return Color.FromArgb(255, v, t, p);
            else if (hi == 1) return Color.FromArgb(255, q, v, p);
            else if (hi == 2) return Color.FromArgb(255, p, v, t);
            else if (hi == 3) return Color.FromArgb(255, p, q, v);
            else if (hi == 4) return Color.FromArgb(255, t, p, v);
            else return Color.FromArgb(255, v, p, q);
        }

        private Bitmap RenderFractalToBitmap(int renderWidth, int renderHeight, double currentCenterX, double currentCenterY,
                                            double currentZoom, int currentMaxIterations,
                                            int numThreads, Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0) return new Bitmap(1, 1);

            string polyStr = string.Empty;
            bool useBlackWhite = false;
            bool useGradient = false;
            bool usePastel = false;
            bool useContrast = false;
            bool useFire = false;
            bool useContrasting = false;

            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() =>
                {
                    polyStr = richTextBox1.Text;
                    useBlackWhite = oldRenderBW.Checked;
                    useGradient = colorBox0.Checked;
                    usePastel = colorBox1.Checked;
                    useContrast = colorBox2.Checked;
                    useFire = colorBox3.Checked;
                    useContrasting = colorBox4.Checked;
                }));
            }
            else
            {
                polyStr = richTextBox1.Text;
                useBlackWhite = oldRenderBW.Checked;
                useGradient = colorBox0.Checked;
                usePastel = colorBox1.Checked;
                useContrast = colorBox2.Checked;
                useFire = colorBox3.Checked;
                useContrasting = colorBox4.Checked;
            }

            Polynomial p;
            try
            {
                p = PolynomialParser.ParsePolynomialWithMathNet(polyStr);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Ошибка парсинга в RenderFractalToBitmap: {ex.Message}");
                throw;
            }

            List<Complex> roots = FindRoots(p);
            if (roots.Count == 0 && p.coefficients.Any(c => c != Complex.Zero))
            {
                Console.Error.WriteLine("Предупреждение: не удалось найти корни в RenderFractalToBitmap для не нулевого полинома.");
            }

            Color[] rootColors = new Color[Math.Max(1, roots.Count)];
            int numColorsToGenerate = roots.Count > 0 ? roots.Count : 1;

            for (int i = 0; i < numColorsToGenerate; i++)
            {
                if (useGradient)
                {
                    double t = (numColorsToGenerate > 1) ? (double)i / (numColorsToGenerate - 1) : 0.5;
                    if (t < 0.5) rootColors[i] = Color.FromArgb((int)(139 * t / 0.5), 0, 0);
                    else { double t2 = (t - 0.5) / 0.5; rootColors[i] = Color.FromArgb((int)(139 + (255 - 139) * t2), (int)(215 * t2), 0); }
                }
                else if (usePastel)
                {
                    Color[] pastelColors = { Color.FromArgb(255, 182, 193), Color.FromArgb(173, 216, 230), Color.FromArgb(189, 252, 201), Color.FromArgb(255, 223, 186), Color.FromArgb(186, 255, 201) };
                    rootColors[i] = pastelColors[i % pastelColors.Length];
                }
                else if (useContrast)
                {
                    Color[] contrastColors = { Color.Red, Color.Yellow, Color.Blue, Color.Lime, Color.Magenta, Color.Cyan };
                    rootColors[i] = contrastColors[i % contrastColors.Length];
                }
                else if (useFire)
                {
                    Color[] fireColors = { Color.FromArgb(200, 0, 0), Color.FromArgb(255, 100, 0), Color.FromArgb(255, 200, 50), Color.FromArgb(255, 255, 150) };
                    rootColors[i] = fireColors[i % fireColors.Length];
                }
                else if (useContrasting)
                {
                    Color[] contrastingColors = { Color.FromArgb(10, 0, 20), Color.Purple, Color.Cyan, Color.FromArgb(20, 0, 120), Color.Fuchsia, Color.Aqua };
                    rootColors[i] = contrastingColors[i % contrastingColors.Length];
                }
                else if (useBlackWhite)
                {
                    rootColors[i] = Color.White;
                }
                else
                {
                    int shade = (numColorsToGenerate > 0) ? (255 * (i + 1) / (numColorsToGenerate + 1)) : 128;
                    rootColors[i] = Color.FromArgb(shade, shade, shade);
                }
            }

            Bitmap bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            byte[] buffer = new byte[Math.Abs(stride) * renderHeight];

            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;
            double epsilon = 1e-7;

            double scale_factor_w = (BASE_SCALE / currentZoom) / renderWidth;
            double scale_factor_h = (BASE_SCALE / currentZoom) / renderHeight;
            Polynomial pDeriv = p.Derivative();

            Parallel.For(0, renderHeight, po, y =>
            {
                int rowOffset = y * stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    double c_re = currentCenterX + (x - renderWidth / 2.0) * scale_factor_w;
                    double c_im = currentCenterY + (renderHeight / 2.0 - y) * scale_factor_h;
                    Complex z = new Complex(c_re, c_im);

                    int iter = 0;
                    while (iter < currentMaxIterations)
                    {
                        Complex pz = p.Evaluate(z);
                        if (pz.Magnitude < epsilon) break;
                        Complex pDz = pDeriv.Evaluate(z);
                        if (pDz.Magnitude < epsilon * 1e-3) break;
                        Complex step = pz / pDz;
                        if (double.IsNaN(step.Real) || double.IsNaN(step.Imaginary) || double.IsInfinity(step.Real) || double.IsInfinity(step.Imaginary)) break;
                        z = z - step;
                        iter++;
                    }

                    int rootIndex = -1;
                    if (roots.Count > 0)
                    {
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
                    }

                    Color pixelColor;
                    if (rootIndex != -1 && roots.Count > 0 && (z - roots[rootIndex]).Magnitude < epsilon * 50)
                    {
                        if (useGradient && !useBlackWhite)
                        {
                            double t = Math.Min(1.0, (double)iter / Math.Max(1, maxIterations * 0.75));
                            int baseHue = (int)(((double)rootIndex / Math.Max(1, roots.Count)) * 330) % 360;
                            int finalHue = (baseHue + (int)(t * 30)) % 360;
                            double saturation = 0.7 + 0.3 * Math.Sin(t * Math.PI);
                            double value = 0.6 + 0.4 * Math.Cos(t * Math.PI * 0.5);
                            pixelColor = HsvToRgb(finalHue, saturation, value);
                        }
                        else
                        {
                            pixelColor = rootColors[rootIndex % rootColors.Length];
                            if (!useBlackWhite)
                            {
                                double factor = Math.Max(0.2, 1.0 - 0.9 * Math.Sqrt(Math.Min(1.0, (double)iter / maxIterations)));
                                pixelColor = Color.FromArgb(pixelColor.A, (int)(pixelColor.R * factor), (int)(pixelColor.G * factor), (int)(pixelColor.B * factor));
                            }
                        }
                    }
                    else
                    {
                        double darkness = Math.Min(1.0, (double)iter / maxIterations);
                        int darkShade = (int)(30 * (1.0 - darkness));
                        pixelColor = (usePastel && !useBlackWhite) ? Color.FromArgb(darkShade, darkShade, darkShade + 10) : Color.Black;
                    }

                    int bufferIndex = rowOffset + x * 3;
                    buffer[bufferIndex] = pixelColor.B;
                    buffer[bufferIndex + 1] = pixelColor.G;
                    buffer[bufferIndex + 2] = pixelColor.R;
                }
                long currentDone = Interlocked.Increment(ref done);
                if (renderHeight > 0) reportProgressCallback((int)(100.0 * currentDone / renderHeight));
            });

            Marshal.Copy(buffer, 0, scan0, buffer.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private List<Complex> FindRoots(Polynomial p, int maxIter = 150, double epsilon = 1e-7)
        {
            List<Complex> roots = new List<Complex>();
            if (p == null || p.coefficients == null || p.coefficients.Count == 0) return roots;

            Polynomial pDeriv = p.Derivative();
            if (p.coefficients.Count == 1)
            {
                if (p.coefficients[0] == Complex.Zero) { }
                return roots;
            }
            if (pDeriv.coefficients.Count == 1 && pDeriv.coefficients[0] == Complex.Zero && p.coefficients.Count > 1)
            {
                Console.WriteLine("Предупреждение: Производная равна нулю для неконстантного полинома. Метод Ньютона не применим.");
                return roots;
            }

            double[] reValuesBase = { 0.0, 1.0, -1.0, 0.5, -0.5, 0.1, -0.1, 2.0, -2.0, 1.5, -1.5 };
            double[] imValuesBase = { 0.0, 1.0, -1.0, 0.5, -0.5, 0.1, -0.1, 2.0, -2.0, 1.5, -1.5 };

            List<double> reList = new List<double>(reValuesBase);
            List<double> imList = new List<double>(imValuesBase);

            int numPolyCoeffs = p.coefficients.Count;
            int pointsOnCircle = Math.Max(6, numPolyCoeffs * 2 + 2);

            if (pointsOnCircle > 0)
            {
                double[] radii = { 0.7, 1.0, 1.3, 0.4, 1.6 };
                if (numPolyCoeffs > 5) radii = radii.Concat(new[] { 0.2, 2.0 }).ToArray();

                for (int r_idx = 0; r_idx < radii.Length; r_idx++)
                {
                    for (int k = 0; k < pointsOnCircle / radii.Length + 1; k++)
                    {
                        double angle = 2 * Math.PI * k / (pointsOnCircle / radii.Length + 1);
                        reList.Add(Math.Cos(angle) * radii[r_idx]);
                        imList.Add(Math.Sin(angle) * radii[r_idx]);
                    }
                }
            }

            double[] reValues = reList.Distinct().ToArray();
            double[] imValues = imList.Distinct().ToArray();

            object lockObj = new object();

            Parallel.ForEach(reValues, new ParallelOptions { MaxDegreeOfParallelism = threadCount > 0 ? threadCount : -1 }, re_init =>
            {
                foreach (double im_init in imValues)
                {
                    Complex z = new Complex(re_init, im_init);
                    for (int i = 0; i < maxIter; i++)
                    {
                        Complex pz = p.Evaluate(z);
                        if (pz.Magnitude < epsilon)
                        {
                            lock (lockObj) AddRootIfNew(roots, z, epsilon, p);
                            break;
                        }

                        Complex pDz = pDeriv.Evaluate(z);

                        if (pDz.Magnitude < epsilon * 1e-5)
                        {
                            break;
                        }

                        Complex step = pz / pDz;
                        if (double.IsNaN(step.Real) || double.IsNaN(step.Imaginary) ||
                            double.IsInfinity(step.Real) || double.IsInfinity(step.Imaginary))
                        {
                            break;
                        }

                        Complex zNext = z - step;

                        if ((zNext - z).Magnitude < epsilon * 1e-2)
                        {
                            if (p.Evaluate(zNext).Magnitude < epsilon * 100)
                            {
                                lock (lockObj) AddRootIfNew(roots, zNext, epsilon, p);
                            }
                            break;
                        }
                        z = zNext;
                        if (z.Magnitude > 1e5) break;
                    }
                }
            });

            if (roots.Count > 0)
            {
                roots.Sort((r1, r2) =>
                {
                    double realDiff = r1.Real - r2.Real;
                    if (Math.Abs(realDiff) < epsilon * 10)
                    {
                        return r1.Imaginary.CompareTo(r2.Imaginary);
                    }
                    return realDiff.CompareTo(0.0);
                });

                List<Complex> distinctRoots = new List<Complex>();
                if (roots.Count > 0)
                {
                    distinctRoots.Add(roots[0]);
                    for (int i = 1; i < roots.Count; i++)
                    {
                        bool tooClose = false;
                        foreach (var dr in distinctRoots)
                        {
                            if ((roots[i] - dr).Magnitude < epsilon * 20)
                            {
                                tooClose = true;
                                if (p.Evaluate(roots[i]).Magnitude < p.Evaluate(dr).Magnitude)
                                {
                                    distinctRoots.Remove(dr);
                                    distinctRoots.Add(roots[i]);
                                }
                                break;
                            }
                        }
                        if (!tooClose)
                        {
                            distinctRoots.Add(roots[i]);
                        }
                    }
                }
                roots = distinctRoots;
            }
            return roots;
        }

        private void AddRootIfNew(List<Complex> roots, Complex newRoot, double epsilon, Polynomial p)
        {
            for (int i = 0; i < roots.Count; i++)
            {
                if ((newRoot - roots[i]).Magnitude < epsilon * 20)
                {
                    if (p.Evaluate(newRoot).Magnitude < p.Evaluate(roots[i]).Magnitude)
                    {
                        roots[i] = newRoot;
                    }
                    return;
                }
            }
            roots.Add(newRoot);
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || fractal_bitmap.Image == null || width <= 0 || height <= 0) return;

            double zoomFactor = e.Delta > 0 ? 1.25 : 1.0 / 1.25;
            double oldZoom = zoom;

            double mouseRe_before = centerX + (e.X - width / 2.0) * (BASE_SCALE / oldZoom / width);
            double mouseIm_before = centerY + (height / 2.0 - e.Y) * (BASE_SCALE / oldZoom / height);

            zoom = Math.Max((double)nudZoom.Minimum > 0 ? (double)nudZoom.Minimum : 1e-9, Math.Min((double)nudZoom.Maximum, zoom * zoomFactor));

            if (Math.Abs(nudZoom.Value - (decimal)zoom) > 1e-9m)
            {
                nudZoom.ValueChanged -= ParamControl_Changed;
                try { nudZoom.Value = Math.Max(nudZoom.Minimum, Math.Min(nudZoom.Maximum, (decimal)zoom)); }
                catch (ArgumentOutOfRangeException) { }
                nudZoom.ValueChanged += ParamControl_Changed;
            }

            centerX = mouseRe_before - (e.X - width / 2.0) * (BASE_SCALE / zoom / width);
            centerY = mouseIm_before - (height / 2.0 - e.Y) * (BASE_SCALE / zoom / height);

            fractal_bitmap.Invalidate();
            ScheduleRender();
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                panning = true;
                panStart = e.Location;
                fractal_bitmap.Cursor = Cursors.Hand;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || !panning || fractal_bitmap.Image == null || width <= 0 || height <= 0) return;

            double deltaX_pixels = e.X - panStart.X;
            double deltaY_pixels = e.Y - panStart.Y;

            centerX -= deltaX_pixels * (BASE_SCALE / zoom / width);
            centerY += deltaY_pixels * (BASE_SCALE / zoom / height);
            panStart = e.Location;

            fractal_bitmap.Invalidate();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left)
            {
                panning = false;
                fractal_bitmap.Cursor = Cursors.Default;
                ScheduleRender();
            }
        }

        private void btnRender_Click(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            previewRenderCts?.Cancel();
            renderTimer.Stop();
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
                        double currentZoomVal = this.zoom;
                        double currentCenterXVal = this.centerX;
                        double currentCenterYVal = this.centerY;
                        int currentThreadCount = this.threadCount;

                        Bitmap highResBitmap = await Task.Run(() => RenderFractalToBitmap(
                            saveWidth, saveHeight, currentCenterXVal, currentCenterYVal, currentZoomVal,
                            currentMaxIterations, currentThreadCount,
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
                        if (highResBitmap != null)
                        {
                            highResBitmap.Save(saveDialog.FileName, ImageFormat.Png);
                            highResBitmap.Dispose();
                            MessageBox.Show("Изображение успешно сохранено!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Не удалось создать изображение для сохранения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}\n\nПодробности: {ex.InnerException?.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        ScheduleRender();
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
                cbThreads.Enabled = enabled;
                nudZoom.Enabled = enabled;
                nudW.Enabled = enabled;
                nudH.Enabled = enabled;
                cbSelector.Enabled = enabled;
                richTextBox1.Enabled = enabled;
                oldRenderBW.Enabled = enabled;
                colorBox0.Enabled = enabled;
                colorBox1.Enabled = enabled;
                colorBox2.Enabled = enabled;
                colorBox3.Enabled = enabled;
                colorBox4.Enabled = enabled;
            };

            if (this.InvokeRequired) this.Invoke(action);
            else action();
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