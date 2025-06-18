namespace FractalDraving // Изменено с FractalExplorer на FractalDraving
{
    partial class FractalMondelbrotShip
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
            // --- Скопировано из FractalMondelbrot.Designer.cs ---
            this.panel1 = new System.Windows.Forms.Panel();
            this.mondelbrotClassicBox = new System.Windows.Forms.CheckBox();
            this.checkBox6 = new System.Windows.Forms.CheckBox();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.nudW = new System.Windows.Forms.NumericUpDown();
            this.nudH = new System.Windows.Forms.NumericUpDown();
            this.progressPNG = new System.Windows.Forms.ProgressBar();
            this.label8 = new System.Windows.Forms.Label();
            this.oldRenderBW = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.nudZoom = new System.Windows.Forms.NumericUpDown();
            this.colorBox = new System.Windows.Forms.CheckBox();
            this.btnRender = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.label6 = new System.Windows.Forms.Label();
            this.cbThreads = new System.Windows.Forms.ComboBox();
            this.btnSave = new System.Windows.Forms.Button(); // Эта кнопка будет для сохранения PNG (высокое разрешение)
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.nudThreshold = new System.Windows.Forms.NumericUpDown();
            this.nudIterations = new System.Windows.Forms.NumericUpDown();
            this.canvas2 = new System.Windows.Forms.PictureBox();
            // Кнопка для сохранения текущего превью (если нужна отдельная)
            // this.btnSavePreview = new System.Windows.Forms.Button(); 
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudH)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas2)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.mondelbrotClassicBox);
            this.panel1.Controls.Add(this.checkBox6);
            this.panel1.Controls.Add(this.checkBox5);
            this.panel1.Controls.Add(this.checkBox4);
            this.panel1.Controls.Add(this.checkBox3);
            this.panel1.Controls.Add(this.checkBox2);
            this.panel1.Controls.Add(this.checkBox1);
            this.panel1.Controls.Add(this.nudW);
            this.panel1.Controls.Add(this.nudH);
            this.panel1.Controls.Add(this.progressPNG);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.oldRenderBW);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.nudZoom);
            this.panel1.Controls.Add(this.colorBox);
            this.panel1.Controls.Add(this.btnRender);
            this.panel1.Controls.Add(this.progressBar);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.cbThreads);
            this.panel1.Controls.Add(this.btnSave); // Кнопка для сохранения PNG
            // this.panel1.Controls.Add(this.btnSavePreview); // Если нужна отдельная кнопка сохранения превью
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.nudThreshold);
            this.panel1.Controls.Add(this.nudIterations);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(231, 636); // Размер как в FractalMondelbrot
            this.panel1.TabIndex = 0;
            // 
            // mondelbrotClassicBox
            // 
            this.mondelbrotClassicBox.AutoSize = true;
            this.mondelbrotClassicBox.Location = new System.Drawing.Point(126, 347);
            this.mondelbrotClassicBox.Name = "mondelbrotClassicBox";
            this.mondelbrotClassicBox.Size = new System.Drawing.Size(77, 19);
            this.mondelbrotClassicBox.TabIndex = 32;
            this.mondelbrotClassicBox.Text = "Классика";
            this.mondelbrotClassicBox.UseVisualStyleBackColor = true;
            // 
            // checkBox6
            // 
            this.checkBox6.AutoSize = true;
            this.checkBox6.Location = new System.Drawing.Point(195, 372);
            this.checkBox6.Name = "checkBox6";
            this.checkBox6.Size = new System.Drawing.Size(32, 19);
            this.checkBox6.TabIndex = 29;
            this.checkBox6.Text = "6";
            this.checkBox6.UseVisualStyleBackColor = true;
            // 
            // checkBox5
            // 
            this.checkBox5.AutoSize = true;
            this.checkBox5.Location = new System.Drawing.Point(164, 372);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(32, 19);
            this.checkBox5.TabIndex = 28;
            this.checkBox5.Text = "5";
            this.checkBox5.UseVisualStyleBackColor = true;
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Location = new System.Drawing.Point(126, 372);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(32, 19);
            this.checkBox4.TabIndex = 27;
            this.checkBox4.Text = "4";
            this.checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(88, 372);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(32, 19);
            this.checkBox3.TabIndex = 26;
            this.checkBox3.Text = "3";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(50, 372);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(32, 19);
            this.checkBox2.TabIndex = 25;
            this.checkBox2.Text = "2";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(12, 372);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(32, 19);
            this.checkBox1.TabIndex = 24;
            this.checkBox1.Text = "1";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // nudW
            // 
            this.nudW.Location = new System.Drawing.Point(14, 266);
            this.nudW.Maximum = new decimal(new int[] { 30000, 0, 0, 0 }); // Увеличим максимум
            this.nudW.Minimum = new decimal(new int[] { 100, 0, 0, 0 }); // Минимальный размер
            this.nudW.Name = "nudW";
            this.nudW.Size = new System.Drawing.Size(86, 23);
            this.nudW.TabIndex = 23;
            this.nudW.Value = new decimal(new int[] { 1920, 0, 0, 0 });
            // 
            // nudH
            // 
            this.nudH.Location = new System.Drawing.Point(126, 266);
            this.nudH.Maximum = new decimal(new int[] { 30000, 0, 0, 0 }); // Увеличим максимум
            this.nudH.Minimum = new decimal(new int[] { 100, 0, 0, 0 }); // Минимальный размер
            this.nudH.Name = "nudH";
            this.nudH.Size = new System.Drawing.Size(83, 23);
            this.nudH.TabIndex = 22;
            this.nudH.Value = new decimal(new int[] { 1080, 0, 0, 0 });
            // 
            // progressPNG
            // 
            this.progressPNG.Location = new System.Drawing.Point(5, 295);
            this.progressPNG.Name = "progressPNG";
            this.progressPNG.Size = new System.Drawing.Size(218, 23);
            this.progressPNG.TabIndex = 21;
            this.progressPNG.Visible = false; // Скрыт по умолчанию
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(87, 191);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(67, 15);
            this.label8.TabIndex = 20;
            this.label8.Text = "Обработка";
            // 
            // oldRenderBW
            // 
            this.oldRenderBW.AutoSize = true;
            this.oldRenderBW.Location = new System.Drawing.Point(76, 347);
            this.oldRenderBW.Name = "oldRenderBW";
            this.oldRenderBW.Size = new System.Drawing.Size(41, 19);
            this.oldRenderBW.TabIndex = 17;
            this.oldRenderBW.Text = "ЧБ";
            this.oldRenderBW.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 68);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(86, 15);
            this.label5.TabIndex = 16;
            this.label5.Text = "Приближение";
            // 
            // nudZoom
            // 
            this.nudZoom.DecimalPlaces = 3; // Для Burning Ship может потребоваться большая точность
            this.nudZoom.Increment = new decimal(new int[] { 1, 0, 0, 65536 }); // 0.1 или 0.01
            this.nudZoom.Location = new System.Drawing.Point(12, 86);
            this.nudZoom.Maximum = new decimal(new int[] { 1000000000, 0, 0, 0 }); // Очень большой зум
            this.nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 196608 }); // 0.001
            this.nudZoom.Name = "nudZoom";
            this.nudZoom.Size = new System.Drawing.Size(196, 23);
            this.nudZoom.TabIndex = 2; // Порядок табуляции
            this.nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 }); // Начальное значение, установится из кода
            // 
            // colorBox
            // 
            this.colorBox.AutoSize = true;
            this.colorBox.Location = new System.Drawing.Point(12, 347);
            this.colorBox.Name = "colorBox";
            this.colorBox.Size = new System.Drawing.Size(52, 19);
            this.colorBox.TabIndex = 15;
            this.colorBox.Text = "Цвет";
            this.colorBox.UseVisualStyleBackColor = true;
            // 
            // btnRender
            // 
            this.btnRender.Location = new System.Drawing.Point(34, 165); // Позиция кнопки рендера
            this.btnRender.Name = "btnRender";
            this.btnRender.Size = new System.Drawing.Size(164, 23);
            this.btnRender.TabIndex = 5; // Порядок табуляции
            this.btnRender.Text = "Запустить рендер";
            this.btnRender.UseVisualStyleBackColor = true;
            this.btnRender.Click += new System.EventHandler(this.btnRender_Click);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(5, 209);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(218, 23);
            this.progressBar.TabIndex = 14;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 113);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(69, 15);
            this.label6.TabIndex = 13;
            this.label6.Text = "Потоки ЦП";
            // 
            // cbThreads
            // 
            this.cbThreads.FormattingEnabled = true;
            this.cbThreads.Location = new System.Drawing.Point(12, 131);
            this.cbThreads.Name = "cbThreads";
            this.cbThreads.Size = new System.Drawing.Size(195, 23);
            this.cbThreads.TabIndex = 4; // Порядок табуляции
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(30, 237); // Позиция кнопки сохранения PNG
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(164, 23);
            this.btnSave.TabIndex = 6; // Порядок табуляции
            this.btnSave.Text = "Сохранить PNG"; // Текст для сохранения в высоком разрешении
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click_1); // Обработчик для сохранения PNG
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(138, 43);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 15);
            this.label4.TabIndex = 7;
            this.label4.Text = "Порог выхода";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(138, 14);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 15);
            this.label3.TabIndex = 6;
            this.label3.Text = "Итерации";
            // 
            // nudThreshold
            // 
            this.nudThreshold.DecimalPlaces = 1; // Точность для порога
            this.nudThreshold.Increment = new decimal(new int[] { 1, 0, 0, 65536 }); // 0.1
            this.nudThreshold.Location = new System.Drawing.Point(12, 41);
            this.nudThreshold.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.nudThreshold.Minimum = new decimal(new int[] { 1, 0, 0, 0 }); // Порог не должен быть слишком мал
            this.nudThreshold.Name = "nudThreshold";
            this.nudThreshold.Size = new System.Drawing.Size(120, 23);
            this.nudThreshold.TabIndex = 1; // Порядок табуляции
            this.nudThreshold.Value = new decimal(new int[] { 20, 0, 0, 65536 }); // 2.0
            // 
            // nudIterations
            // 
            this.nudIterations.Location = new System.Drawing.Point(12, 12);
            this.nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.nudIterations.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            this.nudIterations.Name = "nudIterations";
            this.nudIterations.Size = new System.Drawing.Size(120, 23);
            this.nudIterations.TabIndex = 0; // Порядок табуляции
            this.nudIterations.Value = new decimal(new int[] { 200, 0, 0, 0 }); // Начальное значение для BS
            // 
            // canvas2
            // 
            this.canvas2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvas2.Location = new System.Drawing.Point(231, 0);
            this.canvas2.Name = "canvas2"; // Имя PictureBox как в FractalMondelbrot
            this.canvas2.Size = new System.Drawing.Size(853, 636); // Размер как в FractalMondelbrot
            this.canvas2.TabIndex = 1;
            this.canvas2.TabStop = false;
            // 
            // FractalMondelbrotShip
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 636); // Размер как в FractalMondelbrot
            this.Controls.Add(this.canvas2);
            this.Controls.Add(this.panel1);
            this.MinimumSize = new System.Drawing.Size(800, 600); // Минимальный размер
            this.Name = "FractalMondelbrotShip";
            // --- ИЗМЕНЕНИЕ: Заголовок окна ---
            this.Text = "Множество Горящий Корабль";
            // --- КОНЕЦ ИЗМЕНЕНИЯ ---
            this.Load += new System.EventHandler(this.Form1_Load); // Имя обработчика как в FractalMondelbrot
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudH)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas2)).EndInit();
            this.ResumeLayout(false);
            // --- Конец скопированного кода ---
        }

        #endregion

        // --- Объявления контролов, скопированные из FractalMondelbrot.Designer.cs ---
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox canvas2; // Имя PictureBox
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nudThreshold;
        private System.Windows.Forms.NumericUpDown nudIterations;
        private System.Windows.Forms.Button btnSave; // Переиспользована для сохранения PNG
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cbThreads;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button btnRender;
        private System.Windows.Forms.CheckBox colorBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown nudZoom;
        private System.Windows.Forms.CheckBox oldRenderBW;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ProgressBar progressPNG;
        private System.Windows.Forms.NumericUpDown nudW;
        private System.Windows.Forms.NumericUpDown nudH;
        private System.Windows.Forms.CheckBox checkBox6;
        private System.Windows.Forms.CheckBox checkBox5;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox mondelbrotClassicBox;
        // private System.Windows.Forms.Button btnSavePreview; // Если нужна отдельная кнопка сохранения превью
    }
}