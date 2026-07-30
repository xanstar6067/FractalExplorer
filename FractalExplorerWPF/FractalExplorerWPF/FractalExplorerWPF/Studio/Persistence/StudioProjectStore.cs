using System.IO.Compression;
using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Studio.Dsl;
using FractalExplorerWPF.Studio.Models;

namespace FractalExplorerWPF.Studio.Persistence;

public static class StudioProjectStore
{
    private const string ManifestEntryName = "project.json";

    public static async Task SaveAsync(
        string path,
        StudioProject project,
        byte[]? previewPng,
        CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(project);
        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        string temporaryPath = fullPath + $".tmp-{Guid.NewGuid():N}";
        try
        {
            await using (var file = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             81920,
                             FileOptions.Asynchronous))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: false))
            {
                ZipArchiveEntry manifest = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
                await using (Stream stream = manifest.Open())
                {
                    await JsonSerializer.SerializeAsync(
                        stream,
                        StudioProjectDto.Capture(project),
                        JsonOptionsFactory.Create(),
                        token);
                }

                if (previewPng is { Length: > 0 })
                {
                    ZipArchiveEntry preview = archive.CreateEntry("preview.png", CompressionLevel.Fastest);
                    await using Stream stream = preview.Open();
                    await stream.WriteAsync(previewPng, token);
                }
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public static async Task<StudioProject> LoadAsync(string path, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using var file = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous);
        using var archive = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: false);
        ZipArchiveEntry manifest = archive.GetEntry(ManifestEntryName) ??
                                   throw new InvalidDataException(
                                       "В контейнере отсутствует project.json.");
        await using Stream stream = manifest.Open();
        StudioProjectDto dto = await JsonSerializer.DeserializeAsync<StudioProjectDto>(
                                   stream,
                                   JsonOptionsFactory.Create(),
                                   token) ??
                               throw new InvalidDataException("Не удалось прочитать проект Fractal Studio.");
        if (dto.FormatVersion != 1)
            throw new InvalidDataException(
                $"Версия проекта {dto.FormatVersion} пока не поддерживается.");
        return dto.Restore();
    }

    private sealed class StudioProjectDto
    {
        public int FormatVersion { get; set; } = 1;
        public string Name { get; set; } = "Новая композиция";
        public decimal MasterCenterX { get; set; } = -0.5m;
        public decimal MasterCenterY { get; set; }
        public decimal MasterZoom { get; set; } = 1;
        public bool AutoRender { get; set; } = true;
        public int PreviewSsaa { get; set; } = 1;
        public int ThreadCount { get; set; }
        public List<StudioLayerDto> Layers { get; set; } = [];

        public static StudioProjectDto Capture(StudioProject project) => new()
        {
            FormatVersion = project.FormatVersion,
            Name = project.Name,
            MasterCenterX = project.MasterCenterX,
            MasterCenterY = project.MasterCenterY,
            MasterZoom = project.MasterZoom,
            AutoRender = project.AutoRender,
            PreviewSsaa = project.PreviewSsaa,
            ThreadCount = project.ThreadCount,
            Layers = project.Layers.Select(StudioLayerDto.Capture).ToList()
        };

        public StudioProject Restore()
        {
            var project = new StudioProject
            {
                Name = Name,
                MasterCenterX = MasterCenterX,
                MasterCenterY = MasterCenterY,
                MasterZoom = MasterZoom,
                AutoRender = AutoRender,
                PreviewSsaa = PreviewSsaa,
                ThreadCount = ThreadCount
            };
            foreach (StudioLayerDto layer in Layers)
                project.Layers.Add(layer.Restore());
            if (project.Layers.Count == 0)
            {
                var layer = new StudioLayer();
                layer.SynchronizeParameters(StudioFormulaCompiler.Compile(layer.FormulaSource));
                project.Layers.Add(layer);
            }
            return project;
        }
    }

    private sealed class StudioLayerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "Фрактальный слой";
        public bool IsVisible { get; set; } = true;
        public double Opacity { get; set; } = 1;
        public StudioBlendMode BlendMode { get; set; }
        public StudioPrecisionMode PrecisionMode { get; set; }
        public string FormulaSource { get; set; } = StudioFormulaPresets.Mandelbrot;
        public decimal CenterX { get; set; } = -0.5m;
        public decimal CenterY { get; set; }
        public decimal Zoom { get; set; } = 1;
        public bool IsLinkedToMasterCamera { get; set; } = true;
        public double PaletteFrequency { get; set; } = 1;
        public double PalettePhase { get; set; }
        public Dictionary<string, string> Parameters { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        public static StudioLayerDto Capture(StudioLayer layer) => new()
        {
            Id = layer.Id,
            Name = layer.Name,
            IsVisible = layer.IsVisible,
            Opacity = layer.Opacity,
            BlendMode = layer.BlendMode,
            PrecisionMode = layer.PrecisionMode,
            FormulaSource = layer.FormulaSource,
            CenterX = layer.CenterX,
            CenterY = layer.CenterY,
            Zoom = layer.Zoom,
            IsLinkedToMasterCamera = layer.IsLinkedToMasterCamera,
            PaletteFrequency = layer.PaletteFrequency,
            PalettePhase = layer.PalettePhase,
            Parameters = layer.Parameters.ToDictionary(
                value => value.Name,
                value => value.Value,
                StringComparer.OrdinalIgnoreCase)
        };

        public StudioLayer Restore()
        {
            var layer = new StudioLayer
            {
                Id = Id == Guid.Empty ? Guid.NewGuid() : Id,
                Name = Name,
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
            StudioCompiledFormula compiled = StudioFormulaCompiler.Compile(layer.FormulaSource);
            layer.SynchronizeParameters(compiled);
            foreach (StudioParameterValue parameter in layer.Parameters)
            {
                if (Parameters.TryGetValue(parameter.Name, out string? value))
                    parameter.Value = value;
            }
            return layer;
        }
    }
}
