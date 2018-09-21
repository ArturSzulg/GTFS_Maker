using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace GTFS_Maker
{
    /// <summary>
    /// Interaction logic for WaitingWindow.xaml
    /// </summary>
    public partial class WaitingWindow : Window
    {
        private MainWindow actualWindow;
        public WaitingWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            actualWindow = mainWindow;
            actualWindow.BlockMainWindow(true);
            int counter = 0;
           
            DispatcherTimer timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 200), DispatcherPriority.Normal, delegate
            {
                counter++;
                LightUp(counter);
                if (counter == 3) counter = 0;
            }, Dispatcher);
        }

        private void LightUp(int withOne)
        {
            // LightBlue = one
            // LightSkyBlue = two

            switch (withOne)
            {
                case 1:
                    R1.Fill = System.Windows.Media.Brushes.LightBlue;
                    R2.Fill = System.Windows.Media.Brushes.LightSkyBlue;
                    R3.Fill = System.Windows.Media.Brushes.LightSkyBlue;
                    break;
                case 2:
                    R1.Fill = System.Windows.Media.Brushes.LightSkyBlue;
                    R2.Fill = System.Windows.Media.Brushes.LightBlue;
                    R3.Fill = System.Windows.Media.Brushes.LightSkyBlue;
                    break;
                case 3:
                    R1.Fill = System.Windows.Media.Brushes.LightSkyBlue;
                    R2.Fill = System.Windows.Media.Brushes.LightSkyBlue;
                    R3.Fill = System.Windows.Media.Brushes.LightBlue;
                    break;
                default:
                    break;
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
