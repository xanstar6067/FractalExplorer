using System.Numerics;
using System.Text;
using System.Windows.Media;
using FractalExplorerWPF.Core.NewtonMath;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Core.Rendering;

public sealed class NewtonPoolsEngine
{
    private const double Epsilon = 1e-6;
    private ExpressionNode? _formula;
    private ExpressionNode? _firstDerivative;
    private ExpressionNode? _secondDerivative;
    private readonly List<ExpressionNode> _inverseDerivatives = [];
    private int _householderOrder = 3;

    public int MaxIterations { get; set; } = 500;
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Scale { get; set; } = 3;
    public IReadOnlyList<Complex> Roots { get; private set; } = [];
    public Color[] RootColors { get; set; } = [];
    public Color BackgroundColor { get; set; } = Colors.Black;
    public bool UseGradient { get; set; }
    public NewtonIterationMethod IterationMethod { get; set; }

    public int HouseholderOrder
    {
        get => _householderOrder;
        set
        {
            _householderOrder = Math.Clamp(value, 2, 12);
            if (_formula is not null) BuildInverseDerivatives();
        }
    }

    public bool SetFormula(string expression, out string debugInfo)
    {
        var debug = new StringBuilder();
        try
        {
            List<Token> tokens = new Tokenizer(expression).Tokenize();
            _formula = new Parser(tokens).Parse().Simplify();
            _firstDerivative = _formula.Differentiate("z").Simplify();
            _secondDerivative = _firstDerivative.Differentiate("z").Simplify();
            BuildInverseDerivatives();
            FindRoots();

            debug.AppendLine($"Источник: {expression}");
            debug.AppendLine("Токены: " + string.Join(" ", tokens.Select(token => $"[{token.Type}:{token.Value}]")));
            debug.AppendLine($"f(z) = {_formula}");
            debug.AppendLine($"f'(z) = {_firstDerivative}");
            debug.AppendLine($"f''(z) = {_secondDerivative}");
            debug.AppendLine($"Найдено корней: {Roots.Count}");
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
            debugInfo = $"ОШИБКА ПАРСИНГА:{Environment.NewLine}{ex.Message}";
            return false;
        }
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
            MaxDegreeOfParallelism = Math.Max(1, threadCount),
            CancellationToken = cancellationToken
        };

        Parallel.For(0, height, options, y =>
        {
            var variables = new Dictionary<string, Complex>(1);
            int row = y * stride;
            for (int x = 0; x < width; x++)
            {
                if ((x & 63) == 0) cancellationToken.ThrowIfCancellationRequested();
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

    public byte[] RenderTile(MandelbrotRenderTile tile, int canvasWidth, int canvasHeight, CancellationToken token)
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
            int canvasY = tile.Y + localY;
            for (int localX = 0; localX < tile.Width; localX++)
            {
                if ((localX & 31) == 0) token.ThrowIfCancellationRequested();
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
            if (f.Magnitude < Epsilon) break;

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
        return f1.Magnitude < Epsilon ? Complex.Zero : -f / f1;
    }

    private Complex ComputeHalleyStep(Dictionary<string, Complex> variables, Complex f)
    {
        Complex f1 = _firstDerivative!.Evaluate(variables);
        if (f1.Magnitude < Epsilon) return Complex.Zero;
        Complex f2 = _secondDerivative!.Evaluate(variables);
        Complex denominator = 2 * f1 * f1 - f * f2;
        return denominator.Magnitude < Epsilon ? Complex.Zero : -(2 * f * f1) / denominator;
    }

    private Complex ComputeHouseholderStep(Dictionary<string, Complex> variables)
    {
        int order = HouseholderOrder;
        if (_inverseDerivatives.Count <= order) return Complex.Zero;
        Complex previous = _inverseDerivatives[order - 1].Evaluate(variables);
        Complex current = _inverseDerivatives[order].Evaluate(variables);
        return current.Magnitude < Epsilon ? Complex.Zero : order * previous / current;
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

    private void FindRoots(int maxIterations = 100)
    {
        var roots = new List<Complex>();
        if (_formula is null || _firstDerivative is null)
        {
            Roots = roots;
            return;
        }

        var starts = new List<Complex>();
        for (double radius = 0.1; radius < 2.5; radius += 0.4)
            for (int index = 0; index < 16; index++)
                starts.Add(Complex.FromPolarCoordinates(radius, 2 * Math.PI * index / 16));
        starts.Add(Complex.Zero);

        foreach (Complex start in starts)
        {
            Complex z = start;
            var variables = new Dictionary<string, Complex>(1);
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                variables["z"] = z;
                Complex f = _formula.Evaluate(variables);
                Complex f1 = _firstDerivative.Evaluate(variables);
                if (f1.Magnitude < Epsilon / 100) break;
                Complex step = f / f1;
                z -= step;
                if (!IsFinite(z) || z.Magnitude > 1e4) break;
                if (step.Magnitude >= Epsilon) continue;

                variables["z"] = z;
                if (_formula.Evaluate(variables).Magnitude < Epsilon * 10 &&
                    roots.All(root => (z - root).Magnitude >= Epsilon))
                    roots.Add(z);
                break;
            }
        }

        Roots = roots.OrderBy(root => root.Real).ThenBy(root => root.Imaginary).ToArray();
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
        if (rootIndex < 0 || distance >= Epsilon) return BackgroundColor;

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
