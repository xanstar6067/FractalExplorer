using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic; // Для InputBox

namespace FractalExplorer
{
    public partial class color_setting_NewtonPoolsForm : Form
    {
        public delegate void PaletteChangedEventHandler(object sender, NewtonColorPalette activePalette);
        public event PaletteChangedEventHandler PaletteChanged;

        private NewtonPaletteManager _paletteManager;
        private int _requiredRootCount;
        private bool _isProgrammaticChange = false;

        public color_setting_NewtonPoolsForm(NewtonPaletteManager manager)
        {
            InitializeComponent();
            _paletteManager = manager;
            this.FormClosing += ColorSetting_FormClosing;
        }

        public void ShowWithRootCount(int rootCount)
        {
            _requiredRootCount = rootCount;
            PopulatePaletteList();
            this.Show();
            this.Activate();
        }

        private void PopulatePaletteList()
        {
            _isProgrammaticChange = true;
            cbPalettes.Items.Clear();
            foreach (var palette in _paletteManager.Palettes)
            {
                cbPalettes.Items.Add(palette.Name);
            }
            cbPalettes.SelectedItem = _paletteManager.ActivePalette.Name;
            _isProgrammaticChange = false;

            RefreshUIFromPalette(_paletteManager.ActivePalette);
        }

        private void RefreshUIFromPalette(NewtonColorPalette palette)
        {
            _isProgrammaticChange = true;

            // Обновляем контролы
            chkIsGradient.Checked = palette.IsGradient;
            panelBackgroundColor.BackColor = palette.BackgroundColor;

            // Управляем доступностью кнопок
            btnSave.Enabled = !palette.IsBuiltIn;
            btnDelete.Enabled = !palette.IsBuiltIn;

            // Очищаем и заполняем пикеры цветов
            flpRootColorPickers.Controls.Clear();
            if (_requiredRootCount == 0)
            {
                flpRootColorPickers.Controls.Add(new Label { Text = "Корни не найдены, цвета не требуются.", ForeColor = Color.Gray, AutoSize = true });
            }
            else
            {
                // Если у палитры не заданы цвета, генерируем их
                List<Color> colorsToUse = palette.RootColors;
                if (colorsToUse == null || colorsToUse.Count == 0)
                {
                    colorsToUse = GenerateHarmonicColors(_requiredRootCount);
                }

                for (int i = 0; i < _requiredRootCount; i++)
                {
                    var color = colorsToUse[i % colorsToUse.Count]; // Зацикливаем цвета, если их меньше, чем корней

                    Panel colorPanel = new Panel
                    {
                        Size = new Size(25, 25),
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = color,
                        Tag = i,
                        Cursor = Cursors.Hand,
                        Margin = new Padding(5)
                    };
                    toolTip1.SetToolTip(colorPanel, $"Цвет для корня №{i + 1}\nНажмите, чтобы изменить");
                    colorPanel.Click += RootColorPanel_Click;
                    flpRootColorPickers.Controls.Add(colorPanel);
                }
            }
            _isProgrammaticChange = false;
        }

        private void cbPalettes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange) return;

