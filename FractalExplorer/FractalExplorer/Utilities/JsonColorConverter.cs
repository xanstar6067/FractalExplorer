using System;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FractalExplorer.Core // Убедитесь, что пространство имен совпадает с вашим
{
    /// <summary>
    /// Кастомный конвертер для System.Drawing.Color, который позволяет
    /// корректно сериализовать и десериализовать цвета в JSON.
    /// Цвет сохраняется в формате #AARRGGBB.
    /// </summary>
    public class JsonColorConverter : JsonConverter<Color>
    {
        /// <summary>
        /// Читает цвет из JSON. Ожидает строку в формате #AARRGGBB.
        /// </summary>
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Проверяем, что в JSON действительно строка
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Ожидалась строка для значения Color.");
            }

            string colorHex = reader.GetString();

            // Проверяем формат строки
            if (string.IsNullOrEmpty(colorHex) || colorHex[0] != '#' || colorHex.Length != 9)
            {
                // Если формат неверный, возвращаем цвет по умолчанию, чтобы избежать падения
                return Color.Black;
            }

            try
            {
                // Парсим значения ARGB из строки
                int a = int.Parse(colorHex.Substring(1, 2), NumberStyles.HexNumber);
                int r = int.Parse(colorHex.Substring(3, 2), NumberStyles.HexNumber);
                int g = int.Parse(colorHex.Substring(5, 2), NumberStyles.HexNumber);
                int b = int.Parse(colorHex.Substring(7, 2), NumberStyles.HexNumber);

                return Color.FromArgb(a, r, g, b);
            }
            catch (Exception ex)
            {
                // В случае ошибки парсинга, возвращаем цвет по умолчанию
                Console.WriteLine($"Ошибка парсинга цвета: {ex.Message}");
                return Color.Magenta; // Яркий цвет, чтобы сразу заметить ошибку
            }
        }

        /// <summary>
        /// Записывает цвет в JSON в виде строки формата #AARRGGBB.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            // Формируем строку и записываем ее в JSON
            // ToString("X2") гарантирует, что у нас всегда будет два символа (например, "0F" вместо "F")
            string hexColor = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
            writer.WriteStringValue(hexColor);
        }
    }
}