namespace FractalExplorer
{
    partial class NewtonPpools
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
            label1 = new Label();
            cbSelector = new ComboBox();
            richTextBox1 = new RichTextBox();
            nudW = new NumericUpDown();
            nudH = new NumericUpDown();
            progressPNG = new ProgressBar();
            label8 = new Label();
            oldRenderBW = new CheckBox();
            label5 = new Label();
            nudZoom = new NumericUpDown();
            colorBox0 = new CheckBox();
            btnRender = new Button();
            progressBar = new ProgressBar();
            label6 = new Label();
            cbThreads = new ComboBox();
            btnSave = new Button();
            label4 = new Label();
            label3 = new Label();
            nudThreshold = new NumericUpDown();
            nudIterations = new NumericUpDown();
            fractal_bitmap = new PictureBox();
            colorBox1 = new CheckBox();
            colorBox2 = new CheckBox();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudW).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudH).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fractal_bitmap).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(colorBox2);
            panel1.Controls.Add(colorBox1);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(cbSelector);
            panel1.Controls.Add(richTextBox1);
            panel1.Controls.Add(nudW);
            panel1.Controls.Add(nudH);
            panel1.Controls.Add(progressPNG);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(oldRenderBW);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(nudZoom);
            panel1.Controls.Add(colorBox0);
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
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(29, 398);
            label1.Name = "label1";
            label1.Size = new Size(163, 15);
            label1.TabIndex = 35;
            label1.Text = "Выбери полином/формулу.";
            // 
            // cbSelector
            // 
            cbSelector.Dock = DockStyle.Bottom;
            cbSelector.FormattingEnabled = true;
            cbSelector.Location = new Point(0, 416);
            cbSelector.Name = "cbSelector";
            cbSelector.Size = new Size(231, 23);
            cbSelector.TabIndex = 34;
            // 
            // richTextBox1
            // 
            richTextBox1.Dock = DockStyle.Bottom;
            richTextBox1.Location = new Point(0, 439);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(231, 197);
            richTextBox1.TabIndex = 33;
            richTextBox1.Text = "";
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
            oldRenderBW.Location = new Point(5, 347);
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
            // colorBox0
            // 
            colorBox0.AutoSize = true;
            colorBox0.Location = new Point(46, 347);
            colorBox0.Name = "colorBox0";
            colorBox0.Size = new Size(52, 19);
            colorBox0.TabIndex = 15;
            colorBox0.Text = "Цвет";
            colorBox0.UseVisualStyleBackColor = true;
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
            btnSave.Click += btnSave_Click;
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
            // fractal_bitmap
            // 
            fractal_bitmap.Dock = DockStyle.Fill;
            fractal_bitmap.Location = new Point(231, 0);
            fractal_bitmap.Name = "fractal_bitmap";
            fractal_bitmap.Size = new Size(853, 636);
            fractal_bitmap.TabIndex = 1;
            fractal_bitmap.TabStop = false;
            // 
            // colorBox1
            // 
            colorBox1.AutoSize = true;
            colorBox1.Location = new Point(102, 347);
            colorBox1.Name = "colorBox1";
            colorBox1.Size = new Size(52, 19);
            colorBox1.TabIndex = 36;
            colorBox1.Text = "Цвет";
            colorBox1.UseVisualStyleBackColor = true;
            // 
            // colorBox2
            // 
            colorBox2.AutoSize = true;
            colorBox2.Location = new Point(155, 347);
            colorBox2.Name = "colorBox2";
            colorBox2.Size = new Size(52, 19);
            colorBox2.TabIndex = 37;
            colorBox2.Text = "Цвет";
            colorBox2.UseVisualStyleBackColor = true;
            // 
            // NewtonPpools
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(fractal_bitmap);
            Controls.Add(panel1);
            MinimumSize = new Size(1100, 675);
            Name = "NewtonPpools";
            Text = "Бассейны Ньютона";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudW).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudH).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
            ((System.ComponentModel.ISupportInitialize)fractal_bitmap).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private PictureBox fractal_bitmap;
        private Label label4;
        private Label label3;
        private NumericUpDown nudThreshold;
        private NumericUpDown nudIterations;
        private Button btnSave;
        private Label label6;
        private ComboBox cbThreads;
        private ProgressBar progressBar;
        private Button btnRender;
        private CheckBox colorBox0;
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
        private CheckBox colorBox2;
        private CheckBox colorBox1;
        private RichTextBox richTextBox1;
        private Label label1;
        private ComboBox cbSelector;
    }
}