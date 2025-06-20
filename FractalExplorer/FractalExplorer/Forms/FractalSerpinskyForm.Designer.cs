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
            panel1 = new Panel();
            abortRender = new Button();
            panel3 = new Panel();
            label2 = new Label();
            label1 = new Label();
            colorColor = new CheckBox();
            renderBW = new CheckBox();
            colorGrayscale = new CheckBox();
            colorBackground = new CheckBox();
            colorFractal = new CheckBox();
            FractalTypeIsChaos = new CheckBox();
            FractalTypeIsGeometry = new CheckBox();
            panel2 = new Panel();
            canvasPalette = new PictureBox();
            nudW2 = new NumericUpDown();
            nudH2 = new NumericUpDown();
            progressPNGSerpinsky = new ProgressBar();
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
            panel3.SuspendLayout();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)canvasPalette).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudW2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudH2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)canvasSerpinsky).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(abortRender);
            panel1.Controls.Add(panel3);
            panel1.Controls.Add(FractalTypeIsChaos);
            panel1.Controls.Add(FractalTypeIsGeometry);
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(nudW2);
            panel1.Controls.Add(nudH2);
            panel1.Controls.Add(progressPNGSerpinsky);
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
            // abortRender
            // 
            abortRender.Location = new Point(134, 271);
            abortRender.Name = "abortRender";
            abortRender.Size = new Size(89, 23);
            abortRender.TabIndex = 39;
            abortRender.Text = "Отмена";
            abortRender.UseVisualStyleBackColor = true;
            abortRender.Click += abortRender_Click;
            // 
            // panel3
            // 
            panel3.Controls.Add(label2);
            panel3.Controls.Add(label1);
            panel3.Controls.Add(colorColor);
            panel3.Controls.Add(renderBW);
            panel3.Controls.Add(colorGrayscale);
            panel3.Controls.Add(colorBackground);
            panel3.Controls.Add(colorFractal);
            panel3.Dock = DockStyle.Bottom;
            panel3.Location = new Point(0, 431);
            panel3.Name = "panel3";
            panel3.Size = new Size(231, 83);
            panel3.TabIndex = 38;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(35, 65);
            label2.Name = "label2";
            label2.Size = new Size(164, 15);
            label2.TabIndex = 38;
            label2.Text = "Кликни для настройки цвета";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(67, 7);
            label1.Name = "label1";
            label1.Size = new Size(91, 15);
            label1.TabIndex = 32;
            label1.Text = "Выберите цвет.";
            // 
            // colorColor
            // 
            colorColor.AutoSize = true;
            colorColor.Location = new Point(169, 25);
            colorColor.Name = "colorColor";
            colorColor.Size = new Size(52, 19);
            colorColor.TabIndex = 37;
            colorColor.Text = "Цвет";
            colorColor.UseVisualStyleBackColor = true;
            // 
            // renderBW
            // 
            renderBW.AutoSize = true;
            renderBW.Location = new Point(15, 25);
            renderBW.Name = "renderBW";
            renderBW.Size = new Size(41, 19);
            renderBW.TabIndex = 17;
            renderBW.Text = "ЧБ";
            renderBW.UseVisualStyleBackColor = true;
            // 
            // colorGrayscale
            // 
            colorGrayscale.AutoSize = true;
            colorGrayscale.Location = new Point(60, 25);
            colorGrayscale.Name = "colorGrayscale";
            colorGrayscale.Size = new Size(112, 19);
            colorGrayscale.TabIndex = 30;
            colorGrayscale.Text = "Оттенки серого";
            colorGrayscale.UseVisualStyleBackColor = true;
            // 
            // colorBackground
            // 
            colorBackground.AutoSize = true;
            colorBackground.Location = new Point(76, 44);
            colorBackground.Name = "colorBackground";
            colorBackground.Size = new Size(49, 19);
            colorBackground.TabIndex = 33;
            colorBackground.Text = "Фон";
            colorBackground.UseVisualStyleBackColor = true;
            // 
            // colorFractal
            // 
            colorFractal.AutoSize = true;
            colorFractal.Location = new Point(131, 44);
            colorFractal.Name = "colorFractal";
            colorFractal.Size = new Size(66, 19);
            colorFractal.TabIndex = 34;
            colorFractal.Text = "Фигура";
            colorFractal.UseVisualStyleBackColor = true;
            // 
            // FractalTypeIsChaos
            // 
            FractalTypeIsChaos.AutoSize = true;
            FractalTypeIsChaos.Location = new Point(130, 85);
            FractalTypeIsChaos.Name = "FractalTypeIsChaos";
            FractalTypeIsChaos.Size = new Size(81, 19);
            FractalTypeIsChaos.TabIndex = 36;
            FractalTypeIsChaos.Text = "Игра хаос";
            FractalTypeIsChaos.UseVisualStyleBackColor = true;
            // 
            // FractalTypeIsGeometry
            // 
            FractalTypeIsGeometry.AutoSize = true;
            FractalTypeIsGeometry.Location = new Point(13, 85);
            FractalTypeIsGeometry.Name = "FractalTypeIsGeometry";
            FractalTypeIsGeometry.Size = new Size(118, 19);
            FractalTypeIsGeometry.TabIndex = 35;
            FractalTypeIsGeometry.Text = "Геометрический";
            FractalTypeIsGeometry.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Controls.Add(canvasPalette);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 514);
            panel2.Name = "panel2";
            panel2.Size = new Size(231, 122);
            panel2.TabIndex = 31;
            // 
            // canvasPalette
            // 
            canvasPalette.Dock = DockStyle.Fill;
            canvasPalette.Location = new Point(0, 0);
            canvasPalette.Name = "canvasPalette";
            canvasPalette.Size = new Size(229, 120);
            canvasPalette.TabIndex = 3;
            canvasPalette.TabStop = false;
            canvasPalette.Click += cancasPalette_Click;
            // 
            // nudW2
            // 
            nudW2.Location = new Point(12, 373);
            nudW2.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudW2.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudW2.Name = "nudW2";
            nudW2.Size = new Size(86, 23);
            nudW2.TabIndex = 23;
            nudW2.Value = new decimal(new int[] { 1920, 0, 0, 0 });
            // 
            // nudH2
            // 
            nudH2.Location = new Point(124, 373);
            nudH2.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudH2.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudH2.Name = "nudH2";
            nudH2.Size = new Size(83, 23);
            nudH2.TabIndex = 22;
            nudH2.Value = new decimal(new int[] { 1080, 0, 0, 0 });
            // 
            // progressPNGSerpinsky
            // 
            progressPNGSerpinsky.Location = new Point(8, 402);
            progressPNGSerpinsky.Name = "progressPNGSerpinsky";
            progressPNGSerpinsky.Size = new Size(218, 23);
            progressPNGSerpinsky.TabIndex = 21;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(84, 297);
            label8.Name = "label8";
            label8.Size = new Size(67, 15);
            label8.TabIndex = 20;
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
            nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudZoom.Location = new Point(12, 56);
            nudZoom.Maximum = new decimal(new int[] { 268435455, 1042612833, 542101086, 0 });
            nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new Size(196, 23);
            nudZoom.TabIndex = 2;
            nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnRender
            // 
            btnRender.Location = new Point(3, 271);
            btnRender.Name = "btnRender";
            btnRender.Size = new Size(123, 23);
            btnRender.TabIndex = 2;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            // 
            // progressBarSerpinsky
            // 
            progressBarSerpinsky.Location = new Point(5, 315);
            progressBarSerpinsky.Name = "progressBarSerpinsky";
            progressBarSerpinsky.Size = new Size(218, 23);
            progressBarSerpinsky.TabIndex = 14;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(19, 224);
            label6.Name = "label6";
            label6.Size = new Size(69, 15);
            label6.TabIndex = 13;
            label6.Text = "Потоки ЦП";
            // 
            // cbCPUThreads
            // 
            cbCPUThreads.FormattingEnabled = true;
            cbCPUThreads.Location = new Point(19, 242);
            cbCPUThreads.Name = "cbCPUThreads";
            cbCPUThreads.Size = new Size(195, 23);
            cbCPUThreads.TabIndex = 12;
            // 
            // btnSavePNG
            // 
            btnSavePNG.Location = new Point(26, 344);
            btnSavePNG.Name = "btnSavePNG";
            btnSavePNG.Size = new Size(164, 23);
            btnSavePNG.TabIndex = 11;
            btnSavePNG.Text = "Сохранить изображение";
            btnSavePNG.UseVisualStyleBackColor = true;
            btnSavePNG.Click += btnSavePNG_Click;
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
            nudIterations.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            nudIterations.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new Size(120, 23);
            nudIterations.TabIndex = 2;
            nudIterations.Value = new decimal(new int[] { 500, 0, 0, 0 });
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
            // FractalSerpinsky
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(canvasSerpinsky);
            Controls.Add(panel1);
            MinimumSize = new Size(1100, 675);
            Name = "FractalSerpinsky";
            Text = "Треугольник Серпинского";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)canvasPalette).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudW2).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudH2).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
            ((System.ComponentModel.ISupportInitialize)canvasSerpinsky).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private NumericUpDown nudW2;
        private NumericUpDown nudH2;
        private ProgressBar progressPNGSerpinsky;
        private Label label8;
        private CheckBox renderBW;
        private Label label5;
        private NumericUpDown nudZoom;
        private Button btnRender;
        private ProgressBar progressBarSerpinsky;
        private Label label6;
        private ComboBox cbCPUThreads;
        private Button btnSavePNG;
        private Label label3;
        private NumericUpDown nudIterations;
        private CheckBox colorGrayscale;
        private Panel panel2;
        private PictureBox canvasPalette;
        private PictureBox canvasSerpinsky;
        private Label label1;
        private CheckBox colorBackground;
        private CheckBox colorFractal;
        private CheckBox FractalTypeIsChaos;
        private CheckBox FractalTypeIsGeometry;
        private CheckBox colorColor;
        private Panel panel3;
        private Label label2;
        private Button abortRender;
    }
}