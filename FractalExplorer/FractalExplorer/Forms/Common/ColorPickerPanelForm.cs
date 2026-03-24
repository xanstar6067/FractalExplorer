using FractalExplorer.Properties;
using FractalExplorer.Utilities.ColorPicking;
using FractalExplorer.Utilities.Theme;
using System.Globalization;
using System.Windows.Forms;

namespace FractalExplorer.Forms.Common
{
    public partial class ColorPickerPanelForm : Form
    {
        private const int CustomColorSlotsCount = 16;

        private readonly Color _originalColor;
        private readonly ScreenEyedropper _screenEyedropper = new();

        private readonly List<Color> _standardColors = new()
        {
            Color.FromArgb(255, 128, 128), Color.FromArgb(255, 255, 128), Color.FromArgb(128, 255, 128), Color.FromArgb(0, 255, 128),
            Color.FromArgb(128, 255, 255), Color.FromArgb(0, 128, 255), Color.FromArgb(255, 128, 192), Color.FromArgb(255, 128, 255),

            Color.FromArgb(255, 0, 0), Color.FromArgb(255, 255, 0), Color.FromArgb(128, 255, 0), Color.FromArgb(0, 255, 64),
            Color.FromArgb(0, 255, 255), Color.FromArgb(0, 128, 192), Color.FromArgb(128, 128, 192), Color.FromArgb(255, 0, 255),

            Color.FromArgb(128, 64, 64), Color.FromArgb(255, 128, 64), Color.FromArgb(0, 255, 0), Color.FromArgb(0, 128, 128),
            Color.FromArgb(0, 64, 128), Color.FromArgb(128, 128, 255), Color.FromArgb(128, 0, 64), Color.FromArgb(255, 0, 128),

            Color.FromArgb(128, 0, 0), Color.FromArgb(255, 128, 0), Color.FromArgb(0, 128, 0), Color.FromArgb(0, 128, 64),
            Color.FromArgb(0, 0, 255), Color.FromArgb(0, 0, 160), Color.FromArgb(128, 0, 128), Color.FromArgb(128, 0, 255),

            Color.FromArgb(64, 0, 0), Color.FromArgb(128, 64, 0), Color.FromArgb(0, 64, 0), Color.FromArgb(0, 64, 64),
            Color.FromArgb(0, 0, 128), Color.FromArgb(0, 0, 64), Color.FromArgb(64, 0, 64), Color.FromArgb(64, 0, 128),

            Color.Black, Color.FromArgb(64, 64, 64), Color.FromArgb(128, 128, 128), Color.FromArgb(192, 192, 192),
            Color.White, Color.FromArgb(255, 255, 224), Color.FromArgb(255, 224, 192), Color.FromArgb(255, 224, 255)
        };

        private readonly Color[] _customColors = new Color[CustomColorSlotsCount];
        private readonly List<Panel> _customColorCells = new();

        private Color _selectedColor;
        private bool _isUpdatingControls;
        private float _hue;
        private float _saturation;
        private float _brightness;

        private Bitmap? _matrixBitmap;
        private Bitmap? _hueBitmap;

        public Color SelectedColor => _selectedColor;

        public ColorPickerPanelForm(Color initialColor)
        {
            InitializeComponent();

            ThemeManager.RegisterForm(this);
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
            Disposed += ColorPickerPanelForm_Disposed;

            _originalColor = initialColor;

            LoadCustomColors();
            BuildStandardPalette();
            BuildCustomPalette();
            BuildHueBitmap();

            ApplySelectedColor(initialColor);
        }

        private void ColorPickerPanelForm_Disposed(object? sender, EventArgs e)
        {
            ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            Disposed -= ColorPickerPanelForm_Disposed;
            _matrixBitmap?.Dispose();
            _hueBitmap?.Dispose();
        }

