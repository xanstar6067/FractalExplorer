using System;

namespace FractalDraving
{
    /// <summary>
    /// Представляет комплексное число, используя тип decimal для высокой точности.
    /// Эта структура является неизменяемой (immutable).
    /// </summary>
    public readonly struct ComplexDecimal
    {
        public readonly decimal Real;
        public readonly decimal Imaginary;

        public ComplexDecimal(decimal real, decimal imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        public static readonly ComplexDecimal Zero = new ComplexDecimal(0m, 0m);

        public decimal MagnitudeSquared => Real * Real + Imaginary * Imaginary;

        public double Magnitude => Math.Sqrt((double)MagnitudeSquared);

        public static ComplexDecimal operator +(ComplexDecimal a, ComplexDecimal b)
        {
            return new ComplexDecimal(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }

        public static ComplexDecimal operator *(ComplexDecimal a, ComplexDecimal b)
        {
            decimal realPart = a.Real * b.Real - a.Imaginary * b.Imaginary;
            decimal imaginaryPart = a.Real * b.Imaginary + a.Imaginary * b.Real;
            return new ComplexDecimal(realPart, imaginaryPart);
        }

        public static ComplexDecimal operator -(ComplexDecimal a, ComplexDecimal b)
        {
            return new ComplexDecimal(a.Real - b.Real, a.Imaginary - b.Imaginary);
        }

        public static ComplexDecimal operator -(ComplexDecimal value)
        {
            return new ComplexDecimal(-value.Real, -value.Imaginary);
        }

        public static ComplexDecimal operator /(ComplexDecimal a, ComplexDecimal b)
        {
            decimal denominator = b.MagnitudeSquared;
            if (denominator == 0)
                throw new DivideByZeroException("Division by zero complex number.");

            decimal realPart = (a.Real * b.Real + a.Imaginary * b.Imaginary) / denominator;
            decimal imaginaryPart = (a.Imaginary * b.Real - a.Real * b.Imaginary) / denominator;
            return new ComplexDecimal(realPart, imaginaryPart);
        }

        public static bool operator ==(ComplexDecimal a, ComplexDecimal b)
        {
            return a.Real == b.Real && a.Imaginary == b.Imaginary;
        }

        public static bool operator !=(ComplexDecimal a, ComplexDecimal b)
        {
            return !(a == b);
        }

        public ComplexDecimal Conjugate()
        {
            return new ComplexDecimal(Real, -Imaginary);
        }

        public static ComplexDecimal FromPolarCoordinates(double magnitude, double angleRadians)
        {
            decimal real = (decimal)(magnitude * Math.Cos(angleRadians));
            decimal imaginary = (decimal)(magnitude * Math.Sin(angleRadians));
            return new ComplexDecimal(real, imaginary);
        }

        public override bool Equals(object obj)
        {
            if (obj is ComplexDecimal other)
                return this == other;
            return false;
        }

        public override int GetHashCode()
        {
            return Real.GetHashCode() ^ Imaginary.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Real} + {Imaginary}i";
        }

        // Дополнительно: арифметика с decimal (полезно в некоторых случаях)
        public static ComplexDecimal operator +(ComplexDecimal a, decimal b)
        {
            return new ComplexDecimal(a.Real + b, a.Imaginary);
        }

        public static ComplexDecimal operator -(ComplexDecimal a, decimal b)
        {
            return new ComplexDecimal(a.Real - b, a.Imaginary);
        }

        public static ComplexDecimal operator *(ComplexDecimal a, decimal b)
        {
            return new ComplexDecimal(a.Real * b, a.Imaginary * b);
        }

        public static ComplexDecimal operator /(ComplexDecimal a, decimal b)
        {
            if (b == 0)
                throw new DivideByZeroException("Division by zero scalar.");
            return new ComplexDecimal(a.Real / b, a.Imaginary / b);
        }
    }
}
