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
            splitContainerMain = new SplitContainer();
            treeViewFractals = new TreeView();
            pnlDetails = new TableLayoutPanel();
            headerPanel = new TableLayoutPanel();
            lblFractalName = new Label();
            lblRenderPatternType = new Label();
            cbRenderPattern = new ComboBox();
            pictureBoxPreview = new PictureBox();
            richTextBoxDescription = new RichTextBox();
            btnLaunchSelected = new Button();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            pnlDetails.SuspendLayout();
            headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).BeginInit();
            SuspendLayout();
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.IsSplitterFixed = true;
            splitContainerMain.Location = new Point(0, 0);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(treeViewFractals);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(pnlDetails);
            splitContainerMain.Size = new Size(844, 521);
            splitContainerMain.SplitterDistance = 284;
            splitContainerMain.TabIndex = 0;
            // 
            // treeViewFractals
            // 
            treeViewFractals.Dock = DockStyle.Fill;
            treeViewFractals.Font = new Font("Segoe UI", 11F);
            treeViewFractals.Location = new Point(0, 0);
            treeViewFractals.Name = "treeViewFractals";
            treeViewFractals.Size = new Size(284, 521);
            treeViewFractals.TabIndex = 0;
            treeViewFractals.AfterSelect += treeViewFractals_AfterSelect;
            // 
            // pnlDetails
            // 
            pnlDetails.ColumnCount = 1;
            pnlDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlDetails.Controls.Add(headerPanel, 0, 0);
            pnlDetails.Controls.Add(pictureBoxPreview, 0, 1);
            pnlDetails.Controls.Add(richTextBoxDescription, 0, 2);
            pnlDetails.Controls.Add(btnLaunchSelected, 0, 4);
            pnlDetails.Dock = DockStyle.Fill;
            pnlDetails.Location = new Point(0, 0);
            pnlDetails.Name = "pnlDetails";
            pnlDetails.Padding = new Padding(5);
            pnlDetails.RowCount = 5;
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            pnlDetails.Size = new Size(556, 521);
            pnlDetails.TabIndex = 0;
            // 
            // headerPanel
            // 
            headerPanel.ColumnCount = 2;
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            headerPanel.Controls.Add(lblFractalName, 0, 0);
            headerPanel.Controls.Add(lblRenderPatternType, 1, 0);
            headerPanel.Controls.Add(cbRenderPattern, 1, 1);
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.Location = new Point(8, 8);
            headerPanel.Name = "headerPanel";
            headerPanel.RowCount = 2;
            headerPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            headerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            headerPanel.Size = new Size(540, 54);
            headerPanel.TabIndex = 0;
            // 
            // lblFractalName
            // 
            lblFractalName.AutoSize = true;
            lblFractalName.Dock = DockStyle.Fill;
            lblFractalName.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            lblFractalName.Location = new Point(3, 0);
            lblFractalName.Name = "lblFractalName";
            headerPanel.SetRowSpan(lblFractalName, 2);
            lblFractalName.Size = new Size(264, 54);
            lblFractalName.TabIndex = 0;
            lblFractalName.Text = "Выберите фрактал";
            lblFractalName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblRenderPatternType
            // 
            lblRenderPatternType.AutoSize = true;
            lblRenderPatternType.Dock = DockStyle.Fill;
            lblRenderPatternType.Font = new Font("Segoe UI", 9F);
            lblRenderPatternType.Location = new Point(273, 0);
            lblRenderPatternType.Name = "lblRenderPatternType";
            lblRenderPatternType.Size = new Size(264, 24);
            lblRenderPatternType.TabIndex = 1;
            lblRenderPatternType.Text = "Тип рендера";
            lblRenderPatternType.TextAlign = ContentAlignment.BottomLeft;
            // 
            // cbRenderPattern
            // 
            cbRenderPattern.Dock = DockStyle.Fill;
            cbRenderPattern.DropDownStyle = ComboBoxStyle.DropDownList;
            cbRenderPattern.Font = new Font("Segoe UI", 10F);
            cbRenderPattern.FormattingEnabled = true;
            cbRenderPattern.Location = new Point(273, 27);
            cbRenderPattern.Name = "cbRenderPattern";
            cbRenderPattern.Size = new Size(264, 25);
            cbRenderPattern.TabIndex = 2;
            // 
            // pictureBoxPreview
            // 
            pictureBoxPreview.Dock = DockStyle.Fill;
            pictureBoxPreview.Location = new Point(8, 68);
            pictureBoxPreview.Name = "pictureBoxPreview";
            pictureBoxPreview.Size = new Size(540, 193);
            pictureBoxPreview.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxPreview.TabIndex = 1;
            pictureBoxPreview.TabStop = false;
            // 
            // richTextBoxDescription
            // 
            richTextBoxDescription.BackColor = SystemColors.Control;
            richTextBoxDescription.BorderStyle = BorderStyle.None;
            richTextBoxDescription.Dock = DockStyle.Fill;
            richTextBoxDescription.Font = new Font("Segoe UI", 10F);
            richTextBoxDescription.Location = new Point(8, 267);
            richTextBoxDescription.Name = "richTextBoxDescription";
            richTextBoxDescription.ReadOnly = true;
            richTextBoxDescription.Size = new Size(540, 193);
            richTextBoxDescription.TabIndex = 2;
            richTextBoxDescription.Text = "";
            // 
            // btnLaunchSelected
            // 
            btnLaunchSelected.Dock = DockStyle.Fill;
            btnLaunchSelected.Enabled = false;
            btnLaunchSelected.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnLaunchSelected.Location = new Point(8, 474);
            btnLaunchSelected.Name = "btnLaunchSelected";
            btnLaunchSelected.Size = new Size(540, 39);
            btnLaunchSelected.TabIndex = 3;
            btnLaunchSelected.Text = "Запустить";
            btnLaunchSelected.UseVisualStyleBackColor = true;
            btnLaunchSelected.Click += btnLaunchSelected_Click;
            // 
            // LauncherHubForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(844, 521);
            Controls.Add(splitContainerMain);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimumSize = new Size(720, 480);
            Name = "LauncherHubForm";
            Text = "Менеджер фракталов";
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            pnlDetails.ResumeLayout(false);
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.TreeView treeViewFractals;
        private System.Windows.Forms.TableLayoutPanel pnlDetails;
        private System.Windows.Forms.TableLayoutPanel headerPanel;
        private System.Windows.Forms.Label lblFractalName;
        private System.Windows.Forms.Label lblRenderPatternType;
        private System.Windows.Forms.ComboBox cbRenderPattern;
        private System.Windows.Forms.PictureBox pictureBoxPreview;
        private System.Windows.Forms.RichTextBox richTextBoxDescription;
        private System.Windows.Forms.Button btnLaunchSelected;
    }
}
