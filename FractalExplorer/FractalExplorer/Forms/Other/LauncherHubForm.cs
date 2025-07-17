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
                Description = "Иконический фрактал, определяемый простой формулой Z = Z² + C.\n\n" +
                              "Он представляет собой множество всех комплексных чисел C, для которых итерация не уходит в бесконечность. Является картой всех связанных множеств Жюлиа и славится бесконечной сложностью своей границы.\n\n" +
                              "Особенности: Исследуйте фрактал с огромным приближением, настраивайте градиентные палитры с помощью плавного окрашивания и сохраняйте изображения в высоком разрешении.",
                PreviewImage = Properties.Resources.mandelbrot_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Множество Мандельброта",
                DisplayName = "Горящий Корабль",
                FormToLaunch = typeof(FractalMondelbrotBurningShip),
                Description = "Поразительная модификация алгоритма Мандельброта, где перед возведением в квадрат берутся абсолютные значения компонент: Z = (|Re(Z)| + i|Im(Z)|)² + C.\n\n" +
                              "Это нарушение симметрии создает совершенно иные, более хаотичные структуры, напоминающие горящий корабль и клубы дыма.\n\n" +
                              "Особенности: Откройте для себя уникальные детали в 'палубах' и 'мачтах' корабля. Полный контроль над цветами и возможность сохранения позволят создать настоящий шедевр.",
                PreviewImage = Properties.Resources.burningship_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Множество Мандельброта",
                DisplayName = "Буффало",
                FormToLaunch = typeof(FractalBuffalo),
                Description = "Вариация множества Мандельброта, которая использует абсолютные значения компонент Z перед возведением в квадрат: Z = (|Re(Z)| - i|Im(Z)|)² + C.\n\n" +
                             "Результатом является симметричный фрактал, напоминающий жука или быка, с уникальными и менее 'шумными' структурами по сравнению с 'Горящим Кораблем'.\n\n" +
                             "Особенности: Исследуйте гладкие, органические формы этого фрактала. Все возможности по настройке палитры и сохранению также доступны.",
                PreviewImage = Properties.Resources.buffalo_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Множество Мандельброта",
                DisplayName = "Симоноброт",
                FormToLaunch = typeof(FractalSimonobrot),
                Description = "Необычный фрактал, определяемый формулой Z = |Z|^p + C, где |Z| - это модуль комплексного числа (действительное число).\n\n" +
                              "Это приводит к совершенно другим структурам, часто с радиальной симметрией и интересными 'лучами'.\n\n" +
                              "Особенности: Экспериментируйте со степенью 'p' (включая отрицательные значения) и используйте опцию инверсии для получения зеркального отражения фрактала.",
                PreviewImage = Properties.Resources.simonobrot_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Множество Мандельброта",
                DisplayName = "Обобщенный Мандельброт",
                FormToLaunch = typeof(FractalGeneralizedMandelbrot),
                Description = "Вариация множества Мандельброта, где используется произвольная степень 'p', а не только квадрат: Z = Z^p + C.\n\n" +
                              "Изменение степени кардинально меняет форму фрактала, создавая так называемые 'мультиброты'.\n\n" +
                              "Особенности: Экспериментируйте с различными степенями, чтобы исследовать бесконечное разнообразие форм. Все возможности по настройке палитры и сохранению также доступны.",
                PreviewImage = Properties.Resources.general_mandelbrot_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Множество Жюлиа",
                DisplayName = "Классическое Жюлиа",
                FormToLaunch = typeof(FractalJulia),
                Description = "Множества, тесно связанные с фракталом Мандельброта. Здесь константа C остается неизменной для всего изображения, а итерируется начальная точка Z₀.\n\n" +
                              "Для каждой точки C из множества Мандельброта существует своё уникальное и красивое множество Жюлиа.\n\n" +
                              "Особенности: Интерактивно задавайте константу C, чтобы исследовать различные множества. Настраивайте цветовую палитру и сохраняйте свои находки.",
                PreviewImage = Properties.Resources.julia_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Множество Жюлиа",
                DisplayName = "Горящий Корабль (Жюлиа)",
                FormToLaunch = typeof(FractalJuliaBurningShip),
                Description = "Соответствующее множество Жюлиа для фрактала 'Горящий корабль'. Также использует итерацию с модулями компонент и постоянной константой C.\n\n" +
                              "Генерирует уникальные асимметричные узоры, сохраняя хаотичный характер своего 'родителя'.\n\n" +
                              "Особенности: Выбирайте константу C и погружайтесь в исследование удивительных и непредсказуемых паттернов. Сохраняйте лучшие результаты в высоком качестве.",
                PreviewImage = Properties.Resources.julia_burningship_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Итерируемые функции",
                DisplayName = "Бассейны Ньютона",
                FormToLaunch = typeof(NewtonPools),
                Description = "Фрактал, визуализирующий работу метода Ньютона для нахождения корней комплексного многочлена. Формула итерации: Z_n+1 = Z_n - f(Z_n) / f'(Z_n).\n\n" +
                              "Разные цвета показывают 'бассейны притяжения' — области, точки из которых сходятся к одному и тому же корню. Границы между этими бассейнами и являются фракталом.\n\n" +
                              "Особенности: Вводите собственные полиномы, настраивайте цвета для каждого корня и исследуйте сложную динамику на комплексной плоскости.",
                PreviewImage = Properties.Resources.newton_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Итерируемые функции",
                DisplayName = "Фрактал Феникс",
                FormToLaunch = typeof(FractalPhoenixForm),
                Description = "Обобщение множества Жюлиа, формула которого включает не только текущее, но и предыдущее значение Z: Z_n+1 = Z_n² + C₁ + C₂*Z_n-1.\n\n" +
                              "Наличие двух констант и 'памяти' о предыдущем шаге создает невероятно сложные и красивые вихревые структуры, похожие на перья мифической птицы.\n\n" +
                              "Особенности: Исследуйте огромное пространство параметров C₁ и C₂, чтобы найти уникальные вариации. Настраивайте палитры и сохраняйте изображения.",
                PreviewImage = Properties.Resources.phoenix_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Геометрические",
                DisplayName = "Треугольник Серпинского",
                FormToLaunch = typeof(FractalSerpinski),
                Description = "Классический геометрический фрактал с простой, но элегантной структурой. Обладает строгим свойством самоподобия.\n\n" +
                              "Может быть построен двумя методами: рекурсивным удалением центральных треугольников (геометрический) или стохастическим методом 'игры в хаос'.\n\n" +
                              "Особенности: Переключайтесь между двумя методами построения, настраивайте цвета фрактала и фона, сохраняйте результат.",
                PreviewImage = Properties.Resources.serpinski_preview
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