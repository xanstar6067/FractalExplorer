using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class NewtonSaveStore
{
    private string FilePath => Path.Combine(AppPaths.SavesDirectory, "NewtonPools_saves.json");

    public List<NewtonState> Load() => !File.Exists(FilePath)
        ? []
        : JsonSerializer.Deserialize<List<NewtonState>>(File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];

    public void Save(IEnumerable<NewtonState> states)
    {
        AppPaths.EnsureSavesDirectory();
        string temporaryPath = FilePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
        File.Move(temporaryPath, FilePath, true);
    }
}
