using lab1.Filters;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace lab1.Windows
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private BitmapSource? _originalImage;
        private WriteableBitmap? _filteredImage;
        private bool _ditheringUsesYCbCr = false;
        public bool DitheringUsesYCbCr
        {
            get => _ditheringUsesYCbCr;
            set
            {
                _ditheringUsesYCbCr = value;
                OnPropertyChanged(nameof(DitheringUsesYCbCr));
            }
        }
        private int _randomSeed = 999;
        public int RandomSeed
        {
            get => _randomSeed;
            set
            {
                _randomSeed = value;
                OnPropertyChanged(nameof(RandomSeed));
            }
        }
        private int _kMeans = 4;
        public int KMeans
        {
            get => _kMeans;
            set
            {
                _kMeans = value;
                OnPropertyChanged(nameof(KMeans));
            }
        }
        private int _kMeansMaxIterations = 1000;
        public int KMeansMaxIterations
        {
            get => _kMeansMaxIterations;
            set
            {
                _kMeansMaxIterations = value;
                OnPropertyChanged(nameof(KMeansMaxIterations));
            }
        }
        private int _averageDitheringLevel = 2;
        public int AverageDitheringLevel
        {
            get => _averageDitheringLevel;
            set
            {
                _averageDitheringLevel = value;
                OnPropertyChanged(nameof(AverageDitheringLevel));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<ConvolutionFilter> ConvolutionFilters { get; set; }
        public ObservableCollection<FunctionalFilter> FunctionalFilters { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ConvolutionFilters = [];
            FunctionalFilters = [];
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
            FunctionalFilters.Clear();
            foreach (var type in Enum.GetValues<EnumFunctionalFilterType>())
                FunctionalFilters.Add(FunctionalFilter.EnumToFilterConverter((EnumFunctionalFilterType)type));
        }

        private void ApplyFunctionalFilter(FunctionalFilter filter)
        {
            if (_filteredImage == null)
                return;
            int stride = _filteredImage.BackBufferStride;
            int height = _filteredImage.PixelHeight;
            int width = _filteredImage.PixelWidth;
            int bytesPerPixel = (_filteredImage.Format.BitsPerPixel + 7) / 8;
            _filteredImage.Lock();
            unsafe
            {
                byte* buffer = (byte*)_filteredImage.BackBuffer;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * stride + x * bytesPerPixel;
                        var (r, g, b) = filter.FilterFunction(buffer[index + 2], buffer[index + 1], buffer[index]);
                        buffer[index] = (byte)b;
                        buffer[index + 1] = (byte)g;
                        buffer[index + 2] = (byte)r;
                    }
                }
            }

            _filteredImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _filteredImage.Unlock();
            FilteredImage.Source = _filteredImage;
        }

        private void ApplyConvolutionFilter(ConvolutionFilter filter)
        {
            if (_filteredImage == null)
                return;
            int stride = _filteredImage.BackBufferStride;
            int height = _filteredImage.PixelHeight;
            int width = _filteredImage.PixelWidth;
            int bytesPerPixel = (_filteredImage.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            int halfKernelHeight = (filter.Kernel.GetLength(0)) / 2;
            int halfKernelWidth = (filter.Kernel.GetLength(1)) / 2;
            _filteredImage.CopyPixels(pixels, stride, 0);
            _filteredImage.Lock();
            unsafe
            {
                byte* buffer = (byte*)_filteredImage.BackBuffer;
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

            _filteredImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _filteredImage.Unlock();
            FilteredImage.Source = _filteredImage;
        }

        private void ApplyMorphology(bool isErosion)
        {
            if (_filteredImage == null)
                return;
            int stride = _filteredImage.BackBufferStride;
            int height = _filteredImage.PixelHeight;
            int width = _filteredImage.PixelWidth;
            int bytesPerPixel = (_filteredImage.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            _filteredImage.CopyPixels(pixels, stride, 0);
            _filteredImage.Lock();
            unsafe
            {
                byte* buffer = (byte*)_filteredImage.BackBuffer;

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

            _filteredImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _filteredImage.Unlock();
            FilteredImage.Source = _filteredImage;
        }

        private void ApplyGrayscale()
        {
            if (_filteredImage == null)
                return;
            int stride = _filteredImage.BackBufferStride;
            int height = _filteredImage.PixelHeight;
            int width = _filteredImage.PixelWidth;
            int bytesPerPixel = (_filteredImage.Format.BitsPerPixel + 7) / 8;
            _filteredImage.Lock();
            unsafe
            {
                byte* buffer = (byte*)_filteredImage.BackBuffer;

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

            _filteredImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _filteredImage.Unlock();
            FilteredImage.Source = _filteredImage;
        }
        
        private void ApplyAverageDithering2()
        {
            if (_filteredImage == null)
                return;

            _averageDitheringLevel = _averageDitheringLevel < 2 ? 2 : _averageDitheringLevel;
            _averageDitheringLevel = _averageDitheringLevel > 255 ? 255 : _averageDitheringLevel;

            YCbCrBitmap yCbCrBitmap = new(_filteredImage);

            int bins = AverageDitheringLevel - 1;
            double binWidth = 255.0 / bins;
            int height = yCbCrBitmap.Height;
            int width = yCbCrBitmap.Width;

            double[] sumY = new double[bins];
            double[] sumCb = new double[bins];
            double[] sumCr = new double[bins];
            int[] countY = new int[bins];
            int[] countCb = new int[bins];
            int[] countCr = new int[bins];

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    int index = j * width + i;
                    int binIndexY = Math.Min((int)(yCbCrBitmap.Y[index] / binWidth), bins - 1);
                    int binIndexCb = Math.Min((int)(yCbCrBitmap.Cb[index] / binWidth), bins - 1);
                    int binIndexCr = Math.Min((int)(yCbCrBitmap.Cr[index] / binWidth), bins - 1);

                    sumY[binIndexY] += yCbCrBitmap.Y[index];
                    sumCb[binIndexCb] += yCbCrBitmap.Cb[index];
                    sumCr[binIndexCr] += yCbCrBitmap.Cr[index];

                    countY[binIndexY]++;
                    countCb[binIndexCb]++;
                    countCr[binIndexCr]++;
                }
            }

            double[] avgY = new double[bins];
            double[] avgCb = new double[bins];
            double[] avgCr = new double[bins];

            for (int i = 0; i < bins; i++)
            {
                double lowerBound = i * binWidth;
                double upperBound = (i + 1) * binWidth;
                double midPoint = (lowerBound + upperBound) / 2.0;

                avgY[i] = (countY[i] > 0) ? (sumY[i] / countY[i]) : midPoint;
                avgCb[i] = (countCb[i] > 0) ? (sumCb[i] / countCb[i]) : midPoint;
                avgCr[i] = (countCr[i] > 0) ? (sumCr[i] / countCr[i]) : midPoint;
            }

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    int index = j * width + i;
                    int binIndexY = Math.Min((int)(yCbCrBitmap.Y[index] / binWidth), bins - 1);
                    int binIndexCb = Math.Min((int)(yCbCrBitmap.Cb[index] / binWidth), bins - 1);
                    int binIndexCr = Math.Min((int)(yCbCrBitmap.Cr[index] / binWidth), bins - 1);

                    yCbCrBitmap.Y[index] = (byte)(yCbCrBitmap.Y[index] <= avgY[binIndexY] ? binIndexY * binWidth : (binIndexY + 1) * binWidth);
                    yCbCrBitmap.Cb[index] = (byte)(yCbCrBitmap.Cb[index] <= avgCb[binIndexCb] ? binIndexCb * binWidth : (binIndexCb + 1) * binWidth);
                    yCbCrBitmap.Cr[index] = (byte)(yCbCrBitmap.Cr[index] <= avgCr[binIndexCr] ? binIndexCr * binWidth : (binIndexCr + 1) * binWidth);
                }
            }

            _filteredImage = yCbCrBitmap.ConvertToRGBBitmap();
            FilteredImage.Source = _filteredImage;
        }
        private void ApplyAverageDithering()
        {
            if (_filteredImage == null)
                return;

            _averageDitheringLevel = _averageDitheringLevel < 2 ? 2 : _averageDitheringLevel;
            _averageDitheringLevel = _averageDitheringLevel > 255 ? 255 : _averageDitheringLevel;

            int bins = AverageDitheringLevel - 1;
            double binWidth = 255.0 / bins;
            int stride = _filteredImage.BackBufferStride;
            int height = _filteredImage.PixelHeight;
            int width = _filteredImage.PixelWidth;
            int bytesPerPixel = (_filteredImage.Format.BitsPerPixel + 7) / 8;
            double[] sumR = new double[bins];
            double[] sumG = new double[bins];
            double[] sumB = new double[bins];
            int[] countR = new int[bins];
            int[] countG = new int[bins];
            int[] countB = new int[bins];

            _filteredImage.Lock();
            unsafe
            {
                byte* buffer = (byte*)_filteredImage.BackBuffer;

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

            double[] avgR = new double[bins];
            double[] avgG = new double[bins];
            double[] avgB = new double[bins];

            for (int i = 0; i < bins; i++)
            {
                double lowerBound = i * binWidth;
                double upperBound = (i + 1) * binWidth;
                double midPoint = (lowerBound + upperBound) / 2.0;

                avgR[i] = (countR[i] > 0) ? (sumR[i] / countR[i]) : midPoint;
                avgG[i] = (countG[i] > 0) ? (sumG[i] / countG[i]) : midPoint;
                avgB[i] = (countB[i] > 0) ? (sumB[i] / countB[i]) : midPoint;
            }

            unsafe
            {
                byte* buffer = (byte*)_filteredImage.BackBuffer;

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

            _filteredImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _filteredImage.Unlock();
            FilteredImage.Source = _filteredImage;
        }

        private void ApplyKMeansColorQuantization()
        {
            if (_filteredImage == null)
                return;

            _kMeans = _kMeans < 2 ? 2 : _kMeans;

            int stride = _filteredImage.BackBufferStride;
            int height = _filteredImage.PixelHeight;
            int width = _filteredImage.PixelWidth;
            int bytesPerPixel = (_filteredImage.Format.BitsPerPixel + 7) / 8;
            int imageLength = width * height * bytesPerPixel;
            int pixelCount = width * height;

            int[] assignments = new int[pixelCount];
            for (int i = 0; i < pixelCount; i++)
                assignments[i] = -1;

            float[] centroids = new float[_kMeans * 3];
            double[] sumR = new double[_kMeans];
            double[] sumG = new double[_kMeans];
            double[] sumB = new double[_kMeans];
            int[] count = new int[_kMeans];

            bool changed = true;
            int iteration = 0;

            Random rand = new(_randomSeed);

            _filteredImage.Lock();
            unsafe
            {
                byte* buffer = (byte*)_filteredImage.BackBuffer;

                for (int cluster = 0; cluster < _kMeans; cluster++)
                {
                    int index = rand.Next(pixelCount);
                    byte* p = buffer + index * bytesPerPixel;
                    centroids[cluster * 3] = p[2];
                    centroids[cluster * 3 + 1] = p[1];
                    centroids[cluster * 3 + 2] = p[0];
                }

                while (changed && iteration < _kMeansMaxIterations)
                {
                    changed = false;
                    iteration++;

                    for (int i = 0; i < pixelCount; i++)
                    {
                        byte* p = buffer + i * bytesPerPixel;
                        float b = p[0];
                        float g = p[1];
                        float r = p[2];
                        int bestCluster = 0;
                        double minDistance = double.MaxValue;
                        for (int cluster = 0; cluster < _kMeans; cluster++)
                        {
                            float dr = r - centroids[cluster * 3];
                            float dg = g - centroids[cluster * 3 + 1];
                            float db = b - centroids[cluster * 3 + 2];
                            double dist = dr * dr + dg * dg + db * db;
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                bestCluster = cluster;
                            }
                        }
                        if (assignments[i] != bestCluster)
                        {
                            changed = true;
                            assignments[i] = bestCluster;
                        }
                        sumR[bestCluster] += r;
                        sumG[bestCluster] += g;
                        sumB[bestCluster] += b;
                        count[bestCluster]++;
                    }

                    for (int cluster = 0; cluster < _kMeans; cluster++)
                    {
                        if (count[cluster] > 0)
                        {
                            centroids[cluster * 3] = (float)(sumR[cluster] / count[cluster]);
                            centroids[cluster * 3 + 1] = (float)(sumG[cluster] / count[cluster]);
                            centroids[cluster * 3 + 2] = (float)(sumB[cluster] / count[cluster]);
                        }
                        else
                        {
                            int index = rand.Next(pixelCount);
                            byte* p = buffer + index * bytesPerPixel;
                            centroids[cluster * 3] = p[2];
                            centroids[cluster * 3 + 1] = p[1];
                            centroids[cluster * 3 + 2] = p[0];
                        }
                        sumR[cluster] = 0;
                        sumG[cluster] = 0;
                        sumB[cluster] = 0;
                        count[cluster] = 0;
                    }
                }

                for (int i = 0; i < pixelCount; i++)
                {
                    byte* p = buffer + i * bytesPerPixel;
                    int cluster = assignments[i];
                    p[0] = (byte)Math.Clamp((int)centroids[cluster * 3 + 2], 0, 255);
                    p[1] = (byte)Math.Clamp((int)centroids[cluster * 3 + 1], 0, 255);
                    p[2] = (byte)Math.Clamp((int)centroids[cluster * 3], 0, 255);
                }
            }

            _filteredImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _filteredImage.Unlock();
            FilteredImage.Source = _filteredImage;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new() { Filter = "Image Files|*.jpg;*.png;*.bmp" };
            if (openFileDialog.ShowDialog() == true)
            {
                _originalImage = new BitmapImage(new Uri(openFileDialog.FileName));
                _filteredImage = new WriteableBitmap(_originalImage);
                OriginalImage.Source = _originalImage;
                FilteredImage.Source = _filteredImage;
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredImage != null)
            {
                SaveFileDialog saveFileDialog = new() { Filter = "PNG Image|*.png" };
                if (saveFileDialog.ShowDialog() == true)
                {
                    using FileStream stream = new(saveFileDialog.FileName, FileMode.Create);
                    PngBitmapEncoder encoder = new();
                    encoder.Frames.Add(BitmapFrame.Create(_filteredImage));
                    encoder.Save(stream);
                }
            }
        }

        private void ResetImage_Click(object sender, RoutedEventArgs e)
        {
            if (_originalImage == null)
                return;
            _filteredImage = new WriteableBitmap(_originalImage);
            FilteredImage.Source = _filteredImage;
        }

        private void CustomFilter_Click(object sender, RoutedEventArgs e)
        {
            CustomFilterWindow customFilterWindow = new(ConvolutionFilters);
            customFilterWindow.ShowDialog();
        }

        private void FunctionalFilterApply_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is FunctionalFilter selectedFilter)
                ApplyFunctionalFilter(selectedFilter);
            else
                MessageBox.Show("Something went wrong with applying the filter.", "Error");
        }

        private void ConvolutionFilterApply_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is ConvolutionFilter selectedFilter)
                ApplyConvolutionFilter(selectedFilter);
            else
                MessageBox.Show("Something went wrong with applying the filter.", "Error");
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
            if (!_ditheringUsesYCbCr)
                ApplyAverageDithering();
            else
                ApplyAverageDithering2();
        }

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !NumericInputRegex().IsMatch(e.Text);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string tagString)
            {
                string[] limits = tagString.Split(',');
                if (limits.Length == 2 && int.TryParse(limits[0], out int min) && int.TryParse(limits[1], out int max))
                {
                    if (int.TryParse(textBox.Text, out int value))
                    {
                        value = Math.Max(min, Math.Min(max, value));
                    }
                    else
                    {
                        value = min;
                    }
                    textBox.Text = value.ToString();
                }
            }
        }

        [GeneratedRegex("^[0-9]+$")]
        private static partial Regex NumericInputRegex();

        private void ApplyKMeansQuantization_Click(object sender, RoutedEventArgs e)
        {
            ApplyKMeansColorQuantization();
        }

        private void ToggleStretch_Click(object sender, RoutedEventArgs e)
        {
            bool isStretchNone = OriginalImage.Stretch == Stretch.None;

            OriginalImage.Stretch = isStretchNone ? Stretch.Uniform : Stretch.None;
            FilteredImage.Stretch = isStretchNone ? Stretch.Uniform : Stretch.None;

            OriginalScrollViewer.HorizontalScrollBarVisibility = isStretchNone ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
            OriginalScrollViewer.VerticalScrollBarVisibility = isStretchNone ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;

            FilteredScrollViewer.HorizontalScrollBarVisibility = isStretchNone ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
            FilteredScrollViewer.VerticalScrollBarVisibility = isStretchNone ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        }

        private void ToggleYCbCrDithering_Click(object sender, RoutedEventArgs e)
        {
            _ditheringUsesYCbCr = !DitheringUsesYCbCr;

            if (sender is MenuItem menuItem)
                menuItem.Header = "[YCbCr] Toggle Dithering Mode";
        }
    }
}