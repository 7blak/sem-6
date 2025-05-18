using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace rasterization_2.shapes;

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
        set { _thickness = value <= 0 ? 1 : value >= 21 ? 21 : value; OnPropertyChanged(nameof(Thickness)); }
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
