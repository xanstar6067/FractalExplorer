using FractalDraving;
using FractalExplorer.Engines;
using FractalExplorer.Resources;
using FractalExplorer.Utilities;
using FractalExplorer.Utilities.RenderUtilities;
using FractalExplorer.Utilities.SaveIO;
using FractalExplorer.Utilities.SaveIO.ColorPalettes;
using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System.Text.Json;

namespace FractalExplorer.Projects
{
    /// <summary>
    /// Форма для отображения и взаимодействия с фракталом Буффало.
    /// </summary>
    public partial class FractalBuffalo : FractalMandelbrotFamilyForm
    {
        public FractalBuffalo()
        {
            Text = "Фрактал Буффало";
        }

        protected override FractalMandelbrotFamilyEngine CreateEngine()
        {
            return new BuffaloEngine();
        }

        protected override void OnPostInitialize()
        {
            // Скрываем ненужные для этого фрактала элементы управления.
            lblRe.Visible = false;
            nudRe.Visible = false;
            lblIm.Visible = false;
            nudIm.Visible = false;
            mandelbrotPreviewPanel.Visible = false;
        }

        protected override string GetSaveFileNameDetails()
        {
            return "buffalo";
        }

        #region ISaveLoadCapableFractal Implementation

        public override string FractalTypeIdentifier => "Buffalo";
        public override Type ConcreteSaveStateType => typeof(MandelbrotFamilySaveState);

        public override FractalSaveStateBase GetCurrentStateForSave(string saveName)
        {
            var state = new MandelbrotFamilySaveState(this.FractalTypeIdentifier)
            {
                SaveName = saveName,
                Timestamp = DateTime.Now,
                CenterX = _centerX,
                CenterY = _centerY,
                Zoom = _zoom,
                Threshold = nudThreshold.Value,
                Iterations = (int)nudIterations.Value,
                PaletteName = _paletteManager.ActivePalette?.Name ?? "Стандартный серый",
                PreviewEngineType = this.FractalTypeIdentifier
            };

            var previewParams = new PreviewParams
            {
                CenterX = state.CenterX,
                CenterY = state.CenterY,
                Zoom = state.Zoom,
                Iterations = state.Iterations,
                PaletteName = state.PaletteName,
                Threshold = state.Threshold,
                PreviewEngineType = state.PreviewEngineType
            };
            state.PreviewParametersJson = JsonSerializer.Serialize(previewParams, new JsonSerializerOptions());
            return state;
        }

        public override void LoadState(FractalSaveStateBase stateBase)
        {
            base.LoadState(stateBase);
        }

        public override List<FractalSaveStateBase> LoadAllSavesForThisType()
        {
            var specificSaves = SaveFileManager.LoadSaves<MandelbrotFamilySaveState>(this.FractalTypeIdentifier);
            return specificSaves.Cast<FractalSaveStateBase>().ToList();
        }

        public override void SaveAllSavesForThisType(List<FractalSaveStateBase> saves)
        {
            var specificSaves = saves.Cast<MandelbrotFamilySaveState>().ToList();
            SaveFileManager.SaveSaves(this.FractalTypeIdentifier, specificSaves);
        }

        /// <summary>
        /// Асинхронно рендерит плитку превью, создавая правильный движок для Буффало.
        /// </summary>
        public override Task<byte[]> RenderPreviewTileAsync(FractalSaveStateBase stateBase, TileInfo tile, int totalWidth, int totalHeight, int tileSize)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(stateBase.PreviewParametersJson))
                    return new byte[tile.Bounds.Width * tile.Bounds.Height * 4];

                PreviewParams previewParams;
                try
                {
                    previewParams = JsonSerializer.Deserialize<PreviewParams>(stateBase.PreviewParametersJson);
                }
                catch { return new byte[tile.Bounds.Width * tile.Bounds.Height * 4]; }

                // Создаем правильный движок
                var previewEngine = new BuffaloEngine();

                // Настраиваем его из параметров
                previewEngine.MaxIterations = previewParams.Iterations;
                previewEngine.CenterX = previewParams.CenterX;
                previewEngine.CenterY = previewParams.CenterY;
                if (previewParams.Zoom == 0) previewParams.Zoom = 0.001m;
                previewEngine.Scale = BaseScale / previewParams.Zoom;
                previewEngine.ThresholdSquared = previewParams.Threshold * previewParams.Threshold;

                var paletteManager = new PaletteManager();
                var paletteForPreview = paletteManager.Palettes.FirstOrDefault(p => p.Name == previewParams.PaletteName) ?? paletteManager.Palettes.First();
                int effectiveMaxColorIterations = paletteForPreview.AlignWithRenderIterations ? previewEngine.MaxIterations : paletteForPreview.MaxColorIterations;

                previewEngine.UseSmoothColoring = false;
                previewEngine.MaxColorIterations = effectiveMaxColorIterations;
                previewEngine.Palette = GenerateDiscretePaletteFunction(paletteForPreview);
                previewEngine.SmoothPalette = GenerateSmoothPaletteFunction(paletteForPreview, effectiveMaxColorIterations);

                return previewEngine.RenderSingleTile(tile, totalWidth, totalHeight, out _);
            });
        }

        /// <summary>
        /// Рендерит полное превью (используется в редких случаях, но лучше реализовать).
        /// </summary>
        public override Bitmap RenderPreview(FractalSaveStateBase stateBase, int previewWidth, int previewHeight)
        {
            var tile = new TileInfo(0, 0, previewWidth, previewHeight);
            byte[] buffer = RenderPreviewTileAsync(stateBase, tile, previewWidth, previewHeight, previewWidth).Result;

            var bmp = new Bitmap(previewWidth, previewHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, previewWidth, previewHeight), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpData.Scan0, buffer.Length);
            bmp.UnlockBits(bmpData);

            // Для совместимости с некоторыми элементами UI, конвертируем в 24bppRgb
            Bitmap finalBmp = new Bitmap(bmp);
            bmp.Dispose();

            return finalBmp;
        }

        #endregion
    }
}