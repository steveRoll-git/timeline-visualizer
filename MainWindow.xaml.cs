﻿using System.Globalization;
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

        private List<DayCell> dayCells = [];

        private bool datePickerFirstSet = false;

        private DateTime DatePickerDate => new(int.Parse(YearComboBox.Text), MonthComboBox.SelectedIndex + 1, DayComboBox.SelectedIndex + 1);

        private static DateTime FirstDayOfWeek(DateTime date)
        {
            return date.AddDays(-((7 + (date.DayOfWeek - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek)) % 7));
        }

        public MainWindow()
        {
            var dateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat;

            InitializeComponent();

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

            using var db = TimelineDB.GetConnection();
            var minDate = new DateTime(db.QueryScalars<long>("SELECT MIN(timestamp) from timeline;")[0]);
            var maxDate = new DateTime(db.QueryScalars<long>("SELECT MAX(timestamp) from timeline;")[0]);
            YearComboBox.ItemsSource = Enumerable.Range(minDate.Year, maxDate.Year - minDate.Year + 1);
            MonthComboBox.ItemsSource = dateTimeFormat.MonthNames.Where(m => m != "");

            for (int week = 0; week < 6; week++)
            {
                for (int weekday = 0; weekday < 7; weekday++)
                {
                    var cell = new DayCell();
                    CalendarGrid.Children.Add(cell);
                    cell.SetValue(Grid.RowProperty, week + 1);
                    cell.SetValue(Grid.ColumnProperty, weekday);
                    dayCells.Add(cell);
                }
            }

            // Initially, the TopLeftDate will be set so that the entire current month will be visible.
            SetTopLeftDate(FirstDayOfWeek(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)));
        }

        private void UpdateDayCells()
        {
            for (int week = 0; week < 6; week++)
            {
                for (int weekday = 0; weekday < 7; weekday++)
                {
                    var cell = dayCells[week * 7 + weekday];
                    cell.SetDate(TopLeftDate.AddDays(week * 7 + weekday), week == 0 && weekday == 0);
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
            DateLabel.Content = date.ToShortDateString();
            UpdateDayCells();
            if (datePickerFirstSet && Math.Abs((DatePickerDate - date).Days) >= 21)
            {
                datePickerFirstSet = false;
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

        private void PrevWeekButton_Click(object sender, RoutedEventArgs e)
        {
            SetTopLeftDate(TopLeftDate.AddDays(-7));
        }

        private void NextWeekButton_Click(object sender, RoutedEventArgs e)
        {
            SetTopLeftDate(TopLeftDate.AddDays(7));
        }

        private void EditPlacesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new PlacesEditor() { Owner = this }.ShowDialog();
        }

        private void DateLabel_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PreviewDatePanel.Visibility = Visibility.Collapsed;
            DatePicker.Visibility = Visibility.Visible;
            if (!datePickerFirstSet)
            {
                datePickerFirstSet = true;
                YearComboBox.Text = TopLeftDate.Year.ToString();
                MonthComboBox.SelectedIndex = TopLeftDate.Month - 1;
                DayComboBox.SelectedIndex = TopLeftDate.Day - 1;
            }
        }

        private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DayComboBox.ItemsSource = Enumerable.Range(1, DateTime.DaysInMonth(int.Parse(YearComboBox.Text), MonthComboBox.SelectedIndex + 1));
        }

        private void SetDateButton_Click(object sender, RoutedEventArgs e)
        {
            SetTopLeftDate(FirstDayOfWeek(DatePickerDate));
            PreviewDatePanel.Visibility = Visibility.Visible;
            DatePicker.Visibility = Visibility.Collapsed;
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            SetTopLeftDate(FirstDayOfWeek(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)));
        }

        private void CalendarGrid_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            SetTopLeftDate(TopLeftDate.AddDays(7 * -Math.Sign(e.Delta)));
        }
    }
}
