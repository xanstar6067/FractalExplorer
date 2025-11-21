namespace FractalExplorer.Forms.Fractals
{
    partial class FractalCollatzForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FractalCollatzForm));
            pnlControls = new TableLayoutPanel();
            nudIterations = new NumericUpDown();
            lblIterations = new Label();
            nudThreshold = new NumericUpDown();
            lblThreshold = new Label();
            lblZoom = new Label();
            nudZoom = new NumericUpDown();
            cbThreads = new ComboBox();
            lblThreads = new Label();
            cbSSAA = new ComboBox();
            lbSSAA = new Label();
            cbSmooth = new CheckBox();
            lblVariation = new Label();
            cbVariation = new ComboBox();
            lblPParameter = new Label();
            nudPParameter = new NumericUpDown();
            btnSaveHighRes = new Button();
            color_configurations = new Button();
            btnRender = new Button();
            btnStateManager = new Button();
            lblProgress = new Label();
            pbRenderProgress = new ProgressBar();
            canvas = new PictureBox();
            pnlControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPParameter).BeginInit();
            ((System.ComponentModel.ISupportInitialize)canvas).BeginInit();
            SuspendLayout();
            // 
            // pnlControls
            // 
            pnlControls.ColumnCount = 2;
            pnlControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            pnlControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            pnlControls.Controls.Add(nudIterations, 0, 0);
            pnlControls.Controls.Add(lblIterations, 1, 0);
            pnlControls.Controls.Add(nudThreshold, 0, 1);
            pnlControls.Controls.Add(lblThreshold, 1, 1);
            pnlControls.Controls.Add(lblZoom, 0, 2);
            pnlControls.Controls.Add(nudZoom, 0, 3);
            pnlControls.Controls.Add(cbThreads, 0, 4);
            pnlControls.Controls.Add(lblThreads, 1, 4);
            pnlControls.Controls.Add(cbSSAA, 0, 5);
            pnlControls.Controls.Add(lbSSAA, 1, 5);
            pnlControls.Controls.Add(cbSmooth, 0, 6);
            pnlControls.Controls.Add(lblVariation, 1, 7);
            pnlControls.Controls.Add(cbVariation, 0, 7);
            pnlControls.Controls.Add(lblPParameter, 1, 8);
            pnlControls.Controls.Add(nudPParameter, 0, 8);
            pnlControls.Controls.Add(btnSaveHighRes, 0, 9);
            pnlControls.Controls.Add(color_configurations, 0, 10);
            pnlControls.Controls.Add(btnRender, 0, 11);
            pnlControls.Controls.Add(btnStateManager, 0, 12);
            pnlControls.Controls.Add(lblProgress, 0, 13);
            pnlControls.Controls.Add(pbRenderProgress, 0, 14);
            pnlControls.Dock = DockStyle.Left;
            pnlControls.Location = new Point(0, 0);
            pnlControls.Name = "pnlControls";
            pnlControls.RowCount = 16;
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
            // nudIterations
            // 
            nudIterations.Dock = DockStyle.Fill;
            nudIterations.Location = new Point(6, 6);
            nudIterations.Margin = new Padding(6, 6, 3, 3);
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new Size(118, 23);
            nudIterations.TabIndex = 9;
            // 
            // lblIterations
            // 
            lblIterations.AutoSize = true;
            lblIterations.Dock = DockStyle.Fill;
            lblIterations.Location = new Point(130, 0);
            lblIterations.Name = "lblIterations";
            lblIterations.Size = new Size(98, 32);
            lblIterations.TabIndex = 10;
            lblIterations.Text = "Итерации";
            lblIterations.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // nudThreshold
            // 
            nudThreshold.Dock = DockStyle.Fill;
            nudThreshold.Location = new Point(6, 35);
            nudThreshold.Margin = new Padding(6, 3, 3, 3);
            nudThreshold.Name = "nudThreshold";
            nudThreshold.Size = new Size(118, 23);
            nudThreshold.TabIndex = 11;
            // 
            // lblThreshold
            // 
            lblThreshold.AutoSize = true;
            lblThreshold.Dock = DockStyle.Fill;
            lblThreshold.Location = new Point(130, 32);
            lblThreshold.Name = "lblThreshold";
            lblThreshold.Size = new Size(98, 29);
            lblThreshold.TabIndex = 12;
            lblThreshold.Text = "Порог выхода";
            lblThreshold.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblZoom
            // 
            lblZoom.AutoSize = true;
            pnlControls.SetColumnSpan(lblZoom, 2);
            lblZoom.Dock = DockStyle.Fill;
            lblZoom.Location = new Point(3, 61);
            lblZoom.Name = "lblZoom";
            lblZoom.Size = new Size(225, 15);
            lblZoom.TabIndex = 13;
            lblZoom.Text = "Приближение";
            lblZoom.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // nudZoom
            // 
            pnlControls.SetColumnSpan(nudZoom, 2);
            nudZoom.Dock = DockStyle.Fill;
            nudZoom.Location = new Point(6, 79);
            nudZoom.Margin = new Padding(6, 3, 6, 3);
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new Size(219, 23);
            nudZoom.TabIndex = 14;
            // 
            // cbThreads
            // 
            cbThreads.Dock = DockStyle.Fill;
            cbThreads.FormattingEnabled = true;
            cbThreads.Location = new Point(6, 108);
            cbThreads.Margin = new Padding(6, 3, 3, 3);
            cbThreads.Name = "cbThreads";
            cbThreads.Size = new Size(118, 23);
            cbThreads.TabIndex = 15;
            // 
            // lblThreads
            // 
            lblThreads.AutoSize = true;
            lblThreads.Dock = DockStyle.Fill;
            lblThreads.Location = new Point(130, 105);
            lblThreads.Name = "lblThreads";
            lblThreads.Size = new Size(98, 29);
            lblThreads.TabIndex = 16;
            lblThreads.Text = "Потоки ЦП";
            lblThreads.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cbSSAA
            // 
            cbSSAA.Dock = DockStyle.Fill;
            cbSSAA.FormattingEnabled = true;
            cbSSAA.Location = new Point(6, 137);
            cbSSAA.Margin = new Padding(6, 3, 3, 3);
            cbSSAA.Name = "cbSSAA";
            cbSSAA.Size = new Size(118, 23);
            cbSSAA.TabIndex = 17;
            // 
            // lbSSAA
            // 
            lbSSAA.AutoSize = true;
            lbSSAA.Dock = DockStyle.Fill;
            lbSSAA.Location = new Point(130, 134);
            lbSSAA.Name = "lbSSAA";
            lbSSAA.Size = new Size(98, 29);
            lbSSAA.TabIndex = 18;
            lbSSAA.Text = "Сглаживание";
            lbSSAA.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cbSmooth
            // 
            cbSmooth.AutoSize = true;
            cbSmooth.Checked = true;
            cbSmooth.CheckState = CheckState.Checked;
            pnlControls.SetColumnSpan(cbSmooth, 2);
            cbSmooth.Dock = DockStyle.Fill;
            cbSmooth.Location = new Point(6, 166);
            cbSmooth.Margin = new Padding(6, 3, 3, 3);
            cbSmooth.Name = "cbSmooth";
            cbSmooth.Size = new Size(222, 19);
            cbSmooth.TabIndex = 19;
            cbSmooth.Text = "Плавное окрашивание";
            cbSmooth.UseVisualStyleBackColor = true;
            // 
            // lblVariation
            // 
            lblVariation.AutoSize = true;
            lblVariation.Dock = DockStyle.Fill;
            lblVariation.Location = new Point(130, 188);
            lblVariation.Name = "lblVariation";
            lblVariation.Size = new Size(98, 29);
            lblVariation.TabIndex = 21;
            lblVariation.Text = "Вариация";
            lblVariation.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cbVariation
            // 
            cbVariation.Dock = DockStyle.Fill;
            cbVariation.DropDownStyle = ComboBoxStyle.DropDownList;
            cbVariation.FormattingEnabled = true;
            cbVariation.Location = new Point(6, 191);
            cbVariation.Margin = new Padding(6, 3, 3, 3);
            cbVariation.Name = "cbVariation";
            cbVariation.Size = new Size(118, 23);
            cbVariation.TabIndex = 20;
            // 
            // lblPParameter
            // 
            lblPParameter.AutoSize = true;
            lblPParameter.Dock = DockStyle.Fill;
            lblPParameter.Location = new Point(130, 217);
            lblPParameter.Name = "lblPParameter";
            lblPParameter.Size = new Size(98, 29);
            lblPParameter.TabIndex = 23;
            lblPParameter.Text = "Параметр P";
            lblPParameter.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // nudPParameter
            // 
            nudPParameter.Dock = DockStyle.Fill;
            nudPParameter.Location = new Point(6, 220);
            nudPParameter.Margin = new Padding(6, 3, 3, 3);
            nudPParameter.Name = "nudPParameter";
            nudPParameter.Size = new Size(118, 23);
            nudPParameter.TabIndex = 22;
            // 
            // btnSaveHighRes
            // 
            pnlControls.SetColumnSpan(btnSaveHighRes, 2);
            btnSaveHighRes.Dock = DockStyle.Fill;
            btnSaveHighRes.Location = new Point(6, 249);
            btnSaveHighRes.Margin = new Padding(6, 3, 6, 3);
            btnSaveHighRes.Name = "btnSaveHighRes";
            btnSaveHighRes.Size = new Size(219, 39);
            btnSaveHighRes.TabIndex = 24;
            btnSaveHighRes.Text = "Сохранить изображение";
            btnSaveHighRes.UseVisualStyleBackColor = true;
            btnSaveHighRes.Click += btnOpenSaveManager_Click;
            // 
            // color_configurations
            // 
            pnlControls.SetColumnSpan(color_configurations, 2);
            color_configurations.Dock = DockStyle.Fill;
            color_configurations.Location = new Point(6, 294);
            color_configurations.Margin = new Padding(6, 3, 6, 3);
            color_configurations.Name = "color_configurations";
            color_configurations.Size = new Size(219, 39);
            color_configurations.TabIndex = 25;
            color_configurations.Text = "Настроить палитру";
            color_configurations.UseVisualStyleBackColor = true;
            color_configurations.Click += color_configurations_Click;
            // 
            // btnRender
            // 
            pnlControls.SetColumnSpan(btnRender, 2);
            btnRender.Dock = DockStyle.Fill;
            btnRender.Location = new Point(6, 339);
            btnRender.Margin = new Padding(6, 3, 6, 3);
            btnRender.Name = "btnRender";
            btnRender.Size = new Size(219, 39);
            btnRender.TabIndex = 26;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            btnRender.Click += btnRender_Click;
            // 
            // btnStateManager
            // 
            pnlControls.SetColumnSpan(btnStateManager, 2);
            btnStateManager.Dock = DockStyle.Fill;
            btnStateManager.Location = new Point(6, 384);
            btnStateManager.Margin = new Padding(6, 3, 6, 3);
            btnStateManager.Name = "btnStateManager";
            btnStateManager.Size = new Size(219, 39);
            btnStateManager.TabIndex = 27;
            btnStateManager.Text = "Менеджер сохранений";
            btnStateManager.UseVisualStyleBackColor = true;
            btnStateManager.Click += btnStateManager_Click;
            // 
            // lblProgress
            // 
            lblProgress.AutoSize = true;
            pnlControls.SetColumnSpan(lblProgress, 2);
            lblProgress.Dock = DockStyle.Fill;
            lblProgress.Location = new Point(3, 426);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(225, 20);
            lblProgress.TabIndex = 28;
            lblProgress.Text = "Обработка";
            lblProgress.TextAlign = ContentAlignment.BottomCenter;
            // 
            // pbRenderProgress
            // 
            pnlControls.SetColumnSpan(pbRenderProgress, 2);
            pbRenderProgress.Dock = DockStyle.Fill;
            pbRenderProgress.Location = new Point(6, 449);
            pbRenderProgress.Margin = new Padding(6, 3, 6, 3);
            pbRenderProgress.Name = "pbRenderProgress";
            pbRenderProgress.Size = new Size(219, 24);
            pbRenderProgress.TabIndex = 29;
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
            // FractalCollatzForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(canvas);
            Controls.Add(pnlControls);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1100, 675);
            Name = "FractalCollatzForm";
            Text = "Фрактал Коллатца";
            FormClosed += FractalCollatzForm_FormClosed;
            Load += FractalCollatzForm_Load;
            pnlControls.ResumeLayout(false);
            pnlControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPParameter).EndInit();
            ((System.ComponentModel.ISupportInitialize)canvas).EndInit();
            ResumeLayout(false);

        }

        #endregion

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
        protected System.Windows.Forms.Button color_configurations;
        private System.Windows.Forms.Button btnStateManager;
        private System.Windows.Forms.CheckBox cbSmooth;
        protected System.Windows.Forms.Label lbSSAA;
        private System.Windows.Forms.ComboBox cbSSAA;
        private System.Windows.Forms.Label lblVariation;
        private System.Windows.Forms.ComboBox cbVariation;
        private System.Windows.Forms.Label lblPParameter;
        private System.Windows.Forms.NumericUpDown nudPParameter;
    }
}