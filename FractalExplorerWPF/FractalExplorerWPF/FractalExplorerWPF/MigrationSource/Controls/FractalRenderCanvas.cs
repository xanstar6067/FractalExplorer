namespace FractalExplorer.Controls
{
    internal sealed class FractalRenderCanvas : PictureBox
    {
        public FractalRenderCanvas()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            UpdateStyles();
        }
    }
}
