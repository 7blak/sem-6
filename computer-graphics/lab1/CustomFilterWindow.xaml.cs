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

namespace lab1
{
    /// <summary>
    /// Interaction logic for CustomFilterWindow.xaml
    /// </summary>
    public partial class CustomFilterWindow : Window
    {
        private int _rows = 3;
        private int _columns = 3;
        private TextBox[,] _gridCells = new TextBox[9, 9];
        private MainWindow _mainWindow;
        private bool finished = false;
        private static double[,] defaultKernel =
        {
            {0,0,0 },
            {0,1,0 },
            {0,0,0 }
        };

        public CustomFilterWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            if (_mainWindow.CustomConvolutionFilter == null)
            {
                InitializeField(1, 1, 1, 0, 3, 3);
                CreateGrid();
                _gridCells[1, 1].Background = Brushes.Wheat;
            }
            else
            {
                InitializeField(_mainWindow.CustomConvolutionFilter.Anchor.X, _mainWindow.CustomConvolutionFilter.Anchor.Y, _mainWindow.CustomConvolutionFilter.Divisor, _mainWindow.CustomConvolutionFilter.Offset,
                    _mainWindow.CustomConvolutionFilter.Kernel.GetLength(0), _mainWindow.CustomConvolutionFilter.Kernel.GetLength(1));
                CreateGrid(_mainWindow.CustomConvolutionFilter.Kernel);
                _gridCells[(int)_mainWindow.CustomConvolutionFilter.Anchor.Y + _rows / 2, (int)_mainWindow.CustomConvolutionFilter.Anchor.X + _columns / 2].Background = Brushes.Wheat;
            }
            finished = true;
        }

