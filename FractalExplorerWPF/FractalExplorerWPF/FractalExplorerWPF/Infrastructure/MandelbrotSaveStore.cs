using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class MandelbrotSaveStore(MandelbrotVariant variant)
{
    private string FilePath => Path.Combine(
        AppPaths.SavesDirectory, $"{MandelbrotVariantDefinition.For(variant).Identifier}_saves.json");

    public List<MandelbrotState> Load()
    {
        if (!File.Exists(FilePath)) return [];
        List<MandelbrotState> states = JsonSerializer.Deserialize<List<MandelbrotState>>(
            File.ReadAllText(FilePath), JsonOptionsFactory.Create()) ?? [];
        foreach (MandelbrotState state in states) state.Variant = variant;
        return states;
    }

    public void Save(IEnumerable<MandelbrotState> states)
    {
        AppPaths.EnsureSavesDirectory();
        string temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(states, JsonOptionsFactory.Create()));
        File.Move(temp, FilePath, true);
    }
}
