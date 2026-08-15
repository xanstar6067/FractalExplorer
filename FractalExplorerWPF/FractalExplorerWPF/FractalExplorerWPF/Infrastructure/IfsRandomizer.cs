using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public static class IfsRandomizer
{
    private const double MaximumContraction = .92;

    public static List<IfsAffineTransform> Create(
        IfsRandomizationSettings settings,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        IfsRandomizationSettings normalized = settings.Clone().Normalize();
        if (normalized.Families.Count == 0)
            throw new InvalidOperationException("Выберите хотя бы одно семейство аффинных преобразований.");

        random ??= Random.Shared;
        int count = random.Next(normalized.MinimumTransforms, normalized.MaximumTransforms + 1);
        List<IfsAffineTransform> result = normalized.PlacementMode == IfsPlacementMode.Bilateral
            ? CreateBilateral(count, normalized, random)
            : CreateFreeOrRadial(count, normalized, random);
        NormalizeProbabilities(result);
        return result;
    }

    public static void NormalizeProbabilities(IList<IfsAffineTransform> transforms)
    {
        if (transforms.Count == 0) return;
        double total = transforms.Sum(transform => Math.Max(0, transform.Probability));
        if (total <= 0)
        {
            double probability = 1d / transforms.Count;
            foreach (IfsAffineTransform transform in transforms) transform.Probability = probability;
            return;
        }

        foreach (IfsAffineTransform transform in transforms)
            transform.Probability = Math.Max(0, transform.Probability) / total;
    }

    private static List<IfsAffineTransform> CreateFreeOrRadial(
        int count,
        IfsRandomizationSettings settings,
        Random random)
    {
        var result = new List<IfsAffineTransform>(count);
        double baseAngle = Range(random, -Math.PI, Math.PI);
        for (int index = 0; index < count; index++)
        {
            IfsTransformFamily family = PickFamily(settings.Families, random);
            double anchorAngle = settings.PlacementMode == IfsPlacementMode.Radial
                ? baseAngle + index * Math.PI * 2 / count + Range(random, -.35, .35)
                : Range(random, -Math.PI, Math.PI);
            double radius = settings.PlacementMode == IfsPlacementMode.Radial
                ? Range(random, .25, .9)
                : Math.Sqrt(random.NextDouble()) * .9;
            double rotation = settings.PlacementMode == IfsPlacementMode.Radial
                ? anchorAngle + Range(random, -.9, .9)
                : Range(random, -Math.PI, Math.PI);
            IfsAffineTransform transform = CreateTransform(
                random, family, rotation,
                Math.Cos(anchorAngle) * radius,
                Math.Sin(anchorAngle) * radius);
            transform.Probability = RawProbability(transform, settings.ProbabilityMode, random);
            result.Add(transform);
        }
        return result;
    }

    private static List<IfsAffineTransform> CreateBilateral(
        int count,
        IfsRandomizationSettings settings,
        Random random)
    {
        var result = new List<IfsAffineTransform>(count);
        if ((count & 1) != 0)
        {
            IfsTransformFamily family = PickFamily(settings.Families, random);
            IfsAffineTransform center = CreateCenteredTransform(random, family);
            center.Probability = RawProbability(center, settings.ProbabilityMode, random);
            result.Add(center);
        }

        while (result.Count < count)
        {
            IfsTransformFamily family = PickFamily(settings.Families, random);
            IfsAffineTransform right = CreateTransform(
                random, family, Range(random, -Math.PI, Math.PI),
                Range(random, .18, .9), Range(random, -.85, .85));
            right.Probability = RawProbability(right, settings.ProbabilityMode, random);
            IfsAffineTransform left = MirrorAcrossYAxis(right);
            result.Add(right);
            result.Add(left);
        }
        return result;
    }

    private static IfsAffineTransform CreateTransform(
        Random random,
        IfsTransformFamily family,
        double rotation,
        double translationX,
        double translationY)
    {
        (double scaleX, double scaleY, double shear) = family switch
        {
            IfsTransformFamily.Similarity => SimilarityParameters(random),
            IfsTransformFamily.Anisotropic => (Range(random, .2, .72), Range(random, .16, .7), 0),
            IfsTransformFamily.Shear => (Range(random, .24, .62), Range(random, .22, .64), Range(random, -.35, .35)),
            IfsTransformFamily.Reflection => (-Range(random, .22, .7), Range(random, .22, .68), Range(random, -.08, .08)),
            IfsTransformFamily.Stem => (Range(random, .03, .16), Range(random, .32, .72), Range(random, -.06, .06)),
            _ => SimilarityParameters(random)
        };

        double cosine = Math.Cos(rotation);
        double sine = Math.Sin(rotation);
        double a = cosine * scaleX;
        double b = cosine * shear - sine * scaleY;
        double c = sine * scaleX;
        double d = sine * shear + cosine * scaleY;
        LimitContraction(ref a, ref b, ref c, ref d);
        return new IfsAffineTransform { A = a, B = b, C = c, D = d, E = translationX, F = translationY };
    }

    private static IfsAffineTransform CreateCenteredTransform(Random random, IfsTransformFamily family)
    {
        double scaleX;
        double scaleY;
        if (family == IfsTransformFamily.Stem)
        {
            scaleX = Range(random, .03, .16);
            scaleY = Range(random, .32, .72);
        }
        else
        {
            scaleX = Range(random, .2, .68);
            scaleY = family == IfsTransformFamily.Similarity ? scaleX : Range(random, .16, .68);
            if (family == IfsTransformFamily.Reflection) scaleX = -scaleX;
        }
        return new IfsAffineTransform { A = scaleX, D = scaleY, F = Range(random, -.75, .75) };
    }

    private static IfsAffineTransform MirrorAcrossYAxis(IfsAffineTransform transform) => new()
    {
        A = transform.A,
        B = -transform.B,
        C = -transform.C,
        D = transform.D,
        E = -transform.E,
        F = transform.F,
        Probability = transform.Probability
    };

    private static (double ScaleX, double ScaleY, double Shear) SimilarityParameters(Random random)
    {
        double scale = Range(random, .26, .7);
        return (scale, scale, 0);
    }

    private static IfsTransformFamily PickFamily(IReadOnlyList<IfsTransformFamily> families, Random random)
    {
        double total = families.Sum(FamilyWeight);
        double target = random.NextDouble() * total;
        foreach (IfsTransformFamily family in families)
        {
            target -= FamilyWeight(family);
            if (target <= 0) return family;
        }
        return families[^1];
    }

    private static double FamilyWeight(IfsTransformFamily family) => family switch
    {
        IfsTransformFamily.Anisotropic => 1.2,
        IfsTransformFamily.Reflection => .75,
        IfsTransformFamily.Stem => .35,
        _ => 1
    };

    private static double RawProbability(
        IfsAffineTransform transform,
        IfsProbabilityMode mode,
        Random random) => mode switch
    {
        IfsProbabilityMode.Uniform => 1,
        IfsProbabilityMode.Random => Range(random, .15, 1.5),
        _ => Math.Max(.02, Math.Abs(transform.A * transform.D - transform.B * transform.C))
            * Range(random, .8, 1.35)
    };

    private static void LimitContraction(ref double a, ref double b, ref double c, ref double d)
    {
        double trace = a * a + b * b + c * c + d * d;
        double determinant = a * d - b * c;
        double discriminant = Math.Max(0, trace * trace - 4 * determinant * determinant);
        double singularValue = Math.Sqrt((trace + Math.Sqrt(discriminant)) * .5);
        if (singularValue <= MaximumContraction) return;
        double scale = MaximumContraction / singularValue;
        a *= scale; b *= scale; c *= scale; d *= scale;
    }

    private static double Range(Random random, double minimum, double maximum) =>
        minimum + random.NextDouble() * (maximum - minimum);
}
