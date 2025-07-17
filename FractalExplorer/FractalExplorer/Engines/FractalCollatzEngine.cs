using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using System;
using System.Numerics;

namespace FractalExplorer.Engines
{
    /// <summary>
    /// Реализует движок для рендеринга фрактала Коллатца.
    /// Итерационная формула: z_next = 0.25 * (2 + 7*z - (2 + 5*z) * cos(pi*z))
    /// </summary>
    public class FractalCollatzEngine : FractalMandelbrotFamilyEngine
    {
        #region Private Helpers

        /// <summary>
        /// Вычисляет косинус комплексного числа с высокой точностью (decimal).
        /// Формула: cos(x + iy) = cos(x)cosh(y) - i*sin(x)sinh(y)
        /// </summary>
        /// <param name="z">Комплексное число.</param>
        /// <returns>Косинус от z.</returns>
        private ComplexDecimal ComplexCos(ComplexDecimal z)
        {
            // Теперь все вычисления производятся с точностью decimal!
            decimal x = z.Real;
            decimal y = z.Imaginary;

            // Используем нашу новую реализацию
            decimal cos_x = DecimalMath.Cos(x);
            decimal sin_x = DecimalMath.Sin(x);
            decimal cosh_y = DecimalMath.Cosh(y);
            decimal sinh_y = DecimalMath.Sinh(y);

            decimal realPart = cos_x * cosh_y;
            decimal imaginaryPart = -sin_x * sinh_y;

            return new ComplexDecimal(realPart, imaginaryPart);
        }

        #endregion

        #region Overridden Methods

        /// <inheritdoc />
        public override void CopySpecificParametersFrom(FractalMandelbrotFamilyEngine source)
        {
            // У этого движка нет специфичных параметров для копирования.
        }

        /// <inheritdoc />
        /// <remarks>
        /// Для фрактала Коллатца итерация зависит только от z.
        /// Начальное z - это сама точка на комплексной плоскости.
        /// Константа 'c' не используется.
        /// </remarks>
        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = new ComplexDecimal(re, im);
            constantC = ComplexDecimal.Zero; // 'c' не используется, но должно быть определено.
        }

        /// <inheritdoc />
        public override int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;
            // Параметр 'c' игнорируется, так как не входит в формулу Коллатца.

            while (iter < MaxIterations && z.MagnitudeSquared <= ThresholdSquared)
            {
                try
                {
                    // z_next = 0.25 * (2 + 7*z - (2 + 5*z) * cos(pi*z))
                    ComplexDecimal cos_pi_z = ComplexCos(z * (decimal)Math.PI);
                    z = (new ComplexDecimal(2, 0) + z * 7 - (new ComplexDecimal(2, 0) + z * 5) * cos_pi_z) / 4;
                    iter++;
                }
                catch (OverflowException)
                {
                    // Если происходит переполнение, считаем, что точка ушла в бесконечность.
                    iter = MaxIterations;
                    break;
                }
            }
            return iter;
        }

        /// <inheritdoc />
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = new ComplexDouble(re, im);
            constantC = ComplexDouble.Zero; // 'c' не используется.
        }

        /// <inheritdoc />
        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            int iter = 0;
            double thresholdSq = (double)ThresholdSquared;
            // Параметр 'c' игнорируется.

            // Преобразуем в System.Numerics.Complex для использования встроенной быстрой функции Cos
            System.Numerics.Complex z_numerics = new System.Numerics.Complex(z.Real, z.Imaginary);

            while (iter < MaxIterations && z_numerics.Magnitude * z_numerics.Magnitude <= thresholdSq)
            {
                // z_next = 0.25 * (2 + 7*z - (2 + 5*z) * cos(pi*z))
                System.Numerics.Complex cos_pi_z = System.Numerics.Complex.Cos(z_numerics * Math.PI);
                z_numerics = 0.25 * (2 + 7 * z_numerics - (2 + 5 * z_numerics) * cos_pi_z);
                iter++;
            }

            // Обновляем исходную переменную z (переданную по ссылке) для корректной работы сглаживания
            z = new ComplexDouble(z_numerics.Real, z_numerics.Imaginary);
            return iter;
        }

        #endregion
    }
}