using rasterization_2.shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace rasterization_2
{
    public static class Util
    {
        public static double Ipart(double x)
        {
            return Math.Floor(x);
        }

        public static double Round(double x)
        {
            return Ipart(x + 0.5);
        }

        public static double Fpart(double x)
        {
            return x - Ipart(x);
        }

        public static double Rfpart(double x)
        {
            return 1 - Fpart(x);
        }

        public static Color BlendColors(Color color1, Color color2, double ratio)
        {
            byte r = (byte)(color1.R * ratio + color2.R * (1 - ratio));
            byte g = (byte)(color1.G * ratio + color2.G * (1 - ratio));
            byte b = (byte)(color1.B * ratio + color2.B * (1 - ratio));
            byte a = (byte)(color1.A * ratio + color2.A * (1 - ratio));

            return Color.FromArgb(a, r, g, b);
        }

        public static double DistancePointToLine(Point p, Point a, Point b)
        {
            Vector ap = p - a, ab = b - a;
            double t = Vector.Multiply(ap, ab) / ab.LengthSquared;
            t = Math.Max(0, Math.Min(1, t));
            Point projection = a + t * ab;
            return (p - projection).Length;
        }

        public static double DistancePointToCircle(Point p, Circle circle)
        {
            return Math.Abs((p - circle.Center).Length - circle.Radius);
        }

        public static Polygon ConvertRectangleToPolygon(Rectangle rectangle)
        {
            var x1 = rectangle.Diagonal.X1;
            var y1 = rectangle.Diagonal.Y1;
            var x2 = rectangle.Diagonal.X2;
            var y2 = rectangle.Diagonal.Y2;

            double left = Math.Min(x1, x2);
            double right = Math.Max(x1, x2);
            double top = Math.Min(y1, y2);
            double bottom = Math.Max(y1, y2);

            return new Polygon
            {
                Thickness = rectangle.Thickness,
                Color = rectangle.Color,
                Vertices = new List<Point>
            {
                new(left, top),     // Top-left
                new(right, top),    // Top-right
                new(right, bottom), // Bottom-right
                new(left, bottom)   // Bottom-left
            }
            };
        }
    }
}
