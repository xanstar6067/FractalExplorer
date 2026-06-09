namespace FractalExplorer.Utilities.UI
{
    /// <summary>
    /// Centralized WinForms debounce timer used to delay noisy UI-triggered actions.
    /// </summary>
    public sealed class DebounceTimer : IDisposable
    {
        private readonly System.Windows.Forms.Timer _timer = new();
        private bool _disposed;

        public DebounceTimer()
        {
            _timer.Tick += Timer_Tick;
        }

        public DebounceTimer(int interval) : this()
        {
            Interval = interval;
        }

        public event EventHandler? Tick;

        public int Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public bool Enabled => _timer.Enabled;

        public void Start()
        {
            ThrowIfDisposed();
            _timer.Start();
        }

        public void Stop()
        {
            if (_disposed)
            {
                return;
            }

            _timer.Stop();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _timer.Tick -= Timer_Tick;
            _timer.Stop();
            _timer.Dispose();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            Tick?.Invoke(this, e);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
