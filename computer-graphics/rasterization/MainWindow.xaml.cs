using Microsoft.VisualBasic;
using System.Media;
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

namespace rasterization;

public class LineSegment
{
    public Point P1 { get; set; }
    public Point P2 { get; set; }
    public int Thickness { get; set; }
    public Color Color { get; set; } = Colors.Black;
}

public partial class MainWindow : Window
{
    private WriteableBitmap _baseBitmap;
    private List<LineSegment> _lines = new List<LineSegment>();
    private bool _isDrawing = false;
    private Point _startPoint;
    private LineSegment? _activeLine = null;
    private Line? _previewLine;
    private bool _movingStart = false, _movingEnd = false;
    private const int HIT_TOLERANCE = 6;

#pragma warning disable CS8618
    public MainWindow()
#pragma warning restore CS8618
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        int w = (int)canvasHost.Width;
        int h = (int)canvasHost.Height;
        _baseBitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        ClearBitmap(_baseBitmap, Colors.White);
        canvas.Source = _baseBitmap;
    }
    #region Mouse Interaction

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(canvas);

        foreach (var line in _lines)
        {
            if ((line.P1 - p).Length <= HIT_TOLERANCE) { SelectLine(line, true); return; }
            if ((line.P2 - p).Length <= HIT_TOLERANCE) { SelectLine(line, false); return; }
        }

        _isDrawing = true;
        _startPoint = p;
        _activeLine = null;

        _previewLine = new Line
        {
            X1 = p.X,
            Y1 = p.Y,
            X2 = p.X,
            Y2 = p.Y,
            Stroke = new SolidColorBrush(Colors.Black),
            StrokeThickness = 1,
        };
        canvasHost.Children.Add(_previewLine);
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        Point p = e.GetPosition(canvas);

        if (_isDrawing && _previewLine != null)
        {
            _previewLine.X2 = p.X;
            _previewLine.Y2 = p.Y;
        }
        else if (_activeLine != null && (_movingStart || _movingEnd))
        {
            if (_movingStart) _activeLine.P1 = p;
            else _activeLine.P2 = p;
            RedrawAllLines();
        }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDrawing)
        {
            Point p = e.GetPosition(canvas);
            var line = new LineSegment { P1 = _startPoint, P2 = p, Thickness = 1 };
            _lines.Add(line);
            DrawLineSymmetricMidpoint(_baseBitmap, line.P1, line.P2, line.Thickness, line.Color);
            canvasHost.Children.Remove(_previewLine);
            _previewLine = null;
            canvas.Source = _baseBitmap;
            _isDrawing = false;
        }
        else
        {
            _activeLine = null;
            _movingStart = _movingEnd = false;
        }
    }

    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(canvas);
        _activeLine = _lines.FirstOrDefault(l => DistancePointToSegment(p, l.P1, l.P2) <= HIT_TOLERANCE);
        if (_activeLine == null) return;

        var cm = new ContextMenu();
        var miDel = new MenuItem { Header = "Delete Line" };
        miDel.Click += (_, __) =>
        {
            _lines.Remove(_activeLine);
            _activeLine = null;
            RedrawAllLines();
        };
        cm.Items.Add(miDel);

        var miThick = new MenuItem { Header = "Change Thickness..." };
        miThick.Click += (_, __) =>
        {
            string inp = Interaction.InputBox("Enter new thickness (px):", "Thickness", _activeLine.Thickness.ToString());
            if (int.TryParse(inp, out int t) && t > 0)
            {
                _activeLine.Thickness = t;
                RedrawAllLines();
            }
        };
        cm.Items.Add(miThick);

        var miColor = new MenuItem { Header = "Change Color..." };
        miColor.Click += (_, __) =>
        {
            var colorDialog = new ColorPickerDialog(_activeLine.Color);
            if (colorDialog.ShowDialog() == true)
            {
                _activeLine.Color = colorDialog.SelectedColor;
                RedrawAllLines();
            }
        };
        cm.Items.Add(miColor);

        cm.IsOpen = true;
    }

    private void SelectLine(LineSegment line, bool moveStart)
    {
        _activeLine = line;
        _movingStart = moveStart;
        _movingEnd = !moveStart;
    }
    #endregion

    #region Drawing & Hit-testing Helpers

    private void RedrawAllLines()
    {
        ClearBitmap(_baseBitmap, Colors.White);
        foreach (var line in _lines)
        {
            DrawLineSymmetricMidpoint(_baseBitmap, line.P1, line.P2, line.Thickness, line.Color);
        }
        canvas.Source = _baseBitmap;
    }

    private static double DistancePointToSegment(Point p, Point a, Point b)
    {
        Vector ap = p - a, ab = b - a;
        double t = Vector.Multiply(ap, ab) / ab.LengthSquared;
        t = Math.Max(0, Math.Min(1, t));
        Point projection = a + t * ab;
        return (p - projection).Length;
    }

    private void ClearBitmap(WriteableBitmap bmp, Color col)
    {
        int w = bmp.PixelWidth;
        int h = bmp.PixelHeight;
        int stride = bmp.BackBufferStride;
        byte[] clear = new byte[stride * h];
        for (int i = 0; i < clear.Length; i += 4)
        {
            clear[i] = col.B;
            clear[i + 1] = col.G;
            clear[i + 2] = col.R;
            clear[i + 3] = col.A;
        }
        bmp.WritePixels(new Int32Rect(0, 0, w, h), clear, stride, 0);
    }

    private unsafe void DrawLineSymmetricMidpoint(
        WriteableBitmap bmp,
        Point pp1, Point pp2,
        int thickness,
        Color color)
    {
        int x1 = (int)pp1.X, y1 = (int)pp1.Y;
        int x2 = (int)pp2.X, y2 = (int)pp2.Y;

        int dx = Math.Abs(x2 - x1), dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1, sy = y1 < y2 ? 1 : -1;
        bool steep = dy > dx;
        if (steep) { (dx, dy) = (dy, dx); }

        int d = 2 * dy - dx;
        int dE = 2 * dy;
        int dNE = 2 * (dy - dx);

        int xl = x1, yl = y1;
        int xr = x2, yr = y2;

        bmp.Lock();
        byte* pBuf = (byte*)bmp.BackBuffer.ToPointer();
        int stride = bmp.BackBufferStride;

        int half = thickness / 2;
        for (int i = 0; i <= dx; i++)
        {
            for (int tx = -half; tx <= half; tx++)
                for (int ty = -half; ty <= half; ty++)
                {
                    int px = steep ? yl + ty : xl + tx;
                    int py = steep ? xl + tx : yl + ty;
                    if (px >= 0 && px < bmp.PixelWidth && py >= 0 && py < bmp.PixelHeight)
                    {
                        int index = (py * stride) + (px * 4);
                        pBuf[index] = color.B;
                        pBuf[index + 1] = color.G;
                        pBuf[index + 2] = color.R;
                        pBuf[index + 3] = color.A;
                    }

                    px = steep ? yr + ty : xr + tx;
                    py = steep ? xr + tx : yr + ty;
                    if (px >= 0 && px < bmp.PixelWidth && py >= 0 && py < bmp.PixelHeight)
                    {
                        int index = (py * stride) + (px * 4);
                        pBuf[index] = color.B;
                        pBuf[index + 1] = color.G;
                        pBuf[index + 2] = color.R;
                        pBuf[index + 3] = color.A;
                    }
                }

            if (d < 0)
                d += dE;
            else
            {
                d += dNE;
                if (steep)  { xl += sx; xr -= sx; }
                else        { yl += sy; yr -= sy; }
            }

            if (steep)  { yl += sy; yr -= sy; }
            else        { xl += sx; xr -= sx; }
        }
    }
    #endregion

    #region Menu Item Handlers
    private void MenuOpenFile_Click(object sender, RoutedEventArgs e)
    {

    }

    private void MenuSaveFile_Click(object sender, RoutedEventArgs e)
    {

    }
    #endregion
}