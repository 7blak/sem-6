using lab1.Filters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace lab1.Windows
{
    public partial class CustomFilterWindow : Window
    {
        private int _rows = 3;
        private int _columns = 3;
        private readonly TextBox[,] _gridCells = new TextBox[9, 9];
        private readonly ConvolutionFilter _originalFilter;
        private readonly ObservableCollection<ConvolutionFilter> _originalFilters;
        private ConvolutionFilter _currentFilter { get; set; }
        private bool _finished = false;

        public CustomFilterWindow(ObservableCollection<ConvolutionFilter> convolutionFilters)
        {
            InitializeComponent();
            _originalFilters = convolutionFilters;
            _currentFilter = convolutionFilters[^1];
            _originalFilter = _currentFilter;
            InitializeForm();
        }

        private void InitializeForm()
        {
            int[] values = [1, 3, 5, 7, 9];

            DivisorTextBox.Text = _currentFilter.Divisor.ToString();
            OffsetTextBox.Text = _currentFilter.Offset.ToString();

            NameTextBox.Text = _currentFilter.Name;

            RowsComboBox.ItemsSource = values;
            ColumnsComboBox.ItemsSource = values;

            if (!_finished)
            {
                PresetComboBox.ItemsSource = _originalFilters;
                PresetComboBox.SelectedIndex = _originalFilters.Count - 1;
            }

            _rows = _currentFilter.Kernel.GetLength(0);
            _columns = _currentFilter.Kernel.GetLength(1);

            XTextBox.Text = (_currentFilter.Anchor.X + _columns / 2).ToString();
            YTextBox.Text = (_currentFilter.Anchor.Y + _rows / 2).ToString();

            RowsComboBox.SelectedItem = _rows;
            ColumnsComboBox.SelectedItem = _columns;

            CreateGrid();
            _gridCells[(int)_currentFilter.Anchor.Y + _rows / 2, (int)_currentFilter.Anchor.X + _columns / 2].Background = Brushes.Wheat;
            _finished = true;
        }
        private void RowsColumnsChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RowsComboBox.SelectedItem != null && ColumnsComboBox.SelectedItem != null && _finished)
            {
                _rows = (int)RowsComboBox.SelectedItem;
                _columns = (int)ColumnsComboBox.SelectedItem;
                UpdateGridVisibility();
                UpdateXYCoordinates();
            }
        }

        private void UpdateXYCoordinates()
        {
            int centerX = _columns / 2;
            int centerY = _rows / 2;

            XTextBox.Text = centerX.ToString();
            YTextBox.Text = centerY.ToString();

            if (_gridCells[int.Parse(YTextBox.Text), int.Parse(XTextBox.Text)] != null)
                _gridCells[int.Parse(YTextBox.Text), int.Parse(XTextBox.Text)].Background = Brushes.Wheat;
        }

        private void CreateGrid()
        {

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
                    TextBox cell = new()
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
                        Text = (i < _currentFilter.Kernel.GetLength(0) && j < _currentFilter.Kernel.GetLength(1)) ? _currentFilter.Kernel[i, j].ToString() : ""
                    };
                    cell.TextChanged += GridCellTextChanged;
                    cell.LostFocus += GridCellLostFocus;
                    Grid.SetRow(cell, i);
                    Grid.SetColumn(cell, j);
                    DataGrid.Children.Add(cell);
                    _gridCells[i, j] = cell;
                }
            }

            UpdateGridVisibility();
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

        private void UpdateGridVisibility()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (_gridCells[i, j] != null)
                    {
                        _gridCells[i, j].IsEnabled = (i < _rows && j < _columns);
                        if (_gridCells[i, j].IsEnabled)
                        {
                            _gridCells[i, j].Text = _gridCells[i, j].Text == "" ? (i < _currentFilter.Kernel.GetLength(0) && j < _currentFilter.Kernel.GetLength(1) ? _currentFilter.Kernel[i, j].ToString() : "0") : _gridCells[i, j].Text;
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

        protected override void OnClosed(EventArgs e)
        {
            PresetComboBox.SelectionChanged -= PresetChanged;
            base.OnClosed(e);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            ValidateDivisor(DivisorTextBox, null!);

            if (!double.TryParse(DivisorTextBox.Text, out double divisor) ||
            !double.TryParse(OffsetTextBox.Text, out double offset) ||
            !double.TryParse(XTextBox.Text, out double x) ||
            !double.TryParse(YTextBox.Text, out double y))
            {
                MessageBox.Show("Please enter valid numbers.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Create action with:\nDivisor: {divisor}\nOffset: {offset}\nX: {x}\nY: {y}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

            PresetComboBox.SelectionChanged -= PresetChanged;

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

            var filterToReplace = _originalFilters.FirstOrDefault(filter => filter.Name == NameTextBox.Text);

            if (filterToReplace != null)
                _originalFilters[_originalFilters.IndexOf(filterToReplace)] = new ConvolutionFilter(NameTextBox.Text, kernel, divisor, new Point(x - _columns / 2, y - _rows / 2), offset);
            else
                _originalFilters.Add(new ConvolutionFilter(NameTextBox.Text, kernel, divisor, new Point(x - _columns / 2, y - _rows / 2), offset));
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

        private void PresetChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && _finished)
            {
                _currentFilter = (ConvolutionFilter)comboBox.SelectedItem;
                InitializeForm();
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _currentFilter = _originalFilter;
            InitializeForm();
        }

        private void AnchorChange(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(XTextBox.Text, out int x) || !int.TryParse(YTextBox.Text, out int y) || x < 0 || y < 0)
                return;
            UpdateGridVisibility();
            if (_finished && y < _rows && x < _columns && _gridCells[y, x] != null)
                _gridCells[y, x].Background = Brushes.Wheat;
        }

        private void AnchorTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(XTextBox.Text, out int x) && int.TryParse(YTextBox.Text, out int y) & x >= 0 & y >= 0)
            {
                XTextBox.Text = x >= _columns ? (_columns / 2).ToString() : x.ToString();
                YTextBox.Text = y >= _rows ? (_rows / 2).ToString() : y.ToString();
            }
            else
                UpdateXYCoordinates();
        }
    }
}
