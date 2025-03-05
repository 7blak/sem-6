using Microsoft.Win32;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using Point = System.Windows.Point;

namespace lab1
{
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

        private void ApplyConvolutionFilter(ConvolutionFilter filter)
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
                                int neighborX = Math.Max(0, Math.Min(width - 1, x + kx + (int)filter.Anchor.X));
                                int neighborY = Math.Max(0, Math.Min(height - 1, y + ky + (int)filter.Anchor.Y));
                                int kernelIndex = neighborY * stride + neighborX * bytesPerPixel;
                                r += pixels[kernelIndex + 2] * filter.DividedKernel[ky + 1, kx + 1];
                                g += pixels[kernelIndex + 1] * filter.DividedKernel[ky + 1, kx + 1];
                                b += pixels[kernelIndex] * filter.DividedKernel[ky + 1, kx + 1];
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
                { 1, 1, 1 },
                { 1, 1, 1 },
                { 1, 1, 1 }
            };
            int divisor = 9;
            Point anchor = new Point(0, 0);

            ApplyConvolutionFilter(new ConvolutionFilter(blurKernel, divisor, anchor));
        }

        private void GaussianBlur(object sender, RoutedEventArgs e)
        {
            double[,] gaussianKernel =
            {
                { 1, 2, 1 },
                { 2, 4, 2 },
                { 1, 2, 1 }
            };
            int divisor = 16;
            Point anchor = new Point(0, 0);

            ApplyConvolutionFilter(new ConvolutionFilter(gaussianKernel, divisor, anchor));
        }

        private void Sharpen(object sender, RoutedEventArgs e)
        {
            double[,] sharpenKernel =
            {
                { 0, -1, 0 },
                { -1, 5, -1 },
                { 0, -1, 0 }
            };
            int divisor = 1;
            Point anchor = new Point(0, 0);

            ApplyConvolutionFilter(new ConvolutionFilter(sharpenKernel, divisor, anchor));
        }

        private void EdgeDetection(object sender, RoutedEventArgs e)
        {
            double[,] edgeKernel =
            {
                { -1, -1, -1 },
                { -1, 8, -1 },
                { -1, -1, -1 }
            };
            int divisor = 1;
            Point anchor = new Point(0, 0);

            ApplyConvolutionFilter(new ConvolutionFilter(edgeKernel, divisor, anchor));
        }

        private void Emboss(object sender, RoutedEventArgs e)
        {
            double[,] embossKernel =
            {
                { -2, -1, 0 },
                { -1, 1, 1 },
                { 0, 1, 2 }
            };
            int divisor = 1;
            Point anchor = new Point(0, 0);

            ApplyConvolutionFilter(new ConvolutionFilter(embossKernel, divisor, anchor));
        }

        private void ResetImage(object sender, RoutedEventArgs e)
        {
            filteredImage = new WriteableBitmap(originalImage);
            FilteredImage.Source = filteredImage;
        }

        private void CustomFilter_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}