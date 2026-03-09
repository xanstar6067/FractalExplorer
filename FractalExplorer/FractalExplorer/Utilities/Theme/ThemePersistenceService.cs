namespace FractalExplorer.Utilities.Theme
{
    public static class ThemePersistenceService
    {
        public static ThemeOperationResult<IReadOnlyList<ThemeDefinition>> LoadCustomThemes()
        {
            try
            {
                return ThemeOperationResult<IReadOnlyList<ThemeDefinition>>.Success(ThemeStorage.LoadCustomThemes());
            }
            catch (Exception ex)
            {
                return ThemeOperationResult<IReadOnlyList<ThemeDefinition>>.Failure("Ошибка загрузки пользовательских тем.", ex);
            }
        }

        public static ThemeOperationResult SaveCustomThemes(IEnumerable<ThemeDefinition> themes)
        {
            try
            {
                ThemeStorage.SaveCustomThemes(themes);
                return ThemeOperationResult.Success();
            }
            catch (Exception ex)
            {
                return ThemeOperationResult.Failure("Ошибка сохранения пользовательских тем.", ex);
            }
        }
    }
}
