namespace FractalExplorer.Forms
{
    partial class FractalNovaForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FractalNovaForm));
            this.pnlControls = new System.Windows.Forms.TableLayoutPanel();
            this.gbNovaParameters = new System.Windows.Forms.GroupBox();
            this.tlpNovaParameters = new System.Windows.Forms.TableLayoutPanel();
            this.nudP_Re = new System.Windows.Forms.NumericUpDown();
            this.lblP_Re = new System.Windows.Forms.Label();
            this.nudP_Im = new System.Windows.Forms.NumericUpDown();
            this.lblP_Im = new System.Windows.Forms.Label();
            this.gbInitialConditions = new System.Windows.Forms.GroupBox();
            this.tlpInitialConditions = new System.Windows.Forms.TableLayoutPanel();
            this.nudZ0_Re = new System.Windows.Forms.NumericUpDown();
            this.lblZ0_Re = new System.Windows.Forms.Label();
            this.nudZ0_Im = new System.Windows.Forms.NumericUpDown();
            this.lblZ0_Im = new System.Windows.Forms.Label();
            this.lblM = new System.Windows.Forms.Label();
            this.nudM = new System.Windows.Forms.NumericUpDown();
            this.nudIterations = new System.Windows.Forms.NumericUpDown();
            this.lblIterations = new System.Windows.Forms.Label();
            this.nudThreshold = new System.Windows.Forms.NumericUpDown();
            this.lblThreshold = new System.Windows.Forms.Label();
            this.lblZoom = new System.Windows.Forms.Label();
            this.nudZoom = new System.Windows.Forms.NumericUpDown();
            this.cbThreads = new System.Windows.Forms.ComboBox();
            this.lblThreads = new System.Windows.Forms.Label();
            this.cbSSAA = new System.Windows.Forms.ComboBox();
            this.lblSSAA = new System.Windows.Forms.Label();
            this.cbSmooth = new System.Windows.Forms.CheckBox();
            this.btnSaveHighRes = new System.Windows.Forms.Button();
            this.btnConfigurePalette = new System.Windows.Forms.Button();
            this.btnRender = new System.Windows.Forms.Button();
            this.btnStateManager = new System.Windows.Forms.Button();
            this.lblProgress = new System.Windows.Forms.Label();
            this.pbRenderProgress = new System.Windows.Forms.ProgressBar();
            this.canvas = new System.Windows.Forms.PictureBox();
            this.pnlControls.SuspendLayout();
            this.gbNovaParameters.SuspendLayout();
            this.tlpNovaParameters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudP_Re)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudP_Im)).BeginInit();
            this.gbInitialConditions.SuspendLayout();
            this.tlpInitialConditions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudZ0_Re)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZ0_Im)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudM)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlControls
            // 
            this.pnlControls.ColumnCount = 2;
            this.pnlControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.pnlControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.pnlControls.Controls.Add(this.gbNovaParameters, 0, 0);
            this.pnlControls.Controls.Add(this.gbInitialConditions, 0, 1);
            this.pnlControls.Controls.Add(this.lblM, 1, 2);
            this.pnlControls.Controls.Add(this.nudM, 0, 2);
            this.pnlControls.Controls.Add(this.nudIterations, 0, 3);
            this.pnlControls.Controls.Add(this.lblIterations, 1, 3);
            this.pnlControls.Controls.Add(this.nudThreshold, 0, 4);
            this.pnlControls.Controls.Add(this.lblThreshold, 1, 4);
            this.pnlControls.Controls.Add(this.lblZoom, 0, 5);
            this.pnlControls.Controls.Add(this.nudZoom, 0, 6);
            this.pnlControls.Controls.Add(this.cbThreads, 0, 7);
            this.pnlControls.Controls.Add(this.lblThreads, 1, 7);
            this.pnlControls.Controls.Add(this.cbSSAA, 0, 8);
            this.pnlControls.Controls.Add(this.lblSSAA, 1, 8);
            this.pnlControls.Controls.Add(this.cbSmooth, 0, 9);
            this.pnlControls.Controls.Add(this.btnSaveHighRes, 0, 10);
            this.pnlControls.Controls.Add(this.btnConfigurePalette, 0, 11);
            this.pnlControls.Controls.Add(this.btnRender, 0, 12);
            this.pnlControls.Controls.Add(this.btnStateManager, 0, 13);
            this.pnlControls.Controls.Add(this.lblProgress, 0, 14);
            this.pnlControls.Controls.Add(this.pbRenderProgress, 0, 15);
            this.pnlControls.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlControls.Location = new System.Drawing.Point(0, 0);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.RowCount = 17;
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.pnlControls.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlControls.Size = new System.Drawing.Size(231, 636);
            this.pnlControls.TabIndex = 0;
            // 
            // gbNovaParameters
            // 
            this.pnlControls.SetColumnSpan(this.gbNovaParameters, 2);
            this.gbNovaParameters.Controls.Add(this.tlpNovaParameters);
            this.gbNovaParameters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbNovaParameters.Location = new System.Drawing.Point(3, 3);
            this.gbNovaParameters.Name = "gbNovaParameters";
            this.gbNovaParameters.Size = new System.Drawing.Size(225, 80);
            this.gbNovaParameters.TabIndex = 0;
            this.gbNovaParameters.TabStop = false;
            this.gbNovaParameters.Text = "Параметры степени (P)";
            // 
            // tlpNovaParameters
            // 
            this.tlpNovaParameters.ColumnCount = 2;
            this.tlpNovaParameters.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.tlpNovaParameters.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tlpNovaParameters.Controls.Add(this.nudP_Re, 0, 0);
            this.tlpNovaParameters.Controls.Add(this.lblP_Re, 1, 0);
            this.tlpNovaParameters.Controls.Add(this.nudP_Im, 0, 1);
            this.tlpNovaParameters.Controls.Add(this.lblP_Im, 1, 1);
            this.tlpNovaParameters.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpNovaParameters.Location = new System.Drawing.Point(3, 19);
            this.tlpNovaParameters.Name = "tlpNovaParameters";
            this.tlpNovaParameters.RowCount = 2;
            this.tlpNovaParameters.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpNovaParameters.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpNovaParameters.Size = new System.Drawing.Size(219, 58);
            this.tlpNovaParameters.TabIndex = 0;
            // 
            // nudP_Re
            // 
            this.nudP_Re.DecimalPlaces = 15;
            this.nudP_Re.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudP_Re.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.nudP_Re.Location = new System.Drawing.Point(3, 3);
            this.nudP_Re.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.nudP_Re.Minimum = new decimal(new int[] { 10, 0, 0, -2147483648 });
            this.nudP_Re.Name = "nudP_Re";
            this.nudP_Re.Size = new System.Drawing.Size(114, 23);
            this.nudP_Re.TabIndex = 0;
            this.nudP_Re.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // lblP_Re
            // 
            this.lblP_Re.AutoSize = true;
            this.lblP_Re.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblP_Re.Location = new System.Drawing.Point(123, 0);
            this.lblP_Re.Name = "lblP_Re";
            this.lblP_Re.Size = new System.Drawing.Size(93, 29);
            this.lblP_Re.TabIndex = 1;
            this.lblP_Re.Text = "Степень P (Re)";
            this.lblP_Re.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudP_Im
            // 
            this.nudP_Im.DecimalPlaces = 15;
            this.nudP_Im.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudP_Im.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.nudP_Im.Location = new System.Drawing.Point(3, 32);
            this.nudP_Im.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.nudP_Im.Minimum = new decimal(new int[] { 10, 0, 0, -2147483648 });
            this.nudP_Im.Name = "nudP_Im";
            this.nudP_Im.Size = new System.Drawing.Size(114, 23);
            this.nudP_Im.TabIndex = 2;
            // 
            // lblP_Im
            // 
            this.lblP_Im.AutoSize = true;
            this.lblP_Im.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblP_Im.Location = new System.Drawing.Point(123, 29);
            this.lblP_Im.Name = "lblP_Im";
            this.lblP_Im.Size = new System.Drawing.Size(93, 29);
            this.lblP_Im.TabIndex = 3;
            this.lblP_Im.Text = "Степень P (Im)";
            this.lblP_Im.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // gbInitialConditions
            // 
            this.pnlControls.SetColumnSpan(this.gbInitialConditions, 2);
            this.gbInitialConditions.Controls.Add(this.tlpInitialConditions);
            this.gbInitialConditions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbInitialConditions.Location = new System.Drawing.Point(3, 89);
            this.gbInitialConditions.Name = "gbInitialConditions";
            this.gbInitialConditions.Size = new System.Drawing.Size(225, 80);
            this.gbInitialConditions.TabIndex = 1;
            this.gbInitialConditions.TabStop = false;
            this.gbInitialConditions.Text = "Начальные условия (Z₀)";
            // 
            // tlpInitialConditions
            // 
            this.tlpInitialConditions.ColumnCount = 2;
            this.tlpInitialConditions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.tlpInitialConditions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tlpInitialConditions.Controls.Add(this.nudZ0_Re, 0, 0);
            this.tlpInitialConditions.Controls.Add(this.lblZ0_Re, 1, 0);
            this.tlpInitialConditions.Controls.Add(this.nudZ0_Im, 0, 1);
            this.tlpInitialConditions.Controls.Add(this.lblZ0_Im, 1, 1);
            this.tlpInitialConditions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpInitialConditions.Location = new System.Drawing.Point(3, 19);
            this.tlpInitialConditions.Name = "tlpInitialConditions";
            this.tlpInitialConditions.RowCount = 2;
            this.tlpInitialConditions.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpInitialConditions.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpInitialConditions.Size = new System.Drawing.Size(219, 58);
            this.tlpInitialConditions.TabIndex = 0;
            // 
            // nudZ0_Re
            // 
            this.nudZ0_Re.DecimalPlaces = 15;
            this.nudZ0_Re.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudZ0_Re.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.nudZ0_Re.Location = new System.Drawing.Point(3, 3);
            this.nudZ0_Re.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.nudZ0_Re.Minimum = new decimal(new int[] { 10, 0, 0, -2147483648 });
            this.nudZ0_Re.Name = "nudZ0_Re";
            this.nudZ0_Re.Size = new System.Drawing.Size(114, 23);
            this.nudZ0_Re.TabIndex = 0;
            this.nudZ0_Re.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblZ0_Re
            // 
            this.lblZ0_Re.AutoSize = true;
            this.lblZ0_Re.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblZ0_Re.Location = new System.Drawing.Point(123, 0);
            this.lblZ0_Re.Name = "lblZ0_Re";
            this.lblZ0_Re.Size = new System.Drawing.Size(93, 29);
            this.lblZ0_Re.TabIndex = 1;
            this.lblZ0_Re.Text = "Начальное Z₀ (Re)";
            this.lblZ0_Re.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudZ0_Im
            // 
            this.nudZ0_Im.DecimalPlaces = 15;
            this.nudZ0_Im.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudZ0_Im.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.nudZ0_Im.Location = new System.Drawing.Point(3, 32);
            this.nudZ0_Im.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.nudZ0_Im.Minimum = new decimal(new int[] { 10, 0, 0, -2147483648 });
            this.nudZ0_Im.Name = "nudZ0_Im";
            this.nudZ0_Im.Size = new System.Drawing.Size(114, 23);
            this.nudZ0_Im.TabIndex = 2;
            // 
            // lblZ0_Im
            // 
            this.lblZ0_Im.AutoSize = true;
            this.lblZ0_Im.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblZ0_Im.Location = new System.Drawing.Point(123, 29);
            this.lblZ0_Im.Name = "lblZ0_Im";
            this.lblZ0_Im.Size = new System.Drawing.Size(93, 29);
            this.lblZ0_Im.TabIndex = 3;
            this.lblZ0_Im.Text = "Начальное Z₀ (Im)";
            this.lblZ0_Im.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblM
            // 
            this.lblM.AutoSize = true;
            this.lblM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblM.Location = new System.Drawing.Point(130, 172);
            this.lblM.Name = "lblM";
            this.lblM.Size = new System.Drawing.Size(98, 29);
            this.lblM.TabIndex = 3;
            this.lblM.Text = "Релаксация (m)";
            this.lblM.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudM
            // 
            this.nudM.DecimalPlaces = 3;
            this.nudM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudM.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.nudM.Location = new System.Drawing.Point(6, 175);
            this.nudM.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.nudM.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            this.nudM.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudM.Name = "nudM";
            this.nudM.Size = new System.Drawing.Size(118, 23);
            this.nudM.TabIndex = 2;
            this.nudM.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // nudIterations
            // 
            this.nudIterations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudIterations.Location = new System.Drawing.Point(6, 204);
            this.nudIterations.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.nudIterations.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            this.nudIterations.Name = "nudIterations";
            this.nudIterations.Size = new System.Drawing.Size(118, 23);
            this.nudIterations.TabIndex = 4;
            this.nudIterations.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // lblIterations
            // 
            this.lblIterations.AutoSize = true;
            this.lblIterations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIterations.Location = new System.Drawing.Point(130, 201);
            this.lblIterations.Name = "lblIterations";
            this.lblIterations.Size = new System.Drawing.Size(98, 29);
            this.lblIterations.TabIndex = 5;
            this.lblIterations.Text = "Итерации";
            this.lblIterations.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nudThreshold
            // 
            this.nudThreshold.DecimalPlaces = 1;
            this.nudThreshold.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudThreshold.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudThreshold.Location = new System.Drawing.Point(6, 233);
            this.nudThreshold.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.nudThreshold.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.nudThreshold.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            this.nudThreshold.Name = "nudThreshold";
            this.nudThreshold.Size = new System.Drawing.Size(118, 23);
            this.nudThreshold.TabIndex = 6;
            this.nudThreshold.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // lblThreshold
            // 
            this.lblThreshold.AutoSize = true;
            this.lblThreshold.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblThreshold.Location = new System.Drawing.Point(130, 230);
            this.lblThreshold.Name = "lblThreshold";
            this.lblThreshold.Size = new System.Drawing.Size(98, 29);
            this.lblThreshold.TabIndex = 7;
            this.lblThreshold.Text = "Порог выхода";
            this.lblThreshold.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblZoom
            // 
            this.lblZoom.AutoSize = true;
            this.pnlControls.SetColumnSpan(this.lblZoom, 2);
            this.lblZoom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblZoom.Location = new System.Drawing.Point(3, 259);
            this.lblZoom.Name = "lblZoom";
            this.lblZoom.Size = new System.Drawing.Size(225, 15);
            this.lblZoom.TabIndex = 8;
            this.lblZoom.Text = "Приближение";
            this.lblZoom.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // nudZoom
            // 
            this.pnlControls.SetColumnSpan(this.nudZoom, 2);
            this.nudZoom.DecimalPlaces = 15;
            this.nudZoom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudZoom.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudZoom.Location = new System.Drawing.Point(6, 277);
            this.nudZoom.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.nudZoom.Maximum = new decimal(new int[] { -1, -1, -1, 0 });
            this.nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 393216 });
            this.nudZoom.Name = "nudZoom";
            this.nudZoom.Size = new System.Drawing.Size(219, 23);
            this.nudZoom.TabIndex = 9;
            this.nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // cbThreads
            // 
            this.cbThreads.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbThreads.FormattingEnabled = true;
            this.cbThreads.Location = new System.Drawing.Point(6, 306);
            this.cbThreads.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.cbThreads.Name = "cbThreads";
            this.cbThreads.Size = new System.Drawing.Size(118, 23);
            this.cbThreads.TabIndex = 10;
            // 
            // lblThreads
            // 
            this.lblThreads.AutoSize = true;
            this.lblThreads.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblThreads.Location = new System.Drawing.Point(130, 303);
            this.lblThreads.Name = "lblThreads";
            this.lblThreads.Size = new System.Drawing.Size(98, 29);
            this.lblThreads.TabIndex = 11;
            this.lblThreads.Text = "Потоки ЦП";
            this.lblThreads.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbSSAA
            // 
            this.cbSSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbSSAA.FormattingEnabled = true;
            this.cbSSAA.Location = new System.Drawing.Point(6, 335);
            this.cbSSAA.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.cbSSAA.Name = "cbSSAA";
            this.cbSSAA.Size = new System.Drawing.Size(118, 23);
            this.cbSSAA.TabIndex = 12;
            // 
            // lblSSAA
            // 
            this.lblSSAA.AutoSize = true;
            this.lblSSAA.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSSAA.Location = new System.Drawing.Point(130, 332);
            this.lblSSAA.Name = "lblSSAA";
            this.lblSSAA.Size = new System.Drawing.Size(98, 29);
            this.lblSSAA.TabIndex = 13;
            this.lblSSAA.Text = "Сглаживание";
            this.lblSSAA.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbSmooth
            // 
            this.cbSmooth.AutoSize = true;
            this.cbSmooth.Checked = true;
            this.cbSmooth.CheckState = System.Windows.Forms.CheckState.Checked;
            this.pnlControls.SetColumnSpan(this.cbSmooth, 2);
            this.cbSmooth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbSmooth.Location = new System.Drawing.Point(6, 364);
            this.cbSmooth.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.cbSmooth.Name = "cbSmooth";
            this.cbSmooth.Size = new System.Drawing.Size(222, 19);
            this.cbSmooth.TabIndex = 14;
            this.cbSmooth.Text = "Плавное окрашивание";
            this.cbSmooth.UseVisualStyleBackColor = true;
            // 
            // btnSaveHighRes
            // 
            this.pnlControls.SetColumnSpan(this.btnSaveHighRes, 2);
            this.btnSaveHighRes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveHighRes.Location = new System.Drawing.Point(6, 389);
            this.btnSaveHighRes.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.btnSaveHighRes.Name = "btnSaveHighRes";
            this.btnSaveHighRes.Size = new System.Drawing.Size(219, 39);
            this.btnSaveHighRes.TabIndex = 15;
            this.btnSaveHighRes.Text = "Сохранить изображение";
            this.btnSaveHighRes.UseVisualStyleBackColor = true;
            this.btnSaveHighRes.Click += new System.EventHandler(this.btnSaveHighRes_Click);
            // 
            // btnConfigurePalette
            // 
            this.pnlControls.SetColumnSpan(this.btnConfigurePalette, 2);
            this.btnConfigurePalette.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnConfigurePalette.Location = new System.Drawing.Point(6, 434);
            this.btnConfigurePalette.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.btnConfigurePalette.Name = "btnConfigurePalette";
            this.btnConfigurePalette.Size = new System.Drawing.Size(219, 39);
            this.btnConfigurePalette.TabIndex = 16;
            this.btnConfigurePalette.Text = "Настроить палитру";
            this.btnConfigurePalette.UseVisualStyleBackColor = true;
            this.btnConfigurePalette.Click += new System.EventHandler(this.btnConfigurePalette_Click);
            // 
            // btnRender
            // 
            this.pnlControls.SetColumnSpan(this.btnRender, 2);
            this.btnRender.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRender.Location = new System.Drawing.Point(6, 479);
            this.btnRender.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.btnRender.Name = "btnRender";
            this.btnRender.Size = new System.Drawing.Size(219, 39);
            this.btnRender.TabIndex = 17;
            this.btnRender.Text = "Запустить рендер";
            this.btnRender.UseVisualStyleBackColor = true;
            this.btnRender.Click += new System.EventHandler(this.btnRender_Click);
            // 
            // btnStateManager
            // 
            this.pnlControls.SetColumnSpan(this.btnStateManager, 2);
            this.btnStateManager.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStateManager.Location = new System.Drawing.Point(6, 524);
            this.btnStateManager.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.btnStateManager.Name = "btnStateManager";
            this.btnStateManager.Size = new System.Drawing.Size(219, 39);
            this.btnStateManager.TabIndex = 18;
            this.btnStateManager.Text = "Менеджер сохранений";
            this.btnStateManager.UseVisualStyleBackColor = true;
            this.btnStateManager.Click += new System.EventHandler(this.btnStateManager_Click);
            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.pnlControls.SetColumnSpan(this.lblProgress, 2);
            this.lblProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblProgress.Location = new System.Drawing.Point(3, 566);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(225, 20);
            this.lblProgress.TabIndex = 19;
            this.lblProgress.Text = "Обработка";
            this.lblProgress.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // pbRenderProgress
            // 
            this.pnlControls.SetColumnSpan(this.pbRenderProgress, 2);
            this.pbRenderProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbRenderProgress.Location = new System.Drawing.Point(6, 589);
            this.pbRenderProgress.Margin = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.pbRenderProgress.Name = "pbRenderProgress";
            this.pbRenderProgress.Size = new System.Drawing.Size(219, 24);
            this.pbRenderProgress.TabIndex = 20;
            // 
            // canvas
            // 
            this.canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvas.Location = new System.Drawing.Point(231, 0);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(853, 636);
            this.canvas.TabIndex = 1;
            this.canvas.TabStop = false;
            // 
            // FractalNovaForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 636);
            this.Controls.Add(this.canvas);
            this.Controls.Add(this.pnlControls);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1100, 675);
            this.Name = "FractalNovaForm";
            this.Text = "Фрактал Nova";
            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            this.gbNovaParameters.ResumeLayout(false);
            this.tlpNovaParameters.ResumeLayout(false);
            this.tlpNovaParameters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudP_Re)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudP_Im)).EndInit();
            this.gbInitialConditions.ResumeLayout(false);
            this.tlpInitialConditions.ResumeLayout(false);
            this.tlpInitialConditions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudZ0_Re)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZ0_Im)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudM)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudZoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.TableLayoutPanel pnlControls;
        private System.Windows.Forms.PictureBox canvas;
        private System.Windows.Forms.GroupBox gbNovaParameters;
        private System.Windows.Forms.TableLayoutPanel tlpNovaParameters;
        private System.Windows.Forms.NumericUpDown nudP_Re;
        private System.Windows.Forms.Label lblP_Re;
        private System.Windows.Forms.NumericUpDown nudP_Im;
        private System.Windows.Forms.Label lblP_Im;
        private System.Windows.Forms.GroupBox gbInitialConditions;
        private System.Windows.Forms.TableLayoutPanel tlpInitialConditions;
        private System.Windows.Forms.NumericUpDown nudZ0_Re;
        private System.Windows.Forms.Label lblZ0_Re;
        private System.Windows.Forms.NumericUpDown nudZ0_Im;
        private System.Windows.Forms.Label lblZ0_Im;
        private System.Windows.Forms.NumericUpDown nudM;
        private System.Windows.Forms.Label lblM;
        private System.Windows.Forms.NumericUpDown nudIterations;
        private System.Windows.Forms.Label lblIterations;
        private System.Windows.Forms.NumericUpDown nudThreshold;
        private System.Windows.Forms.Label lblThreshold;
        private System.Windows.Forms.Label lblZoom;
        private System.Windows.Forms.NumericUpDown nudZoom;
        private System.Windows.Forms.ComboBox cbThreads;
        private System.Windows.Forms.Label lblThreads;
        private System.Windows.Forms.ComboBox cbSSAA;
        private System.Windows.Forms.Label lblSSAA;
        private System.Windows.Forms.CheckBox cbSmooth;
        private System.Windows.Forms.Button btnSaveHighRes;
        private System.Windows.Forms.Button btnConfigurePalette;
        private System.Windows.Forms.Button btnRender;
        private System.Windows.Forms.Button btnStateManager;
        private System.Windows.Forms.Label lblProgress;
        private System.Windows.Forms.ProgressBar pbRenderProgress;
    }
}