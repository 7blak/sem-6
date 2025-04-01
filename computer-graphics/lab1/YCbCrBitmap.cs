using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace lab1
{
    public class YCbCrBitmap
    {
        private readonly WriteableBitmap _originalBitmap;
        public double[] Y { get; set; }
        public double[] Cb { get; set; }
        public double[] Cr { get; set; }
        public double[] Alphas { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public YCbCrBitmap(WriteableBitmap bmp)
        {
            _originalBitmap = bmp;
            Width = bmp.PixelWidth;
            Height = bmp.PixelHeight;
            Y = new double[Width * Height];
            Cb = new double[Width * Height];
            Cr = new double[Width * Height];
            Alphas = new double[Width * Height];
            int bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;
            bmp.Lock();
            unsafe
            {
                byte* p = (byte*)bmp.BackBuffer;

                for (int j = 0; j < Height; j++)
                {

                    for (int i = 0; i < Width; i++)
                    {
                        int index = j * Width + i;
                        byte r = p[2];
                        byte g = p[1];
                        byte b = p[0];
                        Y[index] = 0.299 * r + 0.587 * g + 0.114 * b;
                        Cb[index] = 128 - 0.168736 * r - 0.331264 * g + 0.5 * b;
                        Cr[index] = 128 + 0.5 * r - 0.418688 * g - 0.081312 * b;
                        Alphas[index] = p[3];
                        p += bytesPerPixel;
                    }
                }
            }
        }

        public WriteableBitmap ConvertToRGBBitmap()
        {
            WriteableBitmap bmp = new WriteableBitmap(Width, Height, _originalBitmap.DpiX, _originalBitmap.DpiY, _originalBitmap.Format, null);
            int bytesPerPixel = (bmp.Format.BitsPerPixel + 7) / 8;
            bmp.Lock();
            unsafe
            {
                byte* p = (byte*)bmp.BackBuffer;

                for (int j = 0; j < Height; j++)
                {
                    for (int i = 0; i < Width; i++)
                    {
                        int index = j * Width + i;
                        byte r = (byte)(Y[index] + 1.402 * (Cr[index] - 128));
                        byte g = (byte)(Y[index] - 0.344136 * (Cb[index] - 128) - 0.714136 * (Cr[index] - 128));
                        byte b = (byte)(Y[index] + 1.772 * (Cb[index] - 128));
                        p[3] = (byte)Alphas[index];
                        p[2] = Math.Clamp(r, (byte)0, (byte)255);
                        p[1] = Math.Clamp(g, (byte)0, (byte)255);
                        p[0] = Math.Clamp(b, (byte)0, (byte)255);
                        p += bytesPerPixel;
                    }
                }
            }
            bmp.AddDirtyRect(new System.Windows.Int32Rect(0, 0, Width, Height));
            bmp.Unlock();
            return bmp;
        }
    }
}