            string selectedName = cbPalettes.SelectedItem.ToString();
            _paletteManager.ActivePalette = _paletteManager.Palettes.First(p => p.Name == selectedName);
            RefreshUIFromPalette(_paletteManager.ActivePalette);
            PaletteChanged?.Invoke(this, _paletteManager.ActivePalette);
        }

        private void RootColorPanel_Click(object sender, EventArgs e)
        {
            if (_paletteManager.ActivePalette.IsBuiltIn)
            {
                MessageBox.Show("Встроенные палитры нельзя изменять. Сначала сохраните ее как новую палитру.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Panel clickedPanel = sender as Panel;
            int colorIndex = (int)clickedPanel.Tag;

            using (ColorDialog cd = new ColorDialog { Color = clickedPanel.BackColor })
            {
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    clickedPanel.BackColor = cd.Color;

                    // Обновляем цвет в активной палитре
                    while (_paletteManager.ActivePalette.RootColors.Count <= colorIndex)
                    {
                        _paletteManager.ActivePalette.RootColors.Add(Color.Black);
                    }
                    _paletteManager.ActivePalette.RootColors[colorIndex] = cd.Color;
                    PaletteChanged?.Invoke(this, _paletteManager.ActivePalette);
                }
            }
        }

        private void panelBackgroundColor_Click(object sender, EventArgs e)
        {
            if (_paletteManager.ActivePalette.IsBuiltIn)
            {
                MessageBox.Show("Встроенные палитры нельзя изменять. Сначала сохраните ее как новую палитру.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (ColorDialog cd = new ColorDialog { Color = panelBackgroundColor.BackColor })
            {
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    panelBackgroundColor.BackColor = cd.Color;
                    _paletteManager.ActivePalette.BackgroundColor = cd.Color;
                    PaletteChanged?.Invoke(this, _paletteManager.ActivePalette);
                }
            }
        }

        private void chkIsGradient_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange) return;
            if (_paletteManager.ActivePalette.IsBuiltIn)
            {
                MessageBox.Show("Встроенные палитры нельзя изменять. Сначала сохраните ее как новую палитру.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _isProgrammaticChange = true;
                chkIsGradient.Checked = !chkIsGradient.Checked; // Вернуть обратно
                _isProgrammaticChange = false;
                return;
            }
            _paletteManager.ActivePalette.IsGradient = chkIsGradient.Checked;
            PaletteChanged?.Invoke(this, _paletteManager.ActivePalette);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_paletteManager.ActivePalette.IsBuiltIn)
            {
                MessageBox.Show("Нельзя сохранить изменения во встроенной палитре.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _paletteManager.SavePalettes();
            MessageBox.Show("Палитра сохранена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            string newName = Interaction.InputBox("Введите имя для новой палитры:", "Сохранить палитру как", "Моя палитра");
            if (string.IsNullOrWhiteSpace(newName)) return;

            if (_paletteManager.Palettes.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Палитра с таким именем уже существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var currentPalette = _paletteManager.ActivePalette;
            var newPalette = new NewtonColorPalette
            {
                Name = newName,
                BackgroundColor = currentPalette.BackgroundColor,
                IsGradient = currentPalette.IsGradient,
                IsBuiltIn = false,
                RootColors = new List<Color>()
            };

            // Копируем цвета из UI
            foreach (Panel panel in flpRootColorPickers.Controls.OfType<Panel>())
            {
                newPalette.RootColors.Add(panel.BackColor);
            }

            _paletteManager.Palettes.Add(newPalette);
            _paletteManager.ActivePalette = newPalette;
            _paletteManager.SavePalettes();
            PopulatePaletteList();
            MessageBox.Show($"Палитра '{newName}' создана и сохранена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var paletteToDelete = _paletteManager.ActivePalette;
            if (paletteToDelete.IsBuiltIn) return;

            if (MessageBox.Show($"Вы уверены, что хотите удалить палитру '{paletteToDelete.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _paletteManager.Palettes.Remove(paletteToDelete);
                _paletteManager.ActivePalette = _paletteManager.Palettes.First();
                _paletteManager.SavePalettes();
                PopulatePaletteList();
                PaletteChanged?.Invoke(this, _paletteManager.ActivePalette);
            }
        }

        // Улучшенная генерация цветов
        public static List<Color> GenerateHarmonicColors(int count)
        {
            var colors = new List<Color>();
            if (count == 0) return colors;
            for (int i = 0; i < count; i++)
            {
                float hue = (360f * i) / count;
                colors.Add(ColorFromHSL(hue, 0.85f, 0.6f));
            }
            return colors;
        }

        public static Color ColorFromHSL(float h, float s, float l)
        {
            float r, g, b;
            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                float q = l < 0.5f ? l * (1 + s) : l + s - l * s;
                float p = 2 * l - q;
                h /= 360f;
                r = HueToRgb(p, q, h + 1 / 3f);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1 / 3f);
            }
            return Color.FromArgb(255, (int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1 / 6f) return p + (q - p) * 6 * t;
            if (t < 1 / 2f) return q;
            if (t < 2 / 3f) return p + (q - p) * (2 / 3f - t) * 6;
            return p;
        }

        private void ColorSetting_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
    }
}