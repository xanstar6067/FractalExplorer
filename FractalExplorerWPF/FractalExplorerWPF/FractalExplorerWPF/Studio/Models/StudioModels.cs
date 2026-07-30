using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FractalExplorerWPF.Studio.Dsl;

namespace FractalExplorerWPF.Studio.Models;

public enum StudioPrecisionMode
{
    Double,
    Decimal
}

public enum StudioLayerRenderState
{
    Stale,
    Rendering,
    Ready,
    Error
}

public enum StudioBlendMode
{
    Normal,
    Add,
    Subtract,
    Multiply,
    Screen,
    Overlay,
    SoftLight,
    HardLight,
    Darken,
    Lighten,
    ColorDodge,
    ColorBurn,
    Difference,
    Exclusion,
    Hue,
    Saturation,
    Color,
    Luminosity
}

public sealed class StudioParameterValue : INotifyPropertyChanged
{
    private string _value = string.Empty;

    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required StudioValueKind Kind { get; init; }
    public string? Minimum { get; init; }
    public string? Maximum { get; init; }
    public string? Step { get; init; }

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
                return;
            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class StudioLayer : INotifyPropertyChanged
{
    private string _name = "Фрактальный слой";
    private bool _isVisible = true;
    private double _opacity = 1;
    private StudioBlendMode _blendMode;
    private StudioPrecisionMode _precisionMode;
    private string _formulaSource = StudioFormulaPresets.Mandelbrot;
    private decimal _centerX = -0.5m;
    private decimal _centerY;
    private decimal _zoom = 1m;
    private bool _isLinkedToMasterCamera = true;
    private StudioLayerRenderState _renderState = StudioLayerRenderState.Stale;
    private string? _errorMessage;
    private double _paletteFrequency = 1;
    private double _palettePhase;

    public Guid Id { get; init; } = Guid.NewGuid();
    public ObservableCollection<StudioParameterValue> Parameters { get; } = [];

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    public double Opacity
    {
        get => _opacity;
        set => SetField(ref _opacity, Math.Clamp(value, 0, 1));
    }

    public StudioBlendMode BlendMode
    {
        get => _blendMode;
        set => SetField(ref _blendMode, value);
    }

    public StudioPrecisionMode PrecisionMode
    {
        get => _precisionMode;
        set => SetField(ref _precisionMode, value);
    }

    public string FormulaSource
    {
        get => _formulaSource;
        set => SetField(ref _formulaSource, value);
    }

    public decimal CenterX
    {
        get => _centerX;
        set => SetField(ref _centerX, value);
    }

    public decimal CenterY
    {
        get => _centerY;
        set => SetField(ref _centerY, value);
    }

    public decimal Zoom
    {
        get => _zoom;
        set => SetField(ref _zoom, Math.Clamp(value, 0.01m, 1000000000000000000000000000m));
    }

    public bool IsLinkedToMasterCamera
    {
        get => _isLinkedToMasterCamera;
        set => SetField(ref _isLinkedToMasterCamera, value);
    }

    public StudioLayerRenderState RenderState
    {
        get => _renderState;
        set
        {
            if (SetField(ref _renderState, value))
                OnPropertyChanged(nameof(RenderStateText));
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public double PaletteFrequency
    {
        get => _paletteFrequency;
        set => SetField(ref _paletteFrequency, Math.Clamp(value, 0.01, 100));
    }

    public double PalettePhase
    {
        get => _palettePhase;
        set => SetField(ref _palettePhase, value);
    }

    public string RenderStateText => RenderState switch
    {
        StudioLayerRenderState.Stale => "устарел",
        StudioLayerRenderState.Rendering => "рендерится",
        StudioLayerRenderState.Ready => "готов",
        StudioLayerRenderState.Error => "ошибка",
        _ => RenderState.ToString()
    };

    public void SynchronizeParameters(StudioCompiledFormula formula)
    {
        IReadOnlyDictionary<string, string> defaults = formula.CreateDefaultParameterValues();
        var existing = Parameters.ToDictionary(value => value.Name, StringComparer.OrdinalIgnoreCase);
        var ordered = new List<StudioParameterValue>();
        foreach (StudioFormulaParameter parameter in formula.Document.Parameters)
        {
            string value = existing.TryGetValue(parameter.Name, out StudioParameterValue? current)
                ? current.Value
                : defaults[parameter.Name];
            ordered.Add(new StudioParameterValue
            {
                Name = parameter.Name,
                DisplayName = parameter.DisplayName,
                Kind = parameter.Kind,
                Minimum = parameter.Metadata.GetValueOrDefault("min"),
                Maximum = parameter.Metadata.GetValueOrDefault("max"),
                Step = parameter.Metadata.GetValueOrDefault("step"),
                Value = value
            });
        }

        Parameters.Clear();
        foreach (StudioParameterValue parameter in ordered)
            Parameters.Add(parameter);
    }

    public StudioLayer Clone()
    {
        var clone = new StudioLayer
        {
            Name = Name + " — копия",
            IsVisible = IsVisible,
            Opacity = Opacity,
            BlendMode = BlendMode,
            PrecisionMode = PrecisionMode,
            FormulaSource = FormulaSource,
            CenterX = CenterX,
            CenterY = CenterY,
            Zoom = Zoom,
            IsLinkedToMasterCamera = IsLinkedToMasterCamera,
            PaletteFrequency = PaletteFrequency,
            PalettePhase = PalettePhase
        };
        foreach (StudioParameterValue parameter in Parameters)
        {
            clone.Parameters.Add(new StudioParameterValue
            {
                Name = parameter.Name,
                DisplayName = parameter.DisplayName,
                Kind = parameter.Kind,
                Minimum = parameter.Minimum,
                Maximum = parameter.Maximum,
                Step = parameter.Step,
                Value = parameter.Value
            });
        }
        return clone;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class StudioProject : INotifyPropertyChanged
{
    private string _name = "Новая композиция";
    private decimal _masterCenterX = -0.5m;
    private decimal _masterCenterY;
    private decimal _masterZoom = 1;
    private bool _autoRender = true;
    private int _previewSsaa = 1;
    private int _threadCount;

    public int FormatVersion { get; init; } = 1;
    public ObservableCollection<StudioLayer> Layers { get; } = [];

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public decimal MasterCenterX
    {
        get => _masterCenterX;
        set => SetField(ref _masterCenterX, value);
    }

    public decimal MasterCenterY
    {
        get => _masterCenterY;
        set => SetField(ref _masterCenterY, value);
    }

    public decimal MasterZoom
    {
        get => _masterZoom;
        set => SetField(ref _masterZoom, Math.Clamp(value, 0.01m, 1000000000000000000000000000m));
    }

    public bool AutoRender
    {
        get => _autoRender;
        set => SetField(ref _autoRender, value);
    }

    public int PreviewSsaa
    {
        get => _previewSsaa;
        set => SetField(ref _previewSsaa, Math.Clamp(value, 1, 4));
    }

    public int ThreadCount
    {
        get => _threadCount;
        set => SetField(ref _threadCount, Math.Clamp(value, 0, Environment.ProcessorCount));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record StudioLayerSnapshot(
    Guid Id,
    string Name,
    bool IsVisible,
    double Opacity,
    StudioBlendMode BlendMode,
    StudioPrecisionMode PrecisionMode,
    string FormulaSource,
    decimal CenterX,
    decimal CenterY,
    decimal Zoom,
    double PaletteFrequency,
    double PalettePhase,
    IReadOnlyDictionary<string, string> Parameters)
{
    public static StudioLayerSnapshot Capture(StudioLayer layer, StudioProject project)
    {
        decimal centerX = layer.IsLinkedToMasterCamera ? project.MasterCenterX : layer.CenterX;
        decimal centerY = layer.IsLinkedToMasterCamera ? project.MasterCenterY : layer.CenterY;
        decimal zoom = layer.IsLinkedToMasterCamera ? project.MasterZoom : layer.Zoom;
        return new StudioLayerSnapshot(
            layer.Id,
            layer.Name,
            layer.IsVisible,
            layer.Opacity,
            layer.BlendMode,
            layer.PrecisionMode,
            layer.FormulaSource,
            centerX,
            centerY,
            zoom,
            layer.PaletteFrequency,
            layer.PalettePhase,
            layer.Parameters.ToDictionary(value => value.Name, value => value.Value,
                StringComparer.OrdinalIgnoreCase));
    }
}
