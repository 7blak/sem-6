using Microsoft.VisualBasic;
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
using Xceed.Wpf.Toolkit;
using static System.Net.Mime.MediaTypeNames;

namespace rasterization_2;

public class Line : INotifyPropertyChanged
{
    private double _x1, _y1, _x2, _y2, _thickness;
    private Color _color;

    public double X1
    {
        get => _x1;
        set { _x1 = value; OnPropertyChanged(nameof(X1)); }
    }
    public double Y1
    {
        get => _y1;
        set { _y1 = value; OnPropertyChanged(nameof(Y1)); }
    }
    public double X2
    {
        get => _x2;
        set { _x2 = value; OnPropertyChanged(nameof(X2)); }
    }
    public double Y2
    {
        get => _y2;
        set { _y2 = value; OnPropertyChanged(nameof(Y2)); }
    }
    public double Thickness
    {
        get => _thickness;
        set { _thickness = value <= 0 ? 1 : value >= 20 ? 20 : value; OnPropertyChanged(nameof(Thickness)); }
    }
    public Color Color
    {
        get => _color;
        set { _color = value; OnPropertyChanged(nameof(Color)); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public Line()
    {
        Color = Colors.Black;
        Thickness = 1.0;
    }
}

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public const int CANVAS_WIDTH = 1200;
    public const int CANVAS_HEIGHT = 800;

    private const double SELECT_LINE_ENDPOINT_TOLERANCE = 8.0;

    private bool _isDrawing = false;
    private bool _isSelectToolOn = false;
    private bool _selectedStart = false;
    private bool _selectedEnd = false;

    private double _lineThickness = 3.0;

    private Color _lineColor = Colors.Black;
    private Color _backgroundColor = Colors.White;

    private Point _startPoint;

    private Line? _selectedLine = null;
    private System.Windows.Shapes.Line? _previewLine = null;

    private WriteableBitmap _bitmap = new(400, 400, 96, 96, PixelFormats.Bgra32, null);

    private List<Line> _lines = [];

    public double LineThickness { get { return _lineThickness; } set { if (_lineThickness != value) { _lineThickness = value <= 0 ? 1 : value >= 20 ? 20 : value; OnPropertyChanged(nameof(LineThickness)); } } }

    public Color LineColor { get { return _lineColor; } set { if (_lineColor != value) { _lineColor = value; OnPropertyChanged(nameof(LineColor)); } } }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        ClearBitmap();
    }

    #region Menu Item Handlers
    private void MenuNew_Click(object sender, RoutedEventArgs e)
    {
        NewFileWindow newFileWindow = new();
        if (newFileWindow.ShowDialog() == true)
        {
            _bitmap = new WriteableBitmap(newFileWindow.XWidth, newFileWindow.XHeight, 96, 96, PixelFormats.Bgra32, null);
            _backgroundColor = newFileWindow.Color;
            CanvasHost.Width = newFileWindow.XWidth;
            CanvasHost.Height = newFileWindow.XHeight;
            Canvas.Width = newFileWindow.XWidth;
            Canvas.Height = newFileWindow.XHeight;
            ClearBitmap();
        }
    }
    private void MenuOpenFile_Click(object sender, RoutedEventArgs e)
    {

    }
    private void MenuSave_Click(object sender, RoutedEventArgs e)
    {

    }
    private void MenuSaveFile_Click(object sender, RoutedEventArgs e)
    {

    }

    private void Button_DecreaseThicknessValue(object sender, RoutedEventArgs e)
    {
        LineThickness -= 2.0;
    }

    private void Button_IncreaseThicknessValue(object sender, RoutedEventArgs e)
    {
        LineThickness += 2.0;
    }
    #endregion

