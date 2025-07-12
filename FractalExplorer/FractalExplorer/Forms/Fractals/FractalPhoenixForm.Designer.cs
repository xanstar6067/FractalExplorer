// FractalPhoenixForm.Designer.cs
namespace FractalExplorer.Forms
{
    partial class FractalPhoenixForm
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
            pnlControls = new Panel();
            cbSmooth = new CheckBox();
            btnStateManager = new Button();
            btnSelectPhoenixParameters = new Button();
            lblC2Im = new Label();
            nudC2Im = new NumericUpDown();
            lblC2Re = new Label();
            nudC2Re = new NumericUpDown();
            lblC1Im = new Label();
            nudC1Im = new NumericUpDown();
            lblC1Re = new Label();
            nudC1Re = new NumericUpDown();
            color_configurations = new Button();
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
            nudThreshold = new NumericUpDown();
            nudIterations = new NumericUpDown();
            canvas = new PictureBox();
            lbSSAA = new Label();
            cbSSAA = new ComboBox();
            pnlControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudC2Im).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudC2Re).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudC1Im).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudC1Re).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)canvas).BeginInit();
            SuspendLayout();
            // 
            // pnlControls
            // 
            pnlControls.Controls.Add(lbSSAA);
            pnlControls.Controls.Add(cbSSAA);
            pnlControls.Controls.Add(cbSmooth);
            pnlControls.Controls.Add(btnStateManager);
            pnlControls.Controls.Add(btnSelectPhoenixParameters);
            pnlControls.Controls.Add(lblC2Im);
            pnlControls.Controls.Add(nudC2Im);
            pnlControls.Controls.Add(lblC2Re);
            pnlControls.Controls.Add(nudC2Re);
            pnlControls.Controls.Add(lblC1Im);
            pnlControls.Controls.Add(nudC1Im);
            pnlControls.Controls.Add(lblC1Re);
            pnlControls.Controls.Add(nudC1Re);
            pnlControls.Controls.Add(color_configurations);
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
            pnlControls.Controls.Add(nudThreshold);
            pnlControls.Controls.Add(nudIterations);
            pnlControls.Dock = DockStyle.Left;
            pnlControls.Location = new Point(0, 0);
            pnlControls.Name = "pnlControls";
            pnlControls.Size = new Size(231, 636);
            pnlControls.TabIndex = 0;
            // 
            // cbSmooth
            // 
            cbSmooth.AutoSize = true;
            cbSmooth.Checked = true;
            cbSmooth.CheckState = CheckState.Checked;
            cbSmooth.Location = new Point(12, 336);
            cbSmooth.Name = "cbSmooth";
            cbSmooth.Size = new Size(153, 19);
            cbSmooth.TabIndex = 3;
            cbSmooth.Text = "Плавное окрашивание";
            cbSmooth.UseVisualStyleBackColor = true;
            // 
            // btnStateManager
            // 
            btnStateManager.Location = new Point(12, 472);
            btnStateManager.Name = "btnStateManager";
            btnStateManager.Size = new Size(204, 32);
            btnStateManager.TabIndex = 35;
            btnStateManager.Text = "Менеджер сохранений";
            btnStateManager.UseVisualStyleBackColor = true;
            btnStateManager.Click += btnStateManager_Click;
            // 
            // btnSelectPhoenixParameters
            // 
            btnSelectPhoenixParameters.Location = new Point(12, 128);
            btnSelectPhoenixParameters.Name = "btnSelectPhoenixParameters";
            btnSelectPhoenixParameters.Size = new Size(204, 32);
            btnSelectPhoenixParameters.TabIndex = 8;
            btnSelectPhoenixParameters.Text = "Выбрать C1/C2";
            btnSelectPhoenixParameters.UseVisualStyleBackColor = true;
            btnSelectPhoenixParameters.Click += btnSelectPhoenixParameters_Click;
            // 
            // lblC2Im
            // 
            lblC2Im.AutoSize = true;
            lblC2Im.Location = new Point(138, 99);
            lblC2Im.Name = "lblC2Im";
            lblC2Im.Size = new Size(38, 15);
            lblC2Im.TabIndex = 41;
            lblC2Im.Text = "C2 Im";
            // 
            // nudC2Im
            // 
            nudC2Im.DecimalPlaces = 15;
            nudC2Im.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudC2Im.Location = new Point(12, 97);
            nudC2Im.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudC2Im.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudC2Im.Name = "nudC2Im";
            nudC2Im.Size = new Size(120, 23);
            nudC2Im.TabIndex = 3;
            // 
            // lblC2Re
            // 
            lblC2Re.AutoSize = true;
            lblC2Re.Location = new Point(138, 70);
            lblC2Re.Name = "lblC2Re";
            lblC2Re.Size = new Size(37, 15);
            lblC2Re.TabIndex = 39;
            lblC2Re.Text = "C2 Re";
            // 
            // nudC2Re
            // 
            nudC2Re.DecimalPlaces = 15;
            nudC2Re.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudC2Re.Location = new Point(12, 68);
            nudC2Re.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudC2Re.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudC2Re.Name = "nudC2Re";
            nudC2Re.Size = new Size(120, 23);
            nudC2Re.TabIndex = 2;
            // 
            // lblC1Im
            // 
            lblC1Im.AutoSize = true;
            lblC1Im.Location = new Point(138, 41);
            lblC1Im.Name = "lblC1Im";
            lblC1Im.Size = new Size(58, 15);
            lblC1Im.TabIndex = 37;
            lblC1Im.Text = "C1 Im (Q)";
            // 
            // nudC1Im
            // 
            nudC1Im.DecimalPlaces = 15;
            nudC1Im.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudC1Im.Location = new Point(12, 39);
            nudC1Im.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudC1Im.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudC1Im.Name = "nudC1Im";
            nudC1Im.Size = new Size(120, 23);
            nudC1Im.TabIndex = 1;
            nudC1Im.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // lblC1Re
            // 
            lblC1Re.AutoSize = true;
            lblC1Re.Location = new Point(138, 12);
            lblC1Re.Name = "lblC1Re";
            lblC1Re.Size = new Size(55, 15);
            lblC1Re.TabIndex = 35;
            lblC1Re.Text = "C1 Re (P)";
            // 
            // nudC1Re
            // 
            nudC1Re.DecimalPlaces = 15;
            nudC1Re.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudC1Re.Location = new Point(12, 10);
            nudC1Re.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudC1Re.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudC1Re.Name = "nudC1Re";
            nudC1Re.Size = new Size(120, 23);
            nudC1Re.TabIndex = 0;
            nudC1Re.Value = new decimal(new int[] { 56, 0, 0, 131072 });
            // 
            // color_configurations
            // 
            color_configurations.Location = new Point(12, 396);
            color_configurations.Name = "color_configurations";
            color_configurations.Size = new Size(204, 32);
            color_configurations.TabIndex = 15;
            color_configurations.Text = "Настроить палитру";
            color_configurations.UseVisualStyleBackColor = true;
            color_configurations.Click += color_configurations_Click;
            // 
            // lblProgress
            // 
            lblProgress.AutoSize = true;
            lblProgress.Location = new Point(80, 511);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(67, 15);
            lblProgress.TabIndex = 20;
            lblProgress.Text = "Обработка";
            // 
            // lblZoom
            // 
            lblZoom.AutoSize = true;
            lblZoom.Location = new Point(12, 259);
            lblZoom.Name = "lblZoom";
            lblZoom.Size = new Size(86, 15);
            lblZoom.TabIndex = 16;
            lblZoom.Text = "Приближение";
            // 
            // nudZoom
            // 
            nudZoom.DecimalPlaces = 4;
            nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudZoom.Location = new Point(12, 279);
            nudZoom.Maximum = new decimal(new int[] { -1, -1, -1, 0 });
            nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 393216 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new Size(204, 23);
            nudZoom.TabIndex = 7;
            nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // btnRender
            // 
            btnRender.Location = new Point(12, 434);
            btnRender.Name = "btnRender";
            btnRender.Size = new Size(204, 32);
            btnRender.TabIndex = 16;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            btnRender.Click += btnRender_Click;
            // 
            // pbRenderProgress
            // 
            pbRenderProgress.Location = new Point(12, 529);
            pbRenderProgress.Name = "pbRenderProgress";
            pbRenderProgress.Size = new Size(204, 23);
            pbRenderProgress.TabIndex = 14;
            // 
            // lblThreads
            // 
            lblThreads.AutoSize = true;
            lblThreads.Location = new Point(12, 163);
            lblThreads.Name = "lblThreads";
            lblThreads.Size = new Size(69, 15);
            lblThreads.TabIndex = 13;
            lblThreads.Text = "Потоки ЦП";
            // 
            // cbThreads
            // 
            cbThreads.FormattingEnabled = true;
            cbThreads.Location = new Point(12, 181);
            cbThreads.Name = "cbThreads";
            cbThreads.Size = new Size(204, 23);
            cbThreads.TabIndex = 4;
            // 
            // btnSaveHighRes
            // 
            btnSaveHighRes.Location = new Point(12, 358);
            btnSaveHighRes.Name = "btnSaveHighRes";
            btnSaveHighRes.Size = new Size(204, 32);
            btnSaveHighRes.TabIndex = 12;
            btnSaveHighRes.Text = "Сохранить изображение";
            btnSaveHighRes.UseVisualStyleBackColor = true;
            btnSaveHighRes.Click += btnOpenSaveManager_Click;
            // 
            // lblThreshold
            // 
            lblThreshold.AutoSize = true;
            lblThreshold.Location = new Point(138, 212);
            lblThreshold.Name = "lblThreshold";
            lblThreshold.Size = new Size(85, 15);
            lblThreshold.TabIndex = 7;
            lblThreshold.Text = "Порог выхода";
            // 
            // lblIterations
            // 
            lblIterations.AutoSize = true;
            lblIterations.Location = new Point(138, 235);
            lblIterations.Name = "lblIterations";
            lblIterations.Size = new Size(61, 15);
            lblIterations.TabIndex = 6;
            lblIterations.Text = "Итерации";
            // 
            // nudThreshold
            // 
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            nudThreshold.Location = new Point(12, 210);
            nudThreshold.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudThreshold.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            nudThreshold.Name = "nudThreshold";
            nudThreshold.Size = new Size(120, 23);
            nudThreshold.TabIndex = 6;
            nudThreshold.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // nudIterations
            // 
            nudIterations.Location = new Point(12, 233);
            nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudIterations.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new Size(120, 23);
            nudIterations.TabIndex = 5;
            nudIterations.Value = new decimal(new int[] { 100, 0, 0, 0 });
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
            // lbSSAA
            // 
            lbSSAA.AutoSize = true;
            lbSSAA.Location = new Point(10, 311);
            lbSSAA.Name = "lbSSAA";
            lbSSAA.Size = new Size(112, 15);
            lbSSAA.TabIndex = 43;
            lbSSAA.Text = "Сглаживание SSAA";
            // 
            // cbSSAA
            // 
            cbSSAA.FormattingEnabled = true;
            cbSSAA.Location = new Point(128, 308);
            cbSSAA.Name = "cbSSAA";
            cbSSAA.Size = new Size(95, 23);
            cbSSAA.TabIndex = 42;
            // 
            // FractalPhoenixForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(canvas);
            Controls.Add(pnlControls);
            MinimumSize = new Size(1100, 675);
            Name = "FractalPhoenixForm";
            Text = "Фрактал Феникс";
            FormClosed += FractalPhoenixForm_FormClosed;
            Load += FractalPhoenixForm_Load;
            pnlControls.ResumeLayout(false);
            pnlControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudC2Im).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudC2Re).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudC1Im).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudC1Re).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
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
        protected System.Windows.Forms.Button color_configurations;
        // Новые контролы для Феникса
        protected System.Windows.Forms.Label lblC1Re;
        protected System.Windows.Forms.NumericUpDown nudC1Re;
        protected System.Windows.Forms.Label lblC1Im;
        protected System.Windows.Forms.NumericUpDown nudC1Im;
        protected System.Windows.Forms.Label lblC2Re;
        protected System.Windows.Forms.NumericUpDown nudC2Re;
        protected System.Windows.Forms.Label lblC2Im;
        protected System.Windows.Forms.NumericUpDown nudC2Im;
        protected System.Windows.Forms.Button btnSelectPhoenixParameters;
        private Button btnStateManager;
        private CheckBox cbSmooth;
        protected Label lbSSAA;
        private ComboBox cbSSAA;
    }
}