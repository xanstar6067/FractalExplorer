using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities
{
    /// <summary>
    /// Представляет комплексное число, используя тип BigDecimal для произвольной точности.
    /// Эта структура является неизменяемой (immutable).
    /// </summary>
    public readonly struct ComplexBigDecimal
    {
        /// <summary>
        /// Действительная часть комплексного числа.
        /// </summary>
        public readonly BigDecimal Real;

        /// <summary>
        /// Мнимая часть комплексного числа.
        /// </summary>
        public readonly BigDecimal Imaginary;

        /// <summary>
        /// Инициализирует новый экземпляр структуры.
        /// </summary>
        public ComplexBigDecimal(BigDecimal real, BigDecimal imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        /// <summary>
        /// Получает квадрат модуля (длины) комплексного числа.
        /// Вычисляется как Real^2 + Imaginary^2.
        /// </summary>
        public BigDecimal MagnitudeSquared => Real * Real + Imaginary * Imaginary;

        /// <summary>
        /// Представляет комплексное число 0 + 0i.
        /// </summary>
        public static readonly ComplexBigDecimal Zero = new ComplexBigDecimal(BigDecimal.Zero, BigDecimal.Zero);

        /// <summary>
        /// Сложение двух комплексных чисел.
        /// </summary>
        public static ComplexBigDecimal operator +(ComplexBigDecimal a, ComplexBigDecimal b)
        {
            return new ComplexBigDecimal(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }

        /// <summary>
        /// Вычитание двух комплексных чисел.
        /// </summary>
        public static ComplexBigDecimal operator -(ComplexBigDecimal a, ComplexBigDecimal b)
        {
            return new ComplexBigDecimal(a.Real - b.Real, a.Imaginary - b.Imaginary);
        }

        /// <summary>
        /// Умножение двух комплексных чисел.
        /// </summary>
        public static ComplexBigDecimal operator *(ComplexBigDecimal a, ComplexBigDecimal b)
        {
            BigDecimal realPart = a.Real * b.Real - a.Imaginary * b.Imaginary;
            BigDecimal imaginaryPart = a.Real * b.Imaginary + a.Imaginary * b.Real;
            return new ComplexBigDecimal(realPart, imaginaryPart);
        }

        public override string ToString()
        {
            return $"{Real} + {Imaginary}i";
        }
    }
}
