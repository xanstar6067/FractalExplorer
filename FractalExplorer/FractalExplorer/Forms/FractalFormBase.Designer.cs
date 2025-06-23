namespace FractalDraving
{
    // Этот файл создается для того, чтобы унаследовать от него все остальные формы.
    // Содержимое для него будет сгенерировано Visual Studio, когда вы создадите форму `FractalFormBase`.
    // Для простоты, мы будем редактировать `Designer.cs` каждой конкретной формы, 
    // приводя имена к общему виду, а `FractalFormBase` будет просто базовым классом Form.
    // Ниже я приведу исправленные `.Designer.cs` файлы для каждой из ваших форм.
    // ВАЖНО: Сам файл `FractalFormBase.Designer.cs` должен быть таким:
    partial class FractalFormBase
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        protected void InitializeComponent()
        {
            pnlControls = new Panel();
            color_configurations = new Button();
            mandelbrotPreviewPanel = new Panel();
            mandelbrotPreviewCanvas = new PictureBox();
            checkBox6 = new CheckBox();
            checkBox5 = new CheckBox();
            checkBox4 = new CheckBox();
            checkBox3 = new CheckBox();
            checkBox2 = new CheckBox();
            checkBox1 = new CheckBox();
            nudSaveWidth = new NumericUpDown();
            nudSaveHeight = new NumericUpDown();
            pbHighResProgress = new ProgressBar();
            lblProgress = new Label();
            lblLoupeZoom = new Label();
            nudBaseScale = new NumericUpDown();
            mondelbrotClassicBox = new CheckBox();
            oldRenderBW = new CheckBox();
            lblZoom = new Label();
            nudZoom = new NumericUpDown();
            colorBox = new CheckBox();
            btnRender = new Button();
            pbRenderProgress = new ProgressBar();
            lblThreads = new Label();
            cbThreads = new ComboBox();
            btnSaveHighRes = new Button();
            lblThreshold = new Label();
            lblIterations = new Label();
            lblIm = new Label();
            lblRe = new Label();
            nudThreshold = new NumericUpDown();
            nudIterations = new NumericUpDown();
            nudIm = new NumericUpDown();
            nudRe = new NumericUpDown();
            canvas = new PictureBox();
            pnlControls.SuspendLayout();
            mandelbrotPreviewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mandelbrotPreviewCanvas).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudSaveWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudSaveHeight).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudBaseScale).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIm).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudRe).BeginInit();
            ((System.ComponentModel.ISupportInitialize)canvas).BeginInit();
            SuspendLayout();
            // 
            // pnlControls
            // 
            pnlControls.Controls.Add(color_configurations);
            pnlControls.Controls.Add(mandelbrotPreviewPanel);
            pnlControls.Controls.Add(checkBox6);
            pnlControls.Controls.Add(checkBox5);
            pnlControls.Controls.Add(checkBox4);
            pnlControls.Controls.Add(checkBox3);
            pnlControls.Controls.Add(checkBox2);
            pnlControls.Controls.Add(checkBox1);
            pnlControls.Controls.Add(nudSaveWidth);
            pnlControls.Controls.Add(nudSaveHeight);
            pnlControls.Controls.Add(pbHighResProgress);
            pnlControls.Controls.Add(lblProgress);
            pnlControls.Controls.Add(lblLoupeZoom);
            pnlControls.Controls.Add(nudBaseScale);
            pnlControls.Controls.Add(mondelbrotClassicBox);
            pnlControls.Controls.Add(oldRenderBW);
            pnlControls.Controls.Add(lblZoom);
            pnlControls.Controls.Add(nudZoom);
            pnlControls.Controls.Add(colorBox);
            pnlControls.Controls.Add(btnRender);
            pnlControls.Controls.Add(pbRenderProgress);
            pnlControls.Controls.Add(lblThreads);
            pnlControls.Controls.Add(cbThreads);
            pnlControls.Controls.Add(btnSaveHighRes);
            pnlControls.Controls.Add(lblThreshold);
            pnlControls.Controls.Add(lblIterations);
            pnlControls.Controls.Add(lblIm);
            pnlControls.Controls.Add(lblRe);
            pnlControls.Controls.Add(nudThreshold);
            pnlControls.Controls.Add(nudIterations);
            pnlControls.Controls.Add(nudIm);
            pnlControls.Controls.Add(nudRe);
            pnlControls.Dock = DockStyle.Left;
            pnlControls.Location = new Point(0, 0);
            pnlControls.Name = "pnlControls";
            pnlControls.Size = new Size(231, 636);
            pnlControls.TabIndex = 0;
            // 
            // color_configurations
            // 
            color_configurations.Location = new Point(35, 368);
            color_configurations.Name = "color_configurations";
            color_configurations.Size = new Size(164, 23);
            color_configurations.TabIndex = 33;
            color_configurations.Text = "Настроить палитру";
            color_configurations.UseVisualStyleBackColor = true;
            color_configurations.Click += color_configurations_Click;
            // 
            // mandelbrotPreviewPanel
            // 
            mandelbrotPreviewPanel.Controls.Add(mandelbrotPreviewCanvas);
            mandelbrotPreviewPanel.Dock = DockStyle.Bottom;
            mandelbrotPreviewPanel.Location = new Point(0, 470);
            mandelbrotPreviewPanel.Name = "mandelbrotPreviewPanel";
            mandelbrotPreviewPanel.Size = new Size(231, 166);
            mandelbrotPreviewPanel.TabIndex = 31;
            // 
            // mandelbrotPreviewCanvas
            // 
            mandelbrotPreviewCanvas.Dock = DockStyle.Fill;
            mandelbrotPreviewCanvas.Location = new Point(0, 0);
            mandelbrotPreviewCanvas.Name = "mandelbrotPreviewCanvas";
            mandelbrotPreviewCanvas.Size = new Size(231, 166);
            mandelbrotPreviewCanvas.TabIndex = 30;
            mandelbrotPreviewCanvas.TabStop = false;
            // 
            // checkBox6
            // 
            checkBox6.AutoSize = true;
            checkBox6.Location = new Point(12, 347);
            checkBox6.Name = "checkBox6";
            checkBox6.Size = new Size(32, 19);
            checkBox6.TabIndex = 29;
            checkBox6.Text = "6";
            checkBox6.UseVisualStyleBackColor = true;
            checkBox6.Visible = false;
            // 
            // checkBox5
            // 
            checkBox5.AutoSize = true;
            checkBox5.Location = new Point(12, 347);
            checkBox5.Name = "checkBox5";
            checkBox5.Size = new Size(32, 19);
            checkBox5.TabIndex = 28;
            checkBox5.Text = "5";
            checkBox5.UseVisualStyleBackColor = true;
            checkBox5.Visible = false;
            // 
            // checkBox4
            // 
            checkBox4.AutoSize = true;
            checkBox4.Location = new Point(28, 347);
            checkBox4.Name = "checkBox4";
            checkBox4.Size = new Size(32, 19);
            checkBox4.TabIndex = 27;
            checkBox4.Text = "4";
            checkBox4.UseVisualStyleBackColor = true;
            checkBox4.Visible = false;
            // 
            // checkBox3
            // 
            checkBox3.AutoSize = true;
            checkBox3.Location = new Point(28, 347);
            checkBox3.Name = "checkBox3";
            checkBox3.Size = new Size(32, 19);
            checkBox3.TabIndex = 26;
            checkBox3.Text = "3";
            checkBox3.UseVisualStyleBackColor = true;
            checkBox3.Visible = false;
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Location = new Point(11, 347);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(32, 19);
            checkBox2.TabIndex = 25;
            checkBox2.Text = "2";
            checkBox2.UseVisualStyleBackColor = true;
            checkBox2.Visible = false;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(42, 347);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(32, 19);
            checkBox1.TabIndex = 24;
            checkBox1.Text = "1";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.Visible = false;
            // 
            // nudSaveWidth
            // 
            nudSaveWidth.Location = new Point(12, 289);
            nudSaveWidth.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudSaveWidth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudSaveWidth.Name = "nudSaveWidth";
            nudSaveWidth.Size = new Size(86, 23);
            nudSaveWidth.TabIndex = 23;
            nudSaveWidth.Value = new decimal(new int[] { 1920, 0, 0, 0 });
            // 
            // nudSaveHeight
            // 
            nudSaveHeight.Location = new Point(124, 289);
            nudSaveHeight.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            nudSaveHeight.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudSaveHeight.Name = "nudSaveHeight";
            nudSaveHeight.Size = new Size(83, 23);
            nudSaveHeight.TabIndex = 22;
            nudSaveHeight.Value = new decimal(new int[] { 1080, 0, 0, 0 });
            // 
            // pbHighResProgress
            // 
            pbHighResProgress.Location = new Point(3, 318);
            pbHighResProgress.Name = "pbHighResProgress";
            pbHighResProgress.Size = new Size(218, 23);
            pbHighResProgress.TabIndex = 21;
            // 
            // lblProgress
            // 
            lblProgress.AutoSize = true;
            lblProgress.Location = new Point(88, 423);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(67, 15);
            lblProgress.TabIndex = 20;
            lblProgress.Text = "Обработка";
            // 
            // lblLoupeZoom
            // 
            lblLoupeZoom.AutoSize = true;
            lblLoupeZoom.Location = new Point(12, 169);
            lblLoupeZoom.Name = "lblLoupeZoom";
            lblLoupeZoom.Size = new Size(105, 15);
            lblLoupeZoom.TabIndex = 19;
            lblLoupeZoom.Text = "Увеличение лупы";
            lblLoupeZoom.Visible = false;
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
            // mondelbrotClassicBox
            // 
            mondelbrotClassicBox.AutoSize = true;
            mondelbrotClassicBox.Location = new Point(12, 347);
            mondelbrotClassicBox.Name = "mondelbrotClassicBox";
            mondelbrotClassicBox.Size = new Size(77, 19);
            mondelbrotClassicBox.TabIndex = 32;
            mondelbrotClassicBox.Text = "Классика";
            mondelbrotClassicBox.UseVisualStyleBackColor = true;
            mondelbrotClassicBox.Visible = false;
            // 
            // oldRenderBW
            // 
            oldRenderBW.AutoSize = true;
            oldRenderBW.Location = new Point(19, 347);
            oldRenderBW.Name = "oldRenderBW";
            oldRenderBW.Size = new Size(41, 19);
            oldRenderBW.TabIndex = 17;
            oldRenderBW.Text = "ЧБ";
            oldRenderBW.UseVisualStyleBackColor = true;
            oldRenderBW.Visible = false;
            // 
            // lblZoom
            // 
            lblZoom.AutoSize = true;
            lblZoom.Location = new Point(12, 125);
            lblZoom.Name = "lblZoom";
            lblZoom.Size = new Size(86, 15);
            lblZoom.TabIndex = 16;
            lblZoom.Text = "Приближение";
            // 
            // nudZoom
            // 
            nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudZoom.Location = new Point(11, 143);
            nudZoom.Maximum = new decimal(new int[] { -1, -1, -1, 0 });
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
            colorBox.Visible = false;
            // 
            // btnRender
            // 
            btnRender.Location = new Point(35, 397);
            btnRender.Name = "btnRender";
            btnRender.Size = new Size(164, 23);
            btnRender.TabIndex = 2;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            // 
            // pbRenderProgress
            // 
            pbRenderProgress.Location = new Point(6, 441);
            pbRenderProgress.Name = "pbRenderProgress";
            pbRenderProgress.Size = new Size(218, 23);
            pbRenderProgress.TabIndex = 14;
            // 
            // lblThreads
            // 
            lblThreads.AutoSize = true;
            lblThreads.Location = new Point(12, 213);
            lblThreads.Name = "lblThreads";
            lblThreads.Size = new Size(69, 15);
            lblThreads.TabIndex = 13;
            lblThreads.Text = "Потоки ЦП";
            // 
            // cbThreads
            // 
            cbThreads.FormattingEnabled = true;
            cbThreads.Location = new Point(12, 231);
            cbThreads.Name = "cbThreads";
            cbThreads.Size = new Size(195, 23);
            cbThreads.TabIndex = 12;
            // 
            // btnSaveHighRes
            // 
            btnSaveHighRes.Location = new Point(28, 260);
            btnSaveHighRes.Name = "btnSaveHighRes";
            btnSaveHighRes.Size = new Size(164, 23);
            btnSaveHighRes.TabIndex = 11;
            btnSaveHighRes.Text = "Сохранить изображение";
            btnSaveHighRes.UseVisualStyleBackColor = true;
            // 
            // lblThreshold
            // 
            lblThreshold.AutoSize = true;
            lblThreshold.Location = new Point(138, 101);
            lblThreshold.Name = "lblThreshold";
            lblThreshold.Size = new Size(85, 15);
            lblThreshold.TabIndex = 7;
            lblThreshold.Text = "Порог выхода";
            // 
            // lblIterations
            // 
            lblIterations.AutoSize = true;
            lblIterations.Location = new Point(138, 72);
            lblIterations.Name = "lblIterations";
            lblIterations.Size = new Size(61, 15);
            lblIterations.TabIndex = 6;
            lblIterations.Text = "Итерации";
            // 
            // lblIm
            // 
            lblIm.AutoSize = true;
            lblIm.Location = new Point(138, 43);
            lblIm.Name = "lblIm";
            lblIm.Size = new Size(54, 15);
            lblIm.TabIndex = 5;
            lblIm.Text = "Мнимое";
            // 
            // lblRe
            // 
            lblRe.AutoSize = true;
            lblRe.Location = new Point(131, 14);
            lblRe.Name = "lblRe";
            lblRe.Size = new Size(96, 15);
            lblRe.TabIndex = 4;
            lblRe.Text = "Действительное";
            // 
            // nudThreshold
            // 
            nudThreshold.DecimalPlaces = 3;
            nudThreshold.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudThreshold.Location = new Point(12, 99);
            nudThreshold.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            nudThreshold.Name = "nudThreshold";
            nudThreshold.Size = new Size(120, 23);
            nudThreshold.TabIndex = 3;
            nudThreshold.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // nudIterations
            // 
            nudIterations.Location = new Point(12, 70);
            nudIterations.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
            nudIterations.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new Size(120, 23);
            nudIterations.TabIndex = 2;
            nudIterations.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // nudIm
            // 
            nudIm.DecimalPlaces = 3;
            nudIm.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudIm.Location = new Point(12, 41);
            nudIm.Maximum = new decimal(new int[] { 156, 0, 0, 196608 });
            nudIm.Minimum = new decimal(new int[] { 3, 0, 0, int.MinValue });
            nudIm.Name = "nudIm";
            nudIm.Size = new Size(120, 23);
            nudIm.TabIndex = 1;
            nudIm.Value = new decimal(new int[] { 156, 0, 0, 196608 });
            // 
            // nudRe
            // 
            nudRe.DecimalPlaces = 3;
            nudRe.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudRe.Location = new Point(12, 12);
            nudRe.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            nudRe.Minimum = new decimal(new int[] { 3, 0, 0, int.MinValue });
            nudRe.Name = "nudRe";
            nudRe.Size = new Size(120, 23);
            nudRe.TabIndex = 0;
            nudRe.Value = new decimal(new int[] { 8, 0, 0, -2147418112 });
            // 
            // canvas
            // 
            canvas.Dock = DockStyle.Fill;
            canvas.Location = new Point(231, 0);
            canvas.Name = "canvas";
            canvas.Size = new Size(853, 636);
            canvas.TabIndex = 1;
            canvas.TabStop = false;
            // 
            // FractalFormBase
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(canvas);
            Controls.Add(pnlControls);
            MinimumSize = new Size(1100, 675);
            Name = "FractalFormBase";
            Text = "FractalFormBase";
            Load += FormBase_Load;
            pnlControls.ResumeLayout(false);
            pnlControls.PerformLayout();
            mandelbrotPreviewPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mandelbrotPreviewCanvas).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudSaveWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudSaveHeight).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudBaseScale).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIm).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudRe).EndInit();
            ((System.ComponentModel.ISupportInitialize)canvas).EndInit();
            ResumeLayout(false);

        }
        #endregion

        // ВАЖНО: Объявляем все контролы здесь, чтобы дочерние классы их "видели".
        protected System.Windows.Forms.Panel pnlControls;
        protected System.Windows.Forms.PictureBox canvas;
        protected System.Windows.Forms.Label lblThreshold;
        protected System.Windows.Forms.Label lblIterations;
        protected System.Windows.Forms.NumericUpDown nudThreshold;
        protected System.Windows.Forms.NumericUpDown nudIterations;
        protected System.Windows.Forms.Button btnSaveHighRes;
        protected System.Windows.Forms.Label lblThreads;
        protected System.Windows.Forms.ComboBox cbThreads;
        protected System.Windows.Forms.ProgressBar pbRenderProgress;
        protected System.Windows.Forms.Button btnRender;
        protected System.Windows.Forms.CheckBox colorBox;
        protected System.Windows.Forms.Label lblZoom;
        protected System.Windows.Forms.NumericUpDown nudZoom;
        protected System.Windows.Forms.CheckBox oldRenderBW;
        protected System.Windows.Forms.Label lblProgress;
        protected System.Windows.Forms.ProgressBar pbHighResProgress;
        protected System.Windows.Forms.NumericUpDown nudSaveWidth;
        protected System.Windows.Forms.NumericUpDown nudSaveHeight;
        protected System.Windows.Forms.CheckBox checkBox6;
        protected System.Windows.Forms.CheckBox checkBox5;
        protected System.Windows.Forms.CheckBox checkBox4;
        protected System.Windows.Forms.CheckBox checkBox3;
        protected System.Windows.Forms.CheckBox checkBox2;
        protected System.Windows.Forms.CheckBox checkBox1;
        protected System.Windows.Forms.CheckBox mondelbrotClassicBox;

        // Для Жюлиа фракталов
        protected System.Windows.Forms.Label lblLoupeZoom;
        protected System.Windows.Forms.NumericUpDown nudBaseScale;
        protected System.Windows.Forms.Panel mandelbrotPreviewPanel;
        protected System.Windows.Forms.PictureBox mandelbrotPreviewCanvas;
        protected System.Windows.Forms.Label lblIm;
        protected System.Windows.Forms.Label lblRe;
        protected System.Windows.Forms.NumericUpDown nudIm;
        protected System.Windows.Forms.NumericUpDown nudRe;
        protected Button color_configurations;
    }
}