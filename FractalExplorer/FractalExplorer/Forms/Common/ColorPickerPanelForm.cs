using FractalExplorer.Utilities.ColorPicking;
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

            _originalColor = initialColor;
            ApplySelectedColor(initialColor);
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

        private void numericColor_ValueChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingControls)
            {
                return;
            }

            Color updatedColor = Color.FromArgb((int)numRed.Value, (int)numGreen.Value, (int)numBlue.Value);
            ApplySelectedColor(updatedColor);
        }

        private void ApplySelectedColor(Color color)
        {
            _selectedColor = color;

            _isUpdatingControls = true;
            try
            {
                numRed.Value = color.R;
                numGreen.Value = color.G;
                numBlue.Value = color.B;
            }
            finally
            {
                _isUpdatingControls = false;
            }

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
