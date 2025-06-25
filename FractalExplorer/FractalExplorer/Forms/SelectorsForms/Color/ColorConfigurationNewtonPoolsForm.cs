using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using Microsoft.VisualBasic; // Для Interaction.InputBox

namespace FractalExplorer
{
    /// <summary>
    /// Форма для настройки цветовых палитр для фракталов Ньютона.
    /// Позволяет пользователю выбирать, изменять, сохранять и удалять палитры,
    /// а также настраивать цвета для корней и фон, и режим градиента.
    /// </summary>
    public partial class ColorConfigurationNewtonPoolsForm : Form
    {
        #region Delegates and Events

        /// <summary>
        /// Делегат для события, возникающего при изменении активной палитры.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="activePalette">Активная палитра после изменения.</param>
        public delegate void PaletteChangedEventHandler(object sender, NewtonColorPalette activePalette);

        /// <summary>
        /// Событие, которое возникает при изменении активной палитры.
        /// </summary>
        public event PaletteChangedEventHandler PaletteChanged;

        #endregion

        #region Fields

        /// <summary>
        /// Менеджер палитр, управляющий доступными палитрами Ньютона.
        /// </summary>
        private NewtonPaletteManager _paletteManager;

        /// <summary>
        /// Требуемое количество корней для текущей формулы фрактала,
        /// используемое для отображения соответствующего количества пикеров цветов.
        /// </summary>
        private int _requiredRootCount;

