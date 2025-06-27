using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using System.Drawing.Drawing2D;

namespace FractalExplorer.Utilities
{
    /// <summary>
    /// Базовая форма для настройки цветовых палитр фракталов.
    /// Позволяет пользователю управлять встроенными и пользовательскими палитрами:
    /// выбирать, просматривать, создавать новые, редактировать цвета,
    /// и применять выбранную палитру.
    /// </summary>
    public partial class ColorConfigurationMandelbrotFamilyForm : Form
    {
        #region Fields

        /// <summary>
        /// Менеджер палитр, управляющий доступными палитрами.
        /// </summary>
        private readonly ColorPaletteMandelbrotFamily _paletteManager;

        /// <summary>
        /// Текущая выбранная палитра в списке.
        /// </summary>
        private PaletteManagerMandelbrotFamily _selectedPalette;

        #endregion

        #region Events

        /// <summary>
        /// Событие, которое возникает при применении палитры к главной форме.
        /// </summary>
        public event EventHandler PaletteApplied;

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ColorConfigurationMandelbrotFamilyForm"/>.
        /// </summary>
        /// <param name="paletteManager">Менеджер палитр для управления палитрами.</param>
        public ColorConfigurationMandelbrotFamilyForm(ColorPaletteMandelbrotFamily paletteManager)
        {
            InitializeComponent();
            _paletteManager = paletteManager;
        }

        #endregion

        #region UI Initialization

        /// <summary>
        /// Обработчик события загрузки формы.
        /// Заполняет список палитр и выбирает активную палитру.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void ColorConfigurationForm_Load(object sender, EventArgs e)
        {
            PopulatePaletteList();

            if (_paletteManager.ActivePalette != null)
            {
                // Формируем отображаемое имя для поиска в списке, учитывая встроенные палитры
                string displayNameToFind = _paletteManager.ActivePalette.IsBuiltIn
                    ? $"{_paletteManager.ActivePalette.Name} [Встроенная]"
                    : _paletteManager.ActivePalette.Name;

                lbPalettes.SelectedItem = displayNameToFind;
            }
        }

        /// <summary>
        /// Заполняет ListBox доступными палитрами из менеджера палитр.
        /// </summary>
        private void PopulatePaletteList()
        {
            lbPalettes.Items.Clear();
            foreach (var palette in _paletteManager.Palettes)
            {
                // Добавляем пометку "[Встроенная]" для встроенных палитр
                string displayName = palette.IsBuiltIn ? $"{palette.Name} [Встроенная]" : palette.Name;
                lbPalettes.Items.Add(displayName);
            }
        }

        #endregion

        #region UI Display & Update

        /// <summary>
        /// Обработчик события изменения выбранного элемента в списке палитр.
        /// Обновляет отображаемые детали палитры и состояние элементов управления.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void lbPalettes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbPalettes.SelectedIndex == -1)
            {
                return;
            }

            // Извлекаем реальное имя палитры, убирая пометку "[Встроенная]"
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
        /// Отображает детали выбранной палитры (имя, градиент, цвета) на форме.
        /// </summary>
        private void DisplayPaletteDetails()
        {
            // Временно отписываемся от события, чтобы избежать обработки программного изменения текста
            txtName.TextChanged -= txtName_TextChanged;
            txtName.Text = _selectedPalette.Name;
            txtName.TextChanged += txtName_TextChanged;

            checkIsGradient.Checked = _selectedPalette.IsGradient;

            // Обновляем список цветовых стопов
            lbColorStops.Items.Clear();
            foreach (var color in _selectedPalette.Colors)
            {
                lbColorStops.Items.Add(color.Name);
            }
            panelPreview.Invalidate(); // Запрашиваем перерисовку панели предпросмотра
        }

        /// <summary>
        /// Обновляет состояние элементов управления (включено/отключено)
        /// в зависимости от того, является ли выбранная палитра пользовательской или встроенной.
        /// </summary>
        private void UpdateControlsState()
        {
            if (_selectedPalette == null)
            {
                return;
            }
            bool isCustom = !_selectedPalette.IsBuiltIn;
            txtName.Enabled = isCustom;
            checkIsGradient.Enabled = isCustom;
            lbColorStops.Enabled = isCustom;
            btnAddColor.Enabled = isCustom;
            btnEditColor.Enabled = isCustom;
            btnRemoveColor.Enabled = isCustom;
            btnDelete.Enabled = isCustom;
        }

