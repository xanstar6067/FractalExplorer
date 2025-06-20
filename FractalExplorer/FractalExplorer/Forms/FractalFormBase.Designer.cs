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
            this.pnlControls = new System.Windows.Forms.Panel();
            this.mandelbrotPreviewPanel = new System.Windows.Forms.Panel();
            this.mandelbrotPreviewCanvas = new System.Windows.Forms.PictureBox();
            this.checkBox6 = new System.Windows.Forms.CheckBox();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.nudSaveWidth = new System.Windows.Forms.NumericUpDown();
            this.nudSaveHeight = new System.Windows.Forms.NumericUpDown();
            this.pbHighResProgress = new System.Windows.Forms.ProgressBar();
            this.lblProgress = new System.Windows.Forms.Label();
            this.lblLoupeZoom = new System.Windows.Forms.Label();
            this.nudBaseScale = new System.Windows.Forms.NumericUpDown();
            this.mondelbrotClassicBox = new System.Windows.Forms.CheckBox();
            this.oldRenderBW = new System.Windows.Forms.CheckBox();
            this.lblZoom = new System.Windows.Forms.Label();
            this.nudZoom = new System.Windows.Forms.NumericUpDown();
            this.colorBox = new System.Windows.Forms.CheckBox();
            this.btnRender = new System.Windows.Forms.Button();
            this.pbRenderProgress = new System.Windows.Forms.ProgressBar();
            this.lblThreads = new System.Windows.Forms.Label();
            this.cbThreads = new System.Windows.Forms.ComboBox();
            this.btnSaveHighRes = new System.Windows.Forms.Button();
            this.lblThreshold = new System.Windows.Forms.Label();
            this.lblIterations = new System.Windows.Forms.Label();
            this.lblIm = new System.Windows.Forms.Label();
            this.lblRe = new System.Windows.Forms.Label();
            this.nudThreshold = new System.Windows.Forms.NumericUpDown();
            this.nudIterations = new System.Windows.Forms.NumericUpDown();
            this.nudIm = new System.Windows.Forms.NumericUpDown();
            this.nudRe = new System.Windows.Forms.NumericUpDown();
            this.canvas = new System.Windows.Forms.PictureBox();
            this.pnlControls.SuspendLayout();
            this.mandelbrotPreviewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mandelbrotPreviewCanvas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBaseScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRe)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlControls
            // 
            this.pnlControls.Controls.Add(this.mandelbrotPreviewPanel);
            this.pnlControls.Controls.Add(this.checkBox6);
            this.pnlControls.Controls.Add(this.checkBox5);
            this.pnlControls.Controls.Add(this.checkBox4);
            this.pnlControls.Controls.Add(this.checkBox3);
            this.pnlControls.Controls.Add(this.checkBox2);
            this.pnlControls.Controls.Add(this.checkBox1);
            this.pnlControls.Controls.Add(this.nudSaveWidth);
            this.pnlControls.Controls.Add(this.nudSaveHeight);
            this.pnlControls.Controls.Add(this.pbHighResProgress);
            this.pnlControls.Controls.Add(this.lblProgress);
            this.pnlControls.Controls.Add(this.lblLoupeZoom);
            this.pnlControls.Controls.Add(this.nudBaseScale);
            this.pnlControls.Controls.Add(this.mondelbrotClassicBox);
            this.pnlControls.Controls.Add(this.oldRenderBW);
            this.pnlControls.Controls.Add(this.lblZoom);
            this.pnlControls.Controls.Add(this.nudZoom);
            this.pnlControls.Controls.Add(this.colorBox);
            this.pnlControls.Controls.Add(this.btnRender);
            this.pnlControls.Controls.Add(this.pbRenderProgress);
            this.pnlControls.Controls.Add(this.lblThreads);
            this.pnlControls.Controls.Add(this.cbThreads);
            this.pnlControls.Controls.Add(this.btnSaveHighRes);
            this.pnlControls.Controls.Add(this.lblThreshold);
            this.pnlControls.Controls.Add(this.lblIterations);
            this.pnlControls.Controls.Add(this.lblIm);
            this.pnlControls.Controls.Add(this.lblRe);
            this.pnlControls.Controls.Add(this.nudThreshold);
            this.pnlControls.Controls.Add(this.nudIterations);
            this.pnlControls.Controls.Add(this.nudIm);
            this.pnlControls.Controls.Add(this.nudRe);
            this.pnlControls.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlControls.Location = new System.Drawing.Point(0, 0);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Size = new System.Drawing.Size(231, 636);
            this.pnlControls.TabIndex = 0;
            // 
            // mandelbrotPreviewPanel
            // 
            this.mandelbrotPreviewPanel.Controls.Add(this.mandelbrotPreviewCanvas);
            this.mandelbrotPreviewPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.mandelbrotPreviewPanel.Location = new System.Drawing.Point(0, 470);
            this.mandelbrotPreviewPanel.Name = "mandelbrotPreviewPanel";
            this.mandelbrotPreviewPanel.Size = new System.Drawing.Size(231, 166);
            this.mandelbrotPreviewPanel.TabIndex = 31;
            // 
            // mandelbrotPreviewCanvas
            // 
            this.mandelbrotPreviewCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mandelbrotPreviewCanvas.Location = new System.Drawing.Point(0, 0);
            this.mandelbrotPreviewCanvas.Name = "mandelbrotPreviewCanvas";
            this.mandelbrotPreviewCanvas.Size = new System.Drawing.Size(231, 166);
            this.mandelbrotPreviewCanvas.TabIndex = 30;
            this.mandelbrotPreviewCanvas.TabStop = false;
            // 
            // checkBox6
            // 
            this.checkBox6.AutoSize = true;
            this.checkBox6.Location = new System.Drawing.Point(195, 372);
            this.checkBox6.Name = "checkBox6";
            this.checkBox6.Size = new System.Drawing.Size(32, 19);
            this.checkBox6.TabIndex = 29;
            this.checkBox6.Text = "6";
            this.checkBox6.UseVisualStyleBackColor = true;
            // 
            // checkBox5
            // 
            this.checkBox5.AutoSize = true;
            this.checkBox5.Location = new System.Drawing.Point(164, 372);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(32, 19);
            this.checkBox5.TabIndex = 28;
            this.checkBox5.Text = "5";
            this.checkBox5.UseVisualStyleBackColor = true;
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Location = new System.Drawing.Point(126, 372);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(32, 19);
            this.checkBox4.TabIndex = 27;
            this.checkBox4.Text = "4";
            this.checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(88, 372);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(32, 19);
            this.checkBox3.TabIndex = 26;
            this.checkBox3.Text = "3";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(50, 372);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(32, 19);
            this.checkBox2.TabIndex = 25;
            this.checkBox2.Text = "2";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(12, 372);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(32, 19);
            this.checkBox1.TabIndex = 24;
            this.checkBox1.Text = "1";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // nudSaveWidth
            // 
            this.nudSaveWidth.Location = new System.Drawing.Point(12, 289);
            this.nudSaveWidth.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudSaveWidth.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudSaveWidth.Name = "nudSaveWidth";
            this.nudSaveWidth.Size = new System.Drawing.Size(86, 23);
            this.nudSaveWidth.TabIndex = 23;
            this.nudSaveWidth.Value = new decimal(new int[] {
            1920,
            0,
            0,
            0});
            // 
            // nudSaveHeight
            // 
            this.nudSaveHeight.Location = new System.Drawing.Point(124, 289);
            this.nudSaveHeight.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudSaveHeight.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudSaveHeight.Name = "nudSaveHeight";
            this.nudSaveHeight.Size = new System.Drawing.Size(83, 23);
            this.nudSaveHeight.TabIndex = 22;
            this.nudSaveHeight.Value = new decimal(new int[] {
            1080,
            0,
            0,
            0});
            // 
            // pbHighResProgress
            // 
            this.pbHighResProgress.Location = new System.Drawing.Point(3, 318);
            this.pbHighResProgress.Name = "pbHighResProgress";
            this.pbHighResProgress.Size = new System.Drawing.Size(218, 23);
            this.pbHighResProgress.TabIndex = 21;
            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.lblProgress.Location = new System.Drawing.Point(88, 423);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(67, 15);
            this.lblProgress.TabIndex = 20;
            this.lblProgress.Text = "Обработка";
            // 
            // lblLoupeZoom
            // 
            this.lblLoupeZoom.AutoSize = true;
            this.lblLoupeZoom.Location = new System.Drawing.Point(12, 169);
            this.lblLoupeZoom.Name = "lblLoupeZoom";
            this.lblLoupeZoom.Size = new System.Drawing.Size(105, 15);
            this.lblLoupeZoom.TabIndex = 19;
            this.lblLoupeZoom.Text = "Увеличение лупы";
            this.lblLoupeZoom.Visible = false;
            // 
            // nudBaseScale
            // 
            this.nudBaseScale.Location = new System.Drawing.Point(12, 187);
            this.nudBaseScale.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudBaseScale.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.nudBaseScale.Name = "nudBaseScale";
            this.nudBaseScale.Size = new System.Drawing.Size(195, 23);
            this.nudBaseScale.TabIndex = 18;
            this.nudBaseScale.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.nudBaseScale.Visible = false;
            // 
            // mondelbrotClassicBox
            // 
            this.mondelbrotClassicBox.AutoSize = true;
            this.mondelbrotClassicBox.Location = new System.Drawing.Point(145, 347);
            this.mondelbrotClassicBox.Name = "mondelbrotClassicBox";
            this.mondelbrotClassicBox.Size = new System.Drawing.Size(77, 19);
            this.mondelbrotClassicBox.TabIndex = 32;
            this.mondelbrotClassicBox.Text = "Классика";
            this.mondelbrotClassicBox.UseVisualStyleBackColor = true;
            // 
            // oldRenderBW
            // 
            this.oldRenderBW.AutoSize = true;
            this.oldRenderBW.Location = new System.Drawing.Point(80, 347);
            this.oldRenderBW.Name = "oldRenderBW";
            this.oldRenderBW.Size = new System.Drawing.Size(41, 19);
            this.oldRenderBW.TabIndex = 17;
            this.oldRenderBW.Text = "ЧБ";
            this.oldRenderBW.UseVisualStyleBackColor = true;
            // 
            // lblZoom
            // 
            this.lblZoom.AutoSize = true;
            this.lblZoom.Location = new System.Drawing.Point(12, 125);
            this.lblZoom.Name = "lblZoom";
            this.lblZoom.Size = new System.Drawing.Size(86, 15);
            this.lblZoom.TabIndex = 16;
            this.lblZoom.Text = "Приближение";
            // 
            // nudZoom
            // 
            this.nudZoom.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudZoom.Location = new System.Drawing.Point(11, 143);
            this.nudZoom.Maximum = new decimal(new int[] {
            -1,
            -1,
            -1,
            0});
            this.nudZoom.Name = "nudZoom";
            this.nudZoom.Size = new System.Drawing.Size(196, 23);
            this.nudZoom.TabIndex = 2;
            // 
            // colorBox
            // 
            this.colorBox.AutoSize = true;
            this.colorBox.Location = new System.Drawing.Point(12, 347);
            this.colorBox.Name = "colorBox";
            this.colorBox.Size = new System.Drawing.Size(52, 19);
            this.colorBox.TabIndex = 15;
            this.colorBox.Text = "Цвет";
            this.colorBox.UseVisualStyleBackColor = true;
            // 
            // btnRender
            // 
            this.btnRender.Location = new System.Drawing.Point(35, 397);
            this.btnRender.Name = "btnRender";
            this.btnRender.Size = new System.Drawing.Size(164, 23);
            this.btnRender.TabIndex = 2;
            this.btnRender.Text = "Запустить рендер";
            this.btnRender.UseVisualStyleBackColor = true;
            // 
            // pbRenderProgress
            // 
            this.pbRenderProgress.Location = new System.Drawing.Point(6, 441);
            this.pbRenderProgress.Name = "pbRenderProgress";
            this.pbRenderProgress.Size = new System.Drawing.Size(218, 23);
            this.pbRenderProgress.TabIndex = 14;
            // 
            // lblThreads
            // 
            this.lblThreads.AutoSize = true;
            this.lblThreads.Location = new System.Drawing.Point(12, 213);
            this.lblThreads.Name = "lblThreads";
            this.lblThreads.Size = new System.Drawing.Size(69, 15);
            this.lblThreads.TabIndex = 13;
            this.lblThreads.Text = "Потоки ЦП";
            // 
            // cbThreads
            // 
            this.cbThreads.FormattingEnabled = true;
            this.cbThreads.Location = new System.Drawing.Point(12, 231);
            this.cbThreads.Name = "cbThreads";
            this.cbThreads.Size = new System.Drawing.Size(195, 23);
            this.cbThreads.TabIndex = 12;
            // 
            // btnSaveHighRes
            // 
            this.btnSaveHighRes.Location = new System.Drawing.Point(28, 260);
            this.btnSaveHighRes.Name = "btnSaveHighRes";
            this.btnSaveHighRes.Size = new System.Drawing.Size(164, 23);
            this.btnSaveHighRes.TabIndex = 11;
            this.btnSaveHighRes.Text = "Сохранить изображение";
            this.btnSaveHighRes.UseVisualStyleBackColor = true;
            // 
            // lblThreshold
            // 
            this.lblThreshold.AutoSize = true;
            this.lblThreshold.Location = new System.Drawing.Point(138, 101);
            this.lblThreshold.Name = "lblThreshold";
            this.lblThreshold.Size = new System.Drawing.Size(85, 15);
            this.lblThreshold.TabIndex = 7;
            this.lblThreshold.Text = "Порог выхода";
            // 
            // lblIterations
            // 
            this.lblIterations.AutoSize = true;
            this.lblIterations.Location = new System.Drawing.Point(138, 72);
            this.lblIterations.Name = "lblIterations";
            this.lblIterations.Size = new System.Drawing.Size(61, 15);
            this.lblIterations.TabIndex = 6;
            this.lblIterations.Text = "Итерации";
            // 
            // lblIm
            // 
            this.lblIm.AutoSize = true;
            this.lblIm.Location = new System.Drawing.Point(138, 43);
            this.lblIm.Name = "lblIm";
            this.lblIm.Size = new System.Drawing.Size(54, 15);
            this.lblIm.TabIndex = 5;
            this.lblIm.Text = "Мнимое";
            // 
            // lblRe
            // 
            this.lblRe.AutoSize = true;
            this.lblRe.Location = new System.Drawing.Point(131, 14);
            this.lblRe.Name = "lblRe";
            this.lblRe.Size = new System.Drawing.Size(96, 15);
            this.lblRe.TabIndex = 4;
            this.lblRe.Text = "Действительное";
            // 
            // nudThreshold
            // 
            this.nudThreshold.DecimalPlaces = 3;
            this.nudThreshold.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.nudThreshold.Location = new System.Drawing.Point(12, 99);
            this.nudThreshold.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.nudThreshold.Name = "nudThreshold";
            this.nudThreshold.Size = new System.Drawing.Size(120, 23);
            this.nudThreshold.TabIndex = 3;
            this.nudThreshold.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // nudIterations
            // 
            this.nudIterations.Location = new System.Drawing.Point(12, 70);
            this.nudIterations.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.nudIterations.Minimum = new decimal(new int[] {
            50,
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
            // nudIm
            // 
            this.nudIm.DecimalPlaces = 3;
            this.nudIm.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.nudIm.Location = new System.Drawing.Point(12, 41);
            this.nudIm.Maximum = new decimal(new int[] {
            156,
            0,
            0,
            196608});
            this.nudIm.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            -2147483648});
            this.nudIm.Name = "nudIm";
            this.nudIm.Size = new System.Drawing.Size(120, 23);
            this.nudIm.TabIndex = 1;
            this.nudIm.Value = new decimal(new int[] {
            156,
            0,
            0,
            196608});
            // 
            // nudRe
            // 
            this.nudRe.DecimalPlaces = 3;
            this.nudRe.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.nudRe.Location = new System.Drawing.Point(12, 12);
            this.nudRe.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.nudRe.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            -2147483648});
            this.nudRe.Name = "nudRe";
            this.nudRe.Size = new System.Drawing.Size(120, 23);
            this.nudRe.TabIndex = 0;
            this.nudRe.Value = new decimal(new int[] {
            8,
            0,
            0,
            -2147418112});
            // 
            // canvas
            // 
            this.canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvas.Location = new System.Drawing.Point(231, 0);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(853, 636);
            this.canvas.TabIndex = 1;
            this.canvas.TabStop = false;
            // 
            // FractalFormBase
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 636);
            this.Controls.Add(this.canvas);
            this.Controls.Add(this.pnlControls);
            this.MinimumSize = new System.Drawing.Size(1100, 675);
            this.Name = "FractalFormBase";
            this.Text = "FractalFormBase";
            this.Load += new System.EventHandler(this.FormBase_Load);
            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            this.mandelbrotPreviewPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mandelbrotPreviewCanvas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBaseScale)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRe)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).EndInit();
            this.ResumeLayout(false);

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
    }
}