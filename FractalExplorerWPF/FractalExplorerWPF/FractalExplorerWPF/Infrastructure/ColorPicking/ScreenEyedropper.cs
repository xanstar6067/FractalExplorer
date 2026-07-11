using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using Color = System.Windows.Media.Color;
using DrawingColor = System.Drawing.Color;
using DrawingPoint = System.Drawing.Point;

namespace FractalExplorerWPF.Infrastructure.ColorPicking;

/// <summary>
/// Reusable screen eyedropper. The public API uses WPF types; the input overlay uses
/// the proven Win32 Forms implementation because layered full-screen WPF windows are
/// rendered as opaque black on some GPU/desktop-compositor configurations.
/// </summary>
public sealed class ScreenEyedropper
{
    private static int _isRunning;

    public bool TryPickColor(Window? owner, out Color selectedColor)
    {
        selectedColor = System.Windows.Media.Colors.Transparent;
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0) return false;

        try
        {
            using var overlay = new EyedropperOverlayForm();
            System.Windows.Forms.DialogResult result;
            if (owner is null)
            {
                result = overlay.ShowDialog();
            }
            else
            {
                var nativeOwner = new NativeWindowOwner(new WindowInteropHelper(owner).Handle);
                result = overlay.ShowDialog(nativeOwner);
            }

            if (result != System.Windows.Forms.DialogResult.OK || overlay.SelectedColor is not DrawingColor color)
                return false;

            selectedColor = Color.FromRgb(color.R, color.G, color.B);
            return true;
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }
    }

    private static DrawingColor GetColorAtScreenPoint(DrawingPoint screenPoint)
    {
        using var bitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(screenPoint, DrawingPoint.Empty, new System.Drawing.Size(1, 1));
        return bitmap.GetPixel(0, 0);
    }

    private sealed class EyedropperOverlayForm : System.Windows.Forms.Form
    {
        public DrawingColor? SelectedColor { get; private set; }

        public EyedropperOverlayForm()
        {
            System.Drawing.Rectangle virtualScreen = System.Windows.Forms.SystemInformation.VirtualScreen;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            Bounds = virtualScreen;
            TopMost = true;
            KeyPreview = true;
            BackColor = DrawingColor.Black;
            Opacity = 0.2;
            Cursor = System.Windows.Forms.Cursors.Cross;
            DoubleBuffered = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            Focus();
        }

        protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                SelectedColor = GetColorAtScreenPoint(System.Windows.Forms.Cursor.Position);
                DialogResult = System.Windows.Forms.DialogResult.OK;
                Close();
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                DialogResult = System.Windows.Forms.DialogResult.Cancel;
                Close();
            }
        }

        protected override void OnKeyDown(System.Windows.Forms.KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode != System.Windows.Forms.Keys.Escape) return;
            e.Handled = true;
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }
    }

    private sealed class NativeWindowOwner(nint handle) : System.Windows.Forms.IWin32Window
    {
        public nint Handle { get; } = handle;
    }
}
