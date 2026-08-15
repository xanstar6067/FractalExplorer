using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FractalExplorerWPF.Infrastructure.Serialization;

public sealed class NumericsComplexJsonConverter : JsonConverter<Complex>
{
    public override Complex Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Ожидался объект комплексного числа.");

        double real = 0;
        double imaginary = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return new Complex(real, imaginary);
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            string? propertyName = reader.GetString();
            if (!reader.Read()) throw new JsonException("Неожиданный конец комплексного числа.");
            if (string.Equals(propertyName, "Real", StringComparison.OrdinalIgnoreCase)) real = reader.GetDouble();
            else if (string.Equals(propertyName, "Imaginary", StringComparison.OrdinalIgnoreCase)) imaginary = reader.GetDouble();
            else reader.Skip();
        }

        throw new JsonException("Не найден конец объекта комплексного числа.");
    }

    public override void Write(Utf8JsonWriter writer, Complex value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("Real", value.Real);
        writer.WriteNumber("Imaginary", value.Imaginary);
        writer.WriteEndObject();
    }
}
