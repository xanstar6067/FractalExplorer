using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;

namespace FractalExplorer.Forms
{
    /// <summary>
    /// Диалоговое окно для сохранения и загрузки состояний фракталов.
    /// Предоставляет функциональность для управления пользовательскими сохранениями и предустановками.
    /// </summary>
    public partial class SaveLoadDialogForm : Form
    {
        private readonly ISaveLoadCapableFractal _ownerFractalForm;
        private List<FractalSaveStateBase> _displayedItems;

        // --- Поля для мозаичного рендера ---
        private RenderVisualizerComponent _renderVisualizer;
        private CancellationTokenSource _previewRenderCts;
        private Bitmap _previewBitmap;
        private Bitmap _currentRenderingBitmap;
        private readonly object _bitmapLock = new object();
        private const int TILE_SIZE = 16;
        private bool _isRenderingPreview = false;

        // --- Поля для прогрессивного кэширования превью ---
        /// <summary>
        /// Кешированный битмап полного превью. Заполняется плитка за плиткой.
        /// </summary>
        private static Bitmap _cachedFullPreviewBitmap;
        /// <summary>
        /// Уникальный идентификатор состояния, для которого создан кеш.
        /// </summary>
        private static string _cachedPreviewStateIdentifier;
        /// <summary>
        /// Набор для отслеживания уже отрендеренных плиток в кеше.
        /// Использует Point(X,Y) плитки как ключ.
        /// </summary>
        private static HashSet<Point> _renderedTilesCache;
        /// <summary>
        /// Объект для синхронизации доступа к статическому кешу, чтобы избежать состояний гонки.
        /// </summary>
        private static readonly object _previewCacheLock = new object();

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SaveLoadDialogForm"/>.
        /// </summary>
        /// <param name="ownerFractalForm">Экземпляр формы фрактала, поддерживающий сохранение и загрузку состояний.</param>
        /// <exception cref="ArgumentNullException">Вызывается, если <paramref name="ownerFractalForm"/> равен null.</exception>
        public SaveLoadDialogForm(ISaveLoadCapableFractal ownerFractalForm)
        {
            InitializeComponent();
            _ownerFractalForm = ownerFractalForm ?? throw new ArgumentNullException(nameof(ownerFractalForm));
            this.Text = $"Сохранение/Загрузка: {_ownerFractalForm.FractalTypeIdentifier}";

            // Инициализируем компоненты для визуализации рендера превью.
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += () =>
            {
                if (pictureBoxPreview.IsHandleCreated)
                {
                    pictureBoxPreview.Invalidate();
                }
            };
            pictureBoxPreview.Paint += PictureBoxPreview_Paint;

            // Динамически ищем чекбокс для настроек пресетов, так как его имя может отличаться в разных версиях формы,
            // и подписываемся на событие изменения его состояния.
            var presetsCheckBox = this.Controls.Find("cbPresets", true).FirstOrDefault() as CheckBox ?? this.Controls.Find("checkBoxShowPresets", true).FirstOrDefault() as CheckBox;
            if (presetsCheckBox != null)
            {
                presetsCheckBox.CheckedChanged += new System.EventHandler(this.cbPresets_CheckedChanged);
            }
        }

        /// <summary>
        /// Обрабатывает событие загрузки формы SaveLoadDialogForm.
        /// Инициализирует список сохранений и обновляет состояние кнопок.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void SaveLoadDialogForm_Load(object sender, EventArgs e)
        {
            PopulateList(false);
            UpdateButtonsState();
        }

        /// <summary>
        /// Заполняет список сохранений или предустановок в ListBox.
        /// </summary>
        /// <param name="showPresets">Если true, отображаются предустановки; иначе — пользовательские сохранения.</param>
        private void PopulateList(bool showPresets)
        {
            // Отменяем текущий рендер превью, чтобы избежать отображения устаревших данных
            // или конфликтов при обновлении списка.
            _previewRenderCts?.Cancel();

            if (showPresets)
            {
                _displayedItems = PresetManager.GetPresetsFor(_ownerFractalForm.FractalTypeIdentifier).OrderBy(p => p.SaveName).ToList();
            }
            else
            {
                _displayedItems = _ownerFractalForm.LoadAllSavesForThisType().OrderByDescending(s => s.Timestamp).ToList();
            }

            listBoxSaves.Items.Clear();
            if (_displayedItems != null)
            {
                foreach (var item in _displayedItems)
                {
                    string displayText = showPresets ? item.SaveName : $"{item.SaveName} ({item.Timestamp:yyyy-MM-dd HH:mm:ss})";
                    listBoxSaves.Items.Add(displayText);
                }
            }

            if (listBoxSaves.Items.Count > 0)
            {
                listBoxSaves.SelectedIndex = 0;
            }
            else
            {
                ClearPreview();
                textBoxSaveName.Text = "";
            }
        }

