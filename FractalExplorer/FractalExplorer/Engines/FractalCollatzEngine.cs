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
            try
            {
                // Для вычислений тригонометрических функций используем double,
                // так как в стандартной библиотеке нет аналогов для decimal.
                double x = (double)z.Real;
                double y = (double)z.Imaginary;

                // Проверяем на переполнение при конвертации
                if (double.IsInfinity(x) || double.IsNaN(x) || double.IsInfinity(y) || double.IsNaN(y))
                {
                    throw new OverflowException("Complex number components too large for trigonometric calculations");
                }

                // Ограничиваем значения для предотвращения переполнения в гиперболических функциях
                if (Math.Abs(y) > 700) // exp(700) близко к пределу double
                {
                    throw new OverflowException("Imaginary component too large for hyperbolic functions");
                }

                decimal cos_x = (decimal)Math.Cos(x);
                decimal sin_x = (decimal)Math.Sin(x);
                decimal cosh_y = (decimal)Math.Cosh(y);
                decimal sinh_y = (decimal)Math.Sinh(y);

                // Проверяем результаты гиперболических функций
                if (double.IsInfinity((double)cosh_y) || double.IsInfinity((double)sinh_y))
                {
                    throw new OverflowException("Hyperbolic function result too large");
                }

                decimal realPart = cos_x * cosh_y;
                decimal imaginaryPart = -sin_x * sinh_y;

                return new ComplexDecimal(realPart, imaginaryPart);
            }
            catch (Exception ex) when (ex is OverflowException || ex is ArgumentOutOfRangeException)
            {
                throw new OverflowException("Complex cosine calculation overflow", ex);
            }
        }

        /// <summary>
        /// Проверяет, не приведут ли арифметические операции к переполнению decimal
        /// </summary>
        private bool IsDecimalSafe(ComplexDecimal z)
        {
            const decimal MAX_SAFE_VALUE = decimal.MaxValue / 100; // Оставляем запас для вычислений
            return Math.Abs(z.Real) < MAX_SAFE_VALUE && Math.Abs(z.Imaginary) < MAX_SAFE_VALUE;
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
                    // Проверяем безопасность текущих значений
                    if (!IsDecimalSafe(z))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    // z_next = 0.25 * (2 + 7*z - (2 + 5*z) * cos(pi*z))

                    // Вычисляем pi*z с проверкой переполнения
                    ComplexDecimal pi_z;
                    try
                    {
                        pi_z = z * (decimal)Math.PI;
                    }
                    catch (OverflowException)
                    {
                        iter = MaxIterations;
                        break;
                    }

                    // Проверяем безопасность перед вычислением косинуса
                    if (!IsDecimalSafe(pi_z))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    ComplexDecimal cos_pi_z = ComplexCos(pi_z);

                    // Вычисляем компоненты формулы по частям с проверками
                    ComplexDecimal term1 = new ComplexDecimal(2, 0);
                    ComplexDecimal term2;
                    ComplexDecimal term3;
                    ComplexDecimal term4;

                    try
                    {
                        term2 = z * 7;
                        term3 = new ComplexDecimal(2, 0) + z * 5;
                        term4 = term3 * cos_pi_z;
                    }
                    catch (OverflowException)
                    {
                        iter = MaxIterations;
                        break;
                    }

                    // Проверяем промежуточные результаты
                    if (!IsDecimalSafe(term2) || !IsDecimalSafe(term3) || !IsDecimalSafe(term4))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    ComplexDecimal numerator;
                    try
                    {
                        numerator = term1 + term2 - term4;
                    }
                    catch (OverflowException)
                    {
                        iter = MaxIterations;
                        break;
                    }

                    if (!IsDecimalSafe(numerator))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    z = numerator / 4;
                    iter++;
                }
                catch (OverflowException)
                {
                    // Если происходит переполнение, считаем, что точка ушла в бесконечность.
                    iter = MaxIterations;
                    break;
                }
                catch (Exception ex) when (ex is DivideByZeroException || ex is ArgumentOutOfRangeException)
                {
                    // Другие математические ошибки также считаем как достижение бесконечности
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
                try
                {
                    // Проверяем на переполнение и некорректные значения
                    if (double.IsInfinity(z_numerics.Real) || double.IsNaN(z_numerics.Real) ||
                        double.IsInfinity(z_numerics.Imaginary) || double.IsNaN(z_numerics.Imaginary))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    // Ограничиваем значения для предотвращения переполнения
                    const double MAX_SAFE_VALUE = 1e100;
                    if (Math.Abs(z_numerics.Real) > MAX_SAFE_VALUE || Math.Abs(z_numerics.Imaginary) > MAX_SAFE_VALUE)
                    {
                        iter = MaxIterations;
                        break;
                    }

                    // z_next = 0.25 * (2 + 7*z - (2 + 5*z) * cos(pi*z))
                    System.Numerics.Complex pi_z = z_numerics * Math.PI;

                    // Проверяем результат умножения на pi
                    if (double.IsInfinity(pi_z.Real) || double.IsNaN(pi_z.Real) ||
                        double.IsInfinity(pi_z.Imaginary) || double.IsNaN(pi_z.Imaginary))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    System.Numerics.Complex cos_pi_z = System.Numerics.Complex.Cos(pi_z);

                    // Проверяем результат косинуса
                    if (double.IsInfinity(cos_pi_z.Real) || double.IsNaN(cos_pi_z.Real) ||
                        double.IsInfinity(cos_pi_z.Imaginary) || double.IsNaN(cos_pi_z.Imaginary))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    System.Numerics.Complex newZ = 0.25 * (2 + 7 * z_numerics - (2 + 5 * z_numerics) * cos_pi_z);

                    // Проверяем финальный результат
                    if (double.IsInfinity(newZ.Real) || double.IsNaN(newZ.Real) ||
                        double.IsInfinity(newZ.Imaginary) || double.IsNaN(newZ.Imaginary))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    z_numerics = newZ;
                    iter++;
                }
                catch (Exception ex) when (ex is OverflowException || ex is ArgumentOutOfRangeException)
                {
                    iter = MaxIterations;
                    break;
                }
            }

            // Обновляем исходную переменную z (переданную по ссылке) для корректной работы сглаживания
            z = new ComplexDouble(z_numerics.Real, z_numerics.Imaginary);
            return iter;
        }

        #endregion
    }
}