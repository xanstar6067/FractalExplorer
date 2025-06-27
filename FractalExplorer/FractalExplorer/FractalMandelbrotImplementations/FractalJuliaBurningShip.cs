using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Forms.SelectorsForms.Selector;
using FractalExplorer.Resources;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Drawing.Imaging;
using System.Globalization;

namespace FractalExplorer.Projects
{
    /// <summary>
    /// Форма для отображения и взаимодействия с фракталом "Пылающий корабль" Жюлиа.
    /// Позволяет настраивать параметры фрактала и выбирать константу 'C'
    /// с помощью предпросмотра множества "Пылающий корабль" Мандельброта.
    /// </summary>
    public partial class FractalJuliaBurningShip : FractalMandelbrotFamilyForm
    {
        #region Fields

        /// <summary>
        /// Минимальное значение действительной части для рендеринга предпросмотра множества "Пылающий корабль".
        /// </summary>
        private const decimal BURNING_SHIP_MIN_REAL = -2.0m;

        /// <summary>
        /// Максимальное значение действительной части для рендеринга предпросмотра множества "Пылающий корабль".
        /// </summary>
        private const decimal BURNING_SHIP_MAX_REAL = 1.5m;

        /// <summary>
        /// Минимальное значение мнимой части для рендеринга предпросмотра множества "Пылающий корабль".
        /// </summary>
        private const decimal BURNING_SHIP_MIN_IMAGINARY = -1.0m;

        /// <summary>
        /// Максимальное значение мнимой части для рендеринга предпросмотра множества "Пылающий корабль".
        /// </summary>
        private const decimal BURNING_SHIP_MAX_IMAGINARY = 1.5m;

        /// <summary>
        /// Количество итераций для рендеринга предпросмотра множества "Пылающий корабль".
        /// </summary>
        private const int BURNING_SHIP_PREVIEW_ITERATIONS = 75;

        /// <summary>
        /// Окно выбора константы 'C' на основе множества "Пылающий корабль".
        /// </summary>
        private BurningShipCSelectorForm _burningShipCSelectorWindow;

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="FractalJuliaBurningShip"/>.
        /// </summary>
        public FractalJuliaBurningShip()
        {
            Text = "Фрактал Горящий Корабль (Жюлиа)";
        }

        #endregion

        #region Fractal Engine Overrides

        /// <summary>
        /// Создает и возвращает экземпляр движка для рендеринга фрактала "Пылающий корабль" Жюлиа.
        /// </summary>
        /// <returns>Экземпляр <see cref="JuliaBurningShipEngine"/>.</returns>
        protected override FractalMandelbrotFamilyEngine CreateEngine()
        {
            return new JuliaBurningShipEngine();
        }

        /// <summary>
        /// Получает базовый масштаб для фрактала "Пылающий корабль" Жюлиа.
        /// </summary>
        protected override decimal BaseScale => 4.0m;

        /// <summary>
        /// Получает начальную координату X центра для фрактала "Пылающий корабль" Жюлиа.
        /// </summary>
        protected override decimal InitialCenterX => 0.0m;

        /// <summary>
        /// Получает начальную координату Y центра для фрактала "Пылающий корабль" Жюлиа.
        /// </summary>
        protected override decimal InitialCenterY => 0.0m;

        /// <summary>
        /// Вызывается после завершения инициализации формы.
        /// Отображает специфичные для Жюлиа элементы управления и запускает рендеринг предпросмотра "Пылающего корабля".
        /// </summary>
        protected override void OnPostInitialize()
        {
            mandelbrotPreviewPanel.Visible = true;
            lblRe.Visible = true;
            nudRe.Visible = true;
            lblIm.Visible = true;
            nudIm.Visible = true;

            // Устанавливаем начальные значения для 'c' по умолчанию для примера фрактала "Пылающий корабль" Жюлиа.
            // Эти значения подобраны для демонстрации интересного вида фрактала.
            nudRe.Value = -1.7551867961883m;
            nudIm.Value = 0.01068m;

            var previewCanvas = Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault() as PictureBox;
            if (previewCanvas != null)
            {
                previewCanvas.Click += mandelbrotCanvas_Click;
                previewCanvas.Paint += mandelbrotCanvas_Paint;
                // Запускаем рендеринг предпросмотра асинхронно, чтобы не блокировать основной поток UI
                // и обеспечить отзывчивость формы во время загрузки изображения.
                Task.Run(() => RenderAndDisplayBurningShipSet());
            }
        }

