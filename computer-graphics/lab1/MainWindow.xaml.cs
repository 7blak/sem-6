using Microsoft.Win32;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using System;

namespace lab1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapSource? originalImage;
        private WriteableBitmap? filteredImage;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Image Files|*.jpg;*.png;*.bmp" };
            if (openFileDialog.ShowDialog() == true)
            {
                originalImage = new BitmapImage(new Uri(openFileDialog.FileName));
                filteredImage = new WriteableBitmap(originalImage);
                OriginalImage.Source = originalImage;
                FilteredImage.Source = filteredImage;
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (filteredImage != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "PNG Image|*.png" };
                if (saveFileDialog.ShowDialog() == true)
                {
                    using (FileStream stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(filteredImage));
                        encoder.Save(stream);
                    }
                }
            }
        }

        private void ApplyPixelFilter(Func<int, int, int, (int, int, int)> filter)
        {
            if (filteredImage == null)
                return;
            int stride = filteredImage.BackBufferStride;
            int height = filteredImage.PixelHeight;
            int width = filteredImage.PixelWidth;
            int bytesPerPixel = (filteredImage.Format.BitsPerPixel + 7) / 8;
            filteredImage.Lock();
            unsafe
            {
                byte* buffer = (byte*)filteredImage.BackBuffer;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * stride + x * bytesPerPixel;
                        var (r, g, b) = filter(buffer[index + 2], buffer[index + 1], buffer[index]);
                        buffer[index] = (byte)b;
                        buffer[index + 1] = (byte)g;
                        buffer[index + 2] = (byte)r;
                    }
                }
            }

            filteredImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            filteredImage.Unlock();
            FilteredImage.Source = filteredImage;
        }

        private void ApplyConvolutionFilter(double[,] kernel, System.Windows.Point anchor, int divisor = 1, double factor = 1, double bias = 0)
        {
            if (filteredImage == null)
                return;
            int stride = filteredImage.BackBufferStride;
            int height = filteredImage.PixelHeight;
            int width = filteredImage.PixelWidth;
            int bytesPerPixel = (filteredImage.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            filteredImage.CopyPixels(pixels, stride, 0);
            filteredImage.Lock();
            unsafe
            {
                byte* buffer = (byte*)filteredImage.BackBuffer;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * stride + x * bytesPerPixel;
                        double r = 0, g = 0, b = 0;
                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int neighborX = Math.Max(0, Math.Min(width - 1, x + kx + (int)anchor.X));
                                int neighborY = Math.Max(0, Math.Min(height - 1, y + ky + (int)anchor.Y));
                                int kernelIndex = neighborY * stride + neighborX * bytesPerPixel;
                                r += pixels[kernelIndex + 2] * kernel[ky + 1, kx + 1];
                                g += pixels[kernelIndex + 1] * kernel[ky + 1, kx + 1];
                                b += pixels[kernelIndex] * kernel[ky + 1, kx + 1];
                            }
                        }
                        buffer[index] = (byte)Math.Clamp(b, 0, 255);
                        buffer[index + 1] = (byte)Math.Clamp(g, 0, 255);
                        buffer[index + 2] = (byte)Math.Clamp(r, 0, 255);
                    }
                }
            }

            filteredImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            filteredImage.Unlock();
            FilteredImage.Source = filteredImage;
        }

        private int Clamp(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }

        private void InvertColors(object sender, RoutedEventArgs e)
        {
            ApplyPixelFilter((r, g, b) => (255 - r, 255 - g, 255 - b));
        }

        private void BrightnessCorrection(object sender, RoutedEventArgs e)
        {
            int adjustment = 60;
            ApplyPixelFilter((r, g, b) => (
            Clamp(r + adjustment),
            Clamp(g + adjustment),
            Clamp(b + adjustment)
            ));
        }

        private void ContrastEnhancement(object sender, RoutedEventArgs e)
        {
            double contrast = 1.44;
            ApplyPixelFilter((r, g, b) => (
            Clamp((int)(((r / 255.0 - 0.5) * contrast + 0.5) * 255.0)),
            Clamp((int)(((g / 255.0 - 0.5) * contrast + 0.5) * 255.0)),
            Clamp((int)(((b / 255.0 - 0.5) * contrast + 0.5) * 255.0))
            ));
        }

        private void GammaCorrection(object sender, RoutedEventArgs e)
        {
            double gamma = 0.5;
            ApplyPixelFilter((r, g, b) => (
            Clamp((int)(Math.Pow(r / 255.0, gamma) * 255.0)),
            Clamp((int)(Math.Pow(g / 255.0, gamma) * 255.0)),
            Clamp((int)(Math.Pow(b / 255.0, gamma) * 255.0))
            ));
        }

        private void Blur(object sender, RoutedEventArgs e)
        {
            double[,] blurKernel =
            {
                { 1/9.0, 1/9.0, 1/9.0 },
                { 1/9.0, 1/9.0, 1/9.0 },
                { 1/9.0, 1/9.0, 1/9.0 }
            };
            ApplyConvolutionFilter(blurKernel, new System.Windows.Point(0, 0));
        }

        private void GaussianBlur(object sender, RoutedEventArgs e)
        {
            double[,] gaussianKernel =
            {
                { 1/16.0, 2/16.0, 1/16.0 },
                { 2/16.0, 4/16.0, 2/16.0 },
                { 1/16.0, 2/16.0, 1/16.0 }
            };
            ApplyConvolutionFilter(gaussianKernel, new System.Windows.Point(0, 0));
        }

        private void Sharpen(object sender, RoutedEventArgs e)
        {
            double[,] sharpenKernel =
            {
                { 0, -1, 0 },
                { -1, 5, -1 },
                { 0, -1, 0 }
            };
            ApplyConvolutionFilter(sharpenKernel, new System.Windows.Point(0, 0));
        }

        private void EdgeDetection(object sender, RoutedEventArgs e)
        {
            double[,] edgeKernel =
            {
                { -1, -1, -1 },
                { -1, 8, -1 },
                { -1, -1, -1 }
            };
            ApplyConvolutionFilter(edgeKernel, new System.Windows.Point(0, 0));
        }

        private void Emboss(object sender, RoutedEventArgs e)
        {
            double[,] embossKernel =
            {
                { -2, -1, 0 },
                { -1, 1, 1 },
                { 0, 1, 2 }
            };
            ApplyConvolutionFilter(embossKernel, new System.Windows.Point(0, 0));
        }

        private void ResetImage(object sender, RoutedEventArgs e)
        {
            filteredImage = new WriteableBitmap(originalImage);
            FilteredImage.Source = filteredImage;
        }
    }
}