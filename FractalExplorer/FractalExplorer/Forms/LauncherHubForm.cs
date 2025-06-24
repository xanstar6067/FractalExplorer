using System;
using System.Reflection; // Для доступа к атрибутам сборки
using System.Windows.Forms;
using FractalDraving; // Если FractalMondelbrot, FractalJulia и т.д. в этом namespace
using FractalExplorer.Projects; // Или где находятся ваши классы FractalMondelbrot, FractalJulia и т.д.

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
            DisplayAppVersionInTitle();
        }

        #region Event Handlers

        /// <summary>
        /// Обработчик события загрузки формы.
        /// Устанавливает версию приложения в заголовок окна.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void MainFractalForm_Load(object sender, EventArgs e)
        {
            
        }

        /// <summary>
        /// Отображает версию приложения в заголовке окна.
        /// </summary>
        private void DisplayAppVersionInTitle()
        {
            string appVersion = GetAppVersion();
            this.Text = $"{this.Text} - Версия: {appVersion}";
        }

        /// <summary>
        /// Получает версию приложения из атрибутов сборки.
        /// Приоритет: AssemblyInformationalVersion, затем AssemblyFileVersion, затем AssemblyVersion.
        /// </summary>
        /// <returns>Строка с версией приложения.</returns>
        private string GetAppVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            // 1. Предпочтительно использовать AssemblyInformationalVersionAttribute
            var informationalVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (informationalVersionAttribute != null && !string.IsNullOrWhiteSpace(informationalVersionAttribute.InformationalVersion))
            {
                // InformationalVersion у нас настроена на $(AssemblyVersion)$(VersionSuffix),
                // что даст, например, "1.0.8850.12345" или "1.0.8850.12345-beta1"
                return informationalVersionAttribute.InformationalVersion;
            }

            // 2. Если AssemblyInformationalVersionAttribute отсутствует или пуст, пробуем AssemblyFileVersionAttribute
            // FileVersion у нас $(VersionPrefix).0.0, например, "1.0.0.0"
            var fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (fileVersionAttribute != null && !string.IsNullOrWhiteSpace(fileVersionAttribute.Version))
            {
                return fileVersionAttribute.Version;
            }

            // 3. Как запасной вариант (если по какой-то причине другие атрибуты не сработали), 
            // получаем AssemblyVersion напрямую.
            // AssemblyVersion у нас $(VersionPrefix).*, например, "1.0.8850.12345"
            Version version = assembly.GetName().Version;
            if (version != null)
            {
                return version.ToString();
            }

            return "неизвестно"; // Если ничего не найдено
        }


        /// <summary>
        /// Обработчик события загрузки формы, сгенерированный дизайнером.
        /// (Функция-заглушка, не удалять).
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Аргументы события.</param>
        private void MainFractalForm_Load_1(object sender, EventArgs e)
        {
            // Оставлен как заглушка, как указано в оригинале,
            // но теперь мы используем MainFractalForm_Load для установки версии.
            // Если этот обработчик назначен в дизайнере, убедитесь, что
            // вызов DisplayAppVersionInTitle() происходит в правильном месте
            // или перенесите логику из MainFractalForm_Load сюда, если этот используется.
            // Для чистоты, лучше использовать только один обработчик Load.
            // Я бы рекомендовал удалить этот (MainFractalForm_Load_1) и убедиться,
            // что в дизайнере событие Load привязано к MainFractalForm_Load.
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