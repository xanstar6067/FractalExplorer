namespace FractalExplorer.Forms
{
    partial class FractalNovaForm
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
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FractalNovaForm));
            pnlControls = new TableLayoutPanel();
            gbNovaParameters = new GroupBox();
            tlpNovaParameters = new TableLayoutPanel();
            nudP_Re = new NumericUpDown();
            lblP_Re = new Label();
            nudP_Im = new NumericUpDown();
            lblP_Im = new Label();
            gbInitialConditions = new GroupBox();
            tlpInitialConditions = new TableLayoutPanel();
            nudZ0_Re = new NumericUpDown();
            lblZ0_Re = new Label();
            nudZ0_Im = new NumericUpDown();
            lblZ0_Im = new Label();
            lblM = new Label();
            nudM = new NumericUpDown();
            nudIterations = new NumericUpDown();
            lblIterations = new Label();
            nudThreshold = new NumericUpDown();
            lblThreshold = new Label();
            lblZoom = new Label();
            nudZoom = new NumericUpDown();
            cbThreads = new ComboBox();
            lblThreads = new Label();
            cbSSAA = new ComboBox();
            lblSSAA = new Label();
            cbSmooth = new CheckBox();
            btnSaveHighRes = new Button();
            btnConfigurePalette = new Button();
            btnRender = new Button();
            btnStateManager = new Button();
            lblProgress = new Label();
            pbRenderProgress = new ProgressBar();
            canvas = new PictureBox();
            pnlControls.SuspendLayout();
            gbNovaParameters.SuspendLayout();
            tlpNovaParameters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudP_Re).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudP_Im).BeginInit();
            gbInitialConditions.SuspendLayout();
            tlpInitialConditions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudZ0_Re).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZ0_Im).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudM).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)canvas).BeginInit();
            SuspendLayout();
            // 
            // pnlControls
            // 
            pnlControls.ColumnCount = 2;
            pnlControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            pnlControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            pnlControls.Controls.Add(gbNovaParameters, 0, 0);
            pnlControls.Controls.Add(gbInitialConditions, 0, 1);
            pnlControls.Controls.Add(lblM, 1, 2);
            pnlControls.Controls.Add(nudM, 0, 2);
            pnlControls.Controls.Add(nudIterations, 0, 3);
            pnlControls.Controls.Add(lblIterations, 1, 3);
            pnlControls.Controls.Add(nudThreshold, 0, 4);
            pnlControls.Controls.Add(lblThreshold, 1, 4);
            pnlControls.Controls.Add(lblZoom, 0, 5);
            pnlControls.Controls.Add(nudZoom, 0, 6);
            pnlControls.Controls.Add(cbThreads, 0, 7);
            pnlControls.Controls.Add(lblThreads, 1, 7);
            pnlControls.Controls.Add(cbSSAA, 0, 8);
            pnlControls.Controls.Add(lblSSAA, 1, 8);
            pnlControls.Controls.Add(cbSmooth, 0, 9);
            pnlControls.Controls.Add(btnSaveHighRes, 0, 10);
            pnlControls.Controls.Add(btnConfigurePalette, 0, 11);
            pnlControls.Controls.Add(btnRender, 0, 12);
            pnlControls.Controls.Add(btnStateManager, 0, 13);
            pnlControls.Controls.Add(lblProgress, 0, 14);
            pnlControls.Controls.Add(pbRenderProgress, 0, 15);
            pnlControls.Dock = DockStyle.Left;
            pnlControls.Location = new Point(0, 0);
            pnlControls.Name = "pnlControls";
            pnlControls.RowCount = 17;
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlControls.Size = new Size(231, 636);
            pnlControls.TabIndex = 0;
            // 
            // gbNovaParameters
            // 
            pnlControls.SetColumnSpan(gbNovaParameters, 2);
            gbNovaParameters.Controls.Add(tlpNovaParameters);
            gbNovaParameters.Dock = DockStyle.Fill;
            gbNovaParameters.Location = new Point(3, 3);
            gbNovaParameters.Name = "gbNovaParameters";
            gbNovaParameters.Size = new Size(225, 80);
            gbNovaParameters.TabIndex = 0;
            gbNovaParameters.TabStop = false;
            gbNovaParameters.Text = "Параметры степени (P)";
            // 
            // tlpNovaParameters
            // 
            tlpNovaParameters.ColumnCount = 2;
            tlpNovaParameters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tlpNovaParameters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tlpNovaParameters.Controls.Add(nudP_Re, 0, 0);
            tlpNovaParameters.Controls.Add(lblP_Re, 1, 0);
            tlpNovaParameters.Controls.Add(nudP_Im, 0, 1);
            tlpNovaParameters.Controls.Add(lblP_Im, 1, 1);
            tlpNovaParameters.Dock = DockStyle.Fill;
            tlpNovaParameters.Location = new Point(3, 19);
            tlpNovaParameters.Name = "tlpNovaParameters";
            tlpNovaParameters.RowCount = 2;
            tlpNovaParameters.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpNovaParameters.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpNovaParameters.Size = new Size(219, 58);
            tlpNovaParameters.TabIndex = 0;
            // 
            // nudP_Re
            // 
            nudP_Re.DecimalPlaces = 15;
            nudP_Re.Dock = DockStyle.Fill;
            nudP_Re.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudP_Re.Location = new Point(3, 3);
            nudP_Re.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudP_Re.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            nudP_Re.Name = "nudP_Re";
            nudP_Re.Size = new Size(114, 23);
            nudP_Re.TabIndex = 0;
            nudP_Re.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // lblP_Re
            // 
            lblP_Re.AutoSize = true;
            lblP_Re.Dock = DockStyle.Fill;
            lblP_Re.Location = new Point(123, 0);
            lblP_Re.Name = "lblP_Re";
            lblP_Re.Size = new Size(93, 29);
            lblP_Re.TabIndex = 1;
            lblP_Re.Text = "Степень P (Re)";
            lblP_Re.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // nudP_Im
            // 
            nudP_Im.DecimalPlaces = 15;
            nudP_Im.Dock = DockStyle.Fill;
            nudP_Im.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudP_Im.Location = new Point(3, 32);
            nudP_Im.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudP_Im.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            nudP_Im.Name = "nudP_Im";
            nudP_Im.Size = new Size(114, 23);
            nudP_Im.TabIndex = 2;
            // 
            // lblP_Im
            // 
            lblP_Im.AutoSize = true;
            lblP_Im.Dock = DockStyle.Fill;
            lblP_Im.Location = new Point(123, 29);
            lblP_Im.Name = "lblP_Im";
            lblP_Im.Size = new Size(93, 29);
            lblP_Im.TabIndex = 3;
            lblP_Im.Text = "Степень P (Im)";
            lblP_Im.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // gbInitialConditions
            // 
            pnlControls.SetColumnSpan(gbInitialConditions, 2);
            gbInitialConditions.Controls.Add(tlpInitialConditions);
            gbInitialConditions.Dock = DockStyle.Fill;
            gbInitialConditions.Location = new Point(3, 89);
            gbInitialConditions.Name = "gbInitialConditions";
            gbInitialConditions.Size = new Size(225, 80);
            gbInitialConditions.TabIndex = 1;
            gbInitialConditions.TabStop = false;
            gbInitialConditions.Text = "Начальные условия (Z₀)";
            // 
            // tlpInitialConditions
            // 
            tlpInitialConditions.ColumnCount = 2;
            tlpInitialConditions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tlpInitialConditions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tlpInitialConditions.Controls.Add(nudZ0_Re, 0, 0);
            tlpInitialConditions.Controls.Add(lblZ0_Re, 1, 0);
            tlpInitialConditions.Controls.Add(nudZ0_Im, 0, 1);
            tlpInitialConditions.Controls.Add(lblZ0_Im, 1, 1);
            tlpInitialConditions.Dock = DockStyle.Fill;
            tlpInitialConditions.Location = new Point(3, 19);
            tlpInitialConditions.Name = "tlpInitialConditions";
            tlpInitialConditions.RowCount = 2;
            tlpInitialConditions.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpInitialConditions.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpInitialConditions.Size = new Size(219, 58);
            tlpInitialConditions.TabIndex = 0;
            // 
            // nudZ0_Re
            // 
            nudZ0_Re.DecimalPlaces = 15;
            nudZ0_Re.Dock = DockStyle.Fill;
            nudZ0_Re.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudZ0_Re.Location = new Point(3, 3);
            nudZ0_Re.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudZ0_Re.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            nudZ0_Re.Name = "nudZ0_Re";
            nudZ0_Re.Size = new Size(114, 23);
            nudZ0_Re.TabIndex = 0;
            nudZ0_Re.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblZ0_Re
            // 
            lblZ0_Re.AutoSize = true;
            lblZ0_Re.Dock = DockStyle.Fill;
            lblZ0_Re.Location = new Point(123, 0);
            lblZ0_Re.Name = "lblZ0_Re";
            lblZ0_Re.Size = new Size(93, 29);
            lblZ0_Re.TabIndex = 1;
            lblZ0_Re.Text = "Начальное Z₀ (Re)";
            lblZ0_Re.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // nudZ0_Im
            // 
            nudZ0_Im.DecimalPlaces = 15;
            nudZ0_Im.Dock = DockStyle.Fill;
            nudZ0_Im.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudZ0_Im.Location = new Point(3, 32);
            nudZ0_Im.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudZ0_Im.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            nudZ0_Im.Name = "nudZ0_Im";
            nudZ0_Im.Size = new Size(114, 23);
            nudZ0_Im.TabIndex = 2;
            // 
            // lblZ0_Im
            // 
            lblZ0_Im.AutoSize = true;
            lblZ0_Im.Dock = DockStyle.Fill;
            lblZ0_Im.Location = new Point(123, 29);
            lblZ0_Im.Name = "lblZ0_Im";
            lblZ0_Im.Size = new Size(93, 29);
            lblZ0_Im.TabIndex = 3;
            lblZ0_Im.Text = "Начальное Z₀ (Im)";
            lblZ0_Im.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblM
            // 
            lblM.AutoSize = true;
            lblM.Dock = DockStyle.Fill;
            lblM.Location = new Point(130, 172);
            lblM.Name = "lblM";
            lblM.Size = new Size(98, 29);
            lblM.TabIndex = 3;
            lblM.Text = "Релаксация (m)";
            lblM.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // nudM
            // 
            nudM.DecimalPlaces = 3;
            nudM.Dock = DockStyle.Fill;
            nudM.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudM.Location = new Point(6, 175);
            nudM.Margin = new Padding(6, 3, 3, 3);
            nudM.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            nudM.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            nudM.Name = "nudM";
            nudM.Size = new Size(118, 23);
            nudM.TabIndex = 2;
            nudM.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // nudIterations
            // 
            nudIterations.Dock = DockStyle.Fill;
            nudIterations.Location = new Point(6, 204);
            nudIterations.Margin = new Padding(6, 3, 3, 3);
            nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudIterations.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new Size(118, 23);
            nudIterations.TabIndex = 4;
            nudIterations.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // lblIterations
            // 
            lblIterations.AutoSize = true;
            lblIterations.Dock = DockStyle.Fill;
            lblIterations.Location = new Point(130, 201);
            lblIterations.Name = "lblIterations";
            lblIterations.Size = new Size(98, 29);
            lblIterations.TabIndex = 5;
            lblIterations.Text = "Итерации";
            lblIterations.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // nudThreshold
            // 
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Dock = DockStyle.Fill;
            nudThreshold.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            nudThreshold.Location = new Point(6, 233);
            nudThreshold.Margin = new Padding(6, 3, 3, 3);
            nudThreshold.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudThreshold.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            nudThreshold.Name = "nudThreshold";
            nudThreshold.Size = new Size(118, 23);
            nudThreshold.TabIndex = 6;
            nudThreshold.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // lblThreshold
            // 
            lblThreshold.AutoSize = true;
            lblThreshold.Dock = DockStyle.Fill;
            lblThreshold.Location = new Point(130, 230);
            lblThreshold.Name = "lblThreshold";
            lblThreshold.Size = new Size(98, 29);
            lblThreshold.TabIndex = 7;
            lblThreshold.Text = "Порог выхода";
            lblThreshold.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblZoom
            // 
            lblZoom.AutoSize = true;
            pnlControls.SetColumnSpan(lblZoom, 2);
            lblZoom.Dock = DockStyle.Fill;
            lblZoom.Location = new Point(3, 259);
            lblZoom.Name = "lblZoom";
            lblZoom.Size = new Size(225, 15);
            lblZoom.TabIndex = 8;
            lblZoom.Text = "Приближение";
            lblZoom.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // nudZoom
            // 
            pnlControls.SetColumnSpan(nudZoom, 2);
            nudZoom.DecimalPlaces = 15;
            nudZoom.Dock = DockStyle.Fill;
            nudZoom.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            nudZoom.Location = new Point(6, 277);
            nudZoom.Margin = new Padding(6, 3, 6, 3);
            nudZoom.Maximum = new decimal(new int[] { -1, -1, -1, 0 });
            nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 393216 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new Size(219, 23);
            nudZoom.TabIndex = 9;
            nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // cbThreads
            // 
            cbThreads.Dock = DockStyle.Fill;
            cbThreads.FormattingEnabled = true;
            cbThreads.Location = new Point(6, 306);
            cbThreads.Margin = new Padding(6, 3, 3, 3);
            cbThreads.Name = "cbThreads";
            cbThreads.Size = new Size(118, 23);
            cbThreads.TabIndex = 10;
            // 
            // lblThreads
            // 
            lblThreads.AutoSize = true;
            lblThreads.Dock = DockStyle.Fill;
            lblThreads.Location = new Point(130, 303);
            lblThreads.Name = "lblThreads";
            lblThreads.Size = new Size(98, 29);
            lblThreads.TabIndex = 11;
            lblThreads.Text = "Потоки ЦП";
            lblThreads.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cbSSAA
            // 
            cbSSAA.Dock = DockStyle.Fill;
            cbSSAA.FormattingEnabled = true;
            cbSSAA.Location = new Point(6, 335);
            cbSSAA.Margin = new Padding(6, 3, 3, 3);
            cbSSAA.Name = "cbSSAA";
            cbSSAA.Size = new Size(118, 23);
            cbSSAA.TabIndex = 12;
            // 
            // lblSSAA
            // 
            lblSSAA.AutoSize = true;
            lblSSAA.Dock = DockStyle.Fill;
            lblSSAA.Location = new Point(130, 332);
            lblSSAA.Name = "lblSSAA";
            lblSSAA.Size = new Size(98, 29);
            lblSSAA.TabIndex = 13;
            lblSSAA.Text = "Сглаживание";
            lblSSAA.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cbSmooth
            // 
            cbSmooth.AutoSize = true;
            cbSmooth.Checked = true;
            cbSmooth.CheckState = CheckState.Checked;
            pnlControls.SetColumnSpan(cbSmooth, 2);
            cbSmooth.Dock = DockStyle.Fill;
            cbSmooth.Location = new Point(6, 364);
            cbSmooth.Margin = new Padding(6, 3, 3, 3);
            cbSmooth.Name = "cbSmooth";
            cbSmooth.Size = new Size(222, 19);
            cbSmooth.TabIndex = 14;
            cbSmooth.Text = "Плавное окрашивание";
            cbSmooth.UseVisualStyleBackColor = true;
            // 
            // btnSaveHighRes
            // 
            pnlControls.SetColumnSpan(btnSaveHighRes, 2);
            btnSaveHighRes.Dock = DockStyle.Fill;
            btnSaveHighRes.Location = new Point(6, 389);
            btnSaveHighRes.Margin = new Padding(6, 3, 6, 3);
            btnSaveHighRes.Name = "btnSaveHighRes";
            btnSaveHighRes.Size = new Size(219, 39);
            btnSaveHighRes.TabIndex = 15;
            btnSaveHighRes.Text = "Сохранить изображение";
            btnSaveHighRes.UseVisualStyleBackColor = true;
            btnSaveHighRes.Click += btnSaveHighRes_Click;
            // 
            // btnConfigurePalette
            // 
            pnlControls.SetColumnSpan(btnConfigurePalette, 2);
            btnConfigurePalette.Dock = DockStyle.Fill;
            btnConfigurePalette.Location = new Point(6, 434);
            btnConfigurePalette.Margin = new Padding(6, 3, 6, 3);
            btnConfigurePalette.Name = "btnConfigurePalette";
            btnConfigurePalette.Size = new Size(219, 39);
            btnConfigurePalette.TabIndex = 16;
            btnConfigurePalette.Text = "Настроить палитру";
            btnConfigurePalette.UseVisualStyleBackColor = true;
            btnConfigurePalette.Click += btnConfigurePalette_Click;
            // 
            // btnRender
            // 
            pnlControls.SetColumnSpan(btnRender, 2);
            btnRender.Dock = DockStyle.Fill;
            btnRender.Location = new Point(6, 479);
            btnRender.Margin = new Padding(6, 3, 6, 3);
            btnRender.Name = "btnRender";
            btnRender.Size = new Size(219, 39);
            btnRender.TabIndex = 17;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            // 
            // btnStateManager
            // 
            pnlControls.SetColumnSpan(btnStateManager, 2);
            btnStateManager.Dock = DockStyle.Fill;
            btnStateManager.Location = new Point(6, 524);
            btnStateManager.Margin = new Padding(6, 3, 6, 3);
            btnStateManager.Name = "btnStateManager";
            btnStateManager.Size = new Size(219, 39);
            btnStateManager.TabIndex = 18;
            btnStateManager.Text = "Менеджер сохранений";
            btnStateManager.UseVisualStyleBackColor = true;
            btnStateManager.Click += btnStateManager_Click;
            // 
            // lblProgress
            // 
            lblProgress.AutoSize = true;
            pnlControls.SetColumnSpan(lblProgress, 2);
            lblProgress.Dock = DockStyle.Fill;
            lblProgress.Location = new Point(3, 566);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(225, 20);
            lblProgress.TabIndex = 19;
            lblProgress.Text = "Обработка";
            lblProgress.TextAlign = ContentAlignment.BottomCenter;
            // 
            // pbRenderProgress
            // 
            pnlControls.SetColumnSpan(pbRenderProgress, 2);
            pbRenderProgress.Dock = DockStyle.Fill;
            pbRenderProgress.Location = new Point(6, 589);
            pbRenderProgress.Margin = new Padding(6, 3, 6, 3);
            pbRenderProgress.Name = "pbRenderProgress";
            pbRenderProgress.Size = new Size(219, 24);
            pbRenderProgress.TabIndex = 20;
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
            // FractalNovaForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(canvas);
            Controls.Add(pnlControls);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1100, 675);
            Name = "FractalNovaForm";
            Text = "Фрактал Nova";
            pnlControls.ResumeLayout(false);
            pnlControls.PerformLayout();
            gbNovaParameters.ResumeLayout(false);
            tlpNovaParameters.ResumeLayout(false);
            tlpNovaParameters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudP_Re).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudP_Im).EndInit();
            gbInitialConditions.ResumeLayout(false);
            tlpInitialConditions.ResumeLayout(false);
            tlpInitialConditions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudZ0_Re).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZ0_Im).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudM).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)canvas).EndInit();
            ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.TableLayoutPanel pnlControls;
        private System.Windows.Forms.PictureBox canvas;
        private System.Windows.Forms.GroupBox gbNovaParameters;
        private System.Windows.Forms.TableLayoutPanel tlpNovaParameters;
        private System.Windows.Forms.NumericUpDown nudP_Re;
        private System.Windows.Forms.Label lblP_Re;
        private System.Windows.Forms.NumericUpDown nudP_Im;
        private System.Windows.Forms.Label lblP_Im;
        private System.Windows.Forms.GroupBox gbInitialConditions;
        private System.Windows.Forms.TableLayoutPanel tlpInitialConditions;
        private System.Windows.Forms.NumericUpDown nudZ0_Re;
        private System.Windows.Forms.Label lblZ0_Re;
        private System.Windows.Forms.NumericUpDown nudZ0_Im;
        private System.Windows.Forms.Label lblZ0_Im;
        private System.Windows.Forms.NumericUpDown nudM;
        private System.Windows.Forms.Label lblM;
        private System.Windows.Forms.NumericUpDown nudIterations;
        private System.Windows.Forms.Label lblIterations;
        private System.Windows.Forms.NumericUpDown nudThreshold;
        private System.Windows.Forms.Label lblThreshold;
        private System.Windows.Forms.Label lblZoom;
        private System.Windows.Forms.NumericUpDown nudZoom;
        private System.Windows.Forms.ComboBox cbThreads;
        private System.Windows.Forms.Label lblThreads;
        private System.Windows.Forms.ComboBox cbSSAA;
        private System.Windows.Forms.Label lblSSAA;
        private System.Windows.Forms.CheckBox cbSmooth;
        private System.Windows.Forms.Button btnSaveHighRes;
        private System.Windows.Forms.Button btnConfigurePalette;
        private System.Windows.Forms.Button btnRender;
        private System.Windows.Forms.Button btnStateManager;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.ProgressBar pbRenderProgress;
    }
}