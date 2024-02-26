using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TimelineVisualizer
{
    /// <summary>
    /// Interaction logic for DayCell.xaml
    /// </summary>
    public partial class DayCell : UserControl
    {
        private DateTime date;

        private static Color KnownColorToColor(System.Drawing.KnownColor color)
        {
            var argb = System.Drawing.Color.FromKnownColor(color).ToArgb();
            return Color.FromArgb((byte)(argb >> 0x18), (byte)(argb >> 0x10), (byte)(argb >> 0x8), (byte)argb);
        }

        public DayCell(DateTime date)
        {
            InitializeComponent();

            this.date = date;
            DayLabel.Content = $"{date.Day}/{date.Month}";

            var sections = TimelineDB.GetPlaceDaySections(date);
            foreach (var section in sections)
            {
                PlacesGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(section.EndTime - section.StartTime, GridUnitType.Star)
                });
                if (section.Place != null)
                {
                    var rect = new Rectangle
                    {
                        Fill = new SolidColorBrush(KnownColorToColor(section.Place.Color)),
                    };
                    PlacesGrid.Children.Add(rect);
                    rect.SetValue(Grid.RowProperty, PlacesGrid.RowDefinitions.Count - 1);
                }
            }
        }
    }
}
