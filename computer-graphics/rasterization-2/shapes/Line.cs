using System.ComponentModel;
using System.Windows.Media;

namespace rasterization_2.shapes;

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
        set { _thickness = value <= 0 ? 1 : value >= 21 ? 21 : value; OnPropertyChanged(nameof(Thickness)); }
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
