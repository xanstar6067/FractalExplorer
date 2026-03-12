namespace FractalExplorer.Forms.Other
{
    partial class ThemeEditorForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ThemeEditorForm));
            splitMain = new SplitContainer();
            leftPanel = new TableLayoutPanel();
            lblThemes = new Label();
            lbThemes = new ListBox();
            rightPanel = new TableLayoutPanel();
            lblThemeName = new Label();
            txtThemeName = new TextBox();
            grpColors = new GroupBox();
            flpColorProperties = new FlowLayoutPanel();
            grpPreview = new GroupBox();
            pnlPreview = new Panel();
            txtPreviewInput = new TextBox();
            pnlInteractiveBorderPreview = new Panel();
            label1 = new Label();
            btnPreviewAction = new Button();
            lblPreviewSubText = new Label();
            lblPreviewTitle = new Label();
            buttonsPanel = new FlowLayoutPanel();
            btnNew = new Button();
            btnCopy = new Button();
            btnDelete = new Button();
            btnSaveChanges = new Button();
            btnApplyTheme = new Button();
            btnClose = new Button();
            btnCopyWindowsTheme = new Button();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            leftPanel.SuspendLayout();
            rightPanel.SuspendLayout();
            grpColors.SuspendLayout();
            grpPreview.SuspendLayout();
            pnlPreview.SuspendLayout();
            pnlInteractiveBorderPreview.SuspendLayout();
            buttonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new Point(0, 0);
            splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(leftPanel);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(rightPanel);
            splitMain.Size = new Size(1172, 620);
            splitMain.SplitterDistance = 315;
            splitMain.TabIndex = 0;
            // 
            // leftPanel
            // 
            leftPanel.ColumnCount = 1;
            leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftPanel.Controls.Add(lblThemes, 0, 0);
            leftPanel.Controls.Add(lbThemes, 0, 1);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(0, 0);
            leftPanel.Name = "leftPanel";
            leftPanel.Padding = new Padding(10);
            leftPanel.RowCount = 2;
            leftPanel.RowStyles.Add(new RowStyle());
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftPanel.Size = new Size(315, 620);
            leftPanel.TabIndex = 0;
            // 
            // lblThemes
            // 
            lblThemes.AutoSize = true;
            lblThemes.Dock = DockStyle.Fill;
            lblThemes.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblThemes.Location = new Point(10, 10);
            lblThemes.Margin = new Padding(0, 0, 0, 8);
            lblThemes.Name = "lblThemes";
            lblThemes.Size = new Size(295, 19);
            lblThemes.TabIndex = 0;
            lblThemes.Text = "Темы";
            // 
            // lbThemes
            // 
            lbThemes.Dock = DockStyle.Fill;
            lbThemes.FormattingEnabled = true;
            lbThemes.Location = new Point(10, 37);
            lbThemes.Margin = new Padding(0);
            lbThemes.Name = "lbThemes";
            lbThemes.Size = new Size(295, 573);
            lbThemes.TabIndex = 1;
            lbThemes.SelectedIndexChanged += lbThemes_SelectedIndexChanged;
            // 
            // rightPanel
            // 
            rightPanel.ColumnCount = 1;
            rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightPanel.Controls.Add(lblThemeName, 0, 0);
            rightPanel.Controls.Add(txtThemeName, 0, 1);
            rightPanel.Controls.Add(grpColors, 0, 2);
            rightPanel.Controls.Add(grpPreview, 0, 3);
            rightPanel.Controls.Add(buttonsPanel, 0, 4);
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Location = new Point(0, 0);
            rightPanel.Name = "rightPanel";
            rightPanel.Padding = new Padding(12);
            rightPanel.RowCount = 5;
            rightPanel.RowStyles.Add(new RowStyle());
            rightPanel.RowStyles.Add(new RowStyle());
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            rightPanel.RowStyles.Add(new RowStyle());
            rightPanel.Size = new Size(853, 620);
            rightPanel.TabIndex = 0;
            // 
            // lblThemeName
            // 
            lblThemeName.AutoSize = true;
            lblThemeName.Location = new Point(12, 12);
            lblThemeName.Margin = new Padding(0, 0, 0, 6);
            lblThemeName.Name = "lblThemeName";
            lblThemeName.Size = new Size(66, 15);
            lblThemeName.TabIndex = 0;
            lblThemeName.Text = "Имя темы:";
            // 
            // txtThemeName
            // 
            txtThemeName.Dock = DockStyle.Top;
            txtThemeName.Location = new Point(12, 33);
            txtThemeName.Margin = new Padding(0, 0, 0, 10);
            txtThemeName.Name = "txtThemeName";
            txtThemeName.Size = new Size(829, 23);
            txtThemeName.TabIndex = 1;
            txtThemeName.TextChanged += txtThemeName_TextChanged;
            // 
            // grpColors
            // 
            grpColors.Controls.Add(flpColorProperties);
            grpColors.Dock = DockStyle.Fill;
            grpColors.Location = new Point(12, 66);
            grpColors.Margin = new Padding(0, 0, 0, 10);
            grpColors.Name = "grpColors";
            grpColors.Size = new Size(829, 295);
            grpColors.TabIndex = 2;
            grpColors.TabStop = false;
            grpColors.Text = "Цветовые свойства";
            // 
            // flpColorProperties
            // 
            flpColorProperties.AutoScroll = true;
            flpColorProperties.Dock = DockStyle.Fill;
            flpColorProperties.FlowDirection = FlowDirection.TopDown;
            flpColorProperties.Location = new Point(3, 19);
            flpColorProperties.Name = "flpColorProperties";
            flpColorProperties.Size = new Size(823, 273);
            flpColorProperties.TabIndex = 0;
            flpColorProperties.WrapContents = false;
            // 
            // grpPreview
            // 
            grpPreview.Controls.Add(pnlPreview);
            grpPreview.Dock = DockStyle.Fill;
            grpPreview.Location = new Point(12, 371);
            grpPreview.Margin = new Padding(0, 0, 0, 10);
            grpPreview.Name = "grpPreview";
            grpPreview.Size = new Size(829, 193);
            grpPreview.TabIndex = 3;
            grpPreview.TabStop = false;
            grpPreview.Text = "Предпросмотр";
            // 
            // pnlPreview
            // 
            pnlPreview.Controls.Add(txtPreviewInput);
            pnlPreview.Controls.Add(pnlInteractiveBorderPreview);
            pnlPreview.Controls.Add(btnPreviewAction);
            pnlPreview.Controls.Add(lblPreviewSubText);
            pnlPreview.Controls.Add(lblPreviewTitle);
            pnlPreview.Dock = DockStyle.Top;
            pnlPreview.Location = new Point(3, 19);
            pnlPreview.Name = "pnlPreview";
            pnlPreview.Padding = new Padding(12);
            pnlPreview.Size = new Size(823, 145);
            pnlPreview.TabIndex = 0;
            // 
            // txtPreviewInput
            // 
            txtPreviewInput.Location = new Point(16, 73);
            txtPreviewInput.Name = "txtPreviewInput";
            txtPreviewInput.Size = new Size(221, 23);
            txtPreviewInput.TabIndex = 3;
            txtPreviewInput.Text = "Тестовое поле";
            // 
            // pnlInteractiveBorderPreview
            // 
            pnlInteractiveBorderPreview.Controls.Add(label1);
            pnlInteractiveBorderPreview.Location = new Point(393, 26);
            pnlInteractiveBorderPreview.Name = "pnlInteractiveBorderPreview";
            pnlInteractiveBorderPreview.Size = new Size(247, 95);
            pnlInteractiveBorderPreview.TabIndex = 2;
            pnlInteractiveBorderPreview.Paint += pnlInteractiveBorderPreview_Paint;
            pnlInteractiveBorderPreview.MouseEnter += pnlInteractiveBorderPreview_MouseEnter;
            pnlInteractiveBorderPreview.MouseLeave += pnlInteractiveBorderPreview_MouseLeave;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            label1.Location = new Point(5, 37);
            label1.Name = "label1";
            label1.Size = new Size(237, 19);
            label1.TabIndex = 4;
            label1.Text = "Пример интерактивной области";
            // 
            // btnPreviewAction
            // 
            btnPreviewAction.Location = new Point(251, 71);
            btnPreviewAction.Name = "btnPreviewAction";
            btnPreviewAction.Size = new Size(123, 27);
            btnPreviewAction.TabIndex = 2;
            btnPreviewAction.Text = "Тестовая кнопка";
            btnPreviewAction.UseVisualStyleBackColor = true;
            // 
            // lblPreviewSubText
            // 
            lblPreviewSubText.AutoSize = true;
            lblPreviewSubText.Location = new Point(16, 44);
            lblPreviewSubText.Name = "lblPreviewSubText";
            lblPreviewSubText.Size = new Size(157, 15);
            lblPreviewSubText.TabIndex = 1;
            lblPreviewSubText.Text = "Пример вторичного текста";
            // 
            // lblPreviewTitle
            // 
            lblPreviewTitle.AutoSize = true;
            lblPreviewTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblPreviewTitle.Location = new Point(16, 16);
            lblPreviewTitle.Name = "lblPreviewTitle";
            lblPreviewTitle.Size = new Size(192, 19);
            lblPreviewTitle.TabIndex = 0;
            lblPreviewTitle.Text = "Пример основного текста";
            // 
            // buttonsPanel
            // 
            buttonsPanel.AutoSize = true;
            buttonsPanel.Controls.Add(btnNew);
            buttonsPanel.Controls.Add(btnCopy);
            buttonsPanel.Controls.Add(btnDelete);
            buttonsPanel.Controls.Add(btnSaveChanges);
            buttonsPanel.Controls.Add(btnApplyTheme);
            buttonsPanel.Controls.Add(btnClose);
            buttonsPanel.Controls.Add(btnCopyWindowsTheme);
            buttonsPanel.Dock = DockStyle.Fill;
            buttonsPanel.Location = new Point(12, 574);
            buttonsPanel.Margin = new Padding(0);
            buttonsPanel.Name = "buttonsPanel";
            buttonsPanel.Size = new Size(829, 34);
            buttonsPanel.TabIndex = 4;
            // 
            // btnNew
            // 
            btnNew.Location = new Point(3, 3);
            btnNew.Name = "btnNew";
            btnNew.Size = new Size(75, 27);
            btnNew.TabIndex = 0;
            btnNew.Text = "Новая";
            btnNew.UseVisualStyleBackColor = true;
            btnNew.Click += btnNew_Click;
            // 
            // btnCopy
            // 
            btnCopy.Location = new Point(84, 3);
            btnCopy.Name = "btnCopy";
            btnCopy.Size = new Size(95, 27);
            btnCopy.TabIndex = 1;
            btnCopy.Text = "Копировать";
            btnCopy.UseVisualStyleBackColor = true;
            btnCopy.Click += btnCopy_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(185, 3);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(85, 27);
            btnDelete.TabIndex = 2;
            btnDelete.Text = "Удалить";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnSaveChanges
            // 
            btnSaveChanges.Location = new Point(276, 3);
            btnSaveChanges.Name = "btnSaveChanges";
            btnSaveChanges.Size = new Size(150, 27);
            btnSaveChanges.TabIndex = 3;
            btnSaveChanges.Text = "Сохранить изменения";
            btnSaveChanges.UseVisualStyleBackColor = true;
            btnSaveChanges.Click += btnSaveChanges_Click;
            // 
            // btnApplyTheme
            // 
            btnApplyTheme.Location = new Point(432, 3);
            btnApplyTheme.Name = "btnApplyTheme";
            btnApplyTheme.Size = new Size(115, 27);
            btnApplyTheme.TabIndex = 4;
            btnApplyTheme.Text = "Применить тему";
            btnApplyTheme.UseVisualStyleBackColor = true;
            btnApplyTheme.Click += btnApplyTheme_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(553, 3);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(90, 27);
            btnClose.TabIndex = 6;
            btnClose.Text = "Закрыть";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // btnCopyWindowsTheme
            // 
            btnCopyWindowsTheme.Location = new Point(649, 3);
            btnCopyWindowsTheme.Name = "btnCopyWindowsTheme";
            btnCopyWindowsTheme.Size = new Size(170, 27);
            btnCopyWindowsTheme.TabIndex = 5;
            btnCopyWindowsTheme.Text = "Импорт темы Windows";
            btnCopyWindowsTheme.UseVisualStyleBackColor = true;
            btnCopyWindowsTheme.Click += btnCopyWindowsTheme_Click;
            // 
            // ThemeEditorForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1172, 620);
            Controls.Add(splitMain);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimumSize = new Size(980, 600);
            Name = "ThemeEditorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Редактор тем";
            Load += ThemeEditorForm_Load;
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            leftPanel.PerformLayout();
            rightPanel.ResumeLayout(false);
            rightPanel.PerformLayout();
            grpColors.ResumeLayout(false);
            grpPreview.ResumeLayout(false);
            pnlPreview.ResumeLayout(false);
            pnlPreview.PerformLayout();
            pnlInteractiveBorderPreview.ResumeLayout(false);
            pnlInteractiveBorderPreview.PerformLayout();
            buttonsPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitMain;
        private TableLayoutPanel leftPanel;
        private Label lblThemes;
        private ListBox lbThemes;
        private TableLayoutPanel rightPanel;
        private Label lblThemeName;
        private TextBox txtThemeName;
        private GroupBox grpColors;
        private FlowLayoutPanel flpColorProperties;
        private GroupBox grpPreview;
        private Panel pnlPreview;
        private TextBox txtPreviewInput;
        private Button btnPreviewAction;
        private Label lblPreviewSubText;
        private Label lblPreviewTitle;
        private FlowLayoutPanel buttonsPanel;
        private Button btnNew;
        private Button btnCopy;
        private Button btnDelete;
        private Button btnSaveChanges;
        private Button btnApplyTheme;
        private Button btnCopyWindowsTheme;
        private Button btnClose;
        private Panel pnlInteractiveBorderPreview;
        private Label label1;
    }
}
