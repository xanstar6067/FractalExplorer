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
            tableSelectionRoot = new TableLayoutPanel();
            panelColorMatrixContainer = new Panel();
            pnlColorMatrix = new Panel();
            pnlHueSlider = new Panel();
            tableRgb = new TableLayoutPanel();
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
            grpPalettes = new GroupBox();
            tablePalettes = new TableLayoutPanel();
            lblStandard = new Label();
            tableStandardColors = new TableLayoutPanel();
            lblCustom = new Label();
            tableCustomColors = new TableLayoutPanel();
            btnAddToCustomColors = new Button();
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
            tableSelectionRoot.SuspendLayout();
            panelColorMatrixContainer.SuspendLayout();
            tableRgb.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trkRed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkGreen).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trkBlue).BeginInit();
            grpPalettes.SuspendLayout();
            tablePalettes.SuspendLayout();
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
            tableMain.Controls.Add(grpPalettes, 0, 1);
            tableMain.Controls.Add(grpPreview, 0, 2);
            tableMain.Controls.Add(panelButtons, 0, 3);
            tableMain.Dock = DockStyle.Fill;
            tableMain.Location = new Point(0, 0);
            tableMain.Name = "tableMain";
            tableMain.Padding = new Padding(12);
            tableMain.RowCount = 4;
            tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 290F));
            tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 283F));
            tableMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableMain.RowStyles.Add(new RowStyle());
            tableMain.Size = new Size(560, 794);
            tableMain.TabIndex = 0;
            // 
            // grpSelectColor
            // 
            grpSelectColor.Controls.Add(tableSelectionRoot);
            grpSelectColor.Dock = DockStyle.Fill;
            grpSelectColor.Location = new Point(15, 15);
            grpSelectColor.Name = "grpSelectColor";
            grpSelectColor.Size = new Size(530, 284);
            grpSelectColor.TabIndex = 0;
            grpSelectColor.TabStop = false;
            grpSelectColor.Text = "Выбор цвета";
            // 
            // tableSelectionRoot
            // 
            tableSelectionRoot.ColumnCount = 1;
            tableSelectionRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableSelectionRoot.Controls.Add(panelColorMatrixContainer, 0, 0);
            tableSelectionRoot.Controls.Add(tableRgb, 0, 1);
            tableSelectionRoot.Controls.Add(btnEyedropper, 0, 2);
            tableSelectionRoot.Dock = DockStyle.Fill;
            tableSelectionRoot.Location = new Point(3, 19);
            tableSelectionRoot.Name = "tableSelectionRoot";
            tableSelectionRoot.RowCount = 3;
            tableSelectionRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableSelectionRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 106F));
            tableSelectionRoot.RowStyles.Add(new RowStyle());
            tableSelectionRoot.Size = new Size(524, 262);
            tableSelectionRoot.TabIndex = 0;
            // 
            // panelColorMatrixContainer
            // 
            panelColorMatrixContainer.Controls.Add(pnlColorMatrix);
            panelColorMatrixContainer.Controls.Add(pnlHueSlider);
            panelColorMatrixContainer.Dock = DockStyle.Fill;
            panelColorMatrixContainer.Location = new Point(3, 3);
            panelColorMatrixContainer.Name = "panelColorMatrixContainer";
            panelColorMatrixContainer.Size = new Size(518, 121);
            panelColorMatrixContainer.TabIndex = 0;
            // 
            // pnlColorMatrix
            // 
            pnlColorMatrix.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlColorMatrix.BorderStyle = BorderStyle.FixedSingle;
            pnlColorMatrix.Cursor = Cursors.Cross;
            pnlColorMatrix.Location = new Point(0, 0);
            pnlColorMatrix.Name = "pnlColorMatrix";
            pnlColorMatrix.Size = new Size(470, 121);
            pnlColorMatrix.TabIndex = 0;
            pnlColorMatrix.Paint += pnlColorMatrix_Paint;
            pnlColorMatrix.MouseDown += pnlColorMatrix_MouseDown;
            pnlColorMatrix.MouseMove += pnlColorMatrix_MouseMove;
            // 
            // pnlHueSlider
            // 
            pnlHueSlider.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            pnlHueSlider.BorderStyle = BorderStyle.FixedSingle;
            pnlHueSlider.Cursor = Cursors.Hand;
            pnlHueSlider.Location = new Point(476, 0);
            pnlHueSlider.Name = "pnlHueSlider";
            pnlHueSlider.Size = new Size(42, 121);
            pnlHueSlider.TabIndex = 1;
            pnlHueSlider.Paint += pnlHueSlider_Paint;
            pnlHueSlider.MouseDown += pnlHueSlider_MouseDown;
            pnlHueSlider.MouseMove += pnlHueSlider_MouseMove;
            // 
            // tableRgb
            // 
            tableRgb.ColumnCount = 3;
            tableRgb.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableRgb.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableRgb.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
            tableRgb.Controls.Add(lblRed, 0, 0);
            tableRgb.Controls.Add(trkRed, 1, 0);
            tableRgb.Controls.Add(lblRedValue, 2, 0);
            tableRgb.Controls.Add(lblGreen, 0, 1);
            tableRgb.Controls.Add(trkGreen, 1, 1);
            tableRgb.Controls.Add(lblGreenValue, 2, 1);
            tableRgb.Controls.Add(lblBlue, 0, 2);
            tableRgb.Controls.Add(trkBlue, 1, 2);
            tableRgb.Controls.Add(lblBlueValue, 2, 2);
            tableRgb.Dock = DockStyle.Fill;
            tableRgb.Location = new Point(3, 130);
            tableRgb.Name = "tableRgb";
            tableRgb.RowCount = 3;
            tableRgb.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableRgb.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableRgb.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableRgb.Size = new Size(518, 100);
            tableRgb.TabIndex = 1;
            // 
            // lblRed
            // 
            lblRed.AutoSize = true;
            lblRed.Dock = DockStyle.Fill;
            lblRed.Location = new Point(3, 0);
            lblRed.Name = "lblRed";
            lblRed.Size = new Size(14, 33);
            lblRed.TabIndex = 0;
            lblRed.Text = "R";
            lblRed.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // trkRed
            // 
            trkRed.AutoSize = false;
            trkRed.Dock = DockStyle.Fill;
            trkRed.LargeChange = 16;
            trkRed.Location = new Point(23, 4);
            trkRed.Margin = new Padding(3, 4, 3, 0);
            trkRed.Maximum = 255;
            trkRed.Name = "trkRed";
            trkRed.Size = new Size(448, 24);
            trkRed.TabIndex = 1;
            trkRed.TickFrequency = 16;
            trkRed.Scroll += trackColor_Scroll;
            // 
            // lblRedValue
            // 
            lblRedValue.AutoSize = true;
            lblRedValue.Dock = DockStyle.Fill;
            lblRedValue.Location = new Point(477, 0);
            lblRedValue.Name = "lblRedValue";
            lblRedValue.Size = new Size(38, 33);
            lblRedValue.TabIndex = 2;
            lblRedValue.Text = "0";
            lblRedValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblGreen
            // 
            lblGreen.AutoSize = true;
            lblGreen.Dock = DockStyle.Fill;
            lblGreen.Location = new Point(3, 33);
            lblGreen.Name = "lblGreen";
            lblGreen.Size = new Size(14, 33);
            lblGreen.TabIndex = 3;
            lblGreen.Text = "G";
            lblGreen.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // trkGreen
            // 
            trkGreen.AutoSize = false;
            trkGreen.Dock = DockStyle.Fill;
            trkGreen.LargeChange = 16;
            trkGreen.Location = new Point(23, 37);
            trkGreen.Margin = new Padding(3, 4, 3, 0);
            trkGreen.Maximum = 255;
            trkGreen.Name = "trkGreen";
            trkGreen.Size = new Size(448, 24);
            trkGreen.TabIndex = 4;
            trkGreen.TickFrequency = 16;
            trkGreen.Scroll += trackColor_Scroll;
            // 
            // lblGreenValue
            // 
            lblGreenValue.AutoSize = true;
            lblGreenValue.Dock = DockStyle.Fill;
            lblGreenValue.Location = new Point(477, 33);
            lblGreenValue.Name = "lblGreenValue";
            lblGreenValue.Size = new Size(38, 33);
            lblGreenValue.TabIndex = 5;
            lblGreenValue.Text = "0";
            lblGreenValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblBlue
            // 
            lblBlue.AutoSize = true;
            lblBlue.Dock = DockStyle.Fill;
            lblBlue.Location = new Point(3, 66);
            lblBlue.Name = "lblBlue";
            lblBlue.Size = new Size(14, 34);
            lblBlue.TabIndex = 6;
            lblBlue.Text = "B";
            lblBlue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // trkBlue
            // 
            trkBlue.AutoSize = false;
            trkBlue.Dock = DockStyle.Fill;
            trkBlue.LargeChange = 16;
            trkBlue.Location = new Point(23, 70);
            trkBlue.Margin = new Padding(3, 4, 3, 0);
            trkBlue.Maximum = 255;
            trkBlue.Name = "trkBlue";
            trkBlue.Size = new Size(448, 24);
            trkBlue.TabIndex = 7;
            trkBlue.TickFrequency = 16;
            trkBlue.Scroll += trackColor_Scroll;
            // 
            // lblBlueValue
            // 
            lblBlueValue.AutoSize = true;
            lblBlueValue.Dock = DockStyle.Fill;
            lblBlueValue.Location = new Point(477, 66);
            lblBlueValue.Name = "lblBlueValue";
            lblBlueValue.Size = new Size(38, 34);
            lblBlueValue.TabIndex = 8;
            lblBlueValue.Text = "0";
            lblBlueValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // btnEyedropper
            // 
            btnEyedropper.Dock = DockStyle.Top;
            btnEyedropper.Location = new Point(3, 236);
            btnEyedropper.Name = "btnEyedropper";
            btnEyedropper.Size = new Size(518, 23);
            btnEyedropper.TabIndex = 2;
            btnEyedropper.Text = "Пипетка";
            btnEyedropper.UseVisualStyleBackColor = true;
            btnEyedropper.Click += btnEyedropper_Click;
            // 
            // grpPalettes
            // 
            grpPalettes.Controls.Add(tablePalettes);
            grpPalettes.Dock = DockStyle.Fill;
            grpPalettes.Location = new Point(15, 305);
            grpPalettes.Name = "grpPalettes";
            grpPalettes.Size = new Size(530, 277);
            grpPalettes.TabIndex = 1;
            grpPalettes.TabStop = false;
            grpPalettes.Text = "Палитры";
            // 
            // tablePalettes
            // 
            tablePalettes.ColumnCount = 1;
            tablePalettes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tablePalettes.Controls.Add(lblStandard, 0, 0);
            tablePalettes.Controls.Add(tableStandardColors, 0, 1);
            tablePalettes.Controls.Add(lblCustom, 0, 2);
            tablePalettes.Controls.Add(tableCustomColors, 0, 3);
            tablePalettes.Controls.Add(btnAddToCustomColors, 0, 4);
            tablePalettes.Dock = DockStyle.Fill;
            tablePalettes.Location = new Point(3, 19);
            tablePalettes.Name = "tablePalettes";
            tablePalettes.RowCount = 5;
            tablePalettes.RowStyles.Add(new RowStyle());
            tablePalettes.RowStyles.Add(new RowStyle(SizeType.Absolute, 144F));
            tablePalettes.RowStyles.Add(new RowStyle());
            tablePalettes.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            tablePalettes.RowStyles.Add(new RowStyle());
            tablePalettes.Size = new Size(524, 255);
            tablePalettes.TabIndex = 0;
            // 
            // lblStandard
            // 
            lblStandard.AutoSize = true;
            lblStandard.Location = new Point(3, 0);
            lblStandard.Name = "lblStandard";
            lblStandard.Size = new Size(137, 15);
            lblStandard.TabIndex = 0;
            lblStandard.Text = "Основные цвета:";
            // 
            // tableStandardColors
            // 
            tableStandardColors.ColumnCount = 8;
            tableStandardColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableStandardColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableStandardColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableStandardColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableStandardColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableStandardColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableStandardColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableStandardColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableStandardColors.Anchor = AnchorStyles.None;
            tableStandardColors.Location = new Point(0, 15);
            tableStandardColors.Margin = new Padding(0);
            tableStandardColors.Name = "tableStandardColors";
            tableStandardColors.RowCount = 6;
            tableStandardColors.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableStandardColors.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableStandardColors.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableStandardColors.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableStandardColors.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableStandardColors.RowStyles.Add(new RowStyle(SizeType.Percent, 16.666666F));
            tableStandardColors.Size = new Size(524, 144);
            tableStandardColors.TabIndex = 1;
            // 
            // lblCustom
            // 
            lblCustom.AutoSize = true;
            lblCustom.Location = new Point(3, 159);
            lblCustom.Name = "lblCustom";
            lblCustom.Size = new Size(127, 15);
            lblCustom.TabIndex = 2;
            lblCustom.Text = "Пользовательские:";
            // 
            // tableCustomColors
            // 
            tableCustomColors.ColumnCount = 8;
            tableCustomColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableCustomColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableCustomColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableCustomColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableCustomColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableCustomColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableCustomColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableCustomColors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            tableCustomColors.Anchor = AnchorStyles.None;
            tableCustomColors.Location = new Point(0, 174);
            tableCustomColors.Margin = new Padding(0);
            tableCustomColors.Name = "tableCustomColors";
            tableCustomColors.RowCount = 2;
            tableCustomColors.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableCustomColors.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableCustomColors.Size = new Size(524, 48);
            tableCustomColors.TabIndex = 3;
            // 
            // btnAddToCustomColors
            // 
            btnAddToCustomColors.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddToCustomColors.Location = new Point(373, 222);
            btnAddToCustomColors.Margin = new Padding(3, 0, 3, 0);
            btnAddToCustomColors.Name = "btnAddToCustomColors";
            btnAddToCustomColors.Size = new Size(148, 23);
            btnAddToCustomColors.TabIndex = 4;
            btnAddToCustomColors.Text = "Добавить цвет";
            btnAddToCustomColors.UseVisualStyleBackColor = true;
            btnAddToCustomColors.Click += btnAddToCustomColors_Click;
            // 
            // grpPreview
            // 
            grpPreview.Controls.Add(tablePreview);
            grpPreview.Dock = DockStyle.Fill;
            grpPreview.Location = new Point(15, 588);
            grpPreview.Name = "grpPreview";
            grpPreview.Size = new Size(530, 161);
            grpPreview.TabIndex = 2;
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
            tablePreview.Size = new Size(524, 139);
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
            pnlCurrentColor.Size = new Size(256, 103);
            pnlCurrentColor.TabIndex = 1;
            // 
            // lblCurrentHexValue
            // 
            lblCurrentHexValue.AutoSize = true;
            lblCurrentHexValue.Location = new Point(3, 124);
            lblCurrentHexValue.Name = "lblCurrentHexValue";
            lblCurrentHexValue.Size = new Size(25, 15);
            lblCurrentHexValue.TabIndex = 2;
            lblCurrentHexValue.Text = "#...";
            // 
            // lblNew
            // 
            lblNew.AutoSize = true;
            lblNew.Location = new Point(265, 0);
            lblNew.Name = "lblNew";
            lblNew.Size = new Size(72, 15);
            lblNew.TabIndex = 3;
            lblNew.Text = "Новый цвет";
            // 
            // pnlNewColor
            // 
            pnlNewColor.BorderStyle = BorderStyle.FixedSingle;
            pnlNewColor.Dock = DockStyle.Fill;
            pnlNewColor.Location = new Point(265, 18);
            pnlNewColor.Name = "pnlNewColor";
            pnlNewColor.Size = new Size(256, 103);
            pnlNewColor.TabIndex = 4;
            // 
            // lblNewHexValue
            // 
            lblNewHexValue.AutoSize = true;
            lblNewHexValue.Location = new Point(265, 124);
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
            panelButtons.Location = new Point(15, 755);
            panelButtons.Name = "panelButtons";
            panelButtons.Size = new Size(530, 24);
            panelButtons.TabIndex = 3;
            // 
            // btnOk
            // 
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(452, 0);
            btnOk.Margin = new Padding(3, 0, 0, 0);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 23);
            btnOk.TabIndex = 0;
            btnOk.Text = "ОК";
            btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(371, 0);
            btnCancel.Margin = new Padding(3, 0, 0, 0);
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
            ClientSize = new Size(560, 794);
            Controls.Add(tableMain);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ColorPickerPanelForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Выбор цвета";
            Resize += ColorPickerPanelForm_Resize;
            tableMain.ResumeLayout(false);
            tableMain.PerformLayout();
            grpSelectColor.ResumeLayout(false);
            tableSelectionRoot.ResumeLayout(false);
            panelColorMatrixContainer.ResumeLayout(false);
            tableRgb.ResumeLayout(false);
            tableRgb.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trkRed).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkGreen).EndInit();
            ((System.ComponentModel.ISupportInitialize)trkBlue).EndInit();
            grpPalettes.ResumeLayout(false);
            tablePalettes.ResumeLayout(false);
            tablePalettes.PerformLayout();
            grpPreview.ResumeLayout(false);
            tablePreview.ResumeLayout(false);
            tablePreview.PerformLayout();
            panelButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableMain;
        private GroupBox grpSelectColor;
        private TableLayoutPanel tableSelectionRoot;
        private Panel panelColorMatrixContainer;
        private Panel pnlColorMatrix;
        private Panel pnlHueSlider;
        private TableLayoutPanel tableRgb;
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
        private GroupBox grpPalettes;
        private TableLayoutPanel tablePalettes;
        private Label lblStandard;
        private TableLayoutPanel tableStandardColors;
        private Label lblCustom;
        private TableLayoutPanel tableCustomColors;
        private Button btnAddToCustomColors;
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
