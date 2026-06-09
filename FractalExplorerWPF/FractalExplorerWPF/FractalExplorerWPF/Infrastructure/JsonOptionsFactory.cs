using System.Text.Json;

namespace FractalExplorerWPF.Infrastructure;

public static class JsonOptionsFactory
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new WpfColorJsonConverter());
        return options;
    }
}
