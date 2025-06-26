namespace FractalExplorer.Utilities
{
    partial class ColorConfigurationMandelbrotFamilyForm
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
            this.components = new System.ComponentModel.Container();
            this.lbPalettes = new System.Windows.Forms.ListBox();
            this.panelPreview = new System.Windows.Forms.Panel();
            this.lbColorStops = new System.Windows.Forms.ListBox();
            this.btnAddColor = new System.Windows.Forms.Button();
            this.btnRemoveColor = new System.Windows.Forms.Button();
            this.btnEditColor = new System.Windows.Forms.Button();
            this.txtName = new System.Windows.Forms.TextBox();
            this.checkIsGradient = new System.Windows.Forms.CheckBox();
            this.btnNew = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button(); // Переименовано
            this.btnClose = new System.Windows.Forms.Button(); // Переименовано
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            //
            // lbPalettes
            //
            this.lbPalettes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbPalettes.FormattingEnabled = true;
            this.lbPalettes.ItemHeight = 16;
            this.lbPalettes.Location = new System.Drawing.Point(8, 23);
            this.lbPalettes.Name = "lbPalettes";
            this.lbPalettes.Size = new System.Drawing.Size(220, 324);
            this.lbPalettes.TabIndex = 0;
            this.lbPalettes.SelectedIndexChanged += new System.EventHandler(this.lbPalettes_SelectedIndexChanged);
            //
            // panelPreview
            //
            this.panelPreview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelPreview.Location = new System.Drawing.Point(9, 79);
            this.panelPreview.Name = "panelPreview";
            this.panelPreview.Size = new System.Drawing.Size(469, 40);
            this.panelPreview.TabIndex = 1;
            this.panelPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.panelPreview_Paint);
            //
            // lbColorStops
            //
            this.lbColorStops.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbColorStops.FormattingEnabled = true;
            this.lbColorStops.ItemHeight = 16;
            this.lbColorStops.Location = new System.Drawing.Point(9, 149);
            this.lbColorStops.Name = "lbColorStops";
            this.lbColorStops.Size = new System.Drawing.Size(341, 196);
            this.lbColorStops.TabIndex = 2;
            //
            // btnAddColor
            //
            this.btnAddColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddColor.Location = new System.Drawing.Point(356, 149);
            this.btnAddColor.Name = "btnAddColor";
            this.btnAddColor.Size = new System.Drawing.Size(122, 30);
            this.btnAddColor.TabIndex = 3;
            this.btnAddColor.Text = "Добавить...";
            this.btnAddColor.UseVisualStyleBackColor = true;
            this.btnAddColor.Click += new System.EventHandler(this.btnAddColor_Click);
            //
            // btnRemoveColor
            //
            this.btnRemoveColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemoveColor.Location = new System.Drawing.Point(356, 221);
            this.btnRemoveColor.Name = "btnRemoveColor";
            this.btnRemoveColor.Size = new System.Drawing.Size(122, 30);
            this.btnRemoveColor.TabIndex = 5;
            this.btnRemoveColor.Text = "Удалить цвет";
            this.btnRemoveColor.UseVisualStyleBackColor = true;
            this.btnRemoveColor.Click += new System.EventHandler(this.btnRemoveColor_Click);
            //
            // btnEditColor
            //
            this.btnEditColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEditColor.Location = new System.Drawing.Point(356, 185);
            this.btnEditColor.Name = "btnEditColor";
            this.btnEditColor.Size = new System.Drawing.Size(122, 30);
            this.btnEditColor.TabIndex = 4;
            this.btnEditColor.Text = "Изменить...";
            this.btnEditColor.UseVisualStyleBackColor = true;
            this.btnEditColor.Click += new System.EventHandler(this.btnEditColor_Click);
            //
            // txtName
            //
            this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtName.Location = new System.Drawing.Point(9, 41);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(341, 22);
            this.txtName.TabIndex = 6;
            this.txtName.TextChanged += new System.EventHandler(this.txtName_TextChanged);
            //
            // checkIsGradient
            //
            this.checkIsGradient.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkIsGradient.AutoSize = true;
            this.checkIsGradient.Location = new System.Drawing.Point(356, 43);
            this.checkIsGradient.Name = "checkIsGradient";
            this.checkIsGradient.Size = new System.Drawing.Size(91, 20);
            this.checkIsGradient.TabIndex = 7;
            this.checkIsGradient.Text = "Градиент";
            this.checkIsGradient.UseVisualStyleBackColor = true;
            this.checkIsGradient.CheckedChanged += new System.EventHandler(this.checkIsGradient_CheckedChanged);
            //
            // btnNew
            //
            this.btnNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnNew.Location = new System.Drawing.Point(8, 353);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(109, 30);
            this.btnNew.TabIndex = 8;
            this.btnNew.Text = "Новая";
            this.btnNew.UseVisualStyleBackColor = true;
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            //
            // btnDelete
            //
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDelete.Location = new System.Drawing.Point(123, 353);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(105, 30);
            this.btnDelete.TabIndex = 9;
            this.btnDelete.Text = "Удалить";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            //
            // btnSave
            //
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSave.Location = new System.Drawing.Point(12, 420);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(170, 30);
            this.btnSave.TabIndex = 10;
            this.btnSave.Text = "Сохранить палитры";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnApply
            //
            this.btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApply.Location = new System.Drawing.Point(500, 420);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(120, 30);
            this.btnApply.TabIndex = 11;
            this.btnApply.Text = "Применить";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click); // ИЗМЕНЕНО
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(626, 420);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(120, 30);
            this.btnClose.TabIndex = 12;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click); // ИЗМЕНЕНО
            //
            // colorDialog1
            //
            this.colorDialog1.AnyColor = true;
            this.colorDialog1.FullOpen = true;
            //
            // groupBox1
            //
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox1.Controls.Add(this.lbPalettes);
            this.groupBox1.Controls.Add(this.btnNew);
            this.groupBox1.Controls.Add(this.btnDelete);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(236, 393);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Список палитр";
            //
            // groupBox2
            //
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.txtName);
            this.groupBox2.Controls.Add(this.panelPreview);
            this.groupBox2.Controls.Add(this.lbColorStops);
            this.groupBox2.Controls.Add(this.btnAddColor);
            this.groupBox2.Controls.Add(this.checkIsGradient);
            this.groupBox2.Controls.Add(this.btnEditColor);
            this.groupBox2.Controls.Add(this.btnRemoveColor);
            this.groupBox2.Location = new System.Drawing.Point(267, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(484, 393);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Редактор палитры";
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 130);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(115, 16);
            this.label2.TabIndex = 9;
            this.label2.Text = "Ключевые цвета:";
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(129, 16);
            this.label1.TabIndex = 8;
            this.label1.Text = "Название палитры:";
            //
            // ColorConfigurationForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(763, 462);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnClose); // ИЗМЕНЕНО
            this.Controls.Add(this.btnApply); // ИЗМЕНЕНО
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(781, 509);
            this.Name = "ColorConfigurationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Настройка цветовых палитр";
            this.Load += new System.EventHandler(this.ColorConfigurationForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox lbPalettes;
        private System.Windows.Forms.Panel panelPreview;
        private System.Windows.Forms.ListBox lbColorStops;
        private System.Windows.Forms.Button btnAddColor;
        private System.Windows.Forms.Button btnRemoveColor;
        private System.Windows.Forms.Button btnEditColor;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.CheckBox checkIsGradient;
        private System.Windows.Forms.Button btnNew;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnApply; // Переименовано
        private System.Windows.Forms.Button btnClose; // Переименовано
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
    }
}