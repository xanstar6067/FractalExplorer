using FractalDraving;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.Theme;

namespace FractalExplorer.Utilities
{
    /// <summary>
    /// Окно runtime-настроек режима окрашивания и быстрой смены палитры.
    /// Не управляет CRUD палитр, а только применяет выбранные параметры.
    /// </summary>
    public sealed class ColoringModeSettingsForm : Form
    {
        private readonly PaletteManager _paletteManager;
        private readonly FractalMandelbrotFamilyForm.ColoringRuntimeState _workingState;

        private readonly ComboBox _cbMode = new();
        private readonly ComboBox _cbPalette = new();
        private readonly Panel _dynamicParametersPanel = new();
        private readonly Button _btnApply = new();
        private readonly Button _btnCancel = new();

        private NumericUpDown? _nudBlendPower;
        private NumericUpDown? _nudIterationOffset;
        private CheckBox? _chkHistogramEqualization;
        private NumericUpDown? _nudHistogramContrast;
        private CheckBox? _chkHistogramInputUseSmooth;

        public event EventHandler<ColoringModeSettingsAppliedEventArgs>? SettingsApplied;

        public ColoringModeSettingsForm(
            PaletteManager paletteManager,
            FractalMandelbrotFamilyForm.ColoringRuntimeState runtimeState,
            string? activePaletteName,
            Icon? ownerIcon)
        {
            _paletteManager = paletteManager ?? throw new ArgumentNullException(nameof(paletteManager));
            _workingState = runtimeState?.Clone() ?? throw new ArgumentNullException(nameof(runtimeState));
            ThemeManager.RegisterForm(this);

            InitializeUi();
            PopulateModes();
            PopulatePalettes(activePaletteName);
            ApplyStateToUi();

            if (ownerIcon is not null)
            {
                Icon = ownerIcon;
            }
        }

