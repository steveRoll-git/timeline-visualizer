using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace TimelineVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            DatePicker.SelectedDate = DateTime.Now;

            var weekdays = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
            for(int i = 0; i < weekdays.Length; i++)
            {
                var label = new Label()
                {
                    Content = weekdays[i],
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                CalendarGrid.Children.Add(label);
                label.SetValue(Grid.ColumnProperty, i);
            }
        }

        private void LoadJSONMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Load timeline data from a JSON file",
                Filter = "JSON files|*.json"
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                string filename = dialog.FileName;
                var loadWindow = new LoadWindow(filename)
                {
                    Owner = this
                };
                loadWindow.StartLoading();
                loadWindow.ShowDialog();
            }
        }
    }
}
