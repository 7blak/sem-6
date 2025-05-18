using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace rasterization_2.shapes
{
    public class Rectangle : INotifyPropertyChanged
    {
        private int _selectedVertexIndex = -1;
        private double _thickness;
        private Color _color;
        private Line _diagonal;
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
        public Line Diagonal
        {
            get => _diagonal;
            set { _diagonal = value; OnPropertyChanged(nameof(Diagonal)); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public Rectangle()
        {
            Color = Colors.Black;
            Thickness = 1.0;
            _diagonal = new Line();
        }
    }
}
