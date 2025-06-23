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
    /// <summary>
    /// Движок для рендеринга фракталов Ньютона, основанных на комплексных функциях.
    /// Вычисляет точки, сходящиеся к корням заданной функции, и раскрашивает их.
    /// </summary>
    public class NewtonFractalEngine
    {
        #region Fields

        private ExpressionNode f_ast;
        private ExpressionNode f_deriv_ast;

        /// <summary>
        /// Максимальное количество итераций для метода Ньютона.
        /// </summary>
        public int MaxIterations { get; set; } = 100;

        /// <summary>
        /// Координата X центра видимой области фрактала.
        /// </summary>
        public double CenterX { get; set; } = 0.0;

        /// <summary>
        /// Координата Y центра видимой области фрактала.
        /// </summary>
        public double CenterY { get; set; } = 0.0;

        /// <summary>
        /// Текущий масштаб рендеринга.
        /// </summary>
        public double Scale { get; set; } = 3.0;

        /// <summary>
        /// Список найденных корней комплексной функции.
        /// </summary>
        public List<Complex> Roots { get; private set; } = new List<Complex>();

        /// <summary>
        /// Массив цветов, используемых для раскраски областей притяжения корней.
        /// </summary>
        public Color[] RootColors { get; set; } = Array.Empty<Color>();

        /// <summary>
        /// Цвет фона, используемый для точек, не сходящихся к корням.
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.Black;

        /// <summary>
        /// Флаг, указывающий, использовать ли градиентную раскраску в зависимости от количества итераций.
        /// </summary>
        public bool UseGradient { get; set; } = false;

        /// <summary>
        /// Малое значение для сравнения с нулем или для определения сходимости.
        /// </summary>
        private const double epsilon = 1e-6;

        #endregion

        #region Public Methods

        /// <summary>
        /// Устанавливает комплексную функцию для вычисления фрактала Ньютона.
        /// Парсит строковое выражение, строит AST для функции и ее производной,
        /// а также пытается найти корни функции.
        /// </summary>
        /// <param name="expression">Строковое представление комплексной функции, например, "z^3 - 1".</param>
        /// <param name="debugInfo">Выходной параметр, содержащий отладочную информацию о парсинге и производной.</param>
        /// <returns>True, если функция успешно установлена и пропарсена; иначе False.</returns>
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

                // Отладочная информация
                sb.AppendLine("Исходная функция: f(z) = " + f_ast.ToString());
                sb.AppendLine("AST f(z): " + f_ast.Print());
                sb.AppendLine("Производная: f'(z) = " + f_deriv_ast.ToString());
                sb.AppendLine("AST f'(z): " + f_deriv_ast.Print());

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
        /// Отрисовывает одну плитку (тайл) в ее собственный байтовый массив.
        /// Метод Ньютона применяется для каждой точки пикселя, чтобы определить, к какому корню она сходится.
        /// </summary>
        /// <param name="tile">Информация о плитке для отрисовки.</param>
        /// <param name="canvasWidth">Общая ширина холста.</param>
        /// <param name="canvasHeight">Общая высота холста.</param>
        /// <param name="bytesPerPixel">Выходной параметр, возвращающий количество байтов на пиксель в созданном буфере.</param>
        /// <returns>Массив байт с данными пикселей плитки.</returns>
        public byte[] RenderSingleTile(TileInfo tile, int canvasWidth, int canvasHeight, out int bytesPerPixel)
        {
            bytesPerPixel = 4; // Используем 4 байта на пиксель для поддержки формата 32bppArgb
            byte[] buffer = new byte[tile.Bounds.Width * tile.Bounds.Height * bytesPerPixel];

            // Если функция не задана или корни не найдены, возвращаем пустой (черный) буфер.
            if (f_ast == null || f_deriv_ast == null || Roots.Count == 0)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = 0;
                }
                return buffer;
            }

            double halfWidthPixels = canvasWidth / 2.0;
            double halfHeightPixels = canvasHeight / 2.0;
            double unitsPerPixel = Scale / canvasWidth;
            var variables = new Dictionary<string, Complex>();

            for (int y = 0; y < tile.Bounds.Height; y++)
            {
                int canvasY = tile.Bounds.Y + y;
                if (canvasY >= canvasHeight)
                {
                    continue;
                }

                for (int x = 0; x < tile.Bounds.Width; x++)
                {
                    int canvasX = tile.Bounds.X + x;
                    if (canvasX >= canvasWidth)
                    {
                        continue;
                    }

                    double complexReal = CenterX + (canvasX - halfWidthPixels) * unitsPerPixel;
                    double complexImaginary = CenterY + (canvasY - halfHeightPixels) * unitsPerPixel;
                    Complex z = new Complex(complexReal, complexImaginary);

                    int iter = 0;
                    while (iter < MaxIterations)
                    {
                        variables["z"] = z;
                        Complex fValue = f_ast.Evaluate(variables);
                        if (fValue.Magnitude < epsilon)
                        {
                            break;
                        }

                        Complex fDerivValue = f_deriv_ast.Evaluate(variables);
                        if (fDerivValue == Complex.Zero)
                        {
                            break;
                        }

                        z -= fValue / fDerivValue;
                        iter++;
                    }

                    Color pixelColor = GetPixelColor(z, iter);
                    int bufferIndex = (y * tile.Bounds.Width + x) * bytesPerPixel;

                    buffer[bufferIndex] = pixelColor.B;
                    buffer[bufferIndex + 1] = pixelColor.G;
                    buffer[bufferIndex + 2] = pixelColor.R;
                    buffer[bufferIndex + 3] = 255; // Alpha-канал, полностью непрозрачный
                }
            }
            return buffer;
        }

        /// <summary>
        /// Рендерит фрактал Ньютона в новый объект Bitmap, используя параллельные вычисления.
        /// </summary>
        /// <param name="renderWidth">Ширина генерируемого изображения в пикселях.</param>
        /// <param name="renderHeight">Высота генерируемого изображения в пикселях.</param>
        /// <param name="numThreads">Количество потоков для использования в параллельных вычислениях.</param>
        /// <param name="reportProgressCallback">Callback-функция для отчета о прогрессе рендеринга (значение от 0 до 100).</param>
        /// <returns>Объект Bitmap, содержащий отрисованный фрактал.</returns>
        public Bitmap RenderToBitmap(int renderWidth, int renderHeight, int numThreads, Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0 || f_ast == null || f_deriv_ast == null)
            {
                return new Bitmap(1, 1);
            }

            // Если корни не найдены, возвращаем пустой (залитый фоновым цветом) Bitmap.
            if (Roots.Count == 0)
            {
                var emptyBmp = new Bitmap(renderWidth, renderHeight);
                using (var g = Graphics.FromImage(emptyBmp))
                {
                    g.Clear(BackgroundColor);
                }
                return emptyBmp;
            }

            var bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            byte[] buffer = new byte[Math.Abs(stride) * renderHeight];

            var po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;

            double unitsPerPixel = Scale / renderWidth;

            Parallel.For(0, renderHeight, po, y =>
            {
                var variables = new Dictionary<string, Complex>();
                int rowOffset = y * stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    double complexReal = CenterX + (x - renderWidth / 2.0) * unitsPerPixel;
                    double complexImaginary = CenterY + (y - renderHeight / 2.0) * unitsPerPixel;
                    Complex z = new Complex(complexReal, complexImaginary);

                    int iter = 0;
                    while (iter < MaxIterations)
                    {
                        variables["z"] = z;
                        Complex fValue = f_ast.Evaluate(variables);
                        if (fValue.Magnitude < epsilon)
                        {
                            break;
                        }

                        Complex fDerivValue = f_deriv_ast.Evaluate(variables);
                        if (fDerivValue == Complex.Zero)
                        {
                            break;
                        }

                        z -= fValue / fDerivValue;
                        iter++;
                    }

                    Color pixelColor = GetPixelColor(z, iter);
                    int index = rowOffset + x * 3; // Предполагаем 3 байта на пиксель (24bpp)

                    buffer[index] = pixelColor.B;
                    buffer[index + 1] = pixelColor.G;
                    buffer[index + 2] = pixelColor.R;
                }

                long currentDone = Interlocked.Increment(ref done);
                // Отчет о прогрессе
                reportProgressCallback((int)(100.0 * currentDone / renderHeight));
            });

            Marshal.Copy(buffer, 0, scan0, buffer.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        #endregion

        #region Private / Helper Methods

        /// <summary>
        /// Определяет цвет пикселя на основе сходимости точки к корню и количества итераций.
        /// </summary>
        /// <param name="z">Конечная комплексная точка после итераций метода Ньютона.</param>
        /// <param name="iter">Количество итераций, выполненных до сходимости или достижения максимума.</param>
        /// <returns>Цвет пикселя.</returns>
        private Color GetPixelColor(Complex z, int iter)
        {
            // Если нет корней или нет цветов для них, просто возвращаем цвет фона.
            if (Roots.Count == 0 || RootColors.Length == 0)
            {
                return BackgroundColor;
            }

            int rootIndex = -1;
            double minDist = double.MaxValue;

            // Находим ближайший корень
            for (int r = 0; r < Roots.Count; r++)
            {
                double dist = (z - Roots[r]).Magnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    rootIndex = r;
                }
            }

            // Если точка сошлась к корню (достаточно близко)
            if (rootIndex != -1 && minDist < epsilon)
            {
                Color baseColor = RootColors[rootIndex % RootColors.Length];
                if (UseGradient)
                {
                    // Создаем градиент на основе количества итераций
                    double t = Math.Min(1.0, (double)iter / MaxIterations);
                    t = 1.0 - Math.Pow(1.0 - t, 2); // Нелинейный градиент
                    return LerpColor(baseColor, BackgroundColor, t);
                }
                else
                {
                    return baseColor;
                }
            }

            // Точка не сошлась к корню или слишком далека от любого корня
            return BackgroundColor;
        }

        /// <summary>
        /// Выполняет линейную интерполяцию между двумя цветами.
        /// </summary>
        /// <param name="a">Начальный цвет.</param>
        /// <param name="b">Конечный цвет.</param>
        /// <param name="t">Коэффициент интерполяции (от 0 до 1).</param>
        /// <returns>Интерполированный цвет.</returns>
        private Color LerpColor(Color a, Color b, double t)
        {
            t = Math.Max(0, Math.Min(1, t)); // Ограничиваем t в пределах [0, 1]
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t)
            );
        }

        /// <summary>
        /// Внутренний метод для поиска корней заданной комплексной функции
        /// методом Ньютона, начиная с различных начальных точек.
        /// </summary>
        /// <param name="maxIter">Максимальное количество итераций для поиска каждого корня.</param>
        private void FindRootsInternal(int maxIter = 100)
        {
            Roots.Clear();
            if (f_ast == null || f_deriv_ast == null)
            {
                return;
            }

            // Генерируем набор начальных точек для поиска корней
            var startPoints = new List<Complex>();
            for (double r = 0.1; r < 2.5; r += 0.4)
            {
                for (int i = 0; i < 16; i++)
                {
                    double angle = 2 * Math.PI * i / 16.0;
                    startPoints.Add(Complex.FromPolarCoordinates(r, angle));
                }
            }
            startPoints.Add(Complex.Zero); // Добавляем начало координат

            foreach (var startPoint in startPoints)
            {
                Complex z = startPoint;
                var variables = new Dictionary<string, Complex>();

                for (int i = 0; i < maxIter; i++)
                {
                    variables["z"] = z;
                    Complex fValue = f_ast.Evaluate(variables);
                    Complex fDerivValue = f_deriv_ast.Evaluate(variables);

                    // Избегаем деления на ноль, если производная очень мала
                    if (fDerivValue.Magnitude < epsilon / 100)
                    {
                        break;
                    }

                    Complex step = fValue / fDerivValue;
                    z -= step;

                    // Если шаг стал очень мал, считаем, что сошлись
                    if (step.Magnitude < epsilon)
                    {
                        // Проверяем, не найден ли этот корень ранее
                        if (!Roots.Any(root => (z - root).Magnitude < epsilon))
                        {
                            // Дополнительная проверка: действительно ли это корень (f(z) близко к нулю)
                            variables["z"] = z;
                            if (f_ast.Evaluate(variables).Magnitude < epsilon * 10)
                            {
                                Roots.Add(z);
                            }
                        }
                        break;
                    }

                    // Если точка ушла слишком далеко, считаем, что не сойдется
                    if (z.Magnitude > 1e4)
                    {
                        break;
                    }
                }
            }

            // Сортируем и удаляем дубликаты корней
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

        /// <summary>
        /// Преобразует цвет из цветового пространства HSV (оттенок, насыщенность, яркость) в RGB.
        /// </summary>
        /// <param name="hue">Оттенок (0-360).</param>
        /// <param name="saturation">Насыщенность (0-1).</param>
        /// <param name="value">Яркость (0-1).</param>
        /// <returns>Цвет в формате RGB.</returns>
        private Color HsvToRgb(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);
            value = value * 255; // Переводим значение в диапазон 0-255

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

        #endregion
    }
}