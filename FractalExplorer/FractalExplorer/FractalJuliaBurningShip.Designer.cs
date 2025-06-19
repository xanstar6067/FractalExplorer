// --- START OF FILE burningShipComplex.Designer.cs ---

namespace FractalDraving
{
    partial class FractalJuliaBurningShip
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
            panel2 = new Panel();
            mandelbrotCanvas1 = new PictureBox();
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
            label7 = new Label();
            nudBaseScale = new NumericUpDown();
            oldRenderBW = new CheckBox();
            label5 = new Label();
            nudZoom = new NumericUpDown();
            colorBox = new CheckBox();
            btnRender1 = new Button();
            progressBar1 = new ProgressBar();
            label6 = new Label();
            cbThreads1 = new ComboBox();
            btnSave = new Button();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            nudThreshold1 = new NumericUpDown();
            nudIterations1 = new NumericUpDown();
            nudIm1 = new NumericUpDown();
            nudRe1 = new NumericUpDown();
            canvas1 = new PictureBox();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mandelbrotCanvas1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudW).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudH).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudBaseScale).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIm1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudRe1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)canvas1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(panel2);
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
            panel1.Controls.Add(label7);
            panel1.Controls.Add(nudBaseScale);
            panel1.Controls.Add(oldRenderBW);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(nudZoom);
            panel1.Controls.Add(colorBox);
            panel1.Controls.Add(btnRender1);
            panel1.Controls.Add(progressBar1);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(cbThreads1);
            panel1.Controls.Add(btnSave);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(nudThreshold1);
            panel1.Controls.Add(nudIterations1);
            panel1.Controls.Add(nudIm1);
            panel1.Controls.Add(nudRe1);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(231, 636);
            panel1.TabIndex = 0;
            // 
            // panel2
            // 
            panel2.Controls.Add(mandelbrotCanvas1);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 470);
            panel2.Name = "panel2";
            panel2.Size = new Size(231, 166);
            panel2.TabIndex = 31;
            // 
            // mandelbrotCanvas1
            // 
            mandelbrotCanvas1.Dock = DockStyle.Fill;
            mandelbrotCanvas1.Location = new Point(0, 0);
            mandelbrotCanvas1.Name = "mandelbrotCanvas1";
            mandelbrotCanvas1.Size = new Size(231, 166);
            mandelbrotCanvas1.TabIndex = 30;
            mandelbrotCanvas1.TabStop = false;
            mandelbrotCanvas1.Click += mandelbrotCanvas_Click;
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
            label8.Location = new Point(88, 423);
            label8.Name = "label8";
            label8.Size = new Size(67, 15);
            label8.TabIndex = 20;
            label8.Text = "Обработка";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(12, 169);
            label7.Name = "label7";
            label7.Size = new Size(105, 15);
            label7.TabIndex = 19;
            label7.Text = "Увеличение лупы";
            label7.Visible = false;
            // 
            // nudBaseScale
            // 
            nudBaseScale.Location = new Point(12, 187);
            nudBaseScale.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudBaseScale.Minimum = new decimal(new int[] { 10000, 0, 0, int.MinValue });
            nudBaseScale.Name = "nudBaseScale";
            nudBaseScale.Size = new Size(195, 23);
            nudBaseScale.TabIndex = 18;
            nudBaseScale.Value = new decimal(new int[] { 4, 0, 0, 0 });
            nudBaseScale.Visible = false;
            // 
            // oldRenderBW
            // 
            oldRenderBW.AutoSize = true;
            oldRenderBW.Location = new Point(89, 347);
            oldRenderBW.Name = "oldRenderBW";
            oldRenderBW.Size = new Size(110, 19);
            oldRenderBW.TabIndex = 17;
            oldRenderBW.Text = "Старый рендер";
            oldRenderBW.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 125);
            label5.Name = "label5";
            label5.Size = new Size(86, 15);
            label5.TabIndex = 16;
            label5.Text = "Приближение";
            // 
            // nudZoom
            // 
            nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudZoom.Location = new Point(11, 143);
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
            // btnRender1
            // 
            btnRender1.Location = new Point(35, 397);
            btnRender1.Name = "btnRender1";
            btnRender1.Size = new Size(164, 23);
            btnRender1.TabIndex = 2;
            btnRender1.Text = "Запустить рендер";
            btnRender1.UseVisualStyleBackColor = true;
            btnRender1.Click += btnRender_Click;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(6, 441);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(218, 23);
            progressBar1.TabIndex = 14;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(12, 213);
            label6.Name = "label6";
            label6.Size = new Size(69, 15);
            label6.TabIndex = 13;
            label6.Text = "Потоки ЦП";
            // 
            // cbThreads1
            // 
            cbThreads1.FormattingEnabled = true;
            cbThreads1.Location = new Point(12, 231);
            cbThreads1.Name = "cbThreads1";
            cbThreads1.Size = new Size(195, 23);
            cbThreads1.TabIndex = 12;
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
            label4.Location = new Point(138, 101);
            label4.Name = "label4";
            label4.Size = new Size(85, 15);
            label4.TabIndex = 7;
            label4.Text = "Порог выхода";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(138, 72);
            label3.Name = "label3";
            label3.Size = new Size(61, 15);
            label3.TabIndex = 6;
            label3.Text = "Итерации";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(138, 43);
            label2.Name = "label2";
            label2.Size = new Size(54, 15);
            label2.TabIndex = 5;
            label2.Text = "Мнимое";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(131, 14);
            label1.Name = "label1";
            label1.Size = new Size(96, 15);
            label1.TabIndex = 4;
            label1.Text = "Действительное";
            // 
            // nudThreshold1
            // 
            nudThreshold1.DecimalPlaces = 3;
            nudThreshold1.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudThreshold1.Location = new Point(12, 99);
            nudThreshold1.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            nudThreshold1.Name = "nudThreshold1";
            nudThreshold1.Size = new Size(120, 23);
            nudThreshold1.TabIndex = 3;
            nudThreshold1.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // nudIterations1
            // 
            nudIterations1.Location = new Point(12, 70);
            nudIterations1.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
            nudIterations1.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            nudIterations1.Name = "nudIterations1";
            nudIterations1.Size = new Size(120, 23);
            nudIterations1.TabIndex = 2;
            nudIterations1.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // nudIm1
            // 
            nudIm1.DecimalPlaces = 3;
            nudIm1.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudIm1.Location = new Point(12, 41);
            nudIm1.Maximum = new decimal(new int[] { 156, 0, 0, 196608 });
            nudIm1.Minimum = new decimal(new int[] { 3, 0, 0, int.MinValue });
            nudIm1.Name = "nudIm1";
            nudIm1.Size = new Size(120, 23);
            nudIm1.TabIndex = 1;
            nudIm1.Value = new decimal(new int[] { 4395, 0, 0, -2147090432 });
            // 
            // nudRe1
            // 
            nudRe1.DecimalPlaces = 3;
            nudRe1.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudRe1.Location = new Point(12, 12);
            nudRe1.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            nudRe1.Minimum = new decimal(new int[] { 3, 0, 0, int.MinValue });
            nudRe1.Name = "nudRe1";
            nudRe1.Size = new Size(120, 23);
            nudRe1.TabIndex = 0;
            nudRe1.Value = new decimal(new int[] { 1636, 0, 0, -2147287040 });
            // 
            // canvas1
            // 
            canvas1.Dock = DockStyle.Fill;
            canvas1.Location = new Point(231, 0);
            canvas1.Name = "canvas1";
            canvas1.Size = new Size(853, 636);
            canvas1.TabIndex = 1;
            canvas1.TabStop = false;
            // 
            // FractalburningShipJulia
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(canvas1);
            Controls.Add(panel1);
            MinimumSize = new Size(1100, 675);
            Name = "FractalburningShipJulia";
            Text = "Фрактал Горящий Корабль (Жюлиа)";
            Load += Form1_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mandelbrotCanvas1).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudW).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudH).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudBaseScale).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold1).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations1).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIm1).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudRe1).EndInit();
            ((System.ComponentModel.ISupportInitialize)canvas1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private PictureBox canvas1;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label1;
        private NumericUpDown nudThreshold1;
        private NumericUpDown nudIterations1;
        private NumericUpDown nudIm1;
        private NumericUpDown nudRe1;
        private Button btnSave;
        private Label label6;
        private ComboBox cbThreads1;
        private ProgressBar progressBar1;
        private Button btnRender1;
        private CheckBox colorBox;
        private Label label5;
        private NumericUpDown nudZoom;
        private CheckBox oldRenderBW;
        private Label label7;
        private NumericUpDown nudBaseScale;
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
        private PictureBox mandelbrotCanvas1;
        private Panel panel2;
    }
}