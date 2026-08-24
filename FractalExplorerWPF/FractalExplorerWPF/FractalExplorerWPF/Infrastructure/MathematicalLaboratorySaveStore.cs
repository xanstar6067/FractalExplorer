using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class MathematicalLaboratorySaveStore(MathematicalLaboratoryKind kind)
{
    private string FilePath => Path.Combine(
        AppPaths.SavesDirectory, $"MathematicalLaboratory_{kind}_saves.json");

    public List<MathematicalLaboratoryState> Load() => !File.Exists(FilePath)
        ? []
        : JsonSerializer.Deserialize<List<MathematicalLaboratoryState>>(
            File.ReadAllText(FilePath), JsonOptionsFactory.Create())?
            .Where(state => state.Kind == kind).ToList() ?? [];

    public void Save(IEnumerable<MathematicalLaboratoryState> states)
    {
        AppPaths.EnsureSavesDirectory();
        File.WriteAllText(FilePath,
            JsonSerializer.Serialize(states.Where(state => state.Kind == kind), JsonOptionsFactory.Create()));
    }
}
