using Microsoft.VisualBasic;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
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
    Circle,
    Polygon
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

public class Polygon : INotifyPropertyChanged
{
    private int _selectedVertexIndex = -1;
    private double _thickness;
    private Color _color;
    private List<Point> _vertices;
    public int SelectedVertexIndex
    {
        get => _selectedVertexIndex;
        set { _selectedVertexIndex = value; OnPropertyChanged(nameof(SelectedVertexIndex)); }
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
    public List<Point> Vertices
    {
        get => _vertices;
        set { _vertices = value; OnPropertyChanged(nameof(Vertices)); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public Polygon()
    {
        Color = Colors.Black;
        Thickness = 1.0;
        _vertices = [];
    }
}

public class ProjectData
{
    public int BitmapWidth { get; set; }
    public int BitmapHeight { get; set; }
    public required List<LineDto> Lines { get; set; }
    public required List<CircleDto> Circles { get; set; }
    public required List<PolygonDto> Polygons { get; set; }
}

public class LineDto
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public double Thickness { get; set; }
    public required string Color { get; set; }
}

public class CircleDto
{
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Radius { get; set; }
    public double Thickness { get; set; }
    public required string Color { get; set; }
}

public class PolygonDto
{
    public required List<VertexDto> Vertices { get; set; }
    public double Thickness { get; set; }
    public required string Color { get; set; }
}

public class VertexDto
{
    public double X { get; set; }
    public double Y { get; set; }
}

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const double SELECT_LINE_ENDPOINT_TOLERANCE = 8.0;
    private const double SELECT_LINE_NEAR_TOLERANCE = 5.0;
    private const double SELECT_LINE_SQUARE_SIZE = 6.0;
    private const double SELECT_CIRCLE_NEAR_TOLERANCE = 5.0;
    private const double SELECT_POLYGON_VERTEX_TOLERANCE = 4.5;

    private bool _isDrawingLine = false;
    private bool _isDrawingCircle = false;
    private bool _isDrawingPolygon = false;
    private bool _isDraggingMarker = false;
    private bool _isDraggingStartPoint = false;
    private bool _isAntialiasingOn = false;

    private double _currentThickness = 3.0;

    private string? _currentFilePath = null;

    private Color _currentColor = Colors.Black;
    private Color _backgroundColor = Colors.White;

    private Point _startPoint;

    private ToolType _currentTool = ToolType.Line;

    private Line? _selectedLine = null;
    private Circle? _selectedCircle = null;
    private Polygon? _selectedPolygon = null;
    private Polygon? _currentPolygon = null;

    private System.Windows.Shapes.Line? _previewLine = null;
    private System.Windows.Shapes.Ellipse? _previewCircle = null;


    private WriteableBitmap _bitmap = new(400, 400, 96, 96, PixelFormats.Bgra32, null);

    private List<Point> _verticesCopy = [];

    private List<Line> _lines = [];
    private List<Circle> _circles = [];
    private List<Polygon> _polygons = [];

    public bool IsAntialiasingOn { get { return _isAntialiasingOn; } set { if (_isAntialiasingOn != value) { _isAntialiasingOn = value; OnPropertyChanged(nameof(IsAntialiasingOn)); } } }
    public double CurrentThickness { get { return _currentThickness; } set { if (_currentThickness != value) { _currentThickness = value <= 0 ? 1 : value >= 20 ? 20 : value; OnPropertyChanged(nameof(CurrentThickness)); } } }

    public Color CurrentColor { get { return _currentColor; } set { if (_currentColor != value) { _currentColor = value; OnPropertyChanged(nameof(CurrentColor)); } } }

    public Line? SelectedLine { get { return _selectedLine; } set { if (_selectedLine != value) { _selectedLine = value; OnPropertyChanged(nameof(SelectedLine)); } } }
    public Circle? SelectedCircle { get { return _selectedCircle; } set { if (_selectedCircle != value) { _selectedCircle = value; OnPropertyChanged(nameof(SelectedCircle)); } } }
    public Polygon? SelectedPolygon { get { return _selectedPolygon; } set { if (_selectedPolygon != value) { _selectedPolygon = value; OnPropertyChanged(nameof(SelectedPolygon)); } } }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        PropertyChanged += MainWindow_PropertyChanged;
        KeyDown += MainWindow_KeyDown;
        ClearBitmap();
    }

