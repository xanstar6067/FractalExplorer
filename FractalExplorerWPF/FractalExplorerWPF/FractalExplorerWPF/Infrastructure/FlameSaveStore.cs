using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class FlameSaveStore
{
    private string FilePath => Path.Combine(AppPaths.SavesDirectory, "Flame_saves.json");
    public List<FlameState> Load() => !File.Exists(FilePath) ? [] : JsonSerializer.Deserialize<List<FlameState>>(File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];
    public void Save(IEnumerable<FlameState> states)
    {
        AppPaths.EnsureSavesDirectory();
        File.WriteAllText(FilePath, JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
    }
}
