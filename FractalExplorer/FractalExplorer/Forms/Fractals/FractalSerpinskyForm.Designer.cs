namespace FractalExplorer
{
    partial class FractalSerpinsky
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.color_configurations = new System.Windows.Forms.Button();
            this.btnStateManager = new System.Windows.Forms.Button();
            this.abortRender = new System.Windows.Forms.Button();
            this.FractalTypeIsChaos = new System.Windows.Forms.CheckBox();
            this.FractalTypeIsGeometry = new System.Windows.Forms.CheckBox();
            this.nudW2 = new System.Windows.Forms.NumericUpDown();
            this.nudH2 = new System.Windows.Forms.NumericUpDown();
            this.progressPNGSerpinsky = new System.Windows.Forms.ProgressBar();
            this.label8 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.nudZoom = new System.Windows.Forms.NumericUpDown();
            this.btnRender = new System.Windows.Forms.Button();
            this.progressBarSerpinsky = new System.Windows.Forms.ProgressBar();
            this.label6 = new System.Windows.Forms.Label();
            this.cbCPUThreads = new System.Windows.Forms.ComboBox();
            this.btnSavePNG = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.nudIterations = new System.Windows.Forms.NumericUpDown();
            this.canvasSerpinsky = new System.Windows.Forms.PictureBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudW2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudH2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvasSerpinsky)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.color_configurations);
            this.panel1.Controls.Add(this.btnStateManager);
            this.panel1.Controls.Add(this.abortRender);
            this.panel1.Controls.Add(this.FractalTypeIsChaos);
            this.panel1.Controls.Add(this.FractalTypeIsGeometry);
            this.panel1.Controls.Add(this.nudW2);
            this.panel1.Controls.Add(this.nudH2);
            this.panel1.Controls.Add(this.progressPNGSerpinsky);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.nudZoom);
            this.panel1.Controls.Add(this.btnRender);
            this.panel1.Controls.Add(this.progressBarSerpinsky);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.cbCPUThreads);
            this.panel1.Controls.Add(this.btnSavePNG);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.nudIterations);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(231, 636);
            this.panel1.TabIndex = 1;
            // 
            // color_configurations
            // 
            this.color_configurations.Location = new System.Drawing.Point(4, 396);
            this.color_configurations.Name = "color_configurations";
            this.color_configurations.Size = new System.Drawing.Size(218, 34);
            this.color_configurations.TabIndex = 15;
            this.color_configurations.Text = "Настроить палитру";
            this.color_configurations.UseVisualStyleBackColor = true;
            this.color_configurations.Click += new System.EventHandler(this.color_configurations_Click);
            // 
            // btnStateManager
            // 
            this.btnStateManager.Location = new System.Drawing.Point(4, 356);
            this.btnStateManager.Name = "btnStateManager";
            this.btnStateManager.Size = new System.Drawing.Size(218, 34);
            this.btnStateManager.TabIndex = 14;
            this.btnStateManager.Text = "Менеджер сохранений";
            this.btnStateManager.UseVisualStyleBackColor = true;
            this.btnStateManager.Click += new System.EventHandler(this.btnStateManager_Click);
            // 
            // abortRender
            // 
            this.abortRender.Location = new System.Drawing.Point(130, 201);
            this.abortRender.Name = "abortRender";
            this.abortRender.Size = new System.Drawing.Size(89, 23);
            this.abortRender.TabIndex = 6;
            this.abortRender.Text = "Отмена";
            this.abortRender.UseVisualStyleBackColor = true;
            this.abortRender.Click += new System.EventHandler(this.abortRender_Click);
            // 
            // FractalTypeIsChaos
            // 
            this.FractalTypeIsChaos.AutoSize = true;
            this.FractalTypeIsChaos.Location = new System.Drawing.Point(130, 85);
            this.FractalTypeIsChaos.Name = "FractalTypeIsChaos";
            this.FractalTypeIsChaos.Size = new System.Drawing.Size(81, 19);
            this.FractalTypeIsChaos.TabIndex = 3;
            this.FractalTypeIsChaos.Text = "Игра хаос";
            this.FractalTypeIsChaos.UseVisualStyleBackColor = true;
            // 
            // FractalTypeIsGeometry
            // 
            this.FractalTypeIsGeometry.AutoSize = true;
            this.FractalTypeIsGeometry.Location = new System.Drawing.Point(13, 85);
            this.FractalTypeIsGeometry.Name = "FractalTypeIsGeometry";
            this.FractalTypeIsGeometry.Size = new System.Drawing.Size(118, 19);
            this.FractalTypeIsGeometry.TabIndex = 2;
            this.FractalTypeIsGeometry.Text = "Геометрический";
            this.FractalTypeIsGeometry.UseVisualStyleBackColor = true;
            // 
            // nudW2
            // 
            this.nudW2.Location = new System.Drawing.Point(8, 297);
            this.nudW2.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudW2.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudW2.Name = "nudW2";
            this.nudW2.Size = new System.Drawing.Size(86, 23);
            this.nudW2.TabIndex = 9;
            this.nudW2.Value = new decimal(new int[] {
            1920,
            0,
            0,
            0});
            // 
            // nudH2
            // 
            this.nudH2.Location = new System.Drawing.Point(120, 297);
            this.nudH2.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudH2.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudH2.Name = "nudH2";
            this.nudH2.Size = new System.Drawing.Size(83, 23);
            this.nudH2.TabIndex = 10;
            this.nudH2.Value = new decimal(new int[] {
            1080,
            0,
            0,
            0});
            // 
            // progressPNGSerpinsky
            // 
            this.progressPNGSerpinsky.Location = new System.Drawing.Point(4, 327);
            this.progressPNGSerpinsky.Name = "progressPNGSerpinsky";
            this.progressPNGSerpinsky.Size = new System.Drawing.Size(218, 23);
            this.progressPNGSerpinsky.TabIndex = 12;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(80, 227);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(67, 15);
            this.label8.TabIndex = 7;
            this.label8.Text = "Обработка";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 38);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(86, 15);
            this.label5.TabIndex = 16;
            this.label5.Text = "Приближение";
            // 
            // nudZoom
            // 
            this.nudZoom.DecimalPlaces = 2;
            this.nudZoom.Location = new System.Drawing.Point(12, 56);
            this.nudZoom.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.nudZoom.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.nudZoom.Name = "nudZoom";
            this.nudZoom.Size = new System.Drawing.Size(196, 23);
            this.nudZoom.TabIndex = 1;
            this.nudZoom.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // btnRender
            // 
            this.btnRender.Location = new System.Drawing.Point(4, 201);
            this.btnRender.Name = "btnRender";
            this.btnRender.Size = new System.Drawing.Size(123, 23);
            this.btnRender.TabIndex = 5;
            this.btnRender.Text = "Запустить рендер";
            this.btnRender.UseVisualStyleBackColor = true;
            this.btnRender.Click += new System.EventHandler(this.btnRender_Click);
            // 
            // progressBarSerpinsky
            // 
            this.progressBarSerpinsky.Location = new System.Drawing.Point(4, 245);
            this.progressBarSerpinsky.Name = "progressBarSerpinsky";
            this.progressBarSerpinsky.Size = new System.Drawing.Size(218, 23);
            this.progressBarSerpinsky.TabIndex = 8;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 154);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(69, 15);
            this.label6.TabIndex = 13;
            this.label6.Text = "Потоки ЦП";
            // 
            // cbCPUThreads
            // 
            this.cbCPUThreads.FormattingEnabled = true;
            this.cbCPUThreads.Location = new System.Drawing.Point(15, 172);
            this.cbCPUThreads.Name = "cbCPUThreads";
            this.cbCPUThreads.Size = new System.Drawing.Size(195, 23);
            this.cbCPUThreads.TabIndex = 4;
            // 
            // btnSavePNG
            // 
            this.btnSavePNG.Location = new System.Drawing.Point(22, 268);
            this.btnSavePNG.Name = "btnSavePNG";
            this.btnSavePNG.Size = new System.Drawing.Size(182, 23);
            this.btnSavePNG.TabIndex = 11;
            this.btnSavePNG.Text = "Сохранить изображение";
            this.btnSavePNG.UseVisualStyleBackColor = true;
            this.btnSavePNG.Click += new System.EventHandler(this.btnOpenSaveManager_Click);
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
            20,
            0,
            0,
            0});
            this.nudIterations.Name = "nudIterations";
            this.nudIterations.Size = new System.Drawing.Size(120, 23);
            this.nudIterations.TabIndex = 0;
            this.nudIterations.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            // 
            // canvasSerpinsky
            // 
            this.canvasSerpinsky.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvasSerpinsky.Location = new System.Drawing.Point(231, 0);
            this.canvasSerpinsky.Name = "canvasSerpinsky";
            this.canvasSerpinsky.Size = new System.Drawing.Size(853, 636);
            this.canvasSerpinsky.TabIndex = 2;
            this.canvasSerpinsky.TabStop = false;
            // 
            // FractalSerpinsky
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 636);
            this.Controls.Add(this.canvasSerpinsky);
            this.Controls.Add(this.panel1);
            this.MinimumSize = new System.Drawing.Size(1100, 675);
            this.Name = "FractalSerpinsky";
            this.Text = "Треугольник Серпинского";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudW2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudH2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvasSerpinsky)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.NumericUpDown nudW2;
        private System.Windows.Forms.NumericUpDown nudH2;
        private System.Windows.Forms.ProgressBar progressPNGSerpinsky;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown nudZoom;
        private System.Windows.Forms.Button btnRender;
        private System.Windows.Forms.ProgressBar progressBarSerpinsky;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbCPUThreads;
        private System.Windows.Forms.Button btnSavePNG;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nudIterations;
        private System.Windows.Forms.PictureBox canvasSerpinsky;
        private System.Windows.Forms.CheckBox FractalTypeIsChaos;
        private System.Windows.Forms.CheckBox FractalTypeIsGeometry;
        private System.Windows.Forms.Button abortRender;
        private System.Windows.Forms.Button color_configurations;
        private System.Windows.Forms.Button btnStateManager;
    }
}