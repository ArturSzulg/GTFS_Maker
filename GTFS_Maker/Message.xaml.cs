using GTFS_Maker;
using System.Windows;
using System.Windows.Input;

namespace Interfejs
{
    public partial class Message : Window
    {
        private MainWindow mainWindowHandler;

        public Message(MainWindow mWindowHandler, string subject, string text)
        {
            mainWindowHandler = mWindowHandler;
            InitializeComponent();
            mainWindowHandler.BlockMainWindow(true);
            CustomizeDialogBox(subject, text);
        }

        private void CustomizeDialogBox(string subject, string text)
        {
            MessageSubject.Text = subject;
            MessageText.Text = text;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindowHandler.BlockMainWindow(false);
            Close();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
    }
}
