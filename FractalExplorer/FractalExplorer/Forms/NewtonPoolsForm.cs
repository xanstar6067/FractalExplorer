using System.Text;
using System.Numerics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Globalization;

namespace FractalExplorer
{
    public partial class NewtonPools : Form
    {
        // --- Системные и UI компоненты ---
        private readonly System.Windows.Forms.Timer renderTimer;
        private int maxIterations;
        // private int threadCount; // <-- УБРАНО: Больше не нужно хранить, будем получать по запросу
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

        // --- Новые поля для хранения AST ---
        private ExpressionNode f_ast = null; // AST для f(z)
        private ExpressionNode f_deriv_ast = null; // AST для f'(z)

        // --- Новые поля для кастомных цветов ---
        private color_setting_NewtonPoolsForm colorSettingsForm;
        private List<Color> userDefinedRootColors = new List<Color>();
        private Color userDefinedBackgroundColor = Color.Black;
        private bool useCustomPalette = false;

        private readonly string[] presetPolynomials = {
            // --- Классические полиномы ---
            "z^3-1",
            "z^4-1",
            "z^5-1",
            "z^6-1",
            "z^3-2*z+2",
            "z^5 - z^2 + 1",
            "z^6 + 3*z^3 - 2",
            "z^4 - 4*z^2 + 4", // (z^2-2)^2
            "z^7 + z^4 - z + 1",
            "z^8 + 15*z^4 - 16", // (z^4+16)(z^4-1)
            "z^4 + z^3 + z^2 + z + 1",

            // --- С комплексными и дробными коэффициентами ---
            "z^2 - i",
            "(z^2-1)*(z-2*i)",
            "(1+2*i)*z^2+z-1",
            "(0.5-0.3*i)*z^3+2",
            "0.5*z^3 - 1.25*z + 2",
            "(2+i)*z^3 - (1-2*i)*z + 1",
            "i*z^4 + z - 1",
            "(1+0.5*i)*z^2 - z + (2-3*i)",
            "(0.3+1.7*i)*z^3 + (1-i)",
            "(2-i)*z^5 + (3+2*i)*z^2 - 1",
            "-2*z^3 + 0.75*z^2 - 1",
            "z^6 - 1.5*z^3 + 0.25",
            "-0.1*z^4 + z - 2",
            "(1/2)*z^3 + (3/4)*z - 1",
            "(2+3*i)*(z^2) - (1-i)*z + 4",

            // --- Дробно-рациональные функции (для теста деления) ---
            "(z^2-1)/(z^2+1)",
            "(z^3-1)/(z^3+1)",
            "z^2 / (z-1)^2",
            "(z^4-1)/(z*z-2*z+1)" // (z^4-1)/(z-1)^2
        };

        public NewtonPools()
        {
            InitializeComponent();

            renderTimer = new System.Windows.Forms.Timer { Interval = 300 };
            InitializeForm();
        }

