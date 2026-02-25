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
            lblFractalName = new Label();
            pictureBoxPreview = new PictureBox();
            settingsPanel = new TableLayoutPanel();
            lblRenderPatternType = new Label();
            cbRenderPattern = new ComboBox();
            btnLaunchSelected = new Button();
            richTextBoxDescription = new RichTextBox();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            pnlDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).BeginInit();
            settingsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainerMain
            // 
            splitContainerMain.BackColor = SystemColors.ControlLight;
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.IsSplitterFixed = true;
            splitContainerMain.Location = new Point(0, 0);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.BackColor = SystemColors.Window;
            splitContainerMain.Panel1.Controls.Add(treeViewFractals);
            splitContainerMain.Panel1.Padding = new Padding(10);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.BackColor = SystemColors.Control;
            splitContainerMain.Panel2.Controls.Add(pnlDetails);
            splitContainerMain.Size = new Size(844, 521);
            splitContainerMain.SplitterDistance = 284;
            splitContainerMain.SplitterWidth = 2;
            splitContainerMain.TabIndex = 0;
            // 
            // treeViewFractals
            // 
            treeViewFractals.BorderStyle = BorderStyle.None;
            treeViewFractals.Dock = DockStyle.Fill;
            treeViewFractals.Font = new Font("Segoe UI", 11F);
            treeViewFractals.Location = new Point(10, 10);
            treeViewFractals.Name = "treeViewFractals";
            treeViewFractals.ShowLines = false;
            treeViewFractals.Size = new Size(264, 501);
            treeViewFractals.TabIndex = 0;
            treeViewFractals.AfterSelect += treeViewFractals_AfterSelect;
            // 
            // pnlDetails
            // 
            pnlDetails.ColumnCount = 2;
            pnlDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            pnlDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlDetails.Controls.Add(lblFractalName, 0, 0);
            pnlDetails.Controls.Add(pictureBoxPreview, 0, 1);
            pnlDetails.Controls.Add(settingsPanel, 1, 1);
            pnlDetails.Controls.Add(richTextBoxDescription, 0, 2);
            pnlDetails.Dock = DockStyle.Fill;
            pnlDetails.Location = new Point(0, 0);
            pnlDetails.Name = "pnlDetails";
            pnlDetails.Padding = new Padding(15);
            pnlDetails.RowCount = 3;
            pnlDetails.RowStyles.Add(new RowStyle());
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 202F));
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlDetails.Size = new Size(558, 521);
            pnlDetails.TabIndex = 0;
            // 
            // lblFractalName
            // 
            lblFractalName.AutoSize = true;
            pnlDetails.SetColumnSpan(lblFractalName, 2);
            lblFractalName.Dock = DockStyle.Fill;
            lblFractalName.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            lblFractalName.Location = new Point(15, 15);
            lblFractalName.Margin = new Padding(0, 0, 0, 15);
            lblFractalName.Name = "lblFractalName";
            lblFractalName.Size = new Size(528, 30);
            lblFractalName.TabIndex = 0;
            lblFractalName.Text = "Выберите фрактал";
            lblFractalName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pictureBoxPreview
            // 
            pictureBoxPreview.BackColor = SystemColors.ControlLight;
            pictureBoxPreview.Dock = DockStyle.Fill;
            pictureBoxPreview.Location = new Point(15, 60);
            pictureBoxPreview.Margin = new Padding(0, 0, 15, 0);
            pictureBoxPreview.Name = "pictureBoxPreview";
            pictureBoxPreview.Size = new Size(205, 202);
            pictureBoxPreview.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxPreview.TabIndex = 1;
            pictureBoxPreview.TabStop = false;
            // 
            // settingsPanel
            // 
            settingsPanel.ColumnCount = 1;
            settingsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            settingsPanel.Controls.Add(lblRenderPatternType, 0, 0);
            settingsPanel.Controls.Add(cbRenderPattern, 0, 1);
            settingsPanel.Controls.Add(btnLaunchSelected, 0, 2);
            settingsPanel.Dock = DockStyle.Fill;
            settingsPanel.Location = new Point(235, 60);
            settingsPanel.Margin = new Padding(0);
            settingsPanel.Name = "settingsPanel";
            settingsPanel.RowCount = 3;
            settingsPanel.RowStyles.Add(new RowStyle());
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            settingsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            settingsPanel.Size = new Size(308, 202);
            settingsPanel.TabIndex = 4;
            // 
            // lblRenderPatternType
            // 
            lblRenderPatternType.AutoSize = true;
            lblRenderPatternType.Dock = DockStyle.Fill;
            lblRenderPatternType.Font = new Font("Segoe UI", 10F);
            lblRenderPatternType.Location = new Point(0, 0);
            lblRenderPatternType.Margin = new Padding(0, 0, 0, 5);
            lblRenderPatternType.Name = "lblRenderPatternType";
            lblRenderPatternType.Size = new Size(308, 19);
            lblRenderPatternType.TabIndex = 1;
            lblRenderPatternType.Text = "Тип рендера:";
            // 
            // cbRenderPattern
            // 
            cbRenderPattern.Dock = DockStyle.Top;
            cbRenderPattern.DropDownStyle = ComboBoxStyle.DropDownList;
            cbRenderPattern.Font = new Font("Segoe UI", 11F);
            cbRenderPattern.FormattingEnabled = true;
            cbRenderPattern.Location = new Point(0, 24);
            cbRenderPattern.Margin = new Padding(0);
            cbRenderPattern.Name = "cbRenderPattern";
            cbRenderPattern.Size = new Size(308, 28);
            cbRenderPattern.TabIndex = 2;
            // 
            // btnLaunchSelected
            // 
            btnLaunchSelected.BackColor = SystemColors.Highlight;
            btnLaunchSelected.Dock = DockStyle.Fill;
            btnLaunchSelected.Enabled = false;
            btnLaunchSelected.FlatAppearance.BorderSize = 0;
            btnLaunchSelected.FlatStyle = FlatStyle.Flat;
            btnLaunchSelected.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnLaunchSelected.ForeColor = Color.White;
            btnLaunchSelected.Location = new Point(0, 157);
            btnLaunchSelected.Margin = new Padding(0, 5, 0, 0);
            btnLaunchSelected.Name = "btnLaunchSelected";
            btnLaunchSelected.Size = new Size(308, 45);
            btnLaunchSelected.TabIndex = 3;
            btnLaunchSelected.Text = "Запустить";
            btnLaunchSelected.UseVisualStyleBackColor = false;
            btnLaunchSelected.Click += btnLaunchSelected_Click;
            // 
            // richTextBoxDescription
            // 
            richTextBoxDescription.BackColor = SystemColors.Control;
            richTextBoxDescription.BorderStyle = BorderStyle.None;
            pnlDetails.SetColumnSpan(richTextBoxDescription, 2);
            richTextBoxDescription.Dock = DockStyle.Fill;
            richTextBoxDescription.Font = new Font("Segoe UI", 11F);
            richTextBoxDescription.ForeColor = Color.FromArgb(60, 60, 60);
            richTextBoxDescription.Location = new Point(15, 277);
            richTextBoxDescription.Margin = new Padding(0, 15, 0, 0);
            richTextBoxDescription.Name = "richTextBoxDescription";
            richTextBoxDescription.ReadOnly = true;
            richTextBoxDescription.Size = new Size(528, 229);
            richTextBoxDescription.TabIndex = 2;
            richTextBoxDescription.Text = "";
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
            pnlDetails.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).EndInit();
            settingsPanel.ResumeLayout(false);
            settingsPanel.PerformLayout();
            ResumeLayout(false);

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