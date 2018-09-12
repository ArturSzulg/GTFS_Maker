using Microsoft.Win32;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GTFS_Maker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindow actualWindow;
        public string currentDirectory;
        public string stopsFilePath;
        public string stopsFileExtension;
        public string timetableFilePath;
        public string timetableFileExtension = "xslx";
        public string typeOfRoute;
        public Dictionary<string, string> servicesDictionary; //
        public Dictionary<string, string> routesDictionary;
        public List<string> noMatchServices;
        public Dictionary<string, string> routesSigns;
        public MainWindow()
        {
            actualWindow = this;
            InitializeComponent();
            currentDirectory = Directory.GetCurrentDirectory();
            servicesDictionary = new Dictionary<string, string> { };
            routesDictionary = new Dictionary<string, string> { };
            routesSigns = new Dictionary<string, string> { {"Tram","0" }, { "Metro", "1" }, { "Rail", "2" }, { "Bus", "3" } };
            noMatchServices = new List<string> { };
            ServicesListBox.Items.Clear();
            DispatcherTimer timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                CurentTimeTextBlock.Text = DateTime.Now.ToString("HH:mm");
                if (IsAgencyFilled()) ChooseStopsFile.IsEnabled = true;
                else ChooseStopsFile.IsEnabled = GenerateGTFS.IsEnabled = false;
            }, Dispatcher);
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\GTFS");
        }

        private void TopGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        
        public bool IsServicesDictioranyContainingKey(string askingKey)
        {
            return (servicesDictionary.ContainsKey(askingKey));
        }

        public bool IsServicesDictioranyContainingValue(string askingValue)
        {
            return (servicesDictionary.ContainsValue(askingValue));
        }

        public bool IsDictioranyContainingKey(Dictionary<string,string> dictionary, string askingKey)
        {
            return (dictionary.ContainsKey(askingKey));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) => this.Close();

        public void HideTextFromTextBox(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
            tb.GotFocus -= HideTextFromTextBox;
        }
        
        private bool IsEmpty(TextBox textBox)
        {
            return (textBox.Text.Length == 0);
        }

        private void ChooseStopsFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = currentDirectory;
            openFileDialog.Filter = "Pliki txt|*.txt|Pliki Excel|*.xlsx|TXT lub Excel|*.txt;*.xlsx";
            openFileDialog.FilterIndex = 3;
            openFileDialog.RestoreDirectory = true;
            Nullable<bool> dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == true)
            {
                StopsPath.Text = openFileDialog.FileName;
                stopsFilePath = openFileDialog.FileName;
                stopsFileExtension = openFileDialog.SafeFileName.Split('.')[1];
            }
            if (IsStopsFileAdded()) ChooseTimetableFile.IsEnabled = IsEnabled.Equals(true);
        }

        private void ChooseTimetableFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = currentDirectory;
            openFileDialog.Filter = "Pliki Excel|*.xlsx";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            Nullable<bool> dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == true)
            {
                TimetablePath.Text = openFileDialog.FileName;
                timetableFilePath = openFileDialog.FileName;
            }
            if (IsStopsFileAdded() && IsTimetableFileAdded())
            {
                bool AreStopsMatched = Program.CheckStopsMatching(actualWindow);
                ShowStopsMatching(AreStopsMatched);
                if (AreStopsMatched)
                {
                    ServicesListBox.Items.Clear();
                    ShowServicesMatching(Program.CheckServicesMatching(actualWindow));
                    ShowServices();
                }
            }
        }

        private void ShowServices()
        {
            foreach (var service in servicesDictionary)
            {
                ServicesListBox.Items.Add(service.Key + " = " + service.Value);
            }
        }

        private bool IsAgencyFilled()
        {
            return (CityName.Text != "" && CityName.Text != "Nazwa miasta" && Agency.Text != "" && Agency.Text != "Nazwa zarządcy" && Site.Text != "" && Site.Text != "Adres strony zarządcy");
        }

        private bool IsStopsFileAdded()
        {
            return (stopsFilePath != null);
        }

        private bool IsTimetableFileAdded()
        {
            return (timetableFilePath != null);
        }

        private void HelpXLSXButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("Struktura_Rozkładow.xlsx");
        }

        private void ShowStopsMatching(bool IsAllMatched)
        {
            if (IsAllMatched)
            {
                StopsMatchingFlag.Visibility = Visibility.Visible;
                StopsMatchingFlag.Background = Brushes.Green;
                AddNewService.IsEnabled = true;
                StopsMatchingFlag.Content = "Przystanki się pokrywają";
            }
            else
            {
                StopsMatchingFlag.Visibility = Visibility.Visible;
                StopsMatchingFlag.Background = Brushes.Red;
                AddNewService.IsEnabled = false;
                ChooseTimetableFile.IsEnabled = false;
                StopsMatchingFlag.Content = "Błąd naciśnij po informacje";
                StopsPath.Text = "Wybierz ponownie plik zawierający poprawione przystanki i współrzędne";
                TimetablePath.Text = "Wybierz ponownie poprawiony plik z rozkładami";
                stopsFilePath=stopsFileExtension=timetableFilePath=timetableFileExtension = null;
            }
        }

        private void ShowServicesMatching(bool IsAllMatched)
        {
            if (IsAllMatched)
            {
                // pozwala generować GTFSa
                Interfejs.Message successMessage = new Interfejs.Message(this, "Informacja", "Wszystko się pokrywa, możesz generować pliki GTFS");
                successMessage.Owner = this;
                successMessage.Show();
                successMessage.Topmost = true;
                GenerateGTFS.IsEnabled = true;
                AddNewService.IsEnabled = false;
            }
            else
            {
                GenerateGTFS.IsEnabled = false;
                AddNewService.IsEnabled = true;
                Interfejs.Message successMessage = new Interfejs.Message(this, "Niespójność", "Rodzaje kursów pobranych z poszczególnych arkuszów z rozkładu nie pokrywają się z tymi z arkusza 'Services'. Za chwilę uruchomi się plik z listą nieoznaczonych rodzajów kursów. Wprowadź ręcznie");
                successMessage.Owner = this;
                successMessage.Show();
                successMessage.Topmost = true;
                Task.Delay(3000);
                System.Diagnostics.Process.Start("services_NoMatch.txt"); // mb change because of autoinrement 
            }
        }

        private void StopsMatchingFlag_Click(object sender, RoutedEventArgs e)
        {
            if (StopsMatchingFlag.Background == Brushes.Red)
            {
                Interfejs.Message successMessage = new Interfejs.Message(this, "No to klops", "Pobrałem przystanki z pliku, oraz wszystkie z arkusza, niestety nie ma między nimi pełnej zgodności. Za chwilę urchomi się plik z listą niesparowanych przystanków. Sprawdz też zgodność z schematem");
                successMessage.Owner = this;
                successMessage.Show();
                successMessage.Topmost = true;
                Task.Delay(3000);
                System.Diagnostics.Process.Start("stops_NoMatch.txt"); // mb change because of autoinrement 
            }
            else
            {
                Interfejs.Message successMessage = new Interfejs.Message(this, "Poprawnie", "Przystanki w pełni się pokrywają, możesz kontynuować :)");
                successMessage.Owner = this;
                successMessage.Show();
                successMessage.Topmost = true;
            }
        }

        public void BlockMainWindow(bool youWantBlockIt)
        {
            if (youWantBlockIt)
            {
                //MainWindow actualWindow = this;
                //actualWindow.Topmost = true;
                actualWindow.IsEnabled = false;
                actualWindow.Opacity = 0.6;
            }
            else
            {
                //MainWindow actualWindow = this;
                //actualWindow.Topmost = false;
                actualWindow.IsEnabled = true;
                actualWindow.Opacity = 100;
            }

        }

        private void AddNewService_Click(object sender, RoutedEventArgs e)
        {
            if (!IsServicesDictioranyContainingKey(ServiceSymbol.Text) && !IsServicesDictioranyContainingValue(ServiceFullName.Text))
            {
                servicesDictionary.Add(ServiceSymbol.Text, ServiceFullName.Text);
                ServicesListBox.Items.Add(ServiceSymbol.Text + " = " + ServiceFullName.Text);
                if (noMatchServices.Contains(ServiceSymbol.Text))
                {
                    noMatchServices.Remove(ServiceSymbol.Text);
                    if(noMatchServices.Count == 0)
                    {
                        ShowServicesMatching(true);
                    }
                }
            }
            else
            {
                Interfejs.Message successMessage = new Interfejs.Message(this, "Potwórzenie", "Taki symbol, lub pełna nazwa juz została wprowadzona");
                successMessage.Owner = this;
                successMessage.Show();
                successMessage.Topmost = true;
            }
            // TO DO check services matching, but olny if Stops are good :)
            // delete old, or always make new - autoincrement
            //ShowServicesMatching(true);

        }

        private void ClearUI()
        {
            CityName.Text = "Nazwa miasta";
            Agency.Text = "Nazwa zarządcy";
            Site.Text = "Adres strony zarządcy";
            StopsPath.Text = "Plik zawierający przystanki i współrzędne w formacie xlsx lub txt";
            TimetablePath.Text = "Plik z ustrukturyzowanymi rozkładami jazdy - Więcej info w menu";
            ChooseStopsFile.IsEnabled = ChooseTimetableFile.IsEnabled = AddNewService.IsEnabled = GenerateGTFS.IsEnabled = false;
            StopsMatchingFlag.Visibility = Visibility.Hidden;
            stopsFilePath = stopsFileExtension = timetableFilePath = typeOfRoute = null;
            servicesDictionary.Clear();
            routesDictionary.Clear();
            noMatchServices.Clear();
            routesSigns.Clear();
            Rail.IsChecked = Metro.IsChecked = Bus.IsChecked = Tram.IsChecked = false;
            ServicesListBox.Items.Clear();
        }

        private void CheckBoxClicked(object sender, RoutedEventArgs e)
        {
            Tram.IsChecked = Bus.IsChecked = Metro.IsChecked = Rail.IsChecked = false;
            CheckBox checkBoxHandler = sender as CheckBox;
            checkBoxHandler.IsChecked = true;
            routesSigns.TryGetValue(checkBoxHandler.Name, out typeOfRoute);
            if (typeOfRoute == null)
            {
                Interfejs.Message successMessage = new Interfejs.Message(this, "Problem", "Błąd z rozpoznaniem rodzaju transportu");
                successMessage.Owner = this;
                successMessage.Show();
                successMessage.Topmost = true;
                GenerateGTFS.IsEnabled = true;
            }
        }

        private void GenerateGTFS_Click(object sender, RoutedEventArgs e)
        {
            Agency agency = new Agency(currentDirectory + @"\GTFS");
            Program.MakeAgencyTXT();
            Stop_time stopTime = new Stop_time(currentDirectory + @"\GTFS");
            Trip trip = new Trip(currentDirectory + @"\GTFS");
            if (Program.MakeTripsNStopTimes())
            {
                Interfejs.Message successMessage = new Interfejs.Message(this, "Gratuluję", "Udało się wytworzyć pliki GTFS. Znajdują się one w folderze GTFS, a jego lokalizacją jest folder zawierający plik z którego uruchomiony został ten program.");
                successMessage.Owner = this;
                successMessage.Show();
                successMessage.Topmost = true;
                ClearUI();
            }
            else
            {
                Interfejs.Message successMessage = new Interfejs.Message(this, "Błąd", "Nie udało się wytworzyć plików GTFS. Sprawdź zgodność Twoich plików z wymaganymi schematami. Zamknij pliki robocze z rozkładami i przystankami.");
                successMessage.Owner = this;
                successMessage.Show();
                successMessage.Topmost = true;
            }
        }

        
    }
}
