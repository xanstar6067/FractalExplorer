using System.Drawing;

namespace FractalDraving
{
    /// <summary>
    /// Представляет одну плитку (тайл) для мозаичного рендеринга.
    /// </summary>
    public readonly struct TileInfo
    {
        /// <summary>
        /// Границы плитки в пиксельных координатах.
        /// </summary>
        public readonly Rectangle Bounds;

        /// <summary>
        /// Центральная точка плитки, используется для сортировки.
        /// </summary>
        public readonly Point Center;

        public TileInfo(int x, int y, int width, int height)
        {
            Bounds = new Rectangle(x, y, width, height);
            Center = new Point(x + width / 2, y + height / 2);
        }
    }
}