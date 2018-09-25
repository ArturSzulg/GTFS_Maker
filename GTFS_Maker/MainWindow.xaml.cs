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
using System.IO.Compression;
using OfficeOpenXml;
using System.Threading;

namespace GTFS_Maker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow actualWindow;
        public string currentDirectory;
        public string stopsFilePath;
        public string stopsFileExtension;
        public string timetableFilePath;
        public string timetableFileExtension = "xslx";
        public string typeOfRoute;
        public bool isTypeServiceClicked;
        public Dictionary<string, string> servicesDictionary; //
        public Dictionary<string, string> routesDictionary;
        public List<string> noMatchServices;
        public Dictionary<string, string> routesSigns;
        public MainWindow()
        {
            Program.mainWindowHandler =actualWindow = this;
            InitializeComponent();
            currentDirectory = Directory.GetCurrentDirectory();
            servicesDictionary = new Dictionary<string, string> { };
            routesDictionary = new Dictionary<string, string> { };
            routesSigns = new Dictionary<string, string> {
                                                           { "Tram","0" }, { "Metro", "1" }, { "Rail", "2" }, { "Bus", "3" },{"Prom","4"}
                                                           ,{"Pociąg wysokiej prędkości","101"},{"Pociąg dalekobieżny","102"},{"Pociąg międzyregionalny","103"},{"Pociąg do transportu samochodów","104"},{"Pociąg sypialny","105"},{"Pociąg regionalny","106"},{"Pociąg turystyczny","107"},{"Pociąg wahadłowy","108"},{"Pociąg podmiejski","109"},{"Trolejbus","800"},{"Statek","1000"},{"Samolot","1100"}
                                                           ,{"Kolej miejska","400"},{"Kolej podziemna","402"},{"Kolej linowa","5"},{"Kolej gondolowa","6"},{"Kolej zębata","7"},{"Taxi","1500"},{"Taxi grupowe","1501"},{"Taxi wodne","1502"},{"Różne","1700"}
                                                         };
            noMatchServices = new List<string> { };
            ServicesListBox.Items.Clear();
            isTypeServiceClicked = false;
            DispatcherTimer timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 200), DispatcherPriority.Normal, delegate
            {
                CurentTimeTextBlock.Text = DateTime.Now.ToString("HH:mm");
                if (IsAgencyFilled() && isTypeServiceClicked) ChooseStopsFile.IsEnabled = true;
                else ChooseStopsFile.IsEnabled = GenerateGTFS.IsEnabled = false;
            }, Dispatcher);
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\GTFS");
            for (int i = 4; i < routesSigns.Count; i++)
            {
                OtherTypes.Items.Add(routesSigns.Keys.ElementAt(i));
            }
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
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
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
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
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
                if (Program.TestFormulasInTimeTable())
                { 
                    try
                    {
                        bool AreStopsMatched = Program.CheckStopsMatching(actualWindow);
                        ShowStopsMatching(AreStopsMatched);
                        if (AreStopsMatched )
                        {
                            ServicesListBox.Items.Clear();
                            ShowServicesMatching(Program.CheckServicesMatching(actualWindow));//////////////////////////////////////////////////////////////////////////
                            ShowServices();
                        }
                    }
                    catch
                    {
                        Interfejs.Message successMessage = new Interfejs.Message(this, "Błąd", "Sprawdź zgodność wybranych plików z wymaganą strukturą");
                        successMessage.Owner = this;
                        successMessage.Show();
                        successMessage.Topmost = true;
                        ClearUI();
                    }
                }
                //else
                //{
                //    Interfejs.Message successMessage = new Interfejs.Message(this, "Uwaga", "Wybrany rozkład zawiera komórki które mają błędne formatowanie. Zmień formuły zawierające czasy na typ 'GG:MM'");
                //    successMessage.Owner = this;
                //    successMessage.Show();
                //    successMessage.Topmost = true;
                //    ClearUI(false);
                //}
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
            System.Diagnostics.Process.Start(currentDirectory + "//Schemas//Struktura_Rozkładow.xlsx");
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

        public void ShowServicesMatching(bool IsAllMatched)
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
                Interfejs.Message successMessage = new Interfejs.Message(this, "Ostrzeżenie", "Pobrałem przystanki z pliku, oraz wszystkie z arkusza, niestety nie ma między nimi pełnej zgodności. Za chwilę urchomi się plik z listą niesparowanych przystanków. Sprawdz też zgodność z schematem");
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
                    NewCalendar newCalendar = new NewCalendar(actualWindow, ServiceFullName.Text, noMatchServices.Count);
                    newCalendar.Owner = this;
                    newCalendar.Show();
                    newCalendar.Topmost = true;
                }
            }
            else
            {
                Interfejs.Message successMessage = new Interfejs.Message(this, "Potwórzenie", "Taki symbol, lub pełna nazwa juz została wprowadzona");
                successMessage.Owner = this;
                successMessage.Show();
                successMessage.Topmost = true;
            }
        }

        public void ClearUI(bool WithAgency = true)
        {
            if (WithAgency)
            { 
                CityName.Text = "Nazwa miasta";
                Agency.Text = "Nazwa zarządcy";
                Site.Text = "Adres strony zarządcy";
            }
            isTypeServiceClicked = false;
            StopsPath.Text = "Plik zawierający przystanki i współrzędne w formacie xlsx lub txt";
            TimetablePath.Text = "Plik z ustrukturyzowanymi rozkładami jazdy - Więcej info w menu";
            ChooseStopsFile.IsEnabled = !WithAgency;
            ChooseTimetableFile.IsEnabled = AddNewService.IsEnabled = GenerateGTFS.IsEnabled = false;
            StopsMatchingFlag.Visibility = Visibility.Hidden;
            stopsFilePath = stopsFileExtension = timetableFilePath = typeOfRoute = null;
            servicesDictionary.Clear();
            routesDictionary.Clear();
            Rail.IsChecked = Metro.IsChecked = Bus.IsChecked = Tram.IsChecked = false;
            ServicesListBox.Items.Clear();
        }

        private void CheckBoxClicked(object sender, RoutedEventArgs e)
        {
            Tram.IsChecked = Bus.IsChecked = Metro.IsChecked = Rail.IsChecked = Other.IsChecked = false;
            CheckBox checkBoxHandler = sender as CheckBox;
            checkBoxHandler.IsChecked = true;
            string selectedTypeOfRouteName = checkBoxHandler.Name;
            if (checkBoxHandler.Name == "Other") selectedTypeOfRouteName = OtherTypes.SelectedValue.ToString();
            routesSigns.TryGetValue(selectedTypeOfRouteName, out typeOfRoute);
            isTypeServiceClicked = true;
            if (typeOfRoute == null)
            {
                Tram.IsChecked = Bus.IsChecked = Metro.IsChecked = Rail.IsChecked = Other.IsChecked = false;
                Interfejs.Message successMessage = new Interfejs.Message(this, "Problem", "Błąd z rozpoznaniem rodzaju transportu");
                successMessage.Owner = this;
                successMessage.Show();
                successMessage.Topmost = true;
                GenerateGTFS.IsEnabled = true;
                isTypeServiceClicked = false;
            }
        }

        private async void GenerateGTFS_Click(object sender, RoutedEventArgs e)
        {
            Agency agency = new Agency(currentDirectory + @"\GTFS");
            Program.MakeAgencyTXT();
            Stop_time stopTime = new Stop_time(currentDirectory + @"\GTFS");
            Trip trip = new Trip(currentDirectory + @"\GTFS");



            WaitingWindow waitingWindow = new WaitingWindow(actualWindow);
            waitingWindow.Owner = this;
            waitingWindow.Show();
            bool response = await Program.MakeAsyncTripsnStopTimes();
            BlockMainWindow(false);
            waitingWindow.Close();

            if (response)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Pliki ZIP|*.zip";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string year = DateTime.Now.Date.ToString().Remove(5);
                string month = DateTime.Now.Date.ToString().Remove(8).Remove(0, 5);
                string day= DateTime.Now.Date.ToString().Remove(10).Remove(0,8);
                saveFileDialog.FileName = CityName.Text.ToLower().Replace(' ','-') + "-" + year + month + day + ".zip";
                saveFileDialog.DefaultExt = "zip";
                Nullable<bool> dialogResult = saveFileDialog.ShowDialog();
                if (dialogResult == true)
                {
                    try
                    {
                        if (File.Exists(saveFileDialog.FileName))
                        {
                            File.Delete(saveFileDialog.FileName);
                        }
                        ZipFile.CreateFromDirectory(currentDirectory + @"\GTFS", (saveFileDialog.FileName));
                        Interfejs.Message successMessage = new Interfejs.Message(this, "Gratuluję", "Udało się pomyślnie wytworzyć pliki GTFS oraz spakować je do ZIP-a.");
                        successMessage.Owner = this;
                        successMessage.Show();
                        successMessage.Topmost = true;
                        ClearUI();

                        string path = currentDirectory + @"\validation.bat";
                        if (File.Exists(path)) File.Delete(path);//del existing one
                        using (FileStream fs = File.Create(path))// now create new
                        {
                            Byte[] text = new UTF8Encoding(true).GetBytes(@"start """" /B fv.exe """ + saveFileDialog.FileName + @"""");
                            fs.Write(text, 0, text.Length);
                        }
                        System.Diagnostics.Process.Start("validation.bat");


                    }
                    catch
                    {
                        Interfejs.Message successMessage = new Interfejs.Message(this, "Błąd #02", "Nie udało się wytworzyć plików GTFS. Sprawdź zgodność Twoich plików z wymaganymi schematami. Zamknij pliki robocze z rozkładami i przystankami.");
                        successMessage.Owner = this;
                        successMessage.Show();
                        successMessage.Topmost = true;
                        ClearUI(false);
                    }

                }
            }
            else
            {
                Interfejs.Message successMessage = new Interfejs.Message(this, "Błąd #01", "Nie udało się wytworzyć plików GTFS. Sprawdź zgodność Twoich plików z wymaganymi schematami. Zamknij pliki robocze z rozkładami i przystankami.");
                successMessage.Owner = this;
                successMessage.Show();
                successMessage.Topmost = true;
                ClearUI(false);
            }

        }

        private void HelpStopsButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start( currentDirectory +"//Schemas//Struktura_Przystanków.xlsx");
        }

        private void GenerateStopsList_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog.Title = "Wybierz plik z którego mam pobrać przystanki";
            openFileDialog.Filter = "Pliki Excel|*.xlsx";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            Nullable<bool> dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == true)
            {
                try
                {
                    Program.MakeStopsListFile(openFileDialog.FileName);
                    Interfejs.Message successMessage = new Interfejs.Message(this, "Gratuluje", "Plik z listą przystanków został utworzony");
                    successMessage.Owner = this;
                    successMessage.Show();
                    successMessage.Topmost = true;
                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(GhostCellsException))
                    {
                        Interfejs.Message successMessage = new Interfejs.Message(this, "Błąd #03", "Najprawopodobniej wybrany plik nie jest zgodny z wymaganą strukturą");
                        successMessage.Owner = this;
                        successMessage.Show();
                        successMessage.Topmost = true;
                    }
                }
            }
        }

        private void GenerateServicesList_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog.Title = "Wybierz plik z którego mam pobrać typy serwisów";
            openFileDialog.Filter = "Pliki Excel|*.xlsx";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            Nullable<bool> dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == true)
            {
                try
                {
                    Program.MakeServicesListFile(openFileDialog.FileName);
                    Interfejs.Message successMessage = new Interfejs.Message(this, "Gratuluje", "Plik z listą typów serwisów został utworzony");
                    successMessage.Owner = this;
                    successMessage.Show();
                    successMessage.Topmost = true;
                }
                catch (Exception ex)
                {
                    if (ex.GetType() != typeof(GhostCellsException))
                    {
                        Interfejs.Message successMessage = new Interfejs.Message(this, "Błąd #04", "Najprawopodobniej wybrany plik nie jest zgodny z wymaganą strukturą");
                        successMessage.Owner = this;
                        successMessage.Show();
                        successMessage.Topmost = true;
                    }

                }
            }
        }

        private void RunValidator_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog.Filter = "Pliki ZIP|*.zip";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Title = "Wybierz archiwum zip które chcesz poddać walidacji";
            openFileDialog.RestoreDirectory = true;
            Nullable<bool> dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == true)
            {
                string path = currentDirectory + @"\validation.bat";
                if (File.Exists(path)) File.Delete(path);//del existing one
                using (FileStream fs = File.Create(path))// now create new
                {
                    Byte[] text = new UTF8Encoding(true).GetBytes(@"start """" /B fv.exe """ + openFileDialog.FileName + @"""");
                    fs.Write(text, 0, text.Length);
                }
                System.Diagnostics.Process.Start("validation.bat");
            }
        }
    }
}
//start "" /B fv.exe "C:\Users\Tunio\Desktop\GTFSowe\Nauka PKM\test-2018-09-17.zip"