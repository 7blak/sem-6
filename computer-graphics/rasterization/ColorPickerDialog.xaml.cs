using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace rasterization
{
    public partial class ColorPickerDialog : Window
    {
        public Color SelectedColor { get; private set; }
        public ColorPickerDialog(Color initialColor)
        {
            InitializeComponent();
            colorPicker.SelectedColor = initialColor;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (colorPicker.SelectedColor.HasValue)
                SelectedColor = colorPicker.SelectedColor.Value;
            DialogResult = true;
        }
    }
}
