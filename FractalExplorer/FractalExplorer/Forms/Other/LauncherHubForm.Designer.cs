namespace FractalExplorer
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
            // --- НОВЫЕ ЭЛЕМЕНТЫ УПРАВЛЕНИЯ ---
            groupBoxSystemInfo = new GroupBox();
            tblSystemInfo = new TableLayoutPanel();
            lblPowerValue = new Label();
            lblTempLabel = new Label();
            lblPowerLabel = new Label();
            lblTempValue = new Label();
            // --- КОНЕЦ НОВЫХ ЭЛЕМЕНТОВ ---
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            pnlDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPreview).BeginInit();
            // --- ИНИЦИАЛИЗАЦИЯ НОВЫХ ЭЛЕМЕНТОВ ---
            groupBoxSystemInfo.SuspendLayout();
            tblSystemInfo.SuspendLayout();
            // --- КОНЕЦ ИНИЦИАЛИЗАЦИИ ---
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
            splitContainerMain.Size = new Size(844, 521); // --- ИЗМЕНЕН РАЗМЕР ФОРМЫ ---
            splitContainerMain.SplitterDistance = 284;
            splitContainerMain.TabIndex = 0;
            // 
            // treeViewFractals
            // 
            treeViewFractals.Dock = DockStyle.Fill;
            treeViewFractals.Font = new Font("Segoe UI", 11F);
            treeViewFractals.Location = new Point(0, 0);
            treeViewFractals.Name = "treeViewFractals";
            treeViewFractals.Size = new Size(284, 521); // --- ИЗМЕНЕН РАЗМЕР ---
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
            pnlDetails.Controls.Add(btnLaunchSelected, 0, 4); // --- ИЗМЕНЕН ИНДЕКС СТРОКИ ---
            pnlDetails.Controls.Add(groupBoxSystemInfo, 0, 3); // --- ДОБАВЛЕН НОВЫЙ ЭЛЕМЕНТ ---
            pnlDetails.Dock = DockStyle.Fill;
            pnlDetails.Location = new Point(0, 0);
            pnlDetails.Name = "pnlDetails";
            pnlDetails.Padding = new Padding(5);
            pnlDetails.RowCount = 5; // --- ИЗМЕНЕНО КОЛИЧЕСТВО СТРОК ---
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F)); // --- ДОБАВЛЕНА НОВАЯ СТРОКА ---
            pnlDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            pnlDetails.Size = new Size(556, 521); // --- ИЗМЕНЕН РАЗМЕР ---
            pnlDetails.TabIndex = 0;
            // 
            // lblFractalName
            // 
            lblFractalName.AutoSize = true;
            lblFractalName.Dock = DockStyle.Fill;
            lblFractalName.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            lblFractalName.Location = new Point(8, 5);
            lblFractalName.Name = "lblFractalName";
            lblFractalName.Size = new Size(540, 40);
            lblFractalName.TabIndex = 0;
            lblFractalName.Text = "Выберите фрактал";
            lblFractalName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pictureBoxPreview
            // 
            pictureBoxPreview.Dock = DockStyle.Fill;
            pictureBoxPreview.Location = new Point(8, 48);
            pictureBoxPreview.Name = "pictureBoxPreview";
            pictureBoxPreview.Size = new Size(540, 164);
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
            richTextBoxDescription.Location = new Point(8, 218);
            richTextBoxDescription.Name = "richTextBoxDescription";
            richTextBoxDescription.ReadOnly = true;
            richTextBoxDescription.Size = new Size(540, 164);
            richTextBoxDescription.TabIndex = 2;
            richTextBoxDescription.Text = "";
            // 
            // btnLaunchSelected
            // 
            btnLaunchSelected.Dock = DockStyle.Fill;
            btnLaunchSelected.Enabled = false;
            btnLaunchSelected.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnLaunchSelected.Location = new Point(8, 468); // --- ИЗМЕНЕНО ПОЛОЖЕНИЕ ---
            btnLaunchSelected.Name = "btnLaunchSelected";
            btnLaunchSelected.Size = new Size(540, 45);
            btnLaunchSelected.TabIndex = 3;
            btnLaunchSelected.Text = "Запустить";
            btnLaunchSelected.UseVisualStyleBackColor = true;
            btnLaunchSelected.Click += btnLaunchSelected_Click;
            // 
            // --- ОПИСАНИЕ НОВЫХ ЭЛЕМЕНТОВ ---
            // 
            // groupBoxSystemInfo
            // 
            groupBoxSystemInfo.Controls.Add(tblSystemInfo);
            groupBoxSystemInfo.Dock = DockStyle.Fill;
            groupBoxSystemInfo.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            groupBoxSystemInfo.Location = new Point(8, 388);
            groupBoxSystemInfo.Name = "groupBoxSystemInfo";
            groupBoxSystemInfo.Size = new Size(540, 74);
            groupBoxSystemInfo.TabIndex = 4;
            groupBoxSystemInfo.TabStop = false;
            groupBoxSystemInfo.Text = "Мониторинг системы";
            // 
            // tblSystemInfo
            // 
            tblSystemInfo.ColumnCount = 2;
            tblSystemInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            tblSystemInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblSystemInfo.Controls.Add(lblPowerValue, 1, 1);
            tblSystemInfo.Controls.Add(lblTempLabel, 0, 0);
            tblSystemInfo.Controls.Add(lblPowerLabel, 0, 1);
            tblSystemInfo.Controls.Add(lblTempValue, 1, 0);
            tblSystemInfo.Dock = DockStyle.Fill;
            tblSystemInfo.Font = new Font("Segoe UI", 9F);
            tblSystemInfo.Location = new Point(3, 19);
            tblSystemInfo.Name = "tblSystemInfo";
            tblSystemInfo.RowCount = 2;
            tblSystemInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tblSystemInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tblSystemInfo.Size = new Size(534, 52);
            tblSystemInfo.TabIndex = 0;
            // 
            // lblPowerValue
            // 
            lblPowerValue.AutoSize = true;
            lblPowerValue.Dock = DockStyle.Fill;
            lblPowerValue.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblPowerValue.Location = new Point(183, 26);
            lblPowerValue.Name = "lblPowerValue";
            lblPowerValue.Size = new Size(348, 26);
            lblPowerValue.TabIndex = 3;
            lblPowerValue.Text = "N/A";
            lblPowerValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblTempLabel
            // 
            lblTempLabel.AutoSize = true;
            lblTempLabel.Dock = DockStyle.Fill;
            lblTempLabel.Font = new Font("Segoe UI", 9.75F);
            lblTempLabel.Location = new Point(3, 0);
            lblTempLabel.Name = "lblTempLabel";
            lblTempLabel.Size = new Size(174, 26);
            lblTempLabel.TabIndex = 0;
            lblTempLabel.Text = "Температура процессора:";
            lblTempLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblPowerLabel
            // 
            lblPowerLabel.AutoSize = true;
            lblPowerLabel.Dock = DockStyle.Fill;
            lblPowerLabel.Font = new Font("Segoe UI", 9.75F);
            lblPowerLabel.Location = new Point(3, 26);
            lblPowerLabel.Name = "lblPowerLabel";
            lblPowerLabel.Size = new Size(174, 26);
            lblPowerLabel.TabIndex = 1;
            lblPowerLabel.Text = "Потребляемая мощность:";
            lblPowerLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblTempValue
            // 
            lblTempValue.AutoSize = true;
            lblTempValue.Dock = DockStyle.Fill;
            lblTempValue.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            lblTempValue.Location = new Point(183, 0);
            lblTempValue.Name = "lblTempValue";
            lblTempValue.Size = new Size(348, 26);
            lblTempValue.TabIndex = 2;
            lblTempValue.Text = "N/A";
            lblTempValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // --- КОНЕЦ ОПИСАНИЯ НОВЫХ ЭЛЕМЕНТОВ ---
            // 
            // LauncherHubForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(844, 521); // --- ИЗМЕНЕН РАЗМЕР ФОРМЫ ---
            Controls.Add(splitContainerMain);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
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
            groupBoxSystemInfo.ResumeLayout(false);
            tblSystemInfo.ResumeLayout(false);
            tblSystemInfo.PerformLayout();
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

        private GroupBox groupBoxSystemInfo;
        private TableLayoutPanel tblSystemInfo;
        private Label lblPowerValue;
        private Label lblTempLabel;
        private Label lblPowerLabel;
        private Label lblTempValue;
    }
}