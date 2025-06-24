using System;
using System.Windows.Forms; // Добавлен using для System.Windows.Forms, если он не был добавлен автоматически
using FractalDraving;
using FractalExplorer.Projects;


namespace FractalExplorer
{
    /// <summary>
    /// Главная форма приложения, служащая хабом для запуска различных фрактальных форм.
    /// </summary>
    public partial class LauncherHubForm : Form
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="LauncherHubForm"/>.
        /// </summary>
        public LauncherHubForm()
        {
            InitializeComponent();
        }

        #region Event Handlers

        /// <summary>
        /// Обработчик события загрузки формы.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void MainFractalForm_Load(object sender, EventArgs e)
        {
            // Этот метод может быть пустым, если не требуется никакой инициализации при загрузке.
        }

        /// <summary>
        /// Обработчик события загрузки формы, сгенерированный дизайнером.
        /// (Функция-заглушка, не удалять).
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void MainFractalForm_Load_1(object sender, EventArgs e)
        {
            // Оставлен как заглушка, как указано в оригинале.
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Launch Mondelbrot".
        /// Открывает новую форму для фрактала Мандельброта.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnLaunchMondelbrot_Click(object sender, EventArgs e)
        {
            var form = new FractalMondelbrot();
            form.Show();
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Launch Julia".
        /// Открывает новую форму для фрактала Жюлиа.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnLaunchJulia_Click(object sender, EventArgs e)
        {
            var form = new FractalJulia();
            form.Show();
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Launch Serpinsky".
        /// Открывает новую форму для фрактала Серпинского.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnLaunchSerpinsky_Click(object sender, EventArgs e)
        {
            var form = new FractalSerpinsky();
            form.Show();
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Launch Newton".
        /// Открывает новую форму для фрактала Ньютона.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnLaunchNewton_Click(object sender, EventArgs e)
        {
            var form = new NewtonPools();
            form.Show();
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Launch Burning Ship Julia".
        /// Открывает новую форму для фрактала "Пылающий корабль" Жюлиа.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnLaunchBurningShipJulia_Click(object sender, EventArgs e)
        {
            var form = new FractalJuliaBurningShip();
            form.Show();
        }

        /// <summary>
        /// Обработчик события клика по кнопке "Launch Burning Ship Mandelbrot".
        /// Открывает новую форму для фрактала "Пылающий корабль" Мандельброта.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void btnLaunchBurningShipMandelbrot_Click(object sender, EventArgs e)
        {
            var form = new FractalMondelbrotBurningShip();
            form.Show();
        }

        #endregion
    }
}