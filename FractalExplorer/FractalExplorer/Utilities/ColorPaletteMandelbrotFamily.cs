using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using FractalExplorer.Utilities;

namespace FractalExplorer.Core 
{
    /// <summary>
    /// Управляет коллекцией цветовых палитр, их загрузкой, сохранением и активной палитрой.
    /// </summary>
    public class ColorPaletteMandelbrotFamily
    {
        #region Fields

        /// <summary>
        /// Имя файла конфигурации, используемого для сохранения пользовательских палитр.
        /// </summary>
        private const string CONFIG_FILE_NAME = "palettes.json";

        #endregion

        #region Properties

        /// <summary>
        /// Получает список всех доступных цветовых палитр (включая встроенные и пользовательские).
        /// </summary>
        public List<PaletteManagerBase> Palettes { get; private set; }

        /// <summary>
        /// Получает или устанавливает активную (текущую используемую) цветовую палитру.
        /// </summary>
        public PaletteManagerBase ActivePalette { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ColorPaletteMandelbrotFamily"/>.
        /// Загружает встроенные и пользовательские палитры из файла,
        /// а затем устанавливает активную палитру по умолчанию.
        /// </summary>
        public ColorPaletteMandelbrotFamily()
        {
            Palettes = new List<PaletteManagerBase>();
            LoadPalettes();

            // Инициализируем активную палитру:
            // Сначала пытаемся найти палитру с именем "Стандартный серый".
            // Если ее нет, выбираем первую палитру из загруженного списка.
            ActivePalette = Palettes.FirstOrDefault(p => p.Name == "Стандартный серый") ?? Palettes.First();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Загружает встроенные палитры и пользовательские палитры из файла конфигурации.
        /// Пользовательские палитры загружаются только если файл существует и корректен.
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

                    var options = new JsonSerializerOptions();
                    // Добавляем наш кастомный конвертер для System.Drawing.Color
                    options.Converters.Add(new JsonColorConverter());

                    var customPalettes = JsonSerializer.Deserialize<List<PaletteManagerBase>>(json, options);

                    if (customPalettes != null)
                    {
                        // Добавляем загруженные пользовательские палитры к списку.
                        // Помечаем их как не встроенные, так как они были загружены из файла.
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
                // В случае ошибки загрузки файла, отображаем сообщение пользователю
                // и продолжаем работу только с встроенными палитрами.
                MessageBox.Show($"Ошибка загрузки файла палитр: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Сохраняет только пользовательские палитры (которые не являются встроенными) в файл конфигурации JSON.
        /// </summary>
        public void SaveCustomPalettes()
        {
            try
            {
                // Выбираем только пользовательские палитры для сохранения
                var customPalettes = Palettes.Where(p => !p.IsBuiltIn).ToList();

                var options = new JsonSerializerOptions { WriteIndented = true }; // Форматируем JSON для удобочитаемости
                // Добавляем наш кастомный конвертер для System.Drawing.Color
                options.Converters.Add(new JsonColorConverter());

                string json = JsonSerializer.Serialize(customPalettes, options);

                string filePath = Path.Combine(Application.StartupPath, CONFIG_FILE_NAME);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                // Отображаем сообщение об ошибке, если сохранение не удалось
                MessageBox.Show($"Ошибка сохранения файла палитр: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Добавляет набор встроенных (предустановленных) цветовых палитр в коллекцию.
        /// Эти палитры помечены как встроенные и не могут быть изменены или удалены пользователем.
        /// </summary>
        private void AddBuiltInPalettes()
        {
            // 1. Палитра по умолчанию "Стандартный серый".
            // Она использует специальную логарифмическую формулу для расчета цвета,
            // поэтому список Colors пуст, а IsGradient установлен в False (т.к. это не простой линейный градиент).
            Palettes.Add(new PaletteManagerBase("Стандартный серый", new List<Color>(), false, true));

            // 2. "Черно-белый" градиент.
            // Это линейный градиент от белого к черному.
            Palettes.Add(new PaletteManagerBase("Черно-белый", new List<Color> { Color.White, Color.Black }, true, true));

            // Примечание: Старая палитра "Черно-белый" (которая была дискретной и вызывала проблемы) удалена.

            // 3. Остальные палитры (без изменений в их определении, только форматирование)
            Palettes.Add(new PaletteManagerBase("Классика", new List<Color> { Color.FromArgb(0, 0, 0), Color.FromArgb(200, 50, 30), Color.FromArgb(255, 255, 255) }, true, true));
            Palettes.Add(new PaletteManagerBase("Радуга", new List<Color> { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Indigo, Color.Violet }, true, true));
            Palettes.Add(new PaletteManagerBase("Огонь", new List<Color> { Color.Black, Color.FromArgb(200, 0, 0), Color.FromArgb(255, 100, 0), Color.FromArgb(255, 255, 100), Color.White }, true, true));
            Palettes.Add(new PaletteManagerBase("Лёд", new List<Color> { Color.Black, Color.FromArgb(0, 0, 100), Color.FromArgb(0, 120, 200), Color.FromArgb(170, 220, 255), Color.White }, true, true));
            Palettes.Add(new PaletteManagerBase("Психоделика", new List<Color> { Color.FromArgb(10, 0, 20), Color.Magenta, Color.Cyan, Color.FromArgb(230, 230, 250) }, true, true));
        }

        #endregion
    }
}