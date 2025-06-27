using System.Text.Json;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;


namespace FractalExplorer.Utilities.SaveIO
{
    /// <summary>
    /// Управляет загрузкой и сохранением состояний фракталов из файлов и в файлы.
    /// Этот статический класс предоставляет служебные методы для взаимодействия с файловой системой
    /// для сохранения и извлечения данных конфигурации фракталов.
    /// </summary>
    public static class SaveFileManager
    {
        /// <summary>
        /// Формирует полный путь к файлу сохранения или загрузки на основе идентификатора типа фрактала.
        /// </summary>
        /// <param name="fractalTypeIdentifier">Уникальный идентификатор типа фрактала (например, "Mandelbrot", "Julia").</param>
        /// <returns>Полный путь к файлу сохранения.</returns>
        private static string GetSaveFilePath(string fractalTypeIdentifier)
        {
            // Сохранения хранятся в поддиректории 'Saves' в директории запуска приложения,
            // чтобы поддерживать организацию и предотвратить засорение корневой директории приложения.
            string savesDirectory = Path.Combine(Application.StartupPath, "Saves");
            if (!Directory.Exists(savesDirectory))
            {
                Directory.CreateDirectory(savesDirectory);
            }

            return Path.Combine(savesDirectory, $"{fractalTypeIdentifier}_saves.json");
        }

        /// <summary>
        /// Настраивает и возвращает <see cref="JsonSerializerOptions"/> для единообразной сериализации и десериализации JSON.
        /// </summary>
        /// <returns>Экземпляр <see cref="JsonSerializerOptions"/>.</returns>
        private static JsonSerializerOptions GetJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                // Форматирует вывод JSON с отступами для удобочитаемости, что помогает при отладке и ручном просмотре.
                WriteIndented = true
            };

            // Добавляет пользовательские конвертеры, необходимые для определенных типов данных, таких как Color и ComplexDecimal,
            // чтобы гарантировать их правильную сериализацию и десериализацию библиотекой System.Text.Json.
            options.Converters.Add(new JsonConverters.JsonColorConverter());
            options.Converters.Add(new Utilities.JsonConverters.JsonComplexDecimalConverter());

            return options;
        }

        /// <summary>
        /// Загружает список состояний сохранения фрактала из JSON-файла, специфичного для данного идентификатора типа фрактала.
        /// </summary>
        /// <typeparam name="T">Конкретный тип <see cref="FractalSaveStateBase"/> для загрузки.</typeparam>
        /// <param name="fractalTypeIdentifier">Уникальный идентификатор типа фрактала (например, "Mandelbrot", "Julia").</param>
        /// <returns>Список загруженных состояний сохранения или пустой список, если файл не существует или произошла ошибка во время загрузки.</returns>
        public static List<T> LoadSaves<T>(string fractalTypeIdentifier) where T : FractalSaveStateBase
        {
            string filePath = GetSaveFilePath(fractalTypeIdentifier);
            if (!File.Exists(filePath))
            {
                // Возвращает пустой список, если для данного идентификатора не существует файла сохранения,
                // предотвращая исключения "файл не найден" и указывая, что нет доступных для загрузки состояний.
                return new List<T>();
            }

            try
            {
                string json = File.ReadAllText(filePath);
                // Десериализует содержимое JSON в список указанного типа состояния сохранения.
                // Использует оператор объединения с null, чтобы убедиться, что пустой список возвращается,
                // если десериализация приводит к null (например, из-за пустого или некорректного JSON-файла).
                return JsonSerializer.Deserialize<List<T>>(json, GetJsonOptions()) ?? new List<T>();
            }
            catch (Exception ex)
            {
                // Отображает удобное для пользователя сообщение об ошибке, если исключение возникает
                // во время чтения файла или десериализации JSON. Это помогает информировать пользователя
                // о сбое без краха приложения.
                MessageBox.Show($"Ошибка загрузки сохранений для '{fractalTypeIdentifier}': {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Возвращает пустой список, чтобы указать, что состояния не могут быть загружены из-за ошибки.
                return new List<T>();
            }
        }

        /// <summary>
        /// Сохраняет список состояний сохранения фрактала в JSON-файл, специфичный для данного идентификатора типа фрактала.
        /// </summary>
        /// <typeparam name="T">Конкретный тип <see cref="FractalSaveStateBase"/> для сохранения.</typeparam>
        /// <param name="fractalTypeIdentifier">Уникальный идентификатор типа фрактала (например, "Mandelbrot", "Julia").</param>
        /// <param name="states">Список состояний сохранения для сохранения.</param>
        public static void SaveSaves<T>(string fractalTypeIdentifier, List<T> states) where T : FractalSaveStateBase
        {
            string filePath = GetSaveFilePath(fractalTypeIdentifier);
            try
            {
                // Сериализует список состояний сохранения в строку JSON, используя настроенные параметры.
                // Это подготавливает объекты в памяти для постоянного хранения.
                string json = JsonSerializer.Serialize(states, GetJsonOptions());
                // Записывает строку JSON в указанный файл, перезаписывая любое существующее содержимое.
                // Это гарантирует, что сохраненные данные всегда являются самым актуальным состоянием.
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                // Отображает удобное для пользователя сообщение об ошибке, если исключение возникает
                // во время сериализации JSON или записи файла. Это предотвращает скрытые сбои
                // и предоставляет обратную связь пользователю.
                MessageBox.Show($"Ошибка сохранения для '{fractalTypeIdentifier}': {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
