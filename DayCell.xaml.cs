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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TimelineVisualizer
{
    /// <summary>
    /// Interaction logic for DayCell.xaml
    /// </summary>
    public partial class DayCell : UserControl
    {
        private DateTime date;
        public DayCell(DateTime date)
        {
            InitializeComponent();

            this.date = date;
            if (date.Day == 1)
            {
                DayLabel.Content = $"{date.Day}/{date.Month}";
            }
            else
            {
                DayLabel.Content = date.Day;
            }
        }
    }
}
