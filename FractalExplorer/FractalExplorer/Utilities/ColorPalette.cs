using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;

namespace FractalExplorer.Core
{
    public class ColorPalette
    {
        public string Name { get; set; }
        public List<Color> Colors { get; set; } = new List<Color>();
        public bool IsGradient { get; set; } = true;

        // Это свойство не будет сохраняться в JSON, оно для внутренней логики UI
        [JsonIgnore]
        public bool IsBuiltIn { get; set; } = false;

        public ColorPalette() { }

        public ColorPalette(string name, List<Color> colors, bool isGradient, bool isBuiltIn = false)
        {
            Name = name;
            Colors = colors;
            IsGradient = isGradient;
            IsBuiltIn = isBuiltIn;
        }
    }
}