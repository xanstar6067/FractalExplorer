using FractalExplorer.Utilities.ColorPicking;
using FractalExplorer.Utilities.Theme;
using System.Windows.Forms;

namespace FractalExplorer.Forms.Common
{
    public partial class ColorPickerPanelForm : Form
    {
        private readonly Color _originalColor;
        private readonly ScreenEyedropper _screenEyedropper = new();
        private Color _selectedColor;
        private bool _isUpdatingControls;

        public Color SelectedColor => _selectedColor;

        public ColorPickerPanelForm(Color initialColor)
        {
            InitializeComponent();

            ThemeManager.RegisterForm(this);
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
            Disposed += ColorPickerPanelForm_Disposed;

            _originalColor = initialColor;
            ApplySelectedColor(initialColor);
        }

        private void ColorPickerPanelForm_Disposed(object? sender, EventArgs e)
        {
            ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            Disposed -= ColorPickerPanelForm_Disposed;
        }

        private void ThemeManager_ThemeChanged(object? sender, EventArgs e)
        {
            ApplySelectedColor(_selectedColor);
        }

        private void btnOpenColorDialog_Click(object? sender, EventArgs e)
        {
            using ColorDialog colorDialog = new()
            {
                AnyColor = true,
                FullOpen = true,
                Color = _selectedColor
            };

            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                ApplySelectedColor(colorDialog.Color);
            }
        }

        private void btnEyedropper_Click(object? sender, EventArgs e)
        {
            Hide();

            try
            {
                if (_screenEyedropper.TryPickColor(null, out Color pickedColor))
                {
                    ApplySelectedColor(pickedColor);
                }
            }
            finally
            {
                Show();
                Activate();
            }
        }

        private void trackColor_Scroll(object? sender, EventArgs e)
        {
            if (_isUpdatingControls)
            {
                return;
            }

            Color updatedColor = Color.FromArgb(trkRed.Value, trkGreen.Value, trkBlue.Value);
            ApplySelectedColor(updatedColor);
        }

        private void pnlNewColor_Click(object? sender, EventArgs e)
        {
            btnOpenColorDialog_Click(sender, e);
        }

        private void ApplySelectedColor(Color color)
        {
            _selectedColor = color;

            _isUpdatingControls = true;
            try
            {
                trkRed.Value = color.R;
                trkGreen.Value = color.G;
                trkBlue.Value = color.B;
            }
            finally
            {
                _isUpdatingControls = false;
            }

            lblRedValue.Text = color.R.ToString();
            lblGreenValue.Text = color.G.ToString();
            lblBlueValue.Text = color.B.ToString();

            pnlCurrentColor.BackColor = _originalColor;
            pnlNewColor.BackColor = _selectedColor;

            lblCurrentHexValue.Text = ToHexString(_originalColor);
            lblNewHexValue.Text = ToHexString(_selectedColor);
        }

        private static string ToHexString(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
