using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Globalization;

namespace FractalExplorer.Projects
{
    /// <summary>
    /// Форма для отображения и взаимодействия с фракталом Симоноброт (z -> |z|^p + c).
    /// </summary>
    public partial class FractalSimonobrot : FractalMandelbrotFamilyForm
    {
        private NumericUpDown nudPower;
        private Label lblPower;
        private CheckBox chkInversion;

        public FractalSimonobrot()
        {
            Text = "Фрактал Симоноброт";
        }

        protected override FractalMandelbrotFamilyEngine CreateEngine()
        {
            return new SimonobrotEngine();
        }

        protected override void OnPostInitialize()
        {
            // Скрываем стандартные элементы управления для константы C.
            lblRe.Visible = false;
            nudRe.Visible = false;
            lblIm.Visible = false;
            nudIm.Visible = false;
            mandelbrotPreviewPanel.Visible = false;

            pnlCustomControls.Visible = true;

            var innerTable = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 2, // Добавляем строку для инверсии
                Dock = DockStyle.Fill,
            };
            innerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            innerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            innerTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 29F));
            innerTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 29F));

            // Элементы управления для степени
            lblPower = new Label { Text = "Степень (p)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            nudPower = new NumericUpDown
            {
                Minimum = -10,
                Maximum = 10,
                DecimalPlaces = 2,
                Increment = 0.1m,
                Value = 2.0m,
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 3, 3, 3)
            };
            nudPower.ValueChanged += ParamControl_Changed;

            // Элемент управления для инверсии
            chkInversion = new CheckBox
            {
                Text = "Инверсия",
                Dock = DockStyle.Fill,
                Checked = false
            };
            chkInversion.CheckedChanged += ParamControl_Changed;

            innerTable.Controls.Add(nudPower, 0, 0);
            innerTable.Controls.Add(lblPower, 1, 0);
            innerTable.Controls.Add(chkInversion, 0, 1);
            innerTable.SetColumnSpan(chkInversion, 2); // Растягиваем на две колонки

            pnlCustomControls.Height = 60; // Увеличиваем высоту панели
            pnlCustomControls.Controls.Add(innerTable);
        }

        protected override void UpdateEngineSpecificParameters()
        {
            if (_fractalEngine is SimonobrotEngine engine)
            {
                engine.Power = nudPower.Value;
                engine.UseInversion = chkInversion.Checked;
            }
        }

        protected override string GetSaveFileNameDetails()
        {
            string powerString = nudPower.Value.ToString("F2", CultureInfo.InvariantCulture).Replace(".", "_");
            return $"simonobrot_p{powerString}";
        }

        // --- Временная реализация для запуска (позже будет дополнена) ---
        public override string FractalTypeIdentifier => "Simonobrot";
        public override Type ConcreteSaveStateType => typeof(GeneralizedMandelbrotSaveState); // Можно переиспользовать

        public override List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            // TODO: Будет реализовано на следующем этапе
            return new List<FractalSaveStateBase>();
        }

        public override void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            // TODO: Будет реализовано на следующем этапе
        }
    }
}