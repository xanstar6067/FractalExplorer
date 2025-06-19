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

        private void btnLaunchBurningShipComplex_Click(object sender, EventArgs e)
        {
            var form = new FractalburningShipJulia();
            form.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var form = new FractalMondelbrotShip();
            form.Show();
        }
    }
}

