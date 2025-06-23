using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace FractalExplorer
{
    // Простая структура для хранения данных одной палитры
    public class NewtonColorPalette
    {
        public string Name { get; set; }
        public List<Color> RootColors { get; set; } = new List<Color>();
        public Color BackgroundColor { get; set; } = Color.Black;
        public bool IsGradient { get; set; } = false;
        public bool IsBuiltIn { get; set; } = false;
    }

    // Локальный менеджер палитр только для Бассейнов Ньютона
    public class NewtonPaletteManager
    {
        private const string PALETTE_FILE = "newton_palettes.json";
        public List<NewtonColorPalette> Palettes { get; private set; }
        public NewtonColorPalette ActivePalette { get; set; }

        public NewtonPaletteManager()
        {
            Palettes = new List<NewtonColorPalette>();
            LoadPalettes();
            ActivePalette = Palettes.FirstOrDefault() ?? new NewtonColorPalette { Name = "Default" };
        }

        private void LoadPalettes()
        {
            // --- ВОССТАНОВЛЕННЫЙ И РАСШИРЕННЫЙ СПИСОК ПАЛИТР ---

            // Палитра по умолчанию
            Palettes.Add(new NewtonColorPalette { Name = "Оттенки серого (Градиент)", RootColors = new List<Color> { Color.White, Color.LightGray, Color.DarkGray }, IsGradient = true, IsBuiltIn = true });

            // Палитры, у которых нет заданных цветов (они будут генерироваться автоматически)
            Palettes.Add(new NewtonColorPalette { Name = "Классика (Гармонический)", IsGradient = false, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Классика (Гармонический, Градиент)", IsGradient = true, IsBuiltIn = true });

            // Восстановленные палитры
            Palettes.Add(new NewtonColorPalette { Name = "Чёрно-белый (Дискретный)", RootColors = new List<Color> { Color.White }, IsGradient = false, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Пастель (Дискретная)", RootColors = new List<Color> { Color.FromArgb(255, 182, 193), Color.FromArgb(173, 216, 230), Color.FromArgb(189, 252, 201), Color.FromArgb(253, 253, 150) }, IsGradient = false, IsBuiltIn = true, BackgroundColor = Color.FromArgb(40, 40, 40) });
            Palettes.Add(new NewtonColorPalette { Name = "Контраст (Дискретный)", RootColors = new List<Color> { Color.Red, Color.Yellow, Color.Blue }, IsGradient = false, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Огонь (Градиент)", RootColors = new List<Color> { Color.FromArgb(200, 0, 0), Color.FromArgb(255, 100, 0), Color.FromArgb(255, 255, 100) }, IsGradient = true, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Психоделика (Градиент)", RootColors = new List<Color> { Color.FromArgb(10, 0, 20), Color.Magenta, Color.Cyan }, IsGradient = true, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Огонь и Лёд (Градиент)", RootColors = new List<Color> { Color.FromArgb(255, 100, 0), Color.FromArgb(0, 100, 255), Color.FromArgb(255, 200, 0), Color.FromArgb(0, 200, 255) }, IsGradient = true, IsBuiltIn = true });

            // Загружаем пользовательские палитры
            if (File.Exists(PALETTE_FILE))
            {
                try
                {
                    string json = File.ReadAllText(PALETTE_FILE);
                    var customPalettes = JsonSerializer.Deserialize<List<NewtonColorPalette>>(json);
                    if (customPalettes != null)
                    {
                        Palettes.AddRange(customPalettes);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось загрузить палитры для Ньютона: {ex.Message}");
                }
            }
        }

        public void SavePalettes()
        {
            try
            {
                var customPalettes = Palettes.Where(p => !p.IsBuiltIn).ToList();
                string json = JsonSerializer.Serialize(customPalettes, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(PALETTE_FILE, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить палитры для Ньютона: {ex.Message}");
            }
        }
    }
}