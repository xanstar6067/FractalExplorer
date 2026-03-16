namespace FractalExplorer.Forms.Other
{
    partial class AboutForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblAuthorTitle = new System.Windows.Forms.Label();
            this.lblAuthor = new System.Windows.Forms.Label();
            this.lblRepositoryTitle = new System.Windows.Forms.Label();
            this.linkRepository = new System.Windows.Forms.LinkLabel();
            this.lblDescriptionTitle = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblAuthorTitle
            // 
            this.lblAuthorTitle.AutoSize = true;
            this.lblAuthorTitle.Location = new System.Drawing.Point(18, 22);
            this.lblAuthorTitle.Name = "lblAuthorTitle";
            this.lblAuthorTitle.Size = new System.Drawing.Size(45, 15);
            this.lblAuthorTitle.TabIndex = 0;
            this.lblAuthorTitle.Text = "Автор:";
            // 
            // lblAuthor
            // 
            this.lblAuthor.AutoSize = true;
            this.lblAuthor.Location = new System.Drawing.Point(69, 22);
            this.lblAuthor.Name = "lblAuthor";
            this.lblAuthor.Size = new System.Drawing.Size(78, 15);
            this.lblAuthor.TabIndex = 1;
            this.lblAuthor.Text = "xanstar6067";
            // 
            // lblRepositoryTitle
            // 
            this.lblRepositoryTitle.AutoSize = true;
            this.lblRepositoryTitle.Location = new System.Drawing.Point(18, 52);
            this.lblRepositoryTitle.Name = "lblRepositoryTitle";
            this.lblRepositoryTitle.Size = new System.Drawing.Size(84, 15);
            this.lblRepositoryTitle.TabIndex = 2;
            this.lblRepositoryTitle.Text = "Репозиторий:";
            // 
            // linkRepository
            // 
            this.linkRepository.AutoSize = true;
            this.linkRepository.Location = new System.Drawing.Point(108, 52);
            this.linkRepository.Name = "linkRepository";
            this.linkRepository.Size = new System.Drawing.Size(288, 15);
            this.linkRepository.TabIndex = 3;
            this.linkRepository.TabStop = true;
            this.linkRepository.Text = "https://github.com/xanstar6067/Fractal-Explorer-studio";
            this.linkRepository.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkRepository_LinkClicked);
            // 
            // lblDescriptionTitle
            // 
            this.lblDescriptionTitle.AutoSize = true;
            this.lblDescriptionTitle.Location = new System.Drawing.Point(18, 87);
            this.lblDescriptionTitle.Name = "lblDescriptionTitle";
            this.lblDescriptionTitle.Size = new System.Drawing.Size(68, 15);
            this.lblDescriptionTitle.TabIndex = 4;
            this.lblDescriptionTitle.Text = "Описание:";
            // 
            // lblDescription
            // 
            this.lblDescription.Location = new System.Drawing.Point(18, 109);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(464, 128);
            this.lblDescription.TabIndex = 5;
            this.lblDescription.Text = "Fractal Explorer Studio — это Windows Forms-приложение на C#, которое позволяет " +
    "генерировать, исследовать и настраивать множество фракталов с гибким управлени" +
    "ем цветом, интерактивными селекторами параметров, сохранением состояний и экспо" +
    "ртом изображений в высоком качестве.";
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 280);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lblDescriptionTitle);
            this.Controls.Add(this.linkRepository);
            this.Controls.Add(this.lblRepositoryTitle);
            this.Controls.Add(this.lblAuthor);
            this.Controls.Add(this.lblAuthorTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "AboutForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "О программе";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblAuthorTitle;
        private System.Windows.Forms.Label lblAuthor;
        private System.Windows.Forms.Label lblRepositoryTitle;
        private System.Windows.Forms.LinkLabel linkRepository;
        private System.Windows.Forms.Label lblDescriptionTitle;
        private System.Windows.Forms.Label lblDescription;
    }
}
