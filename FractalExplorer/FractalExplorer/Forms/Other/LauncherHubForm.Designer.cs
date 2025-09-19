namespace FractalExplorer
{
    partial class LauncherHubForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LauncherHubForm));
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.treeViewFractals = new System.Windows.Forms.TreeView();
            this.pnlDetails = new System.Windows.Forms.TableLayoutPanel();
            this.lblFractalName = new System.Windows.Forms.Label();
            this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
            this.richTextBoxDescription = new System.Windows.Forms.RichTextBox();
            this.btnLaunchSelected = new System.Windows.Forms.Button();
            this.groupBoxSystemInfo = new System.Windows.Forms.GroupBox();
            this.tblSystemInfo = new System.Windows.Forms.TableLayoutPanel();
            this.lblPowerValue = new System.Windows.Forms.Label();
            this.lblTempLabel = new System.Windows.Forms.Label();
            this.lblPowerLabel = new System.Windows.Forms.Label();
            this.lblTempValue = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.pnlDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
            this.groupBoxSystemInfo.SuspendLayout();
            this.tblSystemInfo.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.IsSplitterFixed = true;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 0);
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.treeViewFractals);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.pnlDetails);
            this.splitContainerMain.Size = new System.Drawing.Size(844, 521);
            this.splitContainerMain.SplitterDistance = 284;
            this.splitContainerMain.TabIndex = 0;
            // 
            // treeViewFractals
            // 
            this.treeViewFractals.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewFractals.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.treeViewFractals.Location = new System.Drawing.Point(0, 0);
            this.treeViewFractals.Name = "treeViewFractals";
            this.treeViewFractals.Size = new System.Drawing.Size(284, 521);
            this.treeViewFractals.TabIndex = 0;
            this.treeViewFractals.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewFractals_AfterSelect);
            // 
            // pnlDetails
            // 
            this.pnlDetails.ColumnCount = 1;
            this.pnlDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlDetails.Controls.Add(this.lblFractalName, 0, 0);
            this.pnlDetails.Controls.Add(this.pictureBoxPreview, 0, 1);
            this.pnlDetails.Controls.Add(this.richTextBoxDescription, 0, 2);
            this.pnlDetails.Controls.Add(this.btnLaunchSelected, 0, 4);
            this.pnlDetails.Controls.Add(this.groupBoxSystemInfo, 0, 3);
            this.pnlDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDetails.Location = new System.Drawing.Point(0, 0);
            this.pnlDetails.Name = "pnlDetails";
            this.pnlDetails.Padding = new System.Windows.Forms.Padding(5);
            this.pnlDetails.RowCount = 5;
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.pnlDetails.Size = new System.Drawing.Size(556, 521);
            this.pnlDetails.TabIndex = 0;
            // 
            // lblFractalName
            // 
            this.lblFractalName.AutoSize = true;
            this.lblFractalName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFractalName.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            this.lblFractalName.Location = new System.Drawing.Point(8, 5);
            this.lblFractalName.Name = "lblFractalName";
            this.lblFractalName.Size = new System.Drawing.Size(540, 40);
            this.lblFractalName.TabIndex = 0;
            this.lblFractalName.Text = "Выберите фрактал";
            this.lblFractalName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxPreview.Location = new System.Drawing.Point(8, 48);
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.Size = new System.Drawing.Size(540, 164);
            this.pictureBoxPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxPreview.TabIndex = 1;
            this.pictureBoxPreview.TabStop = false;
            // 
            // richTextBoxDescription
            // 
            this.richTextBoxDescription.BackColor = System.Drawing.SystemColors.Control;
            this.richTextBoxDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxDescription.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.richTextBoxDescription.Location = new System.Drawing.Point(8, 218);
            this.richTextBoxDescription.Name = "richTextBoxDescription";
            this.richTextBoxDescription.ReadOnly = true;
            this.richTextBoxDescription.Size = new System.Drawing.Size(540, 164);
            this.richTextBoxDescription.TabIndex = 2;
            this.richTextBoxDescription.Text = "";
            // 
            // btnLaunchSelected
            // 
            this.btnLaunchSelected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLaunchSelected.Enabled = false;
            this.btnLaunchSelected.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.btnLaunchSelected.Location = new System.Drawing.Point(8, 468);
            this.btnLaunchSelected.Name = "btnLaunchSelected";
            this.btnLaunchSelected.Size = new System.Drawing.Size(540, 45);
            this.btnLaunchSelected.TabIndex = 3;
            this.btnLaunchSelected.Text = "Запустить";
            this.btnLaunchSelected.UseVisualStyleBackColor = true;
            this.btnLaunchSelected.Click += new System.EventHandler(this.btnLaunchSelected_Click);
            // 
            // groupBoxSystemInfo
            // 
            this.groupBoxSystemInfo.Controls.Add(this.tblSystemInfo);
            this.groupBoxSystemInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxSystemInfo.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.groupBoxSystemInfo.Location = new System.Drawing.Point(8, 388);
            this.groupBoxSystemInfo.Name = "groupBoxSystemInfo";
            this.groupBoxSystemInfo.Size = new System.Drawing.Size(540, 74);
            this.groupBoxSystemInfo.TabIndex = 4;
            this.groupBoxSystemInfo.TabStop = false;
            this.groupBoxSystemInfo.Text = "Мониторинг системы";
            // 
            // tblSystemInfo
            // 
            this.tblSystemInfo.ColumnCount = 2;
            this.tblSystemInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.tblSystemInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblSystemInfo.Controls.Add(this.lblPowerValue, 1, 1);
            this.tblSystemInfo.Controls.Add(this.lblTempLabel, 0, 0);
            this.tblSystemInfo.Controls.Add(this.lblPowerLabel, 0, 1);
            this.tblSystemInfo.Controls.Add(this.lblTempValue, 1, 0);
            this.tblSystemInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblSystemInfo.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.tblSystemInfo.Location = new System.Drawing.Point(3, 19);
            this.tblSystemInfo.Name = "tblSystemInfo";
            this.tblSystemInfo.RowCount = 2;
            this.tblSystemInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblSystemInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblSystemInfo.Size = new System.Drawing.Size(534, 52);
            this.tblSystemInfo.TabIndex = 0;
            // 
            // lblPowerValue
            // 
            this.lblPowerValue.AutoSize = true;
            this.lblPowerValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPowerValue.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblPowerValue.Location = new System.Drawing.Point(183, 26);
            this.lblPowerValue.Name = "lblPowerValue";
            this.lblPowerValue.Size = new System.Drawing.Size(348, 26);
            this.lblPowerValue.TabIndex = 3;
            this.lblPowerValue.Text = "N/A";
            this.lblPowerValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblTempLabel
            // 
            this.lblTempLabel.AutoSize = true;
            this.lblTempLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTempLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblTempLabel.Location = new System.Drawing.Point(3, 0);
            this.lblTempLabel.Name = "lblTempLabel";
            this.lblTempLabel.Size = new System.Drawing.Size(174, 26);
            this.lblTempLabel.TabIndex = 0;
            this.lblTempLabel.Text = "Температура процессора:";
            this.lblTempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPowerLabel
            // 
            this.lblPowerLabel.AutoSize = true;
            this.lblPowerLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPowerLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblPowerLabel.Location = new System.Drawing.Point(3, 26);
            this.lblPowerLabel.Name = "lblPowerLabel";
            this.lblPowerLabel.Size = new System.Drawing.Size(174, 26);
            this.lblPowerLabel.TabIndex = 1;
            this.lblPowerLabel.Text = "Потребляемая мощность:";
            this.lblPowerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblTempValue
            // 
            this.lblTempValue.AutoSize = true;
            this.lblTempValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTempValue.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblTempValue.Location = new System.Drawing.Point(183, 0);
            this.lblTempValue.Name = "lblTempValue";
            this.lblTempValue.Size = new System.Drawing.Size(348, 26);
            this.lblTempValue.TabIndex = 2;
            this.lblTempValue.Text = "N/A";
            this.lblTempValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LauncherHubForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(844, 521);
            this.Controls.Add(this.splitContainerMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(720, 480);
            this.Name = "LauncherHubForm";
            this.Text = "Менеджер фракталов";
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.pnlDetails.ResumeLayout(false);
            this.pnlDetails.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
            this.groupBoxSystemInfo.ResumeLayout(false);
            this.tblSystemInfo.ResumeLayout(false);
            this.tblSystemInfo.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.TreeView treeViewFractals;
        private System.Windows.Forms.TableLayoutPanel pnlDetails;
        private System.Windows.Forms.Label lblFractalName;
        private System.Windows.Forms.PictureBox pictureBoxPreview;
        private System.Windows.Forms.RichTextBox richTextBoxDescription;
        private System.Windows.Forms.Button btnLaunchSelected;
        private System.Windows.Forms.GroupBox groupBoxSystemInfo;
        private System.Windows.Forms.TableLayoutPanel tblSystemInfo;
        private System.Windows.Forms.Label lblPowerValue;
        private System.Windows.Forms.Label lblTempLabel;
        private System.Windows.Forms.Label lblPowerLabel;
        private System.Windows.Forms.Label lblTempValue;
    }
}