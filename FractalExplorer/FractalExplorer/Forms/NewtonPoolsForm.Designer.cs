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
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnConfigurePalette = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.cbSelector = new System.Windows.Forms.ComboBox();
            this.panel4 = new System.Windows.Forms.Panel();
            this.richTextInput = new System.Windows.Forms.RichTextBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.richTextDebugOutput = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.nudW = new System.Windows.Forms.NumericUpDown();
            this.nudH = new System.Windows.Forms.NumericUpDown();
            this.progressPNG = new System.Windows.Forms.ProgressBar();
            this.label8 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.nudZoom = new System.Windows.Forms.NumericUpDown();
            this.btnRender = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.label6 = new System.Windows.Forms.Label();
            this.cbThreads = new System.Windows.Forms.ComboBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.nudIterations = new System.Windows.Forms.NumericUpDown();
            this.fractal_bitmap = new System.Windows.Forms.PictureBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudH)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fractal_bitmap)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnConfigurePalette);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.nudW);
            this.panel1.Controls.Add(this.nudH);
            this.panel1.Controls.Add(this.progressPNG);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.nudZoom);
            this.panel1.Controls.Add(this.btnRender);
            this.panel1.Controls.Add(this.progressBar);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.cbThreads);
            this.panel1.Controls.Add(this.btnSave);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.nudIterations);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(231, 636);
            this.panel1.TabIndex = 0;
            // 
            // btnConfigurePalette
            // 
            this.btnConfigurePalette.Location = new System.Drawing.Point(32, 360);
            this.btnConfigurePalette.Name = "btnConfigurePalette";
            this.btnConfigurePalette.Size = new System.Drawing.Size(164, 23);
            this.btnConfigurePalette.TabIndex = 41;
            this.btnConfigurePalette.Text = "Настроить палитру";
            this.btnConfigurePalette.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.panel5);
            this.panel2.Controls.Add(this.panel4);
            this.panel2.Controls.Add(this.panel3);
            this.panel2.Controls.Add(this.richTextDebugOutput);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 427);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(231, 209);
            this.panel2.TabIndex = 40;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.cbSelector);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel5.Location = new System.Drawing.Point(0, 21);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(231, 24);
            this.panel5.TabIndex = 41;
            // 
            // cbSelector
            // 
            this.cbSelector.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.cbSelector.FormattingEnabled = true;
            this.cbSelector.Location = new System.Drawing.Point(0, 1);
            this.cbSelector.Name = "cbSelector";
            this.cbSelector.Size = new System.Drawing.Size(231, 23);
            this.cbSelector.TabIndex = 34;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.richTextInput);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel4.Location = new System.Drawing.Point(0, 45);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(231, 64);
            this.panel4.TabIndex = 41;
            // 
            // richTextInput
            // 
            this.richTextInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextInput.Location = new System.Drawing.Point(0, 0);
            this.richTextInput.Name = "richTextInput";
            this.richTextInput.Size = new System.Drawing.Size(231, 64);
            this.richTextInput.TabIndex = 33;
            this.richTextInput.Text = "";
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.label2);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel3.Location = new System.Drawing.Point(0, 109);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(231, 23);
            this.panel3.TabIndex = 37;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(87, 5);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(52, 15);
            this.label2.TabIndex = 38;
            this.label2.Text = "Отладка";
            // 
            // richTextDebugOutput
            // 
            this.richTextDebugOutput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.richTextDebugOutput.Location = new System.Drawing.Point(0, 132);
            this.richTextDebugOutput.Name = "richTextDebugOutput";
            this.richTextDebugOutput.ReadOnly = true;
            this.richTextDebugOutput.Size = new System.Drawing.Size(231, 77);
            this.richTextDebugOutput.TabIndex = 36;
            this.richTextDebugOutput.Text = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(36, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(163, 15);
            this.label1.TabIndex = 35;
            this.label1.Text = "Выбери полином/формулу.";
            // 
            // nudW
            // 
            this.nudW.Location = new System.Drawing.Point(16, 239);
            this.nudW.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudW.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudW.Name = "nudW";
            this.nudW.Size = new System.Drawing.Size(86, 23);
            this.nudW.TabIndex = 23;
            this.nudW.Value = new decimal(new int[] {
            1920,
            0,
            0,
            0});
            // 
            // nudH
            // 
            this.nudH.Location = new System.Drawing.Point(128, 239);
            this.nudH.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudH.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudH.Name = "nudH";
            this.nudH.Size = new System.Drawing.Size(83, 23);
            this.nudH.TabIndex = 22;
            this.nudH.Value = new decimal(new int[] {
            1080,
            0,
            0,
            0});
            // 
            // progressPNG
            // 
            this.progressPNG.Location = new System.Drawing.Point(7, 268);
            this.progressPNG.Name = "progressPNG";
            this.progressPNG.Size = new System.Drawing.Size(218, 23);
            this.progressPNG.TabIndex = 21;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(89, 163);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(67, 15);
            this.label8.TabIndex = 20;
            this.label8.Text = "Обработка";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 40);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(86, 15);
            this.label5.TabIndex = 16;
            this.label5.Text = "Приближение";
            // 
            // nudZoom
            // 
            this.nudZoom.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudZoom.Location = new System.Drawing.Point(14, 58);
            this.nudZoom.Maximum = new decimal(new int[] {
            268435455,
            1042612833,
            542101086,
            0});
            this.nudZoom.Name = "nudZoom";
            this.nudZoom.Size = new System.Drawing.Size(196, 23);
            this.nudZoom.TabIndex = 2;
            // 
            // btnRender
            // 
            this.btnRender.Location = new System.Drawing.Point(36, 137);
            this.btnRender.Name = "btnRender";
            this.btnRender.Size = new System.Drawing.Size(164, 23);
            this.btnRender.TabIndex = 2;
            this.btnRender.Text = "Запустить рендер";
            this.btnRender.UseVisualStyleBackColor = true;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(7, 181);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(218, 23);
            this.progressBar.TabIndex = 14;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 85);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(69, 15);
            this.label6.TabIndex = 13;
            this.label6.Text = "Потоки ЦП";
            // 
            // cbThreads
            // 
            this.cbThreads.FormattingEnabled = true;
            this.cbThreads.Location = new System.Drawing.Point(14, 103);
            this.cbThreads.Name = "cbThreads";
            this.cbThreads.Size = new System.Drawing.Size(195, 23);
            this.cbThreads.TabIndex = 12;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(32, 210);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(164, 23);
            this.btnSave.TabIndex = 11;
            this.btnSave.Text = "Сохранить изображение";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(138, 14);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 15);
            this.label3.TabIndex = 6;
            this.label3.Text = "Итерации";
            // 
            // nudIterations
            // 
            this.nudIterations.Location = new System.Drawing.Point(12, 12);
            this.nudIterations.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.nudIterations.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudIterations.Name = "nudIterations";
            this.nudIterations.Size = new System.Drawing.Size(120, 23);
            this.nudIterations.TabIndex = 2;
            this.nudIterations.Value = new decimal(new int[] {
            500,
            0,
            0,
            0});
            // 
            // fractal_bitmap
            // 
            this.fractal_bitmap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fractal_bitmap.Location = new System.Drawing.Point(231, 0);
            this.fractal_bitmap.Name = "fractal_bitmap";
            this.fractal_bitmap.Size = new System.Drawing.Size(853, 636);
            this.fractal_bitmap.TabIndex = 1;
            this.fractal_bitmap.TabStop = false;
            // 
            // NewtonPools
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 636);
            this.Controls.Add(this.fractal_bitmap);
            this.Controls.Add(this.panel1);
            this.MinimumSize = new System.Drawing.Size(1100, 675);
            this.Name = "NewtonPools";
            this.Text = "Бассейны Ньютона";
            this.Load += new System.EventHandler(this.NewtonPools_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel5.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudH)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fractal_bitmap)).EndInit();
            this.ResumeLayout(false);
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