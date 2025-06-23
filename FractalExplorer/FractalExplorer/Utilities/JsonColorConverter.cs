using System;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FractalExplorer.Core
{
    /// <summary>
    /// Кастомный конвертер для <see cref="System.Drawing.Color"/>, который позволяет
    /// корректно сериализовать и десериализовать цвета в JSON.
    /// Цвет сохраняется в формате #AARRGGBB.
    /// </summary>
    public class JsonColorConverter : JsonConverter<Color>
    {
        #region Read Method

        /// <summary>
        /// Читает цвет из JSON. Ожидает строку в формате #AARRGGBB.
        /// </summary>
        /// <param name="reader">Объект <see cref="Utf8JsonReader"/> для чтения JSON.</param>
        /// <param name="typeToConvert">Тип, который требуется преобразовать (ожидается <see cref="System.Drawing.Color"/>).</param>
        /// <param name="options">Параметры сериализации JSON.</param>
        /// <returns>Десериализованный объект <see cref="System.Drawing.Color"/>.</returns>
        /// <exception cref="JsonException">Выбрасывается, если в JSON ожидалась строка, но найден другой тип токена.</exception>
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Проверяем, что в JSON действительно строка
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Ожидалась строка для значения Color.");
            }

            string colorHex = reader.GetString();

            // Проверяем формат строки: должна начинаться с '#' и иметь длину 9 символов (#AARRGGBB)
            if (string.IsNullOrEmpty(colorHex) || colorHex[0] != '#' || colorHex.Length != 9)
            {
                // Если формат неверный, возвращаем цвет по умолчанию для избежания падения
                return Color.Black;
            }

            try
            {
                // Парсим значения ARGB из шестнадцатеричной строки
                int alpha = int.Parse(colorHex.Substring(1, 2), NumberStyles.HexNumber);
                int red = int.Parse(colorHex.Substring(3, 2), NumberStyles.HexNumber);
                int green = int.Parse(colorHex.Substring(5, 2), NumberStyles.HexNumber);
                int blue = int.Parse(colorHex.Substring(7, 2), NumberStyles.HexNumber);

                return Color.FromArgb(alpha, red, green, blue);
            }
            catch (Exception ex)
            {
                // В случае ошибки парсинга (например, некорректные шестнадцатеричные символы),
                // возвращаем яркий цвет (Magenta), чтобы сразу заметить проблему.
                Console.WriteLine($"Ошибка парсинга цвета: {ex.Message}");
                return Color.Magenta;
            }
        }

        #endregion

        #region Write Method

        /// <summary>
        /// Записывает цвет в JSON в виде строки формата #AARRGGBB.
        /// </summary>
        /// <param name="writer">Объект <see cref="Utf8JsonWriter"/> для записи JSON.</param>
        /// <param name="value">Объект <see cref="System.Drawing.Color"/> для сериализации.</param>
        /// <param name="options">Параметры сериализации JSON.</param>
        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            // Формируем шестнадцатеричную строку цвета с альфа-каналом
            // "X2" гарантирует, что каждое значение будет представлено двумя символами (например, "0F" вместо "F")
            string hexColor = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
            writer.WriteStringValue(hexColor);
        }

        #endregion
    }
}