        /// <summary>
        /// Обновляет специфичные для фрактала "Пылающий корабль" Жюлиа параметры движка.
        /// Устанавливает комплексную константу 'C' на основе значений из UI и вызывает перерисовку предпросмотра.
        /// </summary>
        protected override void UpdateEngineSpecificParameters()
        {
            _fractalEngine.C = new ComplexDecimal(nudRe.Value, nudIm.Value);
            var previewCanvas = Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault();
            if (previewCanvas != null && previewCanvas.IsHandleCreated && !previewCanvas.IsDisposed)
            {
                // Запрашиваем перерисовку маркера на предпросмотре, так как значение 'C' изменилось,
                // чтобы указать новое выбранное положение.
                previewCanvas.Invalidate();
            }
        }

        /// <summary>
        /// Возвращает строку с деталями для имени файла сохранения,
        /// включая значения действительной и мнимой частей константы 'C'.
        /// </summary>
        /// <returns>Строка, содержащая информацию о фрактале для имени файла.</returns>
        protected override string GetSaveFileNameDetails()
        {
            // Форматируем значения действительной и мнимой частей с высокой точностью (15 знаков после запятой)
            // и заменяем десятичную точку на подчеркивание, чтобы избежать проблем с именами файлов.
            string reString = nudRe.Value.ToString("F15", CultureInfo.InvariantCulture).Replace(".", "_");
            string imString = nudIm.Value.ToString("F15", CultureInfo.InvariantCulture).Replace(".", "_");
            return $"burningship_julia_re{reString}_im{imString}";
        }

        #endregion

        #region Burning Ship Preview / C-Parameter Selection

        /// <summary>
        /// Рендерит множество "Пылающий корабль" в фоновом потоке и отображает его на канвасе предпросмотра.
        /// </summary>
        private void RenderAndDisplayBurningShipSet()
        {
            var previewCanvas = Controls.Find("mandelbrotPreviewCanvas", true).FirstOrDefault() as PictureBox;
            if (previewCanvas == null || previewCanvas.Width <= 0 || previewCanvas.Height <= 0)
            {
                return;
            }

            // Рендерим изображение множества "Пылающий корабль".
            Bitmap burningShipImage = RenderBurningShipSetInternal(previewCanvas.Width, previewCanvas.Height, BURNING_SHIP_PREVIEW_ITERATIONS);

            // Обновляем изображение на UI-потоке, используя Invoke, чтобы безопасно взаимодействовать с элементами UI
            // из фонового потока.
            if (previewCanvas.IsHandleCreated && !previewCanvas.IsDisposed)
            {
                previewCanvas.Invoke(() =>
                {
                    // Освобождаем старое изображение, чтобы предотвратить утечки памяти,
                    // прежде чем присваивать новое.
                    previewCanvas.Image?.Dispose();
                    previewCanvas.Image = burningShipImage;
                });
            }
            else
            {
                // Если канвас уже недействителен (например, форма закрывается), просто освобождаем битмап,
                // чтобы избежать утечек ресурсов.
                burningShipImage?.Dispose();
            }
        }

        /// <summary>
        /// Внутренний метод для рендеринга множества "Пылающий корабль" Мандельброта в Bitmap.
        /// </summary>
        /// <param name="canvasWidth">Ширина канваса для рендеринга.</param>
        /// <param name="canvasHeight">Высота канваса для рендеринга.</param>
        /// <param name="iterationsLimit">Максимальное количество итераций.</param>
        /// <returns>Объект <see cref="Bitmap"/> с отрисованным множеством "Пылающий корабль".</returns>
        private Bitmap RenderBurningShipSetInternal(int canvasWidth, int canvasHeight, int iterationsLimit)
        {
            Bitmap bitmap = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format24bppRgb);

