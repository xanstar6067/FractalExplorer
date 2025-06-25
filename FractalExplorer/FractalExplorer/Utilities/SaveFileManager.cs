using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using FractalExplorer.Utilities;
using FractalExplorer.Core; // Для JsonColorConverter, JsonComplexDecimalConverter


namespace FractalExplorer.Utilities
{
    public static class SaveFileManager
    {
        private static string GetSaveFilePath(string fractalTypeIdentifier)
        {
            // Сохраняем в папку Saves в директории приложения
            string savesDirectory = Path.Combine(Application.StartupPath, "Saves");
            if (!Directory.Exists(savesDirectory))
            {
                Directory.CreateDirectory(savesDirectory);
            }
            return Path.Combine(savesDirectory, $"{fractalTypeIdentifier}_saves.json");
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                // Это важно для полиморфной десериализации!
                // Необходимо зарегистрировать все типы, которые могут быть в списке.
                // Но т.к. мы грузим List<T>, где T - конкретный тип, это может быть не нужно здесь.
                // Однако, если бы мы грузили List<FractalSaveStateBase>, это было бы обязательно.
                // Для безопасности оставим, но с учетом List<T> это обычно работает.
                // Вместо этого, лучше явно указывать тип при десериализации List<MyConcreteType>.
            };
            options.Converters.Add(new JsonColorConverter());
            options.Converters.Add(new JsonComplexDecimalConverter());
            return options;
        }

        public static List<T> LoadSaves<T>(string fractalTypeIdentifier) where T : FractalSaveStateBase
        {
            string filePath = GetSaveFilePath(fractalTypeIdentifier);
            if (!File.Exists(filePath))
            {
                return new List<T>();
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<T>>(json, GetJsonOptions()) ?? new List<T>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сохранений для '{fractalTypeIdentifier}': {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<T>();
            }
        }

        public static void SaveSaves<T>(string fractalTypeIdentifier, List<T> states) where T : FractalSaveStateBase
        {
            string filePath = GetSaveFilePath(fractalTypeIdentifier);
            try
            {
                string json = JsonSerializer.Serialize(states, GetJsonOptions());
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения для '{fractalTypeIdentifier}': {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
