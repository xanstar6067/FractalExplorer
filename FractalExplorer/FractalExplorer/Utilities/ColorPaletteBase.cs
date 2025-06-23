using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;

namespace FractalExplorer.Core
{
    public class ColorPaletteBase
    {
        public string Name { get; set; }

        public List<Color> Colors { get; set; } = new List<Color>();

        public bool IsGradient { get; set; } = true;

        [JsonIgnore]
        public bool IsBuiltIn { get; set; } = false;

        public ColorPaletteBase() { }

        public ColorPaletteBase(string name, List<Color> colors, bool isGradient, bool isBuiltIn = false)
        {
            Name = name;
            Colors = colors;
            IsGradient = isGradient;
            IsBuiltIn = isBuiltIn;
        }
    }
}