            // Создаем временный движок "Пылающий корабль" Мандельброта с фиксированными параметрами для предпросмотра.
            // Это позволяет рендерить базовое множество независимо от текущих настроек основного движка Жюлиа.
            var engine = new MandelbrotBurningShipEngine
            {
                MaxIterations = iterationsLimit,
                ThresholdSquared = 4m,
                Palette = GetPaletteMandelbrotClassicColor, // Используем специальную палитру для предпросмотра.
                Scale = BURNING_SHIP_MAX_REAL - BURNING_SHIP_MIN_REAL,
                CenterX = (BURNING_SHIP_MAX_REAL + BURNING_SHIP_MIN_REAL) / 2,
                CenterY = (BURNING_SHIP_MAX_IMAGINARY + BURNING_SHIP_MIN_IMAGINARY) / 2
            };

            // Блокируем биты изображения для прямого и быстрого доступа к буферу пикселей.
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, canvasWidth, canvasHeight), ImageLockMode.WriteOnly, bitmap.PixelFormat);
            int bufferSize = Math.Abs(bitmapData.Stride) * canvasHeight;
            byte[] pixelBuffer = new byte[bufferSize];
            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            // Создаем одну плитку, охватывающую весь канвас, для рендеринга всего предпросмотра за один вызов.
            var tile = new TileInfo(0, 0, canvasWidth, canvasHeight);

            // Рендерим множество в буфер пикселей.
            engine.RenderTile(pixelBuffer, bitmapData.Stride, bytesPerPixel, tile, canvasWidth, canvasHeight);

            // Копируем данные из буфера в битмап и разблокируем биты.
            System.Runtime.InteropServices.Marshal.Copy(pixelBuffer, 0, bitmapData.Scan0, bufferSize);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        /// <summary>
        /// Обработчик события отрисовки канваса предпросмотра "Пылающего корабля".
        /// Рисует маркер, указывающий текущие значения Re/Im константы 'C'.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события рисования.</param>
        private void mandelbrotCanvas_Paint(object sender, PaintEventArgs e)
        {
            var previewCanvas = sender as PictureBox;
            if (previewCanvas?.Image == null)
            {
                return;
            }

            decimal realRange = BURNING_SHIP_MAX_REAL - BURNING_SHIP_MIN_REAL;
            decimal imaginaryRange = BURNING_SHIP_MAX_IMAGINARY - BURNING_SHIP_MIN_IMAGINARY;
            decimal currentCReal = nudRe.Value;
            decimal currentCImaginary = nudIm.Value;

            // Рисуем маркер только если диапазон действителен и текущие значения 'C' находятся в пределах
            // отображаемой области предпросмотра, чтобы маркер не выходил за границы.
            if (realRange > 0 && imaginaryRange > 0 && currentCReal >= BURNING_SHIP_MIN_REAL && currentCReal <= BURNING_SHIP_MAX_REAL &&
                currentCImaginary >= BURNING_SHIP_MIN_IMAGINARY && currentCImaginary <= BURNING_SHIP_MAX_IMAGINARY)
            {
                // Преобразуем координаты 'C' (действительная часть на оси X, мнимая на оси Y) в пиксельные координаты на канвасе предпросмотра.
                // Для Y-координаты учитывается, что в GDI+ Y растет вниз, а в комплексной плоскости мнимая часть растет вверх.
                int markerX = (int)((currentCReal - BURNING_SHIP_MIN_REAL) / realRange * previewCanvas.Width);
                int markerY = (int)((BURNING_SHIP_MAX_IMAGINARY - currentCImaginary) / imaginaryRange * previewCanvas.Height);

                using (Pen markerPen = new Pen(Color.FromArgb(200, Color.LimeGreen), 1.5f))
                {
                    // Рисуем горизонтальную и вертикальную линии, образующие крест, чтобы визуально указать
                    // на текущую выбранную точку 'C' на предпросмотре.
                    e.Graphics.DrawLine(markerPen, 0, markerY, previewCanvas.Width, markerY);
                    e.Graphics.DrawLine(markerPen, markerX, 0, markerX, previewCanvas.Height);
                }
            }
        }

        /// <summary>
        /// Обработчик события клика по канвасу предпросмотра "Пылающего корабля".
        /// Открывает окно выбора константы 'C', позволяющее интерактивно выбирать ее значение.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void mandelbrotCanvas_Click(object sender, EventArgs e)
        {
            double initialReal = (double)nudRe.Value;
            double initialImaginary = (double)nudIm.Value;

            // Создаем или активируем окно выбора константы 'C'.
            // Это предотвращает создание нескольких экземпляров окна, если оно уже открыто.
            if (_burningShipCSelectorWindow == null || _burningShipCSelectorWindow.IsDisposed)
            {
                _burningShipCSelectorWindow = new BurningShipCSelectorForm(this, initialReal, initialImaginary);
                // Подписываемся на событие выбора координат, чтобы обновить NumericUpDown контролы
                // на текущей форме, когда пользователь выбирает новую точку 'C'.
                _burningShipCSelectorWindow.CoordinatesSelected += (re, im) =>
                {
                    nudRe.Value = (decimal)re;
                    nudIm.Value = (decimal)im;
                };
                // Устанавливаем _burningShipCSelectorWindow в null после закрытия формы,
                // чтобы она могла быть открыта снова при следующем клике.
                _burningShipCSelectorWindow.FormClosed += (s, args) => { _burningShipCSelectorWindow = null; };
                _burningShipCSelectorWindow.Show(this);
            }
            else
            {
                _burningShipCSelectorWindow.Activate(); // Активируем существующее окно.
                // Обновляем в нем выбранные координаты, чтобы оно отражало текущие значения 'C'
                // основной формы при повторной активации.
                _burningShipCSelectorWindow.SetSelectedCoordinates(initialReal, initialImaginary, true);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Локальная функция палитры для рендеринга предпросмотра множества Мандельброта (или "Пылающего корабля").
        /// Генерирует классическую цветовую схему, где цвета зависят от количества итераций
        /// до выхода из множества, создавая градиентный эффект.
        /// </summary>
        /// <param name="iter">Количество итераций до выхода за порог.</param>
        /// <param name="maxIter">Максимально допустимое количество итераций.</param>
        /// <param name="maxColorIterations">Максимальное количество итераций для нормализации цвета.</param>
        /// <returns>Цвет пикселя.</returns>
        private Color GetPaletteMandelbrotClassicColor(int iter, int maxIter, int maxColorIterations)
        {
            if (iter == maxIter)
            {
                return Color.Black; // Точки, принадлежащие множеству (не вышедшие за порог), окрашиваются в черный.
            }

            // Нормализуем количество итераций к диапазону [0, 1] для использования в цветовой функции.
            double tClassic = (double)iter / maxIter;
            byte r, g, b;

            // Пример классической цветовой схемы с градиентами.
            // Цвета выбираются таким образом, чтобы создать плавный переход и визуально выделить
            // границы множества.
            if (tClassic < 0.5)
            {
                double t = tClassic * 2;
                r = (byte)(t * 200);
                g = (byte)(t * 50);
                b = (byte)(t * 30);
            }
            else
            {
                double t = (tClassic - 0.5) * 2;
                r = (byte)(200 + t * 55);
                g = (byte)(50 + t * 205);
                b = (byte)(30 + t * 225);
            }
            return Color.FromArgb(r, g, b);
        }

        #endregion

        #region ISaveLoadCapableFractal Overrides

        /// <summary>
        /// Получает строковый идентификатор типа фрактала.
        /// </summary>
        public override string FractalTypeIdentifier => "JuliaBurningShip";

        /// <summary>
        /// Получает конкретный тип состояния сохранения, используемый для фрактала "Пылающий корабль" Жюлиа.
        /// </summary>
        public override Type ConcreteSaveStateType => typeof(JuliaFamilySaveState);

        /// <summary>
        /// Загружает все сохраненные состояния фрактала для данного типа.
        /// </summary>
        /// <returns>Список базовых объектов состояний фрактала.</returns>
        public override List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<JuliaFamilySaveState>(this.FractalTypeIdentifier);
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        /// <summary>
        /// Сохраняет предоставленный список состояний фрактала для данного типа.
        /// </summary>
        /// <param name="saves">Список базовых объектов состояний фрактала для сохранения.</param>
        public override void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<JuliaFamilySaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }

        #endregion
    }
}