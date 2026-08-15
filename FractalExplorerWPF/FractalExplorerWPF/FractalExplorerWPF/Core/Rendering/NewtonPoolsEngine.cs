using System.Numerics;
using System.Text;
using System.Windows.Media;
using FractalExplorerWPF.Core.NewtonMath;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

public sealed class NewtonPoolsEngine
{
    private ExpressionNode? _formula;
    private ExpressionNode? _firstDerivative;
    private ExpressionNode? _secondDerivative;
    private readonly List<ExpressionNode> _inverseDerivatives = [];
    private int _householderOrder = 3;
    private double _rootTolerance = 1e-6;
    private double _rootSearchRadius = 8;

    public int MaxIterations { get; set; } = 500;
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Scale { get; set; } = 3;
    public IReadOnlyList<Complex> Roots { get; private set; } = [];
    public Color[] RootColors { get; set; } = [];
    public Color BackgroundColor { get; set; } = Colors.Black;
    public bool UseGradient { get; set; }
    public NewtonIterationMethod IterationMethod { get; set; }
    public NewtonRootSearchMode RootSearchMode { get; set; }
    public string RootSearchStrategy { get; private set; } = "Не запускался";

    public double RootTolerance
    {
        get => _rootTolerance;
        set => _rootTolerance = Math.Clamp(value, 1e-12, 0.1);
    }

    public double RootSearchRadius
    {
        get => _rootSearchRadius;
        set => _rootSearchRadius = Math.Clamp(value, 0.01, 1e9);
    }

    public int HouseholderOrder
    {
        get => _householderOrder;
        set
        {
            _householderOrder = Math.Clamp(value, 2, 12);
            if (_formula is not null) BuildInverseDerivatives();
        }
    }

    public bool SetFormula(string expression, out string debugInfo, bool discoverRoots = true)
    {
        var debug = new StringBuilder();
        try
        {
            List<Token> tokens = new Tokenizer(expression).Tokenize();
            _formula = new Parser(tokens).Parse().Simplify();
            _firstDerivative = _formula.Differentiate("z").Simplify();
            _secondDerivative = _firstDerivative.Differentiate("z").Simplify();
            BuildInverseDerivatives();
            if (discoverRoots) FindRoots();
            else
            {
                Roots = [];
                RootSearchStrategy = "Использован сохранённый список";
            }

            debug.AppendLine($"Источник: {expression}");
            debug.AppendLine("Токены: " + string.Join(" ", tokens.Select(token => $"[{token.Type}:{token.Value}]")));
            debug.AppendLine($"f(z) = {_formula}");
            debug.AppendLine($"f'(z) = {_firstDerivative}");
            debug.AppendLine($"f''(z) = {_secondDerivative}");
            debug.AppendLine($"Найдено корней: {Roots.Count}");
            debug.AppendLine($"Поиск: {RootSearchStrategy}");
            debug.AppendLine($"Точность: {RootTolerance:G3}; радиус адаптивного поиска: {RootSearchRadius:G6}");
            debugInfo = debug.ToString();
            return true;
        }
        catch (Exception ex)
        {
            _formula = null;
            _firstDerivative = null;
            _secondDerivative = null;
            _inverseDerivatives.Clear();
            Roots = [];
            RootSearchStrategy = "Ошибка формулы";
            debugInfo = $"ОШИБКА ПАРСИНГА:{Environment.NewLine}{ex.Message}";
            return false;
        }
    }

    public void ReplaceRoots(IEnumerable<Complex> roots)
    {
        double mergeDistance = Math.Max(1e-10, RootTolerance * 0.05);
        var accepted = new List<Complex>();
        foreach (Complex candidate in roots)
        {
            if (!IsFinite(candidate)) continue;
            int existing = accepted.FindIndex(root => (root - candidate).Magnitude <= mergeDistance);
            if (existing < 0) accepted.Add(candidate);
            else accepted[existing] = (accepted[existing] + candidate) / 2;
        }

        Roots = accepted.OrderBy(root => root.Real).ThenBy(root => root.Imaginary).ToArray();
    }

