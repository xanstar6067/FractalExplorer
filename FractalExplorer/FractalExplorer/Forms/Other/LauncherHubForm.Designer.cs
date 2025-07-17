﻿namespace FractalExplorer
{
    partial class LauncherHubForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LauncherHubForm));
            splitContainerMain = new SplitContainer();
            treeViewFractals = new TreeView();
            pnlDetails = new TableLayoutPanel();
            lblFractalName = new Label();
            pictureBoxPreview = new PictureBox();
            richTextBoxDescription = new RichTextBox();
            btnLaunchSelected = new Button();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            pnlDetails.SuspendLayout();
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
            splitContainerMain.Size = new Size(800, 450);
            splitContainerMain.SplitterDistance = 270;
            splitContainerMain.TabIndex = 0;
            // 
            // treeViewFractals
            // 
            treeViewFractals.Dock = DockStyle.Fill;
            treeViewFractals.Font = new Font("Segoe UI", 11F);
            treeViewFractals.Location = new Point(0, 0);
            treeViewFractals.Name = "treeViewFractals";
            treeViewFractals.Size = new Size(270, 450);
            treeViewFractals.TabIndex = 0;
            treeViewFractals.AfterSelect += treeViewFractals_AfterSelect;
            // 
            // pnlDetails
            // 
            pnlDetails.ColumnCount = 1;
            pnlDetails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlDetails.Controls.Add(lblFractalName, 0, 0);
            pnlDetails.Controls.Add(pictureBoxPreview, 0, 1);
            pnlDetails.Controls.Add(richTextBoxDescription, 0, 2);
            pnlDetails.Controls.Add(btnLaunchSelected, 0, 3);
            pnlDetails.Dock = DockStyle.Fill;
            pnlDetails.Location = new Point(0, 0);
            pnlDetails.Name = "pnlDetails";
            pnlDetails.Padding = new Padding(5);
            pnlDetails.RowCount = 4;
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            pnlDetails.Size = new Size(526, 450);
            pnlDetails.TabIndex = 0;
            // 
            // lblFractalName
            // 
            lblFractalName.AutoSize = true;
            lblFractalName.Dock = DockStyle.Fill;
            lblFractalName.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            lblFractalName.Location = new Point(8, 5);
            lblFractalName.Name = "lblFractalName";
            lblFractalName.Size = new Size(510, 40);
            lblFractalName.TabIndex = 0;
            lblFractalName.Text = "Выберите фрактал";
            lblFractalName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pictureBoxPreview
            // 
            pictureBoxPreview.Dock = DockStyle.Fill;
            pictureBoxPreview.Location = new Point(8, 48);
            pictureBoxPreview.Name = "pictureBoxPreview";
            pictureBoxPreview.Size = new Size(510, 169);
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
            richTextBoxDescription.Location = new Point(8, 223);
            richTextBoxDescription.Name = "richTextBoxDescription";
            richTextBoxDescription.ReadOnly = true;
            richTextBoxDescription.Size = new Size(510, 169);
            richTextBoxDescription.TabIndex = 2;
            richTextBoxDescription.Text = "";
            // 
            // btnLaunchSelected
            // 
            btnLaunchSelected.Dock = DockStyle.Fill;
            btnLaunchSelected.Enabled = false;
            btnLaunchSelected.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnLaunchSelected.Location = new Point(8, 398);
            btnLaunchSelected.Name = "btnLaunchSelected";
            btnLaunchSelected.Size = new Size(510, 44);
            btnLaunchSelected.TabIndex = 3;
            btnLaunchSelected.Text = "Запустить";
            btnLaunchSelected.UseVisualStyleBackColor = true;
            btnLaunchSelected.Click += btnLaunchSelected_Click;
            // 
            // LauncherHubForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(splitContainerMain);
            Icon = (Icon)resources.GetObject("$this.Icon");
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
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.TreeView treeViewFractals;
        private System.Windows.Forms.TableLayoutPanel pnlDetails;
        private System.Windows.Forms.Label lblFractalName;
        private System.Windows.Forms.PictureBox pictureBoxPreview;
        private System.Windows.Forms.RichTextBox richTextBoxDescription;
        private System.Windows.Forms.Button btnLaunchSelected;
    }
}