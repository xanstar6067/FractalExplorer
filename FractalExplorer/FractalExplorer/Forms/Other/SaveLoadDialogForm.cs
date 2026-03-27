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
using FractalExplorer.Utilities.RenderUtilities;

using FractalExplorer.Utilities.Theme;
namespace FractalExplorer.Forms
{
    /// <summary>
    /// Представляет диалоговое окно для сохранения и загрузки состояний фракталов.
    /// Предоставляет функциональность для управления пользовательскими сохранениями и предустановками,
    /// включая асинхронный рендер предварительного просмотра.
    /// </summary>
    public partial class SaveLoadDialogForm : Form
    {
        private readonly ISaveLoadCapableFractal _ownerFractalForm;
        private List<FractalSaveStateBase> _displayedItems;

        // --- Поля для рендера превью ---
        private CancellationTokenSource _previewRenderCts;
        private Bitmap _previewBitmap;
        private readonly object _bitmapLock = new object();
        private long _previewRequestId = 0;
        private const int PREVIEW_TARGET_TILE_COUNT = 120;
        private const int PREVIEW_MIN_TILE_SIZE = 24;
        private const int PREVIEW_MAX_TILE_SIZE = 40;
        private bool _isPreviewRendering = false;
        private int _previewTotalTiles = 0;
        private int _previewRenderedTiles = 0;
        private RenderVisualizerComponent _previewRenderVisualizer;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SaveLoadDialogForm"/>.
        /// </summary>
        /// <param name="ownerFractalForm">Экземпляр формы фрактала, поддерживающий сохранение и загрузку.</param>
        /// <exception cref="ArgumentNullException">Вызывается, если <paramref name="ownerFractalForm"/> равен null.</exception>
        public SaveLoadDialogForm(ISaveLoadCapableFractal ownerFractalForm)
        {
            InitializeComponent();
            ThemeManager.RegisterForm(this);
            _ownerFractalForm = ownerFractalForm ?? throw new ArgumentNullException(nameof(ownerFractalForm));
            this.Text = $"Сохранение/Загрузка: {_ownerFractalForm.FractalTypeIdentifier}";
            pictureBoxPreview.SizeChanged += pictureBoxPreview_SizeChanged;
            pictureBoxPreview.Paint += pictureBoxPreview_Paint;

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
            CancelAndDisposePreviewCts(); // Отменяем текущий рендер, так как список обновляется.

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
            CancelAndDisposePreviewCts();

            if (listBoxSaves.SelectedIndex >= 0 && _displayedItems != null && listBoxSaves.SelectedIndex < _displayedItems.Count)
            {
                var selectedState = _displayedItems[listBoxSaves.SelectedIndex];
                textBoxSaveName.Text = selectedState.SaveName;
                StartPreviewRender(selectedState);
            }
            else
            {
                ClearPreview();
            }
            UpdateButtonsState();
        }

        private void EnsurePreviewRenderVisualizer(int tileSize)
        {
            if (_previewRenderVisualizer != null)
            {
                _previewRenderVisualizer.NeedsRedraw -= OnPreviewVisualizerNeedsRedraw;
                _previewRenderVisualizer.Dispose();
            }

            _previewRenderVisualizer = new RenderVisualizerComponent(tileSize);
            _previewRenderVisualizer.NeedsRedraw += OnPreviewVisualizerNeedsRedraw;
        }

        private void OnPreviewVisualizerNeedsRedraw()
        {
            if (pictureBoxPreview.IsHandleCreated && !pictureBoxPreview.IsDisposed)
            {
                pictureBoxPreview.BeginInvoke((Action)(() => pictureBoxPreview.Invalidate()));
            }
        }