        private void InitializeForm()
        {
            width = fractal_bitmap.Width;
            height = fractal_bitmap.Height;

            renderTimer.Tick += RenderTimer_Tick;

            // Заполняем ComboBox предустановленными формулами
            foreach (string poly in presetPolynomials)
            {
                cbSelector.Items.Add(poly);
            }
            cbSelector.SelectedIndex = 0;
            richTextInput.Text = cbSelector.SelectedItem.ToString(); // Устанавливаем текст при запуске

            int cores = Environment.ProcessorCount;
            for (int i = 1; i <= cores; i++)
            {
                cbThreads.Items.Add(i);
            }
            cbThreads.Items.Add("Auto");
            cbThreads.SelectedItem = "Auto";

            // Подписки на события
            nudIterations.ValueChanged += ParamControl_Changed;
            cbThreads.SelectedIndexChanged += ParamControl_Changed;
            nudZoom.ValueChanged += ParamControl_Changed;
            cbSelector.SelectedIndexChanged += cbSelector_SelectedIndexChanged;
            //richTextBox1.TextChanged += (s, e) => ScheduleRender(); // Рендер при изменении текста
            oldRenderBW.CheckedChanged += ParamControl_Changed;
            colorBox0.CheckedChanged += ColorBox_Changed;
            colorBox1.CheckedChanged += ColorBox_Changed;
            colorBox2.CheckedChanged += ColorBox_Changed;
            colorBox3.CheckedChanged += ColorBox_Changed;
            colorBox4.CheckedChanged += ColorBox_Changed;
            colorCustom.CheckedChanged += ColorBox_Changed; // Новая подписка
            custom_color.Click += custom_color_Click; // Новая подписка

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

        #region Основная логика (Парсинг и Рендеринг)

        private bool ProcessFormula()
        {
            string expression = richTextInput.Text;
            if (string.IsNullOrWhiteSpace(expression))
            {
                richTextDebugOutput.Text = "Ошибка: Поле для формулы пустое.";
                return false;
            }

            try
            {
                // 1. Токенизация
                var tokenizer = new Tokenizer(expression);
                var tokens = tokenizer.Tokenize();

                // 2. Парсинг и построение AST для f(z)
                var parser = new Parser(tokens);
                f_ast = parser.Parse();

                // 3. Символьное дифференцирование для получения f'(z)
                f_deriv_ast = f_ast.Differentiate("z");

                // 4. Вывод отладочной информации
                var sb = new StringBuilder();
                sb.AppendLine("--- ИСХОДНАЯ ФУНКЦИЯ ---");
                sb.AppendLine($"f(z) = {f_ast}");
                sb.AppendLine("--- AST f(z) ---");
                sb.AppendLine(f_ast.Print());
                sb.AppendLine();
                sb.AppendLine("--- ПРОИЗВОДНАЯ ---");
                sb.AppendLine($"f'(z) = {f_deriv_ast}");
                sb.AppendLine("--- AST f'(z) ---");
                sb.AppendLine(f_deriv_ast.Print());

                richTextDebugOutput.Text = sb.ToString();

                return true;
            }
            catch (Exception ex)
            {
                // Вывод ошибки парсинга
                f_ast = null;
                f_deriv_ast = null;
                richTextDebugOutput.Text = $"ОШИБКА ПАРСИНГА:\n{ex.Message}";
                return false;
            }
        }

        private void ScheduleRender()
        {
            if (isHighResRendering)
            {
                return;
            }
            previewRenderCts?.Cancel();
            renderTimer.Stop();
            renderTimer.Start();
        }

        private async void RenderTimer_Tick(object sender, EventArgs e)
        {
            renderTimer.Stop();
            if (isHighResRendering || isRenderingPreview)
            {
                return;
            }

            // Сначала обрабатываем формулу
            if (!ProcessFormula())
            {
                // Если парсинг не удался, очищаем изображение
                if (fractal_bitmap.Image != null)
                {
                    using (var g = Graphics.FromImage(fractal_bitmap.Image))
                    {
                        g.Clear(Color.Black);
                    }
                    fractal_bitmap.Invalidate();
                }
                return;
            }

            isRenderingPreview = true;
            previewRenderCts?.Dispose();
            previewRenderCts = new CancellationTokenSource();
            CancellationToken token = previewRenderCts.Token;

            UpdateParameters();
            double currentRenderCenterX = centerX;
            double currentRenderCenterY = centerY;
            double currentRenderZoom = zoom;

            try
            {
                await Task.Run(() => RenderFractal(token, currentRenderCenterX, currentRenderCenterY, currentRenderZoom), token);
            }
            catch (OperationCanceledException)
            {
                // Это ожидаемое исключение при отмене, игнорируем.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка рендеринга: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isRenderingPreview = false;
            }
        }

        private void RenderFractal(CancellationToken token, double renderCenterX, double renderCenterY, double renderZoom)
        {
            if (token.IsCancellationRequested || isHighResRendering || f_ast == null || f_deriv_ast == null || fractal_bitmap.Width <= 0 || fractal_bitmap.Height <= 0)
            {
                return;
            }

            // Находим корни, используя новые AST
            List<Complex> roots = FindRoots(f_ast, f_deriv_ast);
            if (roots.Count == 0)
            {
                // Если корни не найдены, просто выходим (экран останется черным).
                return;
            }

            // Логика выбора цветов
            Color[] rootColors = GetRootColors(roots);
            bool usePastel = this.Invoke(new Func<bool>(() => colorBox1.Checked));
            bool useGradient = this.Invoke(new Func<bool>(() => colorBox0.Checked));
            bool useUserPalette = this.Invoke(new Func<bool>(() => useCustomPalette));
            Color bgColor = useUserPalette ? userDefinedBackgroundColor : (usePastel ? CreateSafeColor(50, 50, 50) : Color.Black);


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

                // ИЗМЕНЕНИЕ: Получаем количество потоков через новый метод
                var po = new ParallelOptions { MaxDegreeOfParallelism = GetThreadCount(), CancellationToken = token };
                int done = 0;
                const double epsilon = 1e-6;
                double scale = (BASE_SCALE / renderZoom) / width;

                Parallel.For(0, height, po, y =>
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        double c_re = renderCenterX + (x - width / 2.0) * scale;
                        double c_im = renderCenterY + (y - height / 2.0) * scale;
                        Complex z = new Complex(c_re, c_im);
                        var variables = new Dictionary<string, Complex> { { "z", z } };
                        int iter = 0;
                        while (iter < maxIterations)
                        {
                            Complex f_val = f_ast.Evaluate(variables);
                            if (f_val.Magnitude < epsilon) break;

                            Complex f_deriv_val = f_deriv_ast.Evaluate(variables);
                            if (f_deriv_val == Complex.Zero) break;

                            z -= f_val / f_deriv_val;
                            variables["z"] = z;
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

                        // Определяем цвет пикселя
                        Color pixelColor;
                        if (rootIndex >= 0 && minDist < epsilon)
                        {
                            if (useGradient && !useUserPalette) // Градиент только если не выбрана своя палитра
                            {
                                double t = (double)iter / maxIterations;
                                int hue = (int)(240 * t);
                                pixelColor = HsvToRgb(hue, 0.8, 1.0);
                            }
                            else
                            {
                                // Используем либо цвет из палитры, либо кастомный цвет
                                pixelColor = rootColors[rootIndex];
                            }
                        }
                        else
                        {
                            pixelColor = bgColor;
                        }

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
                        if (token.IsCancellationRequested) { bmp?.Dispose(); return; }
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
                if (bmpData != null && bmp != null) { bmp.UnlockBits(bmpData); }
                bmp?.Dispose();
            }
        }

        private List<Complex> FindRoots(ExpressionNode func, ExpressionNode deriv, int maxIter = 100, double epsilon = 1e-6)
        {
            var roots = new List<Complex>();
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

                    Complex f_val = func.Evaluate(variables);
                    Complex f_deriv_val = deriv.Evaluate(variables);

                    if (f_deriv_val.Magnitude < epsilon / 100)
                    {
                        break;
                    }

                    Complex step = f_val / f_deriv_val;
                    z -= step;

                    if (step.Magnitude < epsilon)
                    {
                        // Проверяем, не является ли этот корень дубликатом
                        if (!roots.Any(root => (z - root).Magnitude < epsilon))
                        {
                            // Финальная проверка: действительно ли это корень?
                            variables["z"] = z;
                            if (func.Evaluate(variables).Magnitude < epsilon * 10)
                            {
                                roots.Add(z);
                            }
                        }
                        break;
                    }
                    if (z.Magnitude > 1e4) break;
                }
            }

            roots = roots.OrderBy(r => r.Real).ThenBy(r => r.Imaginary).ToList();
            if (roots.Count > 1)
            {
                var distinctRoots = new List<Complex> { roots[0] };
                for (int i = 1; i < roots.Count; i++)
                {
                    if ((roots[i] - distinctRoots.Last()).Magnitude > epsilon)
                    {
                        distinctRoots.Add(roots[i]);
                    }
                }
                return distinctRoots;
            }
            return roots;
        }

