namespace FractalExplorer.Utilities
{
    partial class ColorConfigurationMandelbrotFamilyForm
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
            lbPalettes = new ListBox();
            panelPreview = new Panel();
            lbColorStops = new ListBox();
            btnAddColor = new Button();
            btnRemoveColor = new Button();
            btnEditColor = new Button();
            txtName = new TextBox();
            checkIsGradient = new CheckBox();
            btnNew = new Button();
            btnCopy = new Button();
            btnDelete = new Button();
            btnSave = new Button();
            btnApply = new Button();
            btnClose = new Button();
            colorDialog1 = new ColorDialog();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            checkAlignSteps = new CheckBox();
            nudGamma = new NumericUpDown();
            labelGamma = new Label();
            nudMaxColorIterations = new NumericUpDown();
            labelMaxColorIter = new Label();
            label2 = new Label();
            label1 = new Label();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudGamma).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudMaxColorIterations).BeginInit();
            SuspendLayout();
            // 
            // lbPalettes
            // 
            lbPalettes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            lbPalettes.FormattingEnabled = true;
            lbPalettes.ItemHeight = 15;
            lbPalettes.Location = new Point(7, 22);
            lbPalettes.Name = "lbPalettes";
            lbPalettes.Size = new Size(193, 364);
            lbPalettes.TabIndex = 0;
            lbPalettes.SelectedIndexChanged += lbPalettes_SelectedIndexChanged;
            // 
            // panelPreview
            // 
            panelPreview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelPreview.BorderStyle = BorderStyle.FixedSingle;
            panelPreview.Location = new Point(8, 74);
            panelPreview.Name = "panelPreview";
            panelPreview.Size = new Size(411, 38);
            panelPreview.TabIndex = 1;
            panelPreview.Paint += panelPreview_Paint;
            // 
            // lbColorStops
            // 
            lbColorStops.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lbColorStops.FormattingEnabled = true;
            lbColorStops.ItemHeight = 15;
            lbColorStops.Location = new Point(8, 223);
            lbColorStops.Name = "lbColorStops";
            lbColorStops.Size = new Size(299, 169);
            lbColorStops.TabIndex = 2;
            // 
            // btnAddColor
            // 
            btnAddColor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddColor.Location = new Point(312, 223);
            btnAddColor.Name = "btnAddColor";
            btnAddColor.Size = new Size(107, 28);
            btnAddColor.TabIndex = 3;
            btnAddColor.Text = "Добавить...";
            btnAddColor.UseVisualStyleBackColor = true;
            btnAddColor.Click += btnAddColor_Click;
            // 
            // btnRemoveColor
            // 
            btnRemoveColor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRemoveColor.Location = new Point(312, 291);
            btnRemoveColor.Name = "btnRemoveColor";
            btnRemoveColor.Size = new Size(107, 28);
            btnRemoveColor.TabIndex = 5;
            btnRemoveColor.Text = "Удалить цвет";
            btnRemoveColor.UseVisualStyleBackColor = true;
            btnRemoveColor.Click += btnRemoveColor_Click;
            // 
            // btnEditColor
            // 
            btnEditColor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnEditColor.Location = new Point(312, 257);
            btnEditColor.Name = "btnEditColor";
            btnEditColor.Size = new Size(107, 28);
            btnEditColor.TabIndex = 4;
            btnEditColor.Text = "Изменить...";
            btnEditColor.UseVisualStyleBackColor = true;
            btnEditColor.Click += btnEditColor_Click;
            // 
            // txtName
            // 
            txtName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtName.Location = new Point(8, 38);
            txtName.Name = "txtName";
            txtName.Size = new Size(299, 23);
            txtName.TabIndex = 6;
            txtName.TextChanged += txtName_TextChanged;
            txtName.KeyDown += txtName_KeyDown;
            txtName.Leave += txtName_LostFocus;
            // 
            // checkIsGradient
            // 
            checkIsGradient.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            checkIsGradient.AutoSize = true;
            checkIsGradient.Location = new Point(315, 40);
            checkIsGradient.Name = "checkIsGradient";
            checkIsGradient.Size = new Size(76, 19);
            checkIsGradient.TabIndex = 7;
            checkIsGradient.Text = "Градиент";
            checkIsGradient.UseVisualStyleBackColor = true;
            checkIsGradient.CheckedChanged += checkIsGradient_CheckedChanged;
            // 
            // btnNew
            // 
            btnNew.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNew.Location = new Point(7, 391);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(61, 28);
            btnNew.TabIndex = 8;
            btnNew.Text = "Новая";
            btnNew.UseVisualStyleBackColor = true;
            btnNew.Click += btnNew_Click;
            // 
            // btnCopy
            // 
            btnCopy.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCopy.Location = new Point(74, 391);
            btnCopy.Name = "btnCopy";
            btnCopy.Size = new Size(88, 28);
            btnCopy.TabIndex = 15;
            btnCopy.Text = "Копировать";
            btnCopy.UseVisualStyleBackColor = true;
            btnCopy.Click += btnCopy_Click;
            // 
            // btnDelete
            // 
            btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDelete.Location = new Point(166, 391);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(33, 28);
            btnDelete.TabIndex = 9;
            btnDelete.Text = "X";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSave.Location = new Point(10, 454);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(149, 28);
            btnSave.TabIndex = 10;
            btnSave.Text = "Сохранить палитры";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnApply
            // 
            btnApply.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnApply.Location = new Point(438, 454);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(105, 28);
            btnApply.TabIndex = 11;
            btnApply.Text = "Применить";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(548, 454);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(105, 28);
            btnClose.TabIndex = 12;
            btnClose.Text = "Закрыть";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // colorDialog1
            // 
            colorDialog1.AnyColor = true;
            colorDialog1.FullOpen = true;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBox1.Controls.Add(lbPalettes);
            groupBox1.Controls.Add(btnNew);
            groupBox1.Controls.Add(btnCopy);
            groupBox1.Controls.Add(btnDelete);
            groupBox1.Location = new Point(10, 11);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(206, 428);
            groupBox1.TabIndex = 13;
            groupBox1.TabStop = false;
            groupBox1.Text = "Список палитр";
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(checkAlignSteps);
            groupBox2.Controls.Add(nudGamma);
            groupBox2.Controls.Add(labelGamma);
            groupBox2.Controls.Add(nudMaxColorIterations);
            groupBox2.Controls.Add(labelMaxColorIter);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(txtName);
            groupBox2.Controls.Add(panelPreview);
            groupBox2.Controls.Add(lbColorStops);
            groupBox2.Controls.Add(btnAddColor);
            groupBox2.Controls.Add(checkIsGradient);
            groupBox2.Controls.Add(btnEditColor);
            groupBox2.Controls.Add(btnRemoveColor);
            groupBox2.Location = new Point(234, 11);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(424, 428);
            groupBox2.TabIndex = 14;
            groupBox2.TabStop = false;
            groupBox2.Text = "Редактор палитры";
            // 
            // checkAlignSteps
            // 
            checkAlignSteps.AutoSize = true;
            checkAlignSteps.Location = new Point(118, 134);
            checkAlignSteps.Name = "checkAlignSteps";
            checkAlignSteps.Size = new Size(141, 19);
            checkAlignSteps.TabIndex = 14;
            checkAlignSteps.Text = "Как в старые добрые";
            checkAlignSteps.UseVisualStyleBackColor = true;
            checkAlignSteps.CheckedChanged += checkAlignSteps_CheckedChanged;
            // 
            // nudGamma
            // 
            nudGamma.DecimalPlaces = 2;
            nudGamma.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            nudGamma.Location = new Point(8, 179);
            nudGamma.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            nudGamma.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            nudGamma.Name = "nudGamma";
            nudGamma.Size = new Size(105, 23);
            nudGamma.TabIndex = 13;
            nudGamma.Value = new decimal(new int[] { 1, 0, 0, 0 });
            nudGamma.ValueChanged += nudGamma_ValueChanged;
            // 
            // labelGamma
            // 
            labelGamma.AutoSize = true;
            labelGamma.Location = new Point(5, 161);
            labelGamma.Name = "labelGamma";
            labelGamma.Size = new Size(110, 15);
            labelGamma.TabIndex = 12;
            labelGamma.Text = "Гамма-коррекция:";
            // 
            // nudMaxColorIterations
            // 
            nudMaxColorIterations.Location = new Point(8, 133);
            nudMaxColorIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudMaxColorIterations.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            nudMaxColorIterations.Name = "nudMaxColorIterations";
            nudMaxColorIterations.Size = new Size(105, 23);
            nudMaxColorIterations.TabIndex = 11;
            nudMaxColorIterations.Value = new decimal(new int[] { 500, 0, 0, 0 });
            nudMaxColorIterations.ValueChanged += nudMaxColorIterations_ValueChanged;
            // 
            // labelMaxColorIter
            // 
            labelMaxColorIter.AutoSize = true;
            labelMaxColorIter.Location = new Point(5, 115);
            labelMaxColorIter.Name = "labelMaxColorIter";
            labelMaxColorIter.Size = new Size(114, 15);
            labelMaxColorIter.TabIndex = 10;
            labelMaxColorIter.Text = "Длина цикла цвета:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(5, 205);
            label2.Name = "label2";
            label2.Size = new Size(101, 15);
            label2.TabIndex = 9;
            label2.Text = "Ключевые цвета:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(5, 21);
            label1.Name = "label1";
            label1.Size = new Size(113, 15);
            label1.TabIndex = 8;
            label1.Text = "Название палитры:";
            // 
            // ColorConfigurationMandelbrotFamilyForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(669, 501);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(btnSave);
            Controls.Add(btnClose);
            Controls.Add(btnApply);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(685, 540);
            Name = "ColorConfigurationMandelbrotFamilyForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Настройка цветовых палитр";
            Load += ColorConfigurationForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudGamma).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudMaxColorIterations).EndInit();
            ResumeLayout(false);
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
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox checkAlignSteps;
        private System.Windows.Forms.NumericUpDown nudGamma;
        private System.Windows.Forms.Label labelGamma;
        private System.Windows.Forms.NumericUpDown nudMaxColorIterations;
        private System.Windows.Forms.Label labelMaxColorIter;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
    }
}