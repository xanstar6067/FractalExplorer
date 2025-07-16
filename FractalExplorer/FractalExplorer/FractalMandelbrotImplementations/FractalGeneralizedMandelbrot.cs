using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
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
            lblRe.Visible = false;
            nudRe.Visible = false;
            lblIm.Visible = false;
            nudIm.Visible = false;
            mandelbrotPreviewPanel.Visible = false;

            lblPower = new Label
            {
                Name = "lblPower",
                Text = "Степень (p)",
                Location = new Point(138, 222),
                AutoSize = true
            };

            nudPower = new NumericUpDown
            {
                Name = "nudPower",
                Minimum = 2,
                Maximum = 10,
                DecimalPlaces = 2,
                Increment = 0.1m,
                Value = 3.0m,
                Location = new Point(12, 220),
                Size = new Size(120, 23)
            };

            nudPower.ValueChanged += ParamControl_Changed;

            pnlControls.Controls.Add(lblPower);
            pnlControls.Controls.Add(nudPower);
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

        // ---------- НОВЫЙ ПЕРЕОПРЕДЕЛЕННЫЙ МЕТОД 1 ----------
        /// <summary>
        /// Асинхронно рендерит плитку превью, учитывая параметр Power.
        /// </summary>
        public override Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase stateBase, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(stateBase.PreviewParametersJson)) return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];

                GeneralizedMandelbrotPreviewParams previewParams;
                try
                {
                    previewParams = JsonSerializer.Deserialize<GeneralizedMandelbrotPreviewParams>(stateBase.PreviewParametersJson);
                }
                catch
                {
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];
                }

                var previewEngine = new GeneralizedMandelbrotEngine
                {
                    Power = previewParams.Power // <-- Ключевой момент!
                };

                previewEngine.MaxIterations = previewParams.Iterations;
                previewEngine.CenterX = previewParams.CenterX;
                previewEngine.CenterY = previewParams.CenterY;
                if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
                previewEngine.Scale = this.BaseScale / previewParams.Zoom;
                previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;

                // Логика палитры скопирована из базового класса, т.к. доступ к ней private
                var paletteManager = new PaletteManager(); // Создаем временный менеджер для доступа к палитрам
                var paletteForPreview = paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? paletteManager.Palettes.First();
                int effectiveMaxColorIterations = paletteForPreview.AlignWithRenderIterations ? previewEngine.MaxIterations : paletteForPreview.MaxColorIterations;

                previewEngine.UseSmoothColoring = false;
                previewEngine.MaxColorIterations = effectiveMaxColorIterations;
                // Мы не можем получить доступ к private методу GenerateDiscretePaletteFunction, поэтому придется создать его "локальную" версию.
                // Для простоты, мы можем воспользоваться тем, что движок уже умеет это делать, если мы передадим ему палитру.
                // Но так как у нас нет доступа к оригинальному методу, воссоздадим его здесь упрощенно для превью.
                previewEngine.Palette = (iter, maxIter, maxColorIter) =>
                {
                    if (iter == maxIter) return Color.Black;
                    if (paletteForPreview.Colors.Count == 0) return Color.Black;
                    int colorIndex = (iter % effectiveMaxColorIterations) * paletteForPreview.Colors.Count / effectiveMaxColorIterations;
                    return paletteForPreview.Colors[colorIndex];
                };

                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }

        // ---------- НОВЫЙ ПЕРЕОПРЕДЕЛЕННЫЙ МЕТОД 2 ----------
        /// <summary>
        /// Рендерит полное превью, учитывая параметр Power.
        /// </summary>
        public override Bitmap RenderPreview(FractalSaveStateBase stateBase, int previewWidth, int previewHeight)
        {
            // Этот метод менее критичен, но для полноты картины его тоже стоит переопределить
            // Здесь мы можем схитрить и вызвать наш уже исправленный асинхронный метод
            var tile = new TileInfo(0, 0, previewWidth, previewHeight);
            var buffer = RenderPreviewTileAsync(stateBase, tile, previewWidth, previewHeight, previewWidth).Result;

            var bmp = new Bitmap(previewWidth, previewHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, previewWidth, previewHeight), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        #endregion
    }
}