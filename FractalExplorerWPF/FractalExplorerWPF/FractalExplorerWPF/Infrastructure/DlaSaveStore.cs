using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class DlaSaveStore
{
    private string FilePath => Path.Combine(AppPaths.SavesDirectory, "DLA_saves.json");

    public List<DlaState> Load() => !File.Exists(FilePath)
        ? []
        : JsonSerializer.Deserialize<List<DlaState>>(
            File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];

    public void Save(IEnumerable<DlaState> states)
    {
        AppPaths.EnsureSavesDirectory();
        File.WriteAllText(FilePath, JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
    }
}