        /// <summary>
        /// Обрабатывает изменение выбранного элемента в ListBox со сохранениями.
        /// Отменяет текущий рендер превью и запускает новый для выбранного элемента.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void listBoxSaves_SelectedIndexChanged(object sender, EventArgs e)
        {
            _previewRenderCts?.Cancel();

            if (listBoxSaves.SelectedIndex >= 0 && _displayedItems != null && listBoxSaves.SelectedIndex < _displayedItems.Count)
            {
                var selectedState = _displayedItems[listBoxSaves.SelectedIndex];
                textBoxSaveName.Text = selectedState.SaveName;
                StartTiledPreviewRender(selectedState);
            }
            else
            {
                ClearPreview();
            }
            UpdateButtonsState();
        }

        /// <summary>
        /// Запускает асинхронный мозаичный рендер превью для указанного состояния фрактала.
        /// </summary>
        /// <param name="state">Состояние фрактала для рендера превью.</param>
        private async void StartTiledPreviewRender(FractalSaveStateBase state)
        {
            if (state == null || pictureBoxPreview.Width <= 0 || pictureBoxPreview.Height <= 0)
            {
                ClearPreview();
                return;
            }

            _isRenderingPreview = true;
            _previewRenderCts?.Cancel(); // Отменяем предыдущий рендер перед запуском нового
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;

            _renderVisualizer.NotifyRenderSessionStart();

            var newRenderingBitmap = new Bitmap(pictureBoxPreview.Width, pictureBoxPreview.Height, PixelFormat.Format32bppArgb);
            // Используем блокировку, чтобы обеспечить потокобезопасный доступ к _currentRenderingBitmap
            // и избежать конфликтов при одновременной попытке чтения и записи из разных потоков.
            lock (_bitmapLock)
            {
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = newRenderingBitmap;
                _previewBitmap?.Dispose();
                _previewBitmap = null;
            }
            pictureBoxPreview.Invalidate();

            try
            {
                var tiles = GenerateTiles(pictureBoxPreview.Width, pictureBoxPreview.Height);
                var dispatcher = new TileRenderDispatcher(tiles, Environment.ProcessorCount);

                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    _renderVisualizer.NotifyTileRenderStart(tile.Bounds);

                    byte[] tileBuffer = await GetOrRenderPreviewTileAsync(state, tile, pictureBoxPreview.Width, pictureBoxPreview.Height, TILE_SIZE);

                    ct.ThrowIfCancellationRequested();

                    // Используем блокировку для потокобезопасного обновления растрового изображения.
                    // Проверяем токен отмены и соответствие битмапа, чтобы не записывать данные в устаревший
                    // или отмененный битмап, если был начат новый рендер или операция отменена.
                    lock (_bitmapLock)
                    {
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap)
                        {
                            return;
                        }

                        Rectangle tileBounds = tile.Bounds;
                        int bytesPerPixel = 4; // Формат Format32bppArgb использует 4 байта на пиксель (ARGB).
                        int tileRowWidthInBytes = tileBounds.Width * bytesPerPixel;

                        // Блокируем биты растрового изображения для прямого доступа к памяти,
                        // что обеспечивает высокую производительность при копировании данных пикселей.
                        BitmapData bmpData = _currentRenderingBitmap.LockBits(tileBounds, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);

                        // Копируем данные из буфера плитки в растровое изображение построчно.
                        // Это необходимо, так как Scan0 указывает на начало первой строки,
                        // а Stride может быть больше ширины строки данных из-за выравнивания.
                        for (int y = 0; y < tileBounds.Height; y++)
                        {
                            IntPtr currentDestPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            int sourceOffset = y * tileRowWidthInBytes;
                            Marshal.Copy(tileBuffer, sourceOffset, currentDestPtr, tileRowWidthInBytes);
                        }

                        _currentRenderingBitmap.UnlockBits(bmpData);
                    }

                    _renderVisualizer.NotifyTileRenderComplete(tile.Bounds);

                }, token);

