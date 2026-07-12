using System.IO;
using System.Text.Json;
using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Infrastructure;

public sealed class IfsSaveStore
{
    private string FilePath=>Path.Combine(AppPaths.SavesDirectory,"IFS_saves.json");
    public List<IfsState> Load()=>!File.Exists(FilePath)?[]:JsonSerializer.Deserialize<List<IfsState>>(File.ReadAllText(FilePath),JsonOptionsFactory.Create())??[];
    public void Save(IEnumerable<IfsState> states){AppPaths.EnsureSavesDirectory();File.WriteAllText(FilePath,JsonSerializer.Serialize(states,JsonOptionsFactory.Create()));}
}
