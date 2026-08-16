using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class InverseCollatzSaveStore
{
    private string FilePath => Path.Combine(AppPaths.SavesDirectory, "InverseCollatzTree_saves.json");

    public List<InverseCollatzState> Load() => !File.Exists(FilePath)
        ? []
        : JsonSerializer.Deserialize<List<InverseCollatzState>>(
            File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];

    public void Save(IEnumerable<InverseCollatzState> states)
    {
        AppPaths.EnsureSavesDirectory();
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
        File.Move(temporary, FilePath, true);
    }
}
