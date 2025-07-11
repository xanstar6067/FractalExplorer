using System.Numerics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FractalExplorer.Utilities
{
    /// <summary>
    /// Представляет десятичное число с плавающей запятой произвольной точности.
    /// Число хранится в виде Mantissa * 10^Exponent.
    /// </summary>
    public struct BigDecimal : IComparable<BigDecimal>, IEquatable<BigDecimal>
    {
        /// <summary>
        /// Целочисленная часть числа без десятичного разделителя.
        /// </summary>
        public BigInteger Mantissa { get; private set; }

        /// <summary>
        /// Порядок (степень 10) для мантиссы.
        /// </summary>
        public int Exponent { get; private set; }

        /// <summary>
        /// Точность, используемая при операциях деления.
        /// </summary>
        public static int Precision { get; set; } = 100;

        #region Constructors

        public BigDecimal(BigInteger mantissa, int exponent)
        {
            Mantissa = mantissa;
            Exponent = exponent;
            Normalize(this); // Приватный конструктор не нужен, нормализуем здесь
        }

        public BigDecimal(decimal value)
        {
            int[] bits = decimal.GetBits(value);
            bool isNegative = (bits[3] & 0x80000000) != 0;
            byte scale = (byte)((bits[3] >> 16) & 0x7F);

            Mantissa = new BigInteger(new byte[] {
                (byte)(bits[0]), (byte)(bits[0] >> 8), (byte)(bits[0] >> 16), (byte)(bits[0] >> 24),
                (byte)(bits[1]), (byte)(bits[1] >> 8), (byte)(bits[1] >> 16), (byte)(bits[1] >> 24),
                (byte)(bits[2]), (byte)(bits[2] >> 8), (byte)(bits[2] >> 16), (byte)(bits[2] >> 24),
                0 // Пустой байт для положительного знака BigInteger
            });

            if (isNegative)
            {
                Mantissa = BigInteger.Negate(Mantissa);
            }

            Exponent = -scale;
            this = Normalize(this);
        }

        public BigDecimal(double value)
        {
            // Самый надежный способ - через строковое представление
            var str = value.ToString("R", CultureInfo.InvariantCulture);
            this = Parse(str);
        }

        public BigDecimal(long value) : this(new BigInteger(value), 0) { }
        public BigDecimal(int value) : this(new BigInteger(value), 0) { }

        #endregion

        #region Static Properties
        public static readonly BigDecimal Zero = new BigDecimal(0);
        public static readonly BigDecimal One = new BigDecimal(1);
        public static readonly BigDecimal MinusOne = new BigDecimal(-1);
        #endregion

        #region Helpers
        /// <summary>
        /// Убирает лишние нули из мантиссы для канонического представления.
        /// </summary>
        private static BigDecimal Normalize(BigDecimal value)
        {
            if (value.Mantissa == 0)
            {
                value.Exponent = 0;
                return value;
            }

            while (value.Mantissa % 10 == 0)
            {
                value.Mantissa /= 10;
                value.Exponent++;
            }
            return value;
        }

        /// <summary>
        /// Приводит два числа к общему порядку для сложения/вычитания.
        /// </summary>
        private static void MatchExponents(ref BigDecimal a, ref BigDecimal b)
        {
            if (a.Exponent > b.Exponent)
            {
                a.Mantissa *= BigInteger.Pow(10, a.Exponent - b.Exponent);
                a.Exponent = b.Exponent;
            }
            else if (b.Exponent > a.Exponent)
            {
                b.Mantissa *= BigInteger.Pow(10, b.Exponent - a.Exponent);
                b.Exponent = a.Exponent;
            }
        }
        #endregion

        #region Operators

        public static BigDecimal operator +(BigDecimal a, BigDecimal b)
        {
            MatchExponents(ref a, ref b);
            return Normalize(new BigDecimal(a.Mantissa + b.Mantissa, a.Exponent));
        }

        public static BigDecimal operator -(BigDecimal a, BigDecimal b)
        {
            MatchExponents(ref a, ref b);
            return Normalize(new BigDecimal(a.Mantissa - b.Mantissa, a.Exponent));
        }

        public static BigDecimal operator *(BigDecimal a, BigDecimal b)
        {
            return Normalize(new BigDecimal(a.Mantissa * b.Mantissa, a.Exponent + b.Exponent));
        }

        public static BigDecimal operator /(BigDecimal a, BigDecimal b)
        {
            if (b.Mantissa == 0) throw new DivideByZeroException();

            // Увеличиваем мантиссу делимого для получения нужной точности
            BigInteger dividend = a.Mantissa * BigInteger.Pow(10, Precision);
            BigInteger quotient = dividend / b.Mantissa;

            return Normalize(new BigDecimal(quotient, a.Exponent - b.Exponent - Precision));
        }

        public static bool operator ==(BigDecimal a, BigDecimal b) => a.CompareTo(b) == 0;
        public static bool operator !=(BigDecimal a, BigDecimal b) => a.CompareTo(b) != 0;
        public static bool operator <(BigDecimal a, BigDecimal b) => a.CompareTo(b) < 0;
        public static bool operator >(BigDecimal a, BigDecimal b) => a.CompareTo(b) > 0;
        public static bool operator <=(BigDecimal a, BigDecimal b) => a.CompareTo(b) <= 0;
        public static bool operator >=(BigDecimal a, BigDecimal b) => a.CompareTo(b) >= 0;

        #endregion

        #region Conversions & Parsing

        public static explicit operator decimal(BigDecimal value)
        {
            if (value.TryToDecimal(out decimal result))
            {
                return result;
            }
            throw new OverflowException("Значение BigDecimal не может быть представлено как decimal.");
        }

        public bool TryToDecimal(out decimal result)
        {
            try
            {
                if (Exponent >= 0)
                {
                    result = (decimal)Mantissa * (decimal)BigInteger.Pow(10, Exponent);
                }
                else
                {
                    result = (decimal)Mantissa / (decimal)BigInteger.Pow(10, -Exponent);
                }
                return true;
            }
            catch (OverflowException)
            {
                result = 0;
                return false;
            }
        }

        public static BigDecimal Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Zero;
            value = value.Trim();

            // Regex для парсинга научных нотаций
            var match = Regex.Match(value, @"^([+-]?)(\d+)?\.?(\d*)([eE]([+-]?\d+))?$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                throw new FormatException($"Неверный формат строки для BigDecimal: '{value}'");
            }

            var sign = match.Groups[1].Value == "-" ? -1 : 1;
            var intPart = match.Groups[2].Value;
            var fracPart = match.Groups[3].Value;
            var expPart = match.Groups[5].Value;

            if (string.IsNullOrEmpty(intPart) && string.IsNullOrEmpty(fracPart))
            {
                return Zero;
            }

            var mantissaStr = intPart + fracPart;
            var mantissa = BigInteger.Parse(mantissaStr) * sign;

            var exponent = string.IsNullOrEmpty(expPart) ? 0 : int.Parse(expPart);
            exponent -= fracPart.Length;

            return Normalize(new BigDecimal(mantissa, exponent));
        }

        public override string ToString()
        {
            return $"{Mantissa}e{Exponent}";
        }

        public string ToScientificString()
        {
            if (Mantissa == 0) return "0";

            string mStr = Mantissa.ToString();
            int newExponent = Exponent + mStr.Length - 1;

            if (mStr.Length > 1)
            {
                return $"{mStr[0]}.{mStr.Substring(1)}e{newExponent}";
            }
            return $"{mStr}e{newExponent}";
        }

        #endregion

        #region Interface Implementations
        public int CompareTo(BigDecimal other)
        {
            // Сначала сравниваем знаки
            if (this.Mantissa.Sign != other.Mantissa.Sign)
                return this.Mantissa.Sign.CompareTo(other.Mantissa.Sign);

            if (this.Mantissa == 0) return 0; // оба нуля

            // Приводим к общему порядку
            BigDecimal a = this;
            BigDecimal b = other;
            MatchExponents(ref a, ref b);
            return a.Mantissa.CompareTo(b.Mantissa);
        }

        public bool Equals(BigDecimal other) => this == other;

        public override bool Equals(object obj)
        {
            return obj is BigDecimal other && Equals(other);
        }

        public override int GetHashCode()
        {
            // Нормализованное значение всегда будет иметь один и тот же хэш-код
            var normalized = Normalize(this);
            return (normalized.Mantissa, normalized.Exponent).GetHashCode();
        }
        #endregion
    }
}
