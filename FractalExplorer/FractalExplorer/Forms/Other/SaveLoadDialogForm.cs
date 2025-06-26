using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FractalExplorer.Utilities; // Для PresetManager
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;

namespace FractalExplorer.Forms
{
    public partial class SaveLoadDialogForm : Form
    {
        private readonly ISaveLoadCapableFractal _ownerFractalForm;

        // _saves теперь всегда будет хранить ТЕКУЩИЙ отображаемый список.
        // Это упрощает логику, так как не нужно передавать списки туда-сюда.
        private List<FractalSaveStateBase> _displayedItems;

        private Bitmap _currentPreviewBitmap;

        public SaveLoadDialogForm(ISaveLoadCapableFractal ownerFractalForm)
        {
            InitializeComponent(); // Здесь дизайнер уже создал cbPresets
            _ownerFractalForm = ownerFractalForm ?? throw new ArgumentNullException(nameof(ownerFractalForm));
            this.Text = $"Сохранение/Загрузка: {_ownerFractalForm.FractalTypeIdentifier}";

            // ИЗМЕНЕНИЕ: Убираем отсюда весь код программного создания CheckBox.
            // Вместо этого добавляем обработчик события для чекбокса, созданного дизайнером.
            // Убедитесь, что имя контрола в дизайнере - "cbPresets".
            // Если вы назвали его по-другому, измените "this.cbPresets" на правильное имя.
            var presetsCheckBox = this.Controls.Find("cbPresets", true).FirstOrDefault() as CheckBox;
            if (presetsCheckBox != null)
            {
                presetsCheckBox.CheckedChanged += new System.EventHandler(this.cbPresets_CheckedChanged);
            }
        }

        private void SaveLoadDialogForm_Load(object sender, EventArgs e)
        {
            // По умолчанию показываем пользовательские сохранения
            PopulateList(false);
            UpdateButtonsState();
        }

        private void PopulateList(bool showPresets)
        {
            if (showPresets)
            {
                // Загружаем из кода через PresetManager
                _displayedItems = PresetManager.GetPresetsFor(_ownerFractalForm.FractalTypeIdentifier)
                                      .OrderBy(p => p.SaveName)
                                      .ToList();
            }
            else
            {
                // Загружаем из файла
                _displayedItems = _ownerFractalForm.LoadAllSavesForThisType()
                                      .OrderByDescending(s => s.Timestamp)
                                      .ToList();
            }

            listBoxSaves.Items.Clear();
            if (_displayedItems != null)
            {
                foreach (var item in _displayedItems)
                {
                    // Для пресетов не показываем дату
                    string displayText = showPresets
                        ? item.SaveName
                        : $"{item.SaveName} ({item.Timestamp:yyyy-MM-dd HH:mm:ss})";
                    listBoxSaves.Items.Add(displayText);
                }
            }

            if (listBoxSaves.Items.Count > 0)
            {
                listBoxSaves.SelectedIndex = 0;
            }
            else
            {
                // Если список пуст, очищаем превью и поле имени
                ClearPreview();
                textBoxSaveName.Text = "";
            }
        }

        private void listBoxSaves_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearPreview();
            if (listBoxSaves.SelectedIndex >= 0 && _displayedItems != null && listBoxSaves.SelectedIndex < _displayedItems.Count)
            {
                var selectedState = _displayedItems[listBoxSaves.SelectedIndex];
                textBoxSaveName.Text = selectedState.SaveName;
                RenderAndDisplayPreview(selectedState);
            }
            UpdateButtonsState();
        }

