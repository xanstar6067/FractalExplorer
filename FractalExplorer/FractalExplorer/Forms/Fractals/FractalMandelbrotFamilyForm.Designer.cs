namespace FractalDraving
{
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

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        protected void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FractalMandelbrotFamilyForm));

            // --- ГЛАВНЫЙ КОНТЕЙНЕР ТЕПЕРЬ TABLELAYOUTPANEL ДЛЯ КОМПАКТНОГО РАЗМЕЩЕНИЯ ---
            this.pnlControls = new System.Windows.Forms.TableLayoutPanel();

            this.cbSmooth = new System.Windows.Forms.CheckBox();
            this.lbSSAA = new System.Windows.Forms.Label();
            this.cbSSAA = new System.Windows.Forms.ComboBox();
            this.btnStateManager = new System.Windows.Forms.Button();
            this.color_configurations = new System.Windows.Forms.Button();
            this.mandelbrotPreviewPanel = new System.Windows.Forms.Panel();
            this.mandelbrotPreviewCanvas = new System.Windows.Forms.PictureBox();
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
            this.nudBaseScale = new System.Windows.Forms.NumericUpDown();
            this.canvas = new System.Windows.Forms.PictureBox();

            this.pnlControls.SuspendLayout();
            this.mandelbrotPreviewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mandelbrotPreviewCanvas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIm)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRe)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBaseScale)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).BeginInit();
            this.SuspendLayout();

            // 
            // pnlControls (TableLayoutPanel)
            // 
            this.pnlControls.AutoScroll = true;
            this.pnlControls.ColumnCount = 2;
            this.pnlControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 125F));
            this.pnlControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlControls.Controls.Add(this.nudRe, 0, 0);
            this.pnlControls.Controls.Add(this.lblRe, 1, 0);
            this.pnlControls.Controls.Add(this.nudIm, 0, 1);
            this.pnlControls.Controls.Add(this.lblIm, 1, 1);
            this.pnlControls.Controls.Add(this.nudIterations, 0, 2);
            this.pnlControls.Controls.Add(this.lblIterations, 1, 2);
            this.pnlControls.Controls.Add(this.nudThreshold, 0, 3);
            this.pnlControls.Controls.Add(this.lblThreshold, 1, 3);
            this.pnlControls.Controls.Add(this.nudZoom, 0, 4);
            this.pnlControls.Controls.Add(this.lblZoom, 1, 4);
            this.pnlControls.Controls.Add(this.cbThreads, 0, 5);
            this.pnlControls.Controls.Add(this.lblThreads, 1, 5);
            this.pnlControls.Controls.Add(this.cbSSAA, 0, 6);
            this.pnlControls.Controls.Add(this.lbSSAA, 1, 6);
            this.pnlControls.Controls.Add(this.cbSmooth, 0, 7);
            this.pnlControls.Controls.Add(this.btnSaveHighRes, 0, 8);
            this.pnlControls.Controls.Add(this.color_configurations, 0, 9);
            this.pnlControls.Controls.Add(this.btnRender, 0, 10);
            this.pnlControls.Controls.Add(this.btnStateManager, 0, 11);
            this.pnlControls.Controls.Add(this.lblProgress, 0, 12);
            this.pnlControls.Controls.Add(this.pbRenderProgress, 0, 13);
            this.pnlControls.Controls.Add(this.mandelbrotPreviewPanel, 0, 14);
            this.pnlControls.Controls.Add(this.nudBaseScale, 1, 15);
            this.pnlControls.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlControls.Location = new System.Drawing.Point(0, 0);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.RowCount = 16;
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.pnlControls.Size = new System.Drawing.Size(231, 636);
            this.pnlControls.TabIndex = 0;
            //
            // nudRe
            // 
            this.nudRe.DecimalPlaces = 15;
            this.nudRe.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudRe.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            this.nudRe.Margin = new System.Windows.Forms.Padding(6, 6, 3, 3);
            this.nudRe.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            this.nudRe.Minimum = new decimal(new int[] { 2, 0, 0, -2147483648 });
            this.nudRe.Name = "nudRe";
            this.nudRe.Size = new System.Drawing.Size(116, 23);
            this.nudRe.Value = new decimal(new int[] { 8, 0, 0, -2147418112 });
            // 
            // lblRe
            // 
            this.lblRe.AutoSize = true;
            this.lblRe.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRe.Location = new System.Drawing.Point(128, 0);
            this.lblRe.Name = "lblRe";
            this.lblRe.Size = new System.Drawing.Size(100, 32);
            this.lblRe.TabIndex = 4;
            this.lblRe.Text = "Действительное";
            this.lblRe.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudIm
            // 
            this.nudIm.DecimalPlaces = 15;
            this.nudIm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudIm.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            this.nudIm.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.nudIm.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            this.nudIm.Minimum = new decimal(new int[] { 2, 0, 0, -2147483648 });
            this.nudIm.Name = "nudIm";
            this.nudIm.Size = new System.Drawing.Size(116, 23);
            this.nudIm.Value = new decimal(new int[] { 156, 0, 0, 196608 });
            // 
            // lblIm
            // 
            this.lblIm.AutoSize = true;
            this.lblIm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIm.Location = new System.Drawing.Point(128, 32);
            this.lblIm.Name = "lblIm";
            this.lblIm.Size = new System.Drawing.Size(100, 29);
            this.lblIm.TabIndex = 5;
            this.lblIm.Text = "Мнимое";
            this.lblIm.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudIterations
            // 
            this.nudIterations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudIterations.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.nudIterations.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            this.nudIterations.Name = "nudIterations";
            this.nudIterations.Size = new System.Drawing.Size(116, 23);
            this.nudIterations.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // lblIterations
            // 
            this.lblIterations.AutoSize = true;
            this.lblIterations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIterations.Location = new System.Drawing.Point(128, 61);
            this.lblIterations.Name = "lblIterations";
            this.lblIterations.Size = new System.Drawing.Size(100, 29);
            this.lblIterations.TabIndex = 6;
            this.lblIterations.Text = "Итерации";
            this.lblIterations.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudThreshold
            // 
            this.nudThreshold.DecimalPlaces = 1;
            this.nudThreshold.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudThreshold.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudThreshold.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.nudThreshold.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.nudThreshold.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            this.nudThreshold.Name = "nudThreshold";
            this.nudThreshold.Size = new System.Drawing.Size(116, 23);
            this.nudThreshold.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // lblThreshold
            // 
            this.lblThreshold.AutoSize = true;
            this.lblThreshold.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblThreshold.Location = new System.Drawing.Point(128, 90);
            this.lblThreshold.Name = "lblThreshold";
            this.lblThreshold.Size = new System.Drawing.Size(100, 29);
            this.lblThreshold.TabIndex = 7;
            this.lblThreshold.Text = "Порог выхода";
            this.lblThreshold.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudZoom
            // 
            this.pnlControls.SetColumnSpan(this.nudZoom, 2);
            this.nudZoom.DecimalPlaces = 4;
            this.nudZoom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            this.nudZoom.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.nudZoom.Maximum = new decimal(new int[] { -1, -1, -1, 0 });
            this.nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudZoom.Name = "nudZoom";
            this.nudZoom.Size = new System.Drawing.Size(219, 23);
            // 
            // lblZoom
            // 
            this.lblZoom.AutoSize = true;
            this.pnlControls.SetColumnSpan(this.lblZoom, 2);
            this.lblZoom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblZoom.Location = new System.Drawing.Point(3, 119);
            this.lblZoom.Name = "lblZoom";
            this.lblZoom.Size = new System.Drawing.Size(225, 15);
            this.lblZoom.TabIndex = 16;
            this.lblZoom.Text = "Приближение";
            this.lblZoom.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cbThreads
            // 
            this.cbThreads.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbThreads.FormattingEnabled = true;
            this.cbThreads.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.cbThreads.Name = "cbThreads";
            this.cbThreads.Size = new System.Drawing.Size(116, 23);
            // 
            // lblThreads
            // 
            this.lblThreads.AutoSize = true;
            this.lblThreads.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblThreads.Location = new System.Drawing.Point(128, 149);
            this.lblThreads.Name = "lblThreads";
            this.lblThreads.Size = new System.Drawing.Size(100, 29);
            this.lblThreads.TabIndex = 13;
            this.lblThreads.Text = "Потоки ЦП";
            this.lblThreads.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbSSAA
            // 
            this.cbSSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbSSAA.FormattingEnabled = true;
            this.cbSSAA.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.cbSSAA.Name = "cbSSAA";
            this.cbSSAA.Size = new System.Drawing.Size(116, 23);
            // 
            // lbSSAA
            // 
            this.lbSSAA.AutoSize = true;
            this.lbSSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbSSAA.Location = new System.Drawing.Point(128, 178);
            this.lbSSAA.Name = "lbSSAA";
            this.lbSSAA.Size = new System.Drawing.Size(100, 29);
            this.lbSSAA.TabIndex = 36;
            this.lbSSAA.Text = "Сглаживание";
            this.lbSSAA.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbSmooth
            // 
            this.cbSmooth.AutoSize = true;
            this.cbSmooth.Checked = true;
            this.cbSmooth.CheckState = System.Windows.Forms.CheckState.Checked;
            this.pnlControls.SetColumnSpan(this.cbSmooth, 2);
            this.cbSmooth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbSmooth.Location = new System.Drawing.Point(6, 213);
            this.cbSmooth.Margin = new System.Windows.Forms.Padding(6, 6, 3, 3);
            this.cbSmooth.Name = "cbSmooth";
            this.cbSmooth.Size = new System.Drawing.Size(222, 31);
            this.cbSmooth.Text = "Плавное окрашивание";
            this.cbSmooth.UseVisualStyleBackColor = true;
            // 
            // btnSaveHighRes
            // 
            this.pnlControls.SetColumnSpan(this.btnSaveHighRes, 2);
            this.btnSaveHighRes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveHighRes.Margin = new System.Windows.Forms.Padding(6);
            this.btnSaveHighRes.Name = "btnSaveHighRes";
            this.btnSaveHighRes.Size = new System.Drawing.Size(219, 28);
            this.btnSaveHighRes.Text = "Сохранить изображение";
            this.btnSaveHighRes.UseVisualStyleBackColor = true;
            this.btnSaveHighRes.Click += new System.EventHandler(this.btnOpenSaveManager_Click);
            // 
            // color_configurations
            // 
            this.pnlControls.SetColumnSpan(this.color_configurations, 2);
            this.color_configurations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.color_configurations.Margin = new System.Windows.Forms.Padding(6);
            this.color_configurations.Name = "color_configurations";
            this.color_configurations.Size = new System.Drawing.Size(219, 28);
            this.color_configurations.Text = "Настроить палитру";
            this.color_configurations.UseVisualStyleBackColor = true;
            // 
            // btnRender
            // 
            this.pnlControls.SetColumnSpan(this.btnRender, 2);
            this.btnRender.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRender.Margin = new System.Windows.Forms.Padding(6);
            this.btnRender.Name = "btnRender";
            this.btnRender.Size = new System.Drawing.Size(219, 28);
            this.btnRender.Text = "Запустить рендер";
            this.btnRender.UseVisualStyleBackColor = true;
            // 
            // btnStateManager
            // 
            this.pnlControls.SetColumnSpan(this.btnStateManager, 2);
            this.btnStateManager.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStateManager.Margin = new System.Windows.Forms.Padding(6);
            this.btnStateManager.Name = "btnStateManager";
            this.btnStateManager.Size = new System.Drawing.Size(219, 28);
            this.btnStateManager.Text = "Менеджер сохранений";
            this.btnStateManager.UseVisualStyleBackColor = true;
            this.btnStateManager.Click += new System.EventHandler(this.btnStateManager_Click);
            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.pnlControls.SetColumnSpan(this.lblProgress, 2);
            this.lblProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProgress.Location = new System.Drawing.Point(3, 427);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(225, 25);
            this.lblProgress.Text = "Обработка";
            this.lblProgress.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // pbRenderProgress
            // 
            this.pnlControls.SetColumnSpan(this.pbRenderProgress, 2);
            this.pbRenderProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbRenderProgress.Margin = new System.Windows.Forms.Padding(6, 3, 6, 6);
            this.pbRenderProgress.Name = "pbRenderProgress";
            this.pbRenderProgress.Size = new System.Drawing.Size(219, 26);
            // 
            // mandelbrotPreviewPanel
            // 
            this.pnlControls.SetColumnSpan(this.mandelbrotPreviewPanel, 2);
            this.mandelbrotPreviewPanel.Controls.Add(this.mandelbrotPreviewCanvas);
            this.mandelbrotPreviewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mandelbrotPreviewPanel.Margin = new System.Windows.Forms.Padding(6);
            this.mandelbrotPreviewPanel.Name = "mandelbrotPreviewPanel";
            this.mandelbrotPreviewPanel.Size = new System.Drawing.Size(219, 164);
            // 
            // mandelbrotPreviewCanvas
            // 
            this.mandelbrotPreviewCanvas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mandelbrotPreviewCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mandelbrotPreviewCanvas.Location = new System.Drawing.Point(0, 0);
            this.mandelbrotPreviewCanvas.Name = "mandelbrotPreviewCanvas";
            this.mandelbrotPreviewCanvas.TabStop = false;
            // 
            // nudBaseScale
            // 
            this.nudBaseScale.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            this.nudBaseScale.Name = "nudBaseScale";
            this.nudBaseScale.Size = new System.Drawing.Size(26, 23);
            this.nudBaseScale.Value = new decimal(new int[] { 4, 0, 0, 0 });
            this.nudBaseScale.Visible = false;

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
            // FractalMandelbrotFamilyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 636);
            this.Controls.Add(this.canvas);
            this.Controls.Add(this.pnlControls);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1100, 675);
            this.Name = "FractalMandelbrotFamilyForm";
            this.Text = "FractalFormBase";
            this.Load += new System.EventHandler(this.FormBase_Load);

            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            this.mandelbrotPreviewPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mandelbrotPreviewCanvas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIm)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRe)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBaseScale)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).EndInit();
            this.ResumeLayout(false);
        }
        #endregion

        // Изменение типа панели
        protected System.Windows.Forms.TableLayoutPanel pnlControls;

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