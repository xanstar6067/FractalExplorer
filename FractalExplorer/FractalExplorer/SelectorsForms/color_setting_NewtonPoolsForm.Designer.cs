namespace FractalExplorer
{
    partial class color_setting_NewtonPoolsForm
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
            groupBox1 = new GroupBox();
            flpRootColorPickers = new FlowLayoutPanel();
            groupBox2 = new GroupBox();
            panelBackgroundColor = new Panel();
            label1 = new Label();
            btnClose = new Button();
            colorDialog1 = new ColorDialog();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(flpRootColorPickers);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(460, 203);
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
            flpRootColorPickers.Size = new Size(454, 181);
            flpRootColorPickers.TabIndex = 0;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(panelBackgroundColor);
            groupBox2.Controls.Add(label1);
            groupBox2.Location = new Point(12, 221);
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
            panelBackgroundColor.Click += panelBackgroundColor_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 26);
            label1.Name = "label1";
            label1.Size = new Size(180, 15);
            label1.TabIndex = 0;
            label1.Text = "Цвет для несошедшихся точек:";
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(397, 285);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(75, 23);
            btnClose.TabIndex = 2;
            btnClose.Text = "Закрыть";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // color_setting_NewtonPools
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(484, 320);
            Controls.Add(btnClose);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimumSize = new Size(350, 250);
            Name = "color_setting_NewtonPools";
            Text = "Настройка цветов палитры";
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.FlowLayoutPanel flpRootColorPickers;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Panel panelBackgroundColor;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.ColorDialog colorDialog1;
    }
}