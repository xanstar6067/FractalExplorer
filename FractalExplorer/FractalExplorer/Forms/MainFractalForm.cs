using FractalDraving;
using FractalExplorer.Projects;


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
            var form = new FractalMondelbrot();
            form.Show();
        }

        private void btnLaunchJulia_Click(object sender, EventArgs e)
        {
            var form = new FractalJulia();
            form.Show();
        }

        private void btnLaunchSerpinsky_Click(object sender, EventArgs e)
        {
            var form = new FractalSerpinsky();
            form.Show();
        }

        private void btnLaunchNewton_Click(object sender, EventArgs e)
        {
            var form = new NewtonPools();
            form.Show();
        }

        private void btnLaunchBurningShipJulia_Click(object sender, EventArgs e)
        {
            var form = new FractalJuliaBurningShip();
            form.Show();
        }

        private void btnLaunchBurningShipMandelbrot_Click(object sender, EventArgs e)
        {
            var form = new FractalMondelbrotBurningShip();
            form.Show();
        }
    }
}

