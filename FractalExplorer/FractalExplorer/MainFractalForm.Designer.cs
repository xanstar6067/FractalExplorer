namespace FractalExplorer
{
    partial class MainFractalForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabControlFractals = new TabControl();
            tabPageMandelbrot = new TabPage();
            tabPageJulia = new TabPage();
            btnLaunchMondelbrot = new Button();
            btnLaunchJulia = new Button();
            tabControlFractals.SuspendLayout();
            tabPageMandelbrot.SuspendLayout();
            tabPageJulia.SuspendLayout();
            SuspendLayout();
            // 
            // tabControlFractals
            // 
            tabControlFractals.Controls.Add(tabPageMandelbrot);
            tabControlFractals.Controls.Add(tabPageJulia);
            tabControlFractals.Dock = DockStyle.Fill;
            tabControlFractals.Location = new Point(0, 0);
            tabControlFractals.Name = "tabControlFractals";
            tabControlFractals.SelectedIndex = 0;
            tabControlFractals.Size = new Size(800, 191);
            tabControlFractals.TabIndex = 0;
            // 
            // tabPageMandelbrot
            // 
            tabPageMandelbrot.Controls.Add(btnLaunchMondelbrot);
            tabPageMandelbrot.Location = new Point(4, 24);
            tabPageMandelbrot.Name = "tabPageMandelbrot";
            tabPageMandelbrot.Padding = new Padding(3);
            tabPageMandelbrot.Size = new Size(792, 163);
            tabPageMandelbrot.TabIndex = 0;
            tabPageMandelbrot.Text = "Множество Мандельброта";
            tabPageMandelbrot.UseVisualStyleBackColor = true;
            // 
            // tabPageJulia
            // 
            tabPageJulia.Controls.Add(btnLaunchJulia);
            tabPageJulia.Location = new Point(4, 24);
            tabPageJulia.Name = "tabPageJulia";
            tabPageJulia.Padding = new Padding(3);
            tabPageJulia.Size = new Size(792, 163);
            tabPageJulia.TabIndex = 1;
            tabPageJulia.Text = "Множество Жюлиа";
            tabPageJulia.UseVisualStyleBackColor = true;
            // 
            // btnLaunchMondelbrot
            // 
            btnLaunchMondelbrot.Location = new Point(8, 8);
            btnLaunchMondelbrot.Name = "btnLaunchMondelbrot";
            btnLaunchMondelbrot.Size = new Size(75, 23);
            btnLaunchMondelbrot.TabIndex = 0;
            btnLaunchMondelbrot.Text = "button1";
            btnLaunchMondelbrot.UseVisualStyleBackColor = true;
            btnLaunchMondelbrot.Click += this.btnLaunchMondelbrot_Click;
            // 
            // btnLaunchJulia
            // 
            btnLaunchJulia.Location = new Point(8, 8);
            btnLaunchJulia.Name = "btnLaunchJulia";
            btnLaunchJulia.Size = new Size(75, 23);
            btnLaunchJulia.TabIndex = 0;
            btnLaunchJulia.Text = "button2";
            btnLaunchJulia.UseVisualStyleBackColor = true;
            btnLaunchJulia.Click += this.btnLaunchJulia_Click;
            // 
            // MainFractalForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 191);
            Controls.Add(tabControlFractals);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainFractalForm";
            Text = "Менеджер фракталов";
            tabControlFractals.ResumeLayout(false);
            tabPageMandelbrot.ResumeLayout(false);
            tabPageJulia.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControlFractals;
        private TabPage tabPageMandelbrot;
        private TabPage tabPageJulia;
        private Button btnLaunchMondelbrot;
        private Button btnLaunchJulia;
    }
}
