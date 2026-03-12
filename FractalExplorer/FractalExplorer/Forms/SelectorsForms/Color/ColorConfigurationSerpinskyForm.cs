using FractalExplorer.Utilities.ColorPicking;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.Theme;

namespace FractalExplorer.SelectorsForms
{
    /// <summary>
    /// Форма настройки палитр для фрактала Серпинского.
    /// </summary>
    public partial class ColorConfigurationSerpinskyForm : Form
    {
        private readonly SerpinskyPaletteManager _paletteManager;
        private readonly ColorSelectionService _colorSelectionService = ColorSelectionService.Default;

        private SerpinskyColorPalette? _selectedPalette;
        private bool _isProgrammaticChange;
        private bool _hasUnsavedChanges;
        private EventHandler? _themeChangedHandler;
        private bool _fractalColorHovered;
        private bool _backgroundColorHovered;

        public event EventHandler? PaletteApplied;

        public ColorConfigurationSerpinskyForm(SerpinskyPaletteManager paletteManager)
        {
            InitializeComponent();
            _paletteManager = paletteManager;

            EnableDoubleBuffering(panelFractalColor);
            EnableDoubleBuffering(panelBackgroundColor);

            ThemeManager.RegisterForm(this);
            _themeChangedHandler = (_, _) => ApplySerpinskyPaletteThemeHints();
            ThemeManager.ThemeChanged += _themeChangedHandler;

            FormClosing += ColorConfigurationSerpinskyForm_FormClosing;
        }

        private static void EnableDoubleBuffering(Control control)
        {
            var doubleBufferedProperty = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            doubleBufferedProperty?.SetValue(control, true);
        }

        private void ColorConfigurationSerpinskyForm_Load(object sender, EventArgs e)
        {
            PopulatePaletteList();
        }

        private void PopulatePaletteList()
        {
            _isProgrammaticChange = true;
            lbPalettes.Items.Clear();

            foreach (SerpinskyColorPalette palette in _paletteManager.Palettes)
            {
                string displayName = palette.IsBuiltIn ? $"{palette.Name} [Встроенная]" : palette.Name;
                lbPalettes.Items.Add(displayName);
            }

            string activeName = _paletteManager.ActivePalette.IsBuiltIn
                ? $"{_paletteManager.ActivePalette.Name} [Встроенная]"
                : _paletteManager.ActivePalette.Name;
            lbPalettes.SelectedItem = activeName;

            _isProgrammaticChange = false;
            RefreshUIFromPalette(_paletteManager.ActivePalette);
            ResetUnsavedChanges();
        }

        private void RefreshUIFromPalette(SerpinskyColorPalette palette)
        {
            _isProgrammaticChange = true;
            _selectedPalette = palette;

            txtName.Text = palette.Name;
            panelFractalColor.BackColor = palette.FractalColor;
            panelBackgroundColor.BackColor = palette.BackgroundColor;

            UpdateControlsState();
            ApplySerpinskyPaletteThemeHints();
            _isProgrammaticChange = false;

            panelFractalColor.Invalidate();
            panelBackgroundColor.Invalidate();
        }

        private void UpdateControlsState()
        {
            bool hasPalette = _selectedPalette != null;
            bool isCustom = hasPalette && !_selectedPalette!.IsBuiltIn;

            txtName.Enabled = isCustom;
            panelFractalColor.Enabled = isCustom;
            panelBackgroundColor.Enabled = isCustom;
            btnSave.Enabled = isCustom && _hasUnsavedChanges;
            btnDelete.Enabled = isCustom;
            btnCopy.Enabled = hasPalette;
            btnNew.Enabled = true;
        }

