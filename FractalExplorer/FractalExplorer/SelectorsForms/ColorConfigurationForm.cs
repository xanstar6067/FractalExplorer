using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace FractalExplorer.Core
{
    public partial class ColorConfigurationForm : Form
    {
        private readonly PaletteManager _paletteManager;
        private ColorPalette _selectedPalette;

        public ColorConfigurationForm(PaletteManager paletteManager)
        {
            InitializeComponent();
            _paletteManager = paletteManager;
        }

        private void ColorConfigurationForm_Load(object sender, EventArgs e)
        {
            PopulatePaletteList();
            // Выбираем активную палитру при загрузке
            lbPalettes.SelectedItem = _paletteManager.ActivePalette.Name;
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

            // Находим палитру по реальному имени, отбрасывая "[Встроенная]"
            string selectedName = lbPalettes.SelectedItem.ToString().Replace(" [Встроенная]", "");
            _selectedPalette = _paletteManager.Palettes.FirstOrDefault(p => p.Name == selectedName);

            if (_selectedPalette == null) return;

            DisplayPaletteDetails();
            UpdateControlsState();
        }

        private void DisplayPaletteDetails()
        {
            txtName.Text = _selectedPalette.Name;
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
                e.Graphics.Clear(Color.Black);
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

        // --- Обработчики кнопок для редактирования ---

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

            var newPalette = new ColorPalette(newName, new List<Color> { Color.Black, Color.White }, true);
            _paletteManager.Palettes.Add(newPalette);

            PopulatePaletteList();
            lbPalettes.SelectedItem = newPalette.Name;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn)
            {
                if (MessageBox.Show($"Вы уверены, что хотите удалить палитру '{_selectedPalette.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _paletteManager.Palettes.Remove(_selectedPalette);
                    PopulatePaletteList();
                    lbPalettes.SelectedIndex = 0;
                }
            }
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn)
            {
                _selectedPalette.Name = txtName.Text;
            }
        }

        private void checkIsGradient_CheckedChanged(object sender, EventArgs e)
        {
            if (_selectedPalette != null && !_selectedPalette.IsBuiltIn)
            {
                _selectedPalette.IsGradient = checkIsGradient.Checked;
            }
        }

        // --- Основные кнопки формы ---

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Обновляем имена в списке перед сохранением
            int currentSelectedIndex = lbPalettes.SelectedIndex;
            PopulatePaletteList();
            lbPalettes.SelectedIndex = currentSelectedIndex;

            _paletteManager.SaveCustomPalettes();
            MessageBox.Show("Пользовательские палитры сохранены.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (_selectedPalette != null)
            {
                _paletteManager.ActivePalette = _selectedPalette;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}