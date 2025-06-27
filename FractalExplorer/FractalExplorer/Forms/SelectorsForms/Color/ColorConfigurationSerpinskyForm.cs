using FractalExplorer.Utilities.SaveIO.ColorPalettes;

namespace FractalExplorer.SelectorsForms
{
    /// <summary>
    /// Представляет форму для настройки цветовых палитр фрактала Серпинского.
    /// Позволяет пользователю просматривать, создавать, редактировать, удалять
    /// и применять пользовательские цветовые палитры.
    /// </summary>
    public partial class ColorConfigurationSerpinskyForm : Form
    {
        #region Fields
        private readonly SerpinskyPaletteManager _paletteManager;
        private SerpinskyColorPalette _selectedPalette;
        #endregion

        #region Events
        /// <summary>
        /// Происходит, когда выбранная цветовая палитра применена.
        /// </summary>
        public event EventHandler PaletteApplied;
        #endregion

        #region Constructor
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ColorConfigurationSerpinskyForm"/>.
        /// </summary>
        /// <param name="paletteManager">Менеджер палитр, используемый для управления цветовыми схемами.</param>
        public ColorConfigurationSerpinskyForm(SerpinskyPaletteManager paletteManager)
        {
            InitializeComponent();
            _paletteManager = paletteManager;
        }
        #endregion

        #region Form Lifecycle
        /// <summary>
        /// Обрабатывает событие загрузки формы.
        /// Заполняет список палитр и выбирает активную палитру, если она установлена.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void ColorConfigurationSerpinskyForm_Load(object sender, EventArgs e)
        {
            PopulatePaletteList();
            if (_paletteManager.ActivePalette != null)
            {
                // Формируем отображаемое имя для поиска в ListBox,
                // учитывая, что встроенные палитры имеют суффикс "[Встроенная]".
                string displayNameToFind = _paletteManager.ActivePalette.IsBuiltIn
                    ? $"{_paletteManager.ActivePalette.Name} [Встроенная]"
                    : _paletteManager.ActivePalette.Name;
                lbPalettes.SelectedItem = displayNameToFind;
            }
        }
        #endregion

        #region Palette Management
        /// <summary>
        /// Заполняет список доступных цветовых палитр в ListBox.
        /// </summary>
        private void PopulatePaletteList()
        {
            lbPalettes.Items.Clear();
            foreach (var palette in _paletteManager.Palettes)
            {
                string displayName = palette.IsBuiltIn ? $"{palette.Name} [Встроенная]" : palette.Name;
                lbPalettes.Items.Add(displayName);
            }
        }

        /// <summary>
        /// Обрабатывает изменение выбранного элемента в списке палитр.
        /// Обновляет отображаемые детали палитры и состояние элементов управления.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void lbPalettes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbPalettes.SelectedIndex == -1)
            {
                return;
            }

            string selectedName = lbPalettes.SelectedItem.ToString().Replace(" [Встроенная]", "");
            _selectedPalette = _paletteManager.Palettes.FirstOrDefault(p => p.Name == selectedName);
            if (_selectedPalette == null)
            {
                return;
            }

            DisplayPaletteDetails();
            UpdateControlsState();
        }

        /// <summary>
        /// Отображает детали выбранной палитры (имя и цвета) в соответствующих элементах управления.
        /// </summary>
        private void DisplayPaletteDetails()
        {
            // Отписываемся от события TextChanged перед изменением текста,
            // чтобы избежать повторного вызова обработчика и лишних операций во время инициализации.
            txtName.TextChanged -= txtName_TextChanged;
            txtName.Text = _selectedPalette.Name;
            txtName.TextChanged += txtName_TextChanged;

            panelFractalColor.BackColor = _selectedPalette.FractalColor;
            panelBackgroundColor.BackColor = _selectedPalette.BackgroundColor;
        }

        /// <summary>
        /// Обновляет состояние активности элементов управления в зависимости от того,
        /// является ли выбранная палитра встроенной или пользовательской.
        /// </summary>
        private void UpdateControlsState()
        {
            if (_selectedPalette == null)
            {
                return;
            }
            // Отключаем возможность редактирования имени, цветов и удаления для встроенных палитр,
            // чтобы сохранить их целостность и предотвратить случайные изменения.
            bool isCustom = !_selectedPalette.IsBuiltIn;
            txtName.Enabled = isCustom;
            panelFractalColor.Enabled = isCustom;
            panelBackgroundColor.Enabled = isCustom;
            btnDelete.Enabled = isCustom;
        }

