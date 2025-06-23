namespace FractalExplorer
{
    partial class color_setting_NewtonPoolsForm
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
            components = new System.ComponentModel.Container();
            groupBox1 = new GroupBox();
            flpRootColorPickers = new FlowLayoutPanel();
            groupBox2 = new GroupBox();
            panelBackgroundColor = new Panel();
            label1 = new Label();
            btnSave = new Button();
            colorDialog1 = new ColorDialog();
            cbPalettes = new ComboBox();
            label2 = new Label();
            btnSaveAs = new Button();
            btnDelete = new Button();
            chkIsGradient = new CheckBox();
            toolTip1 = new ToolTip(components);
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(flpRootColorPickers);
            groupBox1.Location = new Point(12, 85);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(460, 169);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Цвета для корней";
            // 
            // flpRootColorPickers
            // 
            flpRootColorPickers.AutoScroll = true;
            flpRootColorPickers.Dock = DockStyle.Fill;
            flpRootColorPickers.Location = new Point(3, 19);
            flpRootColorPickers.Name = "flpRootColorPickers";
            flpRootColorPickers.Size = new Size(454, 147);
            flpRootColorPickers.TabIndex = 0;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(panelBackgroundColor);
            groupBox2.Controls.Add(label1);
            groupBox2.Location = new Point(12, 260);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(460, 58);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "Цвет фона";
            // 
            // panelBackgroundColor
            // 
            panelBackgroundColor.BorderStyle = BorderStyle.FixedSingle;
            panelBackgroundColor.Cursor = Cursors.Hand;
            panelBackgroundColor.Location = new Point(180, 22);
            panelBackgroundColor.Name = "panelBackgroundColor";
            panelBackgroundColor.Size = new Size(50, 23);
            panelBackgroundColor.TabIndex = 1;
            toolTip1.SetToolTip(panelBackgroundColor, "Нажмите, чтобы изменить цвет");
            panelBackgroundColor.Click += panelBackgroundColor_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 26);
            label1.Name = "label1";
            label1.Size = new Size(149, 15);
            label1.TabIndex = 0;
            label1.Text = "Цвет для \"далеких\" точек:";
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSave.Location = new Point(172, 56);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(88, 23);
            btnSave.TabIndex = 2;
            btnSave.Text = "Сохранить";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // cbPalettes
            // 
            cbPalettes.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cbPalettes.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPalettes.FormattingEnabled = true;
            cbPalettes.Location = new Point(15, 27);
            cbPalettes.Name = "cbPalettes";
            cbPalettes.Size = new Size(454, 23);
            cbPalettes.TabIndex = 3;
            cbPalettes.SelectedIndexChanged += cbPalettes_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(15, 9);
            label2.Name = "label2";
            label2.Size = new Size(105, 15);
            label2.TabIndex = 4;
            label2.Text = "Текущая палитра:";
            // 
            // btnSaveAs
            // 
            btnSaveAs.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSaveAs.Location = new Point(266, 56);
            btnSaveAs.Name = "btnSaveAs";
            btnSaveAs.Size = new Size(119, 23);
            btnSaveAs.TabIndex = 5;
            btnSaveAs.Text = "Сохранить как...";
            btnSaveAs.UseVisualStyleBackColor = true;
            btnSaveAs.Click += btnSaveAs_Click;
            // 
            // btnDelete
            // 
            btnDelete.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDelete.Location = new Point(391, 56);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(78, 23);
            btnDelete.TabIndex = 6;
            btnDelete.Text = "Удалить";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // chkIsGradient
            // 
            chkIsGradient.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            chkIsGradient.AutoSize = true;
            chkIsGradient.Location = new Point(15, 324);
            chkIsGradient.Name = "chkIsGradient";
            chkIsGradient.Size = new Size(267, 19);
            chkIsGradient.TabIndex = 7;
            chkIsGradient.Text = "Использовать градиент (иначе - дискретно)";
            toolTip1.SetToolTip(chkIsGradient, "Если включено, цвет бассейна будет плавно меняться в зависимости от числа итераций");
            chkIsGradient.UseVisualStyleBackColor = true;
            chkIsGradient.CheckedChanged += chkIsGradient_CheckedChanged;
            // 
            // color_setting_NewtonPoolsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(484, 351);
            Controls.Add(chkIsGradient);
            Controls.Add(btnDelete);
            Controls.Add(btnSaveAs);
            Controls.Add(label2);
            Controls.Add(cbPalettes);
            Controls.Add(btnSave);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            MaximizeBox = false;
            MinimumSize = new Size(500, 390);
            Name = "color_setting_NewtonPoolsForm";
            Text = "Настройка палитры для Бассейнов Ньютона";
            FormClosing += ColorSetting_FormClosing;
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.FlowLayoutPanel flpRootColorPickers;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Panel panelBackgroundColor;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.ComboBox cbPalettes;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnSaveAs;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.CheckBox chkIsGradient;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}