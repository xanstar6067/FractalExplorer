using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities
{
    /// <summary>
    /// Представляет комплексное число, используя тип double для стандартной точности.
    /// Эта структура является неизменяемой (immutable).
    /// </summary>
    public readonly struct ComplexDouble
    {
        /// <summary>
        /// Действительная часть комплексного числа.
        /// </summary>
        public readonly double Real;

        /// <summary>
        /// Мнимая часть комплексного числа.
        /// </summary>
        public readonly double Imaginary;

        /// <summary>
        /// Инициализирует новый экземпляр структуры.
        /// </summary>
        public ComplexDouble(double real, double imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        /// <summary>
        /// Получает квадрат модуля (длины) комплексного числа.
        /// Вычисляется как Real^2 + Imaginary^2.
        /// </summary>
        public double MagnitudeSquared => Real * Real + Imaginary * Imaginary;

        /// <summary>
        /// Представляет комплексное число 0 + 0i.
        /// </summary>
        public static readonly ComplexDouble Zero = new ComplexDouble(0.0, 0.0);

        /// <summary>
        /// Сложение двух комплексных чисел.
        /// </summary>
        public static ComplexDouble operator +(ComplexDouble a, ComplexDouble b)
        {
            return new ComplexDouble(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }

        /// <summary>
        /// Умножение двух комплексных чисел.
        /// </summary>
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
    }
}
