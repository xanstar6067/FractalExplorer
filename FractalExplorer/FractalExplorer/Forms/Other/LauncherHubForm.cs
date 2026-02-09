using FractalExplorer.Forms;
using FractalExplorer.Forms.Fractals;
using FractalExplorer.Projects;
using System.Reflection;

namespace FractalExplorer
{
    /// <summary>
    /// Главная форма приложения, служащая хабом для запуска различных фрактальных форм.
    /// </summary>
    public partial class LauncherHubForm : Form
    {
        /// <summary>
        /// Вспомогательный класс для хранения информации о каждом фрактале, доступном в приложении.
        /// </summary>
        private class FractalInfo
        {
            /// <summary>
            /// Отображаемое имя фрактала.
            /// </summary>
            public string DisplayName { get; set; }
            /// <summary>
            /// Категория или семейство, к которому принадлежит фрактал.
            /// </summary>
            public string Family { get; set; }
            /// <summary>
            /// Тип формы (<see cref="Form"/>), которую нужно запустить для этого фрактала.
            /// </summary>
            public Type FormToLaunch { get; set; }
            /// <summary>
            /// Подробное описание фрактала, его особенностей и математической основы.
            /// </summary>
            public string Description { get; set; }
            /// <summary>
            /// Изображение для предпросмотра, загруженное из ресурсов проекта.
            /// </summary>
            public Image PreviewImage { get; set; }
        }

        /// <summary>
        /// Каталог всех доступных в приложении фракталов.
        /// </summary>
        private readonly List<FractalInfo> _fractalCatalog;

        /// <summary>
        /// Текущий фрактал, выбранный пользователем в дереве.
        /// </summary>
        private FractalInfo _selectedFractal;

        /// <summary>
        /// Таймер для периодического обновления информации о системе в UI.
        /// </summary>
        private readonly System.Windows.Forms.Timer _uiUpdateTimer;

        /// <summary>
        /// Флаг, предотвращающий одновременный запуск нескольких обновлений.
        /// </summary>
        private bool _isUpdatingInfo = false;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="LauncherHubForm"/>.
        /// </summary>
        public LauncherHubForm()
        {
            InitializeComponent();
            _fractalCatalog = new List<FractalInfo>();

            InitializeFractalCatalog();
            PopulateTreeView();
            DisplayAppVersionInTitle();
        }