                token.ThrowIfCancellationRequested();

                // После успешного завершения рендера, устанавливаем временное изображение
                // как основное превью и освобождаем ссылки на промежуточный битмап.
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _previewBitmap?.Dispose();
                        _previewBitmap = _currentRenderingBitmap;
                        _currentRenderingBitmap = null;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Игнорируем исключение, так как отмена — это ожидаемое поведение при смене выбора.
                lock (_bitmapLock)
                {
                    if (_currentRenderingBitmap == newRenderingBitmap)
                    {
                        _currentRenderingBitmap?.Dispose();
                        _currentRenderingBitmap = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка мозаичного рендера превью: {ex.Message}");
            }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer.NotifyRenderSessionComplete();
                if (pictureBoxPreview.IsHandleCreated)
                {
                    pictureBoxPreview.Invalidate();
                }
            }
        }

        /// <summary>
        /// Асинхронно получает или рендерит плитку для предварительного просмотра.
        /// Этот метод использует механизм прогрессивного кэширования, чтобы избежать повторного рендеринга
        /// уже вычисленных плиток, и обеспечивает потокобезопасность при доступе к кешу.
        /// </summary>
        /// <param name="state">Состояние фрактала для рендеринга.</param>
        /// <param name="tile">Информация о запрашиваемой плитке.</param>
        /// <param name="totalWidth">Общая ширина изображения предварительного просмотра.</param>
        /// <param name="totalHeight">Общая высота изображения предварительного просмотра.</param>
        /// <param name="tileSize">Размер одной плитки.</param>
        /// <returns>Массив байтов, представляющий данные пикселей для запрошенной плитки.</returns>
        private async Task<byte[]> GetOrRenderPreviewTileAsync(FractalSaveStateBase state, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            string currentStateIdentifier = $"{state.FractalType}_{state.SaveName}_{state.Timestamp.Ticks}";
            var tileCoord = new Point(tile.Bounds.X, tile.Bounds.Y);

            // Шаг 1: Проверка и инициализация кеша
            lock (_previewCacheLock)
            {
                if (_cachedPreviewStateIdentifier != currentStateIdentifier)
                {
                    _cachedFullPreviewBitmap?.Dispose();
                    _cachedFullPreviewBitmap = new Bitmap(totalWidth, totalHeight, PixelFormat.Format32bppArgb);
                    _renderedTilesCache = new HashSet<Point>();
                    _cachedPreviewStateIdentifier = currentStateIdentifier;
                }
            }

            bool needsRender;
            lock (_previewCacheLock)
            {
                needsRender = !_renderedTilesCache.Contains(tileCoord);
            }

            byte[] tileBuffer;

            // Шаг 2: Рендеринг (если нужно) и обновление кеша
            if (needsRender)
            {
                tileBuffer = await _ownerFractalForm.RenderPreviewTileAsync(state, tile, totalWidth, totalHeight, tileSize);

                lock (_previewCacheLock)
                {
                    if (!_renderedTilesCache.Contains(tileCoord))
                    {
                        // --- НАЧАЛО ИСПРАВЛЕНИЯ: Корректное построчное копирование в кеш ---
                        BitmapData bmpData = _cachedFullPreviewBitmap.LockBits(tile.Bounds, ImageLockMode.WriteOnly, _cachedFullPreviewBitmap.PixelFormat);
                        int bytesPerPixel = 4;
                        int tileRowBytes = tile.Bounds.Width * bytesPerPixel;

                        for (int y = 0; y < tile.Bounds.Height; y++)
                        {
                            IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            int sourceOffset = y * tileRowBytes;
                            Marshal.Copy(tileBuffer, sourceOffset, destPtr, tileRowBytes);
                        }

                        _cachedFullPreviewBitmap.UnlockBits(bmpData);
                        _renderedTilesCache.Add(tileCoord);
                        // --- КОНЕЦ ИСПРАВЛЕНИЯ ---
                    }
                }
            }
            // Шаг 3: Извлечение данных из кеша (если они там уже были)
            else
            {
                tileBuffer = new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                lock (_previewCacheLock)
                {
                    // --- НАЧАЛО ИСПРАВЛЕНИЯ: Корректное построчное чтение из кеша ---
                    BitmapData bmpData = _cachedFullPreviewBitmap.LockBits(tile.Bounds, ImageLockMode.ReadOnly, _cachedFullPreviewBitmap.PixelFormat);
                    int bytesPerPixel = 4;
                    int tileRowBytes = tile.Bounds.Width * bytesPerPixel;

                    for (int y = 0; y < tile.Bounds.Height; y++)
                    {
                        IntPtr sourcePtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                        int destOffset = y * tileRowBytes;
                        Marshal.Copy(sourcePtr, tileBuffer, destOffset, tileRowBytes);
                    }

                    _cachedFullPreviewBitmap.UnlockBits(bmpData);
                    // --- КОНЕЦ ИСПРАВЛЕНИЯ ---
                }
            }

            return tileBuffer;
        }


        /// <summary>
        /// Обрабатывает событие Paint для PictureBoxPreview.
        /// Отображает текущее превью или процесс рендеринга.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события Paint.</param>
        private void PictureBoxPreview_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
            // Используем блокировку, чтобы избежать состояния гонки при доступе к битмапам превью,
            // которые могут обновляться из другого потока рендера.
            lock (_bitmapLock)
            {
                if (_previewBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(_previewBitmap, Point.Empty);
                }
                if (_currentRenderingBitmap != null)
                {
                    e.Graphics.DrawImageUnscaled(_currentRenderingBitmap, Point.Empty);
                }
            }
            if (_renderVisualizer != null && _isRenderingPreview)
            {
                _renderVisualizer.DrawVisualization(e.Graphics);
            }
        }

        /// <summary>
        /// Очищает текущее изображение превью и отменяет активный процесс рендеринга.
        /// </summary>
        private void ClearPreview()
        {
            _previewRenderCts?.Cancel();
            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose();
                _previewBitmap = null;
                _currentRenderingBitmap?.Dispose();
                _currentRenderingBitmap = null;
            }
            if (pictureBoxPreview.IsHandleCreated)
            {
                pictureBoxPreview.Invalidate();
            }
        }

        /// <summary>
        /// Генерирует список плиток для мозаичного рендеринга.
        /// Плитки сортируются по удаленности от центра для более естественного визуального обновления.
        /// </summary>
        /// <param name="width">Ширина общей области рендеринга.</param>
        /// <param name="height">Высота общей области рендеринга.</param>
        /// <returns>Список объектов <see cref="TileInfo"/>.</returns>
        private List<TileInfo> GenerateTiles(int width, int height)
        {
            var tiles = new List<TileInfo>();
            Point center = new Point(width / 2, height / 2);
            for (int y = 0; y < height; y += TILE_SIZE)
            {
                for (int x = 0; x < width; x += TILE_SIZE)
                {
                    int tileWidth = Math.Min(TILE_SIZE, width - x);
                    int tileHeight = Math.Min(TILE_SIZE, height - y);
                    tiles.Add(new TileInfo(x, y, tileWidth, tileHeight));
                }
            }
            // Сортируем плитки по удаленности от центра, чтобы рендер начинался с центральных областей,
            // что создает более естественное визуальное обновление.
            return tiles.OrderBy(t => Math.Pow(t.Center.X - center.X, 2) + Math.Pow(t.Center.Y - center.Y, 2)).ToList();
        }

        /// <summary>
        /// Обрабатывает событие закрытия формы SaveLoadDialogForm.
        /// Очищает превью и освобождает ресурсы визуализатора рендера.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события закрытия формы.</param>
        private void SaveLoadDialogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ClearPreview();
            _renderVisualizer?.Dispose();
        }

        /// <summary>
        /// Обрабатывает событие нажатия кнопки "Загрузить".
        /// Загружает выбранное состояние фрактала и закрывает диалог с результатом OK.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (listBoxSaves.SelectedIndex >= 0 && _displayedItems != null && listBoxSaves.SelectedIndex < _displayedItems.Count)
            {
                _ownerFractalForm.LoadState(_displayedItems[listBoxSaves.SelectedIndex]);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        /// <summary>
        /// Обрабатывает событие нажатия кнопки "Сохранить как новую".
        /// Сохраняет текущее состояние фрактала под новым именем, обрабатывая дубликаты.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnSaveAsNew_Click(object sender, EventArgs e)
        {
            var presetsCheckBox = this.Controls.Find("cbPresets", true).FirstOrDefault() as CheckBox ?? this.Controls.Find("checkBoxShowPresets", true).FirstOrDefault() as CheckBox;
            if (presetsCheckBox.Checked)
            {
                return;
            }

            string saveName = textBoxSaveName.Text.Trim();
            if (string.IsNullOrWhiteSpace(saveName))
            {
                MessageBox.Show("Пожалуйста, введите имя для сохранения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var userSaves = _ownerFractalForm.LoadAllSavesForThisType();
            var existingSave = userSaves.FirstOrDefault(s => s.SaveName.Equals(saveName, StringComparison.OrdinalIgnoreCase));
            if (existingSave != null)
            {
                if (MessageBox.Show($"Сохранение с именем '{saveName}' уже существует. Перезаписать?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    userSaves.Remove(existingSave);
                }
                else
                {
                    return;
                }
            }

            var newState = _ownerFractalForm.GetCurrentStateForSave(saveName);
            userSaves.Add(newState);
            _ownerFractalForm.SaveAllSavesForThisType(userSaves);

            PopulateList(false);
            int newIndex = _displayedItems.FindIndex(s => s.SaveName == saveName && s.Timestamp == newState.Timestamp);
            if (newIndex != -1)
            {
                listBoxSaves.SelectedIndex = newIndex;
            }
        }

        /// <summary>
        /// Обрабатывает событие нажатия кнопки "Удалить".
        /// Удаляет выбранное сохранение после подтверждения пользователя.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnDelete_Click(object sender, EventArgs e)
        {
            var presetsCheckBox = this.Controls.Find("cbPresets", true).FirstOrDefault() as CheckBox ?? this.Controls.Find("checkBoxShowPresets", true).FirstOrDefault() as CheckBox;
            if (presetsCheckBox.Checked)
            {
                return;
            }

            if (listBoxSaves.SelectedIndex >= 0 && _displayedItems != null && listBoxSaves.SelectedIndex < _displayedItems.Count)
            {
                var stateToDelete = _displayedItems[listBoxSaves.SelectedIndex];
                if (MessageBox.Show($"Вы уверены, что хотите удалить сохранение '{stateToDelete.SaveName}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    var userSaves = _ownerFractalForm.LoadAllSavesForThisType();
                    var itemToRemove = userSaves.FirstOrDefault(s => s.SaveName == stateToDelete.SaveName && s.Timestamp == stateToDelete.Timestamp);
                    if (itemToRemove != null)
                    {
                        userSaves.Remove(itemToRemove);
                        _ownerFractalForm.SaveAllSavesForThisType(userSaves);
                        PopulateList(false);
                    }
                }
            }
        }

        /// <summary>
        /// Обновляет состояние (доступность) кнопок в зависимости от выбранного элемента и режима (пресеты/сохранения).
        /// </summary>
        private void UpdateButtonsState()
        {
            var presetsCheckBox = this.Controls.Find("cbPresets", true).FirstOrDefault() as CheckBox ?? this.Controls.Find("checkBoxShowPresets", true).FirstOrDefault() as CheckBox;
            bool itemSelected = listBoxSaves.SelectedIndex != -1;
            bool presetsMode = presetsCheckBox.Checked;

            btnLoad.Enabled = itemSelected;
            btnDelete.Enabled = itemSelected && !presetsMode;
            btnSaveAsNew.Enabled = !presetsMode;
            textBoxSaveName.Enabled = !presetsMode;
        }

        /// <summary>
        /// Обрабатывает изменение состояния чекбокса "Показать пресеты".
        /// Обновляет список отображаемых элементов и состояние кнопок.
        /// </summary>
        /// <param name="sender">Источник события (чекбокс).</param>
        /// <param name="e">Аргументы события.</param>
        private void cbPresets_CheckedChanged(object sender, EventArgs e)
        {
            var presetsCheckBox = sender as CheckBox;
            if (presetsCheckBox != null)
            {
                PopulateList(presetsCheckBox.Checked);
                UpdateButtonsState();
            }
        }

        /// <summary>
        /// Обрабатывает событие нажатия кнопки "Отмена".
        /// Закрывает диалог с результатом Cancel.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}