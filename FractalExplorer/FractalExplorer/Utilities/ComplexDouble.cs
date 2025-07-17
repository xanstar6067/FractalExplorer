using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities
{


    /// <summary>
    /// Представляет комплексное число, используя тип double для высокой производительности.
    /// Эта структура является неизменяемой (immutable).
    /// </summary>
    public readonly struct ComplexDouble
    {
        public readonly double Real;
        public readonly double Imaginary;

        /// <summary>
        /// Получает аргумент (угол) комплексного числа в радианах.
        /// Возвращает значение от -π до π.
        /// </summary>
        public double Argument => Math.Atan2(Imaginary, Real);

        public double MagnitudeSquared => Real * Real + Imaginary * Imaginary;
        public double Magnitude => Math.Sqrt(MagnitudeSquared);

        public ComplexDouble(double real, double imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        public static readonly ComplexDouble Zero = new ComplexDouble(0.0, 0.0);

        public static ComplexDouble operator +(ComplexDouble a, ComplexDouble b)
        {
            return new ComplexDouble(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }

        public static ComplexDouble operator *(ComplexDouble a, ComplexDouble b)
        {
            double realPart = a.Real * b.Real - a.Imaginary * b.Imaginary;
            double imaginaryPart = a.Real * b.Imaginary + a.Imaginary * b.Real;
            return new ComplexDouble(realPart, imaginaryPart);
        }

        public override string ToString()
        {
            return $"{Real} + {Imaginary}i";
        }
        public static ComplexDouble operator *(ComplexDouble a, double scalar)
        {
            return new ComplexDouble(a.Real * scalar, a.Imaginary * scalar);
        }

        // Для удобства можно добавить и обратный оператор, чтобы можно было писать `2.0 * complexNumber`
        public static ComplexDouble operator *(double scalar, ComplexDouble a)
        {
            return new ComplexDouble(a.Real * scalar, a.Imaginary * scalar);
        }

    }
}
