using FractalExplorer.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities
{
    public class JsonComplexDecimalConverter : JsonConverter<ComplexDecimal>
    {
        public override ComplexDecimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
            decimal real = 0; decimal imaginary = 0;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return new ComplexDecimal(real, imaginary);
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();
                    if (propertyName.Equals("Real", StringComparison.OrdinalIgnoreCase)) real = reader.GetDecimal();
                    else if (propertyName.Equals("Imaginary", StringComparison.OrdinalIgnoreCase)) imaginary = reader.GetDecimal();
                }
            }
            throw new JsonException("Error reading ComplexDecimal.");
        }

        public override void Write(Utf8JsonWriter writer, ComplexDecimal value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Real", value.Real);
            writer.WriteNumber("Imaginary", value.Imaginary);
            writer.WriteEndObject();
        }
    }
}
