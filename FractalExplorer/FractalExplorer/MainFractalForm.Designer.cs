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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainFractalForm));
            tabPageJulia = new TabPage();
            richTextBox2 = new RichTextBox();
            btnLaunchJulia = new Button();
            tabPageMandelbrot = new TabPage();
            richTextBox1 = new RichTextBox();
            btnLaunchMondelbrot = new Button();
            tabControlFractals = new TabControl();
            tabPageJulia.SuspendLayout();
            tabPageMandelbrot.SuspendLayout();
            tabControlFractals.SuspendLayout();
            SuspendLayout();
            // 
            // tabPageJulia
            // 
            tabPageJulia.Controls.Add(richTextBox2);
            tabPageJulia.Controls.Add(btnLaunchJulia);
            tabPageJulia.Location = new Point(4, 26);
            tabPageJulia.Name = "tabPageJulia";
            tabPageJulia.Padding = new Padding(3);
            tabPageJulia.Size = new Size(792, 161);
            tabPageJulia.TabIndex = 1;
            tabPageJulia.Text = "Множество Жюлиа";
            tabPageJulia.UseVisualStyleBackColor = true;
            // 
            // richTextBox2
            // 
            richTextBox2.BackColor = SystemColors.Window;
            richTextBox2.BorderStyle = BorderStyle.None;
            richTextBox2.Location = new Point(184, 3);
            richTextBox2.Name = "richTextBox2";
            richTextBox2.ReadOnly = true;
            richTextBox2.Size = new Size(605, 157);
            richTextBox2.TabIndex = 3;
            richTextBox2.Text = resources.GetString("richTextBox2.Text");
            // 
            // btnLaunchJulia
            // 
            btnLaunchJulia.Location = new Point(8, 15);
            btnLaunchJulia.Name = "btnLaunchJulia";
            btnLaunchJulia.Size = new Size(131, 44);
            btnLaunchJulia.TabIndex = 0;
            btnLaunchJulia.Text = "Запустить";
            btnLaunchJulia.UseVisualStyleBackColor = true;
            btnLaunchJulia.Click += btnLaunchJulia_Click;
            // 
            // tabPageMandelbrot
            // 
            tabPageMandelbrot.Controls.Add(richTextBox1);
            tabPageMandelbrot.Controls.Add(btnLaunchMondelbrot);
            tabPageMandelbrot.Location = new Point(4, 26);
            tabPageMandelbrot.Name = "tabPageMandelbrot";
            tabPageMandelbrot.Padding = new Padding(3);
            tabPageMandelbrot.Size = new Size(792, 161);
            tabPageMandelbrot.TabIndex = 0;
            tabPageMandelbrot.Text = "Множество Мандельброта";
            tabPageMandelbrot.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.BackColor = SystemColors.Window;
            richTextBox1.BorderStyle = BorderStyle.None;
            richTextBox1.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 204);
            richTextBox1.Location = new Point(184, 3);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new Size(605, 157);
            richTextBox1.TabIndex = 2;
            richTextBox1.Text = resources.GetString("richTextBox1.Text");
            // 
            // btnLaunchMondelbrot
            // 
            btnLaunchMondelbrot.Location = new Point(8, 15);
            btnLaunchMondelbrot.Name = "btnLaunchMondelbrot";
            btnLaunchMondelbrot.Size = new Size(131, 44);
            btnLaunchMondelbrot.TabIndex = 0;
            btnLaunchMondelbrot.Text = "Запустить";
            btnLaunchMondelbrot.UseVisualStyleBackColor = true;
            btnLaunchMondelbrot.Click += btnLaunchMondelbrot_Click;
            // 
            // tabControlFractals
            // 
            tabControlFractals.Controls.Add(tabPageMandelbrot);
            tabControlFractals.Controls.Add(tabPageJulia);
            tabControlFractals.Dock = DockStyle.Fill;
            tabControlFractals.Font = new Font("Segoe UI", 10F);
            tabControlFractals.Location = new Point(0, 0);
            tabControlFractals.Name = "tabControlFractals";
            tabControlFractals.SelectedIndex = 0;
            tabControlFractals.Size = new Size(800, 191);
            tabControlFractals.TabIndex = 0;
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
            Load += MainFractalForm_Load_1;
            tabPageJulia.ResumeLayout(false);
            tabPageMandelbrot.ResumeLayout(false);
            tabControlFractals.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TabPage tabPageJulia;
        private Button btnLaunchJulia;
        private TabPage tabPageMandelbrot;
        private Button btnLaunchMondelbrot;
        private TabControl tabControlFractals;
        private RichTextBox richTextBox1;
        private RichTextBox richTextBox2;
    }
}
