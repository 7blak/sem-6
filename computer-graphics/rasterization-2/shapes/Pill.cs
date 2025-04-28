using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace rasterization_2.shapes
{
    public class Pill
    {
        private double _thickness;
        private Color _color;
        private Circle _circle1;
        private Circle _circle2;
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
        public Circle Circle1
        {
            get => _circle1;
            set { _circle1 = value; OnPropertyChanged(nameof(Circle1)); }
        }
        public Circle Circle2
        {
            get => _circle2;
            set { _circle2 = value; OnPropertyChanged(nameof(Circle2)); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public Pill()
        {
            _color = Colors.Black;
            _thickness = 1.0;
            _circle1 = new();
            _circle2 = new();
        }
    }
}
