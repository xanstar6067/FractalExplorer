// PhoenixCSelectorForm.Designer.cs
namespace FractalExplorer.SelectorsForms
{
    partial class PhoenixCSelectorForm
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
            components = new System.ComponentModel.Container();
            sliceCanvasP = new PictureBox();
            sliceCanvasQ = new PictureBox();
            lblSliceP = new Label();
            lblSliceQ = new Label();
            nudPReal = new NumericUpDown();
            nudPImaginary = new NumericUpDown();
            nudQReal = new NumericUpDown();
            nudQImaginary = new NumericUpDown();
            lblPReal = new Label();
            lblPImaginary = new Label();
            lblQReal = new Label();
            lblQImaginary = new Label();
            btnApply = new Button();
            btnCancel = new Button();
            lblFixedQForPSlice = new Label();
            lblFixedPForQSlice = new Label();
            toolTipSelector = new ToolTip(components);
            progressBarSliceP = new ProgressBar();
            progressBarSliceQ = new ProgressBar();
            lblSlicePHint = new Label();
            lblSliceQHint = new Label();
            ((System.ComponentModel.ISupportInitialize)sliceCanvasP).BeginInit();
            ((System.ComponentModel.ISupportInitialize)sliceCanvasQ).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPReal).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPImaginary).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudQReal).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudQImaginary).BeginInit();
            SuspendLayout();
            // 
            // sliceCanvasP
            // 
            sliceCanvasP.BorderStyle = BorderStyle.None;
            sliceCanvasP.Location = new Point(12, 48);
            sliceCanvasP.Name = "sliceCanvasP";
            sliceCanvasP.Size = new Size(350, 350);
            sliceCanvasP.TabIndex = 0;
            sliceCanvasP.TabStop = false;
            // 
            // sliceCanvasQ
            // 
            sliceCanvasQ.BorderStyle = BorderStyle.None;
            sliceCanvasQ.Location = new Point(378, 48);
            sliceCanvasQ.Name = "sliceCanvasQ";
            sliceCanvasQ.Size = new Size(350, 350);
            sliceCanvasQ.TabIndex = 1;
            sliceCanvasQ.TabStop = false;
            // 
            // lblSliceP
            // 
            lblSliceP.AutoSize = true;
            lblSliceP.Location = new Point(12, 9);
            lblSliceP.Name = "lblSliceP";
            lblSliceP.Size = new Size(175, 15);
            lblSliceP.TabIndex = 2;
            lblSliceP.Text = "Срез по C1.P (Q фиксировано)";
            // 
            // lblSliceQ
            // 
            lblSliceQ.AutoSize = true;
            lblSliceQ.Location = new Point(375, 9);
            lblSliceQ.Name = "lblSliceQ";
            lblSliceQ.Size = new Size(175, 15);
            lblSliceQ.TabIndex = 3;
            lblSliceQ.Text = "Срез по C1.Q (P фиксировано)";
            // 
            // nudPReal
            // 
            nudPReal.DecimalPlaces = 4;
            nudPReal.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudPReal.Location = new Point(76, 420);
            nudPReal.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudPReal.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudPReal.Name = "nudPReal";
            nudPReal.Size = new Size(100, 23);
            nudPReal.TabIndex = 4;
            // 
            // nudPImaginary
            // 
            nudPImaginary.DecimalPlaces = 4;
            nudPImaginary.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudPImaginary.Location = new Point(262, 420);
            nudPImaginary.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudPImaginary.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudPImaginary.Name = "nudPImaginary";
            nudPImaginary.Size = new Size(100, 23);
            nudPImaginary.TabIndex = 5;
            // 
            // nudQReal
            // 
            nudQReal.DecimalPlaces = 4;
            nudQReal.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudQReal.Location = new Point(442, 420);
            nudQReal.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudQReal.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudQReal.Name = "nudQReal";
            nudQReal.Size = new Size(100, 23);
            nudQReal.TabIndex = 6;
            // 
            // nudQImaginary
            // 
            nudQImaginary.DecimalPlaces = 4;
            nudQImaginary.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudQImaginary.Location = new Point(628, 420);
            nudQImaginary.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudQImaginary.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudQImaginary.Name = "nudQImaginary";
            nudQImaginary.Size = new Size(100, 23);
            nudQImaginary.TabIndex = 7;
            // 
            // lblPReal
            // 
            lblPReal.AutoSize = true;
            lblPReal.Location = new Point(12, 422);
            lblPReal.Name = "lblPReal";
            lblPReal.Size = new Size(58, 15);
            lblPReal.TabIndex = 8;
            lblPReal.Text = "C1.P (Re):";
            // 
            // lblPImaginary
            // 
            lblPImaginary.AutoSize = true;
            lblPImaginary.Location = new Point(182, 422);
            lblPImaginary.Name = "lblPImaginary";
            lblPImaginary.Size = new Size(59, 15);
            lblPImaginary.TabIndex = 9;
            lblPImaginary.Text = "C1.P (Im):";
            // 
            // lblQReal
            // 
            lblQReal.AutoSize = true;
            lblQReal.Location = new Point(378, 422);
            lblQReal.Name = "lblQReal";
            lblQReal.Size = new Size(60, 15);
            lblQReal.TabIndex = 10;
            lblQReal.Text = "C1.Q (Re):";
            // 
            // lblQImaginary
            // 
            lblQImaginary.AutoSize = true;
            lblQImaginary.Location = new Point(548, 422);
            lblQImaginary.Name = "lblQImaginary";
            lblQImaginary.Size = new Size(61, 15);
            lblQImaginary.TabIndex = 11;
            lblQImaginary.Text = "C1.Q (Im):";
            // 
            // btnApply
            // 
            btnApply.Location = new Point(566, 455);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(81, 23);
            btnApply.TabIndex = 12;
            btnApply.Text = "Применить";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(653, 455);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 13;
            btnCancel.Text = "Отмена";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // lblFixedQForPSlice
            // 
            lblFixedQForPSlice.AutoSize = true;
            lblFixedQForPSlice.Location = new Point(193, 9);
            lblFixedQForPSlice.Name = "lblFixedQForPSlice";
            lblFixedQForPSlice.Size = new Size(76, 15);
            lblFixedQForPSlice.TabIndex = 14;
            lblFixedQForPSlice.Text = "(Q = X.XXXX)";
            // 
            // lblFixedPForQSlice
            // 
            lblFixedPForQSlice.AutoSize = true;
            lblFixedPForQSlice.Location = new Point(556, 9);
            lblFixedPForQSlice.Name = "lblFixedPForQSlice";
            lblFixedPForQSlice.Size = new Size(74, 15);
            lblFixedPForQSlice.TabIndex = 15;
            lblFixedPForQSlice.Text = "(P = Y.YYYY)";
            // 
            // progressBarSliceP
            // 
            progressBarSliceP.Location = new Point(12, 404);
            progressBarSliceP.Name = "progressBarSliceP";
            progressBarSliceP.Size = new Size(350, 10);
            progressBarSliceP.Style = ProgressBarStyle.Continuous;
            progressBarSliceP.TabIndex = 16;
            // 
            // progressBarSliceQ
            // 
            progressBarSliceQ.Location = new Point(378, 404);
            progressBarSliceQ.Name = "progressBarSliceQ";
            progressBarSliceQ.Size = new Size(350, 10);
            progressBarSliceQ.Style = ProgressBarStyle.Continuous;
            progressBarSliceQ.TabIndex = 17;
            // 
            // 
            // lblSlicePHint
            // 
            lblSlicePHint.AutoSize = true;
            lblSlicePHint.Location = new Point(12, 30);
            lblSlicePHint.Name = "lblSlicePHint";
            lblSlicePHint.Size = new Size(278, 15);
            lblSlicePHint.TabIndex = 18;
            lblSlicePHint.Text = "Кликните по изображению для выбора точки";
            // 
            // lblSliceQHint
            // 
            lblSliceQHint.AutoSize = true;
            lblSliceQHint.Location = new Point(378, 30);
            lblSliceQHint.Name = "lblSliceQHint";
            lblSliceQHint.Size = new Size(278, 15);
            lblSliceQHint.TabIndex = 19;
            lblSliceQHint.Text = "Кликните по изображению для выбора точки";
            // 
            // PhoenixCSelectorForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(740, 508);
            Controls.Add(lblSliceQHint);
            Controls.Add(lblSlicePHint);
            Controls.Add(progressBarSliceQ);
            Controls.Add(progressBarSliceP);
            Controls.Add(lblFixedPForQSlice);
            Controls.Add(lblFixedQForPSlice);
            Controls.Add(btnCancel);
            Controls.Add(btnApply);
            Controls.Add(lblQImaginary);
            Controls.Add(lblQReal);
            Controls.Add(lblPImaginary);
            Controls.Add(lblPReal);
            Controls.Add(nudQImaginary);
            Controls.Add(nudQReal);
            Controls.Add(nudPImaginary);
            Controls.Add(nudPReal);
            Controls.Add(lblSliceQ);
            Controls.Add(lblSliceP);
            Controls.Add(sliceCanvasQ);
            Controls.Add(sliceCanvasP);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PhoenixCSelectorForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Выбор параметров C1 для Феникса";
            ((System.ComponentModel.ISupportInitialize)sliceCanvasP).EndInit();
            ((System.ComponentModel.ISupportInitialize)sliceCanvasQ).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPReal).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPImaginary).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudQReal).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudQImaginary).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private System.Windows.Forms.PictureBox sliceCanvasP;
        private System.Windows.Forms.PictureBox sliceCanvasQ;
        private System.Windows.Forms.Label lblSliceP;
        private System.Windows.Forms.Label lblSliceQ;
        private System.Windows.Forms.NumericUpDown nudPReal;
        private System.Windows.Forms.NumericUpDown nudPImaginary;
        private System.Windows.Forms.NumericUpDown nudQReal;
        private System.Windows.Forms.NumericUpDown nudQImaginary;
        private System.Windows.Forms.Label lblPReal;
        private System.Windows.Forms.Label lblPImaginary;
        private System.Windows.Forms.Label lblQReal;
        private System.Windows.Forms.Label lblQImaginary;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblFixedQForPSlice;
        private System.Windows.Forms.Label lblFixedPForQSlice;
        private System.Windows.Forms.ToolTip toolTipSelector;
        private System.Windows.Forms.ProgressBar progressBarSliceP;
        private System.Windows.Forms.ProgressBar progressBarSliceQ;
        private System.Windows.Forms.Label lblSlicePHint;
        private System.Windows.Forms.Label lblSliceQHint;
    }
}
