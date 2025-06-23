using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;

namespace FractalExplorer.Core // Убедитесь, что пространство имен совпадает с вашим
{
    public class ColorPalette
    {
        public string Name { get; set; }

        public List<Color> Colors { get; set; } = new List<Color>();

        public bool IsGradient { get; set; } = true;

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