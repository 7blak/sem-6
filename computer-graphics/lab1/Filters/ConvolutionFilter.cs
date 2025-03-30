using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace lab1.Filters
{
    public enum EnumConvolutionFilterType { Default, Blur, GaussianBlur, Sharpen, EdgeDetection, Emboss };

    public class ConvolutionFilter
    {
        private double _divisor;
        public double Divisor { get { return _divisor; } set { _divisor = value == 0 ? 1 : value; CalculateDividedKernel(); } }
        public string Name { get; set; }
        public double[,] Kernel { get; set; }
        public double[,] DividedKernel { get; set; }
        public Point Anchor { get; set; }
        public double Offset { get; set; }

        public ConvolutionFilter(string name, double[,] kernel, double divisor, Point anchor, double offset = 0)
        {
            Name = name;
            Kernel = new double[kernel.GetLength(0), kernel.GetLength(1)];
            for (int i = 0; i < kernel.GetLength(0); i++)
                for (int j = 0; j < kernel.GetLength(1); j++)
                    Kernel[i, j] = kernel[i, j];

            DividedKernel = new double[Kernel.GetLength(0), Kernel.GetLength(1)];
            Divisor = divisor;
            Anchor = anchor;
            Offset = offset;
        }

        private void CalculateDividedKernel()
        {
            for (int i = 0; i < Kernel.GetLength(0); i++)
                for (int j = 0; j < Kernel.GetLength(1); j++)
                    DividedKernel[i, j] = Kernel[i, j] / Divisor;
        }

        public static ConvolutionFilter EnumToFilterConverter(EnumConvolutionFilterType filterType)
        {
            return filterType switch
            {
                EnumConvolutionFilterType.Default => Default(),
                EnumConvolutionFilterType.Blur => Blur(),
                EnumConvolutionFilterType.GaussianBlur => GaussianBlur(),
                EnumConvolutionFilterType.Sharpen => Sharpen(),
                EnumConvolutionFilterType.EdgeDetection => EdgeDetection(),
                EnumConvolutionFilterType.Emboss => Emboss(),
                _ => Default(),
            };
        }

        public static ConvolutionFilter Default()
        {
            return new ConvolutionFilter("Default", new double[3, 3] {
                { 0, 0, 0 },
                { 0, 1, 0 },
                { 0, 0, 0 }
            },
            1,
            new Point(0, 0)
            );
        }

        public static ConvolutionFilter Blur()
        {
            return new ConvolutionFilter("Blur", new double[3, 3] {
                { 1, 1, 1 },
                { 1, 1, 1 },
                { 1, 1, 1 }
            },
            9,
            new Point(0, 0)
            );
        }

        public static ConvolutionFilter GaussianBlur()
        {
            return new ConvolutionFilter("Gaussian Blur", new double[3, 3] {
                { 1, 2, 1 },
                { 2, 4, 2 },
                { 1, 2, 1 }
            },
            16,
            new Point(0, 0)
            );
        }

        public static ConvolutionFilter Sharpen()
        {
            return new ConvolutionFilter("Sharpen", new double[3, 3] {
                { 0, -1, 0 },
                { -1, 5, -1 },
                { 0, -1, 0 }
            },
            1,
            new Point(0, 0)
            );
        }

        public static ConvolutionFilter EdgeDetection()
        {
            return new ConvolutionFilter("Edge Detection", new double[3, 3] {
                { -1, -1, -1 },
                { -1, 8, -1 },
                { -1, -1, -1 }
            },
            1,
            new Point(0, 0)
            );
        }

        public static ConvolutionFilter Emboss()
        {
            return new ConvolutionFilter("Emboss", new double[3, 3] {
                { -2, -1, 0 },
                { -1, 1, 1 },
                { 0, 1, 2 }
            },
            1,
            new Point(0, 0)
            );
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