    public void RenderToBuffer(
        byte[] buffer,
        int width,
        int height,
        int stride,
        int threadCount,
        CancellationToken cancellationToken,
        Action<int>? reportProgress = null)
    {
        if (width <= 0 || height <= 0 || stride < width * 4)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (_formula is null || _firstDerivative is null || Roots.Count == 0)
        {
            Fill(buffer, width, height, stride, BackgroundColor);
            reportProgress?.Invoke(100);
            return;
        }

        long completedRows = 0;
        double unitsPerPixel = Scale / width;
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, threadCount)
        };

        Parallel.For(0, height, options, (y, loopState) =>
        {
            if (cancellationToken.IsCancellationRequested) { loopState.Stop(); return; }
            var variables = new Dictionary<string, Complex>(1);
            int row = y * stride;
            for (int x = 0; x < width; x++)
            {
                if ((x & 63) == 0 && cancellationToken.IsCancellationRequested) { loopState.Stop(); return; }
                Complex z = new(
                    CenterX + (x - width / 2.0) * unitsPerPixel,
                    CenterY - (y - height / 2.0) * unitsPerPixel);
                int iteration = Iterate(ref z, variables);
                Color color = GetPixelColor(z, iteration);
                int offset = row + x * 4;
                buffer[offset] = color.B;
                buffer[offset + 1] = color.G;
                buffer[offset + 2] = color.R;
                buffer[offset + 3] = color.A;
            }

            int rows = (int)Interlocked.Increment(ref completedRows);
            if (rows == height || rows % Math.Max(1, height / 100) == 0)
                reportProgress?.Invoke(rows * 100 / height);
        });
    }

    public byte[]? RenderTile(MandelbrotRenderTile tile, int canvasWidth, int canvasHeight, CancellationToken token)
    {
        byte[] buffer = new byte[checked(tile.Width * tile.Height * 4)];
        if (_formula is null || _firstDerivative is null || Roots.Count == 0)
        {
            Fill(buffer, tile.Width, tile.Height, tile.Width * 4, BackgroundColor);
            return buffer;
        }
        double unitsPerPixel = Scale / canvasWidth;
        var variables = new Dictionary<string, Complex>(1);
        for (int localY = 0; localY < tile.Height; localY++)
        {
            if (token.IsCancellationRequested) return null;
            int canvasY = tile.Y + localY;
            for (int localX = 0; localX < tile.Width; localX++)
            {
                if ((localX & 31) == 0 && token.IsCancellationRequested) return null;
                int canvasX = tile.X + localX;
                Complex z = new(
                    CenterX + (canvasX - canvasWidth / 2.0) * unitsPerPixel,
                    CenterY - (canvasY - canvasHeight / 2.0) * unitsPerPixel);
                int iteration = Iterate(ref z, variables);
                Color color = GetPixelColor(z, iteration);
                int offset = (localY * tile.Width + localX) * 4;
                buffer[offset] = color.B; buffer[offset + 1] = color.G; buffer[offset + 2] = color.R; buffer[offset + 3] = color.A;
            }
        }
        return buffer;
    }

    private int Iterate(ref Complex z, Dictionary<string, Complex> variables)
    {
        int iteration = 0;
        while (iteration < MaxIterations)
        {
            variables["z"] = z;
            Complex f = _formula!.Evaluate(variables);
            if (!IsFinite(f) || f == Complex.Zero) break;
            if ((f.Magnitude <= RootTolerance || (iteration & 7) == 7) && IsNearKnownRoot(z)) break;

            Complex step = IterationMethod switch
            {
                NewtonIterationMethod.Halley => ComputeHalleyStep(variables, f),
                NewtonIterationMethod.Householder => ComputeHouseholderStep(variables),
                _ => ComputeNewtonStep(variables, f)
            };

            if (!IsFinite(step) || step == Complex.Zero) break;
            z += step;
            iteration++;
        }
        return iteration;
    }

    private Complex ComputeNewtonStep(Dictionary<string, Complex> variables, Complex f)
    {
        Complex f1 = _firstDerivative!.Evaluate(variables);
        return !IsFinite(f1) || f1 == Complex.Zero ? Complex.Zero : -f / f1;
    }

    private Complex ComputeHalleyStep(Dictionary<string, Complex> variables, Complex f)
    {
        Complex f1 = _firstDerivative!.Evaluate(variables);
        if (!IsFinite(f1) || f1 == Complex.Zero) return Complex.Zero;
        Complex f2 = _secondDerivative!.Evaluate(variables);
        Complex denominator = 2 * f1 * f1 - f * f2;
        return !IsFinite(denominator) || denominator == Complex.Zero ? Complex.Zero : -(2 * f * f1) / denominator;
    }

    private Complex ComputeHouseholderStep(Dictionary<string, Complex> variables)
    {
        int order = HouseholderOrder;
        if (_inverseDerivatives.Count <= order) return Complex.Zero;
        Complex previous = _inverseDerivatives[order - 1].Evaluate(variables);
        Complex current = _inverseDerivatives[order].Evaluate(variables);
        return !IsFinite(previous) || !IsFinite(current) || current == Complex.Zero
            ? Complex.Zero
            : order * previous / current;
    }

    private void BuildInverseDerivatives()
    {
        _inverseDerivatives.Clear();
        if (_formula is null) return;
        ExpressionNode current = new BinaryOpNode(new NumberNode(Complex.One), "/", _formula).Simplify();
        _inverseDerivatives.Add(current);
        for (int index = 1; index <= Math.Max(2, HouseholderOrder); index++)
        {
            current = current.Differentiate("z").Simplify();
            _inverseDerivatives.Add(current);
        }
    }

    private void FindRoots()
    {
        if (_formula is null || _firstDerivative is null)
        {
            Roots = [];
            RootSearchStrategy = "Формула не задана";
            return;
        }

        if (RootSearchMode == NewtonRootSearchMode.ManualOnly)
        {
            Roots = [];
            RootSearchStrategy = "Только вручную";
            return;
        }

        if (RootSearchMode == NewtonRootSearchMode.Automatic &&
            NewtonRootFinder.TryFindPolynomialRoots(_formula, RootTolerance, out IReadOnlyList<Complex> polynomialRoots, out int degree))
        {
            Roots = polynomialRoots;
            RootSearchStrategy = degree > 0
                ? $"Aberth–Ehrlich, полином степени {degree}"
                : "Константный полином";
            return;
        }

        Roots = NewtonRootFinder.FindAdaptiveRoots(
            _formula,
            _firstDerivative,
            CenterX,
            CenterY,
            RootSearchRadius,
            RootTolerance);
        RootSearchStrategy = $"Адаптивное сканирование, радиус {RootSearchRadius:G6}";
    }

    private bool IsNearKnownRoot(Complex z)
    {
        foreach (Complex root in Roots)
            if ((z - root).Magnitude <= RootTolerance) return true;
        return false;
    }

    private Color GetPixelColor(Complex z, int iteration)
    {
        if (RootColors.Length == 0 || Roots.Count == 0) return BackgroundColor;
        int rootIndex = -1;
        double distance = double.MaxValue;
        for (int index = 0; index < Roots.Count; index++)
        {
            double candidate = (z - Roots[index]).Magnitude;
            if (candidate >= distance) continue;
            distance = candidate;
            rootIndex = index;
        }
        if (rootIndex < 0 || distance > RootTolerance) return BackgroundColor;

        Color baseColor = RootColors[rootIndex % RootColors.Length];
        if (!UseGradient) return baseColor;
        double t = Math.Min(1, (double)iteration / Math.Max(1, MaxIterations));
        t = 1 - Math.Pow(1 - t, 2);
        return Lerp(baseColor, BackgroundColor, t);
    }

    private static Color Lerp(Color start, Color end, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (byte)Math.Round(start.A + (end.A - start.A) * amount),
            (byte)Math.Round(start.R + (end.R - start.R) * amount),
            (byte)Math.Round(start.G + (end.G - start.G) * amount),
            (byte)Math.Round(start.B + (end.B - start.B) * amount));
    }

    private static bool IsFinite(Complex value) =>
        double.IsFinite(value.Real) && double.IsFinite(value.Imaginary);

    private static void Fill(byte[] buffer, int width, int height, int stride, Color color)
    {
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int offset = y * stride + x * 4;
            buffer[offset] = color.B;
            buffer[offset + 1] = color.G;
            buffer[offset + 2] = color.R;
            buffer[offset + 3] = color.A;
        }
    }
}