    #region MainWindow Events
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
        else if (e.PropertyName == nameof(SelectedPolygon))
        {
            UpdateSelectedPolygonMarkers();
        }
        else if (e.PropertyName == nameof(IsAntialiasingOn))
        {
            RedrawAll();
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                DeselectAll();
                if (_isDrawingLine)
                {
                    CanvasHost.Children.Remove(_previewLine);
                    _previewLine = null;
                    _isDrawingLine = false;
                }
                else if (_isDrawingCircle)
                {
                    CanvasHost.Children.Remove(_previewCircle);
                    _previewCircle = null;
                    _isDrawingCircle = false;
                }
                else if (_isDraggingMarker)
                {
                    _isDraggingMarker = false;
                    Canvas.ReleaseMouseCapture();
                }
                else if (_isDrawingPolygon)
                {
                    RemoveCanvasHostChildrenTag("PolygonPreviewLine");
                    _currentPolygon = null;
                    _previewLine = null;
                    _isDrawingPolygon = false;
                }
                break;
            case Key.Enter:
                DeselectAll();
                if (_isDrawingPolygon && _currentPolygon != null)
                {
                    _polygons.Add(_currentPolygon);
                    DrawPolygon(_currentPolygon);
                    RemoveCanvasHostChildrenTag("PolygonPreviewLine");
                    _currentPolygon = null;
                    _previewLine = null;
                    _isDrawingPolygon = false;
                }
                break;
            case Key.Delete:
                if (SelectedLine != null)
                    _lines.Remove(SelectedLine); SelectedLine = null;
                if (SelectedCircle != null)
                    _circles.Remove(SelectedCircle); SelectedCircle = null;
                if (SelectedPolygon != null)
                    _polygons.Remove(SelectedPolygon); SelectedPolygon = null;
                RedrawAll();
                break;
        }
    }
    #endregion

    #region Menu Item Handlers
    private void MenuNew_Click(object sender, RoutedEventArgs e)
    {
        NewFileWindow newFileWindow = new();
        newFileWindow.Owner = this;
        newFileWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
        newFileWindow.Left = Left + 50;
        newFileWindow.Top = Top + 50;
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
        OpenFileDialog openFileDialog = new()
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Open a file"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            _currentFilePath = openFileDialog.FileName;
            string json = File.ReadAllText(_currentFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            ProjectData data = JsonSerializer.Deserialize<ProjectData>(json, options) ?? throw new InvalidOperationException("Failed to deserialize project data");

            _lines.Clear();
            _circles.Clear();
            _polygons.Clear();

            Color ConvertColor(string s)
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
                _lines.Add(line);
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
                _circles.Add(circle);
            }

            foreach (var dto in data.Polygons)
            {
                var polygon = new Polygon
                {
                    Thickness = dto.Thickness,
                    Color = ConvertColor(dto.Color),
                    Vertices = new List<Point>()
                };
                foreach (var v in dto.Vertices)
                    polygon.Vertices.Add(new Point(v.X, v.Y));
                _polygons.Add(polygon);
            }

            _bitmap = new WriteableBitmap(data.BitmapWidth, data.BitmapHeight, 96, 96, PixelFormats.Bgra32, null);
            CanvasHost.Width = data.BitmapWidth;
            CanvasHost.Height = data.BitmapHeight;
            Canvas.Width = data.BitmapWidth;
            Canvas.Height = data.BitmapHeight;
            RedrawAll();
        }
    }

    private void MenuSave_Click(object sender, RoutedEventArgs e)
    {

    }

    private void MenuSaveFile_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = new()
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Save a file"
        };
        if (saveFileDialog.ShowDialog() == true)
        {
            _currentFilePath = saveFileDialog.FileName;

            ProjectData data = new ProjectData
            {
                BitmapWidth = _bitmap.PixelWidth,
                BitmapHeight = _bitmap.PixelHeight,
                Lines = new List<LineDto>(),
                Circles = new List<CircleDto>(),
                Polygons = new List<PolygonDto>()
            };

            string ConvertColor(Color c)
            {
                return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
            }

            foreach (var line in _lines)
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

            foreach (var circle in _circles)
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

            foreach (var polygon in _polygons)
            {
                var polygonDto = new PolygonDto
                {
                    Thickness = polygon.Thickness,
                    Color = ConvertColor(polygon.Color),
                    Vertices = new List<VertexDto>()
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

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_currentFilePath, json);
        }
    }

    private void MenuClearImage_Click(object sender, RoutedEventArgs e)
    {
        ClearBitmap();
        _lines.Clear();
        _circles.Clear();
        _polygons.Clear();
        RemoveCanvasHostChildrenTag("Marker");
        RemoveCanvasHostChildrenTag("PolygonPreviewLine");
        DeselectAll();
        _currentPolygon = null;
    }

    private void MenuTool_Click(object sender, RoutedEventArgs e)
    {
        if (sender == SelectToolItem)
            _currentTool = ToolType.Select;
        else if (sender == LineToolItem)
            _currentTool = ToolType.Line;
        else if (sender == CircleToolItem)
            _currentTool = ToolType.Circle;
        else if (sender == PolygonToolItem)
            _currentTool = ToolType.Polygon;
        else
            throw new ArgumentException("Unknown tool type");

        SelectToolItem.IsChecked = _currentTool == ToolType.Select;
        LineToolItem.IsChecked = _currentTool == ToolType.Line;
        CircleToolItem.IsChecked = _currentTool == ToolType.Circle;
        PolygonToolItem.IsChecked = _currentTool == ToolType.Polygon;
    }

    private void MenuAntialiasing_Click(object sender, RoutedEventArgs e)
    {
        IsAntialiasingOn = !IsAntialiasingOn;
        AntialiasingMenuItem.IsChecked = IsAntialiasingOn;
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
        if (_previewLine != null)
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
        else if (_isDraggingMarker && SelectedPolygon != null && !_isDrawingPolygon)
        {
            Point p = e.GetPosition(Canvas);

            if (_isDraggingStartPoint)
            {
                double dx = p.X - _startPoint.X;
                double dy = p.Y - _startPoint.Y;

                for (int i = 0; i < SelectedPolygon.Vertices.Count; i++)
                    SelectedPolygon.Vertices[i] = new Point(_verticesCopy[i].X + dx, _verticesCopy[i].Y + dy);
            }
            else
                SelectedPolygon.Vertices[SelectedPolygon.SelectedVertexIndex] = p;

            RedrawAll();
            UpdateSelectedPolygonMarkers();
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
                    Select(line);
                    return;
                }
                if ((endPoint - p).Length <= SELECT_LINE_ENDPOINT_TOLERANCE)
                {
                    Select(line);
                    return;
                }
                if (DistancePointToLine(p, startPoint, endPoint) <= SELECT_LINE_NEAR_TOLERANCE)
                {
                    Select(line);
                    return;
                }
            }
            foreach (var circle in _circles)
            {
                if (Math.Abs((circle.Center - p).Length - circle.Radius) <= SELECT_CIRCLE_NEAR_TOLERANCE)
                {
                    Select(circle);
                    return;
                }
            }
            foreach (var polygon in _polygons)
            {
                for (int i = 0; i < polygon.Vertices.Count; i++)
                {
                    var vertex = polygon.Vertices[i];
                    if ((vertex - p).Length <= SELECT_POLYGON_VERTEX_TOLERANCE)
                    {
                        Select(polygon);
                        return;
                    }
                    if (DistancePointToLine(p, vertex, polygon.Vertices[(i + 1) % polygon.Vertices.Count]) <= SELECT_LINE_NEAR_TOLERANCE)
                    {
                        Select(polygon);
                        return;
                    }
                }
            }
        }
        else if (_currentTool == ToolType.Line)
        {
            DeselectAll();
            _isDrawingLine = true;
            _startPoint = p;
            _previewLine = new System.Windows.Shapes.Line()
            {
                X1 = _startPoint.X,
                Y1 = _startPoint.Y,
                X2 = _startPoint.X,
                Y2 = _startPoint.Y,
                Stroke = new SolidColorBrush(_currentColor),
                StrokeThickness = IsAntialiasingOn ? 1 : _currentThickness,
            };
            CanvasHost.Children.Add(_previewLine);
        }
        else if (_currentTool == ToolType.Circle)
        {
            DeselectAll();
            _isDrawingCircle = true;
            _startPoint = p;
            _previewCircle = new System.Windows.Shapes.Ellipse()
            {
                Width = 0,
                Height = 0,
                Stroke = new SolidColorBrush(_currentColor),
                StrokeThickness = IsAntialiasingOn ? 1 : _currentThickness,
            };
            CanvasHost.Children.Add(_previewCircle);
        }
        else if (_currentTool == ToolType.Polygon)
        {
            DeselectAll();
            _isDrawingPolygon = true;
            System.Windows.Shapes.Line polygonPreviewLine = new()
            {
                X1 = p.X,
                Y1 = p.Y,
                X2 = p.X,
                Y2 = p.Y,
                Stroke = new SolidColorBrush(_currentColor),
                StrokeThickness = IsAntialiasingOn ? 1 : _currentThickness,
                Tag = "PolygonPreviewLine"
            };
            _currentPolygon ??= new Polygon()
            {
                Color = _currentColor,
                Thickness = _currentThickness
            };
            _previewLine = polygonPreviewLine;
            Point vertexToAdd = p;
            foreach (var vertex in _currentPolygon.Vertices)
                if ((vertex - p).Length <= SELECT_POLYGON_VERTEX_TOLERANCE)
                    vertexToAdd = vertex;
            if (_currentPolygon.Vertices.Count != 0 && vertexToAdd == _currentPolygon.Vertices.First())
            {
                _polygons.Add(_currentPolygon);
                DrawPolygon(_currentPolygon);
                RemoveCanvasHostChildrenTag("PolygonPreviewLine");
                _currentPolygon = null;
                _previewLine = null;
                _isDrawingPolygon = false;
            }
            else
            {
                _currentPolygon.Vertices.Add(vertexToAdd);
                Select(_currentPolygon);
                CanvasHost.Children.Add(polygonPreviewLine);
            }
        }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(Canvas);

        if (_isDrawingLine)
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
            CanvasHost.Children.Remove(_previewLine);
            _previewLine = null;
            _isDrawingLine = false;
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
            CanvasHost.Children.Remove(_previewCircle);
            _previewCircle = null;
            _isDrawingCircle = false;
        }
        else if (_isDraggingMarker)
        {
            _isDraggingMarker = false;
            Canvas.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point p = e.GetPosition(Canvas);
        SelectedLine = _lines.FirstOrDefault(line => DistancePointToLine(p, new Point(line.X1, line.Y1), new Point(line.X2, line.Y2)) <= SELECT_LINE_ENDPOINT_TOLERANCE);
        SelectedCircle = _circles.FirstOrDefault(circle => DistancePointToCircle(p, circle) <= SELECT_CIRCLE_NEAR_TOLERANCE);
        SelectedPolygon = _polygons.FirstOrDefault(polygon =>
                   polygon.Vertices.Any(vertex => (vertex - p).Length <= SELECT_POLYGON_VERTEX_TOLERANCE) ||
                   polygon.Vertices.Zip(polygon.Vertices.Skip(1).Append(polygon.Vertices.First()), (start, end) =>
                   DistancePointToLine(p, start, end) <= SELECT_LINE_NEAR_TOLERANCE).Any());
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
        else if (SelectedPolygon != null)
        {
            ContextMenu cm = new();

            MenuItem miDel = new() { Header = "Delete Polygon" };
            miDel.Click += (_, __) =>
            {
                _polygons.Remove(SelectedPolygon);
                SelectedPolygon = null;
                RedrawAll();
            };
            cm.Items.Add(miDel);

            MenuItem miThick = new() { Header = "Thickness:" };
            StackPanel miThickContainer = new() { Orientation = Orientation.Horizontal };
            Button btl = new() { Content = "<<", Width = 25, VerticalAlignment = VerticalAlignment.Center };
            btl.Click += (_, __) =>
            {
                SelectedPolygon.Thickness -= 2.0;
                RedrawAll();
            };
            Button btr = new() { Content = ">>", Width = 25, VerticalAlignment = VerticalAlignment.Center };
            btr.Click += (_, __) =>
            {
                SelectedPolygon.Thickness += 2.0;
                RedrawAll();
            };
            TextBox tb = new() { Width = 25, Text = SelectedPolygon.Thickness.ToString() };
            Thickness margin = tb.Margin;
            margin.Left = 10;
            margin.Right = 10;
            tb.Margin = margin;
            Binding miThickBind = new("Thickness") { Source = SelectedPolygon, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            tb.SetBinding(TextBox.TextProperty, miThickBind);
            tb.LostKeyboardFocus += (_, __) =>
            {
                if (double.TryParse(tb.Text, out double newThickness))
                {
                    SelectedPolygon.Thickness = newThickness;
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
            ColorPicker cp = new() { Width = 50, SelectedColor = SelectedPolygon.Color };
            Binding miColorBind = new("Color") { Source = SelectedPolygon, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            cp.SetBinding(ColorPicker.SelectedColorProperty, miColorBind);
            cp.SelectedColorChanged += (_, __) =>
            {
                SelectedPolygon.Color = (Color)cp.SelectedColor;
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
    private void Select<T>(T obj)
    {
        switch (obj)
        {
            case Line line:
                SelectedLine = line;
                break;
            case Circle circle:
                SelectedCircle = circle;
                break;
            case Polygon polygon:
                SelectedPolygon = polygon;
                break;
            default:
                throw new ArgumentException("Unknown object type");
        }
    }

    private void Deselect<T>(T obj)
    {
        switch (obj)
        {
            case Line:
                SelectedLine = null;
                break;
            case Circle:
                SelectedCircle = null;
                break;
            case Polygon:
                SelectedPolygon = null;
                break;
            default:
                throw new ArgumentException("Unknown object type");
        }
    }

    private void DeselectAll()
    {
        SelectedLine = null;
        SelectedCircle = null;
        SelectedPolygon = null;
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

        if (IsAntialiasingOn && dx >= dy)
        {
            double slope = (double)(y2 - y1) / (x2 - x1);

            for (int x = x1; x <= x2; ++x)
            {
                double y = y1 + slope * (x - x1);
                int yFloor = (int)Math.Floor(y);
                double fraction = y - yFloor;

                Color c1 = BlendColors(_currentColor, _backgroundColor, 1 - fraction);
                Color c2 = BlendColors(_currentColor, _backgroundColor, fraction);

                DrawPixel(x, yFloor, buffer, stride, c1);
                DrawPixel(x, yFloor + 1, buffer, stride, c2);
            }
        }
        else if (IsAntialiasingOn)
        {

        }
        else if (dx >= dy)
        {
            d = 2 * dy - dx;
            dE = 2 * dy;
            dNE = 2 * (dy - dx);

            for (int i = 0; i <= dx / 2; i++)
            {
                DrawThickPixel(x1, y1, buffer, stride, line.Color, halfThickness, true);
                DrawThickPixel(x2, y2, buffer, stride, line.Color, halfThickness, true);

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
                DrawThickPixel(x1, y1, buffer, stride, line.Color, halfThickness, false);
                DrawThickPixel(x2, y2, buffer, stride, line.Color, halfThickness, false);

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
        Canvas.Source = _bitmap;
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
        Canvas.Source = _bitmap;

        void Draw8Octants()
        {
            DrawThickPixel(xc + y, yc - x, buffer, stride, circle.Color, halfT, false); // octant 1
            DrawThickPixel(xc + x, yc - y, buffer, stride, circle.Color, halfT, true); // octant 2
            DrawThickPixel(xc - x, yc - y, buffer, stride, circle.Color, halfT, true); // octant 3
            DrawThickPixel(xc - y, yc - x, buffer, stride, circle.Color, halfT, false); // octant 4
            DrawThickPixel(xc - y, yc + x, buffer, stride, circle.Color, halfT, false); // octant 5
            DrawThickPixel(xc - x, yc + y, buffer, stride, circle.Color, halfT, true); // octant 6
            DrawThickPixel(xc + x, yc + y, buffer, stride, circle.Color, halfT, true); // octant 7
            DrawThickPixel(xc + y, yc + x, buffer, stride, circle.Color, halfT, false); // octant 8
        }
    }

    private unsafe void DrawPolygon(Polygon polygon)
    {
        if (polygon == null)
            throw new ArgumentNullException("Current polygon was null, something went wrong");

        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            var vertex = polygon.Vertices[i];

            if (i == polygon.Vertices.Count - 1)
            {
                DrawLine(new Line()
                {
                    X1 = vertex.X,
                    Y1 = vertex.Y,
                    X2 = polygon.Vertices[0].X,
                    Y2 = polygon.Vertices[0].Y,
                    Color = polygon.Color,
                    Thickness = polygon.Thickness
                });
            }
            else
            {
                DrawLine(new Line()
                {
                    X1 = vertex.X,
                    Y1 = vertex.Y,
                    X2 = polygon.Vertices[i + 1].X,
                    Y2 = polygon.Vertices[i + 1].Y,
                    Color = polygon.Color,
                    Thickness = polygon.Thickness
                });
            }
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
        foreach (var polygon in _polygons)
        {
            DrawPolygon(polygon);
        }
    }

    private void UpdateSelectedLineMarkers()
    {
        RemoveCanvasHostChildrenTag("Marker");

        if (SelectedLine == null)
            return;

        Rectangle startMarker = CreateMarker(SelectedLine.X1, SelectedLine.Y1, Colors.White, true);
        Rectangle endMarker = CreateMarker(SelectedLine.X2, SelectedLine.Y2, Colors.White, false);

        CanvasHost.Children.Add(startMarker);
        CanvasHost.Children.Add(endMarker);
    }

    private void UpdateSelectedCircleMarkers()
    {
        RemoveCanvasHostChildrenTag("Marker");

        if (SelectedCircle == null)
            return;

        Rectangle centerMarker = CreateMarker(SelectedCircle.Center.X, SelectedCircle.Center.Y, Colors.White, true);
        Rectangle circumferenceMarker = CreateMarker(SelectedCircle.Center.X + SelectedCircle.Radius, SelectedCircle.Center.Y, Colors.White, false);

        CanvasHost.Children.Add(centerMarker);
        CanvasHost.Children.Add(circumferenceMarker);
    }

    private void UpdateSelectedPolygonMarkers()
    {
        RemoveCanvasHostChildrenTag("Marker");
        if (SelectedPolygon == null)
            return;

        _verticesCopy = SelectedPolygon.Vertices.ToList();

        double sumX = 0, sumY = 0;

        for (int i = 0; i < SelectedPolygon.Vertices.Count; i++)
        {
            sumX += SelectedPolygon.Vertices[i].X;
            sumY += SelectedPolygon.Vertices[i].Y;

            Rectangle marker = CreateMarker(SelectedPolygon.Vertices[i].X, SelectedPolygon.Vertices[i].Y, i == 0 ? Colors.DodgerBlue : Colors.White, false);
            CanvasHost.Children.Add(marker);
        }

        Rectangle centerMarker = CreateMarker(sumX / SelectedPolygon.Vertices.Count, sumY / SelectedPolygon.Vertices.Count, Colors.PaleVioletRed, true);
        CanvasHost.Children.Add(centerMarker);

        _startPoint = new Point(sumX / SelectedPolygon.Vertices.Count, sumY / SelectedPolygon.Vertices.Count);
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
        if (SelectedPolygon != null)
        {
            for (int i = 0; i < SelectedPolygon.Vertices.Count; i++)
            {
                var vertex = SelectedPolygon.Vertices[i];
                if ((vertex - e.GetPosition(Canvas)).Length <= SELECT_POLYGON_VERTEX_TOLERANCE)
                {
                    SelectedPolygon.SelectedVertexIndex = i;
                    break;
                }
            }
        }
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

    private unsafe void DrawThickPixel(int px, int py, byte* buffer, int stride, Color color, int halfThickness, bool isHoriztional)
    {
        if (isHoriztional)
        {
            for (int ty = -halfThickness; ty <= halfThickness; ty++)
            {
                int fy = py + ty;

                if (px < 0 || px >= _bitmap.PixelWidth || fy < 0 || fy >= _bitmap.PixelHeight)
                    continue;

                DrawPixel(px, fy, buffer, stride, color);
            }
        }
        else
        {
            for (int tx = -halfThickness; tx <= halfThickness; tx++)
            {
                int fx = px + tx;

                if (fx < 0 || fx >= _bitmap.PixelWidth || py < 0 || py >= _bitmap.PixelHeight)
                    continue;

                DrawPixel(fx, py, buffer, stride, color);
            }
        }
    }

    private unsafe void DrawPixel(int x, int y, byte* buffer, int stride, Color color)
    {
        int idx = (y * stride) + (x * 4);
        buffer[idx] = color.B;
        buffer[idx + 1] = color.G;
        buffer[idx + 2] = color.R;
        buffer[idx + 3] = color.A;
    }

    Rectangle CreateMarker(double x, double y, Color fillColor, bool isStartMarker)
    {
        Rectangle marker = new()
        {
            Width = SELECT_LINE_SQUARE_SIZE,
            Height = SELECT_LINE_SQUARE_SIZE,
            Fill = new SolidColorBrush(fillColor),
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

    private Color BlendColors(Color color1, Color color2, double ratio)
    {
        byte r = (byte)(color1.R * ratio + color2.R * (1 - ratio));
        byte g = (byte)(color1.G * ratio + color2.G * (1 - ratio));
        byte b = (byte)(color1.B * ratio + color2.B * (1 - ratio));
        byte a = (byte)(color1.A * ratio + color2.A * (1 - ratio));

        return Color.FromArgb(a, r, g, b);
    }
    #endregion
}