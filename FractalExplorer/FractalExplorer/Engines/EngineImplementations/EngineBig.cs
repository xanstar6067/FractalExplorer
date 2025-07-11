using FractalExplorer.Engines.EngineInterfaces;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using System;

namespace FractalExplorer.Engines.EngineImplementations
{
    public class EngineBig : EngineBaseMandelbrotFamily
    {
        // Порог для сравнения, преобразованный в BigDecimal
        private readonly BigDecimal _thresholdSquaredBig = new BigDecimal(4.0);

        protected override int GetIterationsForPixel(int px, int py, int width, int height, RenderOptions options)
        {
            // 1. Парсим строковые параметры в BigDecimal
            var centerX = BigDecimal.Parse(options.CenterX);
            var centerY = BigDecimal.Parse(options.CenterY);
            var scale = BigDecimal.Parse(options.Scale);

            // 2. Рассчитываем координаты точки, используя BigDecimal
            var unitsPerPixel = scale / new BigDecimal(width);
            var re = centerX + (new BigDecimal(px) - new BigDecimal(width) / new BigDecimal(2)) * unitsPerPixel;
            var im = centerY - (new BigDecimal(py) - new BigDecimal(height) / new BigDecimal(2)) * unitsPerPixel;

            // 3. Преобразуем константу Julia в BigDecimal
            var juliaCBig = new ComplexBigDecimal(
                new BigDecimal(options.JuliaC.Real),
                new BigDecimal(options.JuliaC.Imaginary)
            );

            // 4. Вызываем соответствующий метод расчета
            switch (options.FractalType)
            {
                case FractalType.Mandelbrot:
                    return CalculateMandelbrot(new ComplexBigDecimal(re, im));
                case FractalType.Julia:
                    return CalculateJulia(new ComplexBigDecimal(re, im), juliaCBig);
                case FractalType.MandelbrotBurningShip:
                    return CalculateMandelbrotBurningShip(new ComplexBigDecimal(re, im));
                case FractalType.JuliaBurningShip:
                    return CalculateJuliaBurningShip(new ComplexBigDecimal(re, im), juliaCBig);
                default:
                    return 0;
            }
        }

        // --- Методы расчета для конкретных фракталов с точностью BigDecimal ---
        // ПРИМЕЧАНИЕ: Math.Abs не работает с BigDecimal, но нам и не нужно,
        // так как знак хранится в Mantissa. Мы можем просто взять BigInteger.Abs().

        private int CalculateMandelbrot(ComplexBigDecimal c)
        {
            int iter = 0;
            var z = ComplexBigDecimal.Zero;
            while (iter < MaxIterations && z.MagnitudeSquared <= _thresholdSquaredBig)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        private int CalculateJulia(ComplexBigDecimal z, ComplexBigDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= _thresholdSquaredBig)
            {
                z = z * z + c;
                iter++;
            }
            return iter;
        }

        private int CalculateMandelbrotBurningShip(ComplexBigDecimal c)
        {
            int iter = 0;
            var z = ComplexBigDecimal.Zero;
            while (iter < MaxIterations && z.MagnitudeSquared <= _thresholdSquaredBig)
            {
                // ПРАВИЛЬНАЯ ЛОГИКА для Burning Ship
                var z_abs = new ComplexBigDecimal(
                    new BigDecimal(System.Numerics.BigInteger.Abs(z.Real.Mantissa), z.Real.Exponent),
                    new BigDecimal(System.Numerics.BigInteger.Negate(System.Numerics.BigInteger.Abs(z.Imaginary.Mantissa)), z.Imaginary.Exponent) // Мнимая часть должна быть < 0
                );
                z = z_abs * z_abs + c;
                iter++;
            }
            return iter;
        }

        private int CalculateJuliaBurningShip(ComplexBigDecimal z, ComplexBigDecimal c)
        {
            int iter = 0;
            while (iter < MaxIterations && z.MagnitudeSquared <= _thresholdSquaredBig)
            {
                // ПРАВИЛЬНАЯ ЛОГИКА для Burning Ship
                var z_abs = new ComplexBigDecimal(
                    new BigDecimal(System.Numerics.BigInteger.Abs(z.Real.Mantissa), z.Real.Exponent),
                    new BigDecimal(System.Numerics.BigInteger.Negate(System.Numerics.BigInteger.Abs(z.Imaginary.Mantissa)), z.Imaginary.Exponent) // Мнимая часть должна быть < 0
                );
                z = z_abs * z_abs + c;
                iter++;
            }
            return iter;
        }
    }
}