        /// <summary>
        /// Обработчик события отрисовки панели предпросмотра палитры.
        /// Отображает градиент или сплошной цвет выбранной палитры.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события рисования.</param>
        private void panelPreview_Paint(object sender, PaintEventArgs e)
        {
            if (_selectedPalette == null || _selectedPalette.Colors.Count == 0)
            {
                // Если палитра не выбрана или пуста, отображаем стандартный градиент
                using (var linearGradientBrush = new LinearGradientBrush(panelPreview.ClientRectangle, Color.White, Color.Black, 0f))
                {
                    e.Graphics.FillRectangle(linearGradientBrush, panelPreview.ClientRectangle);
                }
                return;
            }

            if (_selectedPalette.Colors.Count == 1)
            {
                // Если в палитре один цвет, отображаем сплошной цвет
                using (var brush = new SolidBrush(_selectedPalette.Colors[0]))
                {
                    e.Graphics.FillRectangle(brush, panelPreview.ClientRectangle);
                }
                return;
            }

            // Для палитры с несколькими цветами создаем линейный градиент
            using (var linearGradientBrush = new LinearGradientBrush(panelPreview.ClientRectangle, Color.Black, Color.Black, 0f))
            {
                var colorBlend = new ColorBlend(_selectedPalette.Colors.Count);
                colorBlend.Colors = _selectedPalette.Colors.ToArray();
                // Равномерно распределяем цвета по градиенту
                colorBlend.Positions = Enumerable.Range(0, _selectedPalette.Colors.Count)
                                                 .Select(i => (float)i / (_selectedPalette.Colors.Count - 1))
                                                 .ToArray();
                linearGradientBrush.InterpolationColors = colorBlend;
                e.Graphics.FillRectangle(linearGradientBrush, panelPreview.ClientRectangle);
            }
        }

        /// <summary>
        /// Обработчик события изменения текста в поле имени палитры.
        /// Обновляет имя палитры в менеджере палитр и в списке.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void txtName_TextChanged(object sender, EventArgs e)
        {
            // Обновляем имя палитры только для пользовательских палитр и при фокусе на текстовом поле
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn && txtName.Focused)
            {
                _selectedPalette.Name = txtName.Text;

                int selectedIndex = lbPalettes.SelectedIndex;
                if (selectedIndex != -1)
                {
                    // Обновляем имя палитры в списке
                    lbPalettes.Items[selectedIndex] = txtName.Text;
                }
            }
        }

        #endregion

        #region Palette Editing Actions

        /// <summary>
        /// Обработчик события клика по кнопке "Add Color".
        /// Открывает диалог выбора цвета и добавляет выбранный цвет в палитру.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnAddColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _selectedPalette.Colors.Add(colorDialog1.Color);
                DisplayPaletteDetails(); // Обновляем отображение палитры
            }
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Edit Color".
        /// Открывает диалог выбора цвета для редактирования выбранного цвета в списке.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnEditColor_Click(object sender, EventArgs e)
        {
            if (lbColorStops.SelectedIndex == -1)
            {
                return;
            }
            colorDialog1.Color = _selectedPalette.Colors[lbColorStops.SelectedIndex];
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _selectedPalette.Colors[lbColorStops.SelectedIndex] = colorDialog1.Color;
                DisplayPaletteDetails(); // Обновляем отображение палитры
            }
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Remove Color".
        /// Удаляет выбранный цвет из палитры (если их больше одного).
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnRemoveColor_Click(object sender, EventArgs e)
        {
            if (lbColorStops.SelectedIndex != -1 && _selectedPalette.Colors.Count > 1)
            {
                _selectedPalette.Colors.RemoveAt(lbColorStops.SelectedIndex);
                DisplayPaletteDetails(); // Обновляем отображение палитры
            }
        }

        /// <summary>
        /// Обработчик события клика по кнопке "New Palette".
        /// Создает новую палитру с именем по умолчанию и добавляет ее в список.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnNew_Click(object sender, EventArgs e)
        {
            string newName = "Новая палитра";
            int counter = 1;
            // Генерируем уникальное имя для новой палитры
            while (_paletteManager.Palettes.Any(p => p.Name == newName))
            {
                newName = $"Новая палитра {++counter}";
            }
            var newPalette = new PaletteManagerMandelbrotFamily(newName, new List<Color> { Color.Black, Color.White }, true);
            _paletteManager.Palettes.Add(newPalette);
            PopulatePaletteList(); // Обновляем список палитр в UI
            lbPalettes.SelectedItem = newPalette.Name; // Выбираем новую палитру в списке
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Delete Palette".
        /// Удаляет текущую выбранную палитру (если она не встроенная).
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn)
            {
                DialogResult confirmResult = MessageBox.Show($"Вы уверены, что хотите удалить палитру '{_selectedPalette.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirmResult == DialogResult.Yes)
                {
                    _paletteManager.Palettes.Remove(_selectedPalette);
                    PopulatePaletteList(); // Обновляем список палитр в UI
                    lbPalettes.SelectedIndex = 0; // Выбираем первую палитру в списке
                }
            }
        }

        /// <summary>
        /// Обработчик события изменения состояния чекбокса "Is Gradient".
        /// Обновляет свойство IsGradient выбранной палитры.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void checkIsGradient_CheckedChanged(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn)
            {
                _selectedPalette.IsGradient = checkIsGradient.Checked;
            }
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Save".
        /// Сохраняет пользовательские палитры в файл.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            _paletteManager.SaveCustomPalettes();
            MessageBox.Show("Пользовательские палитры сохранены.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region Main Form Actions

        /// <summary>
        /// Обработчик события клика по кнопке "Apply".
        /// Устанавливает выбранную палитру как активную в менеджере палитр
        /// и вызывает событие <see cref="PaletteApplied"/> для уведомления главной формы.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_selectedPalette != null)
            {
                _paletteManager.ActivePalette = _selectedPalette;
                PaletteApplied?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Close".
        /// Закрывает форму настроек палитры.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnClose_Click(object sender, EventArgs e)
        {
            Close(); // Просто закрываем форму
        }

        #endregion
    }
}