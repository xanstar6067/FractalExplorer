using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Infrastructure;

public sealed class WpfColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (value is null || value.Length != 9 || value[0] != '#')
        {
            throw new JsonException("Ожидался цвет в формате #AARRGGBB.");
        }

        return Color.FromArgb(
            byte.Parse(value.AsSpan(1, 2), NumberStyles.HexNumber),
            byte.Parse(value.AsSpan(3, 2), NumberStyles.HexNumber),
            byte.Parse(value.AsSpan(5, 2), NumberStyles.HexNumber),
            byte.Parse(value.AsSpan(7, 2), NumberStyles.HexNumber));
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) =>
        writer.WriteStringValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
}
