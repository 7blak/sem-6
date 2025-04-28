using Microsoft.Win32;
using rasterization_2.shapes;
using rasterization_2.serialization;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xceed.Wpf.Toolkit;

using static rasterization_2.Util;

namespace rasterization_2;

public enum ToolType
{
    Select,
    Line,
    Circle,
    Polygon,
    Pill
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
    private bool _isDrawingPill = false;
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

    private Line? _line1 = null;
    private Line? _line2 = null;
    private int _lineCount = 0;

    private System.Windows.Shapes.Line? _previewLine = null;
    private System.Windows.Shapes.Ellipse? _previewCircle = null;


    private WriteableBitmap _bitmap = new(400, 400, 96, 96, PixelFormats.Bgra32, null);

    private List<Point> _verticesCopy = [];

    private List<Line> _lines = [];
    private List<Circle> _circles = [];
    private List<Polygon> _polygons = [];

    public bool IsAntialiasingOn { get { return _isAntialiasingOn; } set { if (_isAntialiasingOn != value) { _isAntialiasingOn = value; OnPropertyChanged(nameof(IsAntialiasingOn)); } } }
    public double CurrentThickness { get { return _currentThickness; } set { if (_currentThickness != value) { _currentThickness = value <= 0 ? 1 : value >= 21 ? 21 : value; OnPropertyChanged(nameof(CurrentThickness)); } } }

    public Color CurrentColor { get { return _currentColor; } set { if (_currentColor != value) { _currentColor = value; OnPropertyChanged(nameof(CurrentColor)); } } }

    public Line? SelectedLine { get { return _selectedLine; } set { if (_selectedLine != value) { _selectedLine = value; OnPropertyChanged(nameof(SelectedLine)); } } }
    public Circle? SelectedCircle { get { return _selectedCircle; } set { if (_selectedCircle != value) { _selectedCircle = value; OnPropertyChanged(nameof(SelectedCircle)); } } }
    public Polygon? SelectedPolygon { get { return _selectedPolygon; } set { if (_selectedPolygon != value) { _selectedPolygon = value; OnPropertyChanged(nameof(SelectedPolygon)); } } }

    public WriteableBitmap Bitmap { get { return _bitmap; } set { if (_bitmap != value) { _bitmap = value; OnPropertyChanged(nameof(Bitmap)); } } }

