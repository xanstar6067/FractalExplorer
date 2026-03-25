using System.Diagnostics;
using System.Text.Json;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;

namespace FractalExplorer.Utilities.SaveIO
{
    /// <summary>
    /// Управляет чтением и записью sidecar-метаданных (*.meta.json) для файлов сохранений.
    /// </summary>
    public static class SaveMetadataManager
    {
        private const string SmoothColoringProperty = "UseSmoothColoring";
        private const string ColoringModeProperty = "ColoringMode";

        private sealed class SaveMetadataFile
        {
            public int Version { get; set; } = 1;
            public List<SaveMetadataEntry> Entries { get; set; } = new();
        }

        private sealed class SaveMetadataEntry
        {
            public string SaveName { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string ColoringMode { get; set; } = string.Empty;
        }

        private enum ColoringMode
        {
            Discrete,
            Smooth
        }

        public static void WriteMetadata<T>(string saveFilePath, IReadOnlyCollection<T> states) where T : FractalSaveStateBase
        {
            string metadataPath = GetMetadataPath(saveFilePath);

            var file = new SaveMetadataFile
            {
                Entries = states.Select(state => new SaveMetadataEntry
                {
                    SaveName = state.SaveName,
                    Timestamp = state.Timestamp,
                    ColoringMode = ResolveColoringModeFromPreview(state.PreviewParametersJson).ToString()
                }).ToList()
            };

            string json = JsonSerializer.Serialize(file, GetJsonOptions());
            File.WriteAllText(metadataPath, json);
        }

        public static void ApplyMetadata<T>(string saveFilePath, List<T> states) where T : FractalSaveStateBase
        {
            string metadataPath = GetMetadataPath(saveFilePath);
            bool hasSaveFile = File.Exists(saveFilePath);
            bool hasMetadataFile = File.Exists(metadataPath);

            if (hasSaveFile && !hasMetadataFile && states.Count > 0)
            {
                Debug.WriteLine($"[SaveMetadataManager] Обнаружен файл сохранений без sidecar: '{saveFilePath}'.");
                return;
            }

            if (!hasSaveFile && hasMetadataFile)
            {
                Debug.WriteLine($"[SaveMetadataManager] Обнаружен sidecar без файла сохранений: '{metadataPath}'.");
                return;
            }

            if (!hasMetadataFile)
            {
                return;
            }

            SaveMetadataFile? metadata;
            try
            {
                string metadataJson = File.ReadAllText(metadataPath);
                metadata = JsonSerializer.Deserialize<SaveMetadataFile>(metadataJson, GetJsonOptions());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SaveMetadataManager] Ошибка чтения sidecar '{metadataPath}': {ex.Message}");
                return;
            }

            if (metadata?.Entries is null)
            {
                Debug.WriteLine($"[SaveMetadataManager] Пустой или некорректный sidecar: '{metadataPath}'.");
                return;
            }

            var byKey = new Dictionary<string, SaveMetadataEntry>();
            foreach (var entry in metadata.Entries)
            {
                string key = CreateKey(entry);
                if (byKey.ContainsKey(key))
                {
                    Debug.WriteLine($"[SaveMetadataManager] Дублирующая запись в sidecar '{metadataPath}' для '{entry.SaveName}' ({entry.Timestamp:O}). Будет использована последняя.");
                }

                byKey[key] = entry;
            }
            var stateKeys = new HashSet<string>(states.Select(CreateKey));

            foreach (var state in states)
            {
                string key = CreateKey(state);
                if (!byKey.TryGetValue(key, out var entry))
                {
                    Debug.WriteLine($"[SaveMetadataManager] Для сохранения '{state.SaveName}' ({state.Timestamp:O}) отсутствует запись в sidecar '{metadataPath}'.");
                    continue;
                }

                bool useSmooth = ParseColoringMode(entry.ColoringMode) == ColoringMode.Smooth;
                state.PreviewParametersJson = UpsertColoringMetadata(state.PreviewParametersJson, useSmooth, entry.ColoringMode);
            }

            foreach (var entry in metadata.Entries)
            {
                string key = CreateKey(entry);
                if (!stateKeys.Contains(key))
                {
                    Debug.WriteLine($"[SaveMetadataManager] В sidecar '{metadataPath}' найдена лишняя запись '{entry.SaveName}' ({entry.Timestamp:O}) без соответствующего сохранения.");
                }
            }
        }

        private static JsonSerializerOptions GetJsonOptions() => new()
        {
            WriteIndented = true
        };

        private static string GetMetadataPath(string saveFilePath)
        {
            string directory = Path.GetDirectoryName(saveFilePath) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(saveFilePath);
            return Path.Combine(directory, $"{fileNameWithoutExtension}.meta.json");
        }

        private static ColoringMode ResolveColoringModeFromPreview(string? previewParametersJson)
        {
            if (string.IsNullOrWhiteSpace(previewParametersJson))
            {
                return ColoringMode.Discrete;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(previewParametersJson);
                if (document.RootElement.TryGetProperty(ColoringModeProperty, out JsonElement modeElement)
                    && modeElement.ValueKind == JsonValueKind.String)
                {
                    return ParseColoringMode(modeElement.GetString());
                }

                if (document.RootElement.TryGetProperty(SmoothColoringProperty, out JsonElement smoothElement)
                    && (smoothElement.ValueKind == JsonValueKind.True || smoothElement.ValueKind == JsonValueKind.False))
                {
                    return smoothElement.GetBoolean() ? ColoringMode.Smooth : ColoringMode.Discrete;
                }
            }
            catch
            {
                // Игнорируем ошибку разбора и используем дефолтный режим.
            }

            return ColoringMode.Discrete;
        }

        private static ColoringMode ParseColoringMode(string? rawValue)
        {
            return Enum.TryParse<ColoringMode>(rawValue, ignoreCase: true, out var mode)
                ? mode
                : ColoringMode.Discrete;
        }

        private static string UpsertColoringMetadata(string? previewParametersJson, bool useSmoothColoring, string coloringMode)
        {
            string modeToStore = ParseColoringMode(coloringMode).ToString();

            if (string.IsNullOrWhiteSpace(previewParametersJson))
            {
                return JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    [SmoothColoringProperty] = useSmoothColoring,
                    [ColoringModeProperty] = modeToStore
                });
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(previewParametersJson);
                var map = new Dictionary<string, JsonElement>();
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    map[property.Name] = property.Value.Clone();
                }

                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    foreach (var kv in map)
                    {
                        if (kv.Key.Equals(SmoothColoringProperty, StringComparison.Ordinal) ||
                            kv.Key.Equals(ColoringModeProperty, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        writer.WritePropertyName(kv.Key);
                        kv.Value.WriteTo(writer);
                    }

                    writer.WriteBoolean(SmoothColoringProperty, useSmoothColoring);
                    writer.WriteString(ColoringModeProperty, modeToStore);
                    writer.WriteEndObject();
                }

                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
            catch
            {
                return JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    [SmoothColoringProperty] = useSmoothColoring,
                    [ColoringModeProperty] = modeToStore
                });
            }
        }

        private static string CreateKey(FractalSaveStateBase state)
            => $"{state.SaveName}::{state.Timestamp:O}";

        private static string CreateKey(SaveMetadataEntry entry)
            => $"{entry.SaveName}::{entry.Timestamp:O}";
    }
}
