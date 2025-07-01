using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using System.Drawing.Drawing2D;

namespace FractalExplorer.Utilities
{
    public partial class ColorConfigurationMandelbrotFamilyForm : Form
    {
        #region Fields
        private readonly ColorPaletteMandelbrotFamily _paletteManager;
        private PaletteManagerMandelbrotFamily _selectedPalette;
        #endregion

        #region Events
        public event EventHandler PaletteApplied;
        #endregion

        #region Constructor
        public ColorConfigurationMandelbrotFamilyForm(ColorPaletteMandelbrotFamily paletteManager)
        {
            InitializeComponent();
            _paletteManager = paletteManager;
            // ИЗМЕНЕНО: Настройка NumericUpDown для Gamma
            nudGamma.Minimum = 0.1m;
            nudGamma.Maximum = 5.0m;
            nudGamma.DecimalPlaces = 2;
            nudGamma.Increment = 0.1m;
            // ИЗМЕНЕНО: Настройка NumericUpDown для MaxColorIterations
            nudMaxColorIterations.Minimum = 2;
            nudMaxColorIterations.Maximum = 100000;
            nudMaxColorIterations.Increment = 10;
        }
        #endregion

        #region UI Initialization
        private void ColorConfigurationForm_Load(object sender, EventArgs e)
        {
            PopulatePaletteList();
            if (_paletteManager.ActivePalette != null)
            {
                string displayNameToFind = _paletteManager.ActivePalette.IsBuiltIn
                    ? $"{_paletteManager.ActivePalette.Name} [Встроенная]"
                    : _paletteManager.ActivePalette.Name;
                lbPalettes.SelectedItem = displayNameToFind;
            }
        }

        private void PopulatePaletteList()
        {
            lbPalettes.Items.Clear();
            foreach (var palette in _paletteManager.Palettes)
            {
                string displayName = palette.IsBuiltIn ? $"{palette.Name} [Встроенная]" : palette.Name;
                lbPalettes.Items.Add(displayName);
            }
        }
        #endregion

