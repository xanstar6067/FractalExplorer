using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace FractalExplorer.Core // Убедитесь, что пространство имен совпадает с вашим
{
    /// <summary>
    /// Управляет коллекцией цветовых палитр, их загрузкой, сохранением и активной палитрой.
    /// </summary>
    public class PaletteManager
    {
        private const string CONFIG_FILE_NAME = "palettes.json";

        /// <summary>
        /// Получает список всех доступных цветовых палитр.
        /// </summary>
        public List<ColorPaletteBase> Palettes { get; private set; }

        /// <summary>
        /// Получает или устанавливает активную (текущую используемую) цветовую палитру.
        /// </summary>
        public ColorPaletteBase ActivePalette { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PaletteManager"/>.
        /// Загружает встроенные палитры и пользовательские палитры из файла.
        /// </summary>
        public PaletteManager()
        {
            Palettes = new List<ColorPaletteBase>();
            LoadPalettes();

            // Инициализируем активную палитру, пытаясь найти "Стандартный серый",
            // иначе выбираем первую палитру из списка.
            ActivePalette = Palettes.FirstOrDefault(p => p.Name == "Стандартный серый") ?? Palettes.First();
        }

        /// <summary>
        /// Загружает встроенные палитры и пользовательские палитры из файла конфигурации.
        /// </summary>
        private void LoadPalettes()
        {
            AddBuiltInPalettes();

            try
            {
                string filePath = Path.Combine(Application.StartupPath, CONFIG_FILE_NAME);
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);

                    // ИЗМЕНЕНИЕ: Создаем настройки для десериализатора
                    var options = new JsonSerializerOptions();
                    // ИЗМЕНЕНИЕ: Добавляем наш конвертер в список "переводчиков"
                    options.Converters.Add(new JsonColorConverter());

                    // ИЗМЕНЕНИЕ: Передаем настройки в метод Deserialize
                    var customPalettes = JsonSerializer.Deserialize<List<ColorPaletteBase>>(json, options);

                    if (customPalettes != null)
                    {
                        // Добавляем пользовательские палитры к списку, но помечаем их как не встроенные
                        foreach (var palette in customPalettes)
                        {
                            palette.IsBuiltIn = false;
                        }
                        Palettes.AddRange(customPalettes);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файла палитр: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // В случае ошибки загрузки, просто продолжаем с встроенными палитрами
            }
        }

        /// <summary>
        /// Сохраняет только пользовательские палитры в файл конфигурации.
        /// Встроенные палитры не сохраняются.
        /// </summary>
        public void SaveCustomPalettes()
        {
            try
            {
                var customPalettes = Palettes.Where(p => !p.IsBuiltIn).ToList();

                // ИЗМЕНЕНИЕ: Создаем настройки для сериализатора
                var options = new JsonSerializerOptions { WriteIndented = true };
                // ИЗМЕНЕНИЕ: Добавляем наш конвертер в список "переводчиков"
                options.Converters.Add(new JsonColorConverter());

                // ИЗМЕНЕНИЕ: Передаем настройки в метод Serialize
                string json = JsonSerializer.Serialize(customPalettes, options);

                string filePath = Path.Combine(Application.StartupPath, CONFIG_FILE_NAME);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения файла палитр: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Добавляет набор встроенных (предустановленных) цветовых палитр.
        /// </summary>
        private void AddBuiltInPalettes()
        {
            // --- ВНЕСЕНЫ ИЗМЕНЕНИЯ СОГЛАСНО ВАШЕМУ ЗАПРОСУ ---

            // 1. Палитра по умолчанию, использует специальную логарифмическую формулу.
            Palettes.Add(new ColorPaletteBase("Стандартный серый", new List<Color>(), false, true));

            // 2. Старый "Серый (линейный)" переименован в "Черно-белый".
            //    Это правильный градиент от белого к черному.
            Palettes.Add(new ColorPaletteBase("Черно-белый", new List<Color> { Color.White, Color.Black }, true, true));

            // 3. Старый "Черно-белый" (который был дискретным и вызывал проблемы) УДАЛЕН.

            // Остальные палитры без изменений
            Palettes.Add(new ColorPaletteBase("Классика", new List<Color> { Color.FromArgb(0, 0, 0), Color.FromArgb(200, 50, 30), Color.FromArgb(255, 255, 255) }, true, true));
            Palettes.Add(new ColorPaletteBase("Радуга", new List<Color> { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Indigo, Color.Violet }, true, true));
            Palettes.Add(new ColorPaletteBase("Огонь", new List<Color> { Color.Black, Color.FromArgb(200, 0, 0), Color.FromArgb(255, 100, 0), Color.FromArgb(255, 255, 100), Color.White }, true, true));
            Palettes.Add(new ColorPaletteBase("Лёд", new List<Color> { Color.Black, Color.FromArgb(0, 0, 100), Color.FromArgb(0, 120, 200), Color.FromArgb(170, 220, 255), Color.White }, true, true));
            Palettes.Add(new ColorPaletteBase("Психоделика", new List<Color> { Color.FromArgb(10, 0, 20), Color.Magenta, Color.Cyan, Color.FromArgb(230, 230, 250) }, true, true));
        }
    }
}