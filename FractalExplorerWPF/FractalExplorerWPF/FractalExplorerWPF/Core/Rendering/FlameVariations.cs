using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Core.Rendering;

internal static class FlameVariations
{
    private const double Epsilon = 1e-12;

    public static (double X, double Y) Apply(
        FlameVariation variation,
        double x,
        double y,
        bool useSecondJuliaBranch = false)
    {
        if (variation == FlameVariation.Linear)
            return (x, y);
        if (variation == FlameVariation.Sinusoidal)
            return (Math.Sin(x), Math.Sin(y));

        double radiusSquared = x * x + y * y;

        return variation switch
        {
            FlameVariation.Spherical => Spherical(x, y, radiusSquared),
            FlameVariation.Swirl => Swirl(x, y, radiusSquared),
            FlameVariation.Horseshoe => Horseshoe(x, y, radiusSquared),
            FlameVariation.Polar => Polar(x, y, radiusSquared),
            FlameVariation.Heart => Heart(x, y, radiusSquared),
            FlameVariation.Disc => Disc(x, y, radiusSquared),
            FlameVariation.Spiral => Spiral(x, y, radiusSquared),
            FlameVariation.Julia => Julia(x, y, radiusSquared, useSecondJuliaBranch),
            FlameVariation.Bubble => Bubble(x, y, radiusSquared),
            FlameVariation.Fisheye => Fisheye(x, y, radiusSquared),
            _ => (x, y)
        };
    }

    private static (double X, double Y) Spherical(double x, double y, double radiusSquared)
    {
        double scale = 1 / (radiusSquared + Epsilon);
        return (x * scale, y * scale);
    }

    private static (double X, double Y) Swirl(double x, double y, double radiusSquared)
    {
        double sine = Math.Sin(radiusSquared);
        double cosine = Math.Cos(radiusSquared);
        return (x * sine - y * cosine, x * cosine + y * sine);
    }

    private static (double X, double Y) Horseshoe(double x, double y, double radiusSquared)
    {
        double scale = 1 / (Math.Sqrt(radiusSquared) + Epsilon);
        return ((x - y) * (x + y) * scale, 2 * x * y * scale);
    }

    private static (double X, double Y) Polar(double x, double y, double radiusSquared)
    {
        double angle = Math.Atan2(x, y);
        return (angle / Math.PI, Math.Sqrt(radiusSquared) - 1);
    }

    private static (double X, double Y) Heart(double x, double y, double radiusSquared)
    {
        double radius = Math.Sqrt(radiusSquared);
        double angle = Math.Atan2(x, y) * radius;
        return (radius * Math.Sin(angle), -radius * Math.Cos(angle));
    }

    private static (double X, double Y) Disc(double x, double y, double radiusSquared)
    {
        double angleScale = Math.Atan2(x, y) / Math.PI;
        double radiusAngle = Math.PI * Math.Sqrt(radiusSquared);
        return (angleScale * Math.Sin(radiusAngle), angleScale * Math.Cos(radiusAngle));
    }

    private static (double X, double Y) Spiral(double x, double y, double radiusSquared)
    {
        double radius = Math.Sqrt(radiusSquared);
        double angle = Math.Atan2(x, y);
        double scale = 1 / (radius + Epsilon);
        return ((Math.Cos(angle) + Math.Sin(radius)) * scale,
            (Math.Sin(angle) - Math.Cos(radius)) * scale);
    }

    private static (double X, double Y) Julia(
        double x,
        double y,
        double radiusSquared,
        bool useSecondBranch)
    {
        double radius = Math.Pow(radiusSquared, .25);
        double angle = Math.Atan2(x, y) * .5 + (useSecondBranch ? Math.PI : 0);
        return (radius * Math.Cos(angle), radius * Math.Sin(angle));
    }

    private static (double X, double Y) Bubble(double x, double y, double radiusSquared)
    {
        double scale = 4 / (radiusSquared + 4);
        return (x * scale, y * scale);
    }

    private static (double X, double Y) Fisheye(double x, double y, double radiusSquared)
    {
        double scale = 2 / (Math.Sqrt(radiusSquared) + 1);
        return (y * scale, x * scale);
    }
}
