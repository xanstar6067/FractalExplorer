using FractalExplorer.Utilities.SaveIO.SaveStateImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FractalExplorer.Utilities.SaveIO
{
    public interface ISaveLoadCapableFractal
    {
        string FractalTypeIdentifier { get; }
        FractalSaveStateBase GetCurrentStateForSave(string saveName);
        void LoadState(FractalSaveStateBase state);
        Bitmap RenderPreview(FractalSaveStateBase state, int previewWidth, int previewHeight);

        Type ConcreteSaveStateType { get; }
        List<FractalSaveStateBase> LoadAllSavesForThisType();
        void SaveAllSavesForThisType(List<FractalSaveStateBase> saves);
    }
}
