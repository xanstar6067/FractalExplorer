using System;
using System.Windows.Forms; // �������� using ��� System.Windows.Forms, ���� �� �� ��� �������� �������������
using FractalDraving;
using FractalExplorer.Projects;


namespace FractalExplorer
{
    /// <summary>
    /// ������� ����� ����������, �������� ����� ��� ������� ��������� ����������� ����.
    /// </summary>
    public partial class MainFractalForm : Form
    {
        /// <summary>
        /// �������������� ����� ��������� ������ <see cref="MainFractalForm"/>.
        /// </summary>
        public MainFractalForm()
        {
            InitializeComponent();
        }

        #region Event Handlers

        /// <summary>
        /// ���������� ������� �������� �����.
        /// </summary>
        /// <param name="sender">�������� �������.</param>
        /// <param name="e">��������� �������.</param>
        private void MainFractalForm_Load(object sender, EventArgs e)
        {
            // ���� ����� ����� ���� ������, ���� �� ��������� ������� ������������� ��� ��������.
        }

        /// <summary>
        /// ���������� ������� �������� �����, ��������������� ����������.
        /// (�������-��������, �� �������).
        /// </summary>
        /// <param name="sender">�������� �������.</param>
        /// <param name="e">��������� �������.</param>
        private void MainFractalForm_Load_1(object sender, EventArgs e)
        {
            // �������� ��� ��������, ��� ������� � ���������.
        }

        /// <summary>
        /// ���������� ������� ����� �� ������ "Launch Mondelbrot".
        /// ��������� ����� ����� ��� �������� ������������.
        /// </summary>
        /// <param name="sender">�������� �������.</param>
        /// <param name="e">��������� �������.</param>
        private void btnLaunchMondelbrot_Click(object sender, EventArgs e)
        {
            var form = new FractalMondelbrot();
            form.Show();
        }

        /// <summary>
        /// ���������� ������� ����� �� ������ "Launch Julia".
        /// ��������� ����� ����� ��� �������� �����.
        /// </summary>
        /// <param name="sender">�������� �������.</param>
        /// <param name="e">��������� �������.</param>
        private void btnLaunchJulia_Click(object sender, EventArgs e)
        {
            var form = new FractalJulia();
            form.Show();
        }

        /// <summary>
        /// ���������� ������� ����� �� ������ "Launch Serpinsky".
        /// ��������� ����� ����� ��� �������� �����������.
        /// </summary>
        /// <param name="sender">�������� �������.</param>
        /// <param name="e">��������� �������.</param>
        private void btnLaunchSerpinsky_Click(object sender, EventArgs e)
        {
            var form = new FractalSerpinsky();
            form.Show();
        }

        /// <summary>
        /// ���������� ������� ����� �� ������ "Launch Newton".
        /// ��������� ����� ����� ��� �������� �������.
        /// </summary>
        /// <param name="sender">�������� �������.</param>
        /// <param name="e">��������� �������.</param>
        private void btnLaunchNewton_Click(object sender, EventArgs e)
        {
            var form = new NewtonPools();
            form.Show();
        }

        /// <summary>
        /// ���������� ������� ����� �� ������ "Launch Burning Ship Julia".
        /// ��������� ����� ����� ��� �������� "�������� �������" �����.
        /// </summary>
        /// <param name="sender">�������� �������.</param>
        /// <param name="e">��������� �������.</param>
        private void btnLaunchBurningShipJulia_Click(object sender, EventArgs e)
        {
            var form = new FractalJuliaBurningShip();
            form.Show();
        }

        /// <summary>
        /// ���������� ������� ����� �� ������ "Launch Burning Ship Mandelbrot".
        /// ��������� ����� ����� ��� �������� "�������� �������" ������������.
        /// </summary>
        /// <param name="sender">�������� �������.</param>
        /// <param name="e">��������� �������.</param>
        private void btnLaunchBurningShipMandelbrot_Click(object sender, EventArgs e)
        {
            var form = new FractalMondelbrotBurningShip();
            form.Show();
        }

        #endregion
    }
}