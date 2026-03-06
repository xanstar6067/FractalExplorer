using System.Text.Json;
using FractalExplorer.Utilities.JsonConverters;

namespace FractalExplorer.Utilities.Theme
{
    public static class ThemeStorage
    {
        private static string GetThemeFilePath()
        {
            string savesDirectory = Path.Combine(Application.StartupPath, "Saves");
            if (!Directory.Exists(savesDirectory))
            {
                Directory.CreateDirectory(savesDirectory);
            }

            return Path.Combine(savesDirectory, "themes.json");
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = true
            };

            options.Converters.Add(new JsonColorConverter());
            return options;
        }

        public static List<ThemeDefinition> LoadCustomThemes()
        {
            string filePath = GetThemeFilePath();
            if (!File.Exists(filePath))
            {
                return new List<ThemeDefinition>();
            }

            string json = File.ReadAllText(filePath);
            List<ThemeDefinition> loadedThemes = JsonSerializer.Deserialize<List<ThemeDefinition>>(json, GetJsonOptions())
                                               ?? new List<ThemeDefinition>();

            return loadedThemes
                .Where(theme => theme is not null && !theme.IsBuiltIn)
                .Select(theme => theme.CloneWith(theme.Id, theme.DisplayName, false))
                .ToList();
        }

        public static void SaveCustomThemes(IEnumerable<ThemeDefinition> themes)
        {
            ArgumentNullException.ThrowIfNull(themes);

            string filePath = GetThemeFilePath();
            List<ThemeDefinition> customThemes = themes
                .Where(theme => theme is not null && !theme.IsBuiltIn)
                .Select(theme => theme.CloneWith(theme.Id, theme.DisplayName, false))
                .ToList();

            string json = JsonSerializer.Serialize(customThemes, GetJsonOptions());
            File.WriteAllText(filePath, json);
        }
    }
}
