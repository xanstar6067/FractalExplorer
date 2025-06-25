using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FractalExplorer.Utilities;

namespace FractalExplorer.SelectorsForms
{
    public partial class ColorConfigurationSerpinskyForm : Form
    {
        private readonly SerpinskyPaletteManager _paletteManager;
        private SerpinskyColorPalette _selectedPalette;
        public event EventHandler PaletteApplied;

        public ColorConfigurationSerpinskyForm(SerpinskyPaletteManager paletteManager)
        {
            InitializeComponent();
            _paletteManager = paletteManager;
        }

        private void ColorConfigurationSerpinskyForm_Load(object sender, EventArgs e)
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

            panelFractalColor.BackColor = _selectedPalette.FractalColor;
            panelBackgroundColor.BackColor = _selectedPalette.BackgroundColor;
        }

        private void UpdateControlsState()
        {
            if (_selectedPalette == null) return;
            bool isCustom = !_selectedPalette.IsBuiltIn;
            txtName.Enabled = isCustom;
            panelFractalColor.Enabled = isCustom;
            panelBackgroundColor.Enabled = isCustom;
            btnDelete.Enabled = isCustom;
        }

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

        private void panelFractalColor_Click(object sender, EventArgs e)
        {
            EditColor(panelFractalColor, color => _selectedPalette.FractalColor = color);
        }

        private void panelBackgroundColor_Click(object sender, EventArgs e)
        {
            EditColor(panelBackgroundColor, color => _selectedPalette.BackgroundColor = color);
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn && txtName.Focused)
            {
                _selectedPalette.Name = txtName.Text;
                lbPalettes.Items[lbPalettes.SelectedIndex] = txtName.Text;
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
            var newPalette = new SerpinskyColorPalette { Name = newName };
            _paletteManager.Palettes.Add(newPalette);
            PopulatePaletteList();
            lbPalettes.SelectedItem = newPalette.Name;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn)
            {
                if (MessageBox.Show($"Удалить палитру '{_selectedPalette.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _paletteManager.Palettes.Remove(_selectedPalette);
                    PopulatePaletteList();
                    lbPalettes.SelectedIndex = 0;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            _paletteManager.SaveCustomPalettes();
            MessageBox.Show("Пользовательские палитры сохранены.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

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
            this.Close();
        }
    }
}