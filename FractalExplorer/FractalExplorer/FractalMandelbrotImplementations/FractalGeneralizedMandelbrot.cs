using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Resources; // ИСПРАВЛЕНИЕ 1: Добавлена эта строка для TileInfo
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.RenderUtilities;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Globalization;
using System.Text.Json;

namespace FractalExplorer.Projects
{
    /// <summary>
    /// Форма для отображения и взаимодействия с Обобщенным множеством Мандельброта (z -> z^p + c).
    /// </summary>
    public partial class FractalGeneralizedMandelbrot : FractalMandelbrotFamilyForm
    {
        private NumericUpDown nudPower;
        private Label lblPower;

        #region Constructor

        public FractalGeneralizedMandelbrot()
        {
            Text = "Обобщённое множество Мандельброта (z^p + c)";
        }

        #endregion

        #region Fractal Engine Overrides

        protected override FractalMandelbrotFamilyEngine CreateEngine()
        {
            return new GeneralizedMandelbrotEngine();
        }

        protected override void OnPostInitialize()
        {
            // Скрываем элементы, которые не нужны для множеств типа Мандельброта
            lblRe.Visible = false;
            nudRe.Visible = false;
            lblIm.Visible = false;
            nudIm.Visible = false;
            mandelbrotPreviewPanel.Visible = false;

            // --- ИСПОЛЬЗУЕМ ЗАГОТОВЛЕННЫЙ КОНТЕЙНЕР ---

            // 1. Делаем наш специальный контейнер видимым
            pnlCustomControls.Visible = true;

            // 2. Создаем маленькую внутреннюю таблицу для красивого расположения наших контролов
            var innerTable = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
            };
            innerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            innerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

            // 3. Создаем сами контролы
            lblPower = new Label
            {
                Text = "Степень (p)",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
            };
            nudPower = new NumericUpDown
            {
                Minimum = 2,
                Maximum = 10,
                DecimalPlaces = 2,
                Increment = 0.1m,
                Value = 3.0m,
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 3, 3, 3)
            };
            nudPower.ValueChanged += ParamControl_Changed;

            // 4. Добавляем контролы во внутреннюю таблицу
            innerTable.Controls.Add(nudPower, 0, 0);
            innerTable.Controls.Add(lblPower, 1, 0);

            // 5. Добавляем нашу маленькую таблицу в большой контейнер-заполнитель
            pnlCustomControls.Controls.Add(innerTable);
        }

        protected override void UpdateEngineSpecificParameters()
        {
            if (_fractalEngine is GeneralizedMandelbrotEngine engine)
            {
                engine.Power = nudPower.Value;
            }
        }

        protected override string GetSaveFileNameDetails()
        {
            string powerString = nudPower.Value.ToString("F2", CultureInfo.InvariantCulture).Replace(".", "_");
            return $"gen_mandelbrot_p{powerString}";
        }

        #endregion

        #region ISaveLoadCapableFractal Overrides

        public override string FractalTypeIdentifier => "GeneralizedMandelbrot";
        public override Type ConcreteSaveStateType => typeof(GeneralizedMandelbrotSaveState);

        public class GeneralizedMandelbrotPreviewParams : PreviewParams
        {
            public decimal Power { get; set; }
        }

        public override FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            var state = new GeneralizedMandelbrotSaveState(this.FractalTypeIdentifier)
            {
                SaveName = saveName,
                Timestamp = DateTime.Now,
                CenterX = _centerX,
                CenterY = _centerY,
                Zoom = _zoom,
                Threshold = nudThreshold.Value,
                Iterations = (int)nudIterations.Value,
                PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый",
                PreviewEngineType = this.FractalTypeIdentifier,
                Power = nudPower.Value
            };

            var previewParams = new GeneralizedMandelbrotPreviewParams
            {
                CenterX = state.CenterX,
                CenterY = state.CenterY,
                Zoom = state.Zoom,
                Iterations = Math.Min(state.Iterations, 1000),
                PaletteName = state.PaletteName,
                Threshold = state.Threshold,
                PreviewEngineType = state.PreviewEngineType,
                Power = state.Power
            };

            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams, new JsonSerializerOptions());
            return state;
        }

        public override void LoadState(FractalSaveStateBase stateBase)
        {
            base.LoadState(stateBase);
            if (stateBase is GeneralizedMandelbrotSaveState state)
            {
                nudPower.Value = Math.Max(nudPower.Minimum, Math.Min(nudPower.Maximum, state.Power));
            }
        }

        public override List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<GeneralizedMandelbrotSaveState>(this.FractalTypeIdentifier);
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        public override void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<GeneralizedMandelbrotSaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }


        

        //public override HighResRenderState GetRenderState()
        public virtual HighResRenderState GetRenderState()
        {
            // 1. Сначала получаем базовое состояние от родительского класса
            var state = base.GetRenderState();

            // 2. Добавляем наш уникальный параметр
            state.Power = this.nudPower.Value;
            state.EngineType = this.FractalTypeIdentifier; // Убедимся, что тип движка правильный

            // 3. Возвращаем дополненное состояние
            return state;
        }

        #endregion
    }
}