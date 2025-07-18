public static class DecimalMath
{
    // Задаем высокоточные константы PI
    private static readonly decimal PI = 3.1415926535897932384626433832m;
    private static readonly decimal PI_X_2 = 6.2831853071795864769252867664m;
    private static readonly decimal PI_OVER_2 = 1.5707963267948966192313216916m;

    /// <summary>
    /// Вычисляет косинус для decimal с использованием ряда Тейлора.
    /// </summary>
    public static decimal Cos(decimal x)
    {
        // Приводим аргумент к диапазону [-PI, PI] для лучшей сходимости и точности
        while (x > PI) x -= PI_X_2;
        while (x < -PI) x += PI_X_2;

        // cos(x) = 1 - x^2/2! + x^4/4! - x^6/6! + ...
        decimal result = 1.0m;
        decimal term = 1.0m;
        // 34 итерации более чем достаточно для полной точности decimal
        for (int i = 1; i <= 34; i += 2)
        {
            term *= -1 * x * x / (i * (i + 1));
            result += term;
        }
        return result;
    }

    /// <summary>
    /// Вычисляет синус для decimal с использованием ряда Тейлора.
    /// </summary>
    public static decimal Sin(decimal x)
    {
        // Для Sin проще всего использовать тождество sin(x) = cos(x - PI/2)
        return Cos(x - PI_OVER_2);
    }
}