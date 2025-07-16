namespace FractalExplorer
{
    partial class FractalSerpinski
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FractalSerpinski));
            pnlControls = new System.Windows.Forms.TableLayoutPanel();
            lblFractalType = new System.Windows.Forms.Label();
            pnlFractalType = new System.Windows.Forms.FlowLayoutPanel();
            FractalTypeIsGeometry = new System.Windows.Forms.CheckBox();
            FractalTypeIsChaos = new System.Windows.Forms.CheckBox();
            nudIterations = new System.Windows.Forms.NumericUpDown();
            label3 = new System.Windows.Forms.Label();
            nudZoom = new System.Windows.Forms.NumericUpDown();
            label5 = new System.Windows.Forms.Label();
            cbCPUThreads = new System.Windows.Forms.ComboBox();
            label6 = new System.Windows.Forms.Label();
            btnSavePNG = new System.Windows.Forms.Button();
            color_configurations = new System.Windows.Forms.Button();
            btnStateManager = new System.Windows.Forms.Button();
            pnlRenderButtons = new System.Windows.Forms.TableLayoutPanel();
            btnRender = new System.Windows.Forms.Button();
            abortRender = new System.Windows.Forms.Button();
            label8 = new System.Windows.Forms.Label();
            progressBarSerpinsky = new System.Windows.Forms.ProgressBar();
            canvasSerpinsky = new System.Windows.Forms.PictureBox();
            pnlControls.SuspendLayout();
            pnlFractalType.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudZoom)).BeginInit();
            pnlRenderButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(canvasSerpinsky)).BeginInit();
            SuspendLayout();
            // 
            // pnlControls
            // 
            pnlControls.ColumnCount = 2;
            pnlControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            pnlControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            pnlControls.Controls.Add(lblFractalType, 0, 0);
            pnlControls.Controls.Add(pnlFractalType, 0, 1);
            pnlControls.Controls.Add(nudIterations, 0, 2);
            pnlControls.Controls.Add(label3, 1, 2);
            pnlControls.Controls.Add(nudZoom, 0, 3);
            pnlControls.Controls.Add(label5, 1, 3);
            pnlControls.Controls.Add(cbCPUThreads, 0, 4);
            pnlControls.Controls.Add(label6, 1, 4);
            pnlControls.Controls.Add(btnSavePNG, 0, 5);
            pnlControls.Controls.Add(color_configurations, 0, 6);
            pnlControls.Controls.Add(btnStateManager, 0, 7);
            pnlControls.Controls.Add(pnlRenderButtons, 0, 8);
            pnlControls.Controls.Add(label8, 0, 9);
            pnlControls.Controls.Add(progressBarSerpinsky, 0, 10);
            pnlControls.Dock = System.Windows.Forms.DockStyle.Left;
            pnlControls.Location = new System.Drawing.Point(0, 0);
            pnlControls.Name = "pnlControls";
            pnlControls.RowCount = 12;
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
            pnlControls.TabIndex = 1;
            // 
            // lblFractalType
            // 
            lblFractalType.AutoSize = true;
            pnlControls.SetColumnSpan(lblFractalType, 2);
            lblFractalType.Dock = System.Windows.Forms.DockStyle.Fill;
            lblFractalType.Location = new System.Drawing.Point(3, 0);
            lblFractalType.Name = "lblFractalType";
            lblFractalType.Size = new System.Drawing.Size(225, 15);
            lblFractalType.TabIndex = 0;
            lblFractalType.Text = "Тип фрактала";
            lblFractalType.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnlFractalType
            // 
            pnlControls.SetColumnSpan(pnlFractalType, 2);
            pnlFractalType.Controls.Add(FractalTypeIsGeometry);
            pnlFractalType.Controls.Add(FractalTypeIsChaos);
            pnlFractalType.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlFractalType.Location = new System.Drawing.Point(3, 18);
            pnlFractalType.Name = "pnlFractalType";
            pnlFractalType.Size = new System.Drawing.Size(225, 29);
            pnlFractalType.TabIndex = 1;
            pnlFractalType.WrapContents = false;
            // 
            // FractalTypeIsGeometry
            // 
            FractalTypeIsGeometry.AutoSize = true;
            FractalTypeIsGeometry.Location = new System.Drawing.Point(3, 3);
            FractalTypeIsGeometry.Name = "FractalTypeIsGeometry";
            FractalTypeIsGeometry.Size = new System.Drawing.Size(118, 19);
            FractalTypeIsGeometry.TabIndex = 2;
            FractalTypeIsGeometry.Text = "Геометрический";
            FractalTypeIsGeometry.UseVisualStyleBackColor = true;
            // 
            // FractalTypeIsChaos
            // 
            FractalTypeIsChaos.AutoSize = true;
            FractalTypeIsChaos.Location = new System.Drawing.Point(127, 3);
            FractalTypeIsChaos.Name = "FractalTypeIsChaos";
            FractalTypeIsChaos.Size = new System.Drawing.Size(81, 19);
            FractalTypeIsChaos.TabIndex = 3;
            FractalTypeIsChaos.Text = "Игра хаос";
            FractalTypeIsChaos.UseVisualStyleBackColor = true;
            // 
            // nudIterations
            // 
            nudIterations.Dock = System.Windows.Forms.DockStyle.Fill;
            nudIterations.Location = new System.Drawing.Point(6, 53);
            nudIterations.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            nudIterations.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new System.Drawing.Size(118, 23);
            nudIterations.TabIndex = 0;
            nudIterations.Value = new decimal(new int[] { 8, 0, 0, 0 });
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Dock = System.Windows.Forms.DockStyle.Fill;
            label3.Location = new System.Drawing.Point(130, 50);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(98, 29);
            label3.TabIndex = 6;
            label3.Text = "Итерации";
            label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudZoom
            // 
            nudZoom.DecimalPlaces = 2;
            nudZoom.Dock = System.Windows.Forms.DockStyle.Fill;
            nudZoom.Location = new System.Drawing.Point(6, 82);
            nudZoom.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            nudZoom.Maximum = new decimal(new int[] { 10000000, 0, 0, 0 });
            nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new System.Drawing.Size(118, 23);
            nudZoom.TabIndex = 1;
            nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Dock = System.Windows.Forms.DockStyle.Fill;
            label5.Location = new System.Drawing.Point(130, 79);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(98, 29);
            label5.TabIndex = 16;
            label5.Text = "Приближение";
            label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbCPUThreads
            // 
            cbCPUThreads.Dock = System.Windows.Forms.DockStyle.Fill;
            cbCPUThreads.FormattingEnabled = true;
            cbCPUThreads.Location = new System.Drawing.Point(6, 111);
            cbCPUThreads.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            cbCPUThreads.Name = "cbCPUThreads";
            cbCPUThreads.Size = new System.Drawing.Size(118, 23);
            cbCPUThreads.TabIndex = 4;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Dock = System.Windows.Forms.DockStyle.Fill;
            label6.Location = new System.Drawing.Point(130, 108);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(98, 29);
            label6.TabIndex = 13;
            label6.Text = "Потоки ЦП";
            label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnSavePNG
            // 
            pnlControls.SetColumnSpan(btnSavePNG, 2);
            btnSavePNG.Dock = System.Windows.Forms.DockStyle.Fill;
            btnSavePNG.Location = new System.Drawing.Point(6, 140);
            btnSavePNG.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            btnSavePNG.Name = "btnSavePNG";
            btnSavePNG.Size = new System.Drawing.Size(219, 39);
            btnSavePNG.TabIndex = 11;
            btnSavePNG.Text = "Сохранить изображение";
            btnSavePNG.UseVisualStyleBackColor = true;
            btnSavePNG.Click += new System.EventHandler(btnOpenSaveManager_Click);
            // 
            // color_configurations
            // 
            pnlControls.SetColumnSpan(color_configurations, 2);
            color_configurations.Dock = System.Windows.Forms.DockStyle.Fill;
            color_configurations.Location = new System.Drawing.Point(6, 185);
            color_configurations.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            color_configurations.Name = "color_configurations";
            color_configurations.Size = new System.Drawing.Size(219, 39);
            color_configurations.TabIndex = 15;
            color_configurations.Text = "Настроить палитру";
            color_configurations.UseVisualStyleBackColor = true;
            color_configurations.Click += new System.EventHandler(color_configurations_Click);
            // 
            // btnStateManager
            // 
            pnlControls.SetColumnSpan(btnStateManager, 2);
            btnStateManager.Dock = System.Windows.Forms.DockStyle.Fill;
            btnStateManager.Location = new System.Drawing.Point(6, 230);
            btnStateManager.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            btnStateManager.Name = "btnStateManager";
            btnStateManager.Size = new System.Drawing.Size(219, 39);
            btnStateManager.TabIndex = 14;
            btnStateManager.Text = "Менеджер сохранений";
            btnStateManager.UseVisualStyleBackColor = true;
            btnStateManager.Click += new System.EventHandler(btnStateManager_Click);
            // 
            // pnlRenderButtons
            // 
            pnlRenderButtons.ColumnCount = 2;
            pnlControls.SetColumnSpan(pnlRenderButtons, 2);
            pnlRenderButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            pnlRenderButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            pnlRenderButtons.Controls.Add(btnRender, 0, 0);
            pnlRenderButtons.Controls.Add(abortRender, 1, 0);
            pnlRenderButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlRenderButtons.Location = new System.Drawing.Point(3, 275);
            pnlRenderButtons.Name = "pnlRenderButtons";
            pnlRenderButtons.RowCount = 1;
            pnlRenderButtons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            pnlRenderButtons.Size = new System.Drawing.Size(225, 39);
            pnlRenderButtons.TabIndex = 17;
            // 
            // btnRender
            // 
            btnRender.Dock = System.Windows.Forms.DockStyle.Fill;
            btnRender.Location = new System.Drawing.Point(3, 3);
            btnRender.Name = "btnRender";
            btnRender.Size = new System.Drawing.Size(140, 33);
            btnRender.TabIndex = 5;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            btnRender.Click += new System.EventHandler(btnRender_Click);
            // 
            // abortRender
            // 
            abortRender.Dock = System.Windows.Forms.DockStyle.Fill;
            abortRender.Location = new System.Drawing.Point(149, 3);
            abortRender.Name = "abortRender";
            abortRender.Size = new System.Drawing.Size(73, 33);
            abortRender.TabIndex = 6;
            abortRender.Text = "Отмена";
            abortRender.UseVisualStyleBackColor = true;
            abortRender.Click += new System.EventHandler(abortRender_Click);
            // 
            // label8
            // 
            label8.AutoSize = true;
            pnlControls.SetColumnSpan(label8, 2);
            label8.Dock = System.Windows.Forms.DockStyle.Fill;
            label8.Location = new System.Drawing.Point(3, 317);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(225, 20);
            label8.TabIndex = 7;
            label8.Text = "Обработка";
            label8.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // progressBarSerpinsky
            // 
            pnlControls.SetColumnSpan(progressBarSerpinsky, 2);
            progressBarSerpinsky.Dock = System.Windows.Forms.DockStyle.Fill;
            progressBarSerpinsky.Location = new System.Drawing.Point(6, 340);
            progressBarSerpinsky.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            progressBarSerpinsky.Name = "progressBarSerpinsky";
            progressBarSerpinsky.Size = new System.Drawing.Size(219, 24);
            progressBarSerpinsky.TabIndex = 8;
            // 
            // canvasSerpinsky
            // 
            canvasSerpinsky.Dock = System.Windows.Forms.DockStyle.Fill;
            canvasSerpinsky.Location = new System.Drawing.Point(231, 0);
            canvasSerpinsky.Name = "canvasSerpinsky";
            canvasSerpinsky.Size = new System.Drawing.Size(853, 636);
            canvasSerpinsky.TabIndex = 2;
            canvasSerpinsky.TabStop = false;
            // 
            // FractalSerpinski
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1084, 636);
            Controls.Add(canvasSerpinsky);
            Controls.Add(pnlControls);
            Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            MinimumSize = new System.Drawing.Size(1100, 675);
            Name = "FractalSerpinski";
            Text = "Треугольник Серпинского";
            pnlControls.ResumeLayout(false);
            pnlControls.PerformLayout();
            pnlFractalType.ResumeLayout(false);
            pnlFractalType.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudZoom)).EndInit();
            pnlRenderButtons.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(canvasSerpinsky)).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel pnlControls;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown nudZoom;
        private System.Windows.Forms.Button btnRender;
        private System.Windows.Forms.ProgressBar progressBarSerpinsky;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbCPUThreads;
        private System.Windows.Forms.Button btnSavePNG;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nudIterations;
        private System.Windows.Forms.PictureBox canvasSerpinsky;
        private System.Windows.Forms.CheckBox FractalTypeIsChaos;
        private System.Windows.Forms.CheckBox FractalTypeIsGeometry;
        private System.Windows.Forms.Button abortRender;
        private System.Windows.Forms.Button color_configurations;
        private System.Windows.Forms.Button btnStateManager;
        private System.Windows.Forms.Label lblFractalType;
        private System.Windows.Forms.FlowLayoutPanel pnlFractalType;
        private System.Windows.Forms.TableLayoutPanel pnlRenderButtons;
    }
}