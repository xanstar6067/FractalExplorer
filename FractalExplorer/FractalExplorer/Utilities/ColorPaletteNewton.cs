using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using FractalExplorer.Core;

namespace FractalExplorer.Utilities
{
    /// <summary>
    /// Представляет цветовую палитру, специфичную для фракталов Ньютона.
    /// Включает цвета для корней, цвет фона и флаги градиента/встроенности.
    /// </summary>
    public class NewtonColorPalette
    {
        #region Properties

        /// <summary>
        /// Получает или устанавливает имя палитры.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Получает или устанавливает список цветов, используемых для отрисовки областей притяжения корней.
        /// </summary>
        public List<Color> RootColors { get; set; } = new List<Color>();

        /// <summary>
        /// Получает или устанавливает цвет фона, используемый для точек, не сходящихся к корням.
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.Black;

        /// <summary>
        /// Получает или устанавливает значение, указывающее, следует ли использовать градиентную заливку
        /// в областях притяжения корней в зависимости от количества итераций.
        /// </summary>
        public bool IsGradient { get; set; } = false;

        /// <summary>
        /// Получает или устанавливает значение, указывающее, является ли палитра встроенной (предопределенной).
        /// Встроенные палитры не могут быть удалены или изменены пользователем через UI.
        /// Игнорируется при сериализации в JSON.
        /// </summary>
        [JsonIgnore]
        public bool IsBuiltIn { get; set; } = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый пустой экземпляр класса <see cref="NewtonColorPalette"/>.
        /// Используется для десериализации.
        /// </summary>
        public NewtonColorPalette() { }

        #endregion
    }

    /// <summary>
    /// Управляет загрузкой, сохранением и выбором цветовых палитр для фракталов Ньютона.
    /// Поддерживает как встроенные, так и пользовательские палитры.
    /// </summary>
    public class NewtonPaletteManager
    {
        #region Fields

        /// <summary>
        /// Имя файла для сохранения пользовательских палитр.
        /// </summary>
        private const string PALETTE_FILE = "newton_palettes.json";

        #endregion

        #region Properties

        /// <summary>
        /// Получает список всех доступных палитр (включая встроенные и пользовательские).
        /// </summary>
        public List<NewtonColorPalette> Palettes { get; private set; }

        /// <summary>
        /// Получает или устанавливает текущую активную палитру.
        /// </summary>
        public NewtonColorPalette ActivePalette { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="NewtonPaletteManager"/>.
        /// Загружает встроенные и пользовательские палитры, а также устанавливает активную палитру по умолчанию.
        /// </summary>
        public NewtonPaletteManager()
        {
            Palettes = new List<NewtonColorPalette>();
            LoadPalettes();
            // Устанавливаем первую палитру как активную, или создаем простую "Default", если список пуст.
            ActivePalette = Palettes.FirstOrDefault() ?? new NewtonColorPalette { Name = "Default" };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Загружает встроенные и пользовательские палитры из файла.
        /// Встроенные палитры добавляются всегда, пользовательские - только если файл существует и корректен.
        /// </summary>
        private void LoadPalettes()
        {
            // Добавляем предопределенные встроенные палитры
            Palettes.Add(new NewtonColorPalette { Name = "Оттенки серого (Градиент)", RootColors = new List<Color> { Color.White, Color.LightGray, Color.DarkGray }, IsGradient = true, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Классика (Гармонический)", IsGradient = false, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Классика (Гармонический, Градиент)", IsGradient = true, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Чёрно-белый (Дискретный)", RootColors = new List<Color> { Color.White }, IsGradient = false, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Пастель (Дискретная)", RootColors = new List<Color> { Color.FromArgb(255, 182, 193), Color.FromArgb(173, 216, 230), Color.FromArgb(189, 252, 201), Color.FromArgb(253, 253, 150) }, IsGradient = false, IsBuiltIn = true, BackgroundColor = Color.FromArgb(40, 40, 40) });
            Palettes.Add(new NewtonColorPalette { Name = "Контраст (Дискретный)", RootColors = new List<Color> { Color.Red, Color.Yellow, Color.Blue }, IsGradient = false, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Огонь (Градиент)", RootColors = new List<Color> { Color.FromArgb(200, 0, 0), Color.FromArgb(255, 100, 0), Color.FromArgb(255, 255, 100) }, IsGradient = true, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Психоделика (Градиент)", RootColors = new List<Color> { Color.FromArgb(10, 0, 20), Color.Magenta, Color.Cyan }, IsGradient = true, IsBuiltIn = true });
            Palettes.Add(new NewtonColorPalette { Name = "Огонь и Лёд (Градиент)", RootColors = new List<Color> { Color.FromArgb(255, 100, 0), Color.FromArgb(0, 100, 255), Color.FromArgb(255, 200, 0), Color.FromArgb(0, 200, 255) }, IsGradient = true, IsBuiltIn = true });

            if (File.Exists(PALETTE_FILE))
            {
                try
                {
                    string json = File.ReadAllText(PALETTE_FILE);
                    string savesDirectory = Path.Combine(Application.StartupPath, "Saves");
                    string filePath = Path.Combine(savesDirectory, PALETTE_FILE);
                    var options = new JsonSerializerOptions();
                    // Добавляем конвертер для System.Drawing.Color
                    options.Converters.Add(new JsonColorConverter());

                    var customPalettes = JsonSerializer.Deserialize<List<NewtonColorPalette>>(json, options);

                    if (customPalettes != null)
                    {
                        // Добавляем загруженные пользовательские палитры к списку
                        Palettes.AddRange(customPalettes);
                    }
                }
                catch (Exception ex)
                {
                    // Отображаем сообщение об ошибке, если загрузка не удалась
                    MessageBox.Show($"Не удалось загрузить палитры для Ньютона: {ex.Message}", "Ошибка загрузки палитр", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Сохраняет все пользовательские палитры (которые не являются встроенными) в файл JSON.
        /// </summary>
        public void SavePalettes()
        {
            try
            {
                var customPalettes = Palettes.Where(p => !p.IsBuiltIn).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new Core.JsonColorConverter());
                string json = JsonSerializer.Serialize(customPalettes, options);

                // --- НАЧАЛО ИЗМЕНЕНИЯ ---
                string savesDirectory = Path.Combine(Application.StartupPath, "Saves");
                if (!Directory.Exists(savesDirectory))
                {
                    Directory.CreateDirectory(savesDirectory);
                }
                string filePath = Path.Combine(savesDirectory, PALETTE_FILE);
                // --- КОНЕЦ ИЗМЕНЕНИЯ ---

                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить палитры для Ньютона: {ex.Message}", "Ошибка сохранения палитр", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}