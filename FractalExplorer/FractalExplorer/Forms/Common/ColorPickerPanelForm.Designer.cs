namespace FractalExplorer.Forms.Common
{
    partial class ColorPickerPanelForm
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
            tableMain = new TableLayoutPanel();
            grpSelectColor = new GroupBox();
            tableSelection = new TableLayoutPanel();
            btnOpenColorDialog = new Button();
            lblRed = new Label();
            trkRed = new TrackBar();
            lblRedValue = new Label();
            lblGreen = new Label();
            trkGreen = new TrackBar();
            lblGreenValue = new Label();
            lblBlue = new Label();
            trkBlue = new TrackBar();
            lblBlueValue = new Label();
            btnEyedropper = new Button();
            grpPreview = new GroupBox();
            tablePreview = new TableLayoutPanel();
            lblCurrent = new Label();
            pnlCurrentColor = new Panel();
            lblCurrentHexValue = new Label();
            lblNew = new Label();
            pnlNewColor = new Panel();
            lblNewHexValue = new Label();
            panelButtons = new FlowLayoutPanel();
            btnOk = new Button();
            btnCancel = new Button();
            tableMain.SuspendLayout();
            grpSelectColor.SuspendLayout();
            tableSelection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkRed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkGreen).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkBlue).BeginInit();
            grpPreview.SuspendLayout();
            tablePreview.SuspendLayout();
            panelButtons.SuspendLayout();
            SuspendLayout();
            // 
            // tableMain
            // 
            tableMain.ColumnCount = 1;
            tableMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableMain.Controls.Add(grpSelectColor, 0, 0);
            tableMain.Controls.Add(grpPreview, 0, 1);
            tableMain.Controls.Add(panelButtons, 0, 2);
            tableMain.Dock = DockStyle.Fill;
            tableMain.Location = new Point(0, 0);
            tableMain.Name = "tableMain";
            tableMain.Padding = new Padding(12);
            tableMain.RowCount = 3;
            tableMain.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            tableMain.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            tableMain.RowStyles.Add(new RowStyle());
            tableMain.Size = new Size(434, 401);
            tableMain.TabIndex = 0;
            // 
            // grpSelectColor
            // 
            grpSelectColor.Controls.Add(tableSelection);
            grpSelectColor.Dock = DockStyle.Fill;
            grpSelectColor.Location = new Point(15, 15);
            grpSelectColor.Name = "grpSelectColor";
            grpSelectColor.Size = new Size(404, 199);
            grpSelectColor.TabIndex = 0;
            grpSelectColor.TabStop = false;
            grpSelectColor.Text = "Выбор цвета";
            // 
            // tableSelection
            // 
            tableSelection.ColumnCount = 3;
            tableSelection.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35F));
            tableSelection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableSelection.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));
            tableSelection.Controls.Add(btnOpenColorDialog, 0, 0);
            tableSelection.Controls.Add(lblRed, 0, 1);
            tableSelection.Controls.Add(trkRed, 1, 1);
            tableSelection.Controls.Add(lblRedValue, 2, 1);
            tableSelection.Controls.Add(lblGreen, 0, 2);
            tableSelection.Controls.Add(trkGreen, 1, 2);
            tableSelection.Controls.Add(lblGreenValue, 2, 2);
            tableSelection.Controls.Add(lblBlue, 0, 3);
            tableSelection.Controls.Add(trkBlue, 1, 3);
            tableSelection.Controls.Add(lblBlueValue, 2, 3);
            tableSelection.Controls.Add(btnEyedropper, 0, 4);
            tableSelection.Dock = DockStyle.Fill;
            tableSelection.Location = new Point(3, 19);
            tableSelection.Name = "tableSelection";
            tableSelection.RowCount = 6;
            tableSelection.RowStyles.Add(new RowStyle());
            tableSelection.RowStyles.Add(new RowStyle());
            tableSelection.RowStyles.Add(new RowStyle());
            tableSelection.RowStyles.Add(new RowStyle());
            tableSelection.RowStyles.Add(new RowStyle());
            tableSelection.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableSelection.Size = new Size(398, 177);
            tableSelection.TabIndex = 0;
            // 
            // btnOpenColorDialog
            // 
            tableSelection.SetColumnSpan(btnOpenColorDialog, 3);
            btnOpenColorDialog.Dock = DockStyle.Top;
            btnOpenColorDialog.Location = new Point(6, 3);
            btnOpenColorDialog.Margin = new Padding(6, 3, 6, 10);
            btnOpenColorDialog.Name = "btnOpenColorDialog";
            btnOpenColorDialog.Size = new Size(386, 27);
            btnOpenColorDialog.TabIndex = 0;
            btnOpenColorDialog.Text = "Открыть стандартный выбор цвета";
            btnOpenColorDialog.UseVisualStyleBackColor = true;
            btnOpenColorDialog.Click += btnOpenColorDialog_Click;
            // 
            // lblRed
            // 
            lblRed.AutoSize = true;
            lblRed.Dock = DockStyle.Fill;
            lblRed.Location = new Point(3, 40);
            lblRed.Name = "lblRed";
            lblRed.Size = new Size(29, 45);
            lblRed.TabIndex = 1;
            lblRed.Text = "R";
            lblRed.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // trkRed
            // 
            trkRed.AutoSize = false;
            trkRed.Dock = DockStyle.Fill;
            trkRed.LargeChange = 16;
            trkRed.Location = new Point(38, 43);
            trkRed.Maximum = 255;
            trkRed.Name = "trkRed";
            trkRed.Size = new Size(307, 24);
            trkRed.TabIndex = 2;
            trkRed.TickFrequency = 16;
            trkRed.Scroll += trackColor_Scroll;
            // 
            // lblRedValue
            // 
            lblRedValue.AutoSize = true;
            lblRedValue.Dock = DockStyle.Fill;
            lblRedValue.Location = new Point(351, 40);
            lblRedValue.Name = "lblRedValue";
            lblRedValue.Size = new Size(44, 45);
            lblRedValue.TabIndex = 3;
            lblRedValue.Text = "0";
            lblRedValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblGreen
            // 
            lblGreen.AutoSize = true;
            lblGreen.Dock = DockStyle.Fill;
            lblGreen.Location = new Point(3, 85);
            lblGreen.Name = "lblGreen";
            lblGreen.Size = new Size(29, 45);
            lblGreen.TabIndex = 4;
            lblGreen.Text = "G";
            lblGreen.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // trkGreen
            // 
            trkGreen.AutoSize = false;
            trkGreen.Dock = DockStyle.Fill;
            trkGreen.LargeChange = 16;
            trkGreen.Location = new Point(38, 88);
            trkGreen.Maximum = 255;
            trkGreen.Name = "trkGreen";
            trkGreen.Size = new Size(307, 24);
            trkGreen.TabIndex = 5;
            trkGreen.TickFrequency = 16;
            trkGreen.Scroll += trackColor_Scroll;
            // 
            // lblGreenValue
            // 
            lblGreenValue.AutoSize = true;
            lblGreenValue.Dock = DockStyle.Fill;
            lblGreenValue.Location = new Point(351, 85);
            lblGreenValue.Name = "lblGreenValue";
            lblGreenValue.Size = new Size(44, 45);
            lblGreenValue.TabIndex = 6;
            lblGreenValue.Text = "0";
            lblGreenValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblBlue
            // 
            lblBlue.AutoSize = true;
            lblBlue.Dock = DockStyle.Fill;
            lblBlue.Location = new Point(3, 130);
            lblBlue.Name = "lblBlue";
            lblBlue.Size = new Size(29, 45);
            lblBlue.TabIndex = 7;
            lblBlue.Text = "B";
            lblBlue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // trkBlue
            // 
            trkBlue.AutoSize = false;
            trkBlue.Dock = DockStyle.Fill;
            trkBlue.LargeChange = 16;
            trkBlue.Location = new Point(38, 133);
            trkBlue.Maximum = 255;
            trkBlue.Name = "trkBlue";
            trkBlue.Size = new Size(307, 24);
            trkBlue.TabIndex = 8;
            trkBlue.TickFrequency = 16;
            trkBlue.Scroll += trackColor_Scroll;
            // 
            // lblBlueValue
            // 
            lblBlueValue.AutoSize = true;
            lblBlueValue.Dock = DockStyle.Fill;
            lblBlueValue.Location = new Point(351, 130);
            lblBlueValue.Name = "lblBlueValue";
            lblBlueValue.Size = new Size(44, 45);
            lblBlueValue.TabIndex = 9;
            lblBlueValue.Text = "0";
            lblBlueValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // btnEyedropper
            // 
            tableSelection.SetColumnSpan(btnEyedropper, 3);
            btnEyedropper.Dock = DockStyle.Top;
            btnEyedropper.Location = new Point(6, 181);
            btnEyedropper.Margin = new Padding(6, 6, 6, 0);
            btnEyedropper.Name = "btnEyedropper";
            btnEyedropper.Size = new Size(386, 27);
            btnEyedropper.TabIndex = 10;
            btnEyedropper.Text = "Пипетка";
            btnEyedropper.UseVisualStyleBackColor = true;
            btnEyedropper.Click += btnEyedropper_Click;
            // 
            // grpPreview
            // 
            grpPreview.Controls.Add(tablePreview);
            grpPreview.Dock = DockStyle.Fill;
            grpPreview.Location = new Point(15, 220);
            grpPreview.Name = "grpPreview";
            grpPreview.Size = new Size(404, 130);
            grpPreview.TabIndex = 1;
            grpPreview.TabStop = false;
            grpPreview.Text = "Предпросмотр";
            // 
            // tablePreview
            // 
            tablePreview.ColumnCount = 2;
            tablePreview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tablePreview.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tablePreview.Controls.Add(lblCurrent, 0, 0);
            tablePreview.Controls.Add(pnlCurrentColor, 0, 1);
            tablePreview.Controls.Add(lblCurrentHexValue, 0, 2);
            tablePreview.Controls.Add(lblNew, 1, 0);
            tablePreview.Controls.Add(pnlNewColor, 1, 1);
            tablePreview.Controls.Add(lblNewHexValue, 1, 2);
            tablePreview.Dock = DockStyle.Fill;
            tablePreview.Location = new Point(3, 19);
            tablePreview.Name = "tablePreview";
            tablePreview.RowCount = 3;
            tablePreview.RowStyles.Add(new RowStyle());
            tablePreview.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tablePreview.RowStyles.Add(new RowStyle());
            tablePreview.Size = new Size(398, 108);
            tablePreview.TabIndex = 0;
            // 
            // lblCurrent
            // 
            lblCurrent.AutoSize = true;
            lblCurrent.Location = new Point(3, 0);
            lblCurrent.Name = "lblCurrent";
            lblCurrent.Size = new Size(96, 15);
            lblCurrent.TabIndex = 0;
            lblCurrent.Text = "Текущий цвет";
            // 
            // pnlCurrentColor
            // 
            pnlCurrentColor.BorderStyle = BorderStyle.FixedSingle;
            pnlCurrentColor.Dock = DockStyle.Fill;
            pnlCurrentColor.Location = new Point(3, 18);
            pnlCurrentColor.Name = "pnlCurrentColor";
            pnlCurrentColor.Size = new Size(193, 72);
            pnlCurrentColor.TabIndex = 1;
            // 
            // lblCurrentHexValue
            // 
            lblCurrentHexValue.AutoSize = true;
            lblCurrentHexValue.Location = new Point(3, 93);
            lblCurrentHexValue.Name = "lblCurrentHexValue";
            lblCurrentHexValue.Size = new Size(25, 15);
            lblCurrentHexValue.TabIndex = 2;
            lblCurrentHexValue.Text = "#...";
            // 
            // lblNew
            // 
            lblNew.AutoSize = true;
            lblNew.Location = new Point(202, 0);
            lblNew.Name = "lblNew";
            lblNew.Size = new Size(72, 15);
            lblNew.TabIndex = 3;
            lblNew.Text = "Новый цвет";
            // 
            // pnlNewColor
            // 
            pnlNewColor.BorderStyle = BorderStyle.FixedSingle;
            pnlNewColor.Cursor = Cursors.Hand;
            pnlNewColor.Dock = DockStyle.Fill;
            pnlNewColor.Location = new Point(202, 18);
            pnlNewColor.Name = "pnlNewColor";
            pnlNewColor.Size = new Size(193, 72);
            pnlNewColor.TabIndex = 4;
            pnlNewColor.Click += pnlNewColor_Click;
            // 
            // lblNewHexValue
            // 
            lblNewHexValue.AutoSize = true;
            lblNewHexValue.Location = new Point(202, 93);
            lblNewHexValue.Name = "lblNewHexValue";
            lblNewHexValue.Size = new Size(25, 15);
            lblNewHexValue.TabIndex = 5;
            lblNewHexValue.Text = "#...";
            // 
            // panelButtons
            // 
            panelButtons.AutoSize = true;
            panelButtons.Controls.Add(btnOk);
            panelButtons.Controls.Add(btnCancel);
            panelButtons.Dock = DockStyle.Fill;
            panelButtons.FlowDirection = FlowDirection.RightToLeft;
            panelButtons.Location = new Point(15, 356);
            panelButtons.Name = "panelButtons";
            panelButtons.Size = new Size(404, 30);
            panelButtons.TabIndex = 2;
            // 
            // btnOk
            // 
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(326, 3);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 23);
            btnOk.TabIndex = 0;
            btnOk.Text = "ОК";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(245, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Отмена";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // ColorPickerPanelForm
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(434, 401);
            Controls.Add(tableMain);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ColorPickerPanelForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Выбор цвета";
            tableMain.ResumeLayout(false);
            tableMain.PerformLayout();
            grpSelectColor.ResumeLayout(false);
            tableSelection.ResumeLayout(false);
            tableSelection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trkRed).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkGreen).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkBlue).EndInit();
            grpPreview.ResumeLayout(false);
            tablePreview.ResumeLayout(false);
            tablePreview.PerformLayout();
            panelButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableMain;
        private GroupBox grpSelectColor;
        private TableLayoutPanel tableSelection;
        private Button btnOpenColorDialog;
        private Label lblRed;
        private TrackBar trkRed;
        private Label lblRedValue;
        private Label lblGreen;
        private TrackBar trkGreen;
        private Label lblGreenValue;
        private Label lblBlue;
        private TrackBar trkBlue;
        private Label lblBlueValue;
        private Button btnEyedropper;
        private GroupBox grpPreview;
        private TableLayoutPanel tablePreview;
        private Label lblCurrent;
        private Panel pnlCurrentColor;
        private Label lblCurrentHexValue;
        private Label lblNew;
        private Panel pnlNewColor;
        private Label lblNewHexValue;
        private FlowLayoutPanel panelButtons;
        private Button btnOk;
        private Button btnCancel;
    }
}
