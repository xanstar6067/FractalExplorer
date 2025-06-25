using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks; // Для Task
using System.Windows.Forms;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.SaveIO; // Для ISaveLoadCapableFractal, FractalSaveStateBase

namespace FractalExplorer.Forms // Убедись, что namespace соответствует твоему
{
    public partial class SaveLoadDialogForm : Form
    {
        private readonly ISaveLoadCapableFractal _ownerFractalForm;
        private List<FractalSaveStateBase> _saves;
        private Bitmap _currentPreviewBitmap; // Для корректного Dispose

        public SaveLoadDialogForm(ISaveLoadCapableFractal ownerFractalForm)
        {
            InitializeComponent();
            _ownerFractalForm = ownerFractalForm ?? throw new ArgumentNullException(nameof(ownerFractalForm));
            // Устанавливаем заголовок окна в зависимости от типа фрактала
            this.Text = $"Сохранение/Загрузка: {_ownerFractalForm.FractalTypeIdentifier}";
        }

        private void SaveLoadDialogForm_Load(object sender, EventArgs e)
        {
            LoadSavesList();
            UpdateButtonsState();
        }

        private void LoadSavesList()
        {
            _saves = _ownerFractalForm.LoadAllSavesForThisType();
            // Сортируем по дате, новые вверху (SaveFileManager теперь это делает при сохранении, но для надежности можно и здесь)
            // _saves = _saves.OrderByDescending(s => s.Timestamp).ToList(); 

            listBoxSaves.Items.Clear();
            if (_saves != null) // Добавим проверку на null
            {
                foreach (var save in _saves)
                {
                    listBoxSaves.Items.Add($"{save.SaveName} ({save.Timestamp:yyyy-MM-dd HH:mm:ss})");
                }
            }

            if (listBoxSaves.Items.Count > 0)
            {
                listBoxSaves.SelectedIndex = 0;
            }
            else
            {
                ClearPreview(); // Очищаем превью, если список пуст
            }
        }

        private void listBoxSaves_SelectedIndexChanged(object sender, EventArgs e)
        {
            ClearPreview(); // Очищаем предыдущее превью
            if (listBoxSaves.SelectedIndex >= 0 && _saves != null && listBoxSaves.SelectedIndex < _saves.Count)
            {
                var selectedState = _saves[listBoxSaves.SelectedIndex];
                textBoxSaveName.Text = selectedState.SaveName; // Предзаполняем имя для удобства
                RenderAndDisplayPreview(selectedState);
            }
            UpdateButtonsState();
        }