    public List<Line> Lines { get { return _lines; } set { if (_lines != value) { _lines = value; OnPropertyChanged(nameof(Lines)); } } }
    public List<Circle> Circles { get { return _circles; } set { if (_circles != value) { _circles = value; OnPropertyChanged(nameof(Circles)); } } }
    public List<Polygon> Polygons { get { return _polygons; } set { if (_polygons != value) { _polygons = value; OnPropertyChanged(nameof(Polygons)); } } }

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
        NewFileWindow newFileWindow = new()
        {
            Owner = this,
            WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
            Left = Left + 50,
            Top = Top + 50
        };
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
            Serialization.LoadFromFile(_currentFilePath = openFileDialog.FileName, this);
        }
    }

    private void MenuSave_Click(object sender, RoutedEventArgs e)
    {
        if (_currentFilePath != null)
            Serialization.SerializeToFile(_currentFilePath, this);
        else
            MenuSaveFile_Click(sender, e);
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
            Serialization.SerializeToFile(saveFileDialog.FileName, this);
        }
    }

    private void MenuClearImage_Click(object sender, RoutedEventArgs e)
    {
        ClearBitmap();
        _lines.Clear();
        _circles.Clear();
        _polygons.Clear();
        RemoveCanvasHostChildrenTag("Marker");
        RemoveCanvasHostChildrenTag("PreviewLine");
        RemoveCanvasHostChildrenTag("PreviewCircle");
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
        else if (sender == PillToolItem)
            _currentTool = ToolType.Pill;
        else
            throw new ArgumentException("Unknown tool type");

        SelectToolItem.IsChecked = _currentTool == ToolType.Select;
        LineToolItem.IsChecked = _currentTool == ToolType.Line;
        CircleToolItem.IsChecked = _currentTool == ToolType.Circle;
        PolygonToolItem.IsChecked = _currentTool == ToolType.Polygon;
        PillToolItem.IsChecked = _currentTool == ToolType.Pill;
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
                Tag = "PreviewLine"
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
                Tag = "PreviewCircle"
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
        else if (_currentTool == ToolType.Pill)
        {
            DeselectAll();
            _isDrawingPill = true;
            if (_lineCount == 0)
            {
                _startPoint = p;
                _lineCount++;
                _previewLine = new System.Windows.Shapes.Line()
                {
                    X1 = _startPoint.X,
                    Y1 = _startPoint.Y,
                    X2 = _startPoint.X,
                    Y2 = _startPoint.Y,
                    Stroke = new SolidColorBrush(_currentColor),
                    StrokeThickness = IsAntialiasingOn ? 1 : _currentThickness,
                    Tag = "PreviewLine"
                };
                CanvasHost.Children.Add(_previewLine);
            }
            else if (_lineCount == 1)
            {
                _line1 = new()
                {
                    X1 = _startPoint.X,
                    Y1 = _startPoint.Y,
                    X2 = p.X,
                    Y2 = p.Y,
                    Color = _currentColor,
                    Thickness = _currentThickness
                };
                _startPoint = p;
                _lineCount++;
                _previewLine = new System.Windows.Shapes.Line()
                {
                    X1 = _startPoint.X,
                    Y1 = _startPoint.Y,
                    X2 = _startPoint.X,
                    Y2 = _startPoint.Y,
                    Stroke = new SolidColorBrush(_currentColor),
                    StrokeThickness = IsAntialiasingOn ? 1 : _currentThickness,
                    Tag = "PreviewLine"
                };
                CanvasHost.Children.Add(_previewLine);
            }
            else if (_lineCount == 2)
            {
                _line2 = new()
                {
                    X1 = _startPoint.X,
                    Y1 = _startPoint.Y,
                    X2 = p.X,
                    Y2 = p.Y,
                    Color = _currentColor,
                    Thickness = _currentThickness
                };
                
                DrawPill(_line1!, _line2!);
                RemoveCanvasHostChildrenTag("PreviewLine");
                _isDrawingPill = false;
                _line1 = null;
                _line2 = null;
                _lineCount = 0;
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

        if (IsAntialiasingOn)
        {
            bool steep = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);
            if (steep)
            {
                (x1, y1) = (y1, x1);
                (x2, y2) = (y2, x2);
            }
            if (x1 > x2)
            {
                (x1, x2) = (x2, x1);
                (y1, y2) = (y2, y1);
            }

            dx = x2 - x1;
            dy = y2 - y1;

            double gradient = dx == 0 ? 1 : (double)dy / dx;

            double xend = x1;
            double yend = y1 + gradient * (xend - x1);
            double xgap = Rfpart(x1 + 0.5);
            double xpxl1 = xend;
            double ypxl1 = Ipart(yend);

            Color c1 = BlendColors(line.Color, _backgroundColor, Rfpart(yend) * xgap);
            Color c2 = BlendColors(line.Color, _backgroundColor, Fpart(yend) * xgap);

            if (steep)
            {
                DrawPixel((int)ypxl1, (int)xpxl1, buffer, stride, c1);
                DrawPixel((int)ypxl1 + 1, (int)xpxl1, buffer, stride, c2);
            }
            else
            {
                DrawPixel((int)xpxl1, (int)ypxl1, buffer, stride, c1);
                DrawPixel((int)xpxl1, (int)ypxl1 + 1, buffer, stride, c2);
            }

            double intery = yend + gradient;

            xend = x2;
            yend = y2 + gradient * (xend - x2);
            xgap = Fpart(x2 + 0.5);
            double xpxl2 = xend;
            double ypxl2 = Ipart(yend);

            c1 = BlendColors(line.Color, _backgroundColor, Rfpart(yend) * xgap);
            c2 = BlendColors(line.Color, _backgroundColor, Fpart(yend) * xgap);

            if (steep)
            {
                DrawPixel((int)ypxl2, (int)xpxl2, buffer, stride, c1);
                DrawPixel((int)ypxl2 + 1, (int)xpxl2, buffer, stride, c2);
            }
            else
            {
                DrawPixel((int)xpxl2, (int)ypxl2, buffer, stride, c1);
                DrawPixel((int)xpxl2, (int)ypxl2 + 1, buffer, stride, c2);
            }

            if (steep)
            {
                for (int x = (int)xpxl1 + 1; x < xpxl2 - 1; x++)
                {
                    DrawPixel((int)Ipart(intery), x, buffer, stride, BlendColors(line.Color, _backgroundColor, Rfpart(intery)));
                    DrawPixel((int)Ipart(intery) + 1, x, buffer, stride, BlendColors(line.Color, _backgroundColor, Fpart(intery)));
                    intery += gradient;
                }
            }
            else
            {
                for (int x = (int)xpxl1 + 1; x < xpxl2 - 1; x++)
                {
                    DrawPixel(x, (int)Ipart(intery), buffer, stride, BlendColors(line.Color, _backgroundColor, Rfpart(intery)));
                    DrawPixel(x, (int)Ipart(intery) + 1, buffer, stride, BlendColors(line.Color, _backgroundColor, Fpart(intery)));
                    intery += gradient;
                }
            }
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
        int radius = (int)Math.Round(circle.Radius);

        int halfT = (int)(circle.Thickness / 2);

        int d = (int)(1 - circle.Radius) + halfT;
        int dE = 3;
        int dSE = (int)(5 - 2 * circle.Radius) + halfT;

        int x = IsAntialiasingOn ? radius : 0;
        int y = IsAntialiasingOn ? 0 : (int)circle.Radius + halfT;

        _bitmap.Lock();
        byte* buffer = (byte*)_bitmap.BackBuffer.ToPointer();
        int stride = _bitmap.BackBufferStride;

        if (IsAntialiasingOn)
        {
            Draw8Octants(x, y, circle.Color);

            while (x > y)
            {
                y++;
                double f = Math.Sqrt(radius * radius - y * y);
                int xi = (int)Math.Ceiling(f);

                Draw8Octants(xi, y, BlendColors(circle.Color, _backgroundColor, 1 - xi + f));
                Draw8Octants(xi - 1, y, BlendColors(circle.Color, _backgroundColor, xi - f));
            }
        }
        else
        {
            DrawThick8Octants();

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
                DrawThick8Octants();
            }
        }

        _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
        _bitmap.Unlock();
        Canvas.Source = _bitmap;

        void DrawThick8Octants()
        {
            DrawThickPixel(xc + y, yc - x, buffer, stride, circle.Color, halfT, false); // octant 1
            DrawThickPixel(xc + x, yc - y, buffer, stride, circle.Color, halfT, true);  // octant 2
            DrawThickPixel(xc - x, yc - y, buffer, stride, circle.Color, halfT, true);  // octant 3
            DrawThickPixel(xc - y, yc - x, buffer, stride, circle.Color, halfT, false); // octant 4
            DrawThickPixel(xc - y, yc + x, buffer, stride, circle.Color, halfT, false); // octant 5
            DrawThickPixel(xc - x, yc + y, buffer, stride, circle.Color, halfT, true);  // octant 6
            DrawThickPixel(xc + x, yc + y, buffer, stride, circle.Color, halfT, true);  // octant 7
            DrawThickPixel(xc + y, yc + x, buffer, stride, circle.Color, halfT, false); // octant 8
        }

        void Draw8Octants(int x, int y, Color color)
        {
            DrawPixel(xc + y, yc - x, buffer, stride, color); // octant 1
            DrawPixel(xc + x, yc - y, buffer, stride, color); // octant 2
            DrawPixel(xc - x, yc - y, buffer, stride, color); // octant 3
            DrawPixel(xc - y, yc - x, buffer, stride, color); // octant 4
            DrawPixel(xc - y, yc + x, buffer, stride, color); // octant 5
            DrawPixel(xc - x, yc + y, buffer, stride, color); // octant 6
            DrawPixel(xc + x, yc + y, buffer, stride, color); // octant 7
            DrawPixel(xc + y, yc + x, buffer, stride, color); // octant 8
        }
    }

    private unsafe void DrawPolygon(Polygon polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon, "Current polygon was null, something went wrong");

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

    private unsafe void DrawPill(Line centerLine, Line radiusLine)
    {
        double dxR = radiusLine.X2 - radiusLine.X1;
        double dyR = radiusLine.Y2 - radiusLine.Y1;
        double radius = Math.Sqrt(dxR * dxR + dyR * dyR);

        double dx = centerLine.X2 - centerLine.X1;
        double dy = centerLine.Y2 - centerLine.Y1;
        double length = Math.Sqrt(dx * dx + dy * dy);

        Vector u = new Vector(dx / length, dy / length); //unit dir. along the centerLine
        Vector n = new Vector(-u.Y, u.X); //perp. to unit normal

        Point c1 = new Point(centerLine.X1, centerLine.Y1);
        Point c2 = new Point(centerLine.X2, centerLine.Y2);

        Point pA1 = c1 + n * radius;
        Point pA2 = c2 + n * radius;
        Point pB1 = c1 - n * radius;
        Point pB2 = c2 - n * radius;

        var edge1 = new Line
        {
            X1 = pA1.X,
            Y1 = pA1.Y,
            X2 = pA2.X,
            Y2 = pA2.Y,
            Color = centerLine.Color,
            Thickness = centerLine.Thickness
        };

        var edge2 = new Line
        {
            X1 = pB1.X,
            Y1 = pB1.Y,
            X2 = pB2.X,
            Y2 = pB2.Y,
            Color = centerLine.Color,
            Thickness = centerLine.Thickness
        };

        DrawLine(edge1);
        DrawLine(edge2);

        DrawSemiCircle(new Circle() { Center = c1, Color = _currentColor, Radius = radius, Thickness = _currentThickness}, u);
        DrawSemiCircle(new Circle() { Center = c2, Color = _currentColor, Radius = radius, Thickness = _currentThickness }, -u);
    }

    private unsafe void DrawSemiCircle(Circle circle, Vector direction)
    {
        int xc = (int)Math.Round(circle.Center.X);
        int yc = (int)Math.Round(circle.Center.Y);
        int radius = (int)Math.Round(circle.Radius);

        int halfT = (int)(circle.Thickness / 2);

        int d = (int)(1 - circle.Radius) + halfT;
        int dE = 3;
        int dSE = (int)(5 - 2 * circle.Radius) + halfT;

        int x = IsAntialiasingOn ? radius : 0;
        int y = IsAntialiasingOn ? 0 : (int)circle.Radius + halfT;

        _bitmap.Lock();
        byte* buffer = (byte*)_bitmap.BackBuffer.ToPointer();
        int stride = _bitmap.BackBufferStride;

        if (IsAntialiasingOn)
        {
            Draw8Octants(x, y, circle.Color);

            while (x > y)
            {
                y++;
                double f = Math.Sqrt(radius * radius - y * y);
                int xi = (int)Math.Ceiling(f);

                Draw8Octants(xi, y, BlendColors(circle.Color, _backgroundColor, 1 - xi + f));
                Draw8Octants(xi - 1, y, BlendColors(circle.Color, _backgroundColor, xi - f));
            }
        }
        else
        {
            DrawThick8Octants();

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
                DrawThick8Octants();
            }
        }

        _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
        _bitmap.Unlock();
        Canvas.Source = _bitmap;

        void DrawThick8Octants()
        {
            var pts = new (int px, int py, bool steep)[]
            {
                (xc + y, yc - x, false),  // octant 1
                (xc + x, yc - y, true),   // octant 2
                (xc - x, yc - y, true),   // octant 3
                (xc - y, yc - x, false),  // octant 4
                (xc - y, yc + x, false),  // octant 5
                (xc - x, yc + y, true),   // octant 6
                (xc + x, yc + y, true),   // octant 7
                (xc + y, yc + x, false),  // octant 8
            };

            foreach (var (px, py, steep) in pts)
            {
                Vector off = new Vector(px - xc, py - yc);
                if (Vector.Multiply(off, direction) <= 0)
                    DrawThickPixel(px, py, buffer, stride, circle.Color, halfT, steep);
            }
        }

        void Draw8Octants(int x, int y, Color color)
        {
            Vector off = new Vector(x - xc, y - yc);
            if (Vector.Multiply(off, direction) < 0)
                return;
            DrawPixel(xc + y, yc - x, buffer, stride, color); // octant 1
            DrawPixel(xc + x, yc - y, buffer, stride, color); // octant 2
            DrawPixel(xc - x, yc - y, buffer, stride, color); // octant 3
            DrawPixel(xc - y, yc - x, buffer, stride, color); // octant 4
            DrawPixel(xc - y, yc + x, buffer, stride, color); // octant 5
            DrawPixel(xc - x, yc + y, buffer, stride, color); // octant 6
            DrawPixel(xc + x, yc + y, buffer, stride, color); // octant 7
            DrawPixel(xc + y, yc + x, buffer, stride, color); // octant 8
        }
    }

    public void RedrawAll()
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

    #region Marker Functions
    private void UpdateSelectedLineMarkers()
    {
        RemoveCanvasHostChildrenTag("Marker");

        if (SelectedLine == null)
            return;

        System.Windows.Shapes.Rectangle startMarker = CreateMarker(SelectedLine.X1, SelectedLine.Y1, Colors.White, true);
        System.Windows.Shapes.Rectangle endMarker = CreateMarker(SelectedLine.X2, SelectedLine.Y2, Colors.White, false);

        CanvasHost.Children.Add(startMarker);
        CanvasHost.Children.Add(endMarker);
    }

    private void UpdateSelectedCircleMarkers()
    {
        RemoveCanvasHostChildrenTag("Marker");

        if (SelectedCircle == null)
            return;

        System.Windows.Shapes.Rectangle centerMarker = CreateMarker(SelectedCircle.Center.X, SelectedCircle.Center.Y, Colors.White, true);
        System.Windows.Shapes.Rectangle circumferenceMarker = CreateMarker(SelectedCircle.Center.X + SelectedCircle.Radius, SelectedCircle.Center.Y, Colors.White, false);

        CanvasHost.Children.Add(centerMarker);
        CanvasHost.Children.Add(circumferenceMarker);
    }

    private void UpdateSelectedPolygonMarkers()
    {
        RemoveCanvasHostChildrenTag("Marker");
        if (SelectedPolygon == null)
            return;

        _verticesCopy = [.. SelectedPolygon.Vertices];

        double sumX = 0, sumY = 0;

        for (int i = 0; i < SelectedPolygon.Vertices.Count; i++)
        {
            sumX += SelectedPolygon.Vertices[i].X;
            sumY += SelectedPolygon.Vertices[i].Y;

            System.Windows.Shapes.Rectangle marker = CreateMarker(SelectedPolygon.Vertices[i].X, SelectedPolygon.Vertices[i].Y, i == 0 ? Colors.DodgerBlue : Colors.White, false);
            CanvasHost.Children.Add(marker);
        }

        System.Windows.Shapes.Rectangle centerMarker = CreateMarker(sumX / SelectedPolygon.Vertices.Count, sumY / SelectedPolygon.Vertices.Count, Colors.PaleVioletRed, true);
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
    #endregion

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
        if (x < 0 || x >= _bitmap.PixelWidth || y < 0 || y >= _bitmap.PixelHeight)
            return;

        int idx = (y * stride) + (x * 4);
        buffer[idx] = color.B;
        buffer[idx + 1] = color.G;
        buffer[idx + 2] = color.R;
        buffer[idx + 3] = color.A;
    }

    System.Windows.Shapes.Rectangle CreateMarker(double x, double y, Color fillColor, bool isStartMarker)
    {
        System.Windows.Shapes.Rectangle marker = new()
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
    #endregion
}