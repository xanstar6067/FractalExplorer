using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Core.Rendering;

public static class ProgressiveRenderBitmap
{
    /// <summary>
    /// Создаёт прозрачный слой для новых плиток. Пока плитка не готова, сквозь него остаётся
    /// виден трансформированный стабильный кадр; готовая плитка заменяет только свою область.
    /// </summary>
    public static WriteableBitmap CreateOverlay(int width, int height, double dpiX, double dpiY) =>
        new(width, height, dpiX, dpiY, PixelFormats.Bgra32, null);

    /// <summary>
    /// Создаёт самостоятельный непрозрачный чёрный буфер, когда нижнего стабильного кадра нет.
    /// </summary>
    public static WriteableBitmap CreateOpaque(int width, int height, double dpiX, double dpiY)
    {
        var bitmap = new WriteableBitmap(width, height, dpiX, dpiY, PixelFormats.Bgra32, null);
        byte[] opaqueBlackRow = new byte[checked(width * 4)];
        for (int offset = 3; offset < opaqueBlackRow.Length; offset += 4) opaqueBlackRow[offset] = byte.MaxValue;

        bitmap.Lock();
        try
        {
            for (int y = 0; y < height; y++)
                Marshal.Copy(opaqueBlackRow, 0, IntPtr.Add(bitmap.BackBuffer, y * bitmap.BackBufferStride), opaqueBlackRow.Length);
            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            bitmap.Unlock();
        }
        return bitmap;
    }

    public static WriteableBitmap CreateSeededOrOpaque(
        int width, int height, double dpiX, double dpiY, BitmapSource? seed)
    {
        if (seed is not null && seed.PixelWidth == width && seed.PixelHeight == height)
            return new WriteableBitmap(seed);
        return CreateOpaque(width, height, dpiX, dpiY);
    }

    /// <summary>
    /// Копирует BGRA-плитку с отсечением по границам назначения. Шаг исходной строки остаётся
    /// равным полной ширине исходной плитки — это важно для крайних и частично отсечённых блоков.
    /// </summary>
    public static bool WriteTile(WriteableBitmap bitmap, MandelbrotRenderTile tile, byte[] pixels)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(pixels);
        if (tile.Width <= 0 || tile.Height <= 0) return false;

        int left = Math.Max(0, tile.X);
        int top = Math.Max(0, tile.Y);
        int right = (int)Math.Min(bitmap.PixelWidth, (long)tile.X + tile.Width);
        int bottom = (int)Math.Min(bitmap.PixelHeight, (long)tile.Y + tile.Height);
        int copyWidth = right - left;
        int copyHeight = bottom - top;
        if (copyWidth <= 0 || copyHeight <= 0) return false;

        const int bytesPerPixel = 4;
        int sourceStride = checked(tile.Width * bytesPerPixel);
        int sourceOffset = checked((top - tile.Y) * sourceStride + (left - tile.X) * bytesPerPixel);
        long requiredLength = (long)sourceOffset + (long)(copyHeight - 1) * sourceStride + (long)copyWidth * bytesPerPixel;
        if (sourceOffset < 0 || requiredLength > pixels.LongLength)
            throw new ArgumentException("Буфер плитки не соответствует её координатам и размеру.", nameof(pixels));

        bitmap.WritePixels(new Int32Rect(left, top, copyWidth, copyHeight), pixels, sourceStride, sourceOffset);
        return true;
    }
}
