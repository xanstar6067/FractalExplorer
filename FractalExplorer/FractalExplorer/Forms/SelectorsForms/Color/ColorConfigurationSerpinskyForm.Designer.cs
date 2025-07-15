namespace FractalExplorer.SelectorsForms
{
    partial class ColorConfigurationSerpinskyForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ColorConfigurationSerpinskyForm));
            groupBox1 = new GroupBox();
            lbPalettes = new ListBox();
            btnNew = new Button();
            btnDelete = new Button();
            groupBox2 = new GroupBox();
            panelBackgroundColor = new Panel();
            label4 = new Label();
            panelFractalColor = new Panel();
            label3 = new Label();
            label1 = new Label();
            txtName = new TextBox();
            btnClose = new Button();
            btnApply = new Button();
            btnSave = new Button();
            colorDialog1 = new ColorDialog();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBox1.Controls.Add(lbPalettes);
            groupBox1.Controls.Add(btnNew);
            groupBox1.Controls.Add(btnDelete);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(236, 269);
            groupBox1.TabIndex = 14;
            groupBox1.TabStop = false;
            groupBox1.Text = "Список палитр";
            // 
            // lbPalettes
            // 
            lbPalettes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lbPalettes.FormattingEnabled = true;
            lbPalettes.ItemHeight = 15;
            lbPalettes.Location = new Point(8, 22);
            lbPalettes.Name = "lbPalettes";
            lbPalettes.Size = new Size(220, 199);
            lbPalettes.TabIndex = 0;
            lbPalettes.SelectedIndexChanged += lbPalettes_SelectedIndexChanged;
            // 
            // btnNew
            // 
            btnNew.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNew.Location = new Point(8, 233);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(109, 30);
            btnNew.TabIndex = 8;
            btnNew.Text = "Новая";
            btnNew.UseVisualStyleBackColor = true;
            btnNew.Click += btnNew_Click;
            // 
            // btnDelete
            // 
            btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDelete.Location = new Point(123, 233);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(105, 30);
            btnDelete.TabIndex = 9;
            btnDelete.Text = "Удалить";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(panelBackgroundColor);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(panelFractalColor);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(txtName);
            groupBox2.Location = new Point(264, 12);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(278, 269);
            groupBox2.TabIndex = 15;
            groupBox2.TabStop = false;
            groupBox2.Text = "Редактор палитры";
            // 
            // panelBackgroundColor
            // 
            panelBackgroundColor.BorderStyle = BorderStyle.FixedSingle;
            panelBackgroundColor.Cursor = Cursors.Hand;
            panelBackgroundColor.Location = new Point(125, 114);
            panelBackgroundColor.Name = "panelBackgroundColor";
            panelBackgroundColor.Size = new Size(130, 23);
            panelBackgroundColor.TabIndex = 13;
            panelBackgroundColor.Click += panelBackgroundColor_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(15, 118);
            label4.Name = "label4";
            label4.Size = new Size(68, 15);
            label4.TabIndex = 12;
            label4.Text = "Цвет фона:";
            // 
            // panelFractalColor
            // 
            panelFractalColor.BorderStyle = BorderStyle.FixedSingle;
            panelFractalColor.Cursor = Cursors.Hand;
            panelFractalColor.Location = new Point(125, 80);
            panelFractalColor.Name = "panelFractalColor";
            panelFractalColor.Size = new Size(130, 23);
            panelFractalColor.TabIndex = 11;
            panelFractalColor.Click += panelFractalColor_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(15, 84);
            label3.Name = "label3";
            label3.Size = new Size(91, 15);
            label3.TabIndex = 10;
            label3.Text = "Цвет фрактала:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(15, 25);
            label1.Name = "label1";
            label1.Size = new Size(113, 15);
            label1.TabIndex = 9;
            label1.Text = "Название палитры:";
            // 
            // txtName
            // 
            txtName.Location = new Point(15, 43);
            txtName.Name = "txtName";
            txtName.Size = new Size(240, 23);
            txtName.TabIndex = 0;
            txtName.TextChanged += txtName_TextChanged;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(448, 298);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(94, 30);
            btnClose.TabIndex = 16;
            btnClose.Text = "Закрыть";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // btnApply
            // 
            btnApply.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnApply.Location = new Point(348, 298);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(94, 30);
            btnApply.TabIndex = 17;
            btnApply.Text = "Применить";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSave.Location = new Point(12, 298);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(155, 30);
            btnSave.TabIndex = 18;
            btnSave.Text = "Сохранить палитры";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // colorDialog1
            // 
            colorDialog1.AnyColor = true;
            colorDialog1.FullOpen = true;
            // 
            // ColorConfigurationSerpinskyForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(554, 340);
            Controls.Add(btnSave);
            Controls.Add(btnApply);
            Controls.Add(btnClose);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(570, 379);
            Name = "ColorConfigurationSerpinskyForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Настройка палитры (Серпинский)";
            Load += ColorConfigurationSerpinskyForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox lbPalettes;
        private System.Windows.Forms.Button btnNew;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Panel panelFractalColor;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Panel panelBackgroundColor;
        private System.Windows.Forms.Label label4;
    }
}