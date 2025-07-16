namespace FractalExplorer
{
    partial class NewtonPools
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewtonPools));
            pnlControls = new TableLayoutPanel();
            lblFormula = new Label();
            cbSelector = new ComboBox();
            richTextInput = new RichTextBox();
            nudIterations = new NumericUpDown();
            lblIterations = new Label();
            nudZoom = new NumericUpDown();
            lblZoom = new Label();
            cbThreads = new ComboBox();
            lblThreads = new Label();
            btnSave = new Button();
            btnConfigurePalette = new Button();
            btnRender = new Button();
            btnStateManager = new Button();
            lblProgress = new Label();
            progressBar = new ProgressBar();
            lblDebug = new Label();
            richTextDebugOutput = new RichTextBox();
            fractal_bitmap = new PictureBox();
            pnlControls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fractal_bitmap).BeginInit();
            SuspendLayout();
            // 
            // pnlControls
            // 
            pnlControls.ColumnCount = 2;
            pnlControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            pnlControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            pnlControls.Controls.Add(lblFormula, 0, 0);
            pnlControls.Controls.Add(cbSelector, 0, 1);
            pnlControls.Controls.Add(richTextInput, 0, 2);
            pnlControls.Controls.Add(nudIterations, 0, 3);
            pnlControls.Controls.Add(lblIterations, 1, 3);
            pnlControls.Controls.Add(nudZoom, 0, 4);
            pnlControls.Controls.Add(lblZoom, 1, 4);
            pnlControls.Controls.Add(cbThreads, 0, 5);
            pnlControls.Controls.Add(lblThreads, 1, 5);
            pnlControls.Controls.Add(btnSave, 0, 6);
            pnlControls.Controls.Add(btnConfigurePalette, 0, 7);
            pnlControls.Controls.Add(btnRender, 0, 8);
            pnlControls.Controls.Add(btnStateManager, 0, 9);
            pnlControls.Controls.Add(lblProgress, 0, 10);
            pnlControls.Controls.Add(progressBar, 0, 11);
            pnlControls.Controls.Add(lblDebug, 0, 12);
            pnlControls.Controls.Add(richTextDebugOutput, 0, 13);
            pnlControls.Dock = DockStyle.Left;
            pnlControls.Location = new Point(0, 0);
            pnlControls.Name = "pnlControls";
            pnlControls.RowCount = 15;
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 115F));
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            pnlControls.Size = new Size(231, 636);
            pnlControls.TabIndex = 0;
            // 
            // lblFormula
            // 
            lblFormula.AutoSize = true;
            pnlControls.SetColumnSpan(lblFormula, 2);
            lblFormula.Dock = DockStyle.Fill;
            lblFormula.Location = new Point(3, 0);
            lblFormula.Name = "lblFormula";
            lblFormula.Size = new Size(225, 25);
            lblFormula.TabIndex = 0;
            lblFormula.Text = "Выбери полином/формулу";
            lblFormula.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // cbSelector
            // 
            pnlControls.SetColumnSpan(cbSelector, 2);
            cbSelector.Dock = DockStyle.Fill;
            cbSelector.FormattingEnabled = true;
            cbSelector.Location = new Point(6, 28);
            cbSelector.Margin = new Padding(6, 3, 6, 3);
            cbSelector.Name = "cbSelector";
            cbSelector.Size = new Size(219, 23);
            cbSelector.TabIndex = 1;
            // 
            // richTextInput
            // 
            pnlControls.SetColumnSpan(richTextInput, 2);
            richTextInput.Dock = DockStyle.Fill;
            richTextInput.Location = new Point(6, 57);
            richTextInput.Margin = new Padding(6, 3, 6, 3);
            richTextInput.Name = "richTextInput";
            richTextInput.Size = new Size(219, 109);
            richTextInput.TabIndex = 2;
            richTextInput.Text = "z^3 - 1";
            // 
            // nudIterations
            // 
            nudIterations.Dock = DockStyle.Fill;
            nudIterations.Location = new Point(6, 172);
            nudIterations.Margin = new Padding(6, 3, 3, 3);
            nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudIterations.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new Size(118, 23);
            nudIterations.TabIndex = 3;
            nudIterations.Value = new decimal(new int[] { 500, 0, 0, 0 });
            // 
            // lblIterations
            // 
            lblIterations.AutoSize = true;
            lblIterations.Dock = DockStyle.Fill;
            lblIterations.Location = new Point(130, 169);
            lblIterations.Name = "lblIterations";
            lblIterations.Size = new Size(98, 29);
            lblIterations.TabIndex = 4;
            lblIterations.Text = "Итерации";
            lblIterations.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // nudZoom
            // 
            nudZoom.DecimalPlaces = 2;
            nudZoom.Dock = DockStyle.Fill;
            nudZoom.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            nudZoom.Location = new Point(6, 201);
            nudZoom.Margin = new Padding(6, 3, 3, 3);
            nudZoom.Maximum = new decimal(new int[] { 268435455, 1042612833, 542101086, 0 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new Size(118, 23);
            nudZoom.TabIndex = 5;
            nudZoom.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // lblZoom
            // 
            lblZoom.AutoSize = true;
            lblZoom.Dock = DockStyle.Fill;
            lblZoom.Location = new Point(130, 198);
            lblZoom.Name = "lblZoom";
            lblZoom.Size = new Size(98, 29);
            lblZoom.TabIndex = 6;
            lblZoom.Text = "Приближение";
            lblZoom.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cbThreads
            // 
            cbThreads.Dock = DockStyle.Fill;
            cbThreads.FormattingEnabled = true;
            cbThreads.Location = new Point(6, 230);
            cbThreads.Margin = new Padding(6, 3, 3, 3);
            cbThreads.Name = "cbThreads";
            cbThreads.Size = new Size(118, 23);
            cbThreads.TabIndex = 7;
            // 
            // lblThreads
            // 
            lblThreads.AutoSize = true;
            lblThreads.Dock = DockStyle.Fill;
            lblThreads.Location = new Point(130, 227);
            lblThreads.Name = "lblThreads";
            lblThreads.Size = new Size(98, 29);
            lblThreads.TabIndex = 8;
            lblThreads.Text = "Потоки ЦП";
            lblThreads.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnSave
            // 
            pnlControls.SetColumnSpan(btnSave, 2);
            btnSave.Dock = DockStyle.Fill;
            btnSave.Location = new Point(6, 259);
            btnSave.Margin = new Padding(6, 3, 6, 3);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(219, 39);
            btnSave.TabIndex = 9;
            btnSave.Text = "Сохранить изображение";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnOpenSaveManager_Click;
            // 
            // btnConfigurePalette
            // 
            pnlControls.SetColumnSpan(btnConfigurePalette, 2);
            btnConfigurePalette.Dock = DockStyle.Fill;
            btnConfigurePalette.Location = new Point(6, 304);
            btnConfigurePalette.Margin = new Padding(6, 3, 6, 3);
            btnConfigurePalette.Name = "btnConfigurePalette";
            btnConfigurePalette.Size = new Size(219, 39);
            btnConfigurePalette.TabIndex = 10;
            btnConfigurePalette.Text = "Настроить палитру";
            btnConfigurePalette.UseVisualStyleBackColor = true;
            // 
            // btnRender
            // 
            pnlControls.SetColumnSpan(btnRender, 2);
            btnRender.Dock = DockStyle.Fill;
            btnRender.Location = new Point(6, 349);
            btnRender.Margin = new Padding(6, 3, 6, 3);
            btnRender.Name = "btnRender";
            btnRender.Size = new Size(219, 39);
            btnRender.TabIndex = 11;
            btnRender.Text = "Запустить рендер";
            btnRender.UseVisualStyleBackColor = true;
            // 
            // btnStateManager
            // 
            pnlControls.SetColumnSpan(btnStateManager, 2);
            btnStateManager.Dock = DockStyle.Fill;
            btnStateManager.Location = new Point(6, 394);
            btnStateManager.Margin = new Padding(6, 3, 6, 3);
            btnStateManager.Name = "btnStateManager";
            btnStateManager.Size = new Size(219, 39);
            btnStateManager.TabIndex = 12;
            btnStateManager.Text = "Менеджер сохранений";
            btnStateManager.UseVisualStyleBackColor = true;
            btnStateManager.Click += btnStateManager_Click;
            // 
            // lblProgress
            // 
            lblProgress.AutoSize = true;
            pnlControls.SetColumnSpan(lblProgress, 2);
            lblProgress.Dock = DockStyle.Fill;
            lblProgress.Location = new Point(3, 436);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(225, 20);
            lblProgress.TabIndex = 13;
            lblProgress.Text = "Обработка";
            lblProgress.TextAlign = ContentAlignment.BottomCenter;
            // 
            // progressBar
            // 
            pnlControls.SetColumnSpan(progressBar, 2);
            progressBar.Dock = DockStyle.Fill;
            progressBar.Location = new Point(6, 459);
            progressBar.Margin = new Padding(6, 3, 6, 3);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(219, 24);
            progressBar.TabIndex = 14;
            // 
            // lblDebug
            // 
            lblDebug.AutoSize = true;
            pnlControls.SetColumnSpan(lblDebug, 2);
            lblDebug.Dock = DockStyle.Fill;
            lblDebug.Location = new Point(3, 486);
            lblDebug.Name = "lblDebug";
            lblDebug.Size = new Size(225, 20);
            lblDebug.TabIndex = 15;
            lblDebug.Text = "Отладка";
            lblDebug.TextAlign = ContentAlignment.BottomCenter;
            // 
            // richTextDebugOutput
            // 
            pnlControls.SetColumnSpan(richTextDebugOutput, 2);
            richTextDebugOutput.Dock = DockStyle.Fill;
            richTextDebugOutput.Location = new Point(6, 509);
            richTextDebugOutput.Margin = new Padding(6, 3, 6, 3);
            richTextDebugOutput.Name = "richTextDebugOutput";
            richTextDebugOutput.ReadOnly = true;
            richTextDebugOutput.Size = new Size(219, 104);
            richTextDebugOutput.TabIndex = 16;
            richTextDebugOutput.Text = "";
            // 
            // fractal_bitmap
            // 
            fractal_bitmap.Dock = DockStyle.Fill;
            fractal_bitmap.Location = new Point(231, 0);
            fractal_bitmap.Name = "fractal_bitmap";
            fractal_bitmap.Size = new Size(853, 636);
            fractal_bitmap.TabIndex = 1;
            fractal_bitmap.TabStop = false;
            // 
            // NewtonPools
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 636);
            Controls.Add(fractal_bitmap);
            Controls.Add(pnlControls);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1100, 675);
            Name = "NewtonPools";
            Text = "Бассейны Ньютона";
            pnlControls.ResumeLayout(false);
            pnlControls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)fractal_bitmap).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel pnlControls;
        private System.Windows.Forms.Label lblFormula;
        private System.Windows.Forms.ComboBox cbSelector;
        private System.Windows.Forms.RichTextBox richTextInput;
        private System.Windows.Forms.NumericUpDown nudIterations;
        private System.Windows.Forms.Label lblIterations;
        private System.Windows.Forms.NumericUpDown nudZoom;
        private System.Windows.Forms.Label lblZoom;
        private System.Windows.Forms.ComboBox cbThreads;
        private System.Windows.Forms.Label lblThreads;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnConfigurePalette;
        private System.Windows.Forms.Button btnRender;
        private System.Windows.Forms.Button btnStateManager;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblDebug;
        private System.Windows.Forms.RichTextBox richTextDebugOutput;
        private System.Windows.Forms.PictureBox fractal_bitmap;
    }
}