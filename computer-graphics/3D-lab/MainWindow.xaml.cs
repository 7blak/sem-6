using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _3D_lab;

public partial class MainWindow : Window
{
    private readonly Point3D[] _vertices;
    private readonly Tuple<int, int>[] _edges;

    public MainWindow()
    {
        InitializeComponent();

        _vertices =
            [
                new Point3D(-1, -1, -1), new Point3D(1, -1, -1),
                new Point3D(1, 1, -1), new Point3D(-1, 1, -1),
                new Point3D(-1, -1, 1), new Point3D(1, -1, 1),
                new Point3D(1, 1, 1), new Point3D(-1, 1, 1)
            ];

        _edges =
            [
                Tuple.Create(0, 1), Tuple.Create(1, 2), Tuple.Create(2, 3), Tuple.Create(3, 0),
                Tuple.Create(4, 5), Tuple.Create(5, 6), Tuple.Create(6, 7), Tuple.Create(7, 4),
                Tuple.Create(0, 4), Tuple.Create(1, 5), Tuple.Create(2, 6), Tuple.Create(3, 7)
            ];
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering += (s, e) => DrawCube();
    }

    private void OnParameterChanged(object sender, RoutedEventArgs e)
    {
        DrawCube();
    }

    private void DrawCube()
    {
        if (DrawCanvas == null)
            return;

        double angleX = SliderRotateX.Value * Math.PI / 180;
        double angleY = SliderRotateY.Value * Math.PI / 180;
        double distance = SliderDistance.Value;

        Matrix3D rotationX = new(
            1, 0, 0, 0,
            0, Math.Cos(angleX), -Math.Sin(angleX), 0,
            0, Math.Sin(angleX), Math.Cos(angleX), 0,
            0, 0, 0, 1);

        Matrix3D rotationY = new(
            Math.Cos(angleY), 0, Math.Sin(angleY), 0,
            0, 1, 0, 0,
            -Math.Sin(angleY), 0, Math.Cos(angleY), 0,
            0, 0, 0, 1);

        Matrix3D transform = rotationX * rotationY;

        double fov = 90;
        double fovRad = 1.0 / Math.Tan(fov * 0.5 * Math.PI / 180);

        DrawCanvas.Children.Clear();
        double centerX = DrawCanvas.ActualWidth / 2;
        double centerY = DrawCanvas.ActualHeight / 2;

        var projected = _vertices.Select(v =>
        {
            var rotated = transform.Transform(v);
            double z = rotated.Z + distance;
            if (z <= 0.1)
                z = 0.1; // Prevent division by zero
            double x = rotated.X * fovRad / z;
            double y = rotated.Y * fovRad / z;
            return new Point(centerX + x * 300, centerY - y * 300);
        }).ToArray();

        foreach (var edge in _edges)
        {
            var start = projected[edge.Item1];
            var end = projected[edge.Item2];

            var line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            DrawCanvas.Children.Add(line);
        }
    }
}