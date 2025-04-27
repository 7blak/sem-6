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

public enum ToolType
{
    Select,
    Line,
    Circle
}

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

public class Circle : INotifyPropertyChanged
{
    private double _radius, _thickness;
    private Color _color;
    private Point _center;

    public double Radius
    {
        get => _radius;
        set { _radius = value; OnPropertyChanged(nameof(Radius)); }
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
    public Point Center
    {
        get => _center;
        set { _center = value; OnPropertyChanged(nameof(Center)); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public Circle()
    {
        Color = Colors.Black;
        Thickness = 1.0;
        Center = new Point(0, 0);
    }
}

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public const int CANVAS_WIDTH = 1200;
    public const int CANVAS_HEIGHT = 800;

    private const double SELECT_LINE_ENDPOINT_TOLERANCE = 8.0;
    private const double SELECT_LINE_NEAR_TOLERANCE = 5.0;
    private const double SELECT_LINE_SQUARE_SIZE = 6.0;
    private const double SELECT_CIRCLE_NEAR_TOLERANCE = 5.0;

    private bool _isDrawing = false;
    private bool _isDraggingMarker = false;
    private bool _isDraggingStartPoint = false;
    private bool _isDrawingCircle = false;

    private double _currentThickness = 3.0;

    private Color _currentColor = Colors.Black;
    private Color _backgroundColor = Colors.White;

    private Point _startPoint;

    private ToolType _currentTool = ToolType.Line;

    private Line? _selectedLine = null;
    private Circle? _selectedCircle = null;
    private System.Windows.Shapes.Line? _previewLine = null;
    private System.Windows.Shapes.Ellipse? _previewCircle = null;

    private WriteableBitmap _bitmap = new(400, 400, 96, 96, PixelFormats.Bgra32, null);

    private List<Line> _lines = [];
    private List<Circle> _circles = [];

    public double CurrentThickness { get { return _currentThickness; } set { if (_currentThickness != value) { _currentThickness = value <= 0 ? 1 : value >= 20 ? 20 : value; OnPropertyChanged(nameof(CurrentThickness)); } } }

    public Color CurrentColor { get { return _currentColor; } set { if (_currentColor != value) { _currentColor = value; OnPropertyChanged(nameof(CurrentColor)); } } }

    public Line? SelectedLine { get { return _selectedLine; } set { if (_selectedLine != value) { _selectedLine = value; OnPropertyChanged(nameof(SelectedLine)); } } }
    public Circle? SelectedCircle { get { return _selectedCircle; } set { if (_selectedCircle != value) { _selectedCircle = value; OnPropertyChanged(nameof(SelectedCircle)); } } }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void MainWindow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedLine))
        {
            UpdateSelectedLineMarkers();
        }
        else if (e.PropertyName == nameof(SelectedCircle))
        {
            UpdateSelectedCircleMarkers();
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        PropertyChanged += MainWindow_PropertyChanged;
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
    private void MenuClearImage_Click(object sender, RoutedEventArgs e)
    {
        ClearBitmap();
        _lines.Clear();
        _circles.Clear();
        RemoveCanvasHostChildrenTag("Marker");
    }
    private void MenuTool_Click(object sender, RoutedEventArgs e)
    {
        if (sender == SelectToolItem)
            _currentTool = ToolType.Select;
        else if (sender == LineToolItem)
            _currentTool = ToolType.Line;
        else if (sender == CircleToolItem)
            _currentTool = ToolType.Circle;
        else
            throw new ArgumentException("Unknown tool type");

        SelectToolItem.IsChecked = _currentTool == ToolType.Select;
        LineToolItem.IsChecked = _currentTool == ToolType.Line;
        CircleToolItem.IsChecked = _currentTool == ToolType.Circle;
    }
    private void Button_DecreaseThicknessValue(object sender, RoutedEventArgs e)
    {
        CurrentThickness -= 2.0;
    }

    private void Button_IncreaseThicknessValue(object sender, RoutedEventArgs e)
    {
        CurrentThickness += 2.0;
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
        else if (_isDraggingMarker && SelectedLine != null)
        {
            Point p = e.GetPosition(Canvas);

            if (_isDraggingStartPoint)
            {
                SelectedLine.X1 = p.X;
                SelectedLine.Y1 = p.Y;
            }
            else
            {
                SelectedLine.X2 = p.X;
                SelectedLine.Y2 = p.Y;
            }

            RedrawAll();
            UpdateSelectedLineMarkers();
        }
        else if (_isDraggingMarker && SelectedCircle != null)
        {
            Point p = e.GetPosition(Canvas);

            if (_isDraggingStartPoint)
            {
                SelectedCircle.Center = p;
            }
            else
            {
                SelectedCircle.Radius = (SelectedCircle.Center - p).Length;
            }

            RedrawAll();
            UpdateSelectedCircleMarkers();
        }
        else if (_isDrawingCircle && _previewCircle != null)
        {
            Point p = e.GetPosition(Canvas);

            double radius = (new Point(_startPoint.X, _startPoint.Y) - p).Length;
            _previewCircle.Width = radius * 2 + 3;
            _previewCircle.Height = radius * 2 + 3;
            System.Windows.Controls.Canvas.SetLeft(_previewCircle, _startPoint.X - radius);
            System.Windows.Controls.Canvas.SetTop(_previewCircle, _startPoint.Y - radius);
        }
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(Canvas);

        if (_currentTool == ToolType.Select)
        {
            foreach (var line in _lines)
            {
                Point startPoint = new(line.X1, line.Y1);
                Point endPoint = new(line.X2, line.Y2);
                if ((startPoint - p).Length <= SELECT_LINE_ENDPOINT_TOLERANCE)
                {
                    SelectLine(line);
                    return;
                }
                if ((endPoint - p).Length <= SELECT_LINE_ENDPOINT_TOLERANCE)
                {
                    SelectLine(line);
                    return;
                }
                if (DistancePointToLine(p, startPoint, endPoint) <= SELECT_LINE_NEAR_TOLERANCE)
                {
                    SelectLine(line);
                    return;
                }
            }
            foreach (var circle in _circles)
            {
                if (Math.Abs((circle.Center - p).Length - circle.Radius) <= SELECT_CIRCLE_NEAR_TOLERANCE)
                {
                    SelectCircle(circle);
                    return;
                }
            }
        }
        else if (_currentTool == ToolType.Line)
        {
            _isDrawing = true;
            _startPoint = p;
            SelectedLine = null;
            SelectedCircle = null;
            _previewLine = new System.Windows.Shapes.Line()
            {
                X1 = _startPoint.X,
                Y1 = _startPoint.Y,
                X2 = _startPoint.X,
                Y2 = _startPoint.Y,
                Stroke = new SolidColorBrush(_currentColor),
                StrokeThickness = _currentThickness,
            };
            CanvasHost.Children.Add(_previewLine);
        }
        else if (_currentTool == ToolType.Circle)
        {
            _isDrawingCircle = true;
            _startPoint = p;
            SelectedLine = null;
            SelectedCircle = null;
            _previewCircle = new System.Windows.Shapes.Ellipse()
            {
                Width = 0,
                Height = 0,
                Stroke = new SolidColorBrush(_currentColor),
                StrokeThickness = _currentThickness,
            };
            CanvasHost.Children.Add(_previewCircle);
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
                Color = _currentColor,
                Thickness = _currentThickness
            };
            _lines.Add(newLine);

            DrawLine(newLine);
        }
        else if (_isDraggingMarker)
        {
            _isDraggingMarker = false;
            Canvas.ReleaseMouseCapture();
            e.Handled = true;
        }
        else if (_isDrawingCircle)
        {
            Circle newCircle = new()
            {
                Center = _startPoint,
                Radius = (_startPoint - p).Length,
                Color = _currentColor,
                Thickness = _currentThickness
            };
            _circles.Add(newCircle);

            DrawCircle(newCircle);
        }
    }

    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(Canvas);
        SelectedLine = _lines.FirstOrDefault(line => DistancePointToLine(p, new Point(line.X1, line.Y1), new Point(line.X2, line.Y2)) <= SELECT_LINE_ENDPOINT_TOLERANCE);
        SelectedCircle = _circles.FirstOrDefault(circle => DistancePointToCircle(p, circle) <= SELECT_CIRCLE_NEAR_TOLERANCE);
        if (SelectedLine != null)
        {
            ContextMenu cm = new();

            MenuItem miDel = new() { Header = "Delete Line" };
            miDel.Click += (_, __) =>
            {
                _lines.Remove(SelectedLine);
                SelectedLine = null;
                RedrawAll();
            };
            cm.Items.Add(miDel);

            MenuItem miThick = new() { Header = "Thickness:" };
            StackPanel miThickContainer = new() { Orientation = Orientation.Horizontal };
            Button btl = new() { Content = "<<", Width = 25, VerticalAlignment = VerticalAlignment.Center };
            btl.Click += (_, __) =>
            {
                SelectedLine.Thickness -= 2.0;
                RedrawAll();
            };
            Button btr = new() { Content = ">>", Width = 25, VerticalAlignment = VerticalAlignment.Center };
            btr.Click += (_, __) =>
            {
                SelectedLine.Thickness += 2.0;
                RedrawAll();
            };
            TextBox tb = new() { Width = 25, Text = SelectedLine.Thickness.ToString() };
            Thickness margin = tb.Margin;
            margin.Left = 10;
            margin.Right = 10;
            tb.Margin = margin;
            Binding miThickBind = new("Thickness") { Source = SelectedLine, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            tb.SetBinding(TextBox.TextProperty, miThickBind);
            tb.LostKeyboardFocus += (_, __) =>
            {
                if (double.TryParse(tb.Text, out double newThickness))
                {
                    SelectedLine.Thickness = newThickness;
                    RedrawAll();
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
            ColorPicker cp = new() { Width = 50, SelectedColor = SelectedLine.Color };
            Binding miColorBind = new("Color") { Source = SelectedLine, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            cp.SetBinding(ColorPicker.SelectedColorProperty, miColorBind);
            cp.SelectedColorChanged += (_, __) =>
            {
                SelectedLine.Color = (Color)cp.SelectedColor;
                RedrawAll();
            };
            miColorContainer.Children.Add(cp);
            miColor.Header = miColorContainer;
            miColor.StaysOpenOnClick = true;
            cm.Items.Add(miColor);

            cm.IsOpen = true;
        }
        else if (SelectedCircle != null)
        {
            ContextMenu cm = new();

            MenuItem miDel = new() { Header = "Delete Circle" };
            miDel.Click += (_, __) =>
            {
                _circles.Remove(SelectedCircle);
                SelectedCircle = null;
                RedrawAll();
            };
            cm.Items.Add(miDel);

            MenuItem miThick = new() { Header = "Thickness:" };
            StackPanel miThickContainer = new() { Orientation = Orientation.Horizontal };
            Button btl = new() { Content = "<<", Width = 25, VerticalAlignment = VerticalAlignment.Center };
            btl.Click += (_, __) =>
            {
                SelectedCircle.Thickness -= 2.0;
                RedrawAll();
            };
            Button btr = new() { Content = ">>", Width = 25, VerticalAlignment = VerticalAlignment.Center };
            btr.Click += (_, __) =>
            {
                SelectedCircle.Thickness += 2.0;
                RedrawAll();
            };
            TextBox tb = new() { Width = 25, Text = SelectedCircle.Thickness.ToString() };
            Thickness margin = tb.Margin;
            margin.Left = 10;
            margin.Right = 10;
            tb.Margin = margin;
            Binding miThickBind = new("Thickness") { Source = SelectedCircle, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            tb.SetBinding(TextBox.TextProperty, miThickBind);
            tb.LostKeyboardFocus += (_, __) =>
            {
                if (double.TryParse(tb.Text, out double newThickness))
                {
                    SelectedCircle.Thickness = newThickness;
                    RedrawAll();
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
            ColorPicker cp = new() { Width = 50, SelectedColor = SelectedCircle.Color };
            Binding miColorBind = new("Color") { Source = SelectedCircle, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            cp.SetBinding(ColorPicker.SelectedColorProperty, miColorBind);
            cp.SelectedColorChanged += (_, __) =>
            {
                SelectedCircle.Color = (Color)cp.SelectedColor;
                RedrawAll();
            };
            miColorContainer.Children.Add(cp);
            miColor.Header = miColorContainer;
            miColor.StaysOpenOnClick = true;
            cm.Items.Add(miColor);

            cm.IsOpen = true;
        }
    }
    #endregion

    #region Bitmap Interactions & Drawing
    private void SelectLine(Line line)
    {
        SelectedLine = line;
    }

    private void SelectCircle(Circle circle)
    {
        SelectedCircle = circle;
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

        int d, dE, dNE;

        if (dx >= dy)
        {
            d = 2 * dy - dx;
            dE = 2 * dy;
            dNE = 2 * (dy - dx);

            for (int i = 0; i <= dx / 2; i++)
            {
                DrawThickPixel(x1, y1, halfThickness, buffer, stride, line.Color);
                DrawThickPixel(x2, y2, halfThickness, buffer, stride, line.Color);

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
                DrawThickPixel(x1, y1, halfThickness, buffer, stride, line.Color);
                DrawThickPixel(x2, y2, halfThickness, buffer, stride, line.Color);

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

    private unsafe void DrawCircle(Circle circle)
    {
        int xc = (int)Math.Round(circle.Center.X);
        int yc = (int)Math.Round(circle.Center.Y);
        int halfT = (int)(circle.Thickness / 2);

        int d = (int)(1 - circle.Radius) + halfT;
        int dE = 3;
        int dSE = (int)(5 - 2 * circle.Radius) + halfT;

        int x = 0;
        int y = (int)circle.Radius + halfT;

        _bitmap.Lock();
        byte* buffer = (byte*)_bitmap.BackBuffer.ToPointer();
        int stride = _bitmap.BackBufferStride;

        Draw8Octants();

        while (y > x)
        {
            if (d < 0)
            {
                d += dE;
                dE += 2;
                dSE += 2;
            }
            else
            {
                d += dSE;
                dE += 2;
                dSE += 4;
                y--;
            }
            x++;
            Draw8Octants();
        }

        _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
        _bitmap.Unlock();
        CanvasHost.Children.Remove(_previewCircle);
        Canvas.Source = _bitmap;
        _isDrawingCircle = false;

        void Draw8Octants()
        {
            DrawThickPixel(xc + x, yc + y, halfT, buffer, stride, circle.Color);
            DrawThickPixel(xc - x, yc + y, halfT, buffer, stride, circle.Color);
            DrawThickPixel(xc + x, yc - y, halfT, buffer, stride, circle.Color);
            DrawThickPixel(xc - x, yc - y, halfT, buffer, stride, circle.Color);
            DrawThickPixel(xc + y, yc + x, halfT, buffer, stride, circle.Color);
            DrawThickPixel(xc - y, yc + x, halfT, buffer, stride, circle.Color);
            DrawThickPixel(xc + y, yc - x, halfT, buffer, stride, circle.Color);
            DrawThickPixel(xc - y, yc - x, halfT, buffer, stride, circle.Color);
        }
    }

    private void RedrawAll()
    {
        ClearBitmap();
        foreach (var line in _lines)
        {
            DrawLine(line);
        }
        foreach (var circle in _circles)
        {
            DrawCircle(circle);
        }
    }

    private void UpdateSelectedLineMarkers()
    {
        RemoveCanvasHostChildrenTag("Marker");

        if (SelectedLine == null)
            return;

        Rectangle startMarker = CreateMarker(SelectedLine.X1, SelectedLine.Y1, true);
        Rectangle endMarker = CreateMarker(SelectedLine.X2, SelectedLine.Y2, false);

        CanvasHost.Children.Add(startMarker);
        CanvasHost.Children.Add(endMarker);
    }

    private void UpdateSelectedCircleMarkers()
    {
        RemoveCanvasHostChildrenTag("Marker");

        if (SelectedCircle == null)
            return;

        Rectangle centerMarker = CreateMarker(SelectedCircle.Center.X, SelectedCircle.Center.Y, true);
        Rectangle circumferenceMarker = CreateMarker(SelectedCircle.Center.X + SelectedCircle.Radius, SelectedCircle.Center.Y, false);

        CanvasHost.Children.Add(centerMarker);
        CanvasHost.Children.Add(circumferenceMarker);
    }
    private void StartMarker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingMarker = true;
        _isDraggingStartPoint = true;
        Canvas.CaptureMouse();
        e.Handled = true;
    }

    private void EndMarker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingMarker = true;
        _isDraggingStartPoint = false;
        Canvas.CaptureMouse();
        e.Handled = true;
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

    private static double DistancePointToCircle(Point p, Circle circle)
    {
        return Math.Abs((p - circle.Center).Length - circle.Radius);
    }

    private void RemoveCanvasHostChildrenTag(string tag)
    {
        for (int i = CanvasHost.Children.Count - 1; i >= 0; i--)
        {
            if (CanvasHost.Children[i] is FrameworkElement fe && fe.Tag?.ToString() == tag)
            {
                CanvasHost.Children.RemoveAt(i);
            }
        }
    }

    private unsafe void DrawThickPixel(int px, int py, int halfThickness, byte* buffer, int stride, Color color)
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
                buffer[idx] = color.B;
                buffer[idx + 1] = color.G;
                buffer[idx + 2] = color.R;
                buffer[idx + 3] = color.A;
            }
        }
    }

    Rectangle CreateMarker(double x, double y, bool isStartMarker)
    {
        Rectangle marker = new()
        {
            Width = SELECT_LINE_SQUARE_SIZE,
            Height = SELECT_LINE_SQUARE_SIZE,
            Fill = new SolidColorBrush(Colors.White),
            Stroke = new SolidColorBrush(Colors.Black),
            StrokeThickness = 1,
            Tag = "Marker"
        };

        if (isStartMarker)
            marker.MouseLeftButtonDown += StartMarker_MouseLeftButtonDown;
        else
            marker.MouseLeftButtonDown += EndMarker_MouseLeftButtonDown;

        System.Windows.Controls.Canvas.SetLeft(marker, x - marker.Width / 2);
        System.Windows.Controls.Canvas.SetTop(marker, y - marker.Height / 2);
        return marker;
    }
    #endregion
}