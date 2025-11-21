using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace FractalExplorer.Forms
{
    partial class FractalNovaJuliaForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FractalNovaJuliaForm));
            pnlControls = new TableLayoutPanel();
            gbNovaParameters = new GroupBox();
            tlpNovaParameters = new TableLayoutPanel();
            nudP_Re = new NumericUpDown();
            lblP_Re = new Label();
            nudP_Im = new NumericUpDown();
            lblP_Im = new Label();
            gbInitialConditions = new GroupBox();
            tlpInitialConditions = new TableLayoutPanel();
            nudZ0_Re = new NumericUpDown();
            lblZ0_Re = new Label();
            nudZ0_Im = new NumericUpDown();
            lblZ0_Im = new Label();
            gbJuliaConstant = new GroupBox();
            tlpJuliaConstant = new TableLayoutPanel();
            nudC_Re = new NumericUpDown();
            lblC_Re = new Label();
            nudC_Im = new NumericUpDown();
            lblC_Im = new Label();
            lblM = new Label();
            nudM = new NumericUpDown();
            nudIterations = new NumericUpDown();
            lblIterations = new Label();
            nudThreshold = new NumericUpDown();
            lblThreshold = new Label();
            lblZoom = new Label();
            nudZoom = new NumericUpDown();
            cbThreads = new ComboBox();
            lblThreads = new Label();
            cbSSAA = new ComboBox();
            lblSSAA = new Label();
            cbSmooth = new CheckBox();
            btnSaveHighRes = new Button();
            btnConfigurePalette = new Button();
            btnRender = new Button();
            btnStateManager = new Button();
            lblProgress = new Label();
            pbRenderProgress = new ProgressBar();
            pnlMapPreview = new Panel();
            pbMandelbrotPreview = new PictureBox();
            canvas = new PictureBox();
            pnlControls.SuspendLayout();
            gbNovaParameters.SuspendLayout();
            tlpNovaParameters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudP_Re).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudP_Im).BeginInit();
            gbInitialConditions.SuspendLayout();
            tlpInitialConditions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudZ0_Re).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZ0_Im).BeginInit();
            gbJuliaConstant.SuspendLayout();
            tlpJuliaConstant.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudC_Re).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudC_Im).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudM).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).BeginInit();
            pnlMapPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbMandelbrotPreview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)canvas).BeginInit();
            SuspendLayout();

            // pnlControls
            pnlControls.ColumnCount = 2;
            pnlControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            pnlControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            pnlControls.Controls.Add(gbNovaParameters, 0, 0);
            pnlControls.Controls.Add(gbInitialConditions, 0, 1);
            pnlControls.Controls.Add(gbJuliaConstant, 0, 2); // NEW
            pnlControls.Controls.Add(lblM, 1, 3);
            pnlControls.Controls.Add(nudM, 0, 3);
            pnlControls.Controls.Add(nudIterations, 0, 4);
            pnlControls.Controls.Add(lblIterations, 1, 4);
            pnlControls.Controls.Add(nudThreshold, 0, 5);
            pnlControls.Controls.Add(lblThreshold, 1, 5);
            pnlControls.Controls.Add(lblZoom, 0, 6);
            pnlControls.Controls.Add(nudZoom, 0, 7);
            pnlControls.Controls.Add(cbThreads, 0, 8);
            pnlControls.Controls.Add(lblThreads, 1, 8);
            pnlControls.Controls.Add(cbSSAA, 0, 9);
            pnlControls.Controls.Add(lblSSAA, 1, 9);
            pnlControls.Controls.Add(cbSmooth, 0, 10);
            pnlControls.Controls.Add(btnSaveHighRes, 0, 11);
            pnlControls.Controls.Add(btnConfigurePalette, 0, 12);
            pnlControls.Controls.Add(btnRender, 0, 13);
            pnlControls.Controls.Add(btnStateManager, 0, 14);
            pnlControls.Controls.Add(lblProgress, 0, 15);
            pnlControls.Controls.Add(pbRenderProgress, 0, 16);
            pnlControls.Controls.Add(pnlMapPreview, 0, 17); // NEW
            pnlControls.Dock = DockStyle.Left;
            pnlControls.Location = new Point(0, 0);
            pnlControls.Name = "pnlControls";
            pnlControls.RowCount = 19;
            pnlControls.RowStyles.Add(new RowStyle()); // P
            pnlControls.RowStyles.Add(new RowStyle()); // Z0
            pnlControls.RowStyles.Add(new RowStyle()); // C (Julia)
            pnlControls.RowStyles.Add(new RowStyle()); // M
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle());
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Absolute, 160F)); // Map Preview
            pnlControls.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            pnlControls.Size = new Size(240, 680);
            pnlControls.TabIndex = 0;

            // gbNovaParameters (P)
            pnlControls.SetColumnSpan(gbNovaParameters, 2);
            gbNovaParameters.Controls.Add(tlpNovaParameters);
            gbNovaParameters.Dock = DockStyle.Fill;
            gbNovaParameters.Location = new Point(3, 3);
            gbNovaParameters.Name = "gbNovaParameters";
            gbNovaParameters.Size = new Size(234, 80);
            gbNovaParameters.TabIndex = 0;
            gbNovaParameters.TabStop = false;
            gbNovaParameters.Text = "Параметры степени (P)";

            // tlpNovaParameters
            tlpNovaParameters.ColumnCount = 2;
            tlpNovaParameters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tlpNovaParameters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tlpNovaParameters.Controls.Add(nudP_Re, 0, 0);
            tlpNovaParameters.Controls.Add(lblP_Re, 1, 0);
            tlpNovaParameters.Controls.Add(nudP_Im, 0, 1);
            tlpNovaParameters.Controls.Add(lblP_Im, 1, 1);
            tlpNovaParameters.Dock = DockStyle.Fill;
            tlpNovaParameters.Location = new Point(3, 19);
            tlpNovaParameters.Name = "tlpNovaParameters";
            tlpNovaParameters.RowCount = 2;
            tlpNovaParameters.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpNovaParameters.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpNovaParameters.Size = new Size(228, 58);
            tlpNovaParameters.TabIndex = 0;

            // nudP_Re
            nudP_Re.DecimalPlaces = 15;
            nudP_Re.Dock = DockStyle.Fill;
            nudP_Re.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudP_Re.Location = new Point(3, 3);
            nudP_Re.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudP_Re.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            nudP_Re.Name = "nudP_Re";
            nudP_Re.Size = new Size(119, 23);
            nudP_Re.TabIndex = 0;
            nudP_Re.Value = new decimal(new int[] { 3, 0, 0, 0 });

            // lblP_Re
            lblP_Re.AutoSize = true;
            lblP_Re.Dock = DockStyle.Fill;
            lblP_Re.Location = new Point(128, 0);
            lblP_Re.Name = "lblP_Re";
            lblP_Re.Size = new Size(97, 29);
            lblP_Re.TabIndex = 1;
            lblP_Re.Text = "Re";
            lblP_Re.TextAlign = ContentAlignment.MiddleLeft;

            // nudP_Im
            nudP_Im.DecimalPlaces = 15;
            nudP_Im.Dock = DockStyle.Fill;
            nudP_Im.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudP_Im.Location = new Point(3, 32);
            nudP_Im.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudP_Im.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            nudP_Im.Name = "nudP_Im";
            nudP_Im.Size = new Size(119, 23);
            nudP_Im.TabIndex = 2;

            // lblP_Im
            lblP_Im.AutoSize = true;
            lblP_Im.Dock = DockStyle.Fill;
            lblP_Im.Location = new Point(128, 29);
            lblP_Im.Name = "lblP_Im";
            lblP_Im.Size = new Size(97, 29);
            lblP_Im.TabIndex = 3;
            lblP_Im.Text = "Im";
            lblP_Im.TextAlign = ContentAlignment.MiddleLeft;

            // gbInitialConditions (Z0)
            pnlControls.SetColumnSpan(gbInitialConditions, 2);
            gbInitialConditions.Controls.Add(tlpInitialConditions);
            gbInitialConditions.Dock = DockStyle.Fill;
            gbInitialConditions.Location = new Point(3, 89);
            gbInitialConditions.Name = "gbInitialConditions";
            gbInitialConditions.Size = new Size(234, 80);
            gbInitialConditions.TabIndex = 1;
            gbInitialConditions.TabStop = false;
            gbInitialConditions.Text = "Начальные условия (Z₀)";

            // tlpInitialConditions
            tlpInitialConditions.ColumnCount = 2;
            tlpInitialConditions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tlpInitialConditions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tlpInitialConditions.Controls.Add(nudZ0_Re, 0, 0);
            tlpInitialConditions.Controls.Add(lblZ0_Re, 1, 0);
            tlpInitialConditions.Controls.Add(nudZ0_Im, 0, 1);
            tlpInitialConditions.Controls.Add(lblZ0_Im, 1, 1);
            tlpInitialConditions.Dock = DockStyle.Fill;
            tlpInitialConditions.Location = new Point(3, 19);
            tlpInitialConditions.Name = "tlpInitialConditions";
            tlpInitialConditions.RowCount = 2;
            tlpInitialConditions.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpInitialConditions.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpInitialConditions.Size = new Size(228, 58);
            tlpInitialConditions.TabIndex = 0;

            // nudZ0_Re
            nudZ0_Re.DecimalPlaces = 15;
            nudZ0_Re.Dock = DockStyle.Fill;
            nudZ0_Re.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudZ0_Re.Location = new Point(3, 3);
            nudZ0_Re.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudZ0_Re.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            nudZ0_Re.Name = "nudZ0_Re";
            nudZ0_Re.Size = new Size(119, 23);
            nudZ0_Re.TabIndex = 0;
            nudZ0_Re.Value = new decimal(new int[] { 1, 0, 0, 0 });

            // lblZ0_Re
            lblZ0_Re.AutoSize = true;
            lblZ0_Re.Dock = DockStyle.Fill;
            lblZ0_Re.Location = new Point(128, 0);
            lblZ0_Re.Name = "lblZ0_Re";
            lblZ0_Re.Size = new Size(97, 29);
            lblZ0_Re.TabIndex = 1;
            lblZ0_Re.Text = "Re";
            lblZ0_Re.TextAlign = ContentAlignment.MiddleLeft;

            // nudZ0_Im
            nudZ0_Im.DecimalPlaces = 15;
            nudZ0_Im.Dock = DockStyle.Fill;
            nudZ0_Im.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudZ0_Im.Location = new Point(3, 32);
            nudZ0_Im.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudZ0_Im.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            nudZ0_Im.Name = "nudZ0_Im";
            nudZ0_Im.Size = new Size(119, 23);
            nudZ0_Im.TabIndex = 2;

            // lblZ0_Im
            lblZ0_Im.AutoSize = true;
            lblZ0_Im.Dock = DockStyle.Fill;
            lblZ0_Im.Location = new Point(128, 29);
            lblZ0_Im.Name = "lblZ0_Im";
            lblZ0_Im.Size = new Size(97, 29);
            lblZ0_Im.TabIndex = 3;
            lblZ0_Im.Text = "Im";
            lblZ0_Im.TextAlign = ContentAlignment.MiddleLeft;

            // gbJuliaConstant
            pnlControls.SetColumnSpan(gbJuliaConstant, 2);
            gbJuliaConstant.Controls.Add(tlpJuliaConstant);
            gbJuliaConstant.Dock = DockStyle.Fill;
            gbJuliaConstant.Location = new Point(3, 175);
            gbJuliaConstant.Name = "gbJuliaConstant";
            gbJuliaConstant.Size = new Size(234, 80);
            gbJuliaConstant.TabIndex = 2;
            gbJuliaConstant.TabStop = false;
            gbJuliaConstant.Text = "Константа C";

            // tlpJuliaConstant
            tlpJuliaConstant.ColumnCount = 2;
            tlpJuliaConstant.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            tlpJuliaConstant.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            tlpJuliaConstant.Controls.Add(nudC_Re, 0, 0);
            tlpJuliaConstant.Controls.Add(lblC_Re, 1, 0);
            tlpJuliaConstant.Controls.Add(nudC_Im, 0, 1);
            tlpJuliaConstant.Controls.Add(lblC_Im, 1, 1);
            tlpJuliaConstant.Dock = DockStyle.Fill;
            tlpJuliaConstant.Location = new Point(3, 19);
            tlpJuliaConstant.Name = "tlpJuliaConstant";
            tlpJuliaConstant.RowCount = 2;
            tlpJuliaConstant.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpJuliaConstant.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpJuliaConstant.Size = new Size(228, 58);
            tlpJuliaConstant.TabIndex = 0;

            // nudC_Re
            nudC_Re.DecimalPlaces = 15;
            nudC_Re.Dock = DockStyle.Fill;
            nudC_Re.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudC_Re.Location = new Point(3, 3);
            nudC_Re.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudC_Re.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudC_Re.Name = "nudC_Re";
            nudC_Re.Size = new Size(119, 23);
            nudC_Re.TabIndex = 0;

            // lblC_Re
            lblC_Re.AutoSize = true;
            lblC_Re.Dock = DockStyle.Fill;
            lblC_Re.Location = new Point(128, 0);
            lblC_Re.Name = "lblC_Re";
            lblC_Re.Size = new Size(97, 29);
            lblC_Re.TabIndex = 1;
            lblC_Re.Text = "Re";
            lblC_Re.TextAlign = ContentAlignment.MiddleLeft;

            // nudC_Im
            nudC_Im.DecimalPlaces = 15;
            nudC_Im.Dock = DockStyle.Fill;
            nudC_Im.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            nudC_Im.Location = new Point(3, 32);
            nudC_Im.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            nudC_Im.Minimum = new decimal(new int[] { 2, 0, 0, int.MinValue });
            nudC_Im.Name = "nudC_Im";
            nudC_Im.Size = new Size(119, 23);
            nudC_Im.TabIndex = 2;

            // lblC_Im
            lblC_Im.AutoSize = true;
            lblC_Im.Dock = DockStyle.Fill;
            lblC_Im.Location = new Point(128, 29);
            lblC_Im.Name = "lblC_Im";
            lblC_Im.Size = new Size(97, 29);
            lblC_Im.TabIndex = 3;
            lblC_Im.Text = "Im";
            lblC_Im.TextAlign = ContentAlignment.MiddleLeft;

            // lblM
            lblM.AutoSize = true;
            lblM.Dock = DockStyle.Fill;
            lblM.Location = new Point(135, 258);
            lblM.Name = "lblM";
            lblM.Size = new Size(102, 29);
            lblM.TabIndex = 3;
            lblM.Text = "Релаксация (m)";
            lblM.TextAlign = ContentAlignment.MiddleLeft;

            // nudM
            nudM.DecimalPlaces = 3;
            nudM.Dock = DockStyle.Fill;
            nudM.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            nudM.Location = new Point(6, 261);
            nudM.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            nudM.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            nudM.Name = "nudM";
            nudM.Size = new Size(126, 23);
            nudM.TabIndex = 2;
            nudM.Value = new decimal(new int[] { 1, 0, 0, 0 });

            // ... (Common controls: Iterations, Threshold, etc.) ...
            // nudIterations
            nudIterations.Dock = DockStyle.Fill;
            nudIterations.Location = new Point(6, 290);
            nudIterations.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            nudIterations.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            nudIterations.Name = "nudIterations";
            nudIterations.Size = new Size(126, 23);
            nudIterations.TabIndex = 4;
            nudIterations.Value = new decimal(new int[] { 100, 0, 0, 0 });

            // lblIterations
            lblIterations.AutoSize = true;
            lblIterations.Dock = DockStyle.Fill;
            lblIterations.Location = new Point(135, 287);
            lblIterations.Name = "lblIterations";
            lblIterations.Size = new Size(102, 29);
            lblIterations.TabIndex = 5;
            lblIterations.Text = "Итерации";
            lblIterations.TextAlign = ContentAlignment.MiddleLeft;

            // nudThreshold
            nudThreshold.DecimalPlaces = 1;
            nudThreshold.Dock = DockStyle.Fill;
            nudThreshold.Location = new Point(6, 319);
            nudThreshold.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            nudThreshold.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            nudThreshold.Name = "nudThreshold";
            nudThreshold.Size = new Size(126, 23);
            nudThreshold.TabIndex = 6;
            nudThreshold.Value = new decimal(new int[] { 10, 0, 0, 0 });

            // lblThreshold
            lblThreshold.AutoSize = true;
            lblThreshold.Dock = DockStyle.Fill;
            lblThreshold.Location = new Point(135, 316);
            lblThreshold.Name = "lblThreshold";
            lblThreshold.Size = new Size(102, 29);
            lblThreshold.TabIndex = 7;
            lblThreshold.Text = "Порог";
            lblThreshold.TextAlign = ContentAlignment.MiddleLeft;

            // lblZoom
            lblZoom.AutoSize = true;
            pnlControls.SetColumnSpan(lblZoom, 2);
            lblZoom.Dock = DockStyle.Fill;
            lblZoom.Location = new Point(3, 345);
            lblZoom.Name = "lblZoom";
            lblZoom.Size = new Size(234, 15);
            lblZoom.TabIndex = 8;
            lblZoom.Text = "Приближение";
            lblZoom.TextAlign = ContentAlignment.MiddleCenter;

            // nudZoom
            pnlControls.SetColumnSpan(nudZoom, 2);
            nudZoom.DecimalPlaces = 15;
            nudZoom.Dock = DockStyle.Fill;
            nudZoom.Location = new Point(6, 363);
            nudZoom.Maximum = new decimal(new int[] { -1, -1, -1, 0 });
            nudZoom.Minimum = new decimal(new int[] { 1, 0, 0, 393216 });
            nudZoom.Name = "nudZoom";
            nudZoom.Size = new Size(228, 23);
            nudZoom.TabIndex = 9;
            nudZoom.Value = new decimal(new int[] { 1, 0, 0, 0 });

            // cbThreads
            cbThreads.Dock = DockStyle.Fill;
            cbThreads.FormattingEnabled = true;
            cbThreads.Location = new Point(6, 392);
            cbThreads.Name = "cbThreads";
            cbThreads.Size = new Size(126, 23);
            cbThreads.TabIndex = 10;

            // lblThreads
            lblThreads.AutoSize = true;
            lblThreads.Dock = DockStyle.Fill;
            lblThreads.Location = new Point(135, 389);
            lblThreads.Name = "lblThreads";
            lblThreads.Size = new Size(102, 29);
            lblThreads.TabIndex = 11;
            lblThreads.Text = "Потоки";
            lblThreads.TextAlign = ContentAlignment.MiddleLeft;

            // cbSSAA
            cbSSAA.Dock = DockStyle.Fill;
            cbSSAA.FormattingEnabled = true;
            cbSSAA.Location = new Point(6, 421);
            cbSSAA.Name = "cbSSAA";
            cbSSAA.Size = new Size(126, 23);
            cbSSAA.TabIndex = 12;

            // lblSSAA
            lblSSAA.AutoSize = true;
            lblSSAA.Dock = DockStyle.Fill;
            lblSSAA.Location = new Point(135, 418);
            lblSSAA.Name = "lblSSAA";
            lblSSAA.Size = new Size(102, 29);
            lblSSAA.TabIndex = 13;
            lblSSAA.Text = "SSAA";
            lblSSAA.TextAlign = ContentAlignment.MiddleLeft;

            // cbSmooth
            cbSmooth.AutoSize = true;
            cbSmooth.Checked = true;
            cbSmooth.CheckState = CheckState.Checked;
            pnlControls.SetColumnSpan(cbSmooth, 2);
            cbSmooth.Dock = DockStyle.Fill;
            cbSmooth.Location = new Point(6, 450);
            cbSmooth.Name = "cbSmooth";
            cbSmooth.Size = new Size(228, 19);
            cbSmooth.TabIndex = 14;
            cbSmooth.Text = "Плавное окрашивание";
            cbSmooth.UseVisualStyleBackColor = true;

            // btnSaveHighRes
            pnlControls.SetColumnSpan(btnSaveHighRes, 2);
            btnSaveHighRes.Dock = DockStyle.Fill;
            btnSaveHighRes.Location = new Point(6, 475);
            btnSaveHighRes.Name = "btnSaveHighRes";
            btnSaveHighRes.Size = new Size(228, 34);
            btnSaveHighRes.TabIndex = 15;
            btnSaveHighRes.Text = "Сохранить (HR)";
            btnSaveHighRes.UseVisualStyleBackColor = true;

            // btnConfigurePalette
            pnlControls.SetColumnSpan(btnConfigurePalette, 2);
            btnConfigurePalette.Dock = DockStyle.Fill;
            btnConfigurePalette.Location = new Point(6, 515);
            btnConfigurePalette.Name = "btnConfigurePalette";
            btnConfigurePalette.Size = new Size(228, 34);
            btnConfigurePalette.TabIndex = 16;
            btnConfigurePalette.Text = "Палитра";
            btnConfigurePalette.UseVisualStyleBackColor = true;

            // btnRender
            pnlControls.SetColumnSpan(btnRender, 2);
            btnRender.Dock = DockStyle.Fill;
            btnRender.Location = new Point(6, 555);
            btnRender.Name = "btnRender";
            btnRender.Size = new Size(228, 34);
            btnRender.TabIndex = 17;
            btnRender.Text = "Рендер";
            btnRender.UseVisualStyleBackColor = true;

            // btnStateManager
            pnlControls.SetColumnSpan(btnStateManager, 2);
            btnStateManager.Dock = DockStyle.Fill;
            btnStateManager.Location = new Point(6, 595);
            btnStateManager.Name = "btnStateManager";
            btnStateManager.Size = new Size(228, 34);
            btnStateManager.TabIndex = 18;
            btnStateManager.Text = "Менеджер";
            btnStateManager.UseVisualStyleBackColor = true;

            // lblProgress
            lblProgress.AutoSize = true;
            pnlControls.SetColumnSpan(lblProgress, 2);
            lblProgress.Dock = DockStyle.Fill;
            lblProgress.Location = new Point(3, 632);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(234, 20);
            lblProgress.TabIndex = 19;
            lblProgress.Text = "Прогресс";
            lblProgress.TextAlign = ContentAlignment.BottomCenter;

            // pbRenderProgress
            pnlControls.SetColumnSpan(pbRenderProgress, 2);
            pbRenderProgress.Dock = DockStyle.Fill;
            pbRenderProgress.Location = new Point(6, 655);
            pbRenderProgress.Name = "pbRenderProgress";
            pbRenderProgress.Size = new Size(228, 24);
            pbRenderProgress.TabIndex = 20;

            // pnlMapPreview (The new panel for map)
            pnlControls.SetColumnSpan(pnlMapPreview, 2);
            pnlMapPreview.Controls.Add(pbMandelbrotPreview);
            pnlMapPreview.Dock = DockStyle.Fill;
            pnlMapPreview.Location = new Point(3, 685);
            pnlMapPreview.Name = "pnlMapPreview";
            pnlMapPreview.Size = new Size(234, 154);
            pnlMapPreview.TabIndex = 21;

            // pbMandelbrotPreview
            pbMandelbrotPreview.BorderStyle = BorderStyle.FixedSingle;
            pbMandelbrotPreview.Dock = DockStyle.Fill;
            pbMandelbrotPreview.Location = new Point(0, 0);
            pbMandelbrotPreview.Name = "pbMandelbrotPreview";
            pbMandelbrotPreview.Size = new Size(234, 154);
            pbMandelbrotPreview.SizeMode = PictureBoxSizeMode.StretchImage;
            pbMandelbrotPreview.TabIndex = 0;
            pbMandelbrotPreview.TabStop = false;

            // canvas
            canvas.Dock = DockStyle.Fill;
            canvas.Location = new Point(240, 0);
            canvas.Name = "canvas";
            canvas.Size = new Size(853, 680);
            canvas.TabIndex = 1;
            canvas.TabStop = false;

            // Form
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1093, 680);
            Controls.Add(canvas);
            Controls.Add(pnlControls);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1100, 720);
            Name = "FractalNovaJuliaForm";
            Text = "Фрактал Nova Julia";

            pnlControls.ResumeLayout(false);
            pnlControls.PerformLayout();
            gbNovaParameters.ResumeLayout(false);
            tlpNovaParameters.ResumeLayout(false);
            tlpNovaParameters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudP_Re).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudP_Im).EndInit();
            gbInitialConditions.ResumeLayout(false);
            tlpInitialConditions.ResumeLayout(false);
            tlpInitialConditions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudZ0_Re).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZ0_Im).EndInit();
            gbJuliaConstant.ResumeLayout(false);
            tlpJuliaConstant.ResumeLayout(false);
            tlpJuliaConstant.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudC_Re).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudC_Im).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudM).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIterations).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudThreshold).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudZoom).EndInit();
            pnlMapPreview.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pbMandelbrotPreview).EndInit();
            ((System.ComponentModel.ISupportInitialize)canvas).EndInit();
            ResumeLayout(false);
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

        // New Julia specific controls
        private System.Windows.Forms.GroupBox gbJuliaConstant;
        private System.Windows.Forms.TableLayoutPanel tlpJuliaConstant;
        private System.Windows.Forms.NumericUpDown nudC_Re;
        private System.Windows.Forms.Label lblC_Re;
        private System.Windows.Forms.NumericUpDown nudC_Im;
        private System.Windows.Forms.Label lblC_Im;
        private System.Windows.Forms.Panel pnlMapPreview;
        private System.Windows.Forms.PictureBox pbMandelbrotPreview;
    }
}
