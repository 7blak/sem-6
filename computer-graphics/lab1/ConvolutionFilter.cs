using System.Windows;

namespace lab1
{
    public class ConvolutionFilter
    {
        public double[,] Kernel {  get; set; }
        public double[,] DividedKernel { get; set; }
        public double Divisor { get; set; }
        public Point Anchor {  get; set; }
        public double Offset { get; set; }

        public ConvolutionFilter(double[,] kernel, double divisor, System.Windows.Point anchor, double offset = 0)
        {
            Kernel = kernel;
            DividedKernel = kernel;
            divisor = (divisor == 0) ? 1 : divisor;
            for (int i = 0; i < kernel.GetLength(0); i++)
                for (int j = 0; j < kernel.GetLength(1); j++)
                    DividedKernel[i, j] /= divisor;
            Divisor = divisor;
            Anchor = anchor;
            Offset = offset;
        }
    }
}
