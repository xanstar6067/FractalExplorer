using FractalExplorer.Forms;
using FractalExplorer.Projects;
using System.Reflection;

namespace FractalExplorer
{
    /// <summary>
    /// Главная форма приложения, служащая хабом для запуска различных фрактальных форм.
    /// </summary>
    public partial class LauncherHubForm : Form
    {
        // Вспомогательный класс для хранения информации о каждом фрактале.
        private class FractalInfo
        {
            public string DisplayName { get; set; }
            public string Family { get; set; }
            public Type FormToLaunch { get; set; }
            public string Description { get; set; }
            // Имя картинки-превью из ресурсов проекта (Properties.Resources)
            public Image PreviewImage { get; set; }
        }

        // Каталог всех доступных фракталов.
        private readonly List<FractalInfo> _fractalCatalog;
        // Текущий выбранный фрактал.
        private FractalInfo _selectedFractal;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="LauncherHubForm"/>.
        /// </summary>
        public LauncherHubForm()
        {
            InitializeComponent();
            _fractalCatalog = new List<FractalInfo>();

            InitializeFractalCatalog(); // Заполняем наш каталог
            PopulateTreeView();         // Заполняем дерево на основе каталога
            DisplayAppVersionInTitle();
        }

        /// <summary>
        /// Здесь мы определяем все наши фракталы.
        /// !!! ЧТОБЫ ДОБАВИТЬ НОВЫЙ ФРАКТАЛ, НУЖНО ДОБАВИТЬ СТРОЧКУ ТОЛЬКО СЮДА !!!
        /// </summary>
        private void InitializeFractalCatalog()
        {
            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Множество Мандельброта",
                DisplayName = "Классический Мандельброт",
                FormToLaunch = typeof(FractalMondelbrot),
                Description = "Иконическое множество, определяемое простой рекуррентной формулой. Является картой всех множеств Жюлиа.",
                //PreviewImage = Properties.Resources.mandelbrot_preview // Убедитесь, что у вас есть этот ресурс
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Множество Мандельброта",
                DisplayName = "Горящий Корабль",
                FormToLaunch = typeof(FractalMondelbrotBurningShip),
                Description = "Модификация классического алгоритма, где используется модуль комплексного числа. Создает структуры, похожие на горящий корабль в дыму.",
                //PreviewImage = Properties.Resources.burningship_preview // И этот
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Множество Мандельброта",
                DisplayName = "Обобщенный Мандельброт",
                FormToLaunch = typeof(FractalGeneralizedMandelbrot),
                Description = "Вариация множества Мандельброта, где используется произвольная степень Z, а не только квадрат. Позволяет исследовать бесконечное разнообразие форм.",
                //PreviewImage = Properties.Resources.mandelbrot_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Множество Жюлиа",
                DisplayName = "Классическое Жюлиа",
                FormToLaunch = typeof(FractalJulia),
                Description = "Множества, тесно связанные с множеством Мандельброта. Для каждой точки C из множества Мандельброта существует своё уникальное множество Жюлиа.",
                //PreviewImage = Properties.Resources.julia_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Множество Жюлиа",
                DisplayName = "Горящий Корабль (Жюлиа)",
                FormToLaunch = typeof(FractalJuliaBurningShip),
                Description = "Соответствующее множество Жюлиа для фрактала 'Горящий корабль'. Также использует модуль компонент комплексного числа.",
                //PreviewImage = Properties.Resources.burningship_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Итерируемые функции",
                DisplayName = "Бассейны Ньютона",
                FormToLaunch = typeof(NewtonPools),
                Description = "Фрактал, получаемый применением метода Ньютона для нахождения корней комплексного многочлена. Разные цвета показывают, к какому корню сходится точка.",
                //PreviewImage = Properties.Resources.newton_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Итерируемые функции",
                DisplayName = "Фрактал Феникс",
                FormToLaunch = typeof(FractalPhoenixForm),
                Description = "Обобщение множества Жюлиа, включающее в рекуррентную формулу предыдущее значение Z. Создает уникальные, похожие на вихри структуры.",
                //PreviewImage = Properties.Resources.phoenix_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Геометрические",
                DisplayName = "Треугольник Серпинского",
                FormToLaunch = typeof(FractalSerpinski),
                Description = "Классический геометрический фрактал, который можно построить как рекурсивным удалением треугольников, так и методом 'игры в хаос'.",
                //PreviewImage = Properties.Resources.serpinski_preview
            });
        }

        /// <summary>
        /// Заполняет TreeView на основе каталога фракталов.
        /// </summary>
        private void PopulateTreeView()
        {
            treeViewFractals.BeginUpdate();
            treeViewFractals.Nodes.Clear();

            var familyNodes = new Dictionary<string, TreeNode>();

            foreach (var fractal in _fractalCatalog)
            {
                if (!familyNodes.ContainsKey(fractal.Family))
                {
                    var newFamilyNode = new TreeNode(fractal.Family);
                    familyNodes[fractal.Family] = newFamilyNode;
                    treeViewFractals.Nodes.Add(newFamilyNode);
                }

                var fractalNode = new TreeNode(fractal.DisplayName)
                {
                    // Сохраняем всю информацию о фрактале в теге узла. Это ключ к успеху!
                    Tag = fractal
                };

                familyNodes[fractal.Family].Nodes.Add(fractalNode);
            }

            treeViewFractals.ExpandAll();
            treeViewFractals.EndUpdate();
        }

        /// <summary>
        /// Обновляет правую панель при выборе узла в дереве.
        /// </summary>
        private void treeViewFractals_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Если выбран узел фрактала (а не семейства)
            if (e.Node.Tag is FractalInfo selected)
            {
                _selectedFractal = selected;
                lblFractalName.Text = selected.DisplayName;
                richTextBoxDescription.Text = selected.Description;
                pictureBoxPreview.Image = selected.PreviewImage;
                btnLaunchSelected.Enabled = true;
            }
            else // Выбрана категория
            {
                _selectedFractal = null;
                lblFractalName.Text = "Выберите фрактал";
                richTextBoxDescription.Text = "Выберите конкретный фрактал из списка слева, чтобы увидеть его описание и запустить.";
                pictureBoxPreview.Image = null;
                btnLaunchSelected.Enabled = false;
            }
        }

        /// <summary>
        /// Запускает форму для выбранного фрактала.
        /// </summary>
        private void btnLaunchSelected_Click(object sender, EventArgs e)
        {
            if (_selectedFractal?.FormToLaunch == null) return;

            // Используем Activator для создания экземпляра формы по её типу.
            if (Activator.CreateInstance(_selectedFractal.FormToLaunch) is Form form)
            {
                form.Show();
            }
        }

        #region Version Display
        private void DisplayAppVersionInTitle()
        {
            string appVersion = GetAppVersion();
            this.Text = $"{this.Text} - Версия: {appVersion}";
        }

        private string GetAppVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var informationalVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (informationalVersionAttribute != null && !string.IsNullOrWhiteSpace(informationalVersionAttribute.InformationalVersion))
            {
                return informationalVersionAttribute.InformationalVersion;
            }
            var fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (fileVersionAttribute != null && !string.IsNullOrWhiteSpace(fileVersionAttribute.Version))
            {
                return fileVersionAttribute.Version;
            }
            Version version = assembly.GetName().Version;
            return version?.ToString() ?? "неизвестно";
        }
        #endregion
    }
}