using FractalExplorer.Utilities.JsonConverters;
using System.Text.Json;
using System.IO; // Добавлена необходимая директива для работы с путями

namespace FractalExplorer.Utilities.SaveIO.ColorPalettes
{
    /// <summary>
    /// Управляет коллекцией цветовых палитр, включая загрузку, сохранение и создание встроенных палитр.
    /// </summary>
    public class ColorPaletteMandelbrotFamily
    {
        private const string CUSTOM_PALETTES_FILE = "custom_palettes_mandelbrot.json";

        /// <summary>
        /// Список всех доступных палитр (встроенных и пользовательских).
        /// </summary>
        public List<PaletteManagerMandelbrotFamily> Palettes { get; private set; }

        /// <summary>
        /// Текущая активная палитра, используемая для рендеринга.
        /// </summary>
        public PaletteManagerMandelbrotFamily ActivePalette { get; set; }

        public ColorPaletteMandelbrotFamily()
        {
            Palettes = new List<PaletteManagerMandelbrotFamily>();
            LoadPalettes();
            ActivePalette ??= Palettes.FirstOrDefault();
        }

        private void LoadPalettes()
        {
            Palettes.Clear();
            Palettes.AddRange(CreateBuiltInPalettes());

            // ИЗМЕНЕНО: Формирование полного пути к файлу в папке "Saves"
            string filePath = Path.Combine(Application.StartupPath, "Saves", CUSTOM_PALETTES_FILE);

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new JsonColorConverter());
                    var customPalettes = JsonSerializer.Deserialize<List<PaletteManagerMandelbrotFamily>>(json, options);
                    if (customPalettes != null)
                    {
                        Palettes.AddRange(customPalettes);
                    }
                }
                catch (Exception ex)
                {
                    // Для согласованности можно использовать MessageBox, как в другом менеджере
                    MessageBox.Show($"Не удалось загрузить палитры для Мандельброта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void SaveCustomPalettes()
        {
            try
            {
                var customPalettes = Palettes.Where(p => !p.IsBuiltIn).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonColorConverter());
                string json = JsonSerializer.Serialize(customPalettes, options);

                // ИЗМЕНЕНО: Создание директории "Saves", если она не существует
                string savesDirectory = Path.Combine(Application.StartupPath, "Saves");
                Directory.CreateDirectory(savesDirectory);

                // ИЗМЕНЕНО: Формирование полного пути для сохранения
                string filePath = Path.Combine(savesDirectory, CUSTOM_PALETTES_FILE);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                // Для согласованности можно использовать MessageBox
                MessageBox.Show($"Не удалось сохранить палитры для Мандельброта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Создает коллекцию встроенных (предопределенных) цветовых палитр.
        /// </summary>
        /// <returns>Коллекция встроенных палитр.</returns>
        private IEnumerable<PaletteManagerMandelbrotFamily> CreateBuiltInPalettes()
        {
            return new List<PaletteManagerMandelbrotFamily>
            {
                new("Стандартный серый", new List<Color>(), true, true, 800, 1.0, false),
                new("Ультрафиолет", new List<Color> { Color.Black, Color.DarkViolet, Color.Violet, Color.White }, true, true, 1000, 1.2, false),
                new("Огонь", new List<Color> { Color.Black, Color.DarkRed, Color.Red, Color.Orange, Color.Yellow, Color.White }, true, true, 400, 0.9, false),
                new("Лёд", new List<Color> { Color.Black, Color.DarkBlue, Color.Blue, Color.Cyan, Color.White }, true, true, 500, 1.2, false),
                new("Огонь и лед", new List<Color> { Color.Black, Color.DarkBlue, Color.Cyan, Color.White, Color.Yellow, Color.Red, Color.DarkRed }, true, true, 700, 1.0, false),
                new("Психоделика", new List<Color> { Color.Red, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta }, false, true, 6, 1.0, false),
                new("Черно-белый", new List<Color> { Color.Black, Color.White }, false, true, 2, 1.0, false),
                new("Зеленый фонарь", new List<Color> { Color.Black, Color.FromArgb(255, 0, 128, 0), Color.FromArgb(255, 0, 204, 0), Color.FromArgb(255, 0, 234, 0), Color.FromArgb(255, 60, 255, 60), Color.FromArgb(255, 145, 255, 145), Color.FromArgb(255, 213, 255, 213), Color.White }, true, true, 120, 1.0, false),
                new("Сепия", new List<Color> { Color.FromArgb(20, 10, 0), Color.FromArgb(255, 240, 192) }, true, true, 500, 1.0, false)
            };
        }
    }
}