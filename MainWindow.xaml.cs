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
        /// <summary>
        /// The date that appears in the CalendarGrid's first (top-left) cell.
        /// </summary>
        public DateTime TopLeftDate;

        private List<DayCell> dayCells = new();

        private static DateTime FirstDayOfWeek(DateTime date)
        {
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            return firstDayOfMonth.AddDays(-((7 + (firstDayOfMonth.DayOfWeek - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek)) % 7));
        }

        public MainWindow()
        {
            var dateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat;

            InitializeComponent();

            DatePicker.SelectedDate = DateTime.Now;

            var weekdays = dateTimeFormat.AbbreviatedDayNames;
            for (int i = 0; i < weekdays.Length; i++)
            {
                var label = new Label()
                {
                    Content = weekdays[i],
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                CalendarGrid.Children.Add(label);
                label.SetValue(Grid.ColumnProperty, i);
            }

            // Initially, the TopLeftDate will be set so that the entire current month will be visible.
            TopLeftDate = FirstDayOfWeek(DateTime.Today);

            GenerateDayCells();
        }

        private void GenerateDayCells()
        {
            foreach (var cell in dayCells)
            {
                CalendarGrid.Children.Remove(cell);
            }
            for (int week = 0; week < 6; week++)
            {
                for (int weekday = 0; weekday < 7; weekday++)
                {
                    var cell = new DayCell(TopLeftDate.AddDays(week * 7 + weekday));
                    CalendarGrid.Children.Add(cell);
                    cell.SetValue(Grid.RowProperty, week + 1);
                    cell.SetValue(Grid.ColumnProperty, weekday);
                    dayCells.Add(cell);
                }
            }
        }

        private void SetTopLeftDate(DateTime date)
        {
            if (TopLeftDate == date)
            {
                return;
            }
            TopLeftDate = date;
            GenerateDayCells();
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

        private void PrevWeekButton_Click(object sender, RoutedEventArgs e)
        {
            SetTopLeftDate(TopLeftDate.AddDays(-7));
        }

        private void NextWeekButton_Click(object sender, RoutedEventArgs e)
        {
            SetTopLeftDate(TopLeftDate.AddDays(7));
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePicker.SelectedDate is DateTime date)
            {
                SetTopLeftDate(FirstDayOfWeek(new DateTime(date.Year, date.Month, 1)));
            }
        }
    }
}
