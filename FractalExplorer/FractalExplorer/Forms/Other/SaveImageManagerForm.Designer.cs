// Обратите внимание: пространство имен вашего проекта может отличаться.
// Убедитесь, что оно совпадает с вашим.
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
            this.pnlMain = new System.Windows.Forms.Panel();
            this.grpEffects = new System.Windows.Forms.GroupBox();
            this.grpPostProcessing = new System.Windows.Forms.GroupBox();
            this.chkApplyBicubic = new System.Windows.Forms.CheckBox();
            this.lblSsaa = new System.Windows.Forms.Label();
            this.cbSSAA = new System.Windows.Forms.ComboBox();
            this.grpOutput = new System.Windows.Forms.GroupBox();
            this.lblJpgQualityValue = new System.Windows.Forms.Label();
            this.trackBarJpgQuality = new System.Windows.Forms.TrackBar();
            this.lblJpgQuality = new System.Windows.Forms.Label();
            this.btnPreset4K = new System.Windows.Forms.Button();
            this.btnPresetFHD = new System.Windows.Forms.Button();
            this.lblFormat = new System.Windows.Forms.Label();
            this.cbFormat = new System.Windows.Forms.ComboBox();
            this.lblX = new System.Windows.Forms.Label();
            this.nudHeight = new System.Windows.Forms.NumericUpDown();
            this.nudWidth = new System.Windows.Forms.NumericUpDown();
            this.lblResolution = new System.Windows.Forms.Label();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.pnlMain.SuspendLayout();
            this.grpEffects.SuspendLayout();
            this.grpPostProcessing.SuspendLayout();
            this.grpOutput.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarJpgQuality)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudWidth)).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlMain
            // 
            this.pnlMain.Controls.Add(this.grpEffects);
            this.pnlMain.Controls.Add(this.grpOutput);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(0, 0);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Padding = new System.Windows.Forms.Padding(10);
            this.pnlMain.Size = new System.Drawing.Size(584, 411);
            this.pnlMain.TabIndex = 0;
            // 
            // grpEffects
            // 
            this.grpEffects.Controls.Add(this.grpPostProcessing);
            this.grpEffects.Controls.Add(this.lblSsaa);
            this.grpEffects.Controls.Add(this.cbSSAA);
            this.grpEffects.Location = new System.Drawing.Point(13, 209);
            this.grpEffects.Name = "grpEffects";
            this.grpEffects.Size = new System.Drawing.Size(558, 189);
            this.grpEffects.TabIndex = 1;
            this.grpEffects.TabStop = false;
            this.grpEffects.Text = "Эффекты и качество";
            // 
            // grpPostProcessing
            // 
            this.grpPostProcessing.Controls.Add(this.chkApplyBicubic);
            this.grpPostProcessing.Enabled = true;
            this.grpPostProcessing.Location = new System.Drawing.Point(9, 81);
            this.grpPostProcessing.Name = "grpPostProcessing";
            this.grpPostProcessing.Size = new System.Drawing.Size(543, 102);
            this.grpPostProcessing.TabIndex = 2;
            this.grpPostProcessing.TabStop = false;
            this.grpPostProcessing.Text = "Пост-обработка";
            // 
            // chkApplyBicubic
            // 
            this.chkApplyBicubic.AutoSize = true;
            this.chkApplyBicubic.Location = new System.Drawing.Point(12, 25);
            this.chkApplyBicubic.Name = "chkApplyBicubic";
            this.chkApplyBicubic.Size = new System.Drawing.Size(262, 19);
            this.chkApplyBicubic.TabIndex = 0;
            this.chkApplyBicubic.Text = "Применить бикубическую интерполяцию";
            this.chkApplyBicubic.UseVisualStyleBackColor = true;
            // 
            // lblSsaa
            // 
            this.lblSsaa.AutoSize = true;
            this.lblSsaa.Location = new System.Drawing.Point(6, 28);
            this.lblSsaa.Name = "lblSsaa";
            this.lblSsaa.Size = new System.Drawing.Size(155, 15);
            this.lblSsaa.TabIndex = 1;
            this.lblSsaa.Text = "Сглаживание (SSAA)";
            // 
            // cbSSAA
            // 
            this.cbSSAA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSSAA.FormattingEnabled = true;
            this.cbSSAA.Items.AddRange(new object[] {
            "Выкл (1x)",
            "Низкое (2x)",
            "Высокое (4x)",
            "Ультра (8x)"});
            this.cbSSAA.Location = new System.Drawing.Point(9, 46);
            this.cbSSAA.Name = "cbSSAA";
            this.cbSSAA.Size = new System.Drawing.Size(249, 23);
            this.cbSSAA.TabIndex = 0;
            // 
            // grpOutput
            // 
            this.grpOutput.Controls.Add(this.lblJpgQualityValue);
            this.grpOutput.Controls.Add(this.trackBarJpgQuality);
            this.grpOutput.Controls.Add(this.lblJpgQuality);
            this.grpOutput.Controls.Add(this.btnPreset4K);
            this.grpOutput.Controls.Add(this.btnPresetFHD);
            this.grpOutput.Controls.Add(this.lblFormat);
            this.grpOutput.Controls.Add(this.cbFormat);
            this.grpOutput.Controls.Add(this.lblX);
            this.grpOutput.Controls.Add(this.nudHeight);
            this.grpOutput.Controls.Add(this.nudWidth);
            this.grpOutput.Controls.Add(this.lblResolution);
            this.grpOutput.Location = new System.Drawing.Point(13, 13);
            this.grpOutput.Name = "grpOutput";
            this.grpOutput.Size = new System.Drawing.Size(558, 190);
            this.grpOutput.TabIndex = 0;
            this.grpOutput.TabStop = false;
            this.grpOutput.Text = "Параметры вывода";
            // 
            // lblJpgQualityValue
            // 
            this.lblJpgQualityValue.AutoSize = true;
            this.lblJpgQualityValue.Location = new System.Drawing.Point(461, 140);
            this.lblJpgQualityValue.Name = "lblJpgQualityValue";
            this.lblJpgQualityValue.Size = new System.Drawing.Size(29, 15);
            this.lblJpgQualityValue.TabIndex = 10;
            this.lblJpgQualityValue.Text = "95%";
            // 
            // trackBarJpgQuality
            // 
            this.trackBarJpgQuality.Location = new System.Drawing.Point(171, 135);
            this.trackBarJpgQuality.Maximum = 100;
            this.trackBarJpgQuality.Minimum = 1;
            this.trackBarJpgQuality.Name = "trackBarJpgQuality";
            this.trackBarJpgQuality.Size = new System.Drawing.Size(284, 45);
            this.trackBarJpgQuality.TabIndex = 9;
            this.trackBarJpgQuality.TickFrequency = 5;
            this.trackBarJpgQuality.Value = 95;
            this.trackBarJpgQuality.Scroll += new System.EventHandler(this.trackBarJpgQuality_Scroll);
            // 
            // lblJpgQuality
            // 
            this.lblJpgQuality.AutoSize = true;
            this.lblJpgQuality.Location = new System.Drawing.Point(6, 140);
            this.lblJpgQuality.Name = "lblJpgQuality";
            this.lblJpgQuality.Size = new System.Drawing.Size(125, 15);
            this.lblJpgQuality.TabIndex = 8;
            this.lblJpgQuality.Text = "Качество JPG:";
            // 
            // btnPreset4K
            // 
            this.btnPreset4K.Location = new System.Drawing.Point(434, 49);
            this.btnPreset4K.Name = "btnPreset4K";
            this.btnPreset4K.Size = new System.Drawing.Size(118, 23);
            this.btnPreset4K.TabIndex = 7;
            this.btnPreset4K.Text = "4K (3840x2160)";
            this.btnPreset4K.UseVisualStyleBackColor = true;
            this.btnPreset4K.Click += new System.EventHandler(this.btnPreset4K_Click);
            // 
            // btnPresetFHD
            // 
            this.btnPresetFHD.Location = new System.Drawing.Point(298, 49);
            this.btnPresetFHD.Name = "btnPresetFHD";
            this.btnPresetFHD.Size = new System.Drawing.Size(130, 23);
            this.btnPresetFHD.TabIndex = 6;
            this.btnPresetFHD.Text = "FullHD (1920x1080)";
            this.btnPresetFHD.UseVisualStyleBackColor = true;
            this.btnPresetFHD.Click += new System.EventHandler(this.btnPresetFHD_Click);
            // 
            // lblFormat
            // 
            this.lblFormat.AutoSize = true;
            this.lblFormat.Location = new System.Drawing.Point(6, 85);
            this.lblFormat.Name = "lblFormat";
            this.lblFormat.Size = new System.Drawing.Size(95, 15);
            this.lblFormat.TabIndex = 5;
            this.lblFormat.Text = "Формат файла:";
            // 
            // cbFormat
            // 
            this.cbFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbFormat.FormattingEnabled = true;
            this.cbFormat.Items.AddRange(new object[] {
            "PNG",
            "JPG",
            "BMP"});
            this.cbFormat.Location = new System.Drawing.Point(9, 103);
            this.cbFormat.Name = "cbFormat";
            this.cbFormat.Size = new System.Drawing.Size(273, 23);
            this.cbFormat.TabIndex = 4;
            this.cbFormat.SelectedIndexChanged += new System.EventHandler(this.cbFormat_SelectedIndexChanged);
            // 
            // lblX
            // 
            this.lblX.AutoSize = true;
            this.lblX.Location = new System.Drawing.Point(137, 53);
            this.lblX.Name = "lblX";
            this.lblX.Size = new System.Drawing.Size(13, 15);
            this.lblX.TabIndex = 3;
            this.lblX.Text = "x";
            // 
            // nudHeight
            // 
            this.nudHeight.Location = new System.Drawing.Point(156, 49);
            this.nudHeight.Maximum = new decimal(new int[] {
            16384,
            0,
            0,
            0});
            this.nudHeight.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudHeight.Name = "nudHeight";
            this.nudHeight.Size = new System.Drawing.Size(126, 23);
            this.nudHeight.TabIndex = 2;
            this.nudHeight.Value = new decimal(new int[] {
            1080,
            0,
            0,
            0});
            // 
            // nudWidth
            // 
            this.nudWidth.Location = new System.Drawing.Point(9, 49);
            this.nudWidth.Maximum = new decimal(new int[] {
            16384,
            0,
            0,
            0});
            this.nudWidth.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudWidth.Name = "nudWidth";
            this.nudWidth.Size = new System.Drawing.Size(122, 23);
            this.nudWidth.TabIndex = 1;
            this.nudWidth.Value = new decimal(new int[] {
            1920,
            0,
            0,
            0});
            // 
            // lblResolution
            // 
            this.lblResolution.AutoSize = true;
            this.lblResolution.Location = new System.Drawing.Point(6, 28);
            this.lblResolution.Name = "lblResolution";
            this.lblResolution.Size = new System.Drawing.Size(147, 15);
            this.lblResolution.TabIndex = 0;
            this.lblResolution.Text = "Разрешение изображения:";
            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.lblStatus);
            this.pnlBottom.Controls.Add(this.progressBar);
            this.pnlBottom.Controls.Add(this.btnCancel);
            this.pnlBottom.Controls.Add(this.btnSave);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 411);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(584, 50);
            this.pnlBottom.TabIndex = 1;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(13, 3);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(46, 15);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "Готово";
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(13, 21);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(387, 23);
            this.progressBar.TabIndex = 2;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(406, 21);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(487, 21);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(84, 23);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // SaveImageManagerForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(584, 461);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlBottom);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SaveImageManagerForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Менеджер сохранения изображений";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SaveImageManagerForm_FormClosing);
            this.Load += new System.EventHandler(this.SaveImageManagerForm_Load);
            this.pnlMain.ResumeLayout(false);
            this.grpEffects.ResumeLayout(false);
            this.grpEffects.PerformLayout();
            this.grpPostProcessing.ResumeLayout(false);
            this.grpPostProcessing.PerformLayout();
            this.grpOutput.ResumeLayout(false);
            this.grpOutput.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarJpgQuality)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudWidth)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.pnlBottom.PerformLayout();
            this.ResumeLayout(false);

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
        // Новое поле для CheckBox
        private System.Windows.Forms.CheckBox chkApplyBicubic;
    }
}