using FractalDraving;

namespace FractalExplorer
{
    public partial class MainFractalForm : Form
    {
        public MainFractalForm()
        {
            InitializeComponent();
        }

        private void MainFractalForm_Load(object sender, EventArgs e)
        {

        }

        private void MainFractalForm_Load_1(object sender, EventArgs e)
        {

        }

        private void btnLaunchMondelbrot_Click(object sender, EventArgs e)
        {
            FractalMondelbrot FractalMondelbrotForm = new FractalMondelbrot();

            FractalMondelbrotForm.Show();
        }

        private void btnLaunchJulia_Click(object sender, EventArgs e)
        {
            // 1. Создаем экземпляр вашей формы FractalJulia
            FractalJulia juliaForm = new FractalJulia();

            // 2. Показываем форму
            // Есть два основных способа:

            // Способ А: Показать как немодальное окно (пользователь может взаимодействовать и с LauncherForm)
            juliaForm.Show();

            // Способ Б: Показать как модальное окно (LauncherForm будет заблокирована, пока juliaForm открыта)
            // juliaForm.ShowDialog();

            // Выберите один из способов (Show() или ShowDialog()) и закомментируйте или удалите другой.
            // Обычно для таких случаев лучше Show(), чтобы пользователь мог запустить оба фрактала одновременно.
        }

        private void btnLaunchSerpinsky_Click(object sender, EventArgs e)
        {
            // FractalSerpinsky
            FractalSerpinsky FractalSerpinskyForm = new FractalSerpinsky();

            FractalSerpinskyForm.Show();

        }

        private void btnLaunchNewton_Click(object sender, EventArgs e)
        {
            // FractalNewtonPool
            NewtonPools NewtonPpoolsForm = new NewtonPools();

            NewtonPpoolsForm.Show();
        }
    }
}
