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
            this.pnlControls = new System.Windows.Forms.TableLayoutPanel();
            this.nudIterations = new System.Windows.Forms.NumericUpDown();
            this.lblIterations = new System.Windows.Forms.Label();
            this.nudThreshold = new System.Windows.Forms.NumericUpDown();
            this.lblThreshold = new System.Windows.Forms.Label();
            this.lblZoom = new System.Windows.Forms.Label();
            this.nudZoom = new System.Windows.Forms.NumericUpDown();
            this.cbThreads = new System.Windows.Forms.ComboBox();
            this.lblThreads = new System.Windows.Forms.Label();
            this.cbSSAA = new System.Windows.Forms.ComboBox();
            this.lbSSAA = new System.Windows.Forms.Label();
            this.cbSmooth = new System.Windows.Forms.CheckBox();
            this.lblVariation = new System.Windows.Forms.Label();
            this.cbVariation = new System.Windows.Forms.ComboBox();
            this.lblPParameter = new System.Windows.Forms.Label();
            this.nudPParameter = new System.Windows.Forms.NumericUpDown();
            this.btnSaveHighRes = new System.Windows.Forms.Button();
            this.color_configurations = new System.Windows.Forms.Button();
            this.btnRender = new System.Windows.Forms.Button();
            this.btnStateManager = new System.Windows.Forms.Button();
            this.lblProgress = new System.Windows.Forms.Label();
            this.pbRenderProgress = new System.Windows.Forms.ProgressBar();
            this.canvas = new System.Windows.Forms.PictureBox();
            this.pnlControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPParameter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlControls
            // 
            this.pnlControls.ColumnCount = 2;
            this.pnlControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.pnlControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.pnlControls.Controls.Add(this.nudIterations, 0, 0);
            this.pnlControls.Controls.Add(this.lblIterations, 1, 0);
            this.pnlControls.Controls.Add(this.nudThreshold, 0, 1);
            this.pnlControls.Controls.Add(this.lblThreshold, 1, 1);
            this.pnlControls.Controls.Add(this.lblZoom, 0, 2);
            this.pnlControls.Controls.Add(this.nudZoom, 0, 3);
            this.pnlControls.Controls.Add(this.cbThreads, 0, 4);
            this.pnlControls.Controls.Add(this.lblThreads, 1, 4);
            this.pnlControls.Controls.Add(this.cbSSAA, 0, 5);
            this.pnlControls.Controls.Add(this.lbSSAA, 1, 5);
            this.pnlControls.Controls.Add(this.cbSmooth, 0, 6);
            this.pnlControls.Controls.Add(this.lblVariation, 1, 7);
            this.pnlControls.Controls.Add(this.cbVariation, 0, 7);
            this.pnlControls.Controls.Add(this.lblPParameter, 1, 8);
            this.pnlControls.Controls.Add(this.nudPParameter, 0, 8);
            this.pnlControls.Controls.Add(this.btnSaveHighRes, 0, 9);
            this.pnlControls.Controls.Add(this.color_configurations, 0, 10);
            this.pnlControls.Controls.Add(this.btnRender, 0, 11);
            this.pnlControls.Controls.Add(this.btnStateManager, 0, 12);
            this.pnlControls.Controls.Add(this.lblProgress, 0, 13);
            this.pnlControls.Controls.Add(this.pbRenderProgress, 0, 14);
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
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlControls.Size = new System.Drawing.Size(231, 636);
            this.pnlControls.TabIndex = 0;
            // 
            // nudIterations
            // 
            this.nudIterations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudIterations.Location = new System.Drawing.Point(6, 6);
            this.nudIterations.Margin = new System.Windows.Forms.Padding(6, 6, 3, 3);
            this.nudIterations.Name = "nudIterations";
            this.nudIterations.Size = new System.Drawing.Size(118, 23);
            this.nudIterations.TabIndex = 9;
            // 
            // lblIterations
            // 
            this.lblIterations.AutoSize = true;
            this.lblIterations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIterations.Location = new System.Drawing.Point(130, 0);
            this.lblIterations.Name = "lblIterations";
            this.lblIterations.Size = new System.Drawing.Size(98, 32);
            this.lblIterations.TabIndex = 10;
            this.lblIterations.Text = "Итерации";
            this.lblIterations.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudThreshold
            // 
            this.nudThreshold.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudThreshold.Location = new System.Drawing.Point(6, 35);
            this.nudThreshold.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.nudThreshold.Name = "nudThreshold";
            this.nudThreshold.Size = new System.Drawing.Size(118, 23);
            this.nudThreshold.TabIndex = 11;
            // 
            // lblThreshold
            // 
            this.lblThreshold.AutoSize = true;
            this.lblThreshold.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblThreshold.Location = new System.Drawing.Point(130, 32);
            this.lblThreshold.Name = "lblThreshold";
            this.lblThreshold.Size = new System.Drawing.Size(98, 29);
            this.lblThreshold.TabIndex = 12;
            this.lblThreshold.Text = "Порог выхода";
            this.lblThreshold.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblZoom
            // 
            this.lblZoom.AutoSize = true;
            this.pnlControls.SetColumnSpan(this.lblZoom, 2);
            this.lblZoom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblZoom.Location = new System.Drawing.Point(3, 61);
            this.lblZoom.Name = "lblZoom";
            this.lblZoom.Size = new System.Drawing.Size(225, 15);
            this.lblZoom.TabIndex = 13;
            this.lblZoom.Text = "Приближение";
            this.lblZoom.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // nudZoom
            // 
            this.pnlControls.SetColumnSpan(this.nudZoom, 2);
            this.nudZoom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudZoom.Location = new System.Drawing.Point(6, 79);
            this.nudZoom.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.nudZoom.Name = "nudZoom";
            this.nudZoom.Size = new System.Drawing.Size(219, 23);
            this.nudZoom.TabIndex = 14;
            // 
            // cbThreads
            // 
            this.cbThreads.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbThreads.FormattingEnabled = true;
            this.cbThreads.Location = new System.Drawing.Point(6, 108);
            this.cbThreads.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.cbThreads.Name = "cbThreads";
            this.cbThreads.Size = new System.Drawing.Size(118, 23);
            this.cbThreads.TabIndex = 15;
            // 
            // lblThreads
            // 
            this.lblThreads.AutoSize = true;
            this.lblThreads.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblThreads.Location = new System.Drawing.Point(130, 105);
            this.lblThreads.Name = "lblThreads";
            this.lblThreads.Size = new System.Drawing.Size(98, 29);
            this.lblThreads.TabIndex = 16;
            this.lblThreads.Text = "Потоки ЦП";
            this.lblThreads.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbSSAA
            // 
            this.cbSSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbSSAA.FormattingEnabled = true;
            this.cbSSAA.Location = new System.Drawing.Point(6, 137);
            this.cbSSAA.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.cbSSAA.Name = "cbSSAA";
            this.cbSSAA.Size = new System.Drawing.Size(118, 23);
            this.cbSSAA.TabIndex = 17;
            // 
            // lbSSAA
            // 
            this.lbSSAA.AutoSize = true;
            this.lbSSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbSSAA.Location = new System.Drawing.Point(130, 134);
            this.lbSSAA.Name = "lbSSAA";
            this.lbSSAA.Size = new System.Drawing.Size(98, 29);
            this.lbSSAA.TabIndex = 18;
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
            this.cbSmooth.Location = new System.Drawing.Point(6, 166);
            this.cbSmooth.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.cbSmooth.Name = "cbSmooth";
            this.cbSmooth.Size = new System.Drawing.Size(222, 19);
            this.cbSmooth.TabIndex = 19;
            this.cbSmooth.Text = "Плавное окрашивание";
            this.cbSmooth.UseVisualStyleBackColor = true;
            // 
            // lblVariation
            // 
            this.lblVariation.AutoSize = true;
            this.lblVariation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVariation.Location = new System.Drawing.Point(130, 188);
            this.lblVariation.Name = "lblVariation";
            this.lblVariation.Size = new System.Drawing.Size(98, 29);
            this.lblVariation.TabIndex = 21;
            this.lblVariation.Text = "Вариация";
            this.lblVariation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbVariation
            // 
            this.cbVariation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbVariation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbVariation.FormattingEnabled = true;
            this.cbVariation.Location = new System.Drawing.Point(6, 191);
            this.cbVariation.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.cbVariation.Name = "cbVariation";
            this.cbVariation.Size = new System.Drawing.Size(118, 23);
            this.cbVariation.TabIndex = 20;
            // 
            // lblPParameter
            // 
            this.lblPParameter.AutoSize = true;
            this.lblPParameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPParameter.Location = new System.Drawing.Point(130, 217);
            this.lblPParameter.Name = "lblPParameter";
            this.lblPParameter.Size = new System.Drawing.Size(98, 29);
            this.lblPParameter.TabIndex = 23;
            this.lblPParameter.Text = "Параметр P";
            this.lblPParameter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudPParameter
            // 
            this.nudPParameter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudPParameter.Location = new System.Drawing.Point(6, 220);
            this.nudPParameter.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.nudPParameter.Name = "nudPParameter";
            this.nudPParameter.Size = new System.Drawing.Size(118, 23);
            this.nudPParameter.TabIndex = 22;
            // 
            // btnSaveHighRes
            // 
            this.pnlControls.SetColumnSpan(this.btnSaveHighRes, 2);
            this.btnSaveHighRes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveHighRes.Location = new System.Drawing.Point(6, 249);
            this.btnSaveHighRes.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.btnSaveHighRes.Name = "btnSaveHighRes";
            this.btnSaveHighRes.Size = new System.Drawing.Size(219, 39);
            this.btnSaveHighRes.TabIndex = 24;
            this.btnSaveHighRes.Text = "Сохранить изображение";
            this.btnSaveHighRes.UseVisualStyleBackColor = true;
            this.btnSaveHighRes.Click += new System.EventHandler(this.btnOpenSaveManager_Click);
            // 
            // color_configurations
            // 
            this.pnlControls.SetColumnSpan(this.color_configurations, 2);
            this.color_configurations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.color_configurations.Location = new System.Drawing.Point(6, 294);
            this.color_configurations.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.color_configurations.Name = "color_configurations";
            this.color_configurations.Size = new System.Drawing.Size(219, 39);
            this.color_configurations.TabIndex = 25;
            this.color_configurations.Text = "Настроить палитру";
            this.color_configurations.UseVisualStyleBackColor = true;
            this.color_configurations.Click += new System.EventHandler(this.color_configurations_Click);
            // 
            // btnRender
            // 
            this.pnlControls.SetColumnSpan(this.btnRender, 2);
            this.btnRender.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRender.Location = new System.Drawing.Point(6, 339);
            this.btnRender.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.btnRender.Name = "btnRender";
            this.btnRender.Size = new System.Drawing.Size(219, 39);
            this.btnRender.TabIndex = 26;
            this.btnRender.Text = "Запустить рендер";
            this.btnRender.UseVisualStyleBackColor = true;
            this.btnRender.Click += new System.EventHandler(this.btnRender_Click);
            // 
            // btnStateManager
            // 
            this.pnlControls.SetColumnSpan(this.btnStateManager, 2);
            this.btnStateManager.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStateManager.Location = new System.Drawing.Point(6, 384);
            this.btnStateManager.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.btnStateManager.Name = "btnStateManager";
            this.btnStateManager.Size = new System.Drawing.Size(219, 39);
            this.btnStateManager.TabIndex = 27;
            this.btnStateManager.Text = "Менеджер сохранений";
            this.btnStateManager.UseVisualStyleBackColor = true;
            this.btnStateManager.Click += new System.EventHandler(this.btnStateManager_Click);
            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.pnlControls.SetColumnSpan(this.lblProgress, 2);
            this.lblProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProgress.Location = new System.Drawing.Point(3, 426);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(225, 20);
            this.lblProgress.TabIndex = 28;
            this.lblProgress.Text = "Обработка";
            this.lblProgress.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // pbRenderProgress
            // 
            this.pnlControls.SetColumnSpan(this.pbRenderProgress, 2);
            this.pbRenderProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbRenderProgress.Location = new System.Drawing.Point(6, 449);
            this.pbRenderProgress.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.pbRenderProgress.Name = "pbRenderProgress";
            this.pbRenderProgress.Size = new System.Drawing.Size(219, 24);
            this.pbRenderProgress.TabIndex = 29;
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
            // FractalCollatzForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 636);
            this.Controls.Add(this.canvas);
            this.Controls.Add(this.pnlControls);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1100, 675);
            this.Name = "FractalCollatzForm";
            this.Text = "Фрактал Коллатца";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FractalCollatzForm_FormClosed);
            this.Load += new System.EventHandler(this.FractalCollatzForm_Load);
            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPParameter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).EndInit();
            this.ResumeLayout(false);

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