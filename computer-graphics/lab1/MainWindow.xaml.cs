using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace lab1
{
    public partial class MainWindow : Window
    {
        private BitmapSource? originalImage;
        private WriteableBitmap? filteredImage;
        public ObservableCollection<ConvolutionFilter> ConvolutionFilters { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();
            ConvolutionFilters = [];
            SetFiltersToDefault();
            DataContext = this;
        }

        private void SetFiltersToDefault()
        {
            ConvolutionFilters.Clear();
            foreach (var type in Enum.GetValues<EnumConvolutionFilterType>())
                ConvolutionFilters.Add(ConvolutionFilter.EnumToFilterConverter((EnumConvolutionFilterType)type));
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
            int halfKernelHeight = (filter.Kernel.GetLength(0)) / 2;
            int halfKernelWidth = (filter.Kernel.GetLength(1)) / 2;
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
                        for (int ky = -halfKernelHeight; ky <= halfKernelHeight; ky++)
                        {
                            for (int kx = -halfKernelWidth; kx <= halfKernelWidth; kx++)
                            {
                                int neighborX = Math.Max(0, Math.Min(width - 1, x + kx + (int)filter.Anchor.X));
                                int neighborY = Math.Max(0, Math.Min(height - 1, y + ky + (int)filter.Anchor.Y));
                                int kernelIndex = neighborY * stride + neighborX * bytesPerPixel;

                                r += pixels[kernelIndex + 2] * filter.DividedKernel[ky + halfKernelHeight, kx + halfKernelWidth];
                                g += pixels[kernelIndex + 1] * filter.DividedKernel[ky + halfKernelHeight, kx + halfKernelWidth];
                                b += pixels[kernelIndex] * filter.DividedKernel[ky + halfKernelHeight, kx + halfKernelWidth];
                            }
                        }
                        buffer[index] = (byte)Math.Clamp(b + filter.Offset, 0, 255);
                        buffer[index + 1] = (byte)Math.Clamp(g + filter.Offset, 0, 255);
                        buffer[index + 2] = (byte)Math.Clamp(r + filter.Offset, 0, 255);
                    }
                }
            }

            filteredImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            filteredImage.Unlock();
            FilteredImage.Source = filteredImage;
        }

        private void ApplyMorphology(bool isErosion)
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
                        int minOrMaxR = isErosion ? 255 : 0;
                        int minOrMaxG = isErosion ? 255 : 0;
                        int minOrMaxB = isErosion ? 255 : 0;

                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int neighborX = Math.Max(0, Math.Min(x + kx, width - 1));
                                int neighborY = Math.Max(0, Math.Min(y + ky, height - 1));
                                int neighborIndex = (neighborY * stride) + (neighborX * bytesPerPixel);

                                byte r = pixels[neighborIndex + 2];
                                byte g = pixels[neighborIndex + 1];
                                byte b = pixels[neighborIndex];

                                minOrMaxR = isErosion ? Math.Min(minOrMaxR, r) : Math.Max(minOrMaxR, r);
                                minOrMaxG = isErosion ? Math.Min(minOrMaxG, g) : Math.Max(minOrMaxG, g);
                                minOrMaxB = isErosion ? Math.Min(minOrMaxB, b) : Math.Max(minOrMaxB, b);

                            }
                        }

                        int index = (y * stride) + (x * bytesPerPixel);
                        buffer[index] = (byte)minOrMaxB;
                        buffer[index + 1] = (byte)minOrMaxG;
                        buffer[index + 2] = (byte)minOrMaxR;
                        buffer[index + 3] = 255;
                    }
                }
            }

            filteredImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            filteredImage.Unlock();
            FilteredImage.Source = filteredImage;
        }
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new() { Filter = "Image Files|*.jpg;*.png;*.bmp" };
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
                SaveFileDialog saveFileDialog = new() { Filter = "PNG Image|*.png" };
                if (saveFileDialog.ShowDialog() == true)
                {
                    using FileStream stream = new(saveFileDialog.FileName, FileMode.Create);
                    PngBitmapEncoder encoder = new();
                    encoder.Frames.Add(BitmapFrame.Create(filteredImage));
                    encoder.Save(stream);
                }
            }
        }

        private void ResetImage_Click(object sender, RoutedEventArgs e)
        {
            if (originalImage == null)
                return;
            filteredImage = new WriteableBitmap(originalImage);
            FilteredImage.Source = filteredImage;
        }

        private void CustomFilter_Click(object sender, RoutedEventArgs e)
        {
            CustomFilterWindow customFilterWindow = new(ConvolutionFilters);
            customFilterWindow.ShowDialog();
        }

        private void InvertColors(object sender, RoutedEventArgs e)
        {
            ApplyPixelFilter((r, g, b) => (
            255 - r,
            255 - g,
            255 - b
            ));
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

        private void CustomFilterApply_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is ConvolutionFilter selectedFilter)
                ApplyConvolutionFilter(selectedFilter);
            else
                MessageBox.Show("Something went wrong with applying the filter.", "Error");
        }

        private void BlurFilter_Click(object sender, RoutedEventArgs e)
        {
            ApplyConvolutionFilter(ConvolutionFilter.Blur());
        }

        private void GaussianBlurFilter_Click(object sender, RoutedEventArgs e)
        {
            ApplyConvolutionFilter(ConvolutionFilter.GaussianBlur());
        }

        private void SharpenFilter_Click(object sender, RoutedEventArgs e)
        {
            ApplyConvolutionFilter(ConvolutionFilter.Sharpen());
        }

        private void EdgeDetectionFilter_Click(object sender, RoutedEventArgs e)
        {
            ApplyConvolutionFilter(ConvolutionFilter.EdgeDetection());
        }

        private void EmbossFilter_Click(object sender, RoutedEventArgs e)
        {
            ApplyConvolutionFilter(ConvolutionFilter.Emboss());
        }

        private static int Clamp(int value)
        {
            return Math.Max(0, Math.Min(255, value));
        }

        private void ResetCustomFilters_Click(object sender, RoutedEventArgs e)
        {
            SetFiltersToDefault();
        }

        private void ApplyErosion_Click(object sender, RoutedEventArgs e)
        {
            ApplyMorphology(true);
        }

        private void ApplyDilation_Click(object sender, RoutedEventArgs e)
        {
            ApplyMorphology(false);
        }
    }
}