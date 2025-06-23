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
            btnConfigurePalette = new Button();
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
            panel1.SuspendLayout();
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
            panel1.Controls.Add(btnConfigurePalette);
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
            // btnConfigurePalette
            // 
            btnConfigurePalette.Location = new Point(7, 297);
            btnConfigurePalette.Name = "btnConfigurePalette";
            btnConfigurePalette.Size = new Size(218, 34);
            btnConfigurePalette.TabIndex = 41;
            btnConfigurePalette.Text = "Настроить палитру";
            btnConfigurePalette.UseVisualStyleBackColor = true;
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
            label1.Location = new Point(36, 4);
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
            Load += NewtonPools_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
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

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox fractal_bitmap;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nudIterations;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbThreads;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button btnRender;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown nudZoom;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ProgressBar progressPNG;
        private System.Windows.Forms.NumericUpDown nudW;
        private System.Windows.Forms.NumericUpDown nudH;
        private System.Windows.Forms.RichTextBox richTextInput;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbSelector;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.RichTextBox richTextDebugOutput;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Button btnConfigurePalette;
    }
}