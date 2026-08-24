using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class GrayScottSaveStore
{
    private string FilePath => Path.Combine(AppPaths.SavesDirectory, "GrayScott_saves.json");

    public List<GrayScottState> Load() => !File.Exists(FilePath)
        ? []
        : JsonSerializer.Deserialize<List<GrayScottState>>(
            File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];

    public void Save(IEnumerable<GrayScottState> states)
    {
        AppPaths.EnsureSavesDirectory();
        File.WriteAllText(FilePath, JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
    }
}
