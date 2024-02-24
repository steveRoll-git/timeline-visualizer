using System.Windows;

namespace TimelineVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LoadWindow : Window
    {
        public readonly string filename;
        private readonly CancellationTokenSource cancellationTokenSource;

        public LoadWindow(string filename)
        {
            InitializeComponent();

            this.filename = filename;
            cancellationTokenSource = new CancellationTokenSource();

            TimelineDB.LoadFinished += TimelineDB_LoadFinished;
        }

        public void StartLoading()
        {
            Progress<double> progress = new();

            LoadProgressGrid.Visibility = Visibility.Visible;

            progress.ProgressChanged += LoadProgress_ProgressChanged;

            LoadProgress_ProgressChanged(null, 0);

            Task.Run(() =>
            {
                TimelineDB.LoadFromJSON(filename, progress, cancellationTokenSource.Token);
            });
        }

        private void TimelineDB_LoadFinished(int numRecords, DateTime from, DateTime to)
        {
            Dispatcher.BeginInvoke(() =>
            {
                LoadFinishTextBlock.DataContext = new { NumRecords = numRecords, From = from.ToShortDateString(), To = to.ToShortDateString() };
                LoadFinishTextBlock.Visibility = Visibility.Visible;
                LoadProgressGrid.Visibility = Visibility.Collapsed;
                CancelButton.Content = "Close";
            });
        }

        private void LoadProgress_ProgressChanged(object? sender, double e)
        {
            LoadProgressBar.Value = e * 100;
            LoadProgressLabel.DataContext = Math.Round(e * 1000) / 10;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }
    }
}