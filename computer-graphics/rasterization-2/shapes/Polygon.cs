using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace rasterization_2.shapes;

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
        set { _thickness = value <= 0 ? 1 : value >= 21 ? 21 : value; OnPropertyChanged(nameof(Thickness)); }
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