        #region UI Display & Update
        private void lbPalettes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbPalettes.SelectedIndex == -1) return;
            string selectedName = lbPalettes.SelectedItem.ToString().Replace(" [Встроенная]", "");
            _selectedPalette = _paletteManager.Palettes.FirstOrDefault(p => p.Name == selectedName);
            if (_selectedPalette == null) return;
            DisplayPaletteDetails();
            UpdateControlsState();
        }

        private void DisplayPaletteDetails()
        {
            // Временно отписываемся, чтобы избежать рекурсивных вызовов
            txtName.TextChanged -= txtName_TextChanged;
            nudMaxColorIterations.ValueChanged -= nudMaxColorIterations_ValueChanged;
            nudGamma.ValueChanged -= nudGamma_ValueChanged;

            txtName.Text = _selectedPalette.Name;
            checkIsGradient.Checked = _selectedPalette.IsGradient;
            nudMaxColorIterations.Value = Math.Max(nudMaxColorIterations.Minimum, Math.Min(nudMaxColorIterations.Maximum, _selectedPalette.MaxColorIterations));
            nudGamma.Value = (decimal)Math.Max((double)nudGamma.Minimum, Math.Min((double)nudGamma.Maximum, _selectedPalette.Gamma));

            lbColorStops.Items.Clear();
            foreach (var color in _selectedPalette.Colors)
            {
                lbColorStops.Items.Add(color.Name);
            }
            panelPreview.Invalidate();

            // Подписываемся обратно
            txtName.TextChanged += txtName_TextChanged;
            nudMaxColorIterations.ValueChanged += nudMaxColorIterations_ValueChanged;
            nudGamma.ValueChanged += nudGamma_ValueChanged;
        }

        private void UpdateControlsState()
        {
            if (_selectedPalette == null) return;
            bool isCustom = !_selectedPalette.IsBuiltIn;
            txtName.Enabled = isCustom;
            checkIsGradient.Enabled = isCustom;
            lbColorStops.Enabled = isCustom;
            btnAddColor.Enabled = isCustom;
            btnEditColor.Enabled = isCustom;
            btnRemoveColor.Enabled = isCustom;
            btnDelete.Enabled = isCustom;
            nudMaxColorIterations.Enabled = isCustom; // НОВОЕ
            nudGamma.Enabled = isCustom;             // НОВОЕ
        }

        // ИЗМЕНЕНО: Предпросмотр теперь учитывает гамму
        private void panelPreview_Paint(object sender, PaintEventArgs e)
        {
            if (_selectedPalette == null || _selectedPalette.Colors.Count == 0)
            {
                using (var linearGradientBrush = new LinearGradientBrush(panelPreview.ClientRectangle, Color.White, Color.Black, 0f))
                {
                    e.Graphics.FillRectangle(linearGradientBrush, panelPreview.ClientRectangle);
                }
                return;
            }

            if (_selectedPalette.Colors.Count == 1)
            {
                Color finalColor = ColorCorrection.ApplyGamma(_selectedPalette.Colors[0], _selectedPalette.Gamma);
                using (var brush = new SolidBrush(finalColor))
                {
                    e.Graphics.FillRectangle(brush, panelPreview.ClientRectangle);
                }
                return;
            }

            using (var linearGradientBrush = new LinearGradientBrush(panelPreview.ClientRectangle, Color.Black, Color.Black, 0f))
            {
                var colorBlend = new ColorBlend(_selectedPalette.Colors.Count);

                // Применяем гамму к каждому цвету в палитре для предпросмотра
                colorBlend.Colors = _selectedPalette.Colors.Select(c => ColorCorrection.ApplyGamma(c, _selectedPalette.Gamma)).ToArray();

                colorBlend.Positions = Enumerable.Range(0, _selectedPalette.Colors.Count)
                                                 .Select(i => (float)i / (_selectedPalette.Colors.Count - 1))
                                                 .ToArray();
                linearGradientBrush.InterpolationColors = colorBlend;
                e.Graphics.FillRectangle(linearGradientBrush, panelPreview.ClientRectangle);
            }
        }
        #endregion

        #region Palette Editing Actions
        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn && txtName.Focused)
            {
                _selectedPalette.Name = txtName.Text;
                int selectedIndex = lbPalettes.SelectedIndex;
                if (selectedIndex != -1)
                {
                    lbPalettes.Items[selectedIndex] = txtName.Text;
                }
            }
        }

        private void btnAddColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _selectedPalette.Colors.Add(colorDialog1.Color);
                DisplayPaletteDetails();
            }
        }

        private void btnEditColor_Click(object sender, EventArgs e)
        {
            if (lbColorStops.SelectedIndex == -1) return;
            colorDialog1.Color = _selectedPalette.Colors[lbColorStops.SelectedIndex];
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _selectedPalette.Colors[lbColorStops.SelectedIndex] = colorDialog1.Color;
                DisplayPaletteDetails();
            }
        }

        private void btnRemoveColor_Click(object sender, EventArgs e)
        {
            if (lbColorStops.SelectedIndex != -1 && _selectedPalette.Colors.Count > 1)
            {
                _selectedPalette.Colors.RemoveAt(lbColorStops.SelectedIndex);
                DisplayPaletteDetails();
            }
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            string newName = "Новая палитра";
            int counter = 1;
            while (_paletteManager.Palettes.Any(p => p.Name == newName))
            {
                newName = $"Новая палитра {++counter}";
            }
            var newPalette = new PaletteManagerMandelbrotFamily(newName, new List<Color> { Color.Black, Color.White }, true, false, 500, 1.0);
            _paletteManager.Palettes.Add(newPalette);
            PopulatePaletteList();
            lbPalettes.SelectedItem = newPalette.Name;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn)
            {
                DialogResult confirmResult = MessageBox.Show($"Вы уверены, что хотите удалить палитру '{_selectedPalette.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirmResult == DialogResult.Yes)
                {
                    _paletteManager.Palettes.Remove(_selectedPalette);
                    PopulatePaletteList();
                    lbPalettes.SelectedIndex = 0;
                }
            }
        }

        private void checkIsGradient_CheckedChanged(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn)
            {
                _selectedPalette.IsGradient = checkIsGradient.Checked;
            }
        }

        // НОВЫЕ обработчики
        private void nudMaxColorIterations_ValueChanged(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn)
            {
                _selectedPalette.MaxColorIterations = (int)nudMaxColorIterations.Value;
            }
        }

        private void nudGamma_ValueChanged(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn)
            {
                _selectedPalette.Gamma = (double)nudGamma.Value;
                panelPreview.Invalidate(); // Обновляем предпросмотр при изменении гаммы
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            _paletteManager.SaveCustomPalettes();
            MessageBox.Show("Пользовательские палитры сохранены.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region Main Form Actions
        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_selectedPalette != null)
            {
                _paletteManager.ActivePalette = _selectedPalette;
                PaletteApplied?.Invoke(this, EventArgs.Empty);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
        #endregion
    }
}
// --- END OF FILE ColorConfigurationMandelbrotFamilyForm.cs ---