        /// <summary>
        /// Запускает асинхронный рендер превью для указанного состояния фрактала.
        /// Использует тот же путь RenderPreview, что и у владельца формы фрактала.
        /// </summary>
        /// <param name="state">Состояние фрактала для рендера превью.</param>
        private async void StartPreviewRender(FractalSaveStateBase state)
        {
            if (state == null || pictureBoxPreview.Width <= 0 || pictureBoxPreview.Height <= 0)
            {
                ClearPreview();
                return;
            }

            CancelAndDisposePreviewCts();
            _previewRenderCts = new CancellationTokenSource();
            var token = _previewRenderCts.Token;
            long requestId = Interlocked.Increment(ref _previewRequestId);

            try
            {
                int previewWidth = pictureBoxPreview.Width;
                int previewHeight = pictureBoxPreview.Height;
                int previewTileSize = GetPreviewTileSize(previewWidth, previewHeight);
                Bitmap renderedPreview = new Bitmap(previewWidth, previewHeight, PixelFormat.Format32bppArgb);
                var tiles = GeneratePreviewTiles(previewWidth, previewHeight, previewTileSize);
                int tileConcurrency = Math.Max(1, Math.Min(Environment.ProcessorCount, 4));
                var dispatcher = new TileRenderDispatcher(tiles, tileConcurrency, RenderPatternSettings.SelectedPattern);
                EnsurePreviewRenderVisualizer(previewTileSize);
                _previewRenderVisualizer?.NotifyRenderSessionStart();
                _previewTotalTiles = tiles.Count;
                _previewRenderedTiles = 0;
                _isPreviewRendering = true;

                using (Graphics g = Graphics.FromImage(renderedPreview))
                {
                    g.Clear(Color.Black);
                }

                lock (_bitmapLock)
                {
                    _previewBitmap = renderedPreview;
                    if (!pictureBoxPreview.IsDisposed)
                    {
                        var oldImage = pictureBoxPreview.Image;
                        pictureBoxPreview.Image = _previewBitmap;
                        if (oldImage != null && !ReferenceEquals(oldImage, _previewBitmap))
                        {
                            oldImage.Dispose();
                        }
                    }
                }

                await dispatcher.RenderAsync(async (tile, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    _previewRenderVisualizer?.NotifyTileRenderStart(tile.Bounds);
                    byte[] tileBuffer = await _ownerFractalForm.RenderPreviewTileAsync(state, tile, previewWidth, previewHeight, previewTileSize);
                    ct.ThrowIfCancellationRequested();
                    await InvokeOnUiThreadAsync(() =>
                    {
                        if (ct.IsCancellationRequested
                            || requestId != Interlocked.Read(ref _previewRequestId)
                            || this.IsDisposed
                            || pictureBoxPreview.IsDisposed)
                        {
                            return;
                        }

                        lock (_bitmapLock)
                        {
                            if (!ReferenceEquals(_previewBitmap, renderedPreview))
                            {
                                return;
                            }

                            CopyTileBufferToBitmap(renderedPreview, tile, tileBuffer);
                        }

                        _previewRenderVisualizer?.NotifyTileRenderComplete(tile.Bounds);
                        Interlocked.Increment(ref _previewRenderedTiles);
                        pictureBoxPreview.Invalidate();
                    });
                }, token);
                token.ThrowIfCancellationRequested();

                if (requestId != Interlocked.Read(ref _previewRequestId))
                {
                    renderedPreview?.Dispose();
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                // Отмена — штатное поведение.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка рендера превью: {ex.Message}");
            }
            finally
            {
                _isPreviewRendering = false;
                _previewRenderVisualizer?.NotifyRenderSessionComplete();
                if (!pictureBoxPreview.IsDisposed)
                {
                    pictureBoxPreview.Invalidate();
                }
            }
        }

        private void pictureBoxPreview_Paint(object sender, PaintEventArgs e)
        {
            if (!_isPreviewRendering)
            {
                return;
            }

            _previewRenderVisualizer?.DrawVisualization(e.Graphics);

            int rendered = Math.Max(0, Interlocked.CompareExchange(ref _previewRenderedTiles, 0, 0));
            int total = Math.Max(1, _previewTotalTiles);
            int percent = Math.Min(100, (int)Math.Round(rendered * 100.0 / total));
            string overlayText = $"Рендер превью: {percent}%";
            const int overlayPadding = 6;
            Size textSize = TextRenderer.MeasureText(overlayText, this.Font);
            Rectangle overlayBounds = new Rectangle(
                overlayPadding,
                overlayPadding,
                textSize.Width + overlayPadding * 2,
                textSize.Height + overlayPadding * 2);

            using (var background = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            {
                e.Graphics.FillRectangle(background, overlayBounds);
            }

            TextRenderer.DrawText(
                e.Graphics,
                overlayText,
                this.Font,
                new Rectangle(overlayBounds.X + overlayPadding, overlayBounds.Y + overlayPadding, overlayBounds.Width, overlayBounds.Height),
                Color.White,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
        }

        private Task InvokeOnUiThreadAsync(Action action)
        {
            if (action == null || IsDisposed)
            {
                return Task.CompletedTask;
            }

            if (!InvokeRequired)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            try
            {
                BeginInvoke((Action)(() =>
                {
                    try
                    {
                        if (!IsDisposed)
                        {
                            action();
                        }
                        tcs.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            return tcs.Task;
        }

        private static int GetPreviewTileSize(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return PREVIEW_MIN_TILE_SIZE;
            }

            int estimatedTileSize = (int)Math.Sqrt((width * height) / (double)PREVIEW_TARGET_TILE_COUNT);
            return Math.Max(PREVIEW_MIN_TILE_SIZE, Math.Min(PREVIEW_MAX_TILE_SIZE, estimatedTileSize));
        }

        private static List<TileInfo> GeneratePreviewTiles(int width, int height, int tileSize)
        {
            var tiles = new List<TileInfo>();
            if (width <= 0 || height <= 0 || tileSize <= 0)
            {
                return tiles;
            }

            for (int y = 0; y < height; y += tileSize)
            {
                for (int x = 0; x < width; x += tileSize)
                {
                    int currentTileWidth = Math.Min(tileSize, width - x);
                    int currentTileHeight = Math.Min(tileSize, height - y);
                    tiles.Add(new TileInfo(x, y, currentTileWidth, currentTileHeight));
                }
            }

            return tiles;
        }

        private static void CopyTileBufferToBitmap(Bitmap bitmap, TileInfo tile, byte[] tileBuffer)
        {
            if (bitmap == null || tileBuffer == null || tileBuffer.Length == 0)
            {
                return;
            }

            const int bytesPerPixel = 4;
            var bitmapRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var tileRect = tile.Bounds;
            tileRect.Intersect(bitmapRect);

            if (tileRect.Width <= 0 || tileRect.Height <= 0)
            {
                return;
            }

            BitmapData bitmapData = null;
            try
            {
                bitmapData = bitmap.LockBits(tileRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                int sourceTileWidthInBytes = tile.Bounds.Width * bytesPerPixel;

                for (int y = 0; y < tileRect.Height; y++)
                {
                    IntPtr destinationPointer = bitmapData.Scan0 + y * bitmapData.Stride;
                    int sourceOffset = ((y + tileRect.Y) - tile.Bounds.Y) * sourceTileWidthInBytes + ((tileRect.X - tile.Bounds.X) * bytesPerPixel);
                    Marshal.Copy(tileBuffer, sourceOffset, destinationPointer, tileRect.Width * bytesPerPixel);
                }
            }
            finally
            {
                if (bitmapData != null)
                {
                    bitmap.UnlockBits(bitmapData);
                }
            }
        }

        /// <summary>
        /// Очищает текущее изображение превью и отменяет активный процесс рендеринга.
        /// </summary>
        private void ClearPreview()
        {
            CancelAndDisposePreviewCts();
            _isPreviewRendering = false;
            _previewRenderVisualizer?.NotifyRenderSessionComplete();
            _previewTotalTiles = 0;
            _previewRenderedTiles = 0;
            lock (_bitmapLock)
            {
                _previewBitmap?.Dispose();
                _previewBitmap = null;
            }
            if (!pictureBoxPreview.IsDisposed)
            {
                var oldImage = pictureBoxPreview.Image;
                pictureBoxPreview.Image = null;
                oldImage?.Dispose();
            }
        }

        /// <summary>
        /// Обрабатывает событие закрытия формы, освобождая ресурсы.
        /// </summary>
        private void SaveLoadDialogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CancelAndDisposePreviewCts();
            ClearPreview();
            if (_previewRenderVisualizer != null)
            {
                _previewRenderVisualizer.NeedsRedraw -= OnPreviewVisualizerNeedsRedraw;
                _previewRenderVisualizer.Dispose();
                _previewRenderVisualizer = null;
            }
        }

        /// <summary>
        /// Отменяет текущий token source рендера превью, освобождает его ресурсы и обнуляет ссылку.
        /// </summary>
        private void CancelAndDisposePreviewCts()
        {
            if (_previewRenderCts == null)
            {
                return;
            }

            try
            {
                _previewRenderCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // CTS уже освобожден в другом участке кода.
            }

            _previewRenderCts.Dispose();
            _previewRenderCts = null;
        }

        private void pictureBoxPreview_SizeChanged(object sender, EventArgs e)
        {
            if (listBoxSaves.SelectedIndex >= 0
                && _displayedItems != null
                && listBoxSaves.SelectedIndex < _displayedItems.Count)
            {
                StartPreviewRender(_displayedItems[listBoxSaves.SelectedIndex]);
            }
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