        private string GenerateUniquePaletteName(string baseName)
        {
            string trimmedBaseName = string.IsNullOrWhiteSpace(baseName) ? "Новая палитра" : baseName.Trim();
            string candidate = trimmedBaseName;
            int suffix = 1;

            while (_paletteManager.Palettes.Any(p => p.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
            {
                candidate = $"{trimmedBaseName} {suffix++}";
            }

            return candidate;
        }

        private bool EnsureEditablePaletteOrWarn()
        {
            if (_selectedPalette == null || _selectedPalette.IsBuiltIn)
            {
                MessageBox.Show("Встроенные палитры нельзя изменять. Сначала сохраните её как новую палитру.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            return true;
        }

        private void MarkUnsavedChanges()
        {
            if (_selectedPalette == null || _selectedPalette.IsBuiltIn)
            {
                return;
            }

            _hasUnsavedChanges = true;
            btnSave.Enabled = true;
        }

        private void ResetUnsavedChanges()
        {
            _hasUnsavedChanges = false;
            btnSave.Enabled = false;
        }

        private void UpdateSelectedPaletteDisplayName()
        {
            if (_selectedPalette == null || _selectedPalette.IsBuiltIn)
            {
                return;
            }

            int selectedIndex = lbPalettes.SelectedIndex;
            if (selectedIndex < 0)
            {
                return;
            }

            lbPalettes.SelectedIndexChanged -= lbPalettes_SelectedIndexChanged;
            lbPalettes.Items[selectedIndex] = _selectedPalette.Name;
            lbPalettes.SelectedIndexChanged += lbPalettes_SelectedIndexChanged;
        }

        private void EditColor(Panel panel, Action<Color> setColorAction)
        {
            if (!EnsureEditablePaletteOrWarn())
            {
                return;
            }

            if (_colorSelectionService.TrySelectColor(this, panel.BackColor, out Color selectedColor))
            {
                panel.BackColor = selectedColor;
                setColorAction(selectedColor);
                MarkUnsavedChanges();
                panel.Invalidate();
            }
        }

        private void lbPalettes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange || lbPalettes.SelectedItem is null)
            {
                return;
            }

            string selectedName = lbPalettes.SelectedItem.ToString()!.Replace(" [Встроенная]", string.Empty);
            SerpinskyColorPalette selected = _paletteManager.Palettes.First(p => p.Name == selectedName);
            RefreshUIFromPalette(selected);
            ResetUnsavedChanges();
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange || _selectedPalette == null || _selectedPalette.IsBuiltIn)
            {
                return;
            }

            _selectedPalette.Name = txtName.Text;
            MarkUnsavedChanges();
        }

        private void txtName_Leave(object sender, EventArgs e)
        {
            UpdateSelectedPaletteDisplayName();
        }

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.SuppressKeyPress = true;
            UpdateSelectedPaletteDisplayName();
        }

        private void panelFractalColor_Click(object sender, EventArgs e)
        {
            if (_selectedPalette == null)
            {
                return;
            }

            EditColor(panelFractalColor, color => _selectedPalette.FractalColor = color);
        }

        private void panelBackgroundColor_Click(object sender, EventArgs e)
        {
            if (_selectedPalette == null)
            {
                return;
            }

            EditColor(panelBackgroundColor, color => _selectedPalette.BackgroundColor = color);
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            string newName = GenerateUniquePaletteName("Новая палитра");
            SerpinskyColorPalette newPalette = new()
            {
                Name = newName,
                FractalColor = Color.Black,
                BackgroundColor = Color.White,
                IsBuiltIn = false
            };

            _paletteManager.Palettes.Add(newPalette);
            _paletteManager.SaveCustomPalettes();

            PopulatePaletteList();
            lbPalettes.SelectedItem = newPalette.Name;
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (_selectedPalette == null)
            {
                return;
            }

            string newName = GenerateUniquePaletteName($"{_selectedPalette.Name} копия");
            SerpinskyColorPalette newPalette = new()
            {
                Name = newName,
                FractalColor = _selectedPalette.FractalColor,
                BackgroundColor = _selectedPalette.BackgroundColor,
                IsBuiltIn = false
            };

            _paletteManager.Palettes.Add(newPalette);
            _paletteManager.SaveCustomPalettes();

            PopulatePaletteList();
            lbPalettes.SelectedItem = newPalette.Name;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPalette == null || _selectedPalette.IsBuiltIn)
            {
                return;
            }

