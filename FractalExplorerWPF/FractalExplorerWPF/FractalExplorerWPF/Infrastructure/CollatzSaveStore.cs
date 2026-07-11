using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class CollatzSaveStore
{
    private string FilePath => Path.Combine(AppPaths.SavesDirectory, "Collatz_saves.json");

    public List<CollatzState> Load() => !File.Exists(FilePath)
        ? []
        : JsonSerializer.Deserialize<List<CollatzState>>(File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];

    public void Save(IEnumerable<CollatzState> states)
    {
        AppPaths.EnsureSavesDirectory();
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
        File.Move(temporary, FilePath, true);
    }
}
