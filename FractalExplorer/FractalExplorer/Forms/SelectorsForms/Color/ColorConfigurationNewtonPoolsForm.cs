using FractalExplorer.Utilities.ColorPicking;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.Theme;
using Microsoft.VisualBasic;

namespace FractalExplorer
{
    /// <summary>
    /// Форма для настройки цветовых палитр для фракталов Ньютона.
    /// Совмещает общий UX редактора палитр (список, превью, стопы)
    /// и специфику бассейнов Ньютона (корневые цвета + служебный фон).
    /// </summary>
    public partial class ColorConfigurationNewtonPoolsForm : Form
    {
        public delegate void PaletteChangedEventHandler(object sender, NewtonColorPalette activePalette);
        public event PaletteChangedEventHandler PaletteChanged;

        private readonly NewtonPaletteManager _paletteManager;
        private readonly ColorSelectionService _colorSelectionService = ColorSelectionService.Default;

        private NewtonColorPalette? _selectedPalette;
        private int _requiredRootCount;
        private bool _isProgrammaticChange;
        private EventHandler? _themeChangedHandler;

        public ColorConfigurationNewtonPoolsForm(NewtonPaletteManager manager)
        {
            InitializeComponent();
            panelBackgroundColor.Tag = "preserve-backcolor";
            _paletteManager = manager;

            ThemeManager.RegisterForm(this);
            _themeChangedHandler = (_, _) => ApplyNewtonPaletteThemeHints();
            ThemeManager.ThemeChanged += _themeChangedHandler;

            FormClosing += ColorSetting_FormClosing;
        }

        public void ShowWithRootCount(int rootCount)
        {
            _requiredRootCount = Math.Max(0, rootCount);
            PopulatePaletteList();
            Show();
            Activate();
        }

        public void UpdateRootCount(int rootCount)
        {
            int normalizedCount = Math.Max(0, rootCount);
            if (_requiredRootCount == normalizedCount)
            {
                return;
            }

            _requiredRootCount = normalizedCount;
            if (_selectedPalette != null)
            {
                RefreshUIFromPalette(_selectedPalette);
            }
        }

        private void PopulatePaletteList()
        {
            _isProgrammaticChange = true;
            lbPalettes.Items.Clear();
            foreach (NewtonColorPalette palette in _paletteManager.Palettes)
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
        }

        private void RefreshUIFromPalette(NewtonColorPalette palette)
        {
            _isProgrammaticChange = true;
            _selectedPalette = palette;

            txtName.Text = palette.Name;
            chkIsGradient.Checked = palette.IsGradient;
            panelBackgroundColor.BackColor = palette.BackgroundColor;
            lblRootCountValue.Text = _requiredRootCount.ToString();

            List<Color> effectiveColors = BuildAutoAdjustedColors(palette, _requiredRootCount);
            lbColorStops.Items.Clear();
            for (int i = 0; i < effectiveColors.Count; i++)
            {
                Color color = effectiveColors[i];
                lbColorStops.Items.Add($"Корень {i + 1}: #{color.R:X2}{color.G:X2}{color.B:X2}");
            }

            if (_requiredRootCount == 0)
            {
                lbColorStops.Items.Add("Корни не найдены — используются только служебные оттенки.");
            }

            UpdateControlsState();
            ApplyNewtonPaletteThemeHints();
            panelPreview.Invalidate();
            _isProgrammaticChange = false;
        }

        private void UpdateControlsState()
        {
            bool hasPalette = _selectedPalette != null;
            bool isCustom = hasPalette && !_selectedPalette!.IsBuiltIn;

            txtName.Enabled = isCustom;
            chkIsGradient.Enabled = isCustom;
            panelBackgroundColor.Enabled = isCustom;
            panelBackgroundColor.Cursor = isCustom ? Cursors.Hand : Cursors.Default;

            lbColorStops.Enabled = hasPalette;
            btnEditColor.Enabled = isCustom && lbColorStops.SelectedIndex >= 0 && _requiredRootCount > 0;
            btnAutoAdjustRoots.Enabled = isCustom;

            btnSave.Enabled = isCustom;
            btnDelete.Enabled = isCustom;
            btnSaveAs.Enabled = hasPalette;
        }

