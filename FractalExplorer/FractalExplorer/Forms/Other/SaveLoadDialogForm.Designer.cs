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
            listBoxSaves = new ListBox();
            pictureBoxPreview = new PictureBox();
            textBoxSaveName = new TextBox();
            btnLoad = new Button();
            btnSaveAsNew = new Button();
            btnDelete = new Button();
            btnCancel = new Button();
            labelSavesList = new Label();
            labelPreview = new Label();
            labelSaveName = new Label();
            cbPresets = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).BeginInit();
            SuspendLayout();
            // 
            // listBoxSaves
            // 
            listBoxSaves.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            listBoxSaves.FormattingEnabled = true;
            listBoxSaves.ItemHeight = 15;
            listBoxSaves.Location = new Point(12, 35);
            listBoxSaves.Name = "listBoxSaves";
            listBoxSaves.Size = new Size(280, 289);
            listBoxSaves.TabIndex = 0;
            listBoxSaves.SelectedIndexChanged += listBoxSaves_SelectedIndexChanged;
            // 
            // pictureBoxPreview
            // 
            pictureBoxPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBoxPreview.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxPreview.Location = new Point(307, 35);
            pictureBoxPreview.Name = "pictureBoxPreview";
            pictureBoxPreview.Size = new Size(365, 244);
            pictureBoxPreview.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxPreview.TabIndex = 1;
            pictureBoxPreview.TabStop = false;
            // 
            // textBoxSaveName
            // 
            textBoxSaveName.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxSaveName.Location = new Point(307, 301);
            textBoxSaveName.Name = "textBoxSaveName";
            textBoxSaveName.Size = new Size(256, 23);
            textBoxSaveName.TabIndex = 2;
            // 
            // btnLoad
            // 
            btnLoad.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnLoad.Location = new Point(497, 330);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(85, 28);
            btnLoad.TabIndex = 4;
            btnLoad.Text = "Загрузить";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // btnSaveAsNew
            // 
            btnSaveAsNew.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSaveAsNew.Location = new Point(569, 300);
            btnSaveAsNew.Name = "btnSaveAsNew";
            btnSaveAsNew.Size = new Size(103, 24);
            btnSaveAsNew.TabIndex = 3;
            btnSaveAsNew.Text = "Сохранить";
            btnSaveAsNew.UseVisualStyleBackColor = true;
            btnSaveAsNew.Click += btnSaveAsNew_Click;
            // 
            // btnDelete
            // 
            btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnDelete.Location = new Point(12, 330);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(85, 28);
            btnDelete.TabIndex = 1;
            btnDelete.Text = "Удалить";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(588, 330);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(84, 28);
            btnCancel.TabIndex = 5;
            btnCancel.Text = "Отмена";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // labelSavesList
            // 
            labelSavesList.AutoSize = true;
            labelSavesList.Location = new Point(12, 13);
            labelSavesList.Name = "labelSavesList";
            labelSavesList.Size = new Size(120, 15);
            labelSavesList.TabIndex = 7;
            labelSavesList.Text = "Список сохранений:";
            // 
            // labelPreview
            // 
            labelPreview.AutoSize = true;
            labelPreview.Location = new Point(307, 13);
            labelPreview.Name = "labelPreview";
            labelPreview.Size = new Size(93, 15);
            labelPreview.TabIndex = 8;
            labelPreview.Text = "Предпросмотр:";
            // 
            // labelSaveName
            // 
            labelSaveName.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelSaveName.AutoSize = true;
            labelSaveName.Location = new Point(307, 283);
            labelSaveName.Name = "labelSaveName";
            labelSaveName.Size = new Size(102, 15);
            labelSaveName.TabIndex = 9;
            labelSaveName.Text = "Имя сохранения:";
            // 
            // cbPresets
            // 
            cbPresets.AutoSize = true;
            cbPresets.Location = new Point(180, 12);
            cbPresets.Name = "cbPresets";
            cbPresets.Size = new Size(112, 19);
            cbPresets.TabIndex = 10;
            cbPresets.Text = "Точки интереса";
            cbPresets.UseVisualStyleBackColor = true;
            // 
            // SaveLoadDialogForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(684, 369);
            Controls.Add(cbPresets);
            Controls.Add(labelSaveName);
            Controls.Add(labelPreview);
            Controls.Add(labelSavesList);
            Controls.Add(btnCancel);
            Controls.Add(btnDelete);
            Controls.Add(btnSaveAsNew);
            Controls.Add(btnLoad);
            Controls.Add(textBoxSaveName);
            Controls.Add(pictureBoxPreview);
            Controls.Add(listBoxSaves);
            MinimumSize = new Size(550, 350);
            Name = "SaveLoadDialogForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Менеджер сохранений";
            FormClosing += SaveLoadDialogForm_FormClosing;
            Load += SaveLoadDialogForm_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).EndInit();
            ResumeLayout(false);
            PerformLayout();

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
        private CheckBox cbPresets;
    }
}