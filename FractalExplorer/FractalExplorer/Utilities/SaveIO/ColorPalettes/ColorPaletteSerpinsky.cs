using FractalExplorer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.SaveIO.ColorPalettes
{
    /// <summary>
    /// Представляет цветовую палитру для фрактала Серпинского.
    /// </summary>
    public class SerpinskyColorPalette
    {
        public string Name { get; set; }
        public Color FractalColor { get; set; } = Color.Black;
        public Color BackgroundColor { get; set; } = Color.White;
        public bool IsBuiltIn { get; set; } = false; // JsonIgnore не нужен, т.к. мы будем сохранять только не-встроенные

        public SerpinskyColorPalette() { }
    }

    /// <summary>
    /// Управляет загрузкой, сохранением и выбором цветовых палитр для фрактала Серпинского.
    /// </summary>
    public class SerpinskyPaletteManager
    {
        private const string PALETTE_FILE = "serpinsky_palettes.json";
        public List<SerpinskyColorPalette> Palettes { get; private set; }
        public SerpinskyColorPalette ActivePalette { get; set; }

        public SerpinskyPaletteManager()
        {
            Palettes = new List<SerpinskyColorPalette>();
            LoadPalettes();
            ActivePalette = Palettes.FirstOrDefault(p => p.Name == "Классический Ч/Б") ?? Palettes.FirstOrDefault();
        }

        private void LoadPalettes()
        {
            // Добавляем встроенные палитры
            Palettes.Add(new SerpinskyColorPalette { Name = "Классический Ч/Б", FractalColor = Color.Black, BackgroundColor = Color.White, IsBuiltIn = true });
            Palettes.Add(new SerpinskyColorPalette { Name = "Инверсия", FractalColor = Color.White, BackgroundColor = Color.Black, IsBuiltIn = true });
            Palettes.Add(new SerpinskyColorPalette { Name = "Оттенки серого", FractalColor = Color.FromArgb(50, 50, 50), BackgroundColor = Color.White, IsBuiltIn = true });
            Palettes.Add(new SerpinskyColorPalette { Name = "Огонь и ночь", FractalColor = Color.OrangeRed, BackgroundColor = Color.FromArgb(10, 0, 20), IsBuiltIn = true });
            Palettes.Add(new SerpinskyColorPalette { Name = "Глубокий океан", FractalColor = Color.Aqua, BackgroundColor = Color.DarkSlateBlue, IsBuiltIn = true });

            string filePath = Path.Combine(Application.StartupPath, PALETTE_FILE);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions();
                    options.Converters.Add(new JsonConverters.JsonColorConverter());
                    var customPalettes = JsonSerializer.Deserialize<List<SerpinskyColorPalette>>(json, options);
                    if (customPalettes != null)
                    {
                        Palettes.AddRange(customPalettes);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось загрузить палитры для Серпинского: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void SaveCustomPalettes()
        {
            try
            {
                var customPalettes = Palettes.Where(p => !p.IsBuiltIn).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonConverters.JsonColorConverter());
                string json = JsonSerializer.Serialize(customPalettes, options);
                string filePath = Path.Combine(Application.StartupPath, PALETTE_FILE);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить палитры для Серпинского: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
