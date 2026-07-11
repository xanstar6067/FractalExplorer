using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace FractalExplorerWPF.Infrastructure.ColorPicking;

/// <summary>Lets the user select a color from any pixel on the virtual desktop.</summary>
public sealed class ScreenEyedropper
{
    private static int _isRunning;

    public bool TryPickColor(Window? owner, out Color selectedColor)
    {
        selectedColor = Colors.Transparent;
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0) return false;

        try
        {
            var overlay = new EyedropperOverlayWindow();
            if (owner is { IsVisible: true }) overlay.Owner = owner;
            if (overlay.ShowDialog() != true || overlay.SelectedColor is not Color color) return false;
            selectedColor = color;
            return true;
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }
    }

    private static Color GetColorAtScreenPoint(int x, int y)
    {
        nint desktop = GetDC(0);
        if (desktop == 0) return Colors.Transparent;
        try
        {
            uint value = GetPixel(desktop, x, y);
            if (value == 0xFFFFFFFF) return Colors.Transparent;
            return Color.FromRgb((byte)(value & 0xFF), (byte)((value >> 8) & 0xFF), (byte)((value >> 16) & 0xFF));
        }
        finally
        {
            ReleaseDC(0, desktop);
        }
    }

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint window, nint deviceContext);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(nint deviceContext, int x, int y);

    private sealed class EyedropperOverlayWindow : Window
    {
        public Color? SelectedColor { get; private set; }

        public EyedropperOverlayWindow()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = true;
            Topmost = true;
            AllowsTransparency = true;
            Background = MediaBrushes.Transparent;
            Cursor = Cursors.Cross;
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ChangedButton == MouseButton.Left)
            {
                Point screenPoint = PointToScreen(e.GetPosition(this));
                SelectedColor = GetColorAtScreenPoint((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y));
                DialogResult = true;
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                DialogResult = false;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            DialogResult = false;
        }
    }
}
