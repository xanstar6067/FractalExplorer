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
            mondelbrotClassicBox = new CheckBox();
            checkBox6 = new CheckBox();
            checkBox5 = new CheckBox();
            checkBox4 = new CheckBox();
            checkBox3 = new CheckBox();
            checkBox2 = new CheckBox();
            checkBox1 = new CheckBox();
            nudW = new NumericUpDown();
            nudH = new NumericUpDown();
            progressPNG = new ProgressBar();
            label8 = new Label();
            oldRenderBW = new CheckBox();
            label5 = new Label();
            nudZoom = new NumericUpDown();
            colorBox = new CheckBox();
            btnRender = new Button();
            progressBar = new ProgressBar();
            label6 = new Label();
            cbThreads = new ComboBox();
            btnSave = new Button();
            label4 = new Label();
            label3 = new Label();
            nudThreshold = new NumericUpDown();
            nudIterations = new NumericUpDown();
            canvas2 = new PictureBox();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudW).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudH).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)canvas2).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(mondelbrotClassicBox);
            panel1.Controls.Add(checkBox6);
            panel1.Controls.Add(checkBox5);
            panel1.Controls.Add(checkBox4);
            panel1.Controls.Add(checkBox3);
            panel1.Controls.Add(checkBox2);
            panel1.Controls.Add(checkBox1);
            panel1.Controls.Add(nudW);
            panel1.Controls.Add(nudH);
            panel1.Controls.Add(progressPNG);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(oldRenderBW);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(nudZoom);
            panel1.Controls.Add(colorBox);
            panel1.Controls.Add(btnRender);
            panel1.Controls.Add(progressBar);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(cbThreads);
            panel1.Controls.Add(btnSave);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(nudThreshold);
            panel1.Controls.Add(nudIterations);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(231, 636);
            panel1.TabIndex = 0;
            // 
            // mondelbrotClassicBox
            // 
            mondelbrotClassicBox.AutoSize = true;
            mondelbrotClassicBox.Location = new Point(126, 347);
            mondelbrotClassicBox.Name = "mondelbrotClassicBox";
            mondelbrotClassicBox.Size = new Size(77, 19);
            mondelbrotClassicBox.TabIndex = 32;
            mondelbrotClassicBox.Text = "Классика";
            mondelbrotClassicBox.UseVisualStyleBackColor = true;
            // 
            // checkBox6
            // 
            checkBox6.AutoSize = true;
            checkBox6.Location = new Point(195, 372);
            checkBox6.Name = "checkBox6";
            checkBox6.Size = new Size(32, 19);
            checkBox6.TabIndex = 29;
            checkBox6.Text = "6";
            checkBox6.UseVisualStyleBackColor = true;
            // 
            // checkBox5
            // 
            checkBox5.AutoSize = true;
            checkBox5.Location = new Point(164, 372);
            checkBox5.Name = "checkBox5";
            checkBox5.Size = new Size(32, 19);
            checkBox5.TabIndex = 28;
            checkBox5.Text = "5";
            checkBox5.UseVisualStyleBackColor = true;
            // 
            // checkBox4
            // 
            checkBox4.AutoSize = true;
            checkBox4.Location = new Point(126, 372);
            checkBox4.Name = "checkBox4";
            checkBox4.Size = new Size(32, 19);
            checkBox4.TabIndex = 27;
            checkBox4.Text = "4";
            checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            checkBox3.AutoSize = true;
            checkBox3.Location = new Point(88, 372);
            checkBox3.Name = "checkBox3";
            checkBox3.Size = new Size(32, 19);
            checkBox3.TabIndex = 26;
            checkBox3.Text = "3";
            checkBox3.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Location = new Point(50, 372);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(32, 19);
            checkBox2.TabIndex = 25;
            checkBox2.Text = "2";
            checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(12, 372);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(32, 19);
            checkBox1.TabIndex = 24;
            checkBox1.Text = "1";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // nudW
            // 
            nudW.Location = new Point(12, 289);
            nudW.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudW.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudW.Name = "nudW";
            nudW.Size = new Size(86, 23);
            nudW.TabIndex = 23;
            nudW.Value = new decimal(new int[] { 1920, 0, 0, 0 });
            // 
            // nudH
            // 
            nudH.Location = new Point(124, 289);
            nudH.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudH.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudH.Name = "nudH";
            nudH.Size = new Size(83, 23);
            nudH.TabIndex = 22;
            nudH.Value = new decimal(new int[] { 1080, 0, 0, 0 });
            // 
            // progressPNG
            // 
            progressPNG.Location = new Point(3, 318);
            progressPNG.Name = "progressPNG";
            progressPNG.Size = new Size(218, 23);
            progressPNG.TabIndex = 21;
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
            // oldRenderBW
            // 
            oldRenderBW.AutoSize = true;
            oldRenderBW.Location = new Point(76, 347);
            oldRenderBW.Name = "oldRenderBW";
            oldRenderBW.Size = new Size(41, 19);
            oldRenderBW.TabIndex = 17;
            oldRenderBW.Text = "ЧБ";
            oldRenderBW.UseVisualStyleBackColor = true;
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
            // nudZoom
            // 
            nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudZoom.Location = new Point(12, 86);
            nudZoom.Maximum = new decimal(new int[] { 268435455, 1042612833, 542101086, 0 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new Size(196, 23);
            nudZoom.TabIndex = 2;
            // 
            // colorBox
            // 
            colorBox.AutoSize = true;
            colorBox.Location = new Point(12, 347);
            colorBox.Name = "colorBox";
            colorBox.Size = new Size(52, 19);
            colorBox.TabIndex = 15;
            colorBox.Text = "Цвет";
            colorBox.UseVisualStyleBackColor = true;
            // 
            // btnRender
            // 
            btnRender.Location = new Point(34, 165);
            btnRender.Name = "btnRender";
            btnRender.Size = new Size(164, 23);
            btnRender.TabIndex = 2;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            btnRender.Click += btnRender_Click;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(5, 209);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(218, 23);
            progressBar.TabIndex = 14;
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
            // cbThreads
            // 
            cbThreads.FormattingEnabled = true;
            cbThreads.Location = new Point(12, 131);
            cbThreads.Name = "cbThreads";
            cbThreads.Size = new Size(195, 23);
            cbThreads.TabIndex = 12;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(28, 260);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(164, 23);
            btnSave.TabIndex = 11;
            btnSave.Text = "Сохранить изображение";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click_1;
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
            // nudThreshold
            // 
            nudThreshold.DecimalPlaces = 3;
            nudThreshold.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudThreshold.Location = new Point(12, 41);
            nudThreshold.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            nudThreshold.Name = "nudThreshold";
            nudThreshold.Size = new Size(120, 23);
            nudThreshold.TabIndex = 3;
            nudThreshold.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // nudIterations
            // 
            nudIterations.Location = new Point(12, 12);
            nudIterations.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
            nudIterations.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new Size(120, 23);
            nudIterations.TabIndex = 2;
            nudIterations.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // canvas2
            // 
            canvas2.Dock = DockStyle.Fill;
            canvas2.Location = new Point(231, 0);
            canvas2.Name = "canvas2";
            canvas2.Size = new Size(853, 636);
            canvas2.TabIndex = 1;
            canvas2.TabStop = false;
            canvas2.Click += btnSave_Click_1;
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
            ((System.ComponentModel.ISupportInitialize)nudW).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudH).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
            ((System.ComponentModel.ISupportInitialize)canvas2).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private PictureBox canvas2;
        private Label label4;
        private Label label3;
        private NumericUpDown nudThreshold;
        private NumericUpDown nudIterations;
        private Button btnSave;
        private Label label6;
        private ComboBox cbThreads;
        private ProgressBar progressBar;
        private Button btnRender;
        private CheckBox colorBox;
        private Label label5;
        private NumericUpDown nudZoom;
        private CheckBox oldRenderBW;
        private Label label8;
        private ProgressBar progressPNG;
        private NumericUpDown nudW;
        private NumericUpDown nudH;
        private CheckBox checkBox6;
        private CheckBox checkBox5;
        private CheckBox checkBox4;
        private CheckBox checkBox3;
        private CheckBox checkBox2;
        private CheckBox checkBox1;
        private CheckBox mondelbrotClassicBox;
    }
}
