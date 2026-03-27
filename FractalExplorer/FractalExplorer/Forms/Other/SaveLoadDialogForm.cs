using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;

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
                Bitmap renderedPreview = await Task.Run(() => _ownerFractalForm.RenderPreview(state, previewWidth, previewHeight), token);
                token.ThrowIfCancellationRequested();

                if (requestId != Interlocked.Read(ref _previewRequestId))
                {
                    renderedPreview?.Dispose();
                    return;
                }

                lock (_bitmapLock)
                {
                    _previewBitmap?.Dispose();
                    _previewBitmap = renderedPreview;
                }

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
            catch (OperationCanceledException)
            {
                // Отмена — штатное поведение.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка рендера превью: {ex.Message}");
            }
        }

        /// <summary>
        /// Очищает текущее изображение превью и отменяет активный процесс рендеринга.
        /// </summary>
        private void ClearPreview()
        {
            CancelAndDisposePreviewCts();
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