        /// <summary>
        /// Флаг, указывающий, происходит ли изменение элементов UI программно,
        /// чтобы избежать рекурсивных вызовов обработчиков событий.
        /// </summary>
        private bool _isProgrammaticChange = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ColorConfigurationNewtonPoolsForm"/>.
        /// </summary>
        /// <param name="manager">Менеджер палитр для управления палитрами.</param>
        public ColorConfigurationNewtonPoolsForm(NewtonPaletteManager manager)
        {
            InitializeComponent();
            _paletteManager = manager;
            // Скрываем форму вместо закрытия при нажатии на кнопку закрытия окна
            FormClosing += ColorSetting_FormClosing;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Отображает форму настроек палитры, устанавливая требуемое количество пикеров цветов
        /// в соответствии с количеством найденных корней для фрактала Ньютона.
        /// </summary>
        /// <param name="rootCount">Количество корней для отображения.</param>
        public void ShowWithRootCount(int rootCount)
        {
            _requiredRootCount = rootCount;
            PopulatePaletteList();
            Show();
            Activate();
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Заполняет выпадающий список палитр и устанавливает активную палитру.
        /// </summary>
        private void PopulatePaletteList()
        {
            _isProgrammaticChange = true;
            cbPalettes.Items.Clear();
            foreach (var palette in _paletteManager.Palettes)
            {
                cbPalettes.Items.Add(palette.Name);
            }
            cbPalettes.SelectedItem = _paletteManager.ActivePalette.Name;
            _isProgrammaticChange = false;

            RefreshUIFromPalette(_paletteManager.ActivePalette);
        }

        /// <summary>
        /// Обновляет элементы пользовательского интерфейса в соответствии с данными предоставленной палитры.
        /// </summary>
        /// <param name="palette">Палитра, данные которой будут использованы для обновления UI.</param>
        private void RefreshUIFromPalette(NewtonColorPalette palette)
        {
            _isProgrammaticChange = true;

            // Обновляем состояние чекбокса градиента и цвета фона
            chkIsGradient.Checked = palette.IsGradient;
            panelBackgroundColor.BackColor = palette.BackgroundColor;

            // Управляем доступностью кнопок сохранения и удаления
            btnSave.Enabled = !palette.IsBuiltIn;
            btnDelete.Enabled = !palette.IsBuiltIn;

            // Очищаем и заново заполняем панель с пикерами цветов корней
            flpRootColorPickers.Controls.Clear();
            if (_requiredRootCount == 0)
            {
                flpRootColorPickers.Controls.Add(new Label { Text = "Корни не найдены, цвета не требуются.", ForeColor = Color.Gray, AutoSize = true });
            }
            else
            {
                // Используем цвета из палитры, если они есть, иначе генерируем гармонические цвета по умолчанию
                List<Color> colorsToUse = palette.RootColors;
                if (colorsToUse == null || colorsToUse.Count == 0)
                {
                    colorsToUse = GenerateHarmonicColors(_requiredRootCount);
                }

                for (int i = 0; i < _requiredRootCount; i++)
                {
                    // Зацикливаем цвета, если их количество меньше, чем количество корней
                    Color color = colorsToUse[i % colorsToUse.Count];

                    Panel colorPanel = new Panel
                    {
                        Size = new Size(25, 25),
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = color,
                        Tag = i, // Сохраняем индекс корня в Tag
                        Cursor = Cursors.Hand,
                        Margin = new Padding(5)
                    };
                    toolTip1.SetToolTip(colorPanel, $"Цвет для корня №{i + 1}\nНажмите, чтобы изменить");
                    colorPanel.Click += RootColorPanel_Click;
                    flpRootColorPickers.Controls.Add(colorPanel);
                }
            }
            _isProgrammaticChange = false;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик события изменения выбранного элемента в выпадающем списке палитр.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void cbPalettes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange)
            {
                return;
            }

            string selectedName = cbPalettes.SelectedItem.ToString();
            _paletteManager.ActivePalette = _paletteManager.Palettes.First(p => p.Name == selectedName);
            RefreshUIFromPalette(_paletteManager.ActivePalette);
            PaletteChanged?.Invoke(this, _paletteManager.ActivePalette);
        }

        /// <summary>
        /// Обработчик события клика по панели выбора цвета для корня.
        /// Открывает диалог выбора цвета и обновляет цвет корня в активной палитре.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void RootColorPanel_Click(object sender, EventArgs e)
        {
            if (_paletteManager.ActivePalette.IsBuiltIn)
            {
                MessageBox.Show("Встроенные палитры нельзя изменять. Сначала сохраните ее как новую палитру.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Panel clickedPanel = sender as Panel;
            int colorIndex = (int)clickedPanel.Tag;

            using (ColorDialog colorDialog = new ColorDialog { Color = clickedPanel.BackColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    clickedPanel.BackColor = colorDialog.Color;

                    // Убеждаемся, что список цветов в палитре достаточно большой
                    while (_paletteManager.ActivePalette.RootColors.Count <= colorIndex)
                    {
                        _paletteManager.ActivePalette.RootColors.Add(Color.Black);
                    }
                    _paletteManager.ActivePalette.RootColors[colorIndex] = colorDialog.Color;
                    PaletteChanged?.Invoke(this, _paletteManager.ActivePalette);
                }
            }
        }

        /// <summary>
        /// Обработчик события клика по панели выбора цвета фона.
        /// Открывает диалог выбора цвета и обновляет цвет фона в активной палитре.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void panelBackgroundColor_Click(object sender, EventArgs e)
        {
            if (_paletteManager.ActivePalette.IsBuiltIn)
            {
                MessageBox.Show("Встроенные палитры нельзя изменять. Сначала сохраните ее как новую палитру.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (ColorDialog colorDialog = new ColorDialog { Color = panelBackgroundColor.BackColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    panelBackgroundColor.BackColor = colorDialog.Color;
                    _paletteManager.ActivePalette.BackgroundColor = colorDialog.Color;
                    PaletteChanged?.Invoke(this, _paletteManager.ActivePalette);
                }
            }
        }

        /// <summary>
        /// Обработчик события изменения состояния чекбокса "IsGradient".
        /// Обновляет режим градиента в активной палитре.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void chkIsGradient_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticChange)
            {
                return;
            }
            if (_paletteManager.ActivePalette.IsBuiltIn)
            {
                MessageBox.Show("Встроенные палитры нельзя изменять. Сначала сохраните ее как новую палитру.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _isProgrammaticChange = true;
                chkIsGradient.Checked = !chkIsGradient.Checked; // Возвращаем состояние чекбокса обратно
                _isProgrammaticChange = false;
                return;
            }
            _paletteManager.ActivePalette.IsGradient = chkIsGradient.Checked;
            PaletteChanged?.Invoke(this, _paletteManager.ActivePalette);
        }

        #endregion

        #region Palette Management Actions

        /// <summary>
        /// Обработчик события клика по кнопке "Save".
        /// Сохраняет текущую активную палитру в файл.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_paletteManager.ActivePalette.IsBuiltIn)
            {
                MessageBox.Show("Нельзя сохранить изменения во встроенной палитре.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _paletteManager.SavePalettes();
            MessageBox.Show("Палитра сохранена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Save As".
        /// Позволяет сохранить текущую палитру под новым именем.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            string newName = Interaction.InputBox("Введите имя для новой палитры:", "Сохранить палитру как", "Моя палитра");
            if (string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            if (_paletteManager.Palettes.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Палитра с таким именем уже существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var currentPalette = _paletteManager.ActivePalette;
            var newPalette = new NewtonColorPalette
            {
                Name = newName,
                BackgroundColor = currentPalette.BackgroundColor,
                IsGradient = currentPalette.IsGradient,
                IsBuiltIn = false,
                RootColors = new List<Color>()
            };

            // Копируем текущие цвета пикеров в новую палитру
            foreach (Panel panel in flpRootColorPickers.Controls.OfType<Panel>())
            {
                newPalette.RootColors.Add(panel.BackColor);
            }

            _paletteManager.Palettes.Add(newPalette);
            _paletteManager.ActivePalette = newPalette;
            _paletteManager.SavePalettes();
            PopulatePaletteList(); // Обновляем список палитр в UI
            MessageBox.Show($"Палитра '{newName}' создана и сохранена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Delete".
        /// Удаляет текущую активную палитру (если она не встроенная).
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnDelete_Click(object sender, EventArgs e)
        {
            var paletteToDelete = _paletteManager.ActivePalette;
            if (paletteToDelete.IsBuiltIn)
            {
                return;
            }

            DialogResult confirmResult = MessageBox.Show($"Вы уверены, что хотите удалить палитру '{paletteToDelete.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmResult == DialogResult.Yes)
            {
                _paletteManager.Palettes.Remove(paletteToDelete);
                _paletteManager.ActivePalette = _paletteManager.Palettes.First(); // Активируем первую оставшуюся палитру
                _paletteManager.SavePalettes();
                PopulatePaletteList(); // Обновляем список палитр в UI
                PaletteChanged?.Invoke(this, _paletteManager.ActivePalette);
            }
        }

        #endregion

        #region Color Conversion / Generation

        /// <summary>
        /// Генерирует список гармоничных цветов на основе количества.
        /// </summary>
        /// <param name="count">Требуемое количество цветов.</param>
        /// <returns>Список сгенерированных цветов.</returns>
        public static List<Color> GenerateHarmonicColors(int count)
        {
            var colors = new List<Color>();
            if (count == 0)
            {
                return colors;
            }
            for (int i = 0; i < count; i++)
            {
                float hue = (360f * i) / count; // Равномерное распределение по оттенку (Hue)
                colors.Add(ColorFromHSL(hue, 0.85f, 0.6f)); // Фиксированная насыщенность (Saturation) и яркость (Lightness)
            }
            return colors;
        }

        /// <summary>
        /// Преобразует цвет из цветового пространства HSL (оттенок, насыщенность, яркость) в RGB.
        /// </summary>
        /// <param name="h">Оттенок (0-360).</param>
        /// <param name="s">Насыщенность (0-1).</param>
        /// <param name="l">Яркость (0-1).</param>
        /// <returns>Цвет в формате RGB.</returns>
        public static Color ColorFromHSL(float h, float s, float l)
        {
            float r, g, b;
            if (s == 0)
            {
                r = g = b = l; // Оттенки серого
            }
            else
            {
                float q = l < 0.5f ? l * (1 + s) : l + s - l * s;
                float p = 2 * l - q;
                h /= 360f; // Нормализуем оттенок к [0, 1]
                r = HueToRgb(p, q, h + 1 / 3f);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1 / 3f);
            }
            return Color.FromArgb(255, (int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        /// <summary>
        /// Вспомогательный метод для преобразования оттенка (Hue) в компонент RGB.
        /// Используется в HSL-to-RGB преобразовании.
        /// </summary>
        /// <param name="p">Параметр p из HSL-преобразования.</param>
        /// <param name="q">Параметр q из HSL-преобразования.</param>
        /// <param name="t">Нормализованный компонент оттенка.</param>
        /// <returns>Значение компонента RGB (от 0 до 1).</returns>
        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0)
            {
                t += 1;
            }
            if (t > 1)
            {
                t -= 1;
            }
            if (t < 1 / 6f)
            {
                return p + (q - p) * 6 * t;
            }
            if (t < 1 / 2f)
            {
                return q;
            }
            if (t < 2 / 3f)
            {
                return p + (q - p) * (2 / 3f - t) * 6;
            }
            return p;
        }

        #endregion

        #region Form Lifecycle

        /// <summary>
        /// Обработчик события закрытия формы.
        /// Скрывает форму вместо полного закрытия, чтобы ее можно было повторно использовать.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события закрытия формы.</param>
        private void ColorSetting_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // Отменяем стандартное закрытие
                Hide(); // Скрываем форму
            }
        }

        #endregion
    }
}