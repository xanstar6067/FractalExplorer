using ExtendedNumerics;
using System;

namespace FractalExplorer.Utilities
{
    /// <summary>
    /// Представляет комплексное число, используя тип BigDecimal для произвольной точности.
    /// Эта структура является неизменяемой (immutable).
    /// </summary>
    public readonly struct ComplexBig
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
        /// Получает квадрат модуля (длины) комплексного числа.
        /// </summary>
        public BigDecimal MagnitudeSquared => Real * Real + Imaginary * Imaginary;

        /// <summary>
        /// Инициализирует новый экземпляр структуры.
        /// </summary>
        public ComplexBig(BigDecimal real, BigDecimal imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        /// <summary>
        /// Представляет комплексное число 0 + 0i.
        /// </summary>
        public static readonly ComplexBig Zero = new ComplexBig(0, 0);

        #region Operators

        public static ComplexBig operator +(ComplexBig a, ComplexBig b)
            => new ComplexBig(a.Real + b.Real, a.Imaginary + b.Imaginary);

        public static ComplexBig operator -(ComplexBig a, ComplexBig b)
            => new ComplexBig(a.Real - b.Real, a.Imaginary - b.Imaginary);

        // Унарный минус, необходим для корректной формулы "Пылающего Корабля"
        public static ComplexBig operator -(ComplexBig a)
            => new ComplexBig(-a.Real, -a.Imaginary);

        public static ComplexBig operator *(ComplexBig a, ComplexBig b)
        {
            var realPart = a.Real * b.Real - a.Imaginary * b.Imaginary;
            var imaginaryPart = a.Real * b.Imaginary + a.Imaginary * b.Real;
            return new ComplexBig(realPart, imaginaryPart);
        }

        public static bool operator ==(ComplexBig a, ComplexBig b)
            => a.Real == b.Real && a.Imaginary == b.Imaginary;

        public static bool operator !=(ComplexBig a, ComplexBig b)
            => !(a == b);

        #endregion

        #region Overrides

        public override bool Equals(object obj) => obj is ComplexBig other && this == other;

        public override int GetHashCode() => Real.GetHashCode() ^ Imaginary.GetHashCode();

        public override string ToString() => $"{Real} + {Imaginary}i";

        #endregion
    }
}