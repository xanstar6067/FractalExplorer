using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class SerpinskySaveStore
{
    private const string FileName = "Serpinsky_saves.json";

    public string FilePath => Path.Combine(AppPaths.SavesDirectory, FileName);

    public List<SerpinskySaveState> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        string json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<List<SerpinskySaveState>>(
            json,
            JsonOptionsFactory.Create()) ?? [];
    }

    public void Save(IReadOnlyCollection<SerpinskySaveState> states)
    {
        AppPaths.EnsureSavesDirectory();
        File.WriteAllText(
            FilePath,
            JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
    }
}
