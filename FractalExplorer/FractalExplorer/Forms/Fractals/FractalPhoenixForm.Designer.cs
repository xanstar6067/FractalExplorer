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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FractalPhoenixForm));
            pnlControls = new System.Windows.Forms.TableLayoutPanel();
            nudC1Re = new System.Windows.Forms.NumericUpDown();
            lblC1Re = new System.Windows.Forms.Label();
            nudC1Im = new System.Windows.Forms.NumericUpDown();
            lblC1Im = new System.Windows.Forms.Label();
            nudC2Re = new System.Windows.Forms.NumericUpDown();
            lblC2Re = new System.Windows.Forms.Label();
            nudC2Im = new System.Windows.Forms.NumericUpDown();
            lblC2Im = new System.Windows.Forms.Label();
            btnSelectPhoenixParameters = new System.Windows.Forms.Button();
            nudIterations = new System.Windows.Forms.NumericUpDown();
            lblIterations = new System.Windows.Forms.Label();
            nudThreshold = new System.Windows.Forms.NumericUpDown();
            lblThreshold = new System.Windows.Forms.Label();
            lblZoom = new System.Windows.Forms.Label();
            nudZoom = new System.Windows.Forms.NumericUpDown();
            cbThreads = new System.Windows.Forms.ComboBox();
            lblThreads = new System.Windows.Forms.Label();
            cbSSAA = new System.Windows.Forms.ComboBox();
            lbSSAA = new System.Windows.Forms.Label();
            cbSmooth = new System.Windows.Forms.CheckBox();
            btnSaveHighRes = new System.Windows.Forms.Button();
            color_configurations = new System.Windows.Forms.Button();
            btnRender = new System.Windows.Forms.Button();
            btnStateManager = new System.Windows.Forms.Button();
            lblProgress = new System.Windows.Forms.Label();
            pbRenderProgress = new System.Windows.Forms.ProgressBar();
            canvas = new System.Windows.Forms.PictureBox();
            pnlControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(nudC1Re)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudC1Im)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudC2Re)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudC2Im)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(canvas)).BeginInit();
            SuspendLayout();
            // 
            // pnlControls
            // 
            pnlControls.ColumnCount = 2;
            pnlControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            pnlControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            pnlControls.Controls.Add(nudC1Re, 0, 0);
            pnlControls.Controls.Add(lblC1Re, 1, 0);
            pnlControls.Controls.Add(nudC1Im, 0, 1);
            pnlControls.Controls.Add(lblC1Im, 1, 1);
            pnlControls.Controls.Add(nudC2Re, 0, 2);
            pnlControls.Controls.Add(lblC2Re, 1, 2);
            pnlControls.Controls.Add(nudC2Im, 0, 3);
            pnlControls.Controls.Add(lblC2Im, 1, 3);
            pnlControls.Controls.Add(btnSelectPhoenixParameters, 0, 4);
            pnlControls.Controls.Add(nudIterations, 0, 5);
            pnlControls.Controls.Add(lblIterations, 1, 5);
            pnlControls.Controls.Add(nudThreshold, 0, 6);
            pnlControls.Controls.Add(lblThreshold, 1, 6);
            pnlControls.Controls.Add(lblZoom, 0, 7);
            pnlControls.Controls.Add(nudZoom, 0, 8);
            pnlControls.Controls.Add(cbThreads, 0, 9);
            pnlControls.Controls.Add(lblThreads, 1, 9);
            pnlControls.Controls.Add(cbSSAA, 0, 10);
            pnlControls.Controls.Add(lbSSAA, 1, 10);
            pnlControls.Controls.Add(cbSmooth, 0, 11);
            pnlControls.Controls.Add(btnSaveHighRes, 0, 12);
            pnlControls.Controls.Add(color_configurations, 0, 13);
            pnlControls.Controls.Add(btnRender, 0, 14);
            pnlControls.Controls.Add(btnStateManager, 0, 15);
            pnlControls.Controls.Add(lblProgress, 0, 16);
            pnlControls.Controls.Add(pbRenderProgress, 0, 17);
            pnlControls.Dock = System.Windows.Forms.DockStyle.Left;
            pnlControls.Location = new System.Drawing.Point(0, 0);
            pnlControls.Name = "pnlControls";
            pnlControls.RowCount = 19;
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            pnlControls.Size = new System.Drawing.Size(231, 636);
            pnlControls.TabIndex = 0;
            // 
            // nudC1Re
            // 
            nudC1Re.DecimalPlaces = 15;
            nudC1Re.Dock = System.Windows.Forms.DockStyle.Fill;
            nudC1Re.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudC1Re.Location = new System.Drawing.Point(6, 6);
            nudC1Re.Margin = new System.Windows.Forms.Padding(6, 6, 3, 3);
            nudC1Re.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudC1Re.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudC1Re.Name = "nudC1Re";
            nudC1Re.Size = new System.Drawing.Size(118, 23);
            nudC1Re.TabIndex = 0;
            nudC1Re.Value = new decimal(new int[] { 56, 0, 0, 131072 });
            // 
            // lblC1Re
            // 
            lblC1Re.AutoSize = true;
            lblC1Re.Dock = System.Windows.Forms.DockStyle.Fill;
            lblC1Re.Location = new System.Drawing.Point(130, 0);
            lblC1Re.Name = "lblC1Re";
            lblC1Re.Size = new System.Drawing.Size(98, 32);
            lblC1Re.TabIndex = 1;
            lblC1Re.Text = "C1 Re (P)";
            lblC1Re.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudC1Im
            // 
            nudC1Im.DecimalPlaces = 15;
            nudC1Im.Dock = System.Windows.Forms.DockStyle.Fill;
            nudC1Im.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudC1Im.Location = new System.Drawing.Point(6, 35);
            nudC1Im.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            nudC1Im.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudC1Im.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudC1Im.Name = "nudC1Im";
            nudC1Im.Size = new System.Drawing.Size(118, 23);
            nudC1Im.TabIndex = 2;
            nudC1Im.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // lblC1Im
            // 
            lblC1Im.AutoSize = true;
            lblC1Im.Dock = System.Windows.Forms.DockStyle.Fill;
            lblC1Im.Location = new System.Drawing.Point(130, 32);
            lblC1Im.Name = "lblC1Im";
            lblC1Im.Size = new System.Drawing.Size(98, 29);
            lblC1Im.TabIndex = 3;
            lblC1Im.Text = "C1 Im (Q)";
            lblC1Im.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudC2Re
            // 
            nudC2Re.DecimalPlaces = 15;
            nudC2Re.Dock = System.Windows.Forms.DockStyle.Fill;
            nudC2Re.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudC2Re.Location = new System.Drawing.Point(6, 64);
            nudC2Re.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            nudC2Re.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudC2Re.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudC2Re.Name = "nudC2Re";
            nudC2Re.Size = new System.Drawing.Size(118, 23);
            nudC2Re.TabIndex = 4;
            // 
            // lblC2Re
            // 
            lblC2Re.AutoSize = true;
            lblC2Re.Dock = System.Windows.Forms.DockStyle.Fill;
            lblC2Re.Location = new System.Drawing.Point(130, 61);
            lblC2Re.Name = "lblC2Re";
            lblC2Re.Size = new System.Drawing.Size(98, 29);
            lblC2Re.TabIndex = 5;
            lblC2Re.Text = "C2 Re";
            lblC2Re.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudC2Im
            // 
            nudC2Im.DecimalPlaces = 15;
            nudC2Im.Dock = System.Windows.Forms.DockStyle.Fill;
            nudC2Im.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudC2Im.Location = new System.Drawing.Point(6, 93);
            nudC2Im.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            nudC2Im.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudC2Im.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudC2Im.Name = "nudC2Im";
            nudC2Im.Size = new System.Drawing.Size(118, 23);
            nudC2Im.TabIndex = 6;
            // 
            // lblC2Im
            // 
            lblC2Im.AutoSize = true;
            lblC2Im.Dock = System.Windows.Forms.DockStyle.Fill;
            lblC2Im.Location = new System.Drawing.Point(130, 90);
            lblC2Im.Name = "lblC2Im";
            lblC2Im.Size = new System.Drawing.Size(98, 29);
            lblC2Im.TabIndex = 7;
            lblC2Im.Text = "C2 Im";
            lblC2Im.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnSelectPhoenixParameters
            // 
            pnlControls.SetColumnSpan(btnSelectPhoenixParameters, 2);
            btnSelectPhoenixParameters.Dock = System.Windows.Forms.DockStyle.Fill;
            btnSelectPhoenixParameters.Location = new System.Drawing.Point(6, 122);
            btnSelectPhoenixParameters.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            btnSelectPhoenixParameters.Name = "btnSelectPhoenixParameters";
            btnSelectPhoenixParameters.Size = new System.Drawing.Size(219, 39);
            btnSelectPhoenixParameters.TabIndex = 8;
            btnSelectPhoenixParameters.Text = "Выбрать C1/C2";
            btnSelectPhoenixParameters.UseVisualStyleBackColor = true;
            btnSelectPhoenixParameters.Click += new System.EventHandler(btnSelectPhoenixParameters_Click);
            // 
            // nudIterations
            // 
            nudIterations.Dock = System.Windows.Forms.DockStyle.Fill;
            nudIterations.Location = new System.Drawing.Point(6, 167);
            nudIterations.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudIterations.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new System.Drawing.Size(118, 23);
            nudIterations.TabIndex = 9;
            nudIterations.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // lblIterations
            // 
            lblIterations.AutoSize = true;
            lblIterations.Dock = System.Windows.Forms.DockStyle.Fill;
            lblIterations.Location = new System.Drawing.Point(130, 164);
            lblIterations.Name = "lblIterations";
            lblIterations.Size = new System.Drawing.Size(98, 29);
            lblIterations.TabIndex = 10;
            lblIterations.Text = "Итерации";
            lblIterations.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudThreshold
            // 
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Dock = System.Windows.Forms.DockStyle.Fill;
            nudThreshold.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            nudThreshold.Location = new System.Drawing.Point(6, 196);
            nudThreshold.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            nudThreshold.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudThreshold.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            nudThreshold.Name = "nudThreshold";
            nudThreshold.Size = new System.Drawing.Size(118, 23);
            nudThreshold.TabIndex = 11;
            nudThreshold.Value = new decimal(new int[] { 4, 0, 0, 0 });
            // 
            // lblThreshold
            // 
            lblThreshold.AutoSize = true;
            lblThreshold.Dock = System.Windows.Forms.DockStyle.Fill;
            lblThreshold.Location = new System.Drawing.Point(130, 193);
            lblThreshold.Name = "lblThreshold";
            lblThreshold.Size = new System.Drawing.Size(98, 29);
            lblThreshold.TabIndex = 12;
            lblThreshold.Text = "Порог выхода";
            lblThreshold.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblZoom
            // 
            lblZoom.AutoSize = true;
            pnlControls.SetColumnSpan(lblZoom, 2);
            lblZoom.Dock = System.Windows.Forms.DockStyle.Fill;
            lblZoom.Location = new System.Drawing.Point(3, 222);
            lblZoom.Name = "lblZoom";
            lblZoom.Size = new System.Drawing.Size(225, 29);
            lblZoom.TabIndex = 13;
            lblZoom.Text = "Приближение";
            lblZoom.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // nudZoom
            // 
            pnlControls.SetColumnSpan(nudZoom, 2);
            nudZoom.DecimalPlaces = 4;
            nudZoom.Dock = System.Windows.Forms.DockStyle.Fill;
            nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudZoom.Location = new System.Drawing.Point(6, 254);
            nudZoom.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            nudZoom.Maximum = new decimal(new int[] { -1, -1, -1, 0 });
            nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 393216 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new System.Drawing.Size(219, 23);
            nudZoom.TabIndex = 14;
            nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // cbThreads
            // 
            cbThreads.Dock = System.Windows.Forms.DockStyle.Fill;
            cbThreads.FormattingEnabled = true;
            cbThreads.Location = new System.Drawing.Point(6, 283);
            cbThreads.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            cbThreads.Name = "cbThreads";
            cbThreads.Size = new System.Drawing.Size(118, 23);
            cbThreads.TabIndex = 15;
            // 
            // lblThreads
            // 
            lblThreads.AutoSize = true;
            lblThreads.Dock = System.Windows.Forms.DockStyle.Fill;
            lblThreads.Location = new System.Drawing.Point(130, 280);
            lblThreads.Name = "lblThreads";
            lblThreads.Size = new System.Drawing.Size(98, 29);
            lblThreads.TabIndex = 16;
            lblThreads.Text = "Потоки ЦП";
            lblThreads.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbSSAA
            // 
            cbSSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            cbSSAA.FormattingEnabled = true;
            cbSSAA.Location = new System.Drawing.Point(6, 312);
            cbSSAA.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            cbSSAA.Name = "cbSSAA";
            cbSSAA.Size = new System.Drawing.Size(118, 23);
            cbSSAA.TabIndex = 17;
            // 
            // lbSSAA
            // 
            lbSSAA.AutoSize = true;
            lbSSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            lbSSAA.Location = new System.Drawing.Point(130, 309);
            lbSSAA.Name = "lbSSAA";
            lbSSAA.Size = new System.Drawing.Size(98, 29);
            lbSSAA.TabIndex = 18;
            lbSSAA.Text = "Сглаживание";
            lbSSAA.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbSmooth
            // 
            cbSmooth.AutoSize = true;
            cbSmooth.Checked = true;
            cbSmooth.CheckState = System.Windows.Forms.CheckState.Checked;
            pnlControls.SetColumnSpan(cbSmooth, 2);
            cbSmooth.Dock = System.Windows.Forms.DockStyle.Fill;
            cbSmooth.Location = new System.Drawing.Point(6, 341);
            cbSmooth.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            cbSmooth.Name = "cbSmooth";
            cbSmooth.Size = new System.Drawing.Size(222, 19);
            cbSmooth.TabIndex = 19;
            cbSmooth.Text = "Плавное окрашивание";
            cbSmooth.UseVisualStyleBackColor = true;
            // 
            // btnSaveHighRes
            // 
            pnlControls.SetColumnSpan(btnSaveHighRes, 2);
            btnSaveHighRes.Dock = System.Windows.Forms.DockStyle.Fill;
            btnSaveHighRes.Location = new System.Drawing.Point(6, 366);
            btnSaveHighRes.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            btnSaveHighRes.Name = "btnSaveHighRes";
            btnSaveHighRes.Size = new System.Drawing.Size(219, 39);
            btnSaveHighRes.TabIndex = 20;
            btnSaveHighRes.Text = "Сохранить изображение";
            btnSaveHighRes.UseVisualStyleBackColor = true;
            btnSaveHighRes.Click += new System.EventHandler(btnOpenSaveManager_Click);
            // 
            // color_configurations
            // 
            pnlControls.SetColumnSpan(color_configurations, 2);
            color_configurations.Dock = System.Windows.Forms.DockStyle.Fill;
            color_configurations.Location = new System.Drawing.Point(6, 411);
            color_configurations.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            color_configurations.Name = "color_configurations";
            color_configurations.Size = new System.Drawing.Size(219, 39);
            color_configurations.TabIndex = 21;
            color_configurations.Text = "Настроить палитру";
            color_configurations.UseVisualStyleBackColor = true;
            color_configurations.Click += new System.EventHandler(color_configurations_Click);
            // 
            // btnRender
            // 
            pnlControls.SetColumnSpan(btnRender, 2);
            btnRender.Dock = System.Windows.Forms.DockStyle.Fill;
            btnRender.Location = new System.Drawing.Point(6, 456);
            btnRender.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            btnRender.Name = "btnRender";
            btnRender.Size = new System.Drawing.Size(219, 39);
            btnRender.TabIndex = 22;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            btnRender.Click += new System.EventHandler(btnRender_Click);
            // 
            // btnStateManager
            // 
            pnlControls.SetColumnSpan(btnStateManager, 2);
            btnStateManager.Dock = System.Windows.Forms.DockStyle.Fill;
            btnStateManager.Location = new System.Drawing.Point(6, 501);
            btnStateManager.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            btnStateManager.Name = "btnStateManager";
            btnStateManager.Size = new System.Drawing.Size(219, 39);
            btnStateManager.TabIndex = 23;
            btnStateManager.Text = "Менеджер сохранений";
            btnStateManager.UseVisualStyleBackColor = true;
            btnStateManager.Click += new System.EventHandler(btnStateManager_Click);
            // 
            // lblProgress
            // 
            lblProgress.AutoSize = true;
            pnlControls.SetColumnSpan(lblProgress, 2);
            lblProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            lblProgress.Location = new System.Drawing.Point(3, 543);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new System.Drawing.Size(225, 20);
            lblProgress.TabIndex = 24;
            lblProgress.Text = "Обработка";
            lblProgress.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // pbRenderProgress
            // 
            pnlControls.SetColumnSpan(pbRenderProgress, 2);
            pbRenderProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            pbRenderProgress.Location = new System.Drawing.Point(6, 566);
            pbRenderProgress.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            pbRenderProgress.Name = "pbRenderProgress";
            pbRenderProgress.Size = new System.Drawing.Size(219, 24);
            pbRenderProgress.TabIndex = 25;
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
            // FractalPhoenixForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1084, 636);
            Controls.Add(canvas);
            Controls.Add(pnlControls);
            Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            MinimumSize = new System.Drawing.Size(1100, 675);
            Name = "FractalPhoenixForm";
            Text = "Фрактал Феникс";
            FormClosed += new System.Windows.Forms.FormClosedEventHandler(FractalPhoenixForm_FormClosed);
            Load += new System.EventHandler(FractalPhoenixForm_Load);
            pnlControls.ResumeLayout(false);
            pnlControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(nudC1Re)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudC1Im)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudC2Re)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudC2Im)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(canvas)).EndInit();
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
        private System.Windows.Forms.Button btnStateManager;
        private System.Windows.Forms.CheckBox cbSmooth;
        protected System.Windows.Forms.Label lbSSAA;
        private System.Windows.Forms.ComboBox cbSSAA;
    }
}