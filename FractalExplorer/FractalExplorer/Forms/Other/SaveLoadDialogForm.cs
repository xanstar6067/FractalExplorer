// --- НАЧАЛО ФАЙЛА SaveLoadDialogForm.cs ---

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
        private const int TILE_SIZE = 32;
        private bool _isRenderingPreview = false;

        // --- Поля для прогрессивного кэширования превью ---
        private static Bitmap _cachedFullPreviewBitmap;
        private static string _cachedPreviewStateIdentifier;
        private static HashSet<Point> _renderedTilesCache;
        private static readonly object _previewCacheLock = new object();

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SaveLoadDialogForm"/>.
        /// </summary>
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
                presetsCheckBox.CheckedChanged += new System.EventHandler(this.cbPresets_CheckedChanged);
            }
        }

        private void SaveLoadDialogForm_Load(object sender, EventArgs e)
        {
            PopulateList(false);
            UpdateButtonsState();
        }

        private void PopulateList(bool showPresets)
        {
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

                    lock (_bitmapLock)
                    {
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap)
                        {
                            return;
                        }

                        Rectangle tileBounds = tile.Bounds;
                        int bytesPerPixel = 4;
                        int tileRowWidthInBytes = tileBounds.Width * bytesPerPixel;

                        BitmapData bmpData = _currentRenderingBitmap.LockBits(tileBounds, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);

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
        /// ИСПРАВЛЕННЫЙ МЕТОД: Асинхронно получает или рендерит плитку для предварительного просмотра.
        /// Этот метод использует механизм прогрессивного кэширования, чтобы избежать повторного рендеринга
        /// уже вычисленных плиток, и обеспечивает потокобезопасность при доступе к кешу.
        /// </summary>
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
                // Выполняем дорогостоящий рендеринг *вне* блокировки
                tileBuffer = await _ownerFractalForm.RenderPreviewTileAsync(state, tile, totalWidth, totalHeight, tileSize);

                // Повторно блокируем для безопасной записи в кеш
                lock (_previewCacheLock)
                {
                    // Проверяем еще раз на случай, если другой поток успел отрендерить эту плитку, пока мы были заняты
                    if (!_renderedTilesCache.Contains(tileCoord))
                    {
                        // LockBits на кешированном битмапе для записи
                        var bmpData = _cachedFullPreviewBitmap.LockBits(tile.Bounds, ImageLockMode.WriteOnly, _cachedFullPreviewBitmap.PixelFormat);
                        Marshal.Copy(tileBuffer, 0, bmpData.Scan0, tileBuffer.Length);
                        _cachedFullPreviewBitmap.UnlockBits(bmpData);

                        _renderedTilesCache.Add(tileCoord);
                    }
                }
            }
            // Шаг 3: Извлечение данных из кеша (если они там уже были)
            else
            {
                tileBuffer = new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                lock (_previewCacheLock)
                {
                    var bmpData = _cachedFullPreviewBitmap.LockBits(tile.Bounds, ImageLockMode.ReadOnly, _cachedFullPreviewBitmap.PixelFormat);
                    Marshal.Copy(bmpData.Scan0, tileBuffer, 0, tileBuffer.Length);
                    _cachedFullPreviewBitmap.UnlockBits(bmpData);
                }
            }

            return tileBuffer;
        }

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

        private void SaveLoadDialogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ClearPreview();
            _renderVisualizer?.Dispose();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (listBoxSaves.SelectedIndex >= 0 && _displayedItems != null && listBoxSaves.SelectedIndex < _displayedItems.Count)
            {
                _ownerFractalForm.LoadState(_displayedItems[listBoxSaves.SelectedIndex]);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

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

        private void cbPresets_CheckedChanged(object sender, EventArgs e)
        {
            var presetsCheckBox = sender as CheckBox;
            if (presetsCheckBox != null)
            {
                PopulateList(presetsCheckBox.Checked);
                UpdateButtonsState();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}

// --- КОНЕЦ ФАЙЛА SaveLoadDialogForm.cs ---