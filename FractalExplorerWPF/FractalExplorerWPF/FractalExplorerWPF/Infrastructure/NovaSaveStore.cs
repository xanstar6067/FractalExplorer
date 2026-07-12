using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class NovaSaveStore(NovaVariant variant)
{
    private string FilePath => Path.Combine(AppPaths.SavesDirectory,
        variant == NovaVariant.Julia ? "NovaJulia_saves.json" : "NovaMandelbrot_saves.json");

    public List<NovaState> Load() => !File.Exists(FilePath) ? [] :
        JsonSerializer.Deserialize<List<NovaState>>(File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];

    public void Save(IEnumerable<NovaState> states)
    {
        AppPaths.EnsureSavesDirectory();
        string temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
        File.Move(temporary, FilePath, true);
    }
}
