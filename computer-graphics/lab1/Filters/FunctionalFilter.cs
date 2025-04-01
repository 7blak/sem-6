using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab1.Filters
{
    public enum EnumFunctionalFilterType { InvertColors, BrightnessCorrection, ContrastEnhancement, GammaCorrection };
    public class FunctionalFilter(string name, double? param, Func<int, int, int, (int, int, int)> filterFunction)
    {
        public string Name { get; set; } = name;
        public double? Param { get; set; } = param;
        public Func<int, int, int, (int, int, int)> FilterFunction { get; set; } = filterFunction;

        public static FunctionalFilter EnumToFilterConverter(EnumFunctionalFilterType filterType)
        {
            return filterType switch
            {
                EnumFunctionalFilterType.InvertColors => InvertColors(),
                EnumFunctionalFilterType.BrightnessCorrection => BrightnessCorrection(),
                EnumFunctionalFilterType.ContrastEnhancement => ContrastEnhancement(),
                EnumFunctionalFilterType.GammaCorrection => GammaCorrection(),
                _ => InvertColors(),
            };
        }

        public static FunctionalFilter InvertColors()
        {
            return new FunctionalFilter("Invert Colors", null,
                (r, g, b) => (
                255 - r,
                255 - g,
                255 - b
                ));
        }

        public static FunctionalFilter BrightnessCorrection()
        {
            return new FunctionalFilter("Brightness Correction", 60,
                (r, g, b) => (
                Math.Clamp(r + 60, 0, 255),
                Math.Clamp(g + 60, 0, 255),
                Math.Clamp(b + 60, 0, 255)
                ));
        }

        public static FunctionalFilter ContrastEnhancement()
        {
            return new FunctionalFilter("Contrast Enhancement", 1.44,
                (r, g, b) => (
                Math.Clamp((int)(((r / 255.0 - 0.5) * 1.44 + 0.5) * 255.0), 0, 255),
                Math.Clamp((int)(((g / 255.0 - 0.5) * 1.44 + 0.5) * 255.0), 0, 255),
                Math.Clamp((int)(((b / 255.0 - 0.5) * 1.44 + 0.5) * 255.0), 0, 255)
                ));
        }

        public static FunctionalFilter GammaCorrection()
        {
            return new FunctionalFilter("Gamma Correction", 0.5,
                (r, g, b) => (
                Math.Clamp((int)(Math.Pow(r / 255.0, 0.5) * 255.0), 0, 255),
                Math.Clamp((int)(Math.Pow(g / 255.0, 0.5) * 255.0), 0, 255),
                Math.Clamp((int)(Math.Pow(b / 255.0, 0.5) * 255.0), 0, 255)
                ));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
