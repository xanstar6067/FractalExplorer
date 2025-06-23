using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace FractalExplorer.Core
{
    public class PaletteManager
    {
        private const string CONFIG_FILE_NAME = "palettes.json";
        public List<ColorPalette> Palettes { get; private set; }
        public ColorPalette ActivePalette { get; set; }

        public PaletteManager()
        {
            Palettes = new List<ColorPalette>();
            LoadPalettes();

            // ИЗМЕНЕНИЕ: Убеждаемся, что "Стандартный серый" является палитрой по умолчанию
            ActivePalette = Palettes.FirstOrDefault(p => p.Name == "Стандартный серый") ?? Palettes.First();
        }

        private void LoadPalettes()
        {
            AddBuiltInPalettes();

            try
            {
                string filePath = Path.Combine(Application.StartupPath, CONFIG_FILE_NAME);
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var customPalettes = JsonSerializer.Deserialize<List<ColorPalette>>(json);
                    if (customPalettes != null)
                    {
                        Palettes.AddRange(customPalettes);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файла палитр: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SaveCustomPalettes()
        {
            try
            {
                var customPalettes = Palettes.Where(p => !p.IsBuiltIn).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(customPalettes, options);
                string filePath = Path.Combine(Application.StartupPath, CONFIG_FILE_NAME);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения файла палитр: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddBuiltInPalettes()
        {
            // ИЗМЕНЕНИЕ: Переименовали палитру, чтобы отразить ее суть. Она будет обработана особым образом.
            Palettes.Add(new ColorPalette("Стандартный серый", new List<Color>(), false, true));
            Palettes.Add(new ColorPalette("Черно-белый", new List<Color> { Color.White, Color.Black }, false, true));
            Palettes.Add(new ColorPalette("Серый (линейный)", new List<Color> { Color.White, Color.Black }, true, true));
            Palettes.Add(new ColorPalette("Классика", new List<Color> { Color.FromArgb(0, 0, 0), Color.FromArgb(200, 50, 30), Color.FromArgb(255, 255, 255) }, true, true));
            Palettes.Add(new ColorPalette("Радуга", new List<Color> { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Indigo, Color.Violet }, true, true));
            Palettes.Add(new ColorPalette("Огонь", new List<Color> { Color.Black, Color.FromArgb(200, 0, 0), Color.FromArgb(255, 100, 0), Color.FromArgb(255, 255, 100), Color.White }, true, true));
            Palettes.Add(new ColorPalette("Лёд", new List<Color> { Color.Black, Color.FromArgb(0, 0, 100), Color.FromArgb(0, 120, 200), Color.FromArgb(170, 220, 255), Color.White }, true, true));
            Palettes.Add(new ColorPalette("Психоделика", new List<Color> { Color.FromArgb(10, 0, 20), Color.Magenta, Color.Cyan, Color.FromArgb(230, 230, 250) }, true, true));
        }
    }
}