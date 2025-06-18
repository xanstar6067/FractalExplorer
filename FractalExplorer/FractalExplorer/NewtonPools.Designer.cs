namespace FractalExplorer
{
    partial class NewtonPools
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
            panel6 = new Panel();
            colorBox2 = new CheckBox();
            colorBox0 = new CheckBox();
            colorBox4 = new CheckBox();
            oldRenderBW = new CheckBox();
            colorBox3 = new CheckBox();
            colorBox1 = new CheckBox();
            panel2 = new Panel();
            panel5 = new Panel();
            cbSelector = new ComboBox();
            panel4 = new Panel();
            richTextInput = new RichTextBox();
            panel3 = new Panel();
            label2 = new Label();
            richTextDebugOutput = new RichTextBox();
            label1 = new Label();
            nudW = new NumericUpDown();
            nudH = new NumericUpDown();
            progressPNG = new ProgressBar();
            label8 = new Label();
            label5 = new Label();
            nudZoom = new NumericUpDown();
            btnRender = new Button();
            progressBar = new ProgressBar();
            label6 = new Label();
            cbThreads = new ComboBox();
            btnSave = new Button();
            label3 = new Label();
            nudIterations = new NumericUpDown();
            fractal_bitmap = new PictureBox();
            colorCustom = new CheckBox();
            custom_color = new Button();
            panel1.SuspendLayout();
            panel6.SuspendLayout();
            panel2.SuspendLayout();
            panel5.SuspendLayout();
            panel4.SuspendLayout();
            panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudW).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudH).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fractal_bitmap).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(panel6);
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(nudW);
            panel1.Controls.Add(nudH);
            panel1.Controls.Add(progressPNG);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(nudZoom);
            panel1.Controls.Add(btnRender);
            panel1.Controls.Add(progressBar);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(cbThreads);
            panel1.Controls.Add(btnSave);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(nudIterations);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(231, 636);
            panel1.TabIndex = 0;
            // 
            // panel6
            // 
            panel6.Controls.Add(custom_color);
            panel6.Controls.Add(colorCustom);
            panel6.Controls.Add(colorBox2);
            panel6.Controls.Add(colorBox0);
            panel6.Controls.Add(colorBox4);
            panel6.Controls.Add(oldRenderBW);
            panel6.Controls.Add(colorBox3);
            panel6.Controls.Add(colorBox1);
            panel6.Dock = DockStyle.Bottom;
            panel6.Location = new Point(0, 323);
            panel6.Name = "panel6";
            panel6.Size = new Size(231, 104);
            panel6.TabIndex = 42;
            // 
            // colorBox2
            // 
            colorBox2.AutoSize = true;
            colorBox2.Location = new Point(170, 3);
            colorBox2.Name = "colorBox2";
            colorBox2.Size = new Size(61, 19);
            colorBox2.TabIndex = 37;
            colorBox2.Text = "Цвет 3";
            colorBox2.UseVisualStyleBackColor = true;
            // 
            // colorBox0
            // 
            colorBox0.AutoSize = true;
            colorBox0.Location = new Point(47, 3);
            colorBox0.Name = "colorBox0";
            colorBox0.Size = new Size(61, 19);
            colorBox0.TabIndex = 15;
            colorBox0.Text = "Цвет 1";
            colorBox0.UseVisualStyleBackColor = true;
            // 
            // colorBox4
            // 
            colorBox4.AutoSize = true;
            colorBox4.Location = new Point(66, 26);
            colorBox4.Name = "colorBox4";
            colorBox4.Size = new Size(61, 19);
            colorBox4.TabIndex = 39;
            colorBox4.Text = "Цвет 5";
            colorBox4.UseVisualStyleBackColor = true;
            // 
            // oldRenderBW
            // 
            oldRenderBW.AutoSize = true;
            oldRenderBW.Location = new Point(8, 3);
            oldRenderBW.Name = "oldRenderBW";
            oldRenderBW.Size = new Size(41, 19);
            oldRenderBW.TabIndex = 17;
            oldRenderBW.Text = "ЧБ";
            oldRenderBW.UseVisualStyleBackColor = true;
            // 
            // colorBox3
            // 
            colorBox3.AutoSize = true;
            colorBox3.Location = new Point(8, 26);
            colorBox3.Name = "colorBox3";
            colorBox3.Size = new Size(61, 19);
            colorBox3.TabIndex = 38;
            colorBox3.Text = "Цвет 4";
            colorBox3.UseVisualStyleBackColor = true;
            // 
            // colorBox1
            // 
            colorBox1.AutoSize = true;
            colorBox1.Location = new Point(109, 3);
            colorBox1.Name = "colorBox1";
            colorBox1.Size = new Size(61, 19);
            colorBox1.TabIndex = 36;
            colorBox1.Text = "Цвет 2";
            colorBox1.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            panel2.Controls.Add(panel5);
            panel2.Controls.Add(panel4);
            panel2.Controls.Add(panel3);
            panel2.Controls.Add(richTextDebugOutput);
            panel2.Controls.Add(label1);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 427);
            panel2.Name = "panel2";
            panel2.Size = new Size(231, 209);
            panel2.TabIndex = 40;
            // 
            // panel5
            // 
            panel5.Controls.Add(cbSelector);
            panel5.Dock = DockStyle.Bottom;
            panel5.Location = new Point(0, 21);
            panel5.Name = "panel5";
            panel5.Size = new Size(231, 24);
            panel5.TabIndex = 41;
            // 
            // cbSelector
            // 
            cbSelector.Dock = DockStyle.Bottom;
            cbSelector.FormattingEnabled = true;
            cbSelector.Location = new Point(0, 1);
            cbSelector.Name = "cbSelector";
            cbSelector.Size = new Size(231, 23);
            cbSelector.TabIndex = 34;
            // 
            // panel4
            // 
            panel4.Controls.Add(richTextInput);
            panel4.Dock = DockStyle.Bottom;
            panel4.Location = new Point(0, 45);
            panel4.Name = "panel4";
            panel4.Size = new Size(231, 64);
            panel4.TabIndex = 41;
            // 
            // richTextInput
            // 
            richTextInput.Dock = DockStyle.Fill;
            richTextInput.Location = new Point(0, 0);
            richTextInput.Name = "richTextInput";
            richTextInput.Size = new Size(231, 64);
            richTextInput.TabIndex = 33;
            richTextInput.Text = "";
            // 
            // panel3
            // 
            panel3.Controls.Add(label2);
            panel3.Dock = DockStyle.Bottom;
            panel3.Location = new Point(0, 109);
            panel3.Name = "panel3";
            panel3.Size = new Size(231, 23);
            panel3.TabIndex = 37;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(87, 5);
            label2.Name = "label2";
            label2.Size = new Size(52, 15);
            label2.TabIndex = 38;
            label2.Text = "Отладка";
            // 
            // richTextDebugOutput
            // 
            richTextDebugOutput.Dock = DockStyle.Bottom;
            richTextDebugOutput.Location = new Point(0, 132);
            richTextDebugOutput.Name = "richTextDebugOutput";
            richTextDebugOutput.ReadOnly = true;
            richTextDebugOutput.Size = new Size(231, 77);
            richTextDebugOutput.TabIndex = 36;
            richTextDebugOutput.Text = "";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(36, 0);
            label1.Name = "label1";
            label1.Size = new Size(163, 15);
            label1.TabIndex = 35;
            label1.Text = "Выбери полином/формулу.";
            // 
            // nudW
            // 
            nudW.Location = new Point(16, 239);
            nudW.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudW.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudW.Name = "nudW";
            nudW.Size = new Size(86, 23);
            nudW.TabIndex = 23;
            nudW.Value = new decimal(new int[] { 1920, 0, 0, 0 });
            // 
            // nudH
            // 
            nudH.Location = new Point(128, 239);
            nudH.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudH.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudH.Name = "nudH";
            nudH.Size = new Size(83, 23);
            nudH.TabIndex = 22;
            nudH.Value = new decimal(new int[] { 1080, 0, 0, 0 });
            // 
            // progressPNG
            // 
            progressPNG.Location = new Point(7, 268);
            progressPNG.Name = "progressPNG";
            progressPNG.Size = new Size(218, 23);
            progressPNG.TabIndex = 21;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(89, 163);
            label8.Name = "label8";
            label8.Size = new Size(67, 15);
            label8.TabIndex = 20;
            label8.Text = "Обработка";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(15, 40);
            label5.Name = "label5";
            label5.Size = new Size(86, 15);
            label5.TabIndex = 16;
            label5.Text = "Приближение";
            // 
            // nudZoom
            // 
            nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudZoom.Location = new Point(14, 58);
            nudZoom.Maximum = new decimal(new int[] { 268435455, 1042612833, 542101086, 0 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new Size(196, 23);
            nudZoom.TabIndex = 2;
            // 
            // btnRender
            // 
            btnRender.Location = new Point(36, 137);
            btnRender.Name = "btnRender";
            btnRender.Size = new Size(164, 23);
            btnRender.TabIndex = 2;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            btnRender.Click += btnRender_Click;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(7, 181);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(218, 23);
            progressBar.TabIndex = 14;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(14, 85);
            label6.Name = "label6";
            label6.Size = new Size(69, 15);
            label6.TabIndex = 13;
            label6.Text = "Потоки ЦП";
            // 
            // cbThreads
            // 
            cbThreads.FormattingEnabled = true;
            cbThreads.Location = new Point(14, 103);
            cbThreads.Name = "cbThreads";
            cbThreads.Size = new Size(195, 23);
            cbThreads.TabIndex = 12;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(32, 210);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(164, 23);
            btnSave.TabIndex = 11;
            btnSave.Text = "Сохранить изображение";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
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
            // nudIterations
            // 
            nudIterations.Location = new Point(12, 12);
            nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudIterations.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
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
            // colorCustom
            // 
            colorCustom.AutoSize = true;
            colorCustom.Location = new Point(8, 51);
            colorCustom.Name = "colorCustom";
            colorCustom.Size = new Size(98, 19);
            colorCustom.TabIndex = 40;
            colorCustom.Text = "Выбери свой";
            colorCustom.UseVisualStyleBackColor = true;
            // 
            // custom_color
            // 
            custom_color.Location = new Point(32, 75);
            custom_color.Name = "custom_color";
            custom_color.Size = new Size(164, 23);
            custom_color.TabIndex = 43;
            custom_color.Text = "Настроить цвета";
            custom_color.UseVisualStyleBackColor = true;
            custom_color.Click += custom_color_Click;
            // 
            // NewtonPools
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(fractal_bitmap);
            Controls.Add(panel1);
            MinimumSize = new Size(1100, 675);
            Name = "NewtonPools";
            Text = "Бассейны Ньютона";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel6.ResumeLayout(false);
            panel6.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel5.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudW).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudH).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
            ((System.ComponentModel.ISupportInitialize)fractal_bitmap).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private PictureBox fractal_bitmap;
        private Label label3;
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
        private RichTextBox richTextInput;
        private Label label1;
        private ComboBox cbSelector;
        private CheckBox colorBox4;
        private CheckBox colorBox3;
        private Panel panel2;
        private RichTextBox richTextDebugOutput;
        private Panel panel3;
        private Label label2;
        private Panel panel4;
        private Panel panel5;
        private Panel panel6;
        private Button custom_color;
        private CheckBox colorCustom;
    }
}