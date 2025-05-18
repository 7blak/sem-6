using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace rasterization_2.shapes;

public class Polygon : INotifyPropertyChanged
{
    private bool _isFillColor;
    private bool _isFilled;
    private int _selectedVertexIndex = -1;
    private double _thickness;
    private Color _color;
    private Color _fillColor;
    private List<Point> _vertices;
    private BitmapSource? _bitmapSource;
    public bool IsFillColor
    {
        get => _isFillColor;
        set { _isFillColor = value; OnPropertyChanged(nameof(IsFillColor)); }
    }
    public bool IsFilled
    {
        get => _isFilled;
        set { _isFilled = value; OnPropertyChanged(nameof(IsFilled)); }
    }
    public int SelectedVertexIndex
    {
        get => _selectedVertexIndex;
        set { _selectedVertexIndex = value; OnPropertyChanged(nameof(SelectedVertexIndex)); }
    }
    public double Thickness
    {
        get => _thickness;
        set { _thickness = value <= 0 ? 1 : value >= 21 ? 21 : value; OnPropertyChanged(nameof(Thickness)); }
    }
    public Color Color
    {
        get => _color;
        set { _color = value; OnPropertyChanged(nameof(Color)); }
    }
    public Color FillColor
    {
        get => _fillColor;
        set { _fillColor = value; OnPropertyChanged(nameof(FillColor)); }
    }
    public List<Point> Vertices
    {
        get => _vertices;
        set { _vertices = value; OnPropertyChanged(nameof(Vertices)); }
    }
    public BitmapSource? BitmapSource
    {
        get => _bitmapSource;
        set { _bitmapSource = value; OnPropertyChanged(nameof(BitmapSource)); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public Polygon()
    {
        _isFillColor = true;
        _isFilled = false;
        Color = Colors.Black;
        FillColor = Colors.White;
        Thickness = 1.0;
        _vertices = [];
        _bitmapSource = null;
    }
}