        private async void RenderAndDisplayPreview(FractalSaveStateBase state)
        {
            if (pictureBoxPreview.Width <= 0 || pictureBoxPreview.Height <= 0 || state == null)
            {
                ClearPreview();
                return;
            }

            // Показываем заглушку "Загрузка..."
            Bitmap loadingBmp = new Bitmap(pictureBoxPreview.Width, pictureBoxPreview.Height);
            using (var g = Graphics.FromImage(loadingBmp))
            {
                g.Clear(Color.LightGray);
                TextRenderer.DrawText(g, "Рендеринг превью...", Font, pictureBoxPreview.ClientRectangle, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            pictureBoxPreview.Image = loadingBmp; // Старый _currentPreviewBitmap будет освобожден при присваивании нового или в ClearPreview

            try
            {
                // Асинхронный рендер превью
                Bitmap previewBitmap = await Task.Run(() => _ownerFractalForm.RenderPreview(state, pictureBoxPreview.Width, pictureBoxPreview.Height));

                // Проверяем, что контрол все еще существует перед обновлением UI
                if (pictureBoxPreview.IsHandleCreated && !pictureBoxPreview.IsDisposed)
                {
                    _currentPreviewBitmap?.Dispose(); // Освобождаем старый, если он был
                    _currentPreviewBitmap = previewBitmap;
                    pictureBoxPreview.Image = _currentPreviewBitmap;
                }
                else
                {
                    previewBitmap?.Dispose(); // Если контрола нет, просто освобождаем новый битмап
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
                _currentPreviewBitmap?.Dispose(); // Освобождаем предыдущий битмап (мог быть loadingBmp или старый _currentPreviewBitmap)
                _currentPreviewBitmap = errorBmp;
                if (pictureBoxPreview.IsHandleCreated && !pictureBoxPreview.IsDisposed)
                {
                    pictureBoxPreview.Image = _currentPreviewBitmap;
                }
            }
        }

        private void ClearPreview()
        {
            // Освобождаем предыдущий битмап перед тем, как очистить Image
            _currentPreviewBitmap?.Dispose();
            _currentPreviewBitmap = null;
            pictureBoxPreview.Image = null;
        }


        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (listBoxSaves.SelectedIndex >= 0 && _saves != null && listBoxSaves.SelectedIndex < _saves.Count)
            {
                _ownerFractalForm.LoadState(_saves[listBoxSaves.SelectedIndex]);
                this.DialogResult = DialogResult.OK; // Устанавливаем результат диалога
                this.Close();
            }
        }

        private void btnSaveAsNew_Click(object sender, EventArgs e)
        {
            string saveName = textBoxSaveName.Text.Trim();
            if (string.IsNullOrWhiteSpace(saveName))
            {
                MessageBox.Show("Пожалуйста, введите имя для сохранения.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxSaveName.Focus();
                return;
            }

            if (_saves == null) _saves = new List<FractalSaveStateBase>();

            // Проверка на дубликат имени
            var existingSave = _saves.FirstOrDefault(s => s.SaveName.Equals(saveName, StringComparison.OrdinalIgnoreCase));
            if (existingSave != null)
            {
                if (MessageBox.Show($"Сохранение с именем '{saveName}' уже существует. Перезаписать?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    // Удаляем старое сохранение с таким же именем
                    _saves.Remove(existingSave);
                }
                else
                {
                    return;
                }
            }

            var newState = _ownerFractalForm.GetCurrentStateForSave(saveName);
            _saves.Add(newState);
            _ownerFractalForm.SaveAllSavesForThisType(_saves); // Сохраняем весь обновленный список

            // Перезагружаем и выбираем новый элемент
            int previouslySelectedIndex = listBoxSaves.SelectedIndex; // Сохраняем, на всякий случай
            LoadSavesList();

            // Пытаемся найти и выбрать только что сохраненный элемент
            int newIndex = -1;
            for (int i = 0; i < _saves.Count; i++) // Ищем в _saves, так как listBoxSaves.Items - это строки
            {
                if (_saves[i].SaveName == saveName && _saves[i].Timestamp == newState.Timestamp)
                {
                    newIndex = i;
                    break;
                }
            }
            if (newIndex != -1 && newIndex < listBoxSaves.Items.Count)
            {
                listBoxSaves.SelectedIndex = newIndex;
            }
            else if (listBoxSaves.Items.Count > 0) // Если не нашли, выбираем первый или последний
            {
                listBoxSaves.SelectedIndex = 0; // или _saves.Count - 1
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (listBoxSaves.SelectedIndex >= 0 && _saves != null && listBoxSaves.SelectedIndex < _saves.Count)
            {
                var stateToDelete = _saves[listBoxSaves.SelectedIndex];
                if (MessageBox.Show($"Вы уверены, что хотите удалить сохранение '{stateToDelete.SaveName}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _saves.RemoveAt(listBoxSaves.SelectedIndex);
                    _ownerFractalForm.SaveAllSavesForThisType(_saves);
                    LoadSavesList(); // Перезагружаем список
                }
            }
        }

        private void UpdateButtonsState()
        {
            bool itemSelected = listBoxSaves.SelectedIndex != -1;
            btnLoad.Enabled = itemSelected;
            btnDelete.Enabled = itemSelected;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel; // Устанавливаем результат диалога
            this.Close();
        }

        // Важно освобождать ресурсы при закрытии формы
        private void SaveLoadDialogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ClearPreview();
        }
    }
}