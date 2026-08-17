using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class ApollonianSaveStore
{
    private string FilePath => Path.Combine(AppPaths.SavesDirectory, "Apollonian_saves.json");

    public List<ApollonianState> Load() => !File.Exists(FilePath)
        ? []
        : JsonSerializer.Deserialize<List<ApollonianState>>(
            File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];

    public void Save(IEnumerable<ApollonianState> states)
    {
        AppPaths.EnsureSavesDirectory();
        File.WriteAllText(FilePath, JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
    }
}
