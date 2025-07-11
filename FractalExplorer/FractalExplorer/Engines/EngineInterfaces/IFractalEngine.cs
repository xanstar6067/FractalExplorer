using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace FractalExplorer.Engines.EngineInterfaces
{
    /// <summary>
    /// Определяет общий контракт для движков рендеринга фракталов
    /// с разной числовой точностью.
    /// </summary>
    public interface IFractalEngine
    {
        /// <summary>
        /// Максимальное количество итераций для одной точки.
        /// </summary>
        int MaxIterations { get; set; }

        /// <summary>
        /// Квадрат порога выхода из множества.
        /// </summary>
        double ThresholdSquared { get; set; }

        /// <summary>
        /// Функция для окрашивания пикселя в зависимости от количества итераций.
        /// </summary>
        Func<int, int, int, Color> Palette { get; set; }

        /// <summary>
        /// Максимальное количество итераций для нормализации цвета.
        /// </summary>
        int MaxColorIterations { get; set; }

        /// <summary>
        /// Рендерит фрактал в Bitmap.
        /// </summary>
        /// <param name="options">Параметры рендеринга.</param>
        /// <param name="cancellationToken">Токен для отмены операции.</param>
        /// <returns>Отрисованное изображение.</returns>
        Bitmap Render(RenderOptions options, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Контейнер для параметров рендеринга.
    /// Мы используем строки для чисел, чтобы сохранить независимость от типа.
    /// </summary>
    public class RenderOptions
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string CenterX { get; set; }
        public string CenterY { get; set; }
        public string Scale { get; set; }
        public int SsaaFactor { get; set; } = 1;
        public int NumThreads { get; set; } = Environment.ProcessorCount;

        // Определяет тип фрактала для рендеринга
        public FractalType FractalType { get; set; }

        // Константа 'C' для фракталов семейства Жюлиа.
        // Передается как ComplexDecimal, т.к. ее точность не требует BigDecimal.
        public ComplexDecimal JuliaC { get; set; }
    }

    /// <summary>
    /// Перечисление для типов фракталов, которые может рендерить движок.
    /// </summary>
    public enum FractalType
    {
        Mandelbrot,
        Julia,
        MandelbrotBurningShip,
        JuliaBurningShip
    }
}
