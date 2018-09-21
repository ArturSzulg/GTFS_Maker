using Parser_GTFS;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace GTFS_Maker
{
    /// <summary>
    /// Interaction logic for NewCalendar.xaml
    /// </summary>
    public partial class NewCalendar : Window
    {
        private MainWindow mainWindowHandler;
        private static string savingPath = Directory.GetCurrentDirectory() + @"\GTFS";
        private static int noMatchService;

    public NewCalendar(MainWindow mWindowHandler,string service, int noMatchServicesValue)
        {
            mainWindowHandler = mWindowHandler;
            InitializeComponent();
            ServiceName.Text = service;
            noMatchService = noMatchServicesValue;
            mainWindowHandler.BlockMainWindow(true);
        }

        public void HideTextFromTextBox(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
            tb.GotFocus -= HideTextFromTextBox;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string service_id = ServiceName.Text;
            string start_date = StartDate.Text;
            string end_date = EndDate.Text;
            string monday = IsCheckedToBinaryString(Monday.IsChecked);
            string tuesday = IsCheckedToBinaryString(Tuesday.IsChecked);
            string wednesday = IsCheckedToBinaryString(Wednesday.IsChecked);
            string thursday = IsCheckedToBinaryString(Thursday.IsChecked);
            string friday = IsCheckedToBinaryString(Friday.IsChecked);
            string saturday = IsCheckedToBinaryString(Saturday.IsChecked);
            string sunday = IsCheckedToBinaryString(Sunday.IsChecked);
            Parser_GTFS.Calendar writeCalendar = new Parser_GTFS.Calendar(service_id, start_date, end_date, monday, tuesday, wednesday, thursday, friday, saturday, sunday, savingPath);
            if (noMatchService == 0)
            {
                mainWindowHandler.ShowServicesMatching(true);
            }
            Close();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private string IsCheckedToBinaryString(bool? isChecked)
        {
            if (isChecked == true) return "1";
            else return "0";
        }
    }
}
