using rasterization_2.shapes;
using System.Buffers.Text;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace rasterization_2.serialization
{
    public static class Serialization
    {
        private static readonly JsonSerializerOptions json_writeOptions = new()
        {
            WriteIndented = true,
        };

        private static readonly JsonSerializerOptions json_readOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public static void SerializeToFile(string filePath, MainWindow mainWindow)
        {
            ProjectData data = new()
            {
                BitmapWidth = mainWindow.Bitmap.PixelWidth,
                BitmapHeight = mainWindow.Bitmap.PixelHeight,
                Lines = [],
                Circles = [],
                Polygons = [],
                Rectangles = []
            };

            static string ConvertColor(Color c)
            {
                return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
            }

            foreach (var line in mainWindow.Lines)
            {
                data.Lines.Add(new LineDto
                {
                    X1 = line.X1,
                    Y1 = line.Y1,
                    X2 = line.X2,
                    Y2 = line.Y2,
                    Thickness = line.Thickness,
                    Color = ConvertColor(line.Color)
                });
            }

            foreach (var circle in mainWindow.Circles)
            {
                data.Circles.Add(new CircleDto
                {
                    CenterX = circle.Center.X,
                    CenterY = circle.Center.Y,
                    Radius = circle.Radius,
                    Thickness = circle.Thickness,
                    Color = ConvertColor(circle.Color)
                });
            }

            foreach (var polygon in mainWindow.Polygons)
            {
                var polygonDto = new PolygonDto
                {
                    Thickness = polygon.Thickness,
                    Color = ConvertColor(polygon.Color),
                    Vertices = [],
                    FillColor = ConvertColor(polygon.FillColor),
                    IsFillColor = polygon.IsFillColor,
                    BitmapSource = EncodeBitmapSourceToBase64(polygon.BitmapSource)
                };
                foreach (var vertex in polygon.Vertices)
                {
                    polygonDto.Vertices.Add(new VertexDto
                    {
                        X = vertex.X,
                        Y = vertex.Y
                    });
                }
                data.Polygons.Add(polygonDto);
            }

            foreach (var rectangle in mainWindow.Rectangles)
            {
                data.Rectangles.Add(new RectangleDto
                {
                    Thickness = rectangle.Thickness,
                    Color = ConvertColor(rectangle.Color),
                    X1 = rectangle.Diagonal.X1,
                    Y1 = rectangle.Diagonal.Y1,
                    X2 = rectangle.Diagonal.X2,
                    Y2 = rectangle.Diagonal.Y2
                });
            }

            string json = JsonSerializer.Serialize(data, json_writeOptions);
            File.WriteAllText(filePath, json);
        }

        public static void LoadFromFile(string filePath, MainWindow mainWindow)
        {
            string json = File.ReadAllText(filePath);

            ProjectData data = JsonSerializer.Deserialize<ProjectData>(json, json_readOptions) ?? throw new InvalidOperationException("Failed to deserialize project data");

            mainWindow.Lines.Clear();
            mainWindow.Circles.Clear();
            mainWindow.Polygons.Clear();

            static Color ConvertColor(string s)
            {
                var cc = TypeDescriptor.GetConverter(typeof(Color));
                return (Color)cc.ConvertFromString(s)!;
            }

            foreach (var dto in data.Lines)
            {
                var line = new Line
                {
                    X1 = dto.X1,
                    Y1 = dto.Y1,
                    X2 = dto.X2,
                    Y2 = dto.Y2,
                    Thickness = dto.Thickness,
                    Color = ConvertColor(dto.Color)
                };
                mainWindow.Lines.Add(line);
            }

            foreach (var dto in data.Circles)
            {
                var circle = new Circle
                {
                    Center = new Point(dto.CenterX, dto.CenterY),
                    Radius = dto.Radius,
                    Thickness = dto.Thickness,
                    Color = ConvertColor(dto.Color)
                };
                mainWindow.Circles.Add(circle);
            }

            foreach (var dto in data.Polygons)
            {
                var polygon = new Polygon
                {
                    Thickness = dto.Thickness,
                    Color = ConvertColor(dto.Color),
                    Vertices = [],
                    FillColor = ConvertColor(dto.FillColor),
                    IsFillColor = dto.IsFillColor,
                    BitmapSource = DecodeBitmapSourceFromBase64(dto.BitmapSource)
                };
                foreach (var v in dto.Vertices)
                    polygon.Vertices.Add(new Point(v.X, v.Y));
                mainWindow.Polygons.Add(polygon);
            }

            foreach (var dto in data.Rectangles)
            {
                var rectangle = new Rectangle
                {
                    Thickness = dto.Thickness,
                    Color = ConvertColor(dto.Color),
                    Diagonal = new Line
                    {
                        X1 = dto.X1,
                        Y1 = dto.Y1,
                        X2 = dto.X2,
                        Y2 = dto.Y2
                    }
                };
                mainWindow.Rectangles.Add(rectangle);
            }

            mainWindow.Bitmap = new WriteableBitmap(data.BitmapWidth, data.BitmapHeight, 96, 96, PixelFormats.Bgra32, null);
            mainWindow.CanvasHost.Width = data.BitmapWidth;
            mainWindow.CanvasHost.Height = data.BitmapHeight;
            mainWindow.Canvas.Width = data.BitmapWidth;
            mainWindow.Canvas.Height = data.BitmapHeight;
            mainWindow.RedrawAll();
        }

        private static string? EncodeBitmapSourceToBase64(BitmapSource? bitmapSource)
        {
            if (bitmapSource == null)
                return null;

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using MemoryStream ms = new();
            encoder.Save(ms);
            return Convert.ToBase64String(ms.ToArray());
        }

        private static BitmapSource? DecodeBitmapSourceFromBase64(string? base64String)
        {
            if (base64String == null)
                return null;

            byte[] bytes = Convert.FromBase64String(base64String);
            using MemoryStream ms = new(bytes);
            BitmapImage bmp = new();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}
