using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using System;
using System.Numerics;

namespace FractalExplorer.Engines
{
    public class FractalCollatzEngine : FractalMandelbrotFamilyEngine
    {
        private decimal _bailoutValue;

        public override decimal ThresholdSquared
        {
            get => base.ThresholdSquared;
            set
            {
                base.ThresholdSquared = value;
                _bailoutValue = (decimal)Math.Sqrt((double)value);
            }
        }

        #region Private Helpers

        // *** ПОЛНОСТЬЮ ПЕРЕРАБОТАННЫЙ МЕТОД ***
        private ComplexDecimal ComplexCos(ComplexDecimal z)
        {
            // --- ОСНОВНАЯ ЧАСТЬ ВЫЧИСЛЕНИЙ ТЕПЕРЬ ИДЕТ В DECIMAL ---
            // Мы больше не теряем точность!
            decimal cos_x = DecimalMath.Cos(z.Real);
            decimal sin_x = DecimalMath.Sin(z.Real);

            // --- ДЛЯ ГИПЕРБОЛИЧЕСКИХ ФУНКЦИЙ ОСТАВЛЯЕМ DOUBLE ---
            // Это компромисс, так как реализация Exp() для decimal очень сложна,
            // но потеря точности в мнимой части гораздо менее критична для стабильности.
            double y = (double)z.Imaginary;
            if (double.IsInfinity(y) || double.IsNaN(y) || Math.Abs(y) > 700)
            {
                throw new OverflowException("Мнимая часть слишком велика для гиперболических функций.");
            }

            double cosh_y_double = Math.Cosh(y);
            double sinh_y_double = Math.Sinh(y);

            if (double.IsInfinity(cosh_y_double) || double.IsInfinity(sinh_y_double))
            {
                throw new OverflowException("Результат гиперболической функции привел к переполнению.");
            }

            decimal cosh_y = (decimal)cosh_y_double;
            decimal sinh_y = (decimal)sinh_y_double;

            // Финальные вычисления, которые могут переполниться,
            // но будут пойманы в главном цикле.
            decimal realPart = cos_x * cosh_y;
            decimal imaginaryPart = -sin_x * sinh_y;

            return new ComplexDecimal(realPart, imaginaryPart);
        }

        #endregion

        #region Overridden Methods

        public override void CopySpecificParametersFrom(FractalMandelbrotFamilyEngine source) { }

        protected override void GetCalculationParameters(decimal re, decimal im, out ComplexDecimal initialZ, out ComplexDecimal constantC)
        {
            initialZ = new ComplexDecimal(re, im);
            constantC = ComplexDecimal.Zero;
        }

        // Основной цикл остается таким же надежным, каким мы его сделали
        public override int CalculateIterations(ref ComplexDecimal z, ComplexDecimal c)
        {
            int iter = 0;

            while (iter < MaxIterations && (Math.Abs(z.Real) < _bailoutValue && Math.Abs(z.Imaginary) < _bailoutValue))
            {
                try
                {
                    // Вызов PI * z остается прежним, так как PI берется из double, но это допустимо
                    // для масштабирования перед передачей в высокоточную функцию.
                    ComplexDecimal pi_z = z * (decimal)Math.PI;

                    // Вызываем наш новый, полностью защищенный ComplexCos
                    ComplexDecimal cos_pi_z = ComplexCos(pi_z);

                    ComplexDecimal term1 = new ComplexDecimal(2, 0);
                    ComplexDecimal term2 = z * 7;
                    ComplexDecimal term3 = term1 + z * 5;
                    ComplexDecimal term4 = term3 * cos_pi_z;
                    ComplexDecimal numerator = term1 + term2 - term4;
                    z = numerator / 4;
                    iter++;
                }
                catch (OverflowException)
                {
                    iter = MaxIterations;
                    break;
                }
            }
            return iter;
        }

        // Методы для double остаются без изменений
        protected override void GetCalculationParametersDouble(double re, double im, out ComplexDouble initialZ, out ComplexDouble constantC)
        {
            initialZ = new ComplexDouble(re, im);
            constantC = ComplexDouble.Zero;
        }

        public override int CalculateIterationsDouble(ref ComplexDouble z, ComplexDouble c)
        {
            // Этот метод не затрагивается, так как он изначально работает с double
            // и не претендует на высокую точность.
            int iter = 0;
            double thresholdSq = (double)ThresholdSquared;
            System.Numerics.Complex z_numerics = new System.Numerics.Complex(z.Real, z.Imaginary);

            while (iter < MaxIterations && z_numerics.Magnitude * z_numerics.Magnitude <= thresholdSq)
            {
                try
                {
                    if (double.IsInfinity(z_numerics.Real) || double.IsNaN(z_numerics.Real) ||
                        double.IsInfinity(z_numerics.Imaginary) || double.IsNaN(z_numerics.Imaginary))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    const double MAX_SAFE_VALUE = 1e6;
                    if (Math.Abs(z_numerics.Real) > MAX_SAFE_VALUE || Math.Abs(z_numerics.Imaginary) > MAX_SAFE_VALUE)
                    {
                        iter = MaxIterations;
                        break;
                    }

                    System.Numerics.Complex pi_z = z_numerics * Math.PI;
                    if (double.IsInfinity(pi_z.Real) || double.IsNaN(pi_z.Real) ||
                        double.IsInfinity(pi_z.Imaginary) || double.IsNaN(pi_z.Imaginary))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    System.Numerics.Complex cos_pi_z = System.Numerics.Complex.Cos(pi_z);
                    if (double.IsInfinity(cos_pi_z.Real) || double.IsNaN(cos_pi_z.Real) ||
                        double.IsInfinity(cos_pi_z.Imaginary) || double.IsNaN(cos_pi_z.Imaginary))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    System.Numerics.Complex newZ = 0.25 * (2 + 7 * z_numerics - (2 + 5 * z_numerics) * cos_pi_z);
                    if (double.IsInfinity(newZ.Real) || double.IsNaN(newZ.Real) ||
                        double.IsInfinity(newZ.Imaginary) || double.IsNaN(newZ.Imaginary))
                    {
                        iter = MaxIterations;
                        break;
                    }

                    z_numerics = newZ;
                    iter++;
                }
                catch (Exception)
                {
                    iter = MaxIterations;
                    break;
                }
            }

            z = new ComplexDouble(z_numerics.Real, z_numerics.Imaginary);
            return iter;
        }

        #endregion
    }
}