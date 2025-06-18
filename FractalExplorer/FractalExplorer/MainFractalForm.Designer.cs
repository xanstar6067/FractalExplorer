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
            btnLaunchBurningShipComplex = new Button();
            richTextBox2 = new RichTextBox();
            btnLaunchJulia = new Button();
            tabPageMandelbrot = new TabPage();
            richTextBox1 = new RichTextBox();
            btnLaunchMondelbrot = new Button();
            tabControlFractals = new TabControl();
            tabPageSerpinsky = new TabPage();
            richTextBox3 = new RichTextBox();
            btnLaunchSerpinsky = new Button();
            tabPageNewtonPools = new TabPage();
            richTextBox4 = new RichTextBox();
            btnLaunchNewton = new Button();
            button1 = new Button();
            tabPageJulia.SuspendLayout();
            tabPageMandelbrot.SuspendLayout();
            tabControlFractals.SuspendLayout();
            tabPageSerpinsky.SuspendLayout();
            tabPageNewtonPools.SuspendLayout();
            SuspendLayout();
            // 
            // tabPageJulia
            // 
            tabPageJulia.Controls.Add(btnLaunchBurningShipComplex);
            tabPageJulia.Controls.Add(richTextBox2);
            tabPageJulia.Controls.Add(btnLaunchJulia);
            tabPageJulia.Location = new Point(4, 26);
            tabPageJulia.Name = "tabPageJulia";
            tabPageJulia.Padding = new Padding(3);
            tabPageJulia.Size = new Size(792, 195);
            tabPageJulia.TabIndex = 1;
            tabPageJulia.Text = "Множество Жюлиа";
            tabPageJulia.UseVisualStyleBackColor = true;
            // 
            // btnLaunchBurningShipComplex
            // 
            btnLaunchBurningShipComplex.Location = new Point(8, 76);
            btnLaunchBurningShipComplex.Name = "btnLaunchBurningShipComplex";
            btnLaunchBurningShipComplex.Size = new Size(131, 54);
            btnLaunchBurningShipComplex.TabIndex = 4;
            btnLaunchBurningShipComplex.Text = "Запустить\r\nГорящий корабль";
            btnLaunchBurningShipComplex.UseVisualStyleBackColor = true;
            btnLaunchBurningShipComplex.Click += btnLaunchBurningShipComplex_Click;
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
            btnLaunchJulia.Size = new Size(131, 55);
            btnLaunchJulia.TabIndex = 0;
            btnLaunchJulia.Text = "Запустить\r\nЖюлиа";
            btnLaunchJulia.UseVisualStyleBackColor = true;
            btnLaunchJulia.Click += btnLaunchJulia_Click;
            // 
            // tabPageMandelbrot
            // 
            tabPageMandelbrot.Controls.Add(button1);
            tabPageMandelbrot.Controls.Add(richTextBox1);
            tabPageMandelbrot.Controls.Add(btnLaunchMondelbrot);
            tabPageMandelbrot.Location = new Point(4, 26);
            tabPageMandelbrot.Name = "tabPageMandelbrot";
            tabPageMandelbrot.Padding = new Padding(3);
            tabPageMandelbrot.Size = new Size(792, 195);
            tabPageMandelbrot.TabIndex = 0;
            tabPageMandelbrot.Text = "Множество Мандельброта";
            tabPageMandelbrot.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.BackColor = SystemColors.Window;
            richTextBox1.BorderStyle = BorderStyle.None;
            richTextBox1.Dock = DockStyle.Right;
            richTextBox1.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 204);
            richTextBox1.Location = new Point(184, 3);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new Size(605, 189);
            richTextBox1.TabIndex = 2;
            richTextBox1.Text = resources.GetString("richTextBox1.Text");
            // 
            // btnLaunchMondelbrot
            // 
            btnLaunchMondelbrot.Location = new Point(8, 15);
            btnLaunchMondelbrot.Name = "btnLaunchMondelbrot";
            btnLaunchMondelbrot.Size = new Size(131, 55);
            btnLaunchMondelbrot.TabIndex = 0;
            btnLaunchMondelbrot.Text = "Запустить\r\nМандельброта";
            btnLaunchMondelbrot.UseVisualStyleBackColor = true;
            btnLaunchMondelbrot.Click += btnLaunchMondelbrot_Click;
            // 
            // tabControlFractals
            // 
            tabControlFractals.Controls.Add(tabPageMandelbrot);
            tabControlFractals.Controls.Add(tabPageJulia);
            tabControlFractals.Controls.Add(tabPageSerpinsky);
            tabControlFractals.Controls.Add(tabPageNewtonPools);
            tabControlFractals.Dock = DockStyle.Right;
            tabControlFractals.Font = new Font("Segoe UI", 10F);
            tabControlFractals.Location = new Point(0, 0);
            tabControlFractals.Name = "tabControlFractals";
            tabControlFractals.SelectedIndex = 0;
            tabControlFractals.Size = new Size(800, 225);
            tabControlFractals.TabIndex = 0;
            // 
            // tabPageSerpinsky
            // 
            tabPageSerpinsky.Controls.Add(richTextBox3);
            tabPageSerpinsky.Controls.Add(btnLaunchSerpinsky);
            tabPageSerpinsky.Location = new Point(4, 26);
            tabPageSerpinsky.Name = "tabPageSerpinsky";
            tabPageSerpinsky.Padding = new Padding(3);
            tabPageSerpinsky.Size = new Size(792, 195);
            tabPageSerpinsky.TabIndex = 2;
            tabPageSerpinsky.Text = "Треугольник Серпинского";
            tabPageSerpinsky.UseVisualStyleBackColor = true;
            // 
            // richTextBox3
            // 
            richTextBox3.BackColor = SystemColors.Window;
            richTextBox3.BorderStyle = BorderStyle.None;
            richTextBox3.Dock = DockStyle.Right;
            richTextBox3.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 204);
            richTextBox3.Location = new Point(184, 3);
            richTextBox3.Name = "richTextBox3";
            richTextBox3.ReadOnly = true;
            richTextBox3.Size = new Size(605, 189);
            richTextBox3.TabIndex = 4;
            richTextBox3.Text = resources.GetString("richTextBox3.Text");
            // 
            // btnLaunchSerpinsky
            // 
            btnLaunchSerpinsky.Location = new Point(8, 15);
            btnLaunchSerpinsky.Name = "btnLaunchSerpinsky";
            btnLaunchSerpinsky.Size = new Size(131, 44);
            btnLaunchSerpinsky.TabIndex = 3;
            btnLaunchSerpinsky.Text = "Запустить";
            btnLaunchSerpinsky.UseVisualStyleBackColor = true;
            btnLaunchSerpinsky.Click += btnLaunchSerpinsky_Click;
            // 
            // tabPageNewtonPools
            // 
            tabPageNewtonPools.Controls.Add(richTextBox4);
            tabPageNewtonPools.Controls.Add(btnLaunchNewton);
            tabPageNewtonPools.Location = new Point(4, 26);
            tabPageNewtonPools.Name = "tabPageNewtonPools";
            tabPageNewtonPools.Padding = new Padding(3);
            tabPageNewtonPools.Size = new Size(792, 195);
            tabPageNewtonPools.TabIndex = 3;
            tabPageNewtonPools.Text = "Бассейны Ньютона";
            tabPageNewtonPools.UseVisualStyleBackColor = true;
            // 
            // richTextBox4
            // 
            richTextBox4.BackColor = SystemColors.Window;
            richTextBox4.BorderStyle = BorderStyle.None;
            richTextBox4.Dock = DockStyle.Right;
            richTextBox4.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 204);
            richTextBox4.Location = new Point(184, 3);
            richTextBox4.Name = "richTextBox4";
            richTextBox4.ReadOnly = true;
            richTextBox4.Size = new Size(605, 189);
            richTextBox4.TabIndex = 6;
            richTextBox4.Text = resources.GetString("richTextBox4.Text");
            // 
            // btnLaunchNewton
            // 
            btnLaunchNewton.Location = new Point(8, 15);
            btnLaunchNewton.Name = "btnLaunchNewton";
            btnLaunchNewton.Size = new Size(131, 44);
            btnLaunchNewton.TabIndex = 5;
            btnLaunchNewton.Text = "Запустить";
            btnLaunchNewton.UseVisualStyleBackColor = true;
            btnLaunchNewton.Click += btnLaunchNewton_Click;
            // 
            // button1
            // 
            button1.Location = new Point(8, 76);
            button1.Name = "button1";
            button1.Size = new Size(131, 55);
            button1.TabIndex = 3;
            button1.Text = "Запустить\r\nГорящий корабль";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // MainFractalForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 225);
            Controls.Add(tabControlFractals);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainFractalForm";
            Text = "Менеджер фракталов";
            Load += MainFractalForm_Load_1;
            tabPageJulia.ResumeLayout(false);
            tabPageMandelbrot.ResumeLayout(false);
            tabControlFractals.ResumeLayout(false);
            tabPageSerpinsky.ResumeLayout(false);
            tabPageNewtonPools.ResumeLayout(false);
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
        private TabPage tabPageSerpinsky;
        private RichTextBox richTextBox3;
        private Button btnLaunchSerpinsky;
        private TabPage tabPageNewtonPools;
        private RichTextBox richTextBox4;
        private Button btnLaunchNewton;
        private Button btnLaunchBurningShipComplex;
        private Button button1;
    }
}
