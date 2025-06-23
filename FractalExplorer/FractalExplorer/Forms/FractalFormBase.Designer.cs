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
            this.color_configurations = new System.Windows.Forms.Button();
            this.mandelbrotPreviewPanel = new System.Windows.Forms.Panel();
            this.mandelbrotPreviewCanvas = new System.Windows.Forms.PictureBox();
            this.nudSaveWidth = new System.Windows.Forms.NumericUpDown();
            this.nudSaveHeight = new System.Windows.Forms.NumericUpDown();
            this.pbHighResProgress = new System.Windows.Forms.ProgressBar();
            this.lblProgress = new System.Windows.Forms.Label();
            this.lblZoom = new System.Windows.Forms.Label();
            this.nudZoom = new System.Windows.Forms.NumericUpDown();
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

            // Скрытый NumericUpDown для обратной совместимости с IFractalForm
            this.nudBaseScale = new System.Windows.Forms.NumericUpDown();

            this.pnlControls.SuspendLayout();
            this.mandelbrotPreviewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mandelbrotPreviewCanvas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSaveHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRe)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBaseScale)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlControls
            // 
            this.pnlControls.Controls.Add(this.color_configurations);
            this.pnlControls.Controls.Add(this.mandelbrotPreviewPanel);
            this.pnlControls.Controls.Add(this.nudSaveWidth);
            this.pnlControls.Controls.Add(this.nudSaveHeight);
            this.pnlControls.Controls.Add(this.pbHighResProgress);
            this.pnlControls.Controls.Add(this.lblProgress);
            this.pnlControls.Controls.Add(this.lblZoom);
            this.pnlControls.Controls.Add(this.nudZoom);
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
            this.pnlControls.Controls.Add(this.nudBaseScale); // Добавлен, но будет скрыт
            this.pnlControls.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlControls.Location = new System.Drawing.Point(0, 0);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Size = new System.Drawing.Size(231, 636);
            this.pnlControls.TabIndex = 0;
            // 
            // color_configurations
            // 
            this.color_configurations.Location = new System.Drawing.Point(35, 368);
            this.color_configurations.Name = "color_configurations";
            this.color_configurations.Size = new System.Drawing.Size(164, 23);
            this.color_configurations.TabIndex = 33;
            this.color_configurations.Text = "Настроить палитру";
            this.color_configurations.UseVisualStyleBackColor = true;
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
            // nudSaveWidth
            // 
            this.nudSaveWidth.Location = new System.Drawing.Point(12, 289);
            this.nudSaveWidth.Maximum = new decimal(new int[] { 32768, 0, 0, 0 });
            this.nudSaveWidth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudSaveWidth.Name = "nudSaveWidth";
            this.nudSaveWidth.Size = new System.Drawing.Size(86, 23);
            this.nudSaveWidth.TabIndex = 23;
            this.nudSaveWidth.Value = new decimal(new int[] { 1920, 0, 0, 0 });
            // 
            // nudSaveHeight
            // 
            this.nudSaveHeight.Location = new System.Drawing.Point(124, 289);
            this.nudSaveHeight.Maximum = new decimal(new int[] { 32768, 0, 0, 0 });
            this.nudSaveHeight.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudSaveHeight.Name = "nudSaveHeight";
            this.nudSaveHeight.Size = new System.Drawing.Size(83, 23);
            this.nudSaveHeight.TabIndex = 22;
            this.nudSaveHeight.Value = new decimal(new int[] { 1080, 0, 0, 0 });
            // 
            // pbHighResProgress
            // 
            this.pbHighResProgress.Location = new System.Drawing.Point(3, 318);
            this.pbHighResProgress.Name = "pbHighResProgress";
            this.pbHighResProgress.Size = new System.Drawing.Size(218, 23);
            this.pbHighResProgress.TabIndex = 21;
            this.pbHighResProgress.Visible = false;
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
            this.nudZoom.DecimalPlaces = 4;
            this.nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            this.nudZoom.Location = new System.Drawing.Point(11, 143);
            this.nudZoom.Maximum = new decimal(new int[] { -1, -1, -1, 0 });
            this.nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudZoom.Name = "nudZoom";
            this.nudZoom.Size = new System.Drawing.Size(196, 23);
            this.nudZoom.TabIndex = 2;
            this.nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 });
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
            this.lblThreads.Location = new System.Drawing.Point(12, 180);
            this.lblThreads.Name = "lblThreads";
            this.lblThreads.Size = new System.Drawing.Size(69, 15);
            this.lblThreads.TabIndex = 13;
            this.lblThreads.Text = "Потоки ЦП";
            // 
            // cbThreads
            // 
            this.cbThreads.FormattingEnabled = true;
            this.cbThreads.Location = new System.Drawing.Point(12, 198);
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
            this.nudThreshold.DecimalPlaces = 1;
            this.nudThreshold.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudThreshold.Location = new System.Drawing.Point(12, 99);
            this.nudThreshold.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.nudThreshold.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            this.nudThreshold.Name = "nudThreshold";
            this.nudThreshold.Size = new System.Drawing.Size(120, 23);
            this.nudThreshold.TabIndex = 3;
            this.nudThreshold.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // nudIterations
            // 
            this.nudIterations.Location = new System.Drawing.Point(12, 70);
            this.nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.nudIterations.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            this.nudIterations.Name = "nudIterations";
            this.nudIterations.Size = new System.Drawing.Size(120, 23);
            this.nudIterations.TabIndex = 2;
            this.nudIterations.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // nudIm
            // 
            this.nudIm.DecimalPlaces = 15;
            this.nudIm.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            this.nudIm.Location = new System.Drawing.Point(12, 41);
            this.nudIm.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            this.nudIm.Minimum = new decimal(new int[] { 2, 0, 0, -2147483648 });
            this.nudIm.Name = "nudIm";
            this.nudIm.Size = new System.Drawing.Size(120, 23);
            this.nudIm.TabIndex = 1;
            this.nudIm.Value = new decimal(new int[] { 156, 0, 0, 196608 });
            // 
            // nudRe
            // 
            this.nudRe.DecimalPlaces = 15;
            this.nudRe.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            this.nudRe.Location = new System.Drawing.Point(12, 12);
            this.nudRe.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            this.nudRe.Minimum = new decimal(new int[] { 2, 0, 0, -2147483648 });
            this.nudRe.Name = "nudRe";
            this.nudRe.Size = new System.Drawing.Size(120, 23);
            this.nudRe.TabIndex = 0;
            this.nudRe.Value = new decimal(new int[] { 8, 0, 0, -2147418112 });
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
            // nudBaseScale
            //
            this.nudBaseScale.Location = new System.Drawing.Point(12, 227);
            this.nudBaseScale.Name = "nudBaseScale";
            this.nudBaseScale.Size = new System.Drawing.Size(195, 23);
            this.nudBaseScale.TabIndex = 18;
            this.nudBaseScale.Value = new decimal(new int[] { 4, 0, 0, 0 });
            this.nudBaseScale.Visible = false; // Важно: этот контрол скрыт
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
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRe)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBaseScale)).EndInit();
            this.ResumeLayout(false);
        }
        #endregion

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
        protected System.Windows.Forms.Label lblZoom;
        protected System.Windows.Forms.NumericUpDown nudZoom;
        protected System.Windows.Forms.Label lblProgress;
        protected System.Windows.Forms.ProgressBar pbHighResProgress;
        protected System.Windows.Forms.NumericUpDown nudSaveWidth;
        protected System.Windows.Forms.NumericUpDown nudSaveHeight;
        protected System.Windows.Forms.Panel mandelbrotPreviewPanel;
        protected System.Windows.Forms.PictureBox mandelbrotPreviewCanvas;
        protected System.Windows.Forms.Label lblIm;
        protected System.Windows.Forms.Label lblRe;
        protected System.Windows.Forms.NumericUpDown nudIm;
        protected System.Windows.Forms.NumericUpDown nudRe;
        protected System.Windows.Forms.Button color_configurations;
        // Этот контрол нужен для IFractalForm, но он невидимый.
        protected System.Windows.Forms.NumericUpDown nudBaseScale;
    }
}