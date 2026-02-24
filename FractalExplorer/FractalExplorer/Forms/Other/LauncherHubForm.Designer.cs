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
            this.settingsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblRenderPatternType = new System.Windows.Forms.Label();
            this.cbRenderPattern = new System.Windows.Forms.ComboBox();
            this.btnLaunchSelected = new System.Windows.Forms.Button();
            this.richTextBoxDescription = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            this.pnlDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
            this.settingsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.BackColor = System.Drawing.SystemColors.ControlLight;
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.IsSplitterFixed = true;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 0);
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.BackColor = System.Drawing.SystemColors.Window;
            this.splitContainerMain.Panel1.Controls.Add(this.treeViewFractals);
            this.splitContainerMain.Panel1.Padding = new System.Windows.Forms.Padding(10);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.BackColor = System.Drawing.SystemColors.Control;
            this.splitContainerMain.Panel2.Controls.Add(this.pnlDetails);
            this.splitContainerMain.Size = new System.Drawing.Size(844, 521);
            this.splitContainerMain.SplitterDistance = 284;
            this.splitContainerMain.SplitterWidth = 2;
            this.splitContainerMain.TabIndex = 0;
            // 
            // treeViewFractals
            // 
            this.treeViewFractals.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.treeViewFractals.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewFractals.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.treeViewFractals.Location = new System.Drawing.Point(10, 10);
            this.treeViewFractals.Name = "treeViewFractals";
            this.treeViewFractals.ShowLines = false;
            this.treeViewFractals.Size = new System.Drawing.Size(264, 501);
            this.treeViewFractals.TabIndex = 0;
            this.treeViewFractals.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewFractals_AfterSelect);
            // 
            // pnlDetails
            // 
            this.pnlDetails.ColumnCount = 2;
            this.pnlDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.pnlDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlDetails.Controls.Add(this.lblFractalName, 0, 0);
            this.pnlDetails.Controls.Add(this.pictureBoxPreview, 0, 1);
            this.pnlDetails.Controls.Add(this.settingsPanel, 1, 1);
            this.pnlDetails.Controls.Add(this.richTextBoxDescription, 0, 2);
            this.pnlDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDetails.Location = new System.Drawing.Point(0, 0);
            this.pnlDetails.Name = "pnlDetails";
            this.pnlDetails.Padding = new System.Windows.Forms.Padding(15);
            this.pnlDetails.RowCount = 3;
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 220F));
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlDetails.Size = new System.Drawing.Size(558, 521);
            this.pnlDetails.TabIndex = 0;
            // 
            // lblFractalName
            // 
            this.lblFractalName.AutoSize = true;
            this.pnlDetails.SetColumnSpan(this.lblFractalName, 2);
            this.lblFractalName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFractalName.Font = new System.Drawing.Font("Segoe UI Semibold", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblFractalName.Location = new System.Drawing.Point(15, 15);
            this.lblFractalName.Margin = new System.Windows.Forms.Padding(0, 0, 0, 15);
            this.lblFractalName.Name = "lblFractalName";
            this.lblFractalName.Size = new System.Drawing.Size(528, 30);
            this.lblFractalName.TabIndex = 0;
            this.lblFractalName.Text = "Выберите фрактал";
            this.lblFractalName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.BackColor = System.Drawing.SystemColors.ControlLight;
            this.pictureBoxPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxPreview.Location = new System.Drawing.Point(15, 60);
            this.pictureBoxPreview.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.Size = new System.Drawing.Size(205, 220);
            this.pictureBoxPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxPreview.TabIndex = 1;
            this.pictureBoxPreview.TabStop = false;
            // 
            // settingsPanel
            // 
            this.settingsPanel.ColumnCount = 1;
            this.settingsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingsPanel.Controls.Add(this.lblRenderPatternType, 0, 0);
            this.settingsPanel.Controls.Add(this.cbRenderPattern, 0, 1);
            this.settingsPanel.Controls.Add(this.btnLaunchSelected, 0, 2);
            this.settingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.settingsPanel.Location = new System.Drawing.Point(235, 60);
            this.settingsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.settingsPanel.Name = "settingsPanel";
            this.settingsPanel.RowCount = 3;
            this.settingsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.settingsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.settingsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.settingsPanel.Size = new System.Drawing.Size(308, 220);
            this.settingsPanel.TabIndex = 4;
            // 
            // lblRenderPatternType
            // 
            this.lblRenderPatternType.AutoSize = true;
            this.lblRenderPatternType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRenderPatternType.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblRenderPatternType.Location = new System.Drawing.Point(0, 0);
            this.lblRenderPatternType.Margin = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.lblRenderPatternType.Name = "lblRenderPatternType";
            this.lblRenderPatternType.Size = new System.Drawing.Size(308, 19);
            this.lblRenderPatternType.TabIndex = 1;
            this.lblRenderPatternType.Text = "Тип рендера:";
            // 
            // cbRenderPattern
            // 
            this.cbRenderPattern.Dock = System.Windows.Forms.DockStyle.Top;
            this.cbRenderPattern.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRenderPattern.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cbRenderPattern.FormattingEnabled = true;
            this.cbRenderPattern.Location = new System.Drawing.Point(0, 24);
            this.cbRenderPattern.Margin = new System.Windows.Forms.Padding(0);
            this.cbRenderPattern.Name = "cbRenderPattern";
            this.cbRenderPattern.Size = new System.Drawing.Size(308, 28);
            this.cbRenderPattern.TabIndex = 2;
            // 
            // btnLaunchSelected
            // 
            this.btnLaunchSelected.BackColor = System.Drawing.SystemColors.Highlight;
            this.btnLaunchSelected.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLaunchSelected.Enabled = false;
            this.btnLaunchSelected.FlatAppearance.BorderSize = 0;
            this.btnLaunchSelected.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLaunchSelected.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnLaunchSelected.ForeColor = System.Drawing.Color.White;
            this.btnLaunchSelected.Location = new System.Drawing.Point(0, 175);
            this.btnLaunchSelected.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.btnLaunchSelected.Name = "btnLaunchSelected";
            this.btnLaunchSelected.Size = new System.Drawing.Size(308, 45);
            this.btnLaunchSelected.TabIndex = 3;
            this.btnLaunchSelected.Text = "Запустить";
            this.btnLaunchSelected.UseVisualStyleBackColor = false;
            this.btnLaunchSelected.Click += new System.EventHandler(this.btnLaunchSelected_Click);
            // 
            // richTextBoxDescription
            // 
            this.richTextBoxDescription.BackColor = System.Drawing.SystemColors.Control;
            this.richTextBoxDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.pnlDetails.SetColumnSpan(this.richTextBoxDescription, 2);
            this.richTextBoxDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxDescription.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.richTextBoxDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.richTextBoxDescription.Location = new System.Drawing.Point(15, 295);
            this.richTextBoxDescription.Margin = new System.Windows.Forms.Padding(0, 15, 0, 0);
            this.richTextBoxDescription.Name = "richTextBoxDescription";
            this.richTextBoxDescription.ReadOnly = true;
            this.richTextBoxDescription.Size = new System.Drawing.Size(528, 211);
            this.richTextBoxDescription.TabIndex = 2;
            this.richTextBoxDescription.Text = "";
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
            this.settingsPanel.ResumeLayout(false);
            this.settingsPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.TreeView treeViewFractals;
        private System.Windows.Forms.TableLayoutPanel pnlDetails;
        private System.Windows.Forms.Label lblFractalName;
        private System.Windows.Forms.PictureBox pictureBoxPreview;
        private System.Windows.Forms.RichTextBox richTextBoxDescription;
        private System.Windows.Forms.TableLayoutPanel settingsPanel;
        private System.Windows.Forms.Label lblRenderPatternType;
        private System.Windows.Forms.ComboBox cbRenderPattern;
        private System.Windows.Forms.Button btnLaunchSelected;
    }
}