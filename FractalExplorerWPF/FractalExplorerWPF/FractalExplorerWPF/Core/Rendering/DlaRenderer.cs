using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Core.Rendering;

public sealed class DlaRenderer
{
    private static readonly (int X, int Y)[] NeighborOffsets =
    [
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),             (1, 0),
        (-1, 1),  (0, 1),   (1, 1)
    ];

    private readonly DlaState _state;
    private readonly bool[] _occupied;
    private readonly int[] _depths;
    private readonly List<DlaParticle> _particles = [];
    private readonly Random _random;
    private readonly int _center;
    private int _minimumX;
    private int _maximumX;
    private int _minimumY;
    private int _maximumY;
    private double _maximumRadius = 1;
    private int _failedBatches;

    public int ParticleCount => _particles.Count;
    public int FailedWalkers { get; private set; }
    public int MaximumDepth { get; private set; }
    public bool Complete => _particles.Count >= _state.ParticleCount || _failedBatches >= 5;

    public DlaRenderer(DlaState state)
    {
        _state = state.Clone();
        _occupied = new bool[checked(state.GridSize * state.GridSize)];
        _depths = new int[_occupied.Length];
        _random = new Random(state.RandomSeed);
        _center = state.GridSize / 2;
        InitializeSeeds();
    }

    public void Grow(int requestedParticles, CancellationToken token)
    {
        int startCount = _particles.Count;
        int desired = Math.Min(_state.ParticleCount, startCount + Math.Max(1, requestedParticles));
        int maximumAttempts = Math.Max(100, (desired - startCount) * 45);
        int attempts = 0;

        while (_particles.Count < desired && attempts++ < maximumAttempts && !token.IsCancellationRequested)
        {
            if (!TryGrowWalker(token)) FailedWalkers++;
        }

        _failedBatches = _particles.Count == startCount ? _failedBatches + 1 : 0;
    }

    public byte[] CreateFrame(int width, int height, CancellationToken token)
    {
        var pixels = new byte[checked(width * height * 4)];
        RasterDrawing.Fill(pixels, _state.BackgroundColor);
        double worldHeight = _state.ViewWidth * height / width;
        double pixelsPerWorld = width / _state.ViewWidth;
        double cellWorldSize = 2d / (_state.GridSize - 1);
        double radiusPixels = Math.Max(0.35, _state.ParticleRadius * cellWorldSize * pixelsPerWorld);

        for (int index = 0; index < _particles.Count; index++)
        {
            if ((index & 511) == 0 && token.IsCancellationRequested) break;
            DlaParticle particle = _particles[index];
            double worldX = (particle.X - _center) * cellWorldSize;
            double worldY = (_center - particle.Y) * cellWorldSize;
            double x = (worldX - (_state.CenterX - _state.ViewWidth / 2)) * pixelsPerWorld;
            double y = ((_state.CenterY + worldHeight / 2) - worldY) * pixelsPerWorld;
            if (x + radiusPixels < 0 || x - radiusPixels >= width ||
                y + radiusPixels < 0 || y - radiusPixels >= height)
                continue;

            RasterDrawing.DrawCircle(pixels, width, height, x, y, radiusPixels,
                1, ColorFor(particle, index), true);
        }
        return pixels;
    }

    private void InitializeSeeds()
    {
        if (_state.SeedMode == DlaSeedMode.Center)
        {
            AddSeed(_center, _center);
            return;
        }

        int seedY = _state.GridSize - Math.Max(12, _state.GridSize / 18);
        if (_state.SeedMode == DlaSeedMode.BottomPoint)
        {
            AddSeed(_center, seedY);
            return;
        }

        int left = _state.GridSize * 15 / 100;
        int right = _state.GridSize * 85 / 100;
        for (int x = left; x <= right; x += 2) AddSeed(x, seedY);
    }

    private void AddSeed(int x, int y)
    {
        int index = Index(x, y);
        if (_occupied[index]) return;
        _occupied[index] = true;
        _particles.Add(new DlaParticle(x, y, 0));
        _minimumX = _particles.Count == 1 ? x : Math.Min(_minimumX, x);
        _maximumX = _particles.Count == 1 ? x : Math.Max(_maximumX, x);
        _minimumY = _particles.Count == 1 ? y : Math.Min(_minimumY, y);
        _maximumY = _particles.Count == 1 ? y : Math.Max(_maximumY, y);
    }

    private bool TryGrowWalker(CancellationToken token)
    {
        (int x, int y) = SpawnWalker();
        for (int step = 0; step < _state.MaxStepsPerWalker; step++)
        {
            if ((step & 255) == 0 && token.IsCancellationRequested) return false;
            int parentDepth = NeighborDepth(x, y);
            if (parentDepth >= 0 && _random.NextDouble() <= _state.Stickiness)
            {
                Attach(x, y, parentDepth + 1);
                return true;
            }

            int safeJump = SafeJumpDistance(x, y);
            (int dx, int dy) = Direction();
            int nextX = x + dx * safeJump;
            int nextY = y + dy * safeJump;
            if (!Inside(nextX, nextY) || OutsideKillBoundary(nextX, nextY)) return false;
            if (_occupied[Index(nextX, nextY)]) continue;
            x = nextX;
            y = nextY;
        }
        return false;
    }

    private (int X, int Y) SpawnWalker()
    {
        if (_state.SeedMode == DlaSeedMode.Center)
        {
            double launchRadius = Math.Min(_center - 4, _maximumRadius + 12);
            double angle = _random.NextDouble() * Math.PI * 2;
            return (_center + (int)Math.Round(Math.Cos(angle) * launchRadius),
                _center + (int)Math.Round(Math.Sin(angle) * launchRadius));
        }

        int launchY = Math.Max(2, _minimumY - 14);
        int margin = _state.SeedMode == DlaSeedMode.BottomPoint ? 42 : 24;
        int left = Math.Max(2, _minimumX - margin);
        int right = Math.Min(_state.GridSize - 3, _maximumX + margin);
        return (_random.Next(left, Math.Max(left + 1, right + 1)), launchY);
    }

    private int SafeJumpDistance(int x, int y)
    {
        if (_state.SeedMode == DlaSeedMode.Center)
        {
            double distance = Math.Sqrt((x - _center) * (double)(x - _center) +
                                        (y - _center) * (double)(y - _center));
            return Math.Clamp((int)Math.Floor(distance - _maximumRadius - 2), 1, 7);
        }

        int horizontalDistance = x < _minimumX ? _minimumX - x : x > _maximumX ? x - _maximumX : 0;
        int verticalDistance = y < _minimumY ? _minimumY - y : y > _maximumY ? y - _maximumY : 0;
        return Math.Clamp(Math.Max(horizontalDistance, verticalDistance) - 2, 1, 6);
    }

    private (int X, int Y) Direction()
    {
        double magnitude = Math.Abs(_state.DriftX) + Math.Abs(_state.DriftY);
        double biasProbability = Math.Clamp(magnitude / (1 + magnitude), 0, 0.78);
        if (magnitude > 0 && _random.NextDouble() < biasProbability)
        {
            if (_random.NextDouble() < Math.Abs(_state.DriftX) / magnitude)
                return (Math.Sign(_state.DriftX), _random.Next(-1, 2));
            return (_random.Next(-1, 2), Math.Sign(_state.DriftY));
        }
        return NeighborOffsets[_random.Next(NeighborOffsets.Length)];
    }

    private int NeighborDepth(int x, int y)
    {
        int depth = int.MaxValue;
        bool found = false;
        foreach ((int dx, int dy) in NeighborOffsets)
        {
            int neighborX = x + dx;
            int neighborY = y + dy;
            if (!Inside(neighborX, neighborY)) continue;
            int index = Index(neighborX, neighborY);
            if (!_occupied[index]) continue;
            depth = Math.Min(depth, _depths[index]);
            found = true;
        }
        return found ? depth : -1;
    }

    private void Attach(int x, int y, int depth)
    {
        int index = Index(x, y);
        if (_occupied[index]) return;
        _occupied[index] = true;
        _depths[index] = depth;
        _particles.Add(new DlaParticle(x, y, depth));
        MaximumDepth = Math.Max(MaximumDepth, depth);
        _minimumX = Math.Min(_minimumX, x);
        _maximumX = Math.Max(_maximumX, x);
        _minimumY = Math.Min(_minimumY, y);
        _maximumY = Math.Max(_maximumY, y);
        double radius = Math.Sqrt((x - _center) * (double)(x - _center) +
                                  (y - _center) * (double)(y - _center));
        _maximumRadius = Math.Max(_maximumRadius, radius);
    }

    private bool OutsideKillBoundary(int x, int y)
    {
        if (_state.SeedMode == DlaSeedMode.Center)
        {
            double killRadius = Math.Min(_center - 2, _maximumRadius + 38);
            double dx = x - _center;
            double dy = y - _center;
            return dx * dx + dy * dy > killRadius * killRadius;
        }

        int sideMargin = _state.SeedMode == DlaSeedMode.BottomPoint ? 90 : 55;
        return y < Math.Max(1, _minimumY - 50) ||
               x < Math.Max(1, _minimumX - sideMargin) ||
               x > Math.Min(_state.GridSize - 2, _maximumX + sideMargin);
    }

    private System.Windows.Media.Color ColorFor(DlaParticle particle, int order)
    {
        double amount = _state.ColoringMode switch
        {
            DlaColoringMode.BranchDepth => particle.Depth / (double)Math.Max(1, MaximumDepth),
            DlaColoringMode.DistanceFromSeed => DistanceAmount(particle),
            _ => order / (double)Math.Max(1, _state.ParticleCount - 1)
        };
        return RasterDrawing.Lerp(_state.StartColor, _state.EndColor, amount);
    }

    private double DistanceAmount(DlaParticle particle)
    {
        if (_state.SeedMode == DlaSeedMode.Center)
        {
            double distance = Math.Sqrt((particle.X - _center) * (double)(particle.X - _center) +
                                        (particle.Y - _center) * (double)(particle.Y - _center));
            return distance / Math.Max(1, _maximumRadius);
        }
        return (_maximumY - particle.Y) / (double)Math.Max(1, _maximumY - _minimumY);
    }

    private bool Inside(int x, int y) =>
        x > 0 && y > 0 && x < _state.GridSize - 1 && y < _state.GridSize - 1;

    private int Index(int x, int y) => y * _state.GridSize + x;

    private readonly record struct DlaParticle(int X, int Y, int Depth);
}