        private void InitializeUi()
        {
            Text = "Настройки окраски";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = true;
            Width = 520;
            Height = 360;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var modeRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true };
            modeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            modeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            modeRow.Controls.Add(new Label { Text = "Режим окраски:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) }, 0, 0);
            _cbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbMode.Dock = DockStyle.Top;
            _cbMode.SelectedIndexChanged += (_, _) => RebuildDynamicParametersPanel();
            modeRow.Controls.Add(_cbMode, 1, 0);

            var paletteRow = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            paletteRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            paletteRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            paletteRow.Controls.Add(new Label { Text = "Палитра (быстро):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) }, 0, 0);
            _cbPalette.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbPalette.Dock = DockStyle.Top;
            paletteRow.Controls.Add(_cbPalette, 1, 0);

            var group = new GroupBox { Text = "Параметры выбранного режима", Dock = DockStyle.Fill, Margin = new Padding(0, 12, 0, 0) };
            _dynamicParametersPanel.Dock = DockStyle.Fill;
            _dynamicParametersPanel.Padding = new Padding(8);
            group.Controls.Add(_dynamicParametersPanel);

            var buttonsRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 0)
            };

            _btnApply.Text = "Применить";
            _btnApply.AutoSize = true;
            _btnApply.Click += BtnApply_Click;

            _btnCancel.Text = "Отмена";
            _btnCancel.AutoSize = true;
            _btnCancel.Click += (_, _) => Close();

            buttonsRow.Controls.Add(_btnApply);
            buttonsRow.Controls.Add(_btnCancel);

            root.Controls.Add(modeRow, 0, 0);
            root.Controls.Add(paletteRow, 0, 1);
            root.Controls.Add(group, 0, 2);
            root.Controls.Add(buttonsRow, 0, 3);

            Controls.Add(root);
        }

        private void PopulateModes()
        {
            _cbMode.Items.Clear();
            _cbMode.Items.Add(new ModeItem(FractalMandelbrotFamilyForm.ColoringModeType.Discrete, "Дискретно"));
            _cbMode.Items.Add(new ModeItem(FractalMandelbrotFamilyForm.ColoringModeType.Smooth, "Плавно"));
            _cbMode.Items.Add(new ModeItem(FractalMandelbrotFamilyForm.ColoringModeType.Histogram, "Histogram"));
            _cbMode.Items.Add(new ModeItem(FractalMandelbrotFamilyForm.ColoringModeType.OrbitTrap, "Orbit Trap"));
            _cbMode.Items.Add(new ModeItem(FractalMandelbrotFamilyForm.ColoringModeType.StripeAverage, "Stripe Average"));
        }

        private void PopulatePalettes(string? activePaletteName)
        {
            _cbPalette.Items.Clear();
            foreach (var palette in _paletteManager.Palettes)
            {
                string label = palette.IsBuiltIn ? $"{palette.Name} [Встроенная]" : $"{palette.Name} [Пользовательская]";
                _cbPalette.Items.Add(new PaletteItem(palette.Name, label));
            }

            if (_cbPalette.Items.Count == 0)
            {
                return;
            }

            PaletteItem? selectedItem = null;
            if (!string.IsNullOrWhiteSpace(activePaletteName))
            {
                selectedItem = _cbPalette.Items
                    .OfType<PaletteItem>()
                    .FirstOrDefault(p => p.PaletteName == activePaletteName);
            }

            _cbPalette.SelectedItem = selectedItem ?? _cbPalette.Items[0];
        }

        private void ApplyStateToUi()
        {
            var modeItem = _cbMode.Items
                .OfType<ModeItem>()
                .FirstOrDefault(m => m.Mode == _workingState.ActiveMode.ModeType);

            _cbMode.SelectedItem = modeItem ?? _cbMode.Items.OfType<ModeItem>().FirstOrDefault();
            RebuildDynamicParametersPanel();
        }

        private void RebuildDynamicParametersPanel()
        {
            _dynamicParametersPanel.Controls.Clear();
            _nudBlendPower = null;
            _nudIterationOffset = null;
            _chkHistogramEqualization = null;
            _nudHistogramContrast = null;
            _chkHistogramInputUseSmooth = null;

            var selectedMode = (_cbMode.SelectedItem as ModeItem)?.Mode
                ?? FractalMandelbrotFamilyForm.ColoringModeType.Smooth;

            if (selectedMode == FractalMandelbrotFamilyForm.ColoringModeType.Smooth)
            {
                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 2,
                    AutoSize = true
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                layout.Controls.Add(new Label { Text = "Плавность (степень):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) }, 0, 0);
                _nudBlendPower = new NumericUpDown
                {
                    DecimalPlaces = 2,
                    Minimum = 0.10m,
                    Maximum = 5.00m,
                    Increment = 0.05m,
                    Value = (decimal)Math.Max(0.10, Math.Min(5.00, _workingState.SmoothBlendPower)),
                    Dock = DockStyle.Fill
                };
                layout.Controls.Add(_nudBlendPower, 1, 0);

                layout.Controls.Add(new Label { Text = "Смещение итерации:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) }, 0, 1);
                _nudIterationOffset = new NumericUpDown
                {
                    DecimalPlaces = 2,
                    Minimum = -100.00m,
                    Maximum = 100.00m,
                    Increment = 0.10m,
                    Value = (decimal)Math.Max(-100.00, Math.Min(100.00, _workingState.SmoothIterationOffset)),
                    Dock = DockStyle.Fill
                };
                layout.Controls.Add(_nudIterationOffset, 1, 1);

                _dynamicParametersPanel.Controls.Add(layout);
                return;
            }

            if (selectedMode == FractalMandelbrotFamilyForm.ColoringModeType.Histogram)
            {
                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    ColumnCount = 2,
                    AutoSize = true
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                _chkHistogramEqualization = new CheckBox
                {
                    AutoSize = true,
                    Text = "Включить equalization (CDF)",
                    Checked = _workingState.HistogramSettings.EnabledEqualization,
                    Dock = DockStyle.Fill
                };
                layout.Controls.Add(_chkHistogramEqualization, 0, 0);
                layout.SetColumnSpan(_chkHistogramEqualization, 2);

                layout.Controls.Add(new Label { Text = "Контраст:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) }, 0, 1);
                _nudHistogramContrast = new NumericUpDown
                {
                    DecimalPlaces = 2,
                    Minimum = 0.10m,
                    Maximum = 4.00m,
                    Increment = 0.05m,
                    Value = (decimal)Math.Max(0.10, Math.Min(4.00, _workingState.HistogramSettings.Contrast)),
                    Dock = DockStyle.Fill
                };
                layout.Controls.Add(_nudHistogramContrast, 1, 1);

                _chkHistogramInputUseSmooth = new CheckBox
                {
                    AutoSize = true,
                    Text = "Вход по smooth-итерациям",
                    Checked = _workingState.HistogramSettings.InputUseSmooth,
                    Dock = DockStyle.Fill
                };
                layout.Controls.Add(_chkHistogramInputUseSmooth, 0, 2);
                layout.SetColumnSpan(_chkHistogramInputUseSmooth, 2);

                _dynamicParametersPanel.Controls.Add(layout);
                return;
            }

            _dynamicParametersPanel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Text = "У выбранного режима нет дополнительных параметров."
            });
        }

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            var mode = (_cbMode.SelectedItem as ModeItem)?.Mode
                ?? FractalMandelbrotFamilyForm.ColoringModeType.Smooth;

            _workingState.ActiveMode = FractalMandelbrotFamilyForm.ColoringModeRuntime.FromType(mode);
            if (_nudBlendPower is not null)
            {
                _workingState.SmoothBlendPower = (double)_nudBlendPower.Value;
            }

            if (_nudIterationOffset is not null)
            {
                _workingState.SmoothIterationOffset = (double)_nudIterationOffset.Value;
            }
            if (_chkHistogramEqualization is not null)
            {
                _workingState.HistogramSettings.EnabledEqualization = _chkHistogramEqualization.Checked;
            }
            if (_nudHistogramContrast is not null)
            {
                _workingState.HistogramSettings.Contrast = (double)_nudHistogramContrast.Value;
            }
            if (_chkHistogramInputUseSmooth is not null)
            {
                _workingState.HistogramSettings.InputUseSmooth = _chkHistogramInputUseSmooth.Checked;
            }

            var selectedPaletteName = (_cbPalette.SelectedItem as PaletteItem)?.PaletteName;

            SettingsApplied?.Invoke(this, new ColoringModeSettingsAppliedEventArgs(
                _workingState.Clone(),
                selectedPaletteName));

            Close();
        }

        private sealed record ModeItem(FractalMandelbrotFamilyForm.ColoringModeType Mode, string DisplayName)
        {
            public override string ToString() => DisplayName;
        }

        private sealed record PaletteItem(string PaletteName, string DisplayName)
        {
            public override string ToString() => DisplayName;
        }
    }

    public sealed class ColoringModeSettingsAppliedEventArgs : EventArgs
    {
        public ColoringModeSettingsAppliedEventArgs(
            FractalMandelbrotFamilyForm.ColoringRuntimeState runtimeState,
            string? selectedPaletteName)
        {
            RuntimeState = runtimeState;
            SelectedPaletteName = selectedPaletteName;
        }

        public FractalMandelbrotFamilyForm.ColoringRuntimeState RuntimeState { get; }
        public string? SelectedPaletteName { get; }
    }
}
