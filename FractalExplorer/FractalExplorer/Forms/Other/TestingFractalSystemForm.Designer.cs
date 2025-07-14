namespace FractalExplorer.Forms.Other
{
    partial class TestingFractalSystemForm
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btnRunTests = new System.Windows.Forms.Button();
            this.lblOverallProgress = new System.Windows.Forms.Label();
            this.progressBarOverall = new System.Windows.Forms.ProgressBar();
            this.lblCurrentTest = new System.Windows.Forms.Label();
            this.progressBarCurrent = new System.Windows.Forms.ProgressBar();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this.tableLayoutPanel1.Controls.Add(this.btnRunTests, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblOverallProgress, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.progressBarOverall, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.lblCurrentTest, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.progressBarCurrent, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.rtbLog, 0, 5);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(10);
            this.tableLayoutPanel1.RowCount = 6;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(800, 450);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // btnRunTests
            // 
            this.btnRunTests.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRunTests.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnRunTests.Location = new System.Drawing.Point(633, 13);
            this.btnRunTests.Name = "btnRunTests";
            this.btnRunTests.Size = new System.Drawing.Size(154, 34);
            this.btnRunTests.TabIndex = 0;
            this.btnRunTests.Text = "Запустить тесты";
            this.btnRunTests.UseVisualStyleBackColor = true;
            this.btnRunTests.Click += new System.EventHandler(this.btnRunTests_Click);
            // 
            // lblOverallProgress
            // 
            this.lblOverallProgress.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.lblOverallProgress, 2);
            this.lblOverallProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOverallProgress.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblOverallProgress.Location = new System.Drawing.Point(13, 50);
            this.lblOverallProgress.Name = "lblOverallProgress";
            this.lblOverallProgress.Size = new System.Drawing.Size(774, 25);
            this.lblOverallProgress.TabIndex = 1;
            this.lblOverallProgress.Text = "Общий прогресс:";
            this.lblOverallProgress.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // progressBarOverall
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.progressBarOverall, 2);
            this.progressBarOverall.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBarOverall.Location = new System.Drawing.Point(13, 78);
            this.progressBarOverall.Name = "progressBarOverall";
            this.progressBarOverall.Size = new System.Drawing.Size(774, 24);
            this.progressBarOverall.TabIndex = 2;
            // 
            // lblCurrentTest
            // 
            this.lblCurrentTest.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.lblCurrentTest, 2);
            this.lblCurrentTest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCurrentTest.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblCurrentTest.Location = new System.Drawing.Point(13, 105);
            this.lblCurrentTest.Name = "lblCurrentTest";
            this.lblCurrentTest.Size = new System.Drawing.Size(774, 25);
            this.lblCurrentTest.TabIndex = 3;
            this.lblCurrentTest.Text = "Текущий тест: -";
            this.lblCurrentTest.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // progressBarCurrent
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.progressBarCurrent, 2);
            this.progressBarCurrent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBarCurrent.Location = new System.Drawing.Point(13, 133);
            this.progressBarCurrent.Name = "progressBarCurrent";
            this.progressBarCurrent.Size = new System.Drawing.Size(774, 24);
            this.progressBarCurrent.TabIndex = 4;
            // 
            // rtbLog
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.rtbLog, 2);
            this.rtbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbLog.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rtbLog.Location = new System.Drawing.Point(13, 163);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(774, 274);
            this.rtbLog.TabIndex = 5;
            this.rtbLog.Text = "";
            this.rtbLog.WordWrap = false;
            // 
            // TestingFractalSystemForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "TestingFractalSystemForm";
            this.Text = "Система визуального тестирования";
            this.Load += new System.EventHandler(this.TestingFractalSystemForm_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button btnRunTests;
        private System.Windows.Forms.Label lblOverallProgress;
        private System.Windows.Forms.ProgressBar progressBarOverall;
        private System.Windows.Forms.Label lblCurrentTest;
        private System.Windows.Forms.ProgressBar progressBarCurrent;
        private System.Windows.Forms.RichTextBox rtbLog;
    }
}