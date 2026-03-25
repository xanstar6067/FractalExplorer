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
            groupBoxPalettes = new GroupBox();
            btnNew = new Button();
            btnDelete = new Button();
            btnCopy = new Button();
            lbPalettes = new ListBox();
            groupBoxEditor = new GroupBox();
            lblEditHint = new Label();
            panelBackgroundColor = new Panel();
            labelBackgroundColor = new Label();
            panelFractalColor = new Panel();
            labelFractalColor = new Label();
            txtName = new TextBox();
            labelName = new Label();
            btnSave = new Button();
            btnApply = new Button();
            btnClose = new Button();
            groupBoxPalettes.SuspendLayout();
            groupBoxEditor.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxPalettes
            // 
            groupBoxPalettes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBoxPalettes.Controls.Add(btnNew);
            groupBoxPalettes.Controls.Add(btnDelete);
            groupBoxPalettes.Controls.Add(btnCopy);
            groupBoxPalettes.Controls.Add(lbPalettes);
            groupBoxPalettes.Location = new Point(12, 12);
            groupBoxPalettes.Name = "groupBoxPalettes";
            groupBoxPalettes.Size = new Size(238, 437);
            groupBoxPalettes.TabIndex = 0;
            groupBoxPalettes.TabStop = false;
            groupBoxPalettes.Text = "Список палитр";
            // 
            // btnNew
            // 
            btnNew.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNew.Location = new Point(7, 400);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(61, 28);
            btnNew.TabIndex = 1;
            btnNew.Text = "Новая";
            btnNew.UseVisualStyleBackColor = true;
            btnNew.Click += btnNew_Click;
            // 
            // btnDelete
            // 
            btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDelete.Location = new Point(168, 400);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(63, 28);
            btnDelete.TabIndex = 3;
            btnDelete.Text = "Удалить";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnCopy
            // 
            btnCopy.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCopy.Location = new Point(74, 400);
            btnCopy.Name = "btnCopy";
            btnCopy.Size = new Size(88, 28);
            btnCopy.TabIndex = 2;
            btnCopy.Text = "Копировать";
            btnCopy.UseVisualStyleBackColor = true;
            btnCopy.Click += btnCopy_Click;
            // 
            // lbPalettes
            // 
            lbPalettes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lbPalettes.FormattingEnabled = true;
            lbPalettes.Location = new Point(7, 22);
            lbPalettes.Name = "lbPalettes";
            lbPalettes.Size = new Size(224, 364);
            lbPalettes.TabIndex = 0;
            lbPalettes.SelectedIndexChanged += lbPalettes_SelectedIndexChanged;
            // 
            // groupBoxEditor
            // 
            groupBoxEditor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxEditor.Controls.Add(lblEditHint);
            groupBoxEditor.Controls.Add(panelBackgroundColor);
            groupBoxEditor.Controls.Add(labelBackgroundColor);
            groupBoxEditor.Controls.Add(panelFractalColor);
            groupBoxEditor.Controls.Add(labelFractalColor);
            groupBoxEditor.Controls.Add(txtName);
            groupBoxEditor.Controls.Add(labelName);
            groupBoxEditor.Location = new Point(272, 12);
            groupBoxEditor.Name = "groupBoxEditor";
            groupBoxEditor.Size = new Size(385, 437);
            groupBoxEditor.TabIndex = 1;
            groupBoxEditor.TabStop = false;
            groupBoxEditor.Text = "Редактор палитры";
            // 
            // lblEditHint
            // 
            lblEditHint.AutoSize = true;
            lblEditHint.Location = new Point(9, 166);
            lblEditHint.Name = "lblEditHint";
            lblEditHint.Size = new Size(182, 15);
            lblEditHint.TabIndex = 6;
            lblEditHint.Text = "Текущая палитра редактируема";
            // 
            // panelBackgroundColor
            // 
            panelBackgroundColor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelBackgroundColor.Cursor = Cursors.Hand;
            panelBackgroundColor.Location = new Point(120, 121);
            panelBackgroundColor.Name = "panelBackgroundColor";
            panelBackgroundColor.Size = new Size(256, 28);
            panelBackgroundColor.TabIndex = 5;
            panelBackgroundColor.Click += panelBackgroundColor_Click;
            panelBackgroundColor.Paint += panelBackgroundColor_Paint;
            panelBackgroundColor.MouseEnter += panelBackgroundColor_MouseEnter;
            panelBackgroundColor.MouseLeave += panelBackgroundColor_MouseLeave;
            // 
            // labelBackgroundColor
            // 
            labelBackgroundColor.AutoSize = true;
            labelBackgroundColor.Location = new Point(9, 128);
            labelBackgroundColor.Name = "labelBackgroundColor";
            labelBackgroundColor.Size = new Size(68, 15);
            labelBackgroundColor.TabIndex = 4;
            labelBackgroundColor.Text = "Цвет фона:";
            // 
            // panelFractalColor
            // 
            panelFractalColor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelFractalColor.Cursor = Cursors.Hand;
            panelFractalColor.Location = new Point(120, 82);
            panelFractalColor.Name = "panelFractalColor";
            panelFractalColor.Size = new Size(256, 28);
            panelFractalColor.TabIndex = 3;
            panelFractalColor.Click += panelFractalColor_Click;
            panelFractalColor.Paint += panelFractalColor_Paint;
            panelFractalColor.MouseEnter += panelFractalColor_MouseEnter;
            panelFractalColor.MouseLeave += panelFractalColor_MouseLeave;
            // 
            // labelFractalColor
            // 
            labelFractalColor.AutoSize = true;
            labelFractalColor.Location = new Point(9, 89);
            labelFractalColor.Name = "labelFractalColor";
            labelFractalColor.Size = new Size(91, 15);
            labelFractalColor.TabIndex = 2;
            labelFractalColor.Text = "Цвет фрактала:";
            // 
            // txtName
            // 
            txtName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtName.Location = new Point(9, 37);
            txtName.Name = "txtName";
            txtName.Size = new Size(367, 23);
            txtName.TabIndex = 1;
            txtName.TextChanged += txtName_TextChanged;
            txtName.KeyDown += txtName_KeyDown;
            txtName.Leave += txtName_Leave;
            // 
            // labelName
            // 
            labelName.AutoSize = true;
            labelName.Location = new Point(9, 19);
            labelName.Name = "labelName";
            labelName.Size = new Size(113, 15);
            labelName.TabIndex = 0;
            labelName.Text = "Название палитры:";
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSave.Location = new Point(19, 461);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(149, 28);
            btnSave.TabIndex = 2;
            btnSave.Text = "Сохранить изменения";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnApply
            // 
            btnApply.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnApply.Location = new Point(432, 461);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(107, 28);
            btnApply.TabIndex = 3;
            btnApply.Text = "Применить";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(545, 461);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(107, 28);
            btnClose.TabIndex = 4;
            btnClose.Text = "Закрыть";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // ColorConfigurationSerpinskyForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(669, 501);
            Controls.Add(btnClose);
            Controls.Add(btnApply);
            Controls.Add(btnSave);
            Controls.Add(groupBoxEditor);
            Controls.Add(groupBoxPalettes);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(685, 540);
            Name = "ColorConfigurationSerpinskyForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Настройка палитры (Серпинский)";
            Load += ColorConfigurationSerpinskyForm_Load;
            groupBoxPalettes.ResumeLayout(false);
            groupBoxEditor.ResumeLayout(false);
            groupBoxEditor.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private GroupBox groupBoxPalettes;
        private Button btnNew;
        private Button btnDelete;
        private Button btnCopy;
        private ListBox lbPalettes;
        private GroupBox groupBoxEditor;
        private Label lblEditHint;
        private Panel panelBackgroundColor;
        private Label labelBackgroundColor;
        private Panel panelFractalColor;
        private Label labelFractalColor;
        private TextBox txtName;
        private Label labelName;
        private Button btnSave;
        private Button btnApply;
        private Button btnClose;
    }
}