        /// <summary>
        /// Открывает диалоговое окно выбора цвета и применяет выбранный цвет.
        /// </summary>
        /// <param name="panel">Панель, чей фоновый цвет будет изменен.</param>
        /// <param name="setColorAction">Действие для установки нового цвета в палитре.</param>
        private void EditColor(Panel panel, Action<Color> setColorAction)
        {
            if (_selectedPalette == null || _selectedPalette.IsBuiltIn)
            {
                MessageBox.Show("Нельзя изменять встроенные палитры. Создайте новую, чтобы редактировать.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            colorDialog1.Color = panel.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                panel.BackColor = colorDialog1.Color;
                setColorAction(colorDialog1.Color);
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Обрабатывает событие клика по панели цвета фрактала, позволяя пользователю изменить цвет фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void panelFractalColor_Click(object sender, EventArgs e)
        {
            EditColor(panelFractalColor, color => _selectedPalette.FractalColor = color);
        }

        /// <summary>
        /// Обрабатывает событие клика по панели цвета фона, позволяя пользователю изменить цвет фона.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void panelBackgroundColor_Click(object sender, EventArgs e)
        {
            EditColor(panelBackgroundColor, color => _selectedPalette.BackgroundColor = color);
        }

        /// <summary>
        /// Обрабатывает изменение текста в поле для ввода имени палитры.
        /// Обновляет имя выбранной палитры и соответствующий элемент в списке.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void txtName_TextChanged(object sender, EventArgs e)
        {
            // Обновляем имя палитры и соответствующий элемент в списке ListBox,
            // только если это пользовательская палитра и изменение вызвано прямым вводом пользователя (элемент находится в фокусе).
            // Это предотвращает обновление при программном изменении текста (например, при загрузке палитры).
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn && txtName.Focused)
            {
                _selectedPalette.Name = txtName.Text;
                // Обновляем текст в ListBox, чтобы изменения имени были сразу видны.
                // Пересоздавать весь список не требуется, достаточно обновить конкретный элемент.
                lbPalettes.Items[lbPalettes.SelectedIndex] = txtName.Text;
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Новая".
        /// Создает новую пользовательскую палитру с уникальным именем и добавляет ее в список.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void btnNew_Click(object sender, EventArgs e)
        {
            string newName = "Новая палитра";
            int counter = 1;
            // Генерируем уникальное имя для новой палитры,
            // чтобы избежать конфликтов имен с существующими палитрами.
            while (_paletteManager.Palettes.Any(p => p.Name == newName))
            {
                newName = $"Новая палитра {++counter}";
            }
            var newPalette = new SerpinskyColorPalette { Name = newName };
            _paletteManager.Palettes.Add(newPalette);
            PopulatePaletteList(); // Перезагружаем список для отображения новой палитры.
            lbPalettes.SelectedItem = newPalette.Name; // Выбираем только что созданную палитру в списке.
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Удалить".
        /// Удаляет выбранную пользовательскую палитру после подтверждения пользователя.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn)
            {
                // Запрашиваем подтверждение перед удалением палитры,
                // так как это необратимая операция.
                if (MessageBox.Show($"Удалить палитру '{_selectedPalette.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _paletteManager.Palettes.Remove(_selectedPalette);
                    PopulatePaletteList(); // Обновляем список после удаления.
                    // Выбираем первый элемент в списке после удаления, чтобы всегда была выбрана палитра.
                    lbPalettes.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Сохранить".
        /// Сохраняет все пользовательские палитры через менеджер палитр.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            _paletteManager.SaveCustomPalettes();
            MessageBox.Show("Пользовательские палитры сохранены.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Применить".
        /// Устанавливает выбранную палитру как активную и вызывает событие <see cref="PaletteApplied"/>.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_selectedPalette != null)
            {
                _paletteManager.ActivePalette = _selectedPalette;
                // Вызываем событие PaletteApplied, чтобы главная форма или другие подписчики
                // могли отреагировать на изменение активной палитры и обновить отображение фрактала.
                PaletteApplied?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки "Закрыть".
        /// Закрывает текущую форму настройки цвета.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion
    }
}