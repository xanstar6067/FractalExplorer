using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace FractalExplorer.Core
{
    public partial class ColorConfigurationFormBase : Form
    {
        private readonly PaletteManager _paletteManager;
        private ColorPaletteBase _selectedPalette;

        // НОВОЕ: Событие, которое будет сообщать главной форме о применении изменений
        public event EventHandler PaletteApplied;

        public ColorConfigurationFormBase(PaletteManager paletteManager)
        {
            InitializeComponent();
            _paletteManager = paletteManager;
        }

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
            txtName.TextChanged -= txtName_TextChanged;
            txtName.Text = _selectedPalette.Name;
            txtName.TextChanged += txtName_TextChanged;

            checkIsGradient.Checked = _selectedPalette.IsGradient;

            lbColorStops.Items.Clear();
            foreach (var color in _selectedPalette.Colors)
            {
                lbColorStops.Items.Add(color.Name);
            }
            panelPreview.Invalidate();
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
        }

        private void panelPreview_Paint(object sender, PaintEventArgs e)
        {
            if (_selectedPalette == null || _selectedPalette.Colors.Count == 0)
            {
                using (var lgb = new LinearGradientBrush(panelPreview.ClientRectangle, Color.White, Color.Black, 0f))
                {
                    e.Graphics.FillRectangle(lgb, panelPreview.ClientRectangle);
                }
                return;
            }

            if (_selectedPalette.Colors.Count == 1)
            {
                using (var brush = new SolidBrush(_selectedPalette.Colors[0]))
                {
                    e.Graphics.FillRectangle(brush, panelPreview.ClientRectangle);
                }
                return;
            }

            using (var lgb = new LinearGradientBrush(panelPreview.ClientRectangle, Color.Black, Color.Black, 0f))
            {
                var cb = new ColorBlend(_selectedPalette.Colors.Count);
                cb.Colors = _selectedPalette.Colors.ToArray();
                cb.Positions = Enumerable.Range(0, _selectedPalette.Colors.Count)
                                         .Select(i => (float)i / (_selectedPalette.Colors.Count - 1))
                                         .ToArray();
                lgb.InterpolationColors = cb;
                e.Graphics.FillRectangle(lgb, panelPreview.ClientRectangle);
            }
        }

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

        // --- Обработчики кнопок для редактирования ---
        private void btnAddColor_Click(object sender, EventArgs e) { if (colorDialog1.ShowDialog() == DialogResult.OK) { _selectedPalette.Colors.Add(colorDialog1.Color); DisplayPaletteDetails(); } }
        private void btnEditColor_Click(object sender, EventArgs e) { if (lbColorStops.SelectedIndex == -1) return; colorDialog1.Color = _selectedPalette.Colors[lbColorStops.SelectedIndex]; if (colorDialog1.ShowDialog() == DialogResult.OK) { _selectedPalette.Colors[lbColorStops.SelectedIndex] = colorDialog1.Color; DisplayPaletteDetails(); } }
        private void btnRemoveColor_Click(object sender, EventArgs e) { if (lbColorStops.SelectedIndex != -1 && _selectedPalette.Colors.Count > 1) { _selectedPalette.Colors.RemoveAt(lbColorStops.SelectedIndex); DisplayPaletteDetails(); } }
        private void btnNew_Click(object sender, EventArgs e) { string newName = "Новая палитра"; int counter = 1; while (_paletteManager.Palettes.Any(p => p.Name == newName)) { newName = $"Новая палитра {++counter}"; } var newPalette = new ColorPaletteBase(newName, new List<Color> { Color.Black, Color.White }, true); _paletteManager.Palettes.Add(newPalette); PopulatePaletteList(); lbPalettes.SelectedItem = newPalette.Name; }
        private void btnDelete_Click(object sender, EventArgs e) { if (_selectedPalette != null && !_selectedPalette.IsBuiltIn) { if (MessageBox.Show($"Вы уверены, что хотите удалить палитру '{_selectedPalette.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _paletteManager.Palettes.Remove(_selectedPalette); PopulatePaletteList(); lbPalettes.SelectedIndex = 0; } } }
        private void checkIsGradient_CheckedChanged(object sender, EventArgs e) { if (_selectedPalette != null && !_selectedPalette.IsBuiltIn) { _selectedPalette.IsGradient = checkIsGradient.Checked; } }
        private void btnSave_Click(object sender, EventArgs e) { _paletteManager.SaveCustomPalettes(); MessageBox.Show("Пользовательские палитры сохранены.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information); }

        // --- Основные кнопки формы ---

        // ИЗМЕНЕНИЕ: Теперь кнопка "Применить"
        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_selectedPalette != null)
            {
                _paletteManager.ActivePalette = _selectedPalette;
                // Вызываем событие, чтобы уведомить главную форму
                PaletteApplied?.Invoke(this, EventArgs.Empty);
            }
            // Форма больше не закрывается
        }

        // ИЗМЕНЕНИЕ: Теперь кнопка "Закрыть"
        private void btnClose_Click(object sender, EventArgs e)
        {
            // Просто закрываем форму
            this.Close();
        }
    }
}