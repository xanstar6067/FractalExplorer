using FractalExplorer.Parsers;
using FractalExplorer.Resources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FractalExplorer.Engines
{
    public class NewtonFractalEngine
    {
        // AST деревья
        private ExpressionNode f_ast;
        private ExpressionNode f_deriv_ast;

        // Параметры рендеринга
        public int MaxIterations { get; set; } = 100;
        public double CenterX { get; set; } = 0.0;
        public double CenterY { get; set; } = 0.0;
        public double Scale { get; set; } = 3.0; // Scale = BASE_SCALE / zoom

        // Параметры цвета
        public List<Complex> Roots { get; private set; } = new List<Complex>();
        public Color[] RootColors { get; set; } = Array.Empty<Color>();
        public Color BackgroundColor { get; set; } = Color.Black;
        public bool UseGradient { get; set; } = false;

        private const double epsilon = 1e-6;

        /// <summary>
        /// Парсит формулу, создает AST и находит корни.
        /// </summary>
        /// <returns>True если успешно, иначе false.</returns>
        public bool SetFormula(string expression, out string debugInfo)
        {
            var sb = new StringBuilder();
            try
            {
                var tokenizer = new Tokenizer(expression);
                var tokens = tokenizer.Tokenize();
                var parser = new Parser(tokens);
                f_ast = parser.Parse();
                f_deriv_ast = f_ast.Differentiate("z");

                sb.AppendLine("--- ИСХОДНАЯ ФУНКЦИЯ ---");
                sb.AppendLine($"f(z) = {f_ast}");
                sb.AppendLine("--- AST f(z) ---");
                sb.AppendLine(f_ast.Print());
                sb.AppendLine();
                sb.AppendLine("--- ПРОИЗВОДНАЯ ---");
                sb.AppendLine($"f'(z) = {f_deriv_ast}");
                sb.AppendLine("--- AST f'(z) ---");
                sb.AppendLine(f_deriv_ast.Print());

                debugInfo = sb.ToString();

                FindRootsInternal();
                return true;
            }
            catch (Exception ex)
            {
                f_ast = null;
                f_deriv_ast = null;
                Roots.Clear();
                debugInfo = $"ОШИБКА ПАРСИНГА:\n{ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Рендерит одну плитку в предоставленный байтовый буфер.
        /// </summary>
        public byte[] RenderSingleTile(TileInfo tile, int canvasWidth, int canvasHeight, out int bytesPerPixel)
        {
            bytesPerPixel = 4; // Используем 32bpp ARGB для прозрачности
            byte[] buffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];

            if (f_ast == null || f_deriv_ast == null || Roots.Count == 0)
            {
                // Если корней нет, возвращаем прозрачный буфер
                for (int i = 0; i < buffer.Length; i++) buffer[i] = 0;
                return buffer;
            }

            double half_width_pixels = canvasWidth / 2.0;
            double half_height_pixels = canvasHeight / 2.0;
            double units_per_pixel = Scale / canvasWidth;
            var variables = new Dictionary<string, Complex>();

            for (int y = 0; y < tile.Bounds.Height; y++)
            {
                int canvasY = tile.Bounds.Y + y;
                if (canvasY >= canvasHeight) continue;

                for (int x = 0; x < tile.Bounds.Width; x++)
                {
                    int canvasX = tile.Bounds.X + x;
                    if (canvasX >= canvasWidth) continue;

                    double c_re = CenterX + (canvasX - half_width_pixels) * units_per_pixel;
                    double c_im = CenterY + (canvasY - half_height_pixels) * units_per_pixel;

                    Complex z = new Complex(c_re, c_im);
                    int iter = 0;
                    while (iter < MaxIterations)
                    {
                        variables["z"] = z;
                        Complex f_val = f_ast.Evaluate(variables);
                        if (f_val.Magnitude < epsilon) break;

                        Complex f_deriv_val = f_deriv_ast.Evaluate(variables);
                        if (f_deriv_val == Complex.Zero) break;

                        z -= f_val / f_deriv_val;
                        iter++;
                    }

                    Color pixelColor = GetPixelColor(z, iter);

                    int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                    buffer[bufferIndex] = pixelColor.B;
                    buffer[bufferIndex + 1] = pixelColor.G;
                    buffer[bufferIndex + 2] = pixelColor.R;
                    buffer[bufferIndex + 3] = 255; // Alpha
                }
            }
            return buffer;
        }
        /* public void RenderTile(byte[] buffer, int stride, int bytesPerPixel, TileInfo tile, int canvasWidth, int canvasHeight)
         {
             if (f_ast == null || f_deriv_ast == null || Roots.Count == 0) return;

             double half_width_pixels = canvasWidth / 2.0;
             double half_height_pixels = canvasHeight / 2.0;
             double units_per_pixel = Scale / canvasWidth;
             var variables = new Dictionary<string, Complex>();

             for (int y = 0; y < tile.Bounds.Height; y++)
             {
                 int canvasY = tile.Bounds.Y + y;
                 if (canvasY >= canvasHeight) continue;

                 for (int x = 0; x < tile.Bounds.Width; x++)
                 {
                     int canvasX = tile.Bounds.X + x;
                     if (canvasX >= canvasWidth) continue;

                     double c_re = CenterX + (canvasX - half_width_pixels) * units_per_pixel;
                     double c_im = CenterY + (canvasY - half_height_pixels) * units_per_pixel; // Знак + т.к. экранные Y инвертированы

                     Complex z = new Complex(c_re, c_im);
                     int iter = 0;
                     while (iter < MaxIterations)
                     {
                         variables["z"] = z;
                         Complex f_val = f_ast.Evaluate(variables);
                         if (f_val.Magnitude < epsilon) break;

                         Complex f_deriv_val = f_deriv_ast.Evaluate(variables);
                         if (f_deriv_val == Complex.Zero) break;

                         z -= f_val / f_deriv_val;
                         iter++;
                     }

                     Color pixelColor = GetPixelColor(z, iter);

                     int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;
                     buffer[bufferIndex] = pixelColor.B;
                     buffer[bufferIndex + 1] = pixelColor.G;
                     buffer[bufferIndex + 2] = pixelColor.R;
                     buffer[bufferIndex + 3] = 255; // Alpha
                 }
             }
         }*/

        /// <summary>
        /// Рендерит полное изображение в Bitmap для сохранения.
        /// </summary>
        public Bitmap RenderToBitmap(int renderWidth, int renderHeight, int numThreads, Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0 || f_ast == null || f_deriv_ast == null)
            {
                return new Bitmap(1, 1);
            }
            if (Roots.Count == 0)
            {
                var emptyBmp = new Bitmap(renderWidth, renderHeight);
                using (var g = Graphics.FromImage(emptyBmp)) g.Clear(BackgroundColor);
                return emptyBmp;
            }

            var bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            byte[] buffer = new byte[Math.Abs(stride) * renderHeight];

            var po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;
            double units_per_pixel = Scale / renderWidth;

            Parallel.For(0, renderHeight, po, y =>
            {
                var variables = new Dictionary<string, Complex>();
                int rowOffset = y * stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    double c_re = CenterX + (x - renderWidth / 2.0) * units_per_pixel;
                    double c_im = CenterY + (y - renderHeight / 2.0) * units_per_pixel;
                    Complex z = new Complex(c_re, c_im);

                    int iter = 0;
                    while (iter < MaxIterations)
                    {
                        variables["z"] = z;
                        Complex pz = f_ast.Evaluate(variables);
                        if (pz.Magnitude < epsilon) break;
                        Complex pDz = f_deriv_ast.Evaluate(variables);
                        if (pDz == Complex.Zero) break;
                        z -= pz / pDz;
                        iter++;
                    }

                    Color pixelColor = GetPixelColor(z, iter);

                    int index = rowOffset + x * 3;
                    buffer[index] = pixelColor.B;
                    buffer[index + 1] = pixelColor.G;
                    buffer[index + 2] = pixelColor.R;
                }
                long currentDone = Interlocked.Increment(ref done);
                reportProgressCallback((int)(100.0 * currentDone / renderHeight));
            });

            Marshal.Copy(buffer, 0, scan0, buffer.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private Color GetPixelColor(Complex z, int iter)
        {
            int rootIndex = -1;
            double minDist = double.MaxValue;
            for (int r = 0; r < Roots.Count; r++)
            {
                double dist = (z - Roots[r]).Magnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    rootIndex = r;
                }
            }

            if (rootIndex != -1 && minDist < epsilon)
            {
                if (UseGradient)
                {
                    double t = (double)iter / MaxIterations;
                    int hue = (int)(240 * t);
                    return HsvToRgb(hue, 0.8, 1.0);
                }
                else
                {
                    return RootColors[rootIndex];
                }
            }
            return BackgroundColor;
        }

        private void FindRootsInternal(int maxIter = 100)
        {
            Roots.Clear();
            if (f_ast == null || f_deriv_ast == null) return;

            var startPoints = new List<Complex>();
            for (double r = 0.1; r < 2.5; r += 0.4)
            {
                for (int i = 0; i < 16; i++)
                {
                    double angle = 2 * Math.PI * i / 16.0;
                    startPoints.Add(Complex.FromPolarCoordinates(r, angle));
                }
            }
            startPoints.Add(Complex.Zero);

            foreach (var startPoint in startPoints)
            {
                Complex z = startPoint;
                var variables = new Dictionary<string, Complex>();

                for (int i = 0; i < maxIter; i++)
                {
                    variables["z"] = z;
                    Complex f_val = f_ast.Evaluate(variables);
                    Complex f_deriv_val = f_deriv_ast.Evaluate(variables);

                    if (f_deriv_val.Magnitude < epsilon / 100) break;

                    Complex step = f_val / f_deriv_val;
                    z -= step;

                    if (step.Magnitude < epsilon)
                    {
                        if (!Roots.Any(root => (z - root).Magnitude < epsilon))
                        {
                            variables["z"] = z;
                            if (f_ast.Evaluate(variables).Magnitude < epsilon * 10)
                            {
                                Roots.Add(z);
                            }
                        }
                        break;
                    }
                    if (z.Magnitude > 1e4) break;
                }
            }

            // Сортировка и удаление дубликатов
            Roots = Roots.OrderBy(r => r.Real).ThenBy(r => r.Imaginary).ToList();
            if (Roots.Count > 1)
            {
                var distinctRoots = new List<Complex> { Roots[0] };
                for (int i = 1; i < Roots.Count; i++)
                {
                    if ((Roots[i] - distinctRoots.Last()).Magnitude > epsilon)
                    {
                        distinctRoots.Add(Roots[i]);
                    }
                }
                Roots = distinctRoots;
            }
        }

        private Color HsvToRgb(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromArgb(v, t, p),
                1 => Color.FromArgb(q, v, p),
                2 => Color.FromArgb(p, v, t),
                3 => Color.FromArgb(p, q, v),
                4 => Color.FromArgb(t, p, v),
                _ => Color.FromArgb(v, p, q),
            };
        }
    }
}