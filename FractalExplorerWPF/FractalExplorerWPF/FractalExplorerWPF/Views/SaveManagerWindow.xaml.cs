using System.Windows;
using FractalExplorerWPF.Infrastructure;

namespace FractalExplorerWPF.Views;

public partial class SaveManagerWindow : Window
{
    private IDisposable? _controller;

    private SaveManagerWindow()
    {
        InitializeComponent();
        Closed += (_, _) => _controller?.Dispose();
    }

    public static bool? Open<TState>(Window owner, SaveManagerConfiguration<TState> configuration)
        where TState : class
    {
        var window = new SaveManagerWindow { Owner = owner };
        window._controller = new SaveManagerController<TState>(window, window.Manager, configuration);
        return window.ShowDialog();
    }
}