        /// <summary>
        /// Инициализирует каталог фракталов, добавляя информацию о каждом из них.
        /// <br/><b>Чтобы добавить новый фрактал, необходимо добавить новую запись только в этот метод.</b>
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
                Family = "Итерируемые функции",
                DisplayName = "Фрактал Коллатца",
                FormToLaunch = typeof(FractalCollatzForm),
                Description = "Представляет собой обобщение знаменитой гипотезы Коллатца (проблемы 3n+1) на комплексную плоскость. Итерационная формула: Z_next = 0.25 * (2 + 7*Z - (2 + 5*Z) * cos(πZ)).\n\n" +
                              "Гипотеза утверждает, что последовательность 3n+1 для любого целого числа в итоге придет к циклу 4-2-1. Этот фрактал визуализирует хаотичное и непредсказуемое поведение этой, казалось бы, простой идеи.\n\n" +
                              "В результате получается уникальная, бесконечно детализированная паутинообразная структура, не похожая ни на один другой фрактал.\n\n" +
                              "Особенности: Исследуйте сложную структуру фрактала с помощью глубокого масштабирования, настраивайте цветовые схемы и сохраняйте полученные изображения в высоком разрешении.",
                PreviewImage = Properties.Resources.collatz_preview
            });

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Итерируемые функции",
                DisplayName = "Фрактал Нова (Мандельброт)",
                FormToLaunch = typeof(FractalNovaMandelbrotForm),
                Description = "Мощное обобщение фрактала Ньютона, которое добавляет в итерационную формулу константу C по аналогии с множеством Мандельброта. Формула: Z_n+1 = Z_n - m * (Z_n^p - 1) / (p*Z_n^(p-1)) + C.\n\n" +
                              "Этот фрактал знаменит своей гибкостью. Варьируя степень 'p' (в том числе делая ее комплексной), начальное значение Z₀ и коэффициент релаксации 'm', можно получить бесконечное разнообразие форм — от симметричных звезд до невероятно сложных спиральных галактик.\n\n" +
                              "Особенности: Полный контроль над уникальными параметрами Nova. Исследуйте, как мнимая часть степени 'p' закручивает фрактал в потрясающие спирали. Все возможности по настройке палитры и сохранению также доступны.",
                PreviewImage = Properties.Resources.NovaMandelbrot_preview


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

            _fractalCatalog.Add(new FractalInfo
            {
                Family = "Итерируемые функции",
                DisplayName = "Фрактал Нова (Жюлиа)",
                FormToLaunch = typeof(FractalNovaJuliaForm), // Используем нашу новую форму
                Description = "Двойственная версия фрактала Нова, аналогичная множеству Жюлиа для классического Мандельброта. Здесь константа C фиксирована, а итерируется начальная точка Z.\n\n" +
                      "Каждой точке на карте Нова-Мандельброта соответствует свое уникальное множество Нова-Жюлиа. Это позволяет исследовать глубокую связь между двумя этими множествами.\n\n" +
                      "Особенности: Используйте встроенную карту для выбора константы C и находите удивительные формы — от дендритов до замкнутых островов. Доступны все настройки параметров P, M и Z₀.",
                PreviewImage = Properties.Resources.NovaJulia_preview 
            });
        }

        /// <summary>
        /// Заполняет элемент управления <see cref="TreeView"/> на основе каталога фракталов, группируя их по семействам.
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
        /// Обрабатывает событие выбора узла в дереве фракталов и обновляет панель информации.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
        private void treeViewFractals_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Если выбран узел фрактала (а не семейства), его Tag будет содержать объект FractalInfo.
            if (e.Node.Tag is FractalInfo selected)
            {
                _selectedFractal = selected;
                lblFractalName.Text = selected.DisplayName;
                richTextBoxDescription.Text = selected.Description;
                pictureBoxPreview.Image = selected.PreviewImage;
                btnLaunchSelected.Enabled = true;
            }
            else // Выбран узел категории.
            {
                _selectedFractal = null;
                lblFractalName.Text = "Выберите фрактал";
                richTextBoxDescription.Text = "Выберите конкретный фрактал из списка слева, чтобы увидеть его описание и запустить.";
                pictureBoxPreview.Image = Properties.Resources.base_img_CHAT_GPT_01;
                btnLaunchSelected.Enabled = false;
            }
        }

        /// <summary>
        /// Обрабатывает нажатие на кнопку "Запустить" и открывает форму для выбранного фрактала.
        /// </summary>
        /// <param name="sender">Источник события.</param>
        /// <param name="e">Данные события.</param>
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

        /// <summary>
        /// Отображает версию приложения в заголовке главной формы.
        /// </summary>
        private void DisplayAppVersionInTitle()
        {
            string appVersion = GetAppVersion();
            this.Text = $"{this.Text} - Версия: {appVersion}";
        }

        /// <summary>
        /// Получает версию приложения из атрибутов сборки.
        /// </summary>
        /// <returns>Строка с версией приложения или "неизвестно", если версия не найдена.</returns>
        private string GetAppVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Предпочтительно использовать InformationalVersion, так как он может содержать семантическую версию (например, "1.2.3-beta").
            var informationalVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (informationalVersionAttribute != null && !string.IsNullOrWhiteSpace(informationalVersionAttribute.InformationalVersion))
            {
                return informationalVersionAttribute.InformationalVersion;
            }

            // В качестве запасного варианта используется FileVersion.
            var fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (fileVersionAttribute != null && !string.IsNullOrWhiteSpace(fileVersionAttribute.Version))
            {
                return fileVersionAttribute.Version;
            }

            // Самый крайний случай - версия сборки.
            Version version = assembly.GetName().Version;
            return version?.ToString() ?? "неизвестно";
        }
        #endregion
    }
}