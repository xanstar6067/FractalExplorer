using FractalExplorer.Utilities.Theme;

namespace FractalExplorer.Forms.Other
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            ThemeManager.RegisterForm(this);
        }
    }
}
