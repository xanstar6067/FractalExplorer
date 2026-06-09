using FractalExplorer.Utilities.JsonConverters;
using System.Text.Json;

namespace FractalExplorer.Utilities.SaveIO.ColorPalettes
{
    /// <summary>
    /// Представляет цветовую палитру для фрактала Серпинского,
    /// содержащую цвета для фрактала и фона, а также метаданные.
    /// </summary>
    public class SerpinskyColorPalette
    {
        /// <summary>
        /// Получает или задает имя цветовой палитры.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Получает или задает основной цвет фрактала. По умолчанию - черный.
        /// </summary>
        public Color FractalColor { get; set; } = Color.Black;

        /// <summary>
        /// Получает или задает цвет фона фрактала. По умолчанию - белый.
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.White;

        /// <summary>
        /// Получает или задает значение, указывающее, является ли палитра встроенной (предустановленной)
        /// и не подлежащей сохранению или удалению пользователем.
        /// Пользовательские палитры не помечаются этим флагом, чтобы их можно было фильтровать для сохранения.
        /// </summary>
        public bool IsBuiltIn { get; set; } = false;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SerpinskyColorPalette"/>.
        /// </summary>
        public SerpinskyColorPalette() { }
    }

    /// <summary>
    /// Управляет загрузкой, сохранением и выбором цветовых палитр для фрактала Серпинского.
    /// Обеспечивает доступ к встроенным и пользовательским палитрам.
    /// </summary>
    public class SerpinskyPaletteManager
    {
        private const string PALETTE_FILE = "serpinsky_palettes.json";

        /// <summary>
        /// Получает список всех доступных цветовых палитр, включая встроенные и пользовательские.
        /// </summary>
        public List<SerpinskyColorPalette> Palettes { get; private set; }

        /// <summary>
        /// Получает или задает активную (текущую) цветовую палитру, которая будет использоваться для рендеринга.
        /// </summary>
        public SerpinskyColorPalette ActivePalette { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SerpinskyPaletteManager"/>.
        /// Загружает встроенные палитры и пытается загрузить пользовательские палитры из файла.
        /// </summary>
        public SerpinskyPaletteManager()
        {
            Palettes = new List<SerpinskyColorPalette>();
            LoadPalettes();
            // Устанавливаем "Классический Ч/Б" как палитру по умолчанию; если ее нет, выбираем первую доступную.
            ActivePalette = Palettes.FirstOrDefault(p => p.Name == "Классический Ч/Б") ?? Palettes.FirstOrDefault();
        }

        /// <summary>
        /// Загружает встроенные и пользовательские цветовые палитры.
        /// Пользовательские палитры считываются из JSON-файла в подпапке "Saves".
        /// </summary>
        private void LoadPalettes()
        {
            // Добавляем набор предустановленных палитр, чтобы пользователь всегда имел базовый выбор.
            Palettes.Add(new SerpinskyColorPalette { Name = "Классический Ч/Б", FractalColor = Color.Black, BackgroundColor = Color.White, IsBuiltIn = true });
            Palettes.Add(new SerpinskyColorPalette { Name = "Инверсия", FractalColor = Color.White, BackgroundColor = Color.Black, IsBuiltIn = true });
            Palettes.Add(new SerpinskyColorPalette { Name = "Оттенки серого", FractalColor = Color.FromArgb(50, 50, 50), BackgroundColor = Color.White, IsBuiltIn = true });
            Palettes.Add(new SerpinskyColorPalette { Name = "Огонь и ночь", FractalColor = Color.OrangeRed, BackgroundColor = Color.FromArgb(10, 0, 20), IsBuiltIn = true });
            Palettes.Add(new SerpinskyColorPalette { Name = "Глубокий океан", FractalColor = Color.Aqua, BackgroundColor = Color.DarkSlateBlue, IsBuiltIn = true });

            // Определяем путь к директории для сохранения файлов, чтобы обеспечить их изоляцию.
            string savesDirectory = Path.Combine(Application.StartupPath, "Saves");
            if (!Directory.Exists(savesDirectory))
            {
                // Если директория сохранений не существует, значит, пользовательских палитр быть не может.
                return;
            }
            string filePath = Path.Combine(savesDirectory, PALETTE_FILE);

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new JsonColorConverter());
                    var customPalettes = JsonSerializer.Deserialize<List<SerpinskyColorPalette>>(json, options);
                    if (customPalettes != null)
                    {
                        // Добавляем загруженные пользовательские палитры к встроенным.
                        Palettes.AddRange(customPalettes);
                    }
                }
                catch (Exception ex)
                {
                    // Сообщаем пользователю об ошибке загрузки, но позволяем приложению продолжить работу
                    // с доступными встроенными палитрами.
                    MessageBox.Show($"Не удалось загрузить палитры для Серпинского: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Сохраняет все пользовательские (не встроенные) цветовые палитры в JSON-файл.
        /// Файл сохраняется в подпапке "Saves", чтобы обеспечить их персистентность между сессиями.
        /// </summary>
        public void SaveCustomPalettes()
        {
            try
            {
                // Фильтруем палитры, чтобы сохранить только те, которые не являются встроенными.
                var customPalettes = Palettes.Where(p => !p.IsBuiltIn).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true }; // Включаем форматирование для читаемости файла.
                options.Converters.Add(new JsonColorConverter());
                string json = JsonSerializer.Serialize(customPalettes, options);

                // Создаем папку "Saves", если она не существует, чтобы избежать ошибок при сохранении.
                string savesDirectory = Path.Combine(Application.StartupPath, "Saves");
                Directory.CreateDirectory(savesDirectory);

                // Формируем полный путь к файлу для сохранения.
                string filePath = Path.Combine(savesDirectory, PALETTE_FILE);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                // Сообщаем пользователю об ошибке сохранения, если возникли проблемы с доступом к файлу или сериализацией.
                MessageBox.Show($"Не удалось сохранить палитры для Серпинского: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}