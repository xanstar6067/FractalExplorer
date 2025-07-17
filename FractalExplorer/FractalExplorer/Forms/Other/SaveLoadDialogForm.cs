using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using FractalExplorer.Projects; // Добавлено для доступа к классам превью-параметров
using System.Text.Json; // Добавлено для десериализации

namespace FractalExplorer.Forms
{
    /// <summary>
    /// Представляет диалоговое окно для сохранения и загрузки состояний фракталов.
    /// Предоставляет функциональность для управления пользовательскими сохранениями и предустановками,
    /// включая асинхронный, мозаичный рендер предварительного просмотра с прогрессивным кэшированием.
    /// </summary>
    public partial class SaveLoadDialogForm : Form
    {
        private readonly ISaveLoadCapableFractal _ownerFractalForm;
        private List<FractalSaveStateBase> _displayedItems;

        // --- Поля для мозаичного рендера ---
        private readonly RenderVisualizerComponent _renderVisualizer;
        private CancellationTokenSource _previewRenderCts;
        private Bitmap _previewBitmap;
        private Bitmap _currentRenderingBitmap;
        private readonly object _bitmapLock = new object();
        private const int TILE_SIZE = 16;
        private bool _isRenderingPreview = false;

        // --- Поля для прогрессивного кэширования превью ---

        /// <summary>
        /// Кешированный битмап полного превью. Заполняется по мере рендеринга плиток.
        /// Является статическим для сохранения между открытиями диалогового окна.
        /// </summary>
        private static Bitmap _cachedFullPreviewBitmap;

        /// <summary>
        /// Уникальный идентификатор состояния, для которого был создан текущий кеш.
        /// </summary>
        private static string _cachedPreviewStateIdentifier;

        /// <summary>
        /// Набор для отслеживания уже отрендеренных плиток в кеше, чтобы избежать повторной работы.
        /// Использует Point(X, Y) плитки в качестве ключа.
        /// </summary>
        private static HashSet<Point> _renderedTilesCache;

        /// <summary>
        /// Объект для синхронизации доступа к статическим полям кеша из разных потоков.
        /// </summary>
        private static readonly object _previewCacheLock = new object();

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SaveLoadDialogForm"/>.
        /// </summary>
        /// <param name="ownerFractalForm">Экземпляр формы фрактала, поддерживающий сохранение и загрузку.</param>
        /// <exception cref="ArgumentNullException">Вызывается, если <paramref name="ownerFractalForm"/> равен null.</exception>
        public SaveLoadDialogForm(ISaveLoadCapableFractal ownerFractalForm)
        {
            InitializeComponent();
            _ownerFractalForm = ownerFractalForm ?? throw new ArgumentNullException(nameof(ownerFractalForm));
            this.Text = $"Сохранение/Загрузка: {_ownerFractalForm.FractalTypeIdentifier}";

            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += () =>
            {
                if (pictureBoxPreview.IsHandleCreated)
                {
                    pictureBoxPreview.Invalidate();
                }
            };
            pictureBoxPreview.Paint += PictureBoxPreview_Paint;

            var presetsCheckBox = this.Controls.Find("cbPresets", true).FirstOrDefault() as CheckBox ?? this.Controls.Find("checkBoxShowPresets", true).FirstOrDefault() as CheckBox;
            if (presetsCheckBox != null)
            {
                presetsCheckBox.CheckedChanged += new EventHandler(this.cbPresets_CheckedChanged);
            }
        }

        /// <summary>
        /// Обрабатывает событие загрузки формы. Инициализирует список сохранений.
        /// </summary>
        private void SaveLoadDialogForm_Load(object sender, EventArgs e)
        {
            PopulateList(false);
            UpdateButtonsState();
        }

