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

namespace TimelineVisualizer
{
    /// <summary>
    /// Interaction logic for PlacesEditor.xaml
    /// </summary>
    public partial class PlacesEditor : Window
    {
        public PlacesEditor()
        {
            InitializeComponent();

            PlacesDataGrid.ItemsSource = TimelineDB.GetPlaces();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            using var db = TimelineDB.GetConnection();
            db.DeleteAll<Place>();
            foreach (var place in PlacesDataGrid.Items.OfType<Place>())
            {
                if (string.IsNullOrWhiteSpace(place.Name))
                {
                    continue;
                }
                db.Insert(place);
            }
            Close();
        }
    }
}
