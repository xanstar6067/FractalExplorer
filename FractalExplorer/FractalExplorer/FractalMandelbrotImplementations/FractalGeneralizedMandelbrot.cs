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
            // Скрываем элементы, которые не нужны для множеств типа Мандельброта
            lblRe.Visible = false;
            nudRe.Visible = false;
            lblIm.Visible = false;
            nudIm.Visible = false;
            mandelbrotPreviewPanel.Visible = false;

            // --- Создаем новые контролы для степени 'p' ---
            lblPower = new Label
            {
                Name = "lblPower",
                Text = "Степень (p)",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                AutoSize = true,
            };

            nudPower = new NumericUpDown
            {
                Name = "nudPower",
                Minimum = 2,
                Maximum = 10,
                DecimalPlaces = 2,
                Increment = 0.1m,
                Value = 3.0m,
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 3, 3, 3)
            };

            nudPower.ValueChanged += ParamControl_Changed;

            // Добавляем контролы в таблицу на свои места
            // Индекс 8 - это строка сразу после "Сглаживание"
            pnlControls.Controls.Add(nudPower, 0, 8);
            pnlControls.Controls.Add(lblPower, 1, 8);
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

        /// <summary>
        /// Асинхронно рендерит плитку превью, корректно обрабатывая все типы палитр.
        /// </summary>
        public override Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase stateBase, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(stateBase.PreviewParametersJson)) return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];

                GeneralizedMandelbrotPreviewParams previewParams;
                try { previewParams = JsonSerializer.Deserialize<GeneralizedMandelbrotPreviewParams>(stateBase.PreviewParametersJson); }
                catch { return new byte[tile.Bounds.Width * tile.Bounds.Height * 4]; }

                var previewEngine = new GeneralizedMandelbrotEngine { Power = previewParams.Power };

                previewEngine.MaxIterations = previewParams.Iterations;
                previewEngine.CenterX = previewParams.CenterX;
                previewEngine.CenterY = previewParams.CenterY;
                if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
                previewEngine.Scale = this.BaseScale / previewParams.Zoom;
                previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;

                var paletteManager = new PaletteManager();
                var paletteForPreview = paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? paletteManager.Palettes.First();
                int effectiveMaxColorIterations = paletteForPreview.AlignWithRenderIterations ? previewEngine.MaxIterations : paletteForPreview.MaxColorIterations;

                previewEngine.UseSmoothColoring = false;
                previewEngine.MaxColorIterations = effectiveMaxColorIterations;

                // --- ИСПРАВЛЕНИЕ: Добавляем специальную обработку для серой палитры ---
                if (paletteForPreview.Name == "Стандартный серый")
                {
                    double gamma = paletteForPreview.Gamma;
                    previewEngine.Palette = (iter, maxIter, maxColorIter) =>
                    {
                        if (iter == maxIter) return Color.Black;
                        double logMax = Math.Log(maxColorIter + 1);
                        if (logMax <= 0) return Color.Black;
                        double tLog = Math.Log(Math.Min(iter, maxColorIter) + 1) / logMax;
                        int cVal = (int)(255.0 * (1 - tLog));
                        return ColorCorrection.ApplyGamma(Color.FromArgb(cVal, cVal, cVal), gamma);
                    };
                }
                else
                {
                    // Стандартная логика для всех остальных палитр на основе списка цветов
                    previewEngine.Palette = (iter, maxIter, maxColorIter) =>
                    {
                        if (iter == maxIter) return Color.Black;
                        if (paletteForPreview.Colors.Count == 0) return Color.Black;
                        int colorIndex = (iter % effectiveMaxColorIterations) * paletteForPreview.Colors.Count / effectiveMaxColorIterations;
                        return paletteForPreview.Colors[colorIndex];
                    };
                }

                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }

        /// <summary>
        /// Рендерит полное превью, используя исправленный асинхронный метод.
        /// </summary>
        public override Bitmap RenderPreview(FractalSaveStateBase stateBase, int previewWidth, int previewHeight)
        {
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