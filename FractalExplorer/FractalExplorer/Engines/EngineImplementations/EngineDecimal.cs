using FractalExplorer.Engines.EngineInterfaces;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using System;
using System.Globalization;

namespace FractalExplorer.Engines.EngineImplementations
{
    public class EngineDecimal : EngineBaseMandelbrotFamily
    {
        // Порог для сравнения с MagnitudeSquared, преобразованный в decimal
        private decimal _thresholdSquaredDecimal;

        protected override int GetIterationsForPixel(int px, int py, int width, int height, RenderOptions options)
        {
            // Обновляем порог
            _thresholdSquaredDecimal = (decimal)this.ThresholdSquared;

            // 1. Парсим строковые параметры в decimal
            decimal centerX = decimal.Parse(options.CenterX, CultureInfo.InvariantCulture);
            decimal centerY = decimal.Parse(options.CenterY, CultureInfo.InvariantCulture);
            decimal scale = decimal.Parse(options.Scale, CultureInfo.InvariantCulture);

            // 2. Рассчитываем координаты точки
            decimal unitsPerPixel = scale / width;
            decimal re = centerX + (px - width / 2.0m) * unitsPerPixel;
            decimal im = centerY - (py - height / 2.0m) * unitsPerPixel;

            // 3. Вызываем соответствующий метод расчета
            switch (options.FractalType)
            {
                case FractalType.Mandelbrot:
                    return CalculateMandelbrot(new ComplexDecimal(re, im));
                case FractalType.Julia:
                    return CalculateJulia(new ComplexDecimal(re, im), options.JuliaC);
                case FractalType.MandelbrotBurningShip:
                    return CalculateMandelbrotBurningShip(new ComplexDecimal(re, im));
                case FractalType.JuliaBurningShip:
                    return CalculateJuliaBurningShip(new ComplexDecimal(re, im), options.JuliaC);
                default:
                    return 0;
            }
        }

        // --- Методы расчета для конкретных фракталов с точностью decimal ---

        private int CalculateMandelbrot(ComplexDecimal c)
        {
            int iter = 0;
            var z = ComplexDecimal.Zero;
            while (iter < MaxIterations && z.MagnitudeSquared <= _thresholdSquaredDecimal)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        private int CalculateJulia(ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= _thresholdSquaredDecimal)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        private int CalculateMandelbrotBurningShip(ComplexDecimal c)
        {
            int iter = 0;
            var z = ComplexDecimal.Zero;
            while (iter < MaxIterations && z.MagnitudeSquared <= _thresholdSquaredDecimal)
            {
                z = new ComplexDecimal(Math.Abs(z.Real), -Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        private int CalculateJuliaBurningShip(ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= _thresholdSquaredDecimal)
            {
                z = new ComplexDecimal(Math.Abs(z.Real), -Math.Abs(z.Imaginary));
                z = z * z + c;
                iter++;
            }
            return iter;
        }
    }
}