        private void InitializeField(double x, double y, double divisor, double offset, int rows, int columns)
        {
            int[] values = { 1, 3, 5, 7, 9 };

            XTextBox.Text = x.ToString();
            YTextBox.Text = y.ToString();

            DivisorTextBox.Text = divisor.ToString();
            OffsetTextBox.Text = offset.ToString();

            RowsComboBox.ItemsSource = values;
            ColumnsComboBox.ItemsSource = values;

            _rows = rows;
            _columns = columns;
            RowsComboBox.SelectedItem = _rows;
            ColumnsComboBox.SelectedItem = _columns;
        }
        private void RowsColumnsChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RowsComboBox.SelectedItem != null && ColumnsComboBox.SelectedItem != null && finished)
            {
                _rows = (int)RowsComboBox.SelectedItem;
                _columns = (int)ColumnsComboBox.SelectedItem;
                if (_mainWindow.CustomConvolutionFilter != null)
                    UpdateGridVisibility(_mainWindow.CustomConvolutionFilter.Kernel);
                else
                    UpdateGridVisibility();
                UpdateXYCoordinates();
                if (_gridCells[int.Parse(YTextBox.Text), int.Parse(XTextBox.Text)] != null)
                    _gridCells[int.Parse(YTextBox.Text), int.Parse(XTextBox.Text)].Background = Brushes.Wheat;
            }
        }

        private void UpdateXYCoordinates()
        {
            int centerX = _columns / 2;
            int centerY = _rows / 2;

            XTextBox.Text = centerX.ToString();
            YTextBox.Text = centerY.ToString();
        }

        private void CreateGrid(double[,]? input = default)
        {
            if (input == null)
                input = defaultKernel;

            DataGrid.Children.Clear();
            DataGrid.RowDefinitions.Clear();
            DataGrid.ColumnDefinitions.Clear();

            for (int i = 0; i < 9; i++)
            {
                DataGrid.RowDefinitions.Add(new RowDefinition());
                DataGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    TextBox cell = new TextBox
                    {
                        Width = 30,
                        Height = 30,
                        TextAlignment = TextAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        IsEnabled = false,
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        Background = Brushes.White,
                        BorderBrush = Brushes.Black,
                        Margin = new Thickness(0),
                        Text = (i < input.GetLength(0) && j < input.GetLength(1)) ? input[i, j].ToString() : ""
                    };
                    cell.TextChanged += GridCellTextChanged;
                    cell.LostFocus += GridCellLostFocus;
                    Grid.SetRow(cell, i);
                    Grid.SetColumn(cell, j);
                    DataGrid.Children.Add(cell);
                    _gridCells[i, j] = cell;
                }
            }

            UpdateGridVisibility(input);
        }

        private void GridCellLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox cell)
            {
                if (!double.TryParse(cell.Text, out _))
                    cell.Text = "0";
            }
            else
                throw new Exception("sender was not TextBox type???");
        }

        private void UpdateGridVisibility(double[,]? input = null)
        {
            if (input == null)
                input = defaultKernel;
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (_gridCells[i, j] != null)
                    {
                        _gridCells[i, j].IsEnabled = (i < _rows && j < _columns);
                        if (_gridCells[i, j].IsEnabled)
                        {
                            _gridCells[i, j].Text = _gridCells[i, j].Text == "" ? (i < input.GetLength(0) && j < input.GetLength(1) ? input[i, j].ToString() : "0" ) : _gridCells[i, j].Text;
                            _gridCells[i, j].Background = Brushes.White;
                        }
                        else
                        {
                            _gridCells[i, j].Text = "";
                            _gridCells[i, j].Background = Brushes.DarkGray;
                        }
                    }
                }
            }
        }


        private void GridCellTextChanged(object sender, TextChangedEventArgs e)
        {
            if (AutoComputeCheckbox.IsChecked == true)
            {
                ComputeDivisor();
            }
        }

        private void AutoComputeChecked(object sender, RoutedEventArgs e)
        {
            DivisorTextBox.Background = Brushes.White;
            if (AutoComputeCheckbox.IsChecked == true)
            {
                DivisorTextBox.IsEnabled = false;
                ComputeDivisor();
            }
            else
            {
                DivisorTextBox.IsEnabled = true;
            }
        }

        private void ComputeDivisor()
        {
            double sum = 0;
            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _columns; j++)
                {
                    if (double.TryParse(_gridCells[i, j].Text, out double value))
                    {
                        sum += value;
                    }
                }
            }
            if (sum == 0)
            {
                sum = 1;
                DivisorTextBox.Background = Brushes.PaleVioletRed;
                WarningIcon.Visibility = Visibility.Visible;
            }
            else
            {
                DivisorTextBox.Background = Brushes.White;
                WarningIcon.Visibility = Visibility.Collapsed;
            }
            DivisorTextBox.Text = sum.ToString("F2");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            ValidateDivisor(DivisorTextBox, null!);

            double divisor, offset, x, y;
            if (!double.TryParse(DivisorTextBox.Text, out divisor) ||
        !double.TryParse(OffsetTextBox.Text, out offset) ||
        !double.TryParse(XTextBox.Text, out x) ||
        !double.TryParse(YTextBox.Text, out y))
            {
                MessageBox.Show("Please enter valid numbers.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Create action with:\nDivisor: {divisor}\nOffset: {offset}\nX: {x}\nY: {y}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

            double[,] kernel = new double[_rows, _columns];
            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _columns; j++)
                {
                    var cell = _gridCells[i, j];
                    if (double.TryParse(cell.Text, out double value))
                        kernel[i, j] = value;
                    else
                    {
                        cell.Text = "0";
                        kernel[i, j] = 0;
                    }
                }
            }

            _mainWindow.CustomConvolutionFilter = new ConvolutionFilter(kernel, divisor, new Point(x - _columns / 2, y - _rows / 2), offset);
            Close();
        }

        private void ValidateDivisor(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(DivisorTextBox.Text, out double value))
            {
                if (value == 0)
                {
                    MessageBox.Show("Divisor cannot be 0. Setting to 1.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DivisorTextBox.Text = "1";
                }
            }
            else
            {
                MessageBox.Show("Invalid divisor number entered. Setting to 1.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DivisorTextBox.Text = "1";
            }
        }
    }
}