            DialogResult confirmResult = MessageBox.Show($"Вы уверены, что хотите удалить палитру '{_selectedPalette.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmResult != DialogResult.Yes)
            {
                return;
            }

            bool deletedWasActive = ReferenceEquals(_paletteManager.ActivePalette, _selectedPalette);
            _paletteManager.Palettes.Remove(_selectedPalette);
            if (deletedWasActive)
            {
                _paletteManager.ActivePalette = _paletteManager.Palettes.First();
            }

            _paletteManager.SaveCustomPalettes();
            PopulatePaletteList();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_selectedPalette == null || _selectedPalette.IsBuiltIn)
            {
                MessageBox.Show("Нельзя сохранить изменения во встроенной палитре.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _paletteManager.SaveCustomPalettes();
            ResetUnsavedChanges();
            MessageBox.Show("Изменения палитры сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_selectedPalette == null)
            {
                return;
            }

            _paletteManager.ActivePalette = _selectedPalette;
            PaletteApplied?.Invoke(this, EventArgs.Empty);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void panelFractalColor_MouseEnter(object sender, EventArgs e)
        {
            if (_fractalColorHovered)
            {
                return;
            }

            _fractalColorHovered = true;
            panelFractalColor.Invalidate();
        }

        private void panelFractalColor_MouseLeave(object sender, EventArgs e)
        {
            if (!_fractalColorHovered)
            {
                return;
            }

            _fractalColorHovered = false;
            panelFractalColor.Invalidate();
        }

        private void panelBackgroundColor_MouseEnter(object sender, EventArgs e)
        {
            if (_backgroundColorHovered)
            {
                return;
            }

            _backgroundColorHovered = true;
            panelBackgroundColor.Invalidate();
        }

        private void panelBackgroundColor_MouseLeave(object sender, EventArgs e)
        {
            if (!_backgroundColorHovered)
            {
                return;
            }

            _backgroundColorHovered = false;
            panelBackgroundColor.Invalidate();
        }

        private void panelFractalColor_Paint(object sender, PaintEventArgs e)
        {
            DrawColorPanelBorder(panelFractalColor, e.Graphics, _fractalColorHovered);
        }

        private void panelBackgroundColor_Paint(object sender, PaintEventArgs e)
        {
            DrawColorPanelBorder(panelBackgroundColor, e.Graphics, _backgroundColorHovered);
        }

        private void DrawColorPanelBorder(Panel panel, Graphics graphics, bool hovered)
        {
            ThemeDefinition theme = ThemeManager.CurrentDefinition;
            Color borderColor = ThemeManager.GetInteractiveBorderColor(theme, panel.BackColor, hovered);
            using Pen borderPen = new(borderColor, hovered ? 2f : 1f);

            Rectangle borderRect = panel.ClientRectangle;
            borderRect.Width = Math.Max(1, borderRect.Width - 1);
            borderRect.Height = Math.Max(1, borderRect.Height - 1);
            graphics.DrawRectangle(borderPen, borderRect);
        }

        private void ApplySerpinskyPaletteThemeHints()
        {
            if (_selectedPalette != null)
            {
                panelFractalColor.BackColor = _selectedPalette.FractalColor;
                panelBackgroundColor.BackColor = _selectedPalette.BackgroundColor;
            }

            panelFractalColor.Invalidate();
            panelBackgroundColor.Invalidate();
            lblEditHint.Text = _selectedPalette?.IsBuiltIn == true
                ? "Встроенная палитра доступна только для просмотра"
                : "Текущая палитра редактируема";
            lblEditHint.ForeColor = ThemeManager.CurrentDefinition.SecondaryText;
        }

        private void ColorConfigurationSerpinskyForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_themeChangedHandler is not null)
            {
                ThemeManager.ThemeChanged -= _themeChangedHandler;
                _themeChangedHandler = null;
            }

            base.OnFormClosed(e);
        }
    }
}
