using lab1.Filters;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace lab1.Windows
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private BitmapSource? originalImage;
        private WriteableBitmap? filteredImage;
        private int averageDitheringLevel = 2;
        public int AverageDitheringLevel
        {
            get => averageDitheringLevel;
            set
            {
                averageDitheringLevel = value;
                OnPropertyChanged(nameof(AverageDitheringLevel));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<ConvolutionFilter> ConvolutionFilters { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ConvolutionFilters = [];
            SetFiltersToDefault();
            DataContext = this;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private void ApplyGrayscale()
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

                for (int i = 0; i < width * height * bytesPerPixel; i += bytesPerPixel)
                {
                    byte b = buffer[i];
                    byte g = buffer[i + 1];
                    byte r = buffer[i + 2];

                    byte gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b);

                    buffer[i] = gray;
                    buffer[i + 1] = gray;
                    buffer[i + 2] = gray;
                }
            }

            filteredImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            filteredImage.Unlock();
            FilteredImage.Source = filteredImage;
        }

        private void ApplyAverageDithering()
        {
            if (filteredImage == null)
                return;

            averageDitheringLevel = averageDitheringLevel < 2 ? 2 : averageDitheringLevel;
            averageDitheringLevel = averageDitheringLevel > 255 ? 255 : averageDitheringLevel;

            int bins = AverageDitheringLevel - 1;
            double binWidth = 255.0 / bins;
            int stride = filteredImage.BackBufferStride;
            int height = filteredImage.PixelHeight;
            int width = filteredImage.PixelWidth;
            int bytesPerPixel = (filteredImage.Format.BitsPerPixel + 7) / 8;
            double[] sumR = new double[bins];
            double[] sumG = new double[bins];
            double[] sumB = new double[bins];
            int[] countR = new int[bins];
            int[] countG = new int[bins];
            int[] countB = new int[bins];

            filteredImage.Lock();
            unsafe
            {
                byte* buffer = (byte*)filteredImage.BackBuffer;

                for (int i = 0; i < height * width * bytesPerPixel; i += bytesPerPixel)
                {
                    byte b = buffer[i];
                    byte g = buffer[i + 1];
                    byte r = buffer[i + 2];

                    int binIndexR = Math.Min((int)(r / binWidth), bins - 1);
                    int binIndexG = Math.Min((int)(g / binWidth), bins - 1);
                    int binIndexB = Math.Min((int)(b / binWidth), bins - 1);

                    sumR[binIndexR] += r;
                    sumG[binIndexG] += g;
                    sumB[binIndexB] += b;

                    countR[binIndexR]++;
                    countG[binIndexG]++;
                    countB[binIndexB]++;
                }
            }

            byte[] avgR = new byte[bins];
            byte[] avgG = new byte[bins];
            byte[] avgB = new byte[bins];

            for (int i = 0; i < bins; i++)
            {
                double lowerBound = i * binWidth;
                double upperBound = (i + 1) * binWidth;
                double midPoint = (lowerBound + upperBound) / 2.0;

                avgR[i] = (byte)((countR[i] > 0) ? (sumR[i] / countR[i]) : midPoint);
                avgG[i] = (byte)((countG[i] > 0) ? (sumG[i] / countG[i]) : midPoint);
                avgB[i] = (byte)((countB[i] > 0) ? (sumB[i] / countB[i]) : midPoint);
            }

            unsafe
            {
                byte* buffer = (byte*)filteredImage.BackBuffer;

                for (int i = 0; i < height * width * bytesPerPixel; i += bytesPerPixel)
                {
                    byte b = buffer[i];
                    byte g = buffer[i + 1];
                    byte r = buffer[i + 2];

                    int binIndexR = Math.Min((int)(r / binWidth), bins - 1);
                    int binIndexG = Math.Min((int)(g / binWidth), bins - 1);
                    int binIndexB = Math.Min((int)(b / binWidth), bins - 1);

                    buffer[i] = (byte)(b <= avgB[binIndexB] ? binIndexB * binWidth : (binIndexB + 1) * binWidth);
                    buffer[i + 1] = (byte)(g <= avgG[binIndexG] ? binIndexG * binWidth : (binIndexG + 1) * binWidth);
                    buffer[i + 2] = (byte)(r <= avgR[binIndexR] ? binIndexR * binWidth : (binIndexR + 1) * binWidth);
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

        private void ApplyGrayscale_Click(object sender, RoutedEventArgs e)
        {
            ApplyGrayscale();
        }

        private void ApplyAverageDithering_Click(object sender, RoutedEventArgs e)
        {
            ApplyAverageDithering();
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !NumericInputRegex().IsMatch(e.Text);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (int.TryParse(textBox.Text, out int value))
                {
                    if (value < 2) value = 2;
                    if (value > 255) value = 255;
                }
                else
                {
                    value = 2;
                }
                textBox.Text = value.ToString();
            }
        }

        [GeneratedRegex("^[0-9]+$")]
        private static partial Regex NumericInputRegex();
    }
}