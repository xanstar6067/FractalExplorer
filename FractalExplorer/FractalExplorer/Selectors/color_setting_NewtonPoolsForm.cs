using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace FractalExplorer
{
    public partial class color_setting_NewtonPoolsForm : Form
    {
        // Делегат и событие для уведомления главной формы об изменениях
        public delegate void ColorsChangedEventHandler(object sender, CustomPalette newPalette);
        public event ColorsChangedEventHandler ColorsChanged;

        // Внутреннее хранилище цветов
        private List<Color> _currentRootColors = new List<Color>();
        private Color _currentBackgroundColor = Color.Black;

        public color_setting_NewtonPoolsForm()
        {
            InitializeComponent();
            // Перехватываем событие закрытия формы, чтобы скрыть ее, а не уничтожить
            this.FormClosing += new FormClosingEventHandler(this.ColorSetting_FormClosing);
        }

        /// <summary>
        /// Метод для заполнения формы элементами выбора цвета. Вызывается из главной формы.
        /// </summary>
        /// <param name="currentRootColors">Список текущих цветов для корней.</param>
        /// <param name="currentBgColor">Текущий цвет фона.</param>
        /// <param name="rootCount">Необходимое количество цветов для корней.</param>
        public void PopulateColorPickers(List<Color> currentRootColors, Color currentBgColor, int rootCount)
        {
            _currentRootColors = new List<Color>(currentRootColors);
            _currentBackgroundColor = currentBgColor;

            // Очищаем панель перед добавлением новых элементов
            flpRootColorPickers.Controls.Clear();

            // Убедимся, что у нас достаточно цветов в списке
            while (_currentRootColors.Count < rootCount)
            {
                _currentRootColors.Add(GetDefaultColorForIndex(_currentRootColors.Count));
            }
            // Можно также обрезать список, если корней стало меньше, но лучше оставить
            // на случай, если пользователь вернется к предыдущей формуле.

            // Создаем элементы для выбора цвета корней
            for (int i = 0; i < rootCount; i++)
            {
                Label lbl = new Label
                {
                    Text = $"Цвет корня {i + 1}:",
                    AutoSize = true,
                    Margin = new Padding(3, 6, 3, 3) // Отступы для выравнивания
                };

                Panel colorPanel = new Panel
                {
                    Size = new Size(50, 20),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = _currentRootColors[i],
                    Tag = i, // Сохраняем индекс в Tag для легкого доступа
                    Cursor = Cursors.Hand,
                    Margin = new Padding(3, 3, 15, 3)
                };

                // При клике на панель открываем диалог выбора цвета
                colorPanel.Click += RootColorPanel_Click;

                flpRootColorPickers.Controls.Add(lbl);
                flpRootColorPickers.Controls.Add(colorPanel);
            }

            if (rootCount == 0)
            {
                Label lbl = new Label
                {
                    Text = "Корни не найдены. Нечего настраивать.",
                    AutoSize = true,
                    ForeColor = Color.Red
                };
                flpRootColorPickers.Controls.Add(lbl);
            }

            // Устанавливаем цвет для фона
            panelBackgroundColor.BackColor = _currentBackgroundColor;
        }

        private void RootColorPanel_Click(object sender, System.EventArgs e)
        {
            Panel clickedPanel = sender as Panel;
            if (clickedPanel == null) return;

            int colorIndex = (int)clickedPanel.Tag;

            using (ColorDialog cd = new ColorDialog())
            {
                cd.Color = clickedPanel.BackColor;
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    // Обновляем цвет панели и в нашем списке
                    clickedPanel.BackColor = cd.Color;
                    _currentRootColors[colorIndex] = cd.Color;

                    // Уведомляем подписчиков об изменении
                    RaiseColorsChangedEvent();
                }
            }
        }

        private void panelBackgroundColor_Click(object sender, System.EventArgs e)
        {
            using (ColorDialog cd = new ColorDialog())
            {
                cd.Color = panelBackgroundColor.BackColor;
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    panelBackgroundColor.BackColor = cd.Color;
                    _currentBackgroundColor = cd.Color;

                    // Уведомляем подписчиков об изменении
                    RaiseColorsChangedEvent();
                }
            }
        }

        // Метод для вызова события
        private void RaiseColorsChangedEvent()
        {
            CustomPalette palette = new CustomPalette
            {
                RootColors = _currentRootColors,
                BackgroundColor = _currentBackgroundColor
            };
            ColorsChanged?.Invoke(this, palette);
        }

        // Вспомогательный метод для генерации цветов по умолчанию
        private Color GetDefaultColorForIndex(int index)
        {
            Color[] defaults = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Magenta, Color.Cyan, Color.Orange, Color.Purple, Color.Brown, Color.LightGreen };
            return defaults[index % defaults.Length];
        }

        private void btnClose_Click(object sender, System.EventArgs e)
        {
            this.Hide(); // Просто скрываем форму, а не закрываем
        }

        // Переопределяем закрытие формы
        private void ColorSetting_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Если форму закрывают через "крестик", мы отменяем закрытие и просто скрываем ее
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
    }
    // Класс для удобной передачи данных о палитре
    public class CustomPalette
    {
        public List<Color> RootColors { get; set; }
        public Color BackgroundColor { get; set; }
    }
}