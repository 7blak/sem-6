﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace lab1
{
    public class ConvolutionFilter
    {
        public double[,] Kernel {  get; set; }
        public double[,] DividedKernel { get; set; }
        public double Divisor { get; set; }
        public Point Anchor {  get; set; }

        public ConvolutionFilter(double[,] kernel, double divisor, System.Windows.Point anchor)
        {
            Kernel = kernel;
            DividedKernel = kernel;
            for (int i = 0; i < kernel.GetLength(0); i++)
                for (int j = 0; j < kernel.GetLength(1); j++)
                    DividedKernel[i, j] /= divisor;
            Divisor = divisor;
            Anchor = anchor;
        }
    }
}
