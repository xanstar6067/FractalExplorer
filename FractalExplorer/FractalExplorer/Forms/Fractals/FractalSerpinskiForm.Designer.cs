namespace FractalExplorer
{
    partial class FractalSerpinski
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FractalSerpinski));
            panel1 = new Panel();
            color_configurations = new Button();
            btnStateManager = new Button();
            abortRender = new Button();
            FractalTypeIsChaos = new CheckBox();
            FractalTypeIsGeometry = new CheckBox();
            label8 = new Label();
            label5 = new Label();
            nudZoom = new NumericUpDown();
            btnRender = new Button();
            progressBarSerpinsky = new ProgressBar();
            label6 = new Label();
            cbCPUThreads = new ComboBox();
            btnSavePNG = new Button();
            label3 = new Label();
            nudIterations = new NumericUpDown();
            canvasSerpinsky = new PictureBox();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)canvasSerpinsky).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(color_configurations);
            panel1.Controls.Add(btnStateManager);
            panel1.Controls.Add(abortRender);
            panel1.Controls.Add(FractalTypeIsChaos);
            panel1.Controls.Add(FractalTypeIsGeometry);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(nudZoom);
            panel1.Controls.Add(btnRender);
            panel1.Controls.Add(progressBarSerpinsky);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(cbCPUThreads);
            panel1.Controls.Add(btnSavePNG);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(nudIterations);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(231, 636);
            panel1.TabIndex = 1;
            // 
            // color_configurations
            // 
            color_configurations.Location = new Point(4, 354);
            color_configurations.Name = "color_configurations";
            color_configurations.Size = new Size(218, 34);
            color_configurations.TabIndex = 15;
            color_configurations.Text = "Настроить палитру";
            color_configurations.UseVisualStyleBackColor = true;
            color_configurations.Click += color_configurations_Click;
            // 
            // btnStateManager
            // 
            btnStateManager.Location = new Point(4, 314);
            btnStateManager.Name = "btnStateManager";
            btnStateManager.Size = new Size(218, 34);
            btnStateManager.TabIndex = 14;
            btnStateManager.Text = "Менеджер сохранений";
            btnStateManager.UseVisualStyleBackColor = true;
            btnStateManager.Click += btnStateManager_Click;
            // 
            // abortRender
            // 
            abortRender.Location = new Point(130, 201);
            abortRender.Name = "abortRender";
            abortRender.Size = new Size(89, 23);
            abortRender.TabIndex = 6;
            abortRender.Text = "Отмена";
            abortRender.UseVisualStyleBackColor = true;
            abortRender.Click += abortRender_Click;
            // 
            // FractalTypeIsChaos
            // 
            FractalTypeIsChaos.AutoSize = true;
            FractalTypeIsChaos.Location = new Point(130, 85);
            FractalTypeIsChaos.Name = "FractalTypeIsChaos";
            FractalTypeIsChaos.Size = new Size(81, 19);
            FractalTypeIsChaos.TabIndex = 3;
            FractalTypeIsChaos.Text = "Игра хаос";
            FractalTypeIsChaos.UseVisualStyleBackColor = true;
            // 
            // FractalTypeIsGeometry
            // 
            FractalTypeIsGeometry.AutoSize = true;
            FractalTypeIsGeometry.Location = new Point(13, 85);
            FractalTypeIsGeometry.Name = "FractalTypeIsGeometry";
            FractalTypeIsGeometry.Size = new Size(118, 19);
            FractalTypeIsGeometry.TabIndex = 2;
            FractalTypeIsGeometry.Text = "Геометрический";
            FractalTypeIsGeometry.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(80, 227);
            label8.Name = "label8";
            label8.Size = new Size(67, 15);
            label8.TabIndex = 7;
            label8.Text = "Обработка";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(13, 38);
            label5.Name = "label5";
            label5.Size = new Size(86, 15);
            label5.TabIndex = 16;
            label5.Text = "Приближение";
            // 
            // nudZoom
            // 
            nudZoom.DecimalPlaces = 2;
            nudZoom.Location = new Point(12, 56);
            nudZoom.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new Size(196, 23);
            nudZoom.TabIndex = 1;
            nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnRender
            // 
            btnRender.Location = new Point(4, 201);
            btnRender.Name = "btnRender";
            btnRender.Size = new Size(123, 23);
            btnRender.TabIndex = 5;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            btnRender.Click += btnRender_Click;
            // 
            // progressBarSerpinsky
            // 
            progressBarSerpinsky.Location = new Point(4, 245);
            progressBarSerpinsky.Name = "progressBarSerpinsky";
            progressBarSerpinsky.Size = new Size(218, 23);
            progressBarSerpinsky.TabIndex = 8;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(15, 154);
            label6.Name = "label6";
            label6.Size = new Size(69, 15);
            label6.TabIndex = 13;
            label6.Text = "Потоки ЦП";
            // 
            // cbCPUThreads
            // 
            cbCPUThreads.FormattingEnabled = true;
            cbCPUThreads.Location = new Point(15, 172);
            cbCPUThreads.Name = "cbCPUThreads";
            cbCPUThreads.Size = new Size(195, 23);
            cbCPUThreads.TabIndex = 4;
            // 
            // btnSavePNG
            // 
            btnSavePNG.Location = new Point(4, 274);
            btnSavePNG.Name = "btnSavePNG";
            btnSavePNG.Size = new Size(218, 34);
            btnSavePNG.TabIndex = 11;
            btnSavePNG.Text = "Сохранить изображение";
            btnSavePNG.UseVisualStyleBackColor = true;
            btnSavePNG.Click += btnOpenSaveManager_Click;
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
            nudIterations.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new Size(120, 23);
            nudIterations.TabIndex = 0;
            nudIterations.Value = new decimal(new int[] { 8, 0, 0, 0 });
            // 
            // canvasSerpinsky
            // 
            canvasSerpinsky.Dock = DockStyle.Fill;
            canvasSerpinsky.Location = new Point(231, 0);
            canvasSerpinsky.Name = "canvasSerpinsky";
            canvasSerpinsky.Size = new Size(853, 636);
            canvasSerpinsky.TabIndex = 2;
            canvasSerpinsky.TabStop = false;
            // 
            // FractalSerpinski
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(canvasSerpinsky);
            Controls.Add(panel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1100, 675);
            Name = "FractalSerpinski";
            Text = "Треугольник Серпинского";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
            ((System.ComponentModel.ISupportInitialize)canvasSerpinsky).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
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