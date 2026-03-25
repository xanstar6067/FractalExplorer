namespace FractalExplorer
{
    partial class ColorConfigurationNewtonPoolsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ColorConfigurationNewtonPoolsForm));
            groupBoxPalettes = new GroupBox();
            btnNew = new Button();
            btnDelete = new Button();
            btnSaveAs = new Button();
            lbPalettes = new ListBox();
            btnSave = new Button();
            groupBoxEditor = new GroupBox();
            lblEditHint = new Label();
            btnAutoAdjustRoots = new Button();
            lblRootCountValue = new Label();
            lblRootCountCaption = new Label();
            chkIsGradient = new CheckBox();
            btnApply = new Button();
            btnClose = new Button();
            btnEditColor = new Button();
            lbColorStops = new ListBox();
            labelStops = new Label();
            panelPreview = new Panel();
            labelPreview = new Label();
            txtName = new TextBox();
            labelName = new Label();
            toolTip1 = new ToolTip(components);
            groupBoxPalettes.SuspendLayout();
            groupBoxEditor.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxPalettes
            // 
            groupBoxPalettes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBoxPalettes.Controls.Add(btnNew);
            groupBoxPalettes.Controls.Add(btnDelete);
            groupBoxPalettes.Controls.Add(btnSaveAs);
            groupBoxPalettes.Controls.Add(lbPalettes);
            groupBoxPalettes.Location = new Point(12, 12);
            groupBoxPalettes.Name = "groupBoxPalettes";
            groupBoxPalettes.Size = new Size(238, 448);
            groupBoxPalettes.TabIndex = 0;
            groupBoxPalettes.TabStop = false;
            groupBoxPalettes.Text = "Список палитр";
            // 
            // btnNew
            // 
            btnNew.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNew.Location = new Point(7, 411);
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
            btnDelete.Location = new Point(168, 411);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(63, 28);
            btnDelete.TabIndex = 3;
            btnDelete.Text = "Удалить";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnSaveAs
            // 
            btnSaveAs.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSaveAs.Location = new Point(74, 411);
            btnSaveAs.Name = "btnSaveAs";
            btnSaveAs.Size = new Size(88, 28);
            btnSaveAs.TabIndex = 2;
            btnSaveAs.Text = "Копировать";
            btnSaveAs.UseVisualStyleBackColor = true;
            btnSaveAs.Click += btnSaveAs_Click;
            // 
            // lbPalettes
            // 
            lbPalettes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lbPalettes.FormattingEnabled = true;
            lbPalettes.Location = new Point(7, 22);
            lbPalettes.Name = "lbPalettes";
            lbPalettes.Size = new Size(224, 379);
            lbPalettes.TabIndex = 0;
            lbPalettes.SelectedIndexChanged += lbPalettes_SelectedIndexChanged;
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
            // groupBoxEditor
            // 
            groupBoxEditor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxEditor.Controls.Add(lblEditHint);
            groupBoxEditor.Controls.Add(btnAutoAdjustRoots);
            groupBoxEditor.Controls.Add(lblRootCountValue);
            groupBoxEditor.Controls.Add(lblRootCountCaption);
            groupBoxEditor.Controls.Add(chkIsGradient);
            groupBoxEditor.Controls.Add(btnApply);
            groupBoxEditor.Controls.Add(btnClose);
            groupBoxEditor.Controls.Add(btnEditColor);
            groupBoxEditor.Controls.Add(lbColorStops);
            groupBoxEditor.Controls.Add(labelStops);
            groupBoxEditor.Controls.Add(panelPreview);
            groupBoxEditor.Controls.Add(labelPreview);
            groupBoxEditor.Controls.Add(txtName);
            groupBoxEditor.Controls.Add(labelName);
            groupBoxEditor.Location = new Point(272, 12);
            groupBoxEditor.Name = "groupBoxEditor";
            groupBoxEditor.Size = new Size(385, 477);
            groupBoxEditor.TabIndex = 1;
            groupBoxEditor.TabStop = false;
            groupBoxEditor.Text = "Редактор палитры Ньютона";
            // 
            // lblEditHint
            // 
            lblEditHint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblEditHint.AutoSize = true;
            lblEditHint.Location = new Point(9, 420);
            lblEditHint.Name = "lblEditHint";
            lblEditHint.Size = new Size(182, 15);
            lblEditHint.TabIndex = 15;
            lblEditHint.Text = "Текущая палитра редактируема";
            // 
            // btnAutoAdjustRoots
            // 
            btnAutoAdjustRoots.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnAutoAdjustRoots.Location = new Point(9, 348);
            btnAutoAdjustRoots.Name = "btnAutoAdjustRoots";
            btnAutoAdjustRoots.Size = new Size(188, 28);
            btnAutoAdjustRoots.TabIndex = 14;
            btnAutoAdjustRoots.Text = "Подстроить под число корней";
            toolTip1.SetToolTip(btnAutoAdjustRoots, "Сохраняет в палитре ровно столько корневых цветов, сколько найдено корней");
            btnAutoAdjustRoots.UseVisualStyleBackColor = true;
            btnAutoAdjustRoots.Click += btnAutoAdjustRoots_Click;
            // 
            // lblRootCountValue
            // 
            lblRootCountValue.AutoSize = true;
            lblRootCountValue.Location = new Point(165, 126);
            lblRootCountValue.Name = "lblRootCountValue";
            lblRootCountValue.Size = new Size(13, 15);
            lblRootCountValue.TabIndex = 13;
            lblRootCountValue.Text = "0";
            // 
            // lblRootCountCaption
            // 
            lblRootCountCaption.AutoSize = true;
            lblRootCountCaption.Location = new Point(9, 126);
            lblRootCountCaption.Name = "lblRootCountCaption";
            lblRootCountCaption.Size = new Size(164, 15);
            lblRootCountCaption.TabIndex = 12;
            lblRootCountCaption.Text = "Найдено корней в формуле:";
            // 
            // chkIsGradient
            // 
            chkIsGradient.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            chkIsGradient.AutoSize = true;
            chkIsGradient.Location = new Point(9, 391);
            chkIsGradient.Name = "chkIsGradient";
            chkIsGradient.Size = new Size(267, 19);
            chkIsGradient.TabIndex = 11;
            chkIsGradient.Text = "Использовать градиент (иначе - дискретно)";
            chkIsGradient.UseVisualStyleBackColor = true;
            chkIsGradient.CheckedChanged += chkIsGradient_CheckedChanged;
            // 
            // btnApply
            // 
            btnApply.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnApply.Location = new Point(158, 441);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(107, 28);
            btnApply.TabIndex = 16;
            btnApply.Text = "Применить";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(271, 441);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(107, 28);
            btnClose.TabIndex = 17;
            btnClose.Text = "Закрыть";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // btnEditColor
            // 
            btnEditColor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnEditColor.Location = new Point(271, 192);
            btnEditColor.Name = "btnEditColor";
            btnEditColor.Size = new Size(107, 28);
            btnEditColor.TabIndex = 6;
            btnEditColor.Text = "Изменить...";
            btnEditColor.UseVisualStyleBackColor = true;
            btnEditColor.Click += btnEditColor_Click;
            // 
            // lbColorStops
            // 
            lbColorStops.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lbColorStops.FormattingEnabled = true;
            lbColorStops.Location = new Point(9, 192);
            lbColorStops.Name = "lbColorStops";
            lbColorStops.Size = new Size(256, 139);
            lbColorStops.TabIndex = 5;
            lbColorStops.SelectedIndexChanged += lbColorStops_SelectedIndexChanged;
            // 
            // labelStops
            // 
            labelStops.AutoSize = true;
            labelStops.Location = new Point(9, 174);
            labelStops.Name = "labelStops";
            labelStops.Size = new Size(85, 15);
            labelStops.TabIndex = 4;
            labelStops.Text = "Цвета корней:";
            // 
            // panelPreview
            // 
            panelPreview.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelPreview.BorderStyle = BorderStyle.FixedSingle;
            panelPreview.Location = new Point(9, 81);
            panelPreview.Name = "panelPreview";
            panelPreview.Size = new Size(369, 38);
            panelPreview.TabIndex = 3;
            panelPreview.Paint += panelPreview_Paint;
            panelPreview.MouseClick += panelPreview_MouseClick;
            panelPreview.MouseLeave += panelPreview_MouseLeave;
            panelPreview.MouseMove += panelPreview_MouseMove;
            // 
            // labelPreview
            // 
            labelPreview.AutoSize = true;
            labelPreview.Location = new Point(9, 63);
            labelPreview.Name = "labelPreview";
            labelPreview.Size = new Size(54, 15);
            labelPreview.TabIndex = 2;
            labelPreview.Text = "Превью:";
            // 
            // txtName
            // 
            txtName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtName.Location = new Point(9, 37);
            txtName.Name = "txtName";
            txtName.Size = new Size(369, 23);
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
            // ColorConfigurationNewtonPoolsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(669, 501);
            Controls.Add(btnSave);
            Controls.Add(groupBoxEditor);
            Controls.Add(groupBoxPalettes);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(685, 540);
            Name = "ColorConfigurationNewtonPoolsForm";
            Text = "Настройка палитры для Бассейнов Ньютона";
            FormClosing += ColorSetting_FormClosing;
            groupBoxPalettes.ResumeLayout(false);
            groupBoxEditor.ResumeLayout(false);
            groupBoxEditor.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBoxPalettes;
        private Button btnNew;
        private Button btnDelete;
        private Button btnSaveAs;
        private Button btnSave;
        private ListBox lbPalettes;
        private GroupBox groupBoxEditor;
        private TextBox txtName;
        private Label labelName;
        private Label labelPreview;
        private Panel panelPreview;
        private Label labelStops;
        private ListBox lbColorStops;
        private Button btnEditColor;
        private Button btnApply;
        private Button btnClose;
        private CheckBox chkIsGradient;
        private Label lblRootCountValue;
        private Label lblRootCountCaption;
        private Button btnAutoAdjustRoots;
        private Label lblEditHint;
        private ToolTip toolTip1;
    }
}