        private void ThemeManager_ThemeChanged(object? sender, EventArgs e)
        {
            BuildHueBitmap();
            RedrawMatrixBitmap();
            ApplySelectedColor(_selectedColor);
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

            ApplySelectedColor(Color.FromArgb(trkRed.Value, trkGreen.Value, trkBlue.Value));
        }

        private void pnlColorMatrix_Paint(object? sender, PaintEventArgs e)
        {
            if (_matrixBitmap != null)
            {
                e.Graphics.DrawImage(_matrixBitmap, 0, 0, pnlColorMatrix.Width, pnlColorMatrix.Height);
            }

            Point marker = GetMatrixMarkerPoint();
            Rectangle markerRect = new(marker.X - 4, marker.Y - 4, 8, 8);
            e.Graphics.DrawEllipse(Pens.Black, markerRect);
            e.Graphics.DrawEllipse(Pens.White, markerRect.X + 1, markerRect.Y + 1, markerRect.Width - 2, markerRect.Height - 2);
        }

        private void pnlColorMatrix_MouseDown(object? sender, MouseEventArgs e)
        {
            UpdateColorFromMatrix(e.Location);
        }

        private void pnlColorMatrix_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                UpdateColorFromMatrix(e.Location);
            }
        }

        private void pnlHueSlider_Paint(object? sender, PaintEventArgs e)
        {
            if (_hueBitmap != null)
            {
                e.Graphics.DrawImage(_hueBitmap, 0, 0, pnlHueSlider.Width, pnlHueSlider.Height);
            }

            int markerY = GetHueMarkerPosition();
            Rectangle markerRect = new(0, markerY - 3, pnlHueSlider.Width - 1, 6);
            e.Graphics.DrawRectangle(Pens.Black, markerRect);
        }

        private void pnlHueSlider_MouseDown(object? sender, MouseEventArgs e)
        {
            UpdateHueFromPoint(e.Y);
        }

        private void pnlHueSlider_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                UpdateHueFromPoint(e.Y);
            }
        }

        private void btnAddToCustomColors_Click(object? sender, EventArgs e)
        {
            for (int i = 0; i < _customColors.Length; i++)
            {
                if (_customColors[i] == Color.Empty)
                {
                    _customColors[i] = _selectedColor;
                    RefreshCustomColorCell(i);
                    SaveCustomColors();
                    return;
                }
            }

            _customColors[0] = _selectedColor;
            RefreshCustomColorCell(0);
            SaveCustomColors();
        }

        private void CustomPaletteCell_Click(object? sender, EventArgs e)
        {
            if (sender is not Panel panel || panel.Tag is not int index)
            {
                return;
            }

            if (_customColors[index] != Color.Empty)
            {
                ApplySelectedColor(_customColors[index]);
            }
            else
            {
                _customColors[index] = _selectedColor;
                RefreshCustomColorCell(index);
                SaveCustomColors();
            }
        }

        private void StandardPaletteCell_Click(object? sender, EventArgs e)
        {
            if (sender is Panel panel && panel.Tag is Color color)
            {
                ApplySelectedColor(color);
            }
        }

        private void ApplySelectedColor(Color color)
        {
            _selectedColor = color;
            _hue = color.GetHue();
            _saturation = color.GetSaturation();
            _brightness = color.GetBrightness();

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

            lblRedValue.Text = color.R.ToString(CultureInfo.InvariantCulture);
            lblGreenValue.Text = color.G.ToString(CultureInfo.InvariantCulture);
            lblBlueValue.Text = color.B.ToString(CultureInfo.InvariantCulture);

            pnlCurrentColor.BackColor = _originalColor;
            pnlNewColor.BackColor = _selectedColor;

            lblCurrentHexValue.Text = ToHexString(_originalColor);
            lblNewHexValue.Text = ToHexString(_selectedColor);

            RedrawMatrixBitmap();
            pnlColorMatrix.Invalidate();
            pnlHueSlider.Invalidate();
        }

        private void UpdateColorFromMatrix(Point location)
        {
            if (pnlColorMatrix.Width <= 1 || pnlColorMatrix.Height <= 1)
            {
                return;
            }

            int x = Math.Max(0, Math.Min(pnlColorMatrix.Width - 1, location.X));
            int y = Math.Max(0, Math.Min(pnlColorMatrix.Height - 1, location.Y));

            _saturation = x / (float)(pnlColorMatrix.Width - 1);
            _brightness = 1f - (y / (float)(pnlColorMatrix.Height - 1));

            ApplySelectedColor(FromAhsb(255, _hue, _saturation, _brightness));
        }

        private void UpdateHueFromPoint(int y)
        {
            if (pnlHueSlider.Height <= 1)
            {
                return;
            }

            int clampedY = Math.Max(0, Math.Min(pnlHueSlider.Height - 1, y));
            _hue = 360f - (clampedY / (float)(pnlHueSlider.Height - 1)) * 360f;
            if (_hue >= 360f)
            {
                _hue = 0f;
            }

            ApplySelectedColor(FromAhsb(255, _hue, _saturation, _brightness));
        }

        private Point GetMatrixMarkerPoint()
        {
            int x = (int)Math.Round(_saturation * Math.Max(1, pnlColorMatrix.Width - 1));
            int y = (int)Math.Round((1f - _brightness) * Math.Max(1, pnlColorMatrix.Height - 1));
            return new Point(x, y);
        }

        private int GetHueMarkerPosition()
        {
            float normalized = 1f - (_hue / 360f);
            return (int)Math.Round(normalized * Math.Max(1, pnlHueSlider.Height - 1));
        }

        private void BuildStandardPalette()
        {
            tableStandardColors.SuspendLayout();
            tableStandardColors.Controls.Clear();

            for (int i = 0; i < _standardColors.Count; i++)
            {
                Panel cell = CreateColorCell(_standardColors[i], i, StandardPaletteCell_Click);
                tableStandardColors.Controls.Add(cell, i % 8, i / 8);
            }

            tableStandardColors.ResumeLayout();
        }

        private void BuildCustomPalette()
        {
            tableCustomColors.SuspendLayout();
            tableCustomColors.Controls.Clear();
            _customColorCells.Clear();

            for (int i = 0; i < _customColors.Length; i++)
            {
                Panel cell = CreateColorCell(_customColors[i], i, CustomPaletteCell_Click);
                _customColorCells.Add(cell);
                tableCustomColors.Controls.Add(cell, i % 8, i / 8);
            }

            tableCustomColors.ResumeLayout();
        }

        private static Panel CreateColorCell(Color color, object tag, EventHandler clickHandler)
        {
            Panel cell = new()
            {
                BackColor = color == Color.Empty ? Color.White : color,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
                Cursor = Cursors.Hand,
                Tag = tag
            };

            cell.Click += clickHandler;
            return cell;
        }

        private void RefreshCustomColorCell(int index)
        {
            if (index < 0 || index >= _customColorCells.Count)
            {
                return;
            }

            _customColorCells[index].BackColor = _customColors[index] == Color.Empty ? Color.White : _customColors[index];
        }

        private void RedrawMatrixBitmap()
        {
            _matrixBitmap?.Dispose();

            int width = Math.Max(1, pnlColorMatrix.Width);
            int height = Math.Max(1, pnlColorMatrix.Height);
            _matrixBitmap = new Bitmap(width, height);

            for (int y = 0; y < height; y++)
            {
                float b = 1f - (y / (float)Math.Max(1, height - 1));
                for (int x = 0; x < width; x++)
                {
                    float s = x / (float)Math.Max(1, width - 1);
                    _matrixBitmap.SetPixel(x, y, FromAhsb(255, _hue, s, b));
                }
            }
        }

        private void BuildHueBitmap()
        {
            _hueBitmap?.Dispose();

            int width = Math.Max(1, pnlHueSlider.Width);
            int height = Math.Max(1, pnlHueSlider.Height);
            _hueBitmap = new Bitmap(width, height);

            for (int y = 0; y < height; y++)
            {
                float h = 360f - (y / (float)Math.Max(1, height - 1)) * 360f;
                if (h >= 360f)
                {
                    h = 0f;
                }

                Color color = FromAhsb(255, h, 1f, 1f);
                for (int x = 0; x < width; x++)
                {
                    _hueBitmap.SetPixel(x, y, color);
                }
            }
        }

        private void ColorPickerPanelForm_Resize(object? sender, EventArgs e)
        {
            BuildHueBitmap();
            RedrawMatrixBitmap();
            pnlColorMatrix.Invalidate();
            pnlHueSlider.Invalidate();
        }

        private void LoadCustomColors()
        {
            string serialized = Settings.Default.ColorPicker_CustomColors;
            if (string.IsNullOrWhiteSpace(serialized))
            {
                Array.Fill(_customColors, Color.Empty);
                return;
            }

            string[] chunks = serialized.Split(';');
            for (int i = 0; i < _customColors.Length; i++)
            {
                if (i < chunks.Length && int.TryParse(chunks[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int argb))
                {
                    _customColors[i] = Color.FromArgb(argb);
                }
                else
                {
                    _customColors[i] = Color.Empty;
                }
            }
        }

        private void SaveCustomColors()
        {
            string serialized = string.Join(";", _customColors.Select(c => c == Color.Empty ? int.MinValue : c.ToArgb().ToString(CultureInfo.InvariantCulture)));
            Settings.Default.ColorPicker_CustomColors = serialized;
            Settings.Default.Save();
        }

        private static string ToHexString(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static Color FromAhsb(int alpha, float hue, float saturation, float brightness)
        {
            if (0 > alpha || 255 < alpha)
            {
                throw new ArgumentOutOfRangeException(nameof(alpha));
            }

            if (0f > hue || 360f < hue)
            {
                throw new ArgumentOutOfRangeException(nameof(hue));
            }

            if (0f > saturation || 1f < saturation)
            {
                throw new ArgumentOutOfRangeException(nameof(saturation));
            }

            if (0f > brightness || 1f < brightness)
            {
                throw new ArgumentOutOfRangeException(nameof(brightness));
            }

            if (0 == saturation)
            {
                int l = Convert.ToInt32(brightness * 255);
                return Color.FromArgb(alpha, l, l, l);
            }

            float fMax;
            float fMid;
            float fMin;

            int sextant;
            if (0.5 < brightness)
            {
                fMax = brightness - (brightness * saturation) + saturation;
                fMin = brightness + (brightness * saturation) - saturation;
            }
            else
            {
                fMax = brightness + (brightness * saturation);
                fMin = brightness - (brightness * saturation);
            }

            sextant = (int)Math.Floor(hue / 60f);
            if (300f <= hue)
            {
                hue -= 360f;
            }

            hue /= 60f;
            hue -= 2f * (float)Math.Floor(((sextant + 1f) % 6f) / 2f);
            if (0 == sextant % 2)
            {
                fMid = hue * (fMax - fMin) + fMin;
            }
            else
            {
                fMid = fMin - hue * (fMax - fMin);
            }

            int iMax = Convert.ToInt32(fMax * 255);
            int iMid = Convert.ToInt32(fMid * 255);
            int iMin = Convert.ToInt32(fMin * 255);

            return sextant switch
            {
                1 => Color.FromArgb(alpha, iMid, iMax, iMin),
                2 => Color.FromArgb(alpha, iMin, iMax, iMid),
                3 => Color.FromArgb(alpha, iMin, iMid, iMax),
                4 => Color.FromArgb(alpha, iMid, iMin, iMax),
                5 => Color.FromArgb(alpha, iMax, iMin, iMid),
                _ => Color.FromArgb(alpha, iMax, iMid, iMin)
            };
        }
    }
}