        private List<Color> BuildAutoAdjustedColors(NewtonColorPalette palette, int requiredCount)
        {
            if (requiredCount <= 0)
            {
                return new List<Color>();
            }

            List<Color> baseColors = palette.RootColors?.Count > 0
                ? new List<Color>(palette.RootColors)
                : new List<Color>();

            List<Color> harmonic = GenerateHarmonicColors(requiredCount);
            List<Color> result = new(requiredCount);

            for (int i = 0; i < requiredCount; i++)
            {
                if (i < baseColors.Count)
                {
                    result.Add(baseColors[i]);
                }
                else
                {
                    result.Add(harmonic[i]);
                }
            }

            return result;
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

        private void lbPalettes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange || lbPalettes.SelectedItem is null)
            {
                return;
            }

            string selectedName = lbPalettes.SelectedItem.ToString()!.Replace(" [Встроенная]", string.Empty);
            NewtonColorPalette selected = _paletteManager.Palettes.First(p => p.Name == selectedName);
            RefreshUIFromPalette(selected);
        }

        private void lbColorStops_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlsState();
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange || _selectedPalette == null || _selectedPalette.IsBuiltIn)
            {
                return;
            }

            _selectedPalette.Name = txtName.Text;
            int idx = lbPalettes.SelectedIndex;
            if (idx >= 0)
            {
                lbPalettes.Items[idx] = _selectedPalette.Name;
            }
        }

        private void btnAddColor_Click(object sender, EventArgs e)
        {
            // Кнопка удалена из UX: для корней число цветов определяется формулой.
        }

        private void btnEditColor_Click(object sender, EventArgs e)
        {
            if (!EnsureEditablePaletteOrWarn())
            {
                return;
            }

            int selectedIndex = lbColorStops.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= _requiredRootCount)
            {
                return;
            }

            List<Color> colors = BuildAutoAdjustedColors(_selectedPalette!, _requiredRootCount);
            if (_colorSelectionService.TrySelectColor(this, colors[selectedIndex], out Color selectedColor))
            {
                while (_selectedPalette!.RootColors.Count <= selectedIndex)
                {
                    _selectedPalette.RootColors.Add(Color.Black);
                }

                _selectedPalette.RootColors[selectedIndex] = selectedColor;
                RefreshUIFromPalette(_selectedPalette);
                lbColorStops.SelectedIndex = selectedIndex;
            }
        }

        private void btnRemoveColor_Click(object sender, EventArgs e)
        {
            // Кнопка удалена из UX: для корней число цветов определяется формулой.
        }

        private void panelBackgroundColor_Click(object sender, EventArgs e)
        {
            if (!EnsureEditablePaletteOrWarn())
            {
                return;
            }

            if (_colorSelectionService.TrySelectColor(this, panelBackgroundColor.BackColor, out Color selectedColor))
            {
                panelBackgroundColor.BackColor = selectedColor;
                _selectedPalette!.BackgroundColor = selectedColor;
                panelPreview.Invalidate();
            }
        }

        private void chkIsGradient_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange)
            {
                return;
            }

            if (!EnsureEditablePaletteOrWarn())
            {
                _isProgrammaticChange = true;
                chkIsGradient.Checked = !chkIsGradient.Checked;
                _isProgrammaticChange = false;
                return;
            }

            _selectedPalette!.IsGradient = chkIsGradient.Checked;
        }

        private void btnAutoAdjustRoots_Click(object sender, EventArgs e)
        {
            if (!EnsureEditablePaletteOrWarn())
            {
                return;
            }

            _selectedPalette!.RootColors = BuildAutoAdjustedColors(_selectedPalette, _requiredRootCount);
            RefreshUIFromPalette(_selectedPalette);
        }

        private void panelPreview_Paint(object sender, PaintEventArgs e)
        {
            Rectangle rect = panelPreview.ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0 || _selectedPalette == null)
            {
                return;
            }

            List<Color> colors = BuildAutoAdjustedColors(_selectedPalette, Math.Max(_requiredRootCount, 1));
            float serviceSegment = 0.16f;
            int rootsWidth = (int)Math.Round(rect.Width * (1f - serviceSegment));
            rootsWidth = Math.Max(1, Math.Min(rect.Width, rootsWidth));

            Rectangle rootsRect = new(rect.X, rect.Y, rootsWidth, rect.Height);
            Rectangle backgroundRect = new(rect.X + rootsWidth, rect.Y, rect.Width - rootsWidth, rect.Height);

            if (colors.Count == 1)
            {
                using SolidBrush brush = new(colors[0]);
                e.Graphics.FillRectangle(brush, rootsRect);
            }
            else
            {
                int baseWidth = rootsRect.Width / colors.Count;
                int remainder = rootsRect.Width % colors.Count;
                int currentX = rootsRect.X;

                for (int i = 0; i < colors.Count; i++)
                {
                    int segmentWidth = baseWidth + (i < remainder ? 1 : 0);
                    if (segmentWidth <= 0)
                    {
                        continue;
                    }

                    using SolidBrush segmentBrush = new(colors[i]);
                    e.Graphics.FillRectangle(segmentBrush, currentX, rootsRect.Y, segmentWidth, rootsRect.Height);
                    currentX += segmentWidth;
                }
            }

            using SolidBrush backgroundBrush = new(_selectedPalette.BackgroundColor);
            e.Graphics.FillRectangle(backgroundBrush, backgroundRect);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_selectedPalette == null || _selectedPalette.IsBuiltIn)
            {
                MessageBox.Show("Нельзя сохранить изменения во встроенной палитре.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _paletteManager.SavePalettes();
            MessageBox.Show("Палитра сохранена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            if (_selectedPalette == null)
            {
                return;
            }

            string newName = Interaction.InputBox("Введите имя для новой палитры:", "Сохранить палитру как", "Моя палитра");
            if (string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            if (_paletteManager.Palettes.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Палитра с таким именем уже существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            NewtonColorPalette newPalette = new()
            {
                Name = newName,
                BackgroundColor = _selectedPalette.BackgroundColor,
                IsGradient = _selectedPalette.IsGradient,
                IsBuiltIn = false,
                RootColors = BuildAutoAdjustedColors(_selectedPalette, _requiredRootCount)
            };

            _paletteManager.Palettes.Add(newPalette);
            _paletteManager.SavePalettes();
            PopulatePaletteList();
            string newDisplayName = newPalette.IsBuiltIn ? $"{newPalette.Name} [Встроенная]" : newPalette.Name;
            lbPalettes.SelectedItem = newDisplayName;
            MessageBox.Show($"Палитра '{newName}' создана и сохранена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            _paletteManager.SavePalettes();
            PopulatePaletteList();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_selectedPalette == null)
            {
                return;
            }

            _paletteManager.ActivePalette = _selectedPalette;
            PaletteChanged?.Invoke(this, _selectedPalette);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void ApplyNewtonPaletteThemeHints()
        {
            ThemeDefinition theme = ThemeManager.CurrentDefinition;
            lblEditHint.ForeColor = theme.SecondaryText;
            lblRootCountValue.ForeColor = theme.PrimaryText;
            lblEditHint.Text = _selectedPalette?.IsBuiltIn == true
                ? "Встроенная палитра доступна только для просмотра"
                : "Текущая палитра редактируема";
        }

        public static List<Color> GenerateHarmonicColors(int count)
        {
            List<Color> colors = new();
            if (count <= 0)
            {
                return colors;
            }

            for (int i = 0; i < count; i++)
            {
                float hue = (360f * i) / count;
                colors.Add(ColorFromHSL(hue, 0.85f, 0.6f));
            }

            return colors;
        }

        public static Color ColorFromHSL(float h, float s, float l)
        {
            float r;
            float g;
            float b;

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
            if (t < 0)
            {
                t += 1;
            }

            if (t > 1)
            {
                t -= 1;
            }

            if (t < 1 / 6f)
            {
                return p + (q - p) * 6 * t;
            }

            if (t < 1 / 2f)
            {
                return q;
            }

            if (t < 2 / 3f)
            {
                return p + (q - p) * (2 / 3f - t) * 6;
            }

            return p;
        }

        private void ColorSetting_FormClosing(object sender, FormClosingEventArgs e)
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
