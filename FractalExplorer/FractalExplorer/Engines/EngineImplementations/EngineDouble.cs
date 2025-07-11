using FractalExplorer.Engines.EngineInterfaces;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using System.Globalization;

namespace FractalExplorer.Engines.EngineImplementations
{
    public class EngineDouble : EngineBaseMandelbrotFamily
    {
        protected override int GetIterationsForPixel(int px, int py, int width, int height, RenderOptions options)
        {
            // 1. Парсим строковые параметры в double
            double centerX = double.Parse(options.CenterX, CultureInfo.InvariantCulture);
            double centerY = double.Parse(options.CenterY, CultureInfo.InvariantCulture);
            double scale = double.Parse(options.Scale, CultureInfo.InvariantCulture);

            // 2. Рассчитываем координаты точки на комплексной плоскости
            double unitsPerPixel = scale / width;
            double re = centerX + (px - width / 2.0) * unitsPerPixel;
            double im = centerY - (py - height / 2.0) * unitsPerPixel;

            // 3. Вызываем соответствующий метод расчета в зависимости от типа фрактала
            switch (options.FractalType)
            {
                case FractalType.Mandelbrot:
                    return CalculateMandelbrot(new ComplexDouble(re, im));
                case FractalType.Julia:
                    return CalculateJulia(new ComplexDouble(re, im), options.JuliaC);
                case FractalType.MandelbrotBurningShip:
                    return CalculateMandelbrotBurningShip(new ComplexDouble(re, im));
                case FractalType.JuliaBurningShip:
                    return CalculateJuliaBurningShip(new ComplexDouble(re, im), options.JuliaC);
                default:
                    return 0;
            }
        }

        // --- Методы расчета для конкретных фракталов с точностью double ---

        private int CalculateMandelbrot(ComplexDouble c)
        {
            int iter = 0;
            var z = ComplexDouble.Zero;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        private int CalculateJulia(ComplexDouble z, ComplexDecimal c_decimal)
        {
            int iter = 0;
            var c = new ComplexDouble((double)c_decimal.Real, (double)c_decimal.Imaginary); // Константа C может быть менее точной
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        private int CalculateMandelbrotBurningShip(ComplexDouble c)
        {
            int iter = 0;
            var z = ComplexDouble.Zero;
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = new ComplexDouble(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        private int CalculateJuliaBurningShip(ComplexDouble z, ComplexDecimal c_decimal)
        {
            int iter = 0;
            var c = new ComplexDouble((double)c_decimal.Real, (double)c_decimal.Imaginary);
            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                z = new ComplexDouble(Math.Abs(z.Real), Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }
}
