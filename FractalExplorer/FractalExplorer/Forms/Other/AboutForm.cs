using FractalExplorer.Utilities.Theme;
using System.Diagnostics;

namespace FractalExplorer.Forms.Other
{
    public partial class AboutForm : Form
    {
        private const string RepositoryUrl = "https://github.com/xanstar6067/Fractal-Explorer-studio";

        public AboutForm()
        {
            InitializeComponent();
            ThemeManager.RegisterForm(this);
        }

        private void LinkRepository_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = RepositoryUrl,
                UseShellExecute = true
            });
        }
    }
}
