using System.Windows;

namespace lab1
{
    public class ConvolutionFilter
    {
        public double[,] Kernel { get; set; }
        public double[,] DividedKernel { get; set; }
        public double Divisor { get; set; }
        public Point Anchor { get; set; }
        public double Offset { get; set; }
        public EnumConvolutionFilterType FilterType { get; set; }

        public ConvolutionFilter(double[,] kernel, double divisor, System.Windows.Point anchor, EnumConvolutionFilterType filterType, double offset = 0)
        {
            Kernel = new double[kernel.GetLength(0), kernel.GetLength(1)];
            for (int i = 0; i < kernel.GetLength(0); i++)
                for (int j = 0; j < kernel.GetLength(1); j++)
                    Kernel[i, j] = kernel[i, j];

            DividedKernel = new double[kernel.GetLength(0), kernel.GetLength(1)];
            divisor = (divisor == 0) ? 1 : divisor;
            for (int i = 0; i < kernel.GetLength(0); i++)
                for (int j = 0; j < kernel.GetLength(1); j++)
                    DividedKernel[i, j] = kernel[i, j] / divisor;

            Divisor = divisor;
            Anchor = anchor;
            FilterType = filterType;
            Offset = offset;
        }

        public static ConvolutionFilter EnumToFilterConverter(EnumConvolutionFilterType filterType)
        {
            switch (filterType)
            {
                case EnumConvolutionFilterType.Custom:
                    return Custom();
                case EnumConvolutionFilterType.Blur:
                    return Blur();
                case EnumConvolutionFilterType.GaussianBlur:
                    return GaussianBlur();
                case EnumConvolutionFilterType.Sharpen:
                    return Sharpen();
                case EnumConvolutionFilterType.EdgeDetection:
                    return EdgeDetection();
                case EnumConvolutionFilterType.Emboss:
                    return Emboss();
                default:
                    return Custom();
            }
        }

        public static ConvolutionFilter Custom()
        {
            return new ConvolutionFilter(new double[3, 3] {
                { 0, 0, 0 },
                { 0, 1, 0 },
                { 0, 0, 0 }
            },
            1,
            new Point(0, 0),
            EnumConvolutionFilterType.Custom
            );
        }

        public static ConvolutionFilter Blur()
        {
            return new ConvolutionFilter(new double[3, 3] {
                { 1, 1, 1 },
                { 1, 1, 1 },
                { 1, 1, 1 }
            },
            9,
            new Point(0, 0),
            EnumConvolutionFilterType.Blur
            );
        }

        public static ConvolutionFilter GaussianBlur()
        {
            return new ConvolutionFilter(new double[3, 3] {
                { 1, 2, 1 },
                { 2, 4, 2 },
                { 1, 2, 1 }
            },
            16,
            new Point(0, 0),
            EnumConvolutionFilterType.GaussianBlur
            );
        }

        public static ConvolutionFilter Sharpen()
        {
            return new ConvolutionFilter(new double[3, 3] {
                { 0, -1, 0 },
                { -1, 5, -1 },
                { 0, -1, 0 }
            },
            1,
            new Point(0, 0),
            EnumConvolutionFilterType.Sharpen
            );
        }

        public static ConvolutionFilter EdgeDetection()
        {
            return new ConvolutionFilter(new double[3, 3] {
                { -1, -1, -1 },
                { -1, 8, -1 },
                { -1, -1, -1 }
            },
            1,
            new Point(0, 0),
            EnumConvolutionFilterType.EdgeDetection
            );
        }

        public static ConvolutionFilter Emboss()
        {
            return new ConvolutionFilter(new double[3, 3] {
                { -2, -1, 0 },
                { -1, 1, 1 },
                { 0, 1, 2 }
            },
            1,
            new Point(0, 0),
            EnumConvolutionFilterType.Emboss
            );
        }
    }
}