    #region Canvas Mouse Interactions
    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDrawing && _previewLine != null)
        {
            Point p = e.GetPosition(Canvas);

            _previewLine.X2 = p.X;
            _previewLine.Y2 = p.Y;
        }
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(Canvas);

        if (_isSelectToolOn)
        {
            foreach (var line in _lines)
            {
                Point startPoint = new(line.X1, line.Y1);
                Point endPoint = new(line.X2, line.Y2);
                if ((startPoint - p).Length <= SELECT_LINE_ENDPOINT_TOLERANCE)
                {
                    SelectLine(line, true);
                    return;
                }
                if ((endPoint - p).Length <= SELECT_LINE_ENDPOINT_TOLERANCE)
                {
                    SelectLine(line, false);
                    return;
                }
            }
        }
        else
        {
            _isDrawing = true;
            _startPoint = p;
            _selectedLine = null;
            _previewLine = new System.Windows.Shapes.Line()
            {
                X1 = _startPoint.X,
                Y1 = _startPoint.Y,
                X2 = _startPoint.X,
                Y2 = _startPoint.Y,
                Stroke = new SolidColorBrush(_lineColor),
                StrokeThickness = _lineThickness,
            };
            CanvasHost.Children.Add(_previewLine);
        }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(Canvas);
        if (_isDrawing)
        {
            Line newLine = new()
            {
                X1 = _startPoint.X,
                Y1 = _startPoint.Y,
                X2 = p.X,
                Y2 = p.Y,
                Color = _lineColor,
                Thickness = _lineThickness
            };
            _lines.Add(newLine);

            DrawLine(newLine);
        }
    }

    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(Canvas);
        _selectedLine = _lines.FirstOrDefault(line => DistancePointToLine(p, new Point(line.X1, line.Y1), new Point(line.X2, line.Y2)) <= SELECT_LINE_ENDPOINT_TOLERANCE);
        if (_selectedLine == null)
            return;

        ContextMenu cm = new();

        MenuItem miDel = new() { Header = "Delete Line" };
        miDel.Click += (_, __) =>
        {
            _lines.Remove(_selectedLine);
            _selectedLine = null;
            RedrawAllLines();
        };
        cm.Items.Add(miDel);

        MenuItem miThick = new() { Header = "Thickness:" };
        StackPanel miThickContainer = new() { Orientation = Orientation.Horizontal };
        Button btl = new() { Content = "<<", Width = 25, VerticalAlignment = VerticalAlignment.Center };
        btl.Click += (_, __) =>
        {
            _selectedLine.Thickness -= 2.0;
            RedrawAllLines();
        };
        Button btr = new() { Content = ">>", Width = 25, VerticalAlignment = VerticalAlignment.Center };
        btr.Click += (_, __) =>
        {
            _selectedLine.Thickness += 2.0;
            RedrawAllLines();
        };
        TextBox tb = new() { Width = 25, Text = _selectedLine.Thickness.ToString() };
        Thickness margin = tb.Margin;
        margin.Left = 10;
        margin.Right = 10;
        tb.Margin = margin;
        Binding miThickBind = new("Thickness") { Source = _selectedLine, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
        tb.SetBinding(TextBox.TextProperty, miThickBind);
        tb.LostKeyboardFocus += (_, __) =>
        {
            if (double.TryParse(tb.Text, out double newThickness))
            {
                _selectedLine.Thickness = newThickness;
                RedrawAllLines();
            }
        };
        miThickContainer.Children.Add(btl);
        miThickContainer.Children.Add(tb);
        miThickContainer.Children.Add(btr);
        miThick.Header = miThickContainer;
        miThick.StaysOpenOnClick = true;
        cm.Items.Add(miThick);

        MenuItem miColor = new() { Header = "Change Color..." };
        StackPanel miColorContainer = new();
        ColorPicker cp = new() { Width = 50, SelectedColor = _selectedLine.Color };
        Binding miColorBind = new("Color") { Source = _selectedLine, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
        cp.SetBinding(ColorPicker.SelectedColorProperty, miColorBind);
        cp.SelectedColorChanged += (_, __) =>
        {
            _selectedLine.Color = (Color)cp.SelectedColor;
            RedrawAllLines();
        };
        miColorContainer.Children.Add(cp);
        miColor.Header = miColorContainer;
        miColor.StaysOpenOnClick = true;
        cm.Items.Add(miColor);

        cm.IsOpen = true;
    }
    #endregion

    #region Bitmap Interactions & Drawing
    private void SelectLine(Line line, bool selectedStart)
    {
        _selectedLine = line;
        _selectedStart = selectedStart;
        _selectedEnd = !selectedStart;
    }

    private unsafe void DrawLine(Line line)
    {
        _bitmap.Lock();
        byte* buffer = (byte*)_bitmap.BackBuffer.ToPointer();
        int stride = _bitmap.BackBufferStride;

        int x1 = (int)Math.Round(line.X1);
        int y1 = (int)Math.Round(line.Y1);
        int x2 = (int)Math.Round(line.X2);
        int y2 = (int)Math.Round(line.Y2);

        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx1 = x1 < x2 ? 1 : -1;
        int sy1 = y1 < y2 ? 1 : -1;
        int sx2 = x2 < x1 ? 1 : -1;
        int sy2 = y2 < y1 ? 1 : -1;

        int halfThickness = (int)(line.Thickness / 2);

        void DrawThickPixel(int px, int py)
        {
            for (int tx = -halfThickness; tx <= halfThickness; tx++)
            {
                for (int ty = -halfThickness; ty <= halfThickness; ty++)
                {
                    int fx = px + tx;
                    int fy = py + ty;

                    if (fx < 0 || fx >= _bitmap.PixelWidth || fy < 0 || fy >= _bitmap.PixelHeight)
                        continue;

                    int idx = (fy * stride) + (fx * 4);
                    buffer[idx] = line.Color.B;
                    buffer[idx + 1] = line.Color.G;
                    buffer[idx + 2] = line.Color.R;
                    buffer[idx + 3] = line.Color.A;
                }
            }
        }

        int d, dE, dNE;

        if (dx >= dy)
        {
            d = 2 * dy - dx;
            dE = 2 * dy;
            dNE = 2 * (dy - dx);

            for (int i = 0; i <= dx / 2; i++)
            {
                DrawThickPixel(x1, y1);
                DrawThickPixel(x2, y2);

                if (d <= 0)
                {
                    d += dE;
                }
                else
                {
                    d += dNE;
                    y1 += sy1;
                    y2 += sy2;
                }

                x1 += sx1;
                x2 += sx2;
            }
        }
        else
        {
            d = 2 * dx - dy;
            dE = 2 * dx;
            dNE = 2 * (dx - dy);

            for (int i = 0; i <= dy / 2; i++)
            {
                DrawThickPixel(x1, y1);
                DrawThickPixel(x2, y2);

                if (d <= 0)
                {
                    d += dE;
                }
                else
                {
                    d += dNE;
                    x1 += sx1;
                    x2 += sx2;
                }

                y1 += sy1;
                y2 += sy2;
            }
        }

        _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
        _bitmap.Unlock();
        CanvasHost.Children.Remove(_previewLine);
        Canvas.Source = _bitmap;
        _isDrawing = false;
    }

    private void RedrawAllLines()
    {
        ClearBitmap();
        foreach (var line in _lines)
        {
            DrawLine(line);
        }
    }
    private unsafe void ClearBitmap()
    {
        int width = _bitmap.PixelWidth;
        int height = _bitmap.PixelHeight;
        int stride = width * 4;

        _bitmap.Lock();
        byte* buffer = (byte*)_bitmap.BackBuffer.ToPointer();

        for (int i = 0; i < width * height * 4; i += 4)
        {
            buffer[i] = _backgroundColor.B;
            buffer[i + 1] = _backgroundColor.G;
            buffer[i + 2] = _backgroundColor.R;
            buffer[i + 3] = _backgroundColor.A;
        }

        _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
        _bitmap.Unlock();
        Canvas.Source = _bitmap;
    }
    #endregion

    #region Helper and Utility Functions
    private static double DistancePointToLine(Point p, Point a, Point b)
    {
        Vector ap = p - a, ab = b - a;
        double t = Vector.Multiply(ap, ab) / ab.LengthSquared;
        t = Math.Max(0, Math.Min(1, t));
        Point projection = a + t * ab;
        return (p - projection).Length;
    }
    #endregion
}