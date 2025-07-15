namespace FractalExplorer.Forms.Other
{
    partial class SaveImageManagerForm
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
            pnlMain = new Panel();
            grpEffects = new GroupBox();
            grpPostProcessing = new GroupBox();
            chkApplyBicubic = new CheckBox();
            lblSsaa = new Label();
            cbSSAA = new ComboBox();
            grpOutput = new GroupBox();
            btnPreset8K = new Button();
            btnPreset2K = new Button();
            btnPreset720p = new Button();
            btnRotate = new Button();
            lblJpgQualityValue = new Label();
            trackBarJpgQuality = new TrackBar();
            lblJpgQuality = new Label();
            btnPreset4K = new Button();
            btnPresetFHD = new Button();
            lblFormat = new Label();
            cbFormat = new ComboBox();
            lblX = new Label();
            nudHeight = new NumericUpDown();
            nudWidth = new NumericUpDown();
            lblResolution = new Label();
            pnlBottom = new Panel();
            lblStatus = new Label();
            progressBar = new ProgressBar();
            btnCancel = new Button();
            btnSave = new Button();
            pnlMain.SuspendLayout();
            grpEffects.SuspendLayout();
            grpPostProcessing.SuspendLayout();
            grpOutput.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarJpgQuality).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudHeight).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudWidth).BeginInit();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            // 
            // pnlMain
            // 
            pnlMain.Controls.Add(grpEffects);
            pnlMain.Controls.Add(grpOutput);
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.Location = new Point(0, 0);
            pnlMain.Name = "pnlMain";
            pnlMain.Padding = new Padding(10);
            pnlMain.Size = new Size(584, 411);
            pnlMain.TabIndex = 0;
            // 
            // grpEffects
            // 
            grpEffects.Controls.Add(grpPostProcessing);
            grpEffects.Controls.Add(lblSsaa);
            grpEffects.Controls.Add(cbSSAA);
            grpEffects.Location = new Point(13, 239);
            grpEffects.Name = "grpEffects";
            grpEffects.Size = new Size(558, 159);
            grpEffects.TabIndex = 1;
            grpEffects.TabStop = false;
            grpEffects.Text = "Эффекты и качество";
            // 
            // grpPostProcessing
            // 
            grpPostProcessing.Controls.Add(chkApplyBicubic);
            grpPostProcessing.Location = new Point(9, 81);
            grpPostProcessing.Name = "grpPostProcessing";
            grpPostProcessing.Size = new Size(543, 68);
            grpPostProcessing.TabIndex = 2;
            grpPostProcessing.TabStop = false;
            grpPostProcessing.Text = "Пост-обработка";
            // 
            // chkApplyBicubic
            // 
            chkApplyBicubic.AutoSize = true;
            chkApplyBicubic.Location = new Point(12, 25);
            chkApplyBicubic.Name = "chkApplyBicubic";
            chkApplyBicubic.Size = new Size(259, 19);
            chkApplyBicubic.TabIndex = 0;
            chkApplyBicubic.Text = "Применить бикубическую интерполяцию";
            chkApplyBicubic.UseVisualStyleBackColor = true;
            // 
            // lblSsaa
            // 
            lblSsaa.AutoSize = true;
            lblSsaa.Location = new Point(6, 28);
            lblSsaa.Name = "lblSsaa";
            lblSsaa.Size = new Size(123, 15);
            lblSsaa.TabIndex = 1;
            lblSsaa.Text = "Сглаживание (SSAA):";
            // 
            // cbSSAA
            // 
            cbSSAA.DropDownStyle = ComboBoxStyle.DropDownList;
            cbSSAA.FormattingEnabled = true;
            cbSSAA.Items.AddRange(new object[] { "Выкл (1x)", "Низкое (2x)", "Высокое (4x)", "Ультра (8x)", "Экстрим (10x)" });
            cbSSAA.Location = new Point(9, 46);
            cbSSAA.Name = "cbSSAA";
            cbSSAA.Size = new Size(249, 23);
            cbSSAA.TabIndex = 0;
            // 
            // grpOutput
            // 
            grpOutput.Controls.Add(btnPreset8K);
            grpOutput.Controls.Add(btnPreset2K);
            grpOutput.Controls.Add(btnPreset720p);
            grpOutput.Controls.Add(btnRotate);
            grpOutput.Controls.Add(lblJpgQualityValue);
            grpOutput.Controls.Add(trackBarJpgQuality);
            grpOutput.Controls.Add(lblJpgQuality);
            grpOutput.Controls.Add(btnPreset4K);
            grpOutput.Controls.Add(btnPresetFHD);
            grpOutput.Controls.Add(lblFormat);
            grpOutput.Controls.Add(cbFormat);
            grpOutput.Controls.Add(lblX);
            grpOutput.Controls.Add(nudHeight);
            grpOutput.Controls.Add(nudWidth);
            grpOutput.Controls.Add(lblResolution);
            grpOutput.Location = new Point(13, 13);
            grpOutput.Name = "grpOutput";
            grpOutput.Size = new Size(558, 220);
            grpOutput.TabIndex = 0;
            grpOutput.TabStop = false;
            grpOutput.Text = "Параметры вывода";
            // 
            // btnPreset8K
            // 
            btnPreset8K.Location = new Point(434, 82);
            btnPreset8K.Name = "btnPreset8K";
            btnPreset8K.Size = new Size(118, 23);
            btnPreset8K.TabIndex = 14;
            btnPreset8K.Text = "8K (7680x4320)";
            btnPreset8K.UseVisualStyleBackColor = true;
            btnPreset8K.Click += btnPreset8K_Click;
            // 
            // btnPreset2K
            // 
            btnPreset2K.Location = new Point(298, 82);
            btnPreset2K.Name = "btnPreset2K";
            btnPreset2K.Size = new Size(130, 23);
            btnPreset2K.TabIndex = 13;
            btnPreset2K.Text = "2K (2560x1440)";
            btnPreset2K.UseVisualStyleBackColor = true;
            btnPreset2K.Click += btnPreset2K_Click;
            // 
            // btnPreset720p
            // 
            btnPreset720p.Location = new Point(298, 20);
            btnPreset720p.Name = "btnPreset720p";
            btnPreset720p.Size = new Size(130, 23);
            btnPreset720p.TabIndex = 12;
            btnPreset720p.Text = "HD (1280x720)";
            btnPreset720p.UseVisualStyleBackColor = true;
            btnPreset720p.Click += btnPreset720p_Click;
            // 
            // btnRotate
            // 
            btnRotate.Location = new Point(219, 51);
            btnRotate.Name = "btnRotate";
            btnRotate.Size = new Size(70, 23);
            btnRotate.TabIndex = 11;
            btnRotate.Text = "Вращать";
            btnRotate.UseVisualStyleBackColor = true;
            btnRotate.Click += btnRotate_Click;
            // 
            // lblJpgQualityValue
            // 
            lblJpgQualityValue.AutoSize = true;
            lblJpgQualityValue.Location = new Point(461, 172);
            lblJpgQualityValue.Name = "lblJpgQualityValue";
            lblJpgQualityValue.Size = new Size(29, 15);
            lblJpgQualityValue.TabIndex = 10;
            lblJpgQualityValue.Text = "95%";
            // 
            // trackBarJpgQuality
            // 
            trackBarJpgQuality.Location = new Point(171, 167);
            trackBarJpgQuality.Maximum = 100;
            trackBarJpgQuality.Minimum = 1;
            trackBarJpgQuality.Name = "trackBarJpgQuality";
            trackBarJpgQuality.Size = new Size(284, 45);
            trackBarJpgQuality.TabIndex = 9;
            trackBarJpgQuality.TickFrequency = 5;
            trackBarJpgQuality.Value = 95;
            trackBarJpgQuality.Scroll += trackBarJpgQuality_Scroll;
            // 
            // lblJpgQuality
            // 
            lblJpgQuality.AutoSize = true;
            lblJpgQuality.Location = new Point(6, 172);
            lblJpgQuality.Name = "lblJpgQuality";
            lblJpgQuality.Size = new Size(82, 15);
            lblJpgQuality.TabIndex = 8;
            lblJpgQuality.Text = "Качество JPG:";
            // 
            // btnPreset4K
            // 
            btnPreset4K.Location = new Point(434, 51);
            btnPreset4K.Name = "btnPreset4K";
            btnPreset4K.Size = new Size(118, 23);
            btnPreset4K.TabIndex = 7;
            btnPreset4K.Text = "4K (3840x2160)";
            btnPreset4K.UseVisualStyleBackColor = true;
            btnPreset4K.Click += btnPreset4K_Click;
            // 
            // btnPresetFHD
            // 
            btnPresetFHD.Location = new Point(298, 51);
            btnPresetFHD.Name = "btnPresetFHD";
            btnPresetFHD.Size = new Size(130, 23);
            btnPresetFHD.TabIndex = 6;
            btnPresetFHD.Text = "FullHD (1920x1080)";
            btnPresetFHD.UseVisualStyleBackColor = true;
            btnPresetFHD.Click += btnPresetFHD_Click;
            // 
            // lblFormat
            // 
            lblFormat.AutoSize = true;
            lblFormat.Location = new Point(6, 117);
            lblFormat.Name = "lblFormat";
            lblFormat.Size = new Size(91, 15);
            lblFormat.TabIndex = 5;
            lblFormat.Text = "Формат файла:";
            // 
            // cbFormat
            // 
            cbFormat.DropDownStyle = ComboBoxStyle.DropDownList;
            cbFormat.FormattingEnabled = true;
            cbFormat.Items.AddRange(new object[] { "PNG", "JPG", "BMP" });
            cbFormat.Location = new Point(9, 135);
            cbFormat.Name = "cbFormat";
            cbFormat.Size = new Size(273, 23);
            cbFormat.TabIndex = 4;
            cbFormat.SelectedIndexChanged += cbFormat_SelectedIndexChanged;
            // 
            // lblX
            // 
            lblX.AutoSize = true;
            lblX.Location = new Point(101, 53);
            lblX.Name = "lblX";
            lblX.Size = new Size(13, 15);
            lblX.TabIndex = 3;
            lblX.Text = "x";
            // 
            // nudHeight
            // 
            nudHeight.Location = new Point(115, 51);
            nudHeight.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
            nudHeight.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            nudHeight.Name = "nudHeight";
            nudHeight.Size = new Size(91, 23);
            nudHeight.TabIndex = 2;
            nudHeight.Value = new decimal(new int[] { 1080, 0, 0, 0 });
            // 
            // nudWidth
            // 
            nudWidth.Location = new Point(9, 51);
            nudWidth.Maximum = new decimal(new int[] { 16384, 0, 0, 0 });
            nudWidth.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            nudWidth.Name = "nudWidth";
            nudWidth.Size = new Size(91, 23);
            nudWidth.TabIndex = 1;
            nudWidth.Value = new decimal(new int[] { 1920, 0, 0, 0 });
            // 
            // lblResolution
            // 
            lblResolution.AutoSize = true;
            lblResolution.Location = new Point(6, 28);
            lblResolution.Name = "lblResolution";
            lblResolution.Size = new Size(155, 15);
            lblResolution.TabIndex = 0;
            lblResolution.Text = "Разрешение изображения:";
            // 
            // pnlBottom
            // 
            pnlBottom.Controls.Add(lblStatus);
            pnlBottom.Controls.Add(progressBar);
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnSave);
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Location = new Point(0, 411);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Size = new Size(584, 50);
            pnlBottom.TabIndex = 1;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(13, 3);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(45, 15);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "Готово";
            // 
            // progressBar
            // 
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Location = new Point(13, 21);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(387, 23);
            progressBar.TabIndex = 2;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(406, 21);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Закрыть";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSave.Location = new Point(487, 21);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(84, 23);
            btnSave.TabIndex = 0;
            btnSave.Text = "Сохранить";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // SaveImageManagerForm
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(584, 461);
            Controls.Add(pnlMain);
            Controls.Add(pnlBottom);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SaveImageManagerForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Менеджер сохранения изображений";
            FormClosing += SaveImageManagerForm_FormClosing;
            Load += SaveImageManagerForm_Load;
            pnlMain.ResumeLayout(false);
            grpEffects.ResumeLayout(false);
            grpEffects.PerformLayout();
            grpPostProcessing.ResumeLayout(false);
            grpPostProcessing.PerformLayout();
            grpOutput.ResumeLayout(false);
            grpOutput.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarJpgQuality).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudHeight).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudWidth).EndInit();
            pnlBottom.ResumeLayout(false);
            pnlBottom.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.GroupBox grpOutput;
        private System.Windows.Forms.Panel pnlBottom;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.GroupBox grpEffects;
        private System.Windows.Forms.Label lblSsaa;
        private System.Windows.Forms.ComboBox cbSSAA;
        private System.Windows.Forms.Label lblFormat;
        private System.Windows.Forms.ComboBox cbFormat;
        private System.Windows.Forms.Label lblX;
        private System.Windows.Forms.NumericUpDown nudHeight;
        private System.Windows.Forms.NumericUpDown nudWidth;
        private System.Windows.Forms.Label lblResolution;
        private System.Windows.Forms.Button btnPreset4K;
        private System.Windows.Forms.Button btnPresetFHD;
        private System.Windows.Forms.GroupBox grpPostProcessing;
        private System.Windows.Forms.TrackBar trackBarJpgQuality;
        private System.Windows.Forms.Label lblJpgQuality;
        private System.Windows.Forms.Label lblJpgQualityValue;
        private System.Windows.Forms.CheckBox chkApplyBicubic;
        private System.Windows.Forms.Button btnRotate;
        private System.Windows.Forms.Button btnPreset720p;
        private System.Windows.Forms.Button btnPreset2K;
        private System.Windows.Forms.Button btnPreset8K;
    }
}