using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class DomainColoringSaveStore
{
    private string FilePath => Path.Combine(AppPaths.SavesDirectory, "DomainColoring_saves.json");

    public List<DomainColoringState> Load() => !File.Exists(FilePath)
        ? []
        : JsonSerializer.Deserialize<List<DomainColoringState>>(
            File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];

    public void Save(IEnumerable<DomainColoringState> states)
    {
        AppPaths.EnsureSavesDirectory();
        string temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
        File.Move(temporaryPath, FilePath, true);
    }
}

