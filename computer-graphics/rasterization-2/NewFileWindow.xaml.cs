using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace rasterization_2
{
    public partial class NewFileWindow : Window, INotifyPropertyChanged
    {
        private int _width;
        private int _height;
        private Color _color;

        public int XWidth
        {
            get { return _width; }
            set { _width = value; OnPropertyChanged(nameof(Width)); }
        }
        public int XHeight
        {
            get { return _height; }
            set { _height = value; OnPropertyChanged(nameof(Height)); }
        }
        public Color Color
        {
            get { return _color; }
            set
            { _color = value; OnPropertyChanged(nameof(Color)); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public NewFileWindow()
        {
            InitializeComponent();
            XWidth = 600;
            XHeight = 400;
            Color = Colors.White;
            DataContext = this;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        [GeneratedRegex("^[0-9]+$")]
        private static partial Regex NumericInputRegex();

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !NumericInputRegex().IsMatch(e.Text);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string tagString)
            {
                string[] limits = tagString.Split(',');
                if (limits.Length == 2 && int.TryParse(limits[0], out int min) && int.TryParse(limits[1], out int max))
                {
                    if (int.TryParse(textBox.Text, out int value))
                    {
                        value = Math.Max(min, Math.Min(max, value));
                    }
                    else
                    {
                        value = min;
                    }
                    textBox.Text = value.ToString();
                }
            }
        }
    }
}