        /// <summary>
        /// Заполняет список в ListBox сохранениями или предустановками.
        /// </summary>
        /// <param name="showPresets">Если true, отображаются предустановки; иначе — пользовательские сохранения.</param>
        private void PopulateList(bool showPresets)
        {
            _previewRenderCts?.Cancel(); // Отменяем текущий рендер, так как список обновляется.

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
        /// Обрабатывает изменение выбранного элемента в списке. Запускает рендер нового превью.
        /// </summary>
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
        /// Управляет жизненным циклом рендеринга, включая отмену, обработку ошибок и обновление UI.
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
            _previewRenderCts?.Cancel();
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;

            _renderVisualizer.NotifyRenderSessionStart();

            var newRenderingBitmap = new Bitmap(pictureBoxPreview.Width, pictureBoxPreview.Height, PixelFormat.Format32bppArgb);
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

                    // Потокобезопасно обновляем часть растрового изображения данными из отрендеренной плитки.
                    lock (_bitmapLock)
                    {
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap) return;

                        BitmapData bmpData = _currentRenderingBitmap.LockBits(tile.Bounds, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
                        int tileRowWidthInBytes = tile.Bounds.Width * 4;

                        // Копируем данные построчно, чтобы учесть возможную разницу между Stride и шириной строки.
                        for (int y = 0; y < tile.Bounds.Height; y++)
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

                // Если рендер завершился успешно, делаем временный битмап основным.
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
                // Отмена — это штатное поведение, игнорируем исключение.
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
        /// Асинхронно получает плитку для превью из кеша или рендерит её, если в кеше её нет.
        /// Использует статический прогрессивный кеш для ускорения повторного отображения.
        /// </summary>
        /// <param name="state">Состояние фрактала для рендеринга.</param>
        /// <param name="tile">Информация о запрашиваемой плитке.</param>
        /// <param name="totalWidth">Общая ширина изображения превью.</param>
        /// <param name="totalHeight">Общая высота изображения превью.</param>
        /// <param name="tileSize">Размер одной плитки.</param>
        /// <returns>Массив байтов с пиксельными данными для запрошенной плитки (формат 32bpp ARGB).</returns>
        private async Task<byte[]> GetOrRenderPreviewTileAsync(FractalSaveStateBase state, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            string currentStateIdentifier = $"{state.FractalType}_{state.SaveName}_{state.Timestamp.Ticks}";
            var tileCoord = new Point(tile.Bounds.X, tile.Bounds.Y);

            lock (_previewCacheLock)
            {
                // Если кеш не соответствует текущему состоянию, сбрасываем его.
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
            if (needsRender)
            {
                // Делегируем вызов рендеринга соответствующей реализации фрактала.
                tileBuffer = await _ownerFractalForm.RenderPreviewTileAsync(state, tile, totalWidth, totalHeight, tileSize);

                lock (_previewCacheLock)
                {
                    // Повторная проверка на случай, если другой поток уже отрендерил эту плитку.
                    if (!_renderedTilesCache.Contains(tileCoord))
                    {
                        // Построчно копируем отрендеренную плитку в общий кеш-битмап.
                        BitmapData bmpData = _cachedFullPreviewBitmap.LockBits(tile.Bounds, ImageLockMode.WriteOnly, _cachedFullPreviewBitmap.PixelFormat);
                        int tileRowBytes = tile.Bounds.Width * 4;
                        for (int y = 0; y < tile.Bounds.Height; y++)
                        {
                            IntPtr destPtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                            Marshal.Copy(tileBuffer, y * tileRowBytes, destPtr, tileRowBytes);
                        }
                        _cachedFullPreviewBitmap.UnlockBits(bmpData);
                        _renderedTilesCache.Add(tileCoord);
                    }
                }
            }
            else
            {
                // Если плитка уже есть в кеше, извлекаем ее данные.
                tileBuffer = new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                lock (_previewCacheLock)
                {
                    // Построчно читаем данные плитки из общего кеш-битмапа.
                    BitmapData bmpData = _cachedFullPreviewBitmap.LockBits(tile.Bounds, ImageLockMode.ReadOnly, _cachedFullPreviewBitmap.PixelFormat);
                    int tileRowBytes = tile.Bounds.Width * 4;
                    for (int y = 0; y < tile.Bounds.Height; y++)
                    {
                        IntPtr sourcePtr = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);
                        Marshal.Copy(sourcePtr, tileBuffer, y * tileRowBytes, tileRowBytes);
                    }
                    _cachedFullPreviewBitmap.UnlockBits(bmpData);
                }
            }

            return tileBuffer;
        }

        /// <summary>
        /// Обрабатывает событие Paint для PictureBox. Отображает текущее превью и/или процесс рендеринга.
        /// </summary>
        private void PictureBoxPreview_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Black);
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
        /// Генерирует и сортирует список плиток для мозаичного рендеринга.
        /// Сортировка по удаленности от центра создает более естественный эффект появления изображения.
        /// </summary>
        /// <returns>Список объектов <see cref="TileInfo"/>, отсортированный от центра к краям.</returns>
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
            return tiles.OrderBy(t => Math.Pow(t.Center.X - center.X, 2) + Math.Pow(t.Center.Y - center.Y, 2)).ToList();
        }

        /// <summary>
        /// Обрабатывает событие закрытия формы, освобождая ресурсы.
        /// </summary>
        private void SaveLoadDialogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ClearPreview();
            _renderVisualizer?.Dispose();
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Загрузить".
        /// </summary>
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
        /// Обрабатывает нажатие кнопки "Сохранить как новую".
        /// </summary>
        private void btnSaveAsNew_Click(object sender, EventArgs e)
        {
            var presetsCheckBox = this.Controls.Find("cbPresets", true).FirstOrDefault() as CheckBox ?? this.Controls.Find("checkBoxShowPresets", true).FirstOrDefault() as CheckBox;
            if (presetsCheckBox.Checked) return;

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
        /// Обрабатывает нажатие кнопки "Удалить".
        /// </summary>
        private void btnDelete_Click(object sender, EventArgs e)
        {
            var presetsCheckBox = this.Controls.Find("cbPresets", true).FirstOrDefault() as CheckBox ?? this.Controls.Find("checkBoxShowPresets", true).FirstOrDefault() as CheckBox;
            if (presetsCheckBox.Checked) return;

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
        /// Обновляет состояние доступности кнопок в зависимости от контекста.
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
        /// </summary>
        private void cbPresets_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is CheckBox presetsCheckBox)
            {
                PopulateList(presetsCheckBox.Checked);
                UpdateButtonsState();
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Отмена".
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}