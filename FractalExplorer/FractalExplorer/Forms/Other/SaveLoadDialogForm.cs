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

namespace FractalExplorer.Forms
{
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
        // ------------------------------------

        public SaveLoadDialogForm(ISaveLoadCapableFractal ownerFractalForm)
        {
            InitializeComponent();
            _ownerFractalForm = ownerFractalForm ?? throw new ArgumentNullException(nameof(ownerFractalForm));
            this.Text = $"Сохранение/Загрузка: {_ownerFractalForm.FractalTypeIdentifier}";

            // Инициализация компонентов для рендера
            _renderVisualizer = new RenderVisualizerComponent(TILE_SIZE);
            _renderVisualizer.NeedsRedraw += () => { if (pictureBoxPreview.IsHandleCreated) pictureBoxPreview.Invalidate(); };
            pictureBoxPreview.Paint += PictureBoxPreview_Paint;

            // Динамический поиск и подписка на чекбокс, если он есть
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
            _previewRenderCts?.Cancel(); // Отменяем рендер при смене списка

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
                StartTiledPreviewRender(selectedState); // Используем новый метод
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

                    byte[] tileBuffer = await _ownerFractalForm.RenderPreviewTileAsync(state, tile, pictureBoxPreview.Width, pictureBoxPreview.Height, TILE_SIZE);

                    ct.ThrowIfCancellationRequested();

                    lock (_bitmapLock)
                    {
                        if (ct.IsCancellationRequested || _currentRenderingBitmap != newRenderingBitmap) return;
                        BitmapData bmpData = _currentRenderingBitmap.LockBits(tile.Bounds, ImageLockMode.WriteOnly, _currentRenderingBitmap.PixelFormat);
                        Marshal.Copy(tileBuffer, 0, bmpData.Scan0, tileBuffer.Length);
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
            catch (OperationCanceledException) { }
            catch (Exception ex) { Console.WriteLine($"Ошибка мозаичного рендера превью: {ex.Message}"); }
            finally
            {
                _isRenderingPreview = false;
                _renderVisualizer.NotifyRenderSessionComplete();
                if (pictureBoxPreview.IsHandleCreated) pictureBoxPreview.Invalidate();
            }
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

        // --- Остальные методы ---
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
            if (presetsCheckBox.Checked) return;

            string saveName = textBoxSaveName.Text.Trim();
            if (string.IsNullOrWhiteSpace(saveName)) { MessageBox.Show("Пожалуйста, введите имя для сохранения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var userSaves = _ownerFractalForm.LoadAllSavesForThisType();
            var existingSave = userSaves.FirstOrDefault(s => s.SaveName.Equals(saveName, StringComparison.OrdinalIgnoreCase));
            if (existingSave != null)
            {
                if (MessageBox.Show($"Сохранение с именем '{saveName}' уже существует. Перезаписать?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    userSaves.Remove(existingSave);
                }
                else { return; }
            }

            var newState = _ownerFractalForm.GetCurrentStateForSave(saveName);
            userSaves.Add(newState);
            _ownerFractalForm.SaveAllSavesForThisType(userSaves);

            PopulateList(false);
            int newIndex = _displayedItems.FindIndex(s => s.SaveName == saveName && s.Timestamp == newState.Timestamp);
            if (newIndex != -1) { listBoxSaves.SelectedIndex = newIndex; }
        }

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