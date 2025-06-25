namespace FractalExplorer.Forms // Убедись, что namespace соответствует твоему
{
    partial class SaveLoadDialogForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBoxSaves = new System.Windows.Forms.ListBox();
            this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
            this.textBoxSaveName = new System.Windows.Forms.TextBox();
            this.btnLoad = new System.Windows.Forms.Button();
            this.btnSaveAsNew = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.labelSavesList = new System.Windows.Forms.Label();
            this.labelPreview = new System.Windows.Forms.Label();
            this.labelSaveName = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // listBoxSaves
            // 
            this.listBoxSaves.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBoxSaves.FormattingEnabled = true;
            this.listBoxSaves.ItemHeight = 15;
            this.listBoxSaves.Location = new System.Drawing.Point(12, 35);
            this.listBoxSaves.Name = "listBoxSaves";
            this.listBoxSaves.Size = new System.Drawing.Size(280, 289);
            this.listBoxSaves.TabIndex = 0;
            this.listBoxSaves.SelectedIndexChanged += new System.EventHandler(this.listBoxSaves_SelectedIndexChanged);
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxPreview.Location = new System.Drawing.Point(307, 35);
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.Size = new System.Drawing.Size(365, 244);
            this.pictureBoxPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxPreview.TabIndex = 1;
            this.pictureBoxPreview.TabStop = false;
            // 
            // textBoxSaveName
            // 
            this.textBoxSaveName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSaveName.Location = new System.Drawing.Point(307, 301);
            this.textBoxSaveName.Name = "textBoxSaveName";
            this.textBoxSaveName.Size = new System.Drawing.Size(256, 23);
            this.textBoxSaveName.TabIndex = 2;
            // 
            // btnLoad
            // 
            this.btnLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLoad.Location = new System.Drawing.Point(497, 330);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(85, 28);
            this.btnLoad.TabIndex = 4;
            this.btnLoad.Text = "Загрузить";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // btnSaveAsNew
            // 
            this.btnSaveAsNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveAsNew.Location = new System.Drawing.Point(569, 300);
            this.btnSaveAsNew.Name = "btnSaveAsNew";
            this.btnSaveAsNew.Size = new System.Drawing.Size(103, 24);
            this.btnSaveAsNew.TabIndex = 3;
            this.btnSaveAsNew.Text = "Сохранить как";
            this.btnSaveAsNew.UseVisualStyleBackColor = true;
            this.btnSaveAsNew.Click += new System.EventHandler(this.btnSaveAsNew_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDelete.Location = new System.Drawing.Point(12, 330);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(85, 28);
            this.btnDelete.TabIndex = 1;
            this.btnDelete.Text = "Удалить";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(588, 330);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(84, 28);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // labelSavesList
            // 
            this.labelSavesList.AutoSize = true;
            this.labelSavesList.Location = new System.Drawing.Point(12, 13);
            this.labelSavesList.Name = "labelSavesList";
            this.labelSavesList.Size = new System.Drawing.Size(119, 15);
            this.labelSavesList.TabIndex = 7;
            this.labelSavesList.Text = "Список сохранений:";
            // 
            // labelPreview
            // 
            this.labelPreview.AutoSize = true;
            this.labelPreview.Location = new System.Drawing.Point(307, 13);
            this.labelPreview.Name = "labelPreview";
            this.labelPreview.Size = new System.Drawing.Size(81, 15);
            this.labelPreview.TabIndex = 8;
            this.labelPreview.Text = "Предпросмотр:";
            // 
            // labelSaveName
            // 
            this.labelSaveName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelSaveName.AutoSize = true;
            this.labelSaveName.Location = new System.Drawing.Point(307, 283);
            this.labelSaveName.Name = "labelSaveName";
            this.labelSaveName.Size = new System.Drawing.Size(103, 15);
            this.labelSaveName.TabIndex = 9;
            this.labelSaveName.Text = "Имя сохранения:";
            // 
            // SaveLoadDialogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 369);
            this.Controls.Add(this.labelSaveName);
            this.Controls.Add(this.labelPreview);
            this.Controls.Add(this.labelSavesList);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnSaveAsNew);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.textBoxSaveName);
            this.Controls.Add(this.pictureBoxPreview);
            this.Controls.Add(this.listBoxSaves);
            this.MinimumSize = new System.Drawing.Size(550, 350);
            this.Name = "SaveLoadDialogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Менеджер сохранений";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SaveLoadDialogForm_FormClosing);
            this.Load += new System.EventHandler(this.SaveLoadDialogForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxSaves;
        private System.Windows.Forms.PictureBox pictureBoxPreview;
        private System.Windows.Forms.TextBox textBoxSaveName;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnSaveAsNew;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label labelSavesList;
        private System.Windows.Forms.Label labelPreview;
        private System.Windows.Forms.Label labelSaveName;
    }
}