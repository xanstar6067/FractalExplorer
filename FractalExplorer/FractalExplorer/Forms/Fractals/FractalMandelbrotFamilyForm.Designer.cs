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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FractalMandelbrotFamilyForm));

            // --- ГЛОБАЛЬНЫЙ РЕДИЗАЙН: ИСПОЛЬЗУЕМ FLOWLAYOUTPANEL ---
            pnlControls = new System.Windows.Forms.FlowLayoutPanel();

            cbSmooth = new System.Windows.Forms.CheckBox();
            lbSSAA = new System.Windows.Forms.Label();
            cbSSAA = new System.Windows.Forms.ComboBox();
            btnStateManager = new System.Windows.Forms.Button();
            color_configurations = new System.Windows.Forms.Button();
            mandelbrotPreviewPanel = new System.Windows.Forms.Panel();
            mandelbrotPreviewCanvas = new System.Windows.Forms.PictureBox();
            lblProgress = new System.Windows.Forms.Label();
            lblZoom = new System.Windows.Forms.Label();
            nudZoom = new System.Windows.Forms.NumericUpDown();
            btnRender = new System.Windows.Forms.Button();
            pbRenderProgress = new System.Windows.Forms.ProgressBar();
            lblThreads = new System.Windows.Forms.Label();
            cbThreads = new System.Windows.Forms.ComboBox();
            btnSaveHighRes = new System.Windows.Forms.Button();
            lblThreshold = new System.Windows.Forms.Label();
            lblIterations = new System.Windows.Forms.Label();
            lblIm = new System.Windows.Forms.Label();
            lblRe = new System.Windows.Forms.Label();
            nudThreshold = new System.Windows.Forms.NumericUpDown();
            nudIterations = new System.Windows.Forms.NumericUpDown();
            nudIm = new System.Windows.Forms.NumericUpDown();
            nudRe = new System.Windows.Forms.NumericUpDown();
            nudBaseScale = new System.Windows.Forms.NumericUpDown();
            canvas = new System.Windows.Forms.PictureBox();

            pnlControls.SuspendLayout();
            mandelbrotPreviewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(mandelbrotPreviewCanvas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudIm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudRe)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudBaseScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(canvas)).BeginInit();
            SuspendLayout();

            // 
            // pnlControls (теперь FlowLayoutPanel)
            // 
            pnlControls.AutoScroll = true;
            pnlControls.Dock = System.Windows.Forms.DockStyle.Left;
            pnlControls.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            pnlControls.Location = new System.Drawing.Point(0, 0);
            pnlControls.Name = "pnlControls";
            pnlControls.Size = new System.Drawing.Size(231, 636);
            pnlControls.TabIndex = 0;
            pnlControls.WrapContents = false;

            // --- Добавляем все контролы на FlowLayoutPanel в нужном порядке ---
            // Порядок добавления определяет их визуальный порядок сверху вниз
            pnlControls.Controls.Add(lblRe);
            pnlControls.Controls.Add(nudRe);
            pnlControls.Controls.Add(lblIm);
            pnlControls.Controls.Add(nudIm);
            pnlControls.Controls.Add(lblIterations);
            pnlControls.Controls.Add(nudIterations);
            pnlControls.Controls.Add(lblThreshold);
            pnlControls.Controls.Add(nudThreshold);
            pnlControls.Controls.Add(lblZoom);
            pnlControls.Controls.Add(nudZoom);
            pnlControls.Controls.Add(lblThreads);
            pnlControls.Controls.Add(cbThreads);
            pnlControls.Controls.Add(lbSSAA);
            pnlControls.Controls.Add(cbSSAA);
            pnlControls.Controls.Add(cbSmooth);
            pnlControls.Controls.Add(btnSaveHighRes);
            pnlControls.Controls.Add(color_configurations);
            pnlControls.Controls.Add(btnRender);
            pnlControls.Controls.Add(btnStateManager);
            pnlControls.Controls.Add(lblProgress);
            pnlControls.Controls.Add(pbRenderProgress);
            pnlControls.Controls.Add(mandelbrotPreviewPanel);
            pnlControls.Controls.Add(nudBaseScale);

            // --- Настройка каждого контрола с отступами (Margin) вместо координат (Location) ---

            // 
            // lblRe
            // 
            lblRe.AutoSize = true;
            lblRe.Margin = new System.Windows.Forms.Padding(125, 10, 3, 0); // Отступ слева для "выравнивания по правому краю"
            lblRe.Name = "lblRe";
            lblRe.Size = new System.Drawing.Size(96, 15);
            lblRe.Text = "Действительное";
            // 
            // nudRe
            // 
            nudRe.DecimalPlaces = 15;
            nudRe.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudRe.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            nudRe.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudRe.Minimum = new decimal(new int[] { 2, 0, 0, -2147483648 });
            nudRe.Name = "nudRe";
            nudRe.Size = new System.Drawing.Size(195, 23);
            nudRe.Value = new decimal(new int[] { 8, 0, 0, -2147418112 });
            // 
            // lblIm
            // 
            lblIm.AutoSize = true;
            lblIm.Margin = new System.Windows.Forms.Padding(125, 10, 3, 0);
            lblIm.Name = "lblIm";
            lblIm.Size = new System.Drawing.Size(54, 15);
            lblIm.Text = "Мнимое";
            // 
            // nudIm
            // 
            nudIm.DecimalPlaces = 15;
            nudIm.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudIm.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            nudIm.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudIm.Minimum = new decimal(new int[] { 2, 0, 0, -2147483648 });
            nudIm.Name = "nudIm";
            nudIm.Size = new System.Drawing.Size(195, 23);
            nudIm.Value = new decimal(new int[] { 156, 0, 0, 196608 });
            // 
            // lblIterations
            // 
            lblIterations.AutoSize = true;
            lblIterations.Margin = new System.Windows.Forms.Padding(125, 10, 3, 0);
            lblIterations.Name = "lblIterations";
            lblIterations.Size = new System.Drawing.Size(61, 15);
            lblIterations.Text = "Итерации";
            // 
            // nudIterations
            // 
            nudIterations.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudIterations.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new System.Drawing.Size(195, 23);
            nudIterations.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // lblThreshold
            // 
            lblThreshold.AutoSize = true;
            lblThreshold.Margin = new System.Windows.Forms.Padding(125, 10, 3, 0);
            lblThreshold.Name = "lblThreshold";
            lblThreshold.Size = new System.Drawing.Size(85, 15);
            lblThreshold.Text = "Порог выхода";
            // 
            // nudThreshold
            // 
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            nudThreshold.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            nudThreshold.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudThreshold.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            nudThreshold.Name = "nudThreshold";
            nudThreshold.Size = new System.Drawing.Size(195, 23);
            nudThreshold.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // lblZoom
            // 
            lblZoom.AutoSize = true;
            lblZoom.Margin = new System.Windows.Forms.Padding(12, 10, 3, 0);
            lblZoom.Name = "lblZoom";
            lblZoom.Size = new System.Drawing.Size(86, 15);
            lblZoom.Text = "Приближение";
            // 
            // nudZoom
            // 
            nudZoom.DecimalPlaces = 4;
            nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudZoom.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            nudZoom.Maximum = new decimal(new int[] { -1, -1, -1, 0 });
            nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new System.Drawing.Size(195, 23);
            nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblThreads
            // 
            lblThreads.AutoSize = true;
            lblThreads.Margin = new System.Windows.Forms.Padding(12, 10, 3, 0);
            lblThreads.Name = "lblThreads";
            lblThreads.Size = new System.Drawing.Size(69, 15);
            lblThreads.Text = "Потоки ЦП";
            // 
            // cbThreads
            // 
            cbThreads.FormattingEnabled = true;
            cbThreads.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            cbThreads.Name = "cbThreads";
            cbThreads.Size = new System.Drawing.Size(195, 23);
            // 
            // lbSSAA
            // 
            lbSSAA.AutoSize = true;
            lbSSAA.Margin = new System.Windows.Forms.Padding(12, 10, 3, 0);
            lbSSAA.Name = "lbSSAA";
            lbSSAA.Size = new System.Drawing.Size(112, 15);
            lbSSAA.Text = "Сглаживание SSAA";
            // 
            // cbSSAA
            // 
            cbSSAA.FormattingEnabled = true;
            cbSSAA.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            cbSSAA.Name = "cbSSAA";
            cbSSAA.Size = new System.Drawing.Size(195, 23);
            // 
            // cbSmooth
            // 
            cbSmooth.AutoSize = true;
            cbSmooth.Checked = true;
            cbSmooth.CheckState = System.Windows.Forms.CheckState.Checked;
            cbSmooth.Margin = new System.Windows.Forms.Padding(12, 10, 3, 10);
            cbSmooth.Name = "cbSmooth";
            cbSmooth.Size = new System.Drawing.Size(153, 19);
            cbSmooth.Text = "Плавное окрашивание";
            cbSmooth.UseVisualStyleBackColor = true;
            // 
            // btnSaveHighRes
            // 
            btnSaveHighRes.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            btnSaveHighRes.Name = "btnSaveHighRes";
            btnSaveHighRes.Size = new System.Drawing.Size(195, 32);
            btnSaveHighRes.Text = "Сохранить изображение";
            btnSaveHighRes.UseVisualStyleBackColor = true;
            btnSaveHighRes.Click += btnOpenSaveManager_Click;
            // 
            // color_configurations
            // 
            color_configurations.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            color_configurations.Name = "color_configurations";
            color_configurations.Size = new System.Drawing.Size(195, 32);
            color_configurations.Text = "Настроить палитру";
            color_configurations.UseVisualStyleBackColor = true;
            // 
            // btnRender
            // 
            btnRender.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            btnRender.Name = "btnRender";
            btnRender.Size = new System.Drawing.Size(195, 32);
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            // 
            // btnStateManager
            // 
            btnStateManager.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            btnStateManager.Name = "btnStateManager";
            btnStateManager.Size = new System.Drawing.Size(195, 32);
            btnStateManager.Text = "Менеджер сохранений";
            btnStateManager.UseVisualStyleBackColor = true;
            btnStateManager.Click += btnStateManager_Click;
            // 
            // lblProgress
            // 
            lblProgress.AutoSize = true;
            lblProgress.Margin = new System.Windows.Forms.Padding(75, 10, 3, 0); // Центрируем надпись
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new System.Drawing.Size(67, 15);
            lblProgress.Text = "Обработка";
            // 
            // pbRenderProgress
            // 
            pbRenderProgress.Margin = new System.Windows.Forms.Padding(12, 3, 3, 3);
            pbRenderProgress.Name = "pbRenderProgress";
            pbRenderProgress.Size = new System.Drawing.Size(195, 23);
            // 
            // mandelbrotPreviewPanel
            // 
            mandelbrotPreviewPanel.Controls.Add(mandelbrotPreviewCanvas);
            mandelbrotPreviewPanel.Margin = new System.Windows.Forms.Padding(12, 10, 3, 3);
            mandelbrotPreviewPanel.Name = "mandelbrotPreviewPanel";
            mandelbrotPreviewPanel.Size = new System.Drawing.Size(195, 166);
            // 
            // mandelbrotPreviewCanvas
            // 
            mandelbrotPreviewCanvas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            mandelbrotPreviewCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            mandelbrotPreviewCanvas.Location = new System.Drawing.Point(0, 0);
            mandelbrotPreviewCanvas.Name = "mandelbrotPreviewCanvas";
            mandelbrotPreviewCanvas.Size = new System.Drawing.Size(195, 166);
            mandelbrotPreviewCanvas.TabStop = false;
            // 
            // nudBaseScale (Скрытый контрол)
            // 
            nudBaseScale.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            nudBaseScale.Name = "nudBaseScale";
            nudBaseScale.Size = new System.Drawing.Size(26, 23);
            nudBaseScale.Value = new decimal(new int[] { 4, 0, 0, 0 });
            nudBaseScale.Visible = false;

            // 
            // canvas
            // 
            canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            canvas.Location = new System.Drawing.Point(231, 0);
            canvas.Name = "canvas";
            canvas.Size = new System.Drawing.Size(853, 636);
            canvas.TabIndex = 1;
            canvas.TabStop = false;

            // 
            // FractalMandelbrotFamilyForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1084, 636);
            Controls.Add(canvas);
            Controls.Add(pnlControls);
            Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            MinimumSize = new System.Drawing.Size(1100, 675);
            Name = "FractalMandelbrotFamilyForm";
            Text = "FractalFormBase";
            Load += FormBase_Load;

            pnlControls.ResumeLayout(false);
            pnlControls.PerformLayout();
            mandelbrotPreviewPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(mandelbrotPreviewCanvas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudIm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudRe)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudBaseScale)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(canvas)).EndInit();
            ResumeLayout(false);
        }
        #endregion

        protected System.Windows.Forms.FlowLayoutPanel pnlControls;
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
        protected System.Windows.Forms.Panel mandelbrotPreviewPanel;
        protected System.Windows.Forms.PictureBox mandelbrotPreviewCanvas;
        protected System.Windows.Forms.Label lblIm;
        protected System.Windows.Forms.Label lblRe;
        protected System.Windows.Forms.NumericUpDown nudIm;
        protected System.Windows.Forms.NumericUpDown nudRe;
        protected System.Windows.Forms.Button color_configurations;
        protected System.Windows.Forms.NumericUpDown nudBaseScale;
        private System.Windows.Forms.Button btnStateManager;
        private System.Windows.Forms.ComboBox cbSSAA;
        protected System.Windows.Forms.Label lbSSAA;
        private System.Windows.Forms.CheckBox cbSmooth;
    }
}