        // --- Методы RenderAndDisplayPreview и ClearPreview остаются без изменений ---
        private async void RenderAndDisplayPreview(FractalSaveStateBase state)
        {
            if (pictureBoxPreview.Width <= 0 || pictureBoxPreview.Height <= 0 || state == null)
            {
                ClearPreview();
                return;
            }

            Bitmap loadingBmp = new Bitmap(pictureBoxPreview.Width, pictureBoxPreview.Height);
            using (var g = Graphics.FromImage(loadingBmp))
            {
                g.Clear(Color.LightGray);
                TextRenderer.DrawText(g, "Рендеринг превью...", Font, pictureBoxPreview.ClientRectangle, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            _currentPreviewBitmap?.Dispose();
            _currentPreviewBitmap = loadingBmp;
            pictureBoxPreview.Image = _currentPreviewBitmap;

            try
            {
                Bitmap previewBitmap = await Task.Run(() => _ownerFractalForm.RenderPreview(state, pictureBoxPreview.Width, pictureBoxPreview.Height));

                if (IsHandleCreated && !IsDisposed && pictureBoxPreview.IsHandleCreated && !pictureBoxPreview.IsDisposed)
                {
                    _currentPreviewBitmap?.Dispose();
                    _currentPreviewBitmap = previewBitmap;
                    pictureBoxPreview.Image = _currentPreviewBitmap;
                }
                else
                {
                    previewBitmap?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка рендера превью: {ex.Message}");
                Bitmap errorBmp = new Bitmap(pictureBoxPreview.Width, pictureBoxPreview.Height);
                using (var g = Graphics.FromImage(errorBmp))
                {
                    g.Clear(Color.MistyRose);
                    TextRenderer.DrawText(g, "Ошибка превью", Font, pictureBoxPreview.ClientRectangle, Color.Red, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                if (IsHandleCreated && !IsDisposed && pictureBoxPreview.IsHandleCreated && !pictureBoxPreview.IsDisposed)
                {
                    _currentPreviewBitmap?.Dispose();
                    _currentPreviewBitmap = errorBmp;
                    pictureBoxPreview.Image = _currentPreviewBitmap;
                }
                else
                {
                    errorBmp.Dispose();
                }
            }
        }

        private void ClearPreview()
        {
            _currentPreviewBitmap?.Dispose();
            _currentPreviewBitmap = null;
            pictureBoxPreview.Image = null;
        }

        // --- Остальные методы (btnLoad_Click, btnSaveAsNew_Click, btnDelete_Click) ---

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
            var presetsCheckBox = this.Controls.Find("cbPresets", true).FirstOrDefault() as CheckBox;
            if (presetsCheckBox.Checked) return; // Нельзя сохранять в режиме пресетов

            string saveName = textBoxSaveName.Text.Trim();
            if (string.IsNullOrWhiteSpace(saveName))
            {
                MessageBox.Show("Пожалуйста, введите имя для сохранения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxSaveName.Focus();
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

            PopulateList(false); // Обновляем список пользовательских сохранений

            // Находим и выбираем только что сохраненный элемент
            int newIndex = _displayedItems.FindIndex(s => s.SaveName == saveName && s.Timestamp == newState.Timestamp);
            if (newIndex != -1)
            {
                listBoxSaves.SelectedIndex = newIndex;
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var presetsCheckBox = this.Controls.Find("cbPresets", true).FirstOrDefault() as CheckBox;
            if (presetsCheckBox.Checked) return; // Нельзя удалять пресеты

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
                        PopulateList(false); // Перезагружаем список
                    }
                }
            }
        }

        private void UpdateButtonsState()
        {
            var presetsCheckBox = this.Controls.Find("cbPresets", true).FirstOrDefault() as CheckBox;
            bool itemSelected = listBoxSaves.SelectedIndex != -1;
            bool presetsMode = presetsCheckBox.Checked;

            btnLoad.Enabled = itemSelected;
            btnDelete.Enabled = itemSelected && !presetsMode;
            btnSaveAsNew.Enabled = !presetsMode;
            textBoxSaveName.Enabled = !presetsMode;
        }

        // ИЗМЕНЕНИЕ: Обработчик события теперь привязан к контролу, созданному дизайнером
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

        private void SaveLoadDialogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ClearPreview();
        }
    }
}