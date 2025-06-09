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
            // 1. ������� ��������� ����� ����� FractalJulia
            FractalJulia juliaForm = new FractalJulia();

            // 2. ���������� �����
            // ���� ��� �������� �������:

            // ������ �: �������� ��� ����������� ���� (������������ ����� ����������������� � � LauncherForm)
            juliaForm.Show();

            // ������ �: �������� ��� ��������� ���� (LauncherForm ����� �������������, ���� juliaForm �������)
            // juliaForm.ShowDialog();

            // �������� ���� �� �������� (Show() ��� ShowDialog()) � ��������������� ��� ������� ������.
            // ������ ��� ����� ������� ����� Show(), ����� ������������ ��� ��������� ��� �������� ������������.
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
