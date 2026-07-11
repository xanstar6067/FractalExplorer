using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class PhoenixSaveStore
{
    private string FilePath => Path.Combine(AppPaths.SavesDirectory, "Phoenix_saves.json");
    public List<PhoenixState> Load() => !File.Exists(FilePath) ? [] :
        JsonSerializer.Deserialize<List<PhoenixState>>(File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];
    public void Save(IEnumerable<PhoenixState> states)
    {
        AppPaths.EnsureSavesDirectory();
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
        File.Move(temporary, FilePath, true);
    }
}
