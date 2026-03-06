using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;

namespace FractalExplorer.Utilities.ColorPicking
{
    public sealed class ScreenEyedropper
    {
        private static int _isRunning;

        public bool TryPickColor(IWin32Window? owner, out Color selectedColor)
        {
            selectedColor = Color.Empty;

            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            {
                return false;
            }

            try
            {
                using EyedropperOverlayForm overlay = new();
                DialogResult result = owner is null ? overlay.ShowDialog() : overlay.ShowDialog(owner);

                if (result != DialogResult.OK || overlay.SelectedColor is null)
                {
                    return false;
                }

                selectedColor = overlay.SelectedColor.Value;
                return true;
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 0);
            }
        }

        private static Color GetColorAtScreenPoint(Point screenPoint)
        {
            using Bitmap bitmap = new(1, 1, PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(screenPoint, Point.Empty, new Size(1, 1));
            return bitmap.GetPixel(0, 0);
        }

        private sealed class EyedropperOverlayForm : Form
        {
            public Color? SelectedColor { get; private set; }

            public EyedropperOverlayForm()
            {
                Rectangle virtualScreenBounds = SystemInformation.VirtualScreen;

                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                Bounds = virtualScreenBounds;
                TopMost = true;
                KeyPreview = true;
                BackColor = Color.Black;
                Opacity = 0.2;
                Cursor = Cursors.Cross;
                DoubleBuffered = true;
            }

            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);
                Activate();
                Focus();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);

                if (e.Button == MouseButtons.Left)
                {
                    Point cursorPosition = Cursor.Position;
                    SelectedColor = GetColorAtScreenPoint(cursorPosition);
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                if (e.Button == MouseButtons.Right)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);

                if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            }
        }
    }
}