        #endregion

        #region Вспомогательные методы и UI обработчики

        private Color CreateSafeColor(int r, int g, int b)
        {
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));
            return Color.FromArgb(r, g, b);
        }

        private Color[] GetRootColors(List<Complex> roots)
        {
            bool useUserDefPalette = this.Invoke(new Func<bool>(() => useCustomPalette));
            if (useUserDefPalette)
            {
                var customColors = new Color[roots.Count];
                for (int i = 0; i < roots.Count; i++)
                {
                    customColors[i] = i < userDefinedRootColors.Count ? userDefinedRootColors[i] : Color.Gray;
                }
                return customColors;
            }

            var rootColors = new Color[roots.Count];
            bool useBlackWhite = this.Invoke(new Func<bool>(() => oldRenderBW.Checked));
            bool useGradient = this.Invoke(new Func<bool>(() => colorBox0.Checked));
            bool usePastel = this.Invoke(new Func<bool>(() => colorBox1.Checked));
            bool useContrast = this.Invoke(new Func<bool>(() => colorBox2.Checked));
            bool useFire = this.Invoke(new Func<bool>(() => colorBox3.Checked));
            bool useContrasting = this.Invoke(new Func<bool>(() => colorBox4.Checked));

            if (useGradient)
            {
                for (int i = 0; i < roots.Count; i++)
                {
                    double t = (roots.Count > 1) ? (double)i / (roots.Count - 1) : 1.0;
                    if (t < 0.5) { rootColors[i] = CreateSafeColor((int)(139 * t / 0.5), 0, 0); }
                    else { double t2 = (t - 0.5) / 0.5; rootColors[i] = CreateSafeColor((int)(139 + (255 - 139) * t2), (int)(215 * t2), 0); }
                }
            }
            else if (usePastel)
            {
                Color[] pastelColors = { Color.FromArgb(255, 182, 193), Color.FromArgb(173, 216, 230), Color.FromArgb(189, 252, 201), };
                for (int i = 0; i < roots.Count; i++) { rootColors[i] = i < pastelColors.Length ? pastelColors[i] : CreateSafeColor(200, 200, 200 + i * 10); }
            }
            else if (useContrast)
            {
                Color[] contrastColors = { Color.FromArgb(255, 0, 0), Color.FromArgb(255, 255, 0), Color.FromArgb(0, 0, 255), };
                for (int i = 0; i < roots.Count; i++) { rootColors[i] = i < contrastColors.Length ? contrastColors[i] : CreateSafeColor((i * 97) % 255, (i * 149) % 255, (i * 211) % 255); }
            }
            else if (useFire)
            {
                Color[] fireColors = { Color.FromArgb(200, 0, 0), Color.FromArgb(255, 100, 0), Color.FromArgb(255, 255, 100), };
                for (int i = 0; i < roots.Count; i++) { rootColors[i] = i < fireColors.Length ? fireColors[i] : CreateSafeColor((i * 97) % 255, (i * 149) % 255, (i * 211) % 255); }
            }
            else if (useContrasting)
            {
                Color[] contrastingColors = { Color.FromArgb(10, 0, 20), Color.FromArgb(255, 0, 255), Color.FromArgb(0, 255, 255), };
                for (int i = 0; i < roots.Count; i++) { rootColors[i] = i < contrastingColors.Length ? contrastingColors[i] : CreateSafeColor((i * 97) % 255, (i * 149) % 255, (i * 211) % 255); }
            }
            else if (useBlackWhite)
            {
                for (int i = 0; i < roots.Count; i++) { rootColors[i] = Color.White; }
            }
            else
            {
                for (int i = 0; i < roots.Count; i++) { int shade = 255 * (i + 1) / (roots.Count > 0 ? roots.Count + 1 : 1); rootColors[i] = CreateSafeColor(shade, shade, shade); }
            }

            return rootColors;
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
                0 => CreateSafeColor(v, t, p),
                1 => CreateSafeColor(q, v, p),
                2 => CreateSafeColor(p, v, t),
                3 => CreateSafeColor(p, q, v),
                4 => CreateSafeColor(t, p, v),
                _ => CreateSafeColor(v, p, q),
            };
        }

        private void cbSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbSelector.SelectedIndex >= 0)
            {
                richTextInput.Text = cbSelector.SelectedItem.ToString();
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
                if (nudZoom.Value != (decimal)zoom) { nudZoom.Value = (decimal)zoom; }
            }
            ScheduleRender();
        }

        private void ColorBox_Changed(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            if (sender is CheckBox currentCb && currentCb.Checked)
            {
                if (currentCb == colorCustom)
                {
                    useCustomPalette = true;
                    foreach (var cb in new[] { oldRenderBW, colorBox0, colorBox1, colorBox2, colorBox3, colorBox4 }) { cb.Checked = false; }
                }
                else
                {
                    useCustomPalette = false;
                    colorCustom.Checked = false;
                    foreach (var cb in new[] { colorBox0, colorBox1, colorBox2, colorBox3, colorBox4 }) { if (cb != currentCb) { cb.Checked = false; } }
                }
            }
            else if (sender == colorCustom && !colorCustom.Checked) { useCustomPalette = false; }
            ScheduleRender();
        }

        private void UpdateParameters()
        {
            maxIterations = (int)nudIterations.Value;
            useCustomPalette = colorCustom.Checked;
        }

        /// <summary>
        /// НОВЫЙ МЕТОД: централизованное получение количества потоков.
        /// </summary>
        private int GetThreadCount()
        {
            if (this.InvokeRequired)
            {
                return this.Invoke(new Func<int>(() =>
                {
                    if (cbThreads.SelectedItem == null) return Environment.ProcessorCount;
                    return cbThreads.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
                }));
            }
            else
            {
                if (cbThreads.SelectedItem == null) return Environment.ProcessorCount;
                return cbThreads.SelectedItem.ToString() == "Auto" ? Environment.ProcessorCount : Convert.ToInt32(cbThreads.SelectedItem);
            }
        }

        private void custom_color_Click(object sender, EventArgs e)
        {
            if (!ProcessFormula())
            {
                MessageBox.Show("Не удалось обработать формулу. Невозможно определить количество корней для настройки палитры.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var roots = FindRoots(f_ast, f_deriv_ast);
            int rootCount = roots.Count;

            if (colorSettingsForm == null || colorSettingsForm.IsDisposed)
            {
                colorSettingsForm = new color_setting_NewtonPoolsForm();
                colorSettingsForm.ColorsChanged += ColorSettingsForm_ColorsChanged;
            }

            colorSettingsForm.PopulateColorPickers(userDefinedRootColors, userDefinedBackgroundColor, rootCount);
            colorSettingsForm.Show();
            colorSettingsForm.Activate();
        }

        private void ColorSettingsForm_ColorsChanged(object sender, CustomPalette newPalette)
        {
            this.userDefinedRootColors = newPalette.RootColors;
            this.userDefinedBackgroundColor = newPalette.BackgroundColor;
            if (useCustomPalette) { ScheduleRender(); }
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (fractal_bitmap.Image == null || width <= 0 || height <= 0)
            {
                e.Graphics.Clear(Color.Black);
                return;
            }

            e.Graphics.Clear(Color.Black);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            double currentScale = (BASE_SCALE / zoom) / width;
            double renderedScale = (BASE_SCALE / renderedZoom) / width;
            float drawScaleRatio = (float)(renderedScale / currentScale);
            float newWidth = width * drawScaleRatio;
            float newHeight = height * drawScaleRatio;
            double deltaRe = renderedCenterX - centerX;
            double deltaIm = renderedCenterY - centerY;
            float offsetX = (float)(deltaRe / currentScale);
            float offsetY = (float)(deltaIm / currentScale);
            float drawX = (width - newWidth) / 2.0f + offsetX;
            float drawY = (height - newHeight) / 2.0f + offsetY;

            var destRect = new RectangleF(drawX, drawY, newWidth, newHeight);
            e.Graphics.DrawImage(fractal_bitmap.Image, destRect);
        }

        private void Canvas_MouseWheel(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            double zoomFactor = e.Delta > 0 ? 1.5 : 1.0 / 1.5;
            double oldZoom = zoom;
            double scaleBefore = (BASE_SCALE / oldZoom) / width;
            double mouseRe = centerX + (e.X - width / 2.0) * scaleBefore;
            double mouseIm = centerY + (e.Y - height / 2.0) * scaleBefore;
            zoom = Math.Max((double)nudZoom.Minimum, Math.Min((double)nudZoom.Maximum, zoom * zoomFactor));
            double scaleAfter = (BASE_SCALE / zoom) / width;
            centerX = mouseRe - (e.X - width / 2.0) * scaleAfter;
            centerY = mouseIm - (e.Y - height / 2.0) * scaleAfter;
            fractal_bitmap.Invalidate();
            if (nudZoom.Value != (decimal)zoom) { nudZoom.Value = (decimal)zoom; }
            else { ScheduleRender(); }
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left) { panning = true; panStart = e.Location; }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isHighResRendering || !panning) return;
            double scale = BASE_SCALE / zoom / width;
            centerX -= (e.X - panStart.X) * scale;
            // ИСПРАВЛЕНИЕ ЗНАКА ДЛЯ КОРРЕКТНОГО ПЕРЕМЕЩЕНИЯ
            centerY -= (e.Y - panStart.Y) * scale;
            panStart = e.Location;
            fractal_bitmap.Invalidate();
            ScheduleRender();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isHighResRendering) return;
            if (e.Button == MouseButtons.Left) { panning = false; }
        }

        private void btnRender_Click(object sender, EventArgs e)
        {
            if (isHighResRendering) return;
            ScheduleRender();
        }

        #endregion

        #region Сохранение изображения

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

            using var saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Сохранить фрактал Ньютона",
                FileName = suggestedFileName
            };

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
                    double currentZoom = this.zoom;
                    double currentCenterX = this.centerX;
                    double currentCenterY = this.centerY;

                    if (!ProcessFormula())
                    {
                        MessageBox.Show("Не удалось обработать формулу. Сохранение отменено.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    Bitmap highResBitmap = await Task.Run(() => RenderFractalToBitmap(
                        saveWidth, saveHeight, currentCenterX, currentCenterY, currentZoom,
                        this.maxIterations, GetThreadCount(), // <-- ИЗМЕНЕНИЕ: Используем GetThreadCount()
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

        private Bitmap RenderFractalToBitmap(int renderWidth, int renderHeight, double currentCenterX, double currentCenterY, double currentZoom, int currentMaxIterations, int numThreads, Action<int> reportProgressCallback)
        {
            if (renderWidth <= 0 || renderHeight <= 0 || f_ast == null || f_deriv_ast == null)
            {
                return new Bitmap(1, 1);
            }

            List<Complex> roots = FindRoots(f_ast, f_deriv_ast);
            if (roots.Count == 0)
            {
                return new Bitmap(renderWidth, renderHeight);
            }

            Color[] rootColors = GetRootColors(roots);
            bool usePastel = this.Invoke(new Func<bool>(() => colorBox1.Checked));
            bool useGradient = this.Invoke(new Func<bool>(() => colorBox0.Checked));
            bool useUserPalette = this.Invoke(new Func<bool>(() => useCustomPalette));
            Color bgColor = useUserPalette ? userDefinedBackgroundColor : (usePastel ? CreateSafeColor(50, 50, 50) : Color.Black);


            var bmp = new Bitmap(renderWidth, renderHeight, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, renderWidth, renderHeight), ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            byte[] buffer = new byte[Math.Abs(stride) * renderHeight];

            var po = new ParallelOptions { MaxDegreeOfParallelism = numThreads };
            long done = 0;
            const double epsilon = 1e-6;
            double scale = (BASE_SCALE / currentZoom) / renderWidth;

            Parallel.For(0, renderHeight, po, y =>
            {
                int rowOffset = y * stride;
                for (int x = 0; x < renderWidth; x++)
                {
                    double c_re = currentCenterX + (x - renderWidth / 2.0) * scale;
                    double c_im = currentCenterY + (y - renderHeight / 2.0) * scale;
                    Complex z = new Complex(c_re, c_im);
                    var variables = new Dictionary<string, Complex> { { "z", z } };
                    int iter = 0;
                    while (iter < currentMaxIterations)
                    {
                        Complex pz = f_ast.Evaluate(variables);
                        if (pz.Magnitude < epsilon) break;
                        Complex pDz = f_deriv_ast.Evaluate(variables);
                        if (pDz == Complex.Zero) break;
                        z -= pz / pDz;
                        variables["z"] = z;
                        iter++;
                    }

                    int rootIndex = -1;
                    double minDist = double.MaxValue;
                    for (int r = 0; r < roots.Count; r++)
                    {
                        double dist = (z - roots[r]).Magnitude;
                        if (dist < minDist) { minDist = dist; rootIndex = r; }
                    }

                    Color pixelColor;
                    if (rootIndex >= 0 && minDist < epsilon)
                    {
                        if (useGradient && !useUserPalette) { double t = (double)iter / currentMaxIterations; int hue = (int)(240 * t); pixelColor = HsvToRgb(hue, 0.8, 1.0); }
                        else { pixelColor = rootColors[rootIndex]; }
                    }
                    else { pixelColor = bgColor; }

                    int index = rowOffset + x * 3;
                    buffer[index] = pixelColor.B; buffer[index + 1] = pixelColor.G; buffer[index + 2] = pixelColor.R;
                }
                long currentDone = Interlocked.Increment(ref done);
                if (renderHeight > 0)
                {
                    reportProgressCallback((int)(100.0 * currentDone / renderHeight));
                }
            });

            Marshal.Copy(buffer, 0, scan0, buffer.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
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
                richTextInput.Enabled = enabled;
                oldRenderBW.Enabled = enabled;
                colorBox0.Enabled = enabled;
                colorBox1.Enabled = enabled;
                colorBox2.Enabled = enabled;
                colorBox3.Enabled = enabled;
                colorBox4.Enabled = enabled;
                colorCustom.Enabled = enabled;
                custom_color.Enabled = enabled;
            };

            if (this.InvokeRequired) { this.Invoke(action); } else { action(); }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            renderTimer?.Stop();
            previewRenderCts?.Cancel();
            previewRenderCts?.Dispose();
            renderTimer?.Dispose();
            colorSettingsForm?.Close();
            base.OnFormClosed(e);
        }

        #endregion
    }

    #region Parser Implementation (Интегрировано из ParserMath)

    public enum TokenType { Number, Variable, Operator, LeftParen, RightParen }
    public record Token(TokenType Type, string Value);

    public class Tokenizer
    {
        private readonly string _text;
        private int _pos;

        public Tokenizer(string text)
        {
            _text = text.Replace(" ", "").ToLower();
            _pos = 0;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            while (_pos < _text.Length)
            {
                char current = _text[_pos];
                if (char.IsDigit(current) || current == '.')
                {
                    var start = _pos;
                    while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.')) { _pos++; }
                    tokens.Add(new Token(TokenType.Number, _text.Substring(start, _pos - start)));
                    continue;
                }
                if (char.IsLetter(current))
                {
                    var start = _pos;
                    while (_pos < _text.Length && char.IsLetterOrDigit(_text[_pos])) { _pos++; }
                    tokens.Add(new Token(TokenType.Variable, _text.Substring(start, _pos - start)));
                    continue;
                }
                if ("+-*/^".Contains(current))
                {
                    tokens.Add(new Token(TokenType.Operator, current.ToString()));
                    _pos++;
                    continue;
                }
                if (current == '(')
                {
                    tokens.Add(new Token(TokenType.LeftParen, current.ToString()));
                    _pos++;
                    continue;
                }
                if (current == ')')
                {
                    tokens.Add(new Token(TokenType.RightParen, current.ToString()));
                    _pos++;
                    continue;
                }
                throw new Exception($"Неизвестный символ '{current}' на позиции {_pos}");
            }
            return InsertImplicitMultiplication(tokens);
        }

        private List<Token> InsertImplicitMultiplication(List<Token> tokens)
        {
            var result = new List<Token>();
            for (int i = 0; i < tokens.Count; i++)
            {
                result.Add(tokens[i]);
                if (i < tokens.Count - 1)
                {
                    var current = tokens[i];
                    var next = tokens[i + 1];
                    if ((current.Type == TokenType.Number || current.Type == TokenType.Variable || current.Type == TokenType.RightParen) &&
                        (next.Type == TokenType.Variable || next.Type == TokenType.LeftParen))
                    {
                        result.Add(new Token(TokenType.Operator, "*"));
                    }
                }
            }
            return result;
        }
    }

    public abstract class ExpressionNode
    {
        public abstract Complex Evaluate(Dictionary<string, Complex> variables);
        public abstract ExpressionNode Differentiate(string varName);
        public abstract string Print(string indent = "");
        public override string ToString() => this.PrintSimple();
        public abstract string PrintSimple();
    }

    public class NumberNode : ExpressionNode
    {
        public Complex Value { get; }
        public NumberNode(Complex value) => Value = value;
        public override Complex Evaluate(Dictionary<string, Complex> variables) => Value;
        public override ExpressionNode Differentiate(string varName) => new NumberNode(Complex.Zero);
        public override string Print(string indent = "") => $"{indent}Number({Value})";
        public override string PrintSimple() => Value.ToString();
    }

    public class VariableNode : ExpressionNode
    {
        public string Name { get; }
        public VariableNode(string name) => Name = name;
        public override Complex Evaluate(Dictionary<string, Complex> variables)
        {
            if (Name == "i") return Complex.ImaginaryOne;
            if (variables.TryGetValue(Name, out var value)) return value;
            throw new Exception($"Значение для переменной '{Name}' не предоставлено.(Возможно вы хотели a*b, укажите операцию явно)");
        }
        public override ExpressionNode Differentiate(string varName)
        {
            if (Name == varName) return new NumberNode(Complex.One);
            if (Name == "i") return new NumberNode(Complex.Zero);
            return new NumberNode(Complex.Zero);
        }
        public override string Print(string indent = "") => $"{indent}Variable({Name})";
        public override string PrintSimple() => Name;
    }

    public class BinaryOpNode : ExpressionNode
    {
        public ExpressionNode Left { get; }
        public string Operator { get; }
        public ExpressionNode Right { get; }
        public BinaryOpNode(ExpressionNode left, string op, ExpressionNode right) { Left = left; Operator = op; Right = right; }
        public override Complex Evaluate(Dictionary<string, Complex> variables)
        {
            var leftVal = Left.Evaluate(variables);
            var rightVal = Right.Evaluate(variables);
            return Operator switch
            {
                "+" => leftVal + rightVal,
                "-" => leftVal - rightVal,
                "*" => leftVal * rightVal,
                "/" => leftVal / rightVal,
                "^" => Complex.Pow(leftVal, rightVal),
                _ => throw new Exception($"Неизвестный бинарный оператор '{Operator}'"),
            };
        }
        public override ExpressionNode Differentiate(string varName)
        {
            var u = Left; var v = Right;
            var du = Left.Differentiate(varName); var dv = Right.Differentiate(varName);
            return Operator switch
            {
                "+" => new BinaryOpNode(du, "+", dv),
                "-" => new BinaryOpNode(du, "-", dv),
                "*" => new BinaryOpNode(new BinaryOpNode(du, "*", v), "+", new BinaryOpNode(u, "*", dv)),
                "/" => new BinaryOpNode(new BinaryOpNode(new BinaryOpNode(du, "*", v), "-", new BinaryOpNode(u, "*", dv)), "/", new BinaryOpNode(v, "^", new NumberNode(new Complex(2, 0)))),
                "^" when v is NumberNode c => new BinaryOpNode(new BinaryOpNode(c, "*", du), "*", new BinaryOpNode(u, "^", new NumberNode(c.Value - 1))),
                _ => throw new Exception($"Дифференцирование для оператора '{Operator}' не поддерживается."),
            };
        }
        public override string Print(string indent = "")
        {
            var sb = new StringBuilder(); sb.AppendLine($"{indent}Op({Operator})");
            sb.AppendLine(Left.Print(indent + "  L:")); sb.Append(Right.Print(indent + "  R:"));
            return sb.ToString();
        }
        public override string PrintSimple() => $"({Left.PrintSimple()} {Operator} {Right.PrintSimple()})";
    }

    public class UnaryOpNode : ExpressionNode
    {
        public string Operator { get; }
        public ExpressionNode Operand { get; }
        public UnaryOpNode(string op, ExpressionNode operand) { Operator = op; Operand = operand; }
        public override Complex Evaluate(Dictionary<string, Complex> variables)
        {
            var operandVal = Operand.Evaluate(variables);
            return Operator switch { "-" => -operandVal, "+" => operandVal, _ => throw new Exception($"Неизвестный унарный оператор '{Operator}'"), };
        }
        public override ExpressionNode Differentiate(string varName) => new UnaryOpNode(Operator, Operand.Differentiate(varName));
        public override string Print(string indent = "")
        {
            var sb = new StringBuilder(); sb.AppendLine($"{indent}UnaryOp({Operator})");
            sb.Append(Operand.Print(indent + "   ")); return sb.ToString();
        }
        public override string PrintSimple() => $"({Operator}{Operand.PrintSimple()})";
    }

    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _pos;
        public Parser(List<Token> tokens) { _tokens = tokens; _pos = 0; }
        private Token Current => _pos < _tokens.Count ? _tokens[_pos] : new Token(TokenType.Operator, "");
        private void Advance() => _pos++;
        public ExpressionNode Parse()
        {
            var result = ParseExpression();
            if (_pos < _tokens.Count) throw new Exception($"Неожиданный токен '{Current.Value}' после завершения выражения.");
            return result;
        }
        private ExpressionNode ParseExpression()
        {
            var node = ParseTerm();
            while (Current.Value == "+" || Current.Value == "-") { var op = Current; Advance(); var right = ParseTerm(); node = new BinaryOpNode(node, op.Value, right); }
            return node;
        }
        private ExpressionNode ParseTerm()
        {
            var node = ParseFactor();
            while (Current.Value == "*" || Current.Value == "/") { var op = Current; Advance(); var right = ParseFactor(); node = new BinaryOpNode(node, op.Value, right); }
            return node;
        }
        private ExpressionNode ParseFactor()
        {
            var node = ParsePrimary();
            if (Current.Value == "^") { var op = Current; Advance(); var right = ParseFactor(); node = new BinaryOpNode(node, op.Value, right); }
            return node;
        }
        private ExpressionNode ParsePrimary()
        {
            var token = Current;
            if (token.Value == "+" || token.Value == "-") { Advance(); return new UnaryOpNode(token.Value, ParsePrimary()); }
            if (token.Type == TokenType.Number)
            {
                Advance();
                if (!double.TryParse(token.Value.Replace('.', ','), out var number) && !double.TryParse(token.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
                { throw new Exception($"Не удалось распознать число '{token.Value}'"); }
                return new NumberNode(new Complex(number, 0));
            }
            if (token.Type == TokenType.Variable) { Advance(); return new VariableNode(token.Value); }
            if (token.Type == TokenType.LeftParen)
            {
                Advance(); var node = ParseExpression();
                if (Current.Type != TokenType.RightParen) throw new Exception("Ожидалась закрывающая скобка ')'");
                Advance(); return node;
            }
            throw new Exception($"Неожиданный токен '{token.Value}'");
        }
    }

    #endregion
}