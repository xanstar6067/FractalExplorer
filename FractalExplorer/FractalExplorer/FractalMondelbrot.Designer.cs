namespace FractalDraving
{
    partial class FractalMondelbrot
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
            panel1 = new Panel();
            mondelbrotClassicBox2 = new CheckBox();
            checkBox62 = new CheckBox();
            checkBox52 = new CheckBox();
            checkBox42 = new CheckBox();
            checkBox32 = new CheckBox();
            checkBox22 = new CheckBox();
            checkBox12 = new CheckBox();
            nudW2 = new NumericUpDown();
            nudH2 = new NumericUpDown();
            progressPNG2 = new ProgressBar();
            label8 = new Label();
            oldRenderBW2 = new CheckBox();
            label5 = new Label();
            nudZoom2 = new NumericUpDown();
            colorBox2 = new CheckBox();
            btnRender2 = new Button();
            progressBar2 = new ProgressBar();
            label6 = new Label();
            cbThreads2 = new ComboBox();
            btnSave2 = new Button();
            label4 = new Label();
            label3 = new Label();
            nudThreshold2 = new NumericUpDown();
            nudIterations2 = new NumericUpDown();
            canvas2 = new PictureBox();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudW2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudH2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)canvas2).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(mondelbrotClassicBox2);
            panel1.Controls.Add(checkBox62);
            panel1.Controls.Add(checkBox52);
            panel1.Controls.Add(checkBox42);
            panel1.Controls.Add(checkBox32);
            panel1.Controls.Add(checkBox22);
            panel1.Controls.Add(checkBox12);
            panel1.Controls.Add(nudW2);
            panel1.Controls.Add(nudH2);
            panel1.Controls.Add(progressPNG2);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(oldRenderBW2);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(nudZoom2);
            panel1.Controls.Add(colorBox2);
            panel1.Controls.Add(btnRender2);
            panel1.Controls.Add(progressBar2);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(cbThreads2);
            panel1.Controls.Add(btnSave2);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(nudThreshold2);
            panel1.Controls.Add(nudIterations2);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(231, 636);
            panel1.TabIndex = 0;
            // 
            // mondelbrotClassicBox2
            // 
            mondelbrotClassicBox2.AutoSize = true;
            mondelbrotClassicBox2.Location = new Point(126, 347);
            mondelbrotClassicBox2.Name = "mondelbrotClassicBox2";
            mondelbrotClassicBox2.Size = new Size(77, 19);
            mondelbrotClassicBox2.TabIndex = 32;
            mondelbrotClassicBox2.Text = "Классика";
            mondelbrotClassicBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox62
            // 
            checkBox62.AutoSize = true;
            checkBox62.Location = new Point(195, 372);
            checkBox62.Name = "checkBox62";
            checkBox62.Size = new Size(32, 19);
            checkBox62.TabIndex = 29;
            checkBox62.Text = "6";
            checkBox62.UseVisualStyleBackColor = true;
            // 
            // checkBox52
            // 
            checkBox52.AutoSize = true;
            checkBox52.Location = new Point(164, 372);
            checkBox52.Name = "checkBox52";
            checkBox52.Size = new Size(32, 19);
            checkBox52.TabIndex = 28;
            checkBox52.Text = "5";
            checkBox52.UseVisualStyleBackColor = true;
            // 
            // checkBox42
            // 
            checkBox42.AutoSize = true;
            checkBox42.Location = new Point(126, 372);
            checkBox42.Name = "checkBox42";
            checkBox42.Size = new Size(32, 19);
            checkBox42.TabIndex = 27;
            checkBox42.Text = "4";
            checkBox42.UseVisualStyleBackColor = true;
            // 
            // checkBox32
            // 
            checkBox32.AutoSize = true;
            checkBox32.Location = new Point(88, 372);
            checkBox32.Name = "checkBox32";
            checkBox32.Size = new Size(32, 19);
            checkBox32.TabIndex = 26;
            checkBox32.Text = "3";
            checkBox32.UseVisualStyleBackColor = true;
            // 
            // checkBox22
            // 
            checkBox22.AutoSize = true;
            checkBox22.Location = new Point(50, 372);
            checkBox22.Name = "checkBox22";
            checkBox22.Size = new Size(32, 19);
            checkBox22.TabIndex = 25;
            checkBox22.Text = "2";
            checkBox22.UseVisualStyleBackColor = true;
            // 
            // checkBox12
            // 
            checkBox12.AutoSize = true;
            checkBox12.Location = new Point(12, 372);
            checkBox12.Name = "checkBox12";
            checkBox12.Size = new Size(32, 19);
            checkBox12.TabIndex = 24;
            checkBox12.Text = "1";
            checkBox12.UseVisualStyleBackColor = true;
            // 
            // nudW2
            // 
            nudW2.Location = new Point(12, 289);
            nudW2.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudW2.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudW2.Name = "nudW2";
            nudW2.Size = new Size(86, 23);
            nudW2.TabIndex = 23;
            nudW2.Value = new decimal(new int[] { 1920, 0, 0, 0 });
            // 
            // nudH2
            // 
            nudH2.Location = new Point(124, 289);
            nudH2.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudH2.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudH2.Name = "nudH2";
            nudH2.Size = new Size(83, 23);
            nudH2.TabIndex = 22;
            nudH2.Value = new decimal(new int[] { 1080, 0, 0, 0 });
            // 
            // progressPNG2
            // 
            progressPNG2.Location = new Point(3, 318);
            progressPNG2.Name = "progressPNG2";
            progressPNG2.Size = new Size(218, 23);
            progressPNG2.TabIndex = 21;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(87, 191);
            label8.Name = "label8";
            label8.Size = new Size(67, 15);
            label8.TabIndex = 20;
            label8.Text = "Обработка";
            // 
            // oldRenderBW2
            // 
            oldRenderBW2.AutoSize = true;
            oldRenderBW2.Location = new Point(76, 347);
            oldRenderBW2.Name = "oldRenderBW2";
            oldRenderBW2.Size = new Size(41, 19);
            oldRenderBW2.TabIndex = 17;
            oldRenderBW2.Text = "ЧБ";
            oldRenderBW2.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(13, 68);
            label5.Name = "label5";
            label5.Size = new Size(86, 15);
            label5.TabIndex = 16;
            label5.Text = "Приближение";
            // 
            // nudZoom2
            // 
            nudZoom2.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudZoom2.Location = new Point(12, 86);
            nudZoom2.Maximum = new decimal(new int[] { 268435455, 1042612833, 542101086, 0 });
            nudZoom2.Name = "nudZoom2";
            nudZoom2.Size = new Size(196, 23);
            nudZoom2.TabIndex = 2;
            // 
            // colorBox2
            // 
            colorBox2.AutoSize = true;
            colorBox2.Location = new Point(12, 347);
            colorBox2.Name = "colorBox2";
            colorBox2.Size = new Size(52, 19);
            colorBox2.TabIndex = 15;
            colorBox2.Text = "Цвет";
            colorBox2.UseVisualStyleBackColor = true;
            // 
            // btnRender2
            // 
            btnRender2.Location = new Point(34, 165);
            btnRender2.Name = "btnRender2";
            btnRender2.Size = new Size(164, 23);
            btnRender2.TabIndex = 2;
            btnRender2.Text = "Запустить рендер";
            btnRender2.UseVisualStyleBackColor = true;
            btnRender2.Click += btnRender_Click;
            // 
            // progressBar2
            // 
            progressBar2.Location = new Point(5, 209);
            progressBar2.Name = "progressBar2";
            progressBar2.Size = new Size(218, 23);
            progressBar2.TabIndex = 14;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(12, 113);
            label6.Name = "label6";
            label6.Size = new Size(69, 15);
            label6.TabIndex = 13;
            label6.Text = "Потоки ЦП";
            // 
            // cbThreads2
            // 
            cbThreads2.FormattingEnabled = true;
            cbThreads2.Location = new Point(12, 131);
            cbThreads2.Name = "cbThreads2";
            cbThreads2.Size = new Size(195, 23);
            cbThreads2.TabIndex = 12;
            // 
            // btnSave2
            // 
            btnSave2.Location = new Point(28, 260);
            btnSave2.Name = "btnSave2";
            btnSave2.Size = new Size(164, 23);
            btnSave2.TabIndex = 11;
            btnSave2.Text = "Сохранить изображение";
            btnSave2.UseVisualStyleBackColor = true;
            btnSave2.Click += btnSave_Click_1;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(138, 43);
            label4.Name = "label4";
            label4.Size = new Size(85, 15);
            label4.TabIndex = 7;
            label4.Text = "Порог выхода";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(138, 14);
            label3.Name = "label3";
            label3.Size = new Size(61, 15);
            label3.TabIndex = 6;
            label3.Text = "Итерации";
            // 
            // nudThreshold2
            // 
            nudThreshold2.DecimalPlaces = 3;
            nudThreshold2.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudThreshold2.Location = new Point(12, 41);
            nudThreshold2.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            nudThreshold2.Name = "nudThreshold2";
            nudThreshold2.Size = new Size(120, 23);
            nudThreshold2.TabIndex = 3;
            nudThreshold2.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // nudIterations2
            // 
            nudIterations2.Location = new Point(12, 12);
            nudIterations2.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
            nudIterations2.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            nudIterations2.Name = "nudIterations2";
            nudIterations2.Size = new Size(120, 23);
            nudIterations2.TabIndex = 2;
            nudIterations2.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // canvas2
            // 
            canvas2.Dock = DockStyle.Fill;
            canvas2.Location = new Point(231, 0);
            canvas2.Name = "canvas2";
            canvas2.Size = new Size(853, 636);
            canvas2.TabIndex = 1;
            canvas2.TabStop = false;
            // 
            // FractalMondelbrot
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(canvas2);
            Controls.Add(panel1);
            MinimumSize = new Size(1100, 675);
            Name = "FractalMondelbrot";
            Text = "Множество Монделброта";
            Load += Form1_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudW2).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudH2).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom2).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold2).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations2).EndInit();
            ((System.ComponentModel.ISupportInitialize)canvas2).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private PictureBox canvas2;
        private Label label4;
        private Label label3;
        private NumericUpDown nudThreshold2;
        private NumericUpDown nudIterations2;
        private Button btnSave2;
        private Label label6;
        private ComboBox cbThreads2;
        private ProgressBar progressBar2;
        private Button btnRender2;
        private CheckBox colorBox2;
        private Label label5;
        private NumericUpDown nudZoom2;
        private CheckBox oldRenderBW2;
        private Label label8;
        private ProgressBar progressPNG2;
        private NumericUpDown nudW2;
        private NumericUpDown nudH2;
        private CheckBox checkBox62;
        private CheckBox checkBox52;
        private CheckBox checkBox42;
        private CheckBox checkBox32;
        private CheckBox checkBox22;
        private CheckBox checkBox12;
        private CheckBox mondelbrotClassicBox2;
    }
}
