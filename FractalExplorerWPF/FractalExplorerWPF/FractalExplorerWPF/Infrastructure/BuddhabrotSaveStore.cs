using System.IO; using System.Text.Json; using FractalExplorerWPF.Models;
namespace FractalExplorerWPF.Infrastructure;
public sealed class BuddhabrotSaveStore
{
    private string FilePath => Path.Combine(AppPaths.SavesDirectory, "Buddhabrot_saves.json");
    public List<BuddhabrotState> Load() => !File.Exists(FilePath) ? [] : JsonSerializer.Deserialize<List<BuddhabrotState>>(File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];
    public void Save(IEnumerable<BuddhabrotState> states) { AppPaths.EnsureSavesDirectory(); File.WriteAllText(FilePath, JsonSerializer.Serialize(states, JsonOptionsFactory.Create())); }
}
