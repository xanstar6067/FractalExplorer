namespace FractalExplorer.Utilities.Theme
{
    public sealed class ThemeCatalog
    {
        private static readonly Dictionary<string, string> LegacyThemeNameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dark"] = BuiltInThemeProvider.DefaultThemeId,
            ["Light"] = "light"
        };

        private readonly List<ThemeDefinition> _builtInThemes;
        private readonly Dictionary<string, ThemeDefinition> _builtInThemesById;
        private readonly Dictionary<string, ThemeDefinition> _customThemesById = new(StringComparer.OrdinalIgnoreCase);

        public ThemeCatalog()
        {
            _builtInThemes = BuiltInThemeProvider.GetThemes().ToList();
            _builtInThemesById = _builtInThemes.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

            ThemeOperationResult<IReadOnlyList<ThemeDefinition>> loadResult = ThemePersistenceService.LoadCustomThemes();
            if (!loadResult.IsSuccess || loadResult.Value is null)
            {
                return;
            }

            foreach (ThemeDefinition custom in loadResult.Value)
            {
                if (!string.IsNullOrWhiteSpace(custom.Id) && !_builtInThemesById.ContainsKey(custom.Id))
                {
                    _customThemesById[custom.Id] = custom.CloneWith(custom.Id, custom.DisplayName, false);
                }
            }
        }

        public string DefaultThemeId => BuiltInThemeProvider.DefaultThemeId;

        public IReadOnlyList<ThemeDefinition> GetAllThemes()
        {
            List<ThemeDefinition> result = new(_builtInThemes.Count + _customThemesById.Count);
            result.AddRange(_builtInThemes);
            result.AddRange(_customThemesById.Values.OrderBy(theme => theme.DisplayName, StringComparer.CurrentCultureIgnoreCase));
            return result;
        }

        public IEnumerable<ThemeDefinition> GetCustomThemes() => _customThemesById.Values;

        public bool TryGetTheme(string id, out ThemeDefinition theme)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (_customThemesById.TryGetValue(id, out theme)) return true;
                if (_builtInThemesById.TryGetValue(id, out theme)) return true;
                if (LegacyThemeNameMap.TryGetValue(id, out string mappedId)) return TryGetTheme(mappedId, out theme);
            }

            theme = _builtInThemesById[DefaultThemeId];
            return false;
        }

        public bool TryResolveThemeId(string? rawValue, out string resolvedThemeId)
        {
            if (!string.IsNullOrWhiteSpace(rawValue) && TryGetTheme(rawValue, out ThemeDefinition theme))
            {
                resolvedThemeId = theme.Id;
                return true;
            }

            resolvedThemeId = DefaultThemeId;
            return false;
        }

        public ThemeOperationResult AddOrUpdateCustomTheme(ThemeDefinition theme)
        {
            if (theme is null) return ThemeOperationResult.Failure("Тема не задана.");
            if (string.IsNullOrWhiteSpace(theme.Id)) return ThemeOperationResult.Failure("Идентификатор темы не может быть пустым.");
            if (_builtInThemesById.ContainsKey(theme.Id)) return ThemeOperationResult.Failure("Нельзя изменить встроенную тему.");

            _customThemesById[theme.Id] = theme.CloneWith(theme.Id, theme.DisplayName, false);
            return ThemePersistenceService.SaveCustomThemes(_customThemesById.Values);
        }

        public ThemeOperationResult<bool> RemoveCustomTheme(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_customThemesById.Remove(id))
            {
                return ThemeOperationResult<bool>.Success(false);
            }

            ThemeOperationResult saveResult = ThemePersistenceService.SaveCustomThemes(_customThemesById.Values);
            return saveResult.IsSuccess
                ? ThemeOperationResult<bool>.Success(true)
                : ThemeOperationResult<bool>.Failure(saveResult.ErrorMessage ?? "Ошибка удаления темы.", saveResult.Exception);
        }

        public ThemeOperationResult<ThemeDefinition> DuplicateTheme(string sourceId, string newId, string newDisplayName)
        {
            if (!TryGetTheme(sourceId, out ThemeDefinition sourceTheme))
            {
                return ThemeOperationResult<ThemeDefinition>.Failure($"Theme with id '{sourceId}' was not found.");
            }

            ThemeDefinition duplicate = sourceTheme.CloneWith(newId, newDisplayName, false);
            ThemeOperationResult saveResult = AddOrUpdateCustomTheme(duplicate);
            return saveResult.IsSuccess
                ? ThemeOperationResult<ThemeDefinition>.Success(duplicate)
                : ThemeOperationResult<ThemeDefinition>.Failure(saveResult.ErrorMessage ?? "Ошибка дублирования.", saveResult.Exception);
        }
    }
}
