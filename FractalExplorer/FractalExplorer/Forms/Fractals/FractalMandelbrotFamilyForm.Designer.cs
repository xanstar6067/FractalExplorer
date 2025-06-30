namespace FractalDraving
{
    // Этот файл создается для того, чтобы унаследовать от него все остальные формы.
    // Содержимое для него будет сгенерировано Visual Studio, когда вы создадите форму `FractalFormBase`.
    // Для простоты, мы будем редактировать `Designer.cs` каждой конкретной формы, 
    // приводя имена к общему виду, а `FractalFormBase` будет просто базовым классом Form.
    // Ниже я приведу исправленные `.Designer.cs` файлы для каждой из ваших форм.
    // ВАЖНО: Сам файл `FractalFormBase.Designer.cs` должен быть таким:
    partial class FractalMandelbrotFamilyForm
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
            btnStateManager = new Button();
            color_configurations = new Button();
            mandelbrotPreviewPanel = new Panel();
            mandelbrotPreviewCanvas = new PictureBox();
            nudSaveWidth = new NumericUpDown();
            nudSaveHeight = new NumericUpDown();
            pbHighResProgress = new ProgressBar();
            lblProgress = new Label();
            lblZoom = new Label();
            nudZoom = new NumericUpDown();
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
            nudBaseScale = new NumericUpDown();
            canvas = new PictureBox();
            cbSSAA = new ComboBox();
            pnlControls.SuspendLayout();
            mandelbrotPreviewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mandelbrotPreviewCanvas).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudSaveWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudSaveHeight).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIm).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudRe).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudBaseScale).BeginInit();
            ((System.ComponentModel.ISupportInitialize)canvas).BeginInit();
            SuspendLayout();
            // 
            // pnlControls
            // 
            pnlControls.Controls.Add(cbSSAA);
            pnlControls.Controls.Add(btnStateManager);
            pnlControls.Controls.Add(color_configurations);
            pnlControls.Controls.Add(mandelbrotPreviewPanel);
            pnlControls.Controls.Add(nudSaveWidth);
            pnlControls.Controls.Add(nudSaveHeight);
            pnlControls.Controls.Add(pbHighResProgress);
            pnlControls.Controls.Add(lblProgress);
            pnlControls.Controls.Add(lblZoom);
            pnlControls.Controls.Add(nudZoom);
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
            pnlControls.Controls.Add(nudBaseScale);
            pnlControls.Dock = DockStyle.Left;
            pnlControls.Location = new Point(0, 0);
            pnlControls.Name = "pnlControls";
            pnlControls.Size = new Size(231, 636);
            pnlControls.TabIndex = 0;
            // 
            // btnStateManager
            // 
            btnStateManager.Location = new Point(3, 379);
            btnStateManager.Name = "btnStateManager";
            btnStateManager.Size = new Size(218, 32);
            btnStateManager.TabIndex = 34;
            btnStateManager.Text = "Менеджер сохранений";
            btnStateManager.UseVisualStyleBackColor = true;
            btnStateManager.Click += btnStateManager_Click;
            // 
            // color_configurations
            // 
            color_configurations.Location = new Point(3, 303);
            color_configurations.Name = "color_configurations";
            color_configurations.Size = new Size(218, 32);
            color_configurations.TabIndex = 33;
            color_configurations.Text = "Настроить палитру";
            color_configurations.UseVisualStyleBackColor = true;
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
            // nudSaveWidth
            // 
            nudSaveWidth.Location = new Point(11, 245);
            nudSaveWidth.Maximum = new decimal(new int[] { 32768, 0, 0, 0 });
            nudSaveWidth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudSaveWidth.Name = "nudSaveWidth";
            nudSaveWidth.Size = new Size(86, 23);
            nudSaveWidth.TabIndex = 23;
            nudSaveWidth.Value = new decimal(new int[] { 1920, 0, 0, 0 });
            // 
            // nudSaveHeight
            // 
            nudSaveHeight.Location = new Point(124, 245);
            nudSaveHeight.Maximum = new decimal(new int[] { 32768, 0, 0, 0 });
            nudSaveHeight.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudSaveHeight.Name = "nudSaveHeight";
            nudSaveHeight.Size = new Size(83, 23);
            nudSaveHeight.TabIndex = 22;
            nudSaveHeight.Value = new decimal(new int[] { 1080, 0, 0, 0 });
            // 
            // pbHighResProgress
            // 
            pbHighResProgress.Location = new Point(3, 274);
            pbHighResProgress.Name = "pbHighResProgress";
            pbHighResProgress.Size = new Size(218, 23);
            pbHighResProgress.TabIndex = 21;
            pbHighResProgress.Visible = false;
            // 
            // lblProgress
            // 
            lblProgress.AutoSize = true;
            lblProgress.Location = new Point(80, 414);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(67, 15);
            lblProgress.TabIndex = 20;
            lblProgress.Text = "Обработка";
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
            nudZoom.DecimalPlaces = 4;
            nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudZoom.Location = new Point(11, 143);
            nudZoom.Maximum = new decimal(new int[] { -1, -1, -1, 0 });
            nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new Size(196, 23);
            nudZoom.TabIndex = 2;
            nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnRender
            // 
            btnRender.Location = new Point(3, 341);
            btnRender.Name = "btnRender";
            btnRender.Size = new Size(218, 32);
            btnRender.TabIndex = 2;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            // 
            // pbRenderProgress
            // 
            pbRenderProgress.Location = new Point(3, 432);
            pbRenderProgress.Name = "pbRenderProgress";
            pbRenderProgress.Size = new Size(218, 23);
            pbRenderProgress.TabIndex = 14;
            // 
            // lblThreads
            // 
            lblThreads.AutoSize = true;
            lblThreads.Location = new Point(12, 169);
            lblThreads.Name = "lblThreads";
            lblThreads.Size = new Size(69, 15);
            lblThreads.TabIndex = 13;
            lblThreads.Text = "Потоки ЦП";
            // 
            // cbThreads
            // 
            cbThreads.FormattingEnabled = true;
            cbThreads.Location = new Point(12, 187);
            cbThreads.Name = "cbThreads";
            cbThreads.Size = new Size(195, 23);
            cbThreads.TabIndex = 12;
            // 
            // btnSaveHighRes
            // 
            btnSaveHighRes.Location = new Point(11, 216);
            btnSaveHighRes.Name = "btnSaveHighRes";
            btnSaveHighRes.Size = new Size(106, 23);
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
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            nudThreshold.Location = new Point(12, 99);
            nudThreshold.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudThreshold.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            nudThreshold.Name = "nudThreshold";
            nudThreshold.Size = new Size(120, 23);
            nudThreshold.TabIndex = 3;
            nudThreshold.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // nudIterations
            // 
            nudIterations.Location = new Point(12, 70);
            nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudIterations.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new Size(120, 23);
            nudIterations.TabIndex = 2;
            nudIterations.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // nudIm
            // 
            nudIm.DecimalPlaces = 15;
            nudIm.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudIm.Location = new Point(12, 41);
            nudIm.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudIm.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudIm.Name = "nudIm";
            nudIm.Size = new Size(120, 23);
            nudIm.TabIndex = 1;
            nudIm.Value = new decimal(new int[] { 156, 0, 0, 196608 });
            // 
            // nudRe
            // 
            nudRe.DecimalPlaces = 15;
            nudRe.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudRe.Location = new Point(12, 12);
            nudRe.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudRe.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudRe.Name = "nudRe";
            nudRe.Size = new Size(120, 23);
            nudRe.TabIndex = 0;
            nudRe.Value = new decimal(new int[] { 8, 0, 0, -2147418112 });
            // 
            // nudBaseScale
            // 
            nudBaseScale.Location = new Point(195, 46);
            nudBaseScale.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            nudBaseScale.Name = "nudBaseScale";
            nudBaseScale.Size = new Size(26, 23);
            nudBaseScale.TabIndex = 18;
            nudBaseScale.Value = new decimal(new int[] { 4, 0, 0, 0 });
            nudBaseScale.Visible = false;
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
            // cbSSAA
            // 
            cbSSAA.FormattingEnabled = true;
            cbSSAA.Location = new Point(123, 216);
            cbSSAA.Name = "cbSSAA";
            cbSSAA.Size = new Size(95, 23);
            cbSSAA.TabIndex = 35;
            // 
            // FractalMandelbrotFamilyForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(canvas);
            Controls.Add(pnlControls);
            MinimumSize = new Size(1100, 675);
            Name = "FractalMandelbrotFamilyForm";
            Text = "FractalFormBase";
            Load += FormBase_Load;
            pnlControls.ResumeLayout(false);
            pnlControls.PerformLayout();
            mandelbrotPreviewPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mandelbrotPreviewCanvas).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudSaveWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudSaveHeight).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIm).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudRe).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudBaseScale).EndInit();
            ((System.ComponentModel.ISupportInitialize)canvas).EndInit();
            ResumeLayout(false);
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
        private Button btnStateManager;
        private ComboBox cbSSAA;
    }
}