using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Core.Rendering;

public sealed class IfsRenderer
{
    private readonly IfsState _state;
    private readonly int _width;
    private readonly int _height;
    private readonly float[] _x;
    private readonly float[] _y;
    private readonly byte[] _pixels;
    private readonly double[] _cumulativeWeights;
    private readonly Random _random = new(12345);

    private double _currentX;
    private double _currentY;
    private bool _burnedIn;
    private bool _boundsReady;
    private float _minX;
    private float _maxX;
    private float _minY;
    private float _maxY;

    public int GeneratedPoints { get; private set; }
    public int PlottedPoints { get; private set; }

    public IfsRenderer(IfsState state, int width, int height)
    {
        _state = state.Clone();
        _width = width;
        _height = height;
        int count = Math.Max(1000, state.Iterations);
        _x = new float[count];
        _y = new float[count];
        _pixels = new byte[checked(width * height * 4)];
        _cumulativeWeights = BuildCumulativeWeights(_state.Transforms);
        FillBackground();
    }

    public void Generate(int count, CancellationToken token)
    {
        if (_state.Transforms.Count == 0)
        {
            GeneratedPoints = _x.Length;
            return;
        }

        // Every point depends on the previous one, so this orbit itself must stay sequential.
        if (!_burnedIn)
        {
            int burn = Math.Min(100, _x.Length / 10);
            for (int index = 0; index < burn; index++) Step();
            _burnedIn = true;
        }

        int end = Math.Min(_x.Length, GeneratedPoints + Math.Max(1, count));
        for (; GeneratedPoints < end; GeneratedPoints++)
        {
            if ((GeneratedPoints & 4095) == 0 && token.IsCancellationRequested) return;
            Step();
            (_x[GeneratedPoints], _y[GeneratedPoints]) = ((float)_currentX, (float)_currentY);
        }

        if (GeneratedPoints == _x.Length && !_boundsReady) PrepareBounds(token);
    }

    public void Plot(int count, CancellationToken token)
    {
        if (!_boundsReady)
            throw new InvalidOperationException("Сначала необходимо построить орбиту IFS.");

        double viewportWidth = Math.Clamp(Math.Abs(_state.Scale), .05, 40);
        double viewportHeight = viewportWidth * _height / _width;
        double left = _state.CenterX - viewportWidth / 2;
        double top = _state.CenterY + viewportHeight / 2;
        float dx = Math.Max(1e-6f, _maxX - _minX);
        float dy = Math.Max(1e-6f, _maxY - _minY);
        int start = PlottedPoints;
        int end = Math.Min(_x.Length, start + Math.Max(1, count));

        Parallel.For(start, end, ParallelOptions(), (pointIndex, loopState) =>
        {
            if (token.IsCancellationRequested)
            {
                loopState.Stop();
                return;
            }
            double nx = (_x[pointIndex] - _minX) / dx;
            double ny = (_y[pointIndex] - _minY) / dy;
            double worldX = (nx - .5) * 2;
            double worldY = (ny - .5) * 2;
            int px = (int)((worldX - left) / viewportWidth * _width);
            int py = (int)((top - worldY) / viewportHeight * _height);
            if ((uint)px >= (uint)_width || (uint)py >= (uint)_height) return;

            int pixel = (py * _width + px) * 4;
            // Collisions are benign because every point writes exactly the same color.
            _pixels[pixel] = _state.FractalColor.B;
            _pixels[pixel + 1] = _state.FractalColor.G;
            _pixels[pixel + 2] = _state.FractalColor.R;
            _pixels[pixel + 3] = 255;
        });

        if (!token.IsCancellationRequested) PlottedPoints = end;
    }

    public byte[] CreateFrame() => (byte[])_pixels.Clone();

    private void Step()
    {
        IfsAffineTransform transform = Pick(_random.NextDouble());
        double nextX = transform.A * _currentX + transform.B * _currentY + transform.E;
        double nextY = transform.C * _currentX + transform.D * _currentY + transform.F;
        _currentX = (float)nextX;
        _currentY = (float)nextY;
    }

    private IfsAffineTransform Pick(double value)
    {
        int lower = 0;
        int upper = _cumulativeWeights.Length - 1;
        while (lower < upper)
        {
            int middle = lower + (upper - lower) / 2;
            if (value <= _cumulativeWeights[middle])
                upper = middle;
            else
                lower = middle + 1;
        }

        return _state.Transforms[lower];
    }

    private static double[] BuildCumulativeWeights(IReadOnlyList<IfsAffineTransform> transforms)
    {
        var cumulativeWeights = new double[transforms.Count];
        if (transforms.Count == 0)
            return cumulativeWeights;

        double total = transforms.Sum(transform => Math.Max(0, transform.Probability));
        double cumulative = 0;
        for (int index = 0; index < transforms.Count; index++)
        {
            cumulative += total > 0
                ? Math.Max(0, transforms[index].Probability) / total
                : 1d / transforms.Count;
            cumulativeWeights[index] = cumulative;
        }

        // Avoid a floating-point gap at the top of the interval.
        cumulativeWeights[^1] = 1;
        return cumulativeWeights;
    }

    private void PrepareBounds(CancellationToken token)
    {
        _minX = float.PositiveInfinity;
        _maxX = float.NegativeInfinity;
        _minY = float.PositiveInfinity;
        _maxY = float.NegativeInfinity;
        object extremaLock = new();

        Parallel.For(0, _x.Length, ParallelOptions(),
            () => (MinX: float.PositiveInfinity, MaxX: float.NegativeInfinity,
                MinY: float.PositiveInfinity, MaxY: float.NegativeInfinity),
            (index, loopState, local) =>
            {
                if (token.IsCancellationRequested)
                {
                    loopState.Stop();
                    return local;
                }
                float x = _x[index];
                float y = _y[index];
                return (Math.Min(local.MinX, x), Math.Max(local.MaxX, x),
                    Math.Min(local.MinY, y), Math.Max(local.MaxY, y));
            },
            local =>
            {
                lock (extremaLock)
                {
                    _minX = Math.Min(_minX, local.MinX);
                    _maxX = Math.Max(_maxX, local.MaxX);
                    _minY = Math.Min(_minY, local.MinY);
                    _maxY = Math.Max(_maxY, local.MaxY);
                }
            });

        _boundsReady = !token.IsCancellationRequested;
    }

    private void FillBackground()
    {
        Parallel.For(0, _height, ParallelOptions(), y =>
        {
            int end = (y + 1) * _width * 4;
            for (int pixel = y * _width * 4; pixel < end; pixel += 4)
            {
                _pixels[pixel] = _state.BackgroundColor.B;
                _pixels[pixel + 1] = _state.BackgroundColor.G;
                _pixels[pixel + 2] = _state.BackgroundColor.R;
                _pixels[pixel + 3] = _state.BackgroundColor.A;
            }
        });
    }

    private static ParallelOptions ParallelOptions() => new()
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };
}
