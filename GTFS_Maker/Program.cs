using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExcelNumberFormat;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using GTFS_Maker;

namespace Parser_GTFS
{
    class Program
    {
        private static MainWindow mainWindowHandler;

        private static string savingPath = Directory.GetCurrentDirectory() + @"\GTFS";

        private static string NormalizeStopsName(string stopName)
        {
            stopName = DelHiddenCharsFromString(stopName);
            string normalizedName = null;
            //bool spaceRecently = false;
            //bool dotRecently = false;
            //bool slashRecently = false;
            //bool dashRecently = false;
            char recentSign = 'S'; // starting Val
            bool signRecently = false; // " " "." "/" "-" "\"

            for (int i = 0; i < stopName.Length; i++)
            {
                char stopsSign = stopName[i];
                if (i == 0)
                {
                    if (char.IsLetter(stopsSign) || char.IsNumber(stopsSign))
                    {
                        normalizedName += char.ToUpper(stopsSign);
                    }
                    //else
                    //{
                    //    normalizedName += "Wrong stop starting name!";
                    //}
                }
                //else if (i == (stopName.Length - 1))
                //{
                //    if (char.IsLetter(stopsSign) || char.IsNumber(stopsSign))
                //    {
                //        if (stopsSign == 'i' && (signRecently || stopName[i - 1] == 'I')) normalizedName += char.ToUpper(stopsSign);
                //        //else if (stopsSign == 'i' && stopName[i-1] == 'I')
                //        else normalizedName += stopsSign;
                //    }
                //}
                else
                {
                    if (char.IsLetter(stopsSign) || char.IsNumber(stopsSign))
                    {
                        //if (spaceRecently || dotRecently || slashRecently || dashRecently) { normalizedName += char.ToUpper(stopsSign); }
                        if (signRecently) { normalizedName += char.ToUpper(stopsSign); }
                        else { normalizedName += stopsSign; }
                        //spaceRecently = dotRecently = slashRecently = dashRecently = false; 
                        signRecently = false;
                    }
                    else if (stopsSign == ' ' || stopsSign == '.' || stopsSign == '/' || stopsSign == '-')
                    {
                        //if (!spaceRecently && !dotRecently && !slashRecently && !dashRecently)
                        if (!signRecently && (i != (stopName.Length - 1)))
                        {
                            normalizedName += stopsSign;
                            recentSign = stopsSign;
                            signRecently = true;
                        }
                        else if (recentSign == ' ' && stopsSign != ' ') // teoretycznie zbędne signRecently ale just in case
                        {
                            normalizedName = normalizedName.Remove(normalizedName.Length - 1);
                            normalizedName += stopsSign;
                            recentSign = stopsSign;
                        }
                    }
                }
            }
            return normalizedName;
        }

        private static void AddUniqeStopToList(List<string> uniqeStops, string stopName)
        {
            if (!uniqeStops.Contains(stopName))
            {
                uniqeStops.Add(stopName);
            }
        }

        private static void WriteStopToFile(string path, string stopName, string Lat, string Lon)
        {
            if (!File.Exists(path))
            {
                using (FileStream fs = new FileStream(path, FileMode.CreateNew))
                {
                    Byte[] text = new UTF8Encoding(true).GetBytes("Stop name:,Lat,Lon" + Environment.NewLine + stopName + "," + Environment.NewLine);
                    fs.Write(text, 0, text.Length);
                }
            }
            else
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true))
                {
                    string text = (stopName + "," + Lat + "," +Lon);
                    sw.WriteLine(text);
                }
            }
        }

        private static string DelHiddenCharsFromString(string text)
        {
            string goodText = null;
            for (int i = 0; i < text.Length; i++)
            {
                char tmp = text[i];
                if (char.IsLetter(tmp) || tmp == ' ' || tmp == '.' || tmp == '-' || tmp == '/' || tmp == '&' || char.IsNumber(tmp))
                {
                    goodText += tmp;
                }
                if (tmp == '&' || tmp == '\\' || tmp == ',')
                {
                    goodText += " ";
                }
            }
            return goodText;
        }

        private static string GetStopIDFromName(string stopName)
        {
            using (var reader = new StreamReader(@"C:\Users\Tunio\Desktop\Myszków\GTFS\stops.txt"))
            {
                List<string> IDs = new List<string>();
                List<string> Names = new List<string>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    IDs.Add(values[0]);
                    Names.Add(values[2]);
                }
                string stopID = null;
                for (int totalRows = 1; totalRows < IDs.Count; totalRows++)
                {
                    string name = Names[totalRows].ToString().Replace("\"", string.Empty);
                    string goodName = DelHiddenCharsFromString(name);

                    if (goodName == stopName)
                    {
                        stopID = IDs[totalRows].ToString();
                    }
                    if (stopID != null) { break; }
                }
                return stopID;
            }
        }




        private static void MakeTripsNStopTimes() // When xlsx contains empty cells, you need to change them from nothing to fe. "_", if not you ll be skipping them automatically
        {
            for (int file =1; file < 9; file++)
            {
                using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(@"C:\Users\Tunio\Desktop\Myszków\LINIA "+file.ToString()+".xlsx")))
                {

                    int numberOfWorksheets = xlPackage.Workbook.Worksheets.Count();
                    for (int sheet = 0; sheet < numberOfWorksheets; sheet++) 
                    {
                        Console.Clear();
                        Console.WriteLine("Aktualnie przerobiłem " + (sheet+1).ToString() + " z " + (numberOfWorksheets).ToString() + " arkuszy" + ", plik" + file.ToString());

                        var myWorksheet = xlPackage.Workbook.Worksheets.ElementAt(sheet); 
                        var totalRows = myWorksheet.Dimension.End.Row;
                        var totalColumns = myWorksheet.Dimension.End.Column;

                        List<string> sheetStopsNameTabe = new List<string> { };//
                        List<string> sheetStopsIDsTabe = new List<string> { };
                        string lineNumber = myWorksheet.Name.Remove(1);

                        var namesRow = myWorksheet.Cells[4, 1, totalRows, 1].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                        // sprawdz czy nie ma pustych
                        for (int i = 0; i < totalRows - 3; i++)
                        {
                            sheetStopsNameTabe.Add(namesRow.ElementAt(i));//
                            sheetStopsIDsTabe.Add(GetStopIDFromName(namesRow.ElementAt(i).ToString()));
                        }
                        string headsign = "HEADSIGN";
                        int tripsIndex = 1;

                        for (int column = 2; column <= totalColumns; column++) // od 2 col włącznie
                        {
                            var serviceRows = myWorksheet.Cells[2, column, 2, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString()); // odwrotnie bo bierze B2:B34
                            var serviceRow = myWorksheet.Cells[2, column].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                            var directionRow = myWorksheet.Cells[3, column].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                            // R Robocze WS Weekendy i Święta
                            string serviceType = serviceRow.First();
                            if (serviceType == "R") serviceType = "Robocze";
                            else if (serviceType == "") serviceType = "Codziennie";
                            else if (serviceType == "SN" || serviceType == "S N") serviceType = "Soboty i Niedziele";
                            else if (serviceType == "RN") serviceType = "Szkolne";
                            else if (serviceType == "S") serviceType = "Soboty";
                            else if (serviceType == "N") serviceType = "Niedziele";
                            else if (serviceType == "R S") serviceType = "Robocze i Soboty";
                            else if (serviceType == "S N DZ") serviceType = "Soboty i Niedziele do 30.09";
                            else if (serviceType == "N DZ") serviceType = "Niedziele do 30.09";
                            else if (serviceType == "R S DZ") serviceType = "Robocze i Soboty do 30.09";
                            else if (serviceType == "DZ") serviceType = "Codziennie do 30.09";
                            else serviceType = "Weekendy i Święta";
                            // stop_times
                            if (directionRow.First() == "down")
                            {
                                var scheduleRows = myWorksheet.Cells[4, column, totalRows, column].Select(c => c.Value == null ? string.Empty : c.Value.ToString()); // od 2 col włącznie
                                for (int rowNumber = totalRows - 4; rowNumber >= 0; rowNumber--) // spr stacje docelową (jak są dziury)
                                {
                                    if (scheduleRows.ElementAt(rowNumber) != " " && scheduleRows.ElementAt(rowNumber) != "")
                                    {
                                        headsign = namesRow.ElementAt(rowNumber);
                                        break;
                                    }
                                }
                                int sequence = 0;

                                for (int rowNumber = 0; rowNumber < totalRows - 3; rowNumber++)
                                {
                                    //string tppp = scheduleRows.ElementAt(rowNumber); // breakpointa i spr jak wyglada czas...
                                    if (scheduleRows.ElementAt(rowNumber) != " " && scheduleRows.ElementAt(rowNumber) != "") //pozostalość po braku wartości w srodku rozkladu
                                    {
                                        string time = scheduleRows.ElementAt(rowNumber);
                                        //string time = scheduleRows.ElementAt(rowNumber).Remove(0, 11);
                                        if (time.Length == 4) time = "0" + time;
                                        time = time + ":00";
                                        time = time.Replace('.', ':');
                                        time = time.Replace(',', ':');
                                        time = time.Replace(' ', ':');
                                        time = time.Replace('-', ':');
                                        time = time.Replace("::", ":");
                                        Stop_time stopTimeHandler = new Stop_time(tripsIndex.ToString() + lineNumber + "M" + lineNumber + sheet.ToString() + lineNumber + lineNumber + file, time, time, sheetStopsIDsTabe[rowNumber], sequence.ToString(), savingPath);
                                        sequence++;
                                    }
                                }
                            }
                            else
                            {
                                var scheduleRows = myWorksheet.Cells[4, column, totalRows, column].Select(c => c.Value == null ? string.Empty : c.Value.ToString()); // od 2 col włącznie
                                for (int rowNumber = 0; rowNumber < totalRows - 3; rowNumber++) // spr stacje docelową (jak są dziury)
                                {
                                    if (scheduleRows.ElementAt(rowNumber) != " " && scheduleRows.ElementAt(rowNumber) != "")
                                    {
                                        headsign = namesRow.ElementAt(rowNumber);
                                        break;
                                    }
                                }
                                int sequence = 0;

                                for (int rowNumber = totalRows - 4; rowNumber >= 0 ; rowNumber--)
                                {
                                    //string tppp = scheduleRows.ElementAt(rowNumber); // breakpointa i spr jak wyglada czas...
                                    if (scheduleRows.ElementAt(rowNumber) != " " && scheduleRows.ElementAt(rowNumber) != "") //pozostalość po braku wartości w srodku rozkladu
                                    {
                                        string time = scheduleRows.ElementAt(rowNumber);
                                        //string time = scheduleRows.ElementAt(rowNumber).Remove(0, 11);
                                        if (time.Length == 4) time = "0" + time;
                                        time = time + ":00";
                                        time = time.Replace('.', ':');
                                        time = time.Replace(',', ':');
                                        time = time.Replace(' ', ':');
                                        time = time.Replace('-', ':');
                                        time = time.Replace("::", ":");
                                        Stop_time stopTimeHandler = new Stop_time(tripsIndex.ToString() + lineNumber + "M" + lineNumber + sheet.ToString() + lineNumber + lineNumber + file, time, time, sheetStopsIDsTabe[rowNumber], sequence.ToString(), savingPath);
                                        sequence++;
                                    }
                                }
                            }
                            // stop_times
                            Trip tripHandler = new Trip(lineNumber, serviceType, tripsIndex.ToString() + lineNumber + "M" + lineNumber + sheet.ToString() + lineNumber + lineNumber + file, headsign, savingPath);
                            tripsIndex++;
                            
                        }
                    }
                }
            }
        }


        private static void MakeRoutesTXT()
        {
            foreach (var route in mainWindowHandler.routesDictionary)
            {
                Route routeHandler = new Route(route.Key, "0", route.Key, route.Value, mainWindowHandler.typeOfRoute, savingPath);
            }
        }

        private static void MakeStopsTXT(List<string> stopNames, List<string> stopLat, List<string> stopLon)
        {
            int cityIDValue = char.ConvertToUtf32(mainWindowHandler.CityName.Text.ElementAt(0).ToString(), 0) + char.ConvertToUtf32("S", 0) + char.ConvertToUtf32(mainWindowHandler.CityName.Text.ElementAt(1).ToString(), 0) + char.ConvertToUtf32(mainWindowHandler.CityName.Text.ElementAt(2).ToString(), 0); // Letters: City 1st, File 1st, City second, county 1st (PL:powiat)
            for (int row = 0; row < stopNames.Count; row++) // 1st is name line
            {
                int stopID = cityIDValue + row;
                string goodName = DelHiddenCharsFromString(stopNames[row]);
                Stop stopHandler = new Stop(stopID.ToString(), stopID.ToString(), stopNames[row].ToString().Replace("\"", string.Empty), stopLat[row].ToString(), stopLon[row].ToString(), savingPath);
                stopID++;
            }
        }

        public static void MakeAgencyTXT()
        {
            Agency agencyHandler = new Agency(0, mainWindowHandler.Agency.Text, mainWindowHandler.Site.Text, savingPath);
        }

        protected static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private static bool AreAllStopsMatched(List<string> StopsFromTimetable, List<string> StopsFromOtherFile)
        {
            List<string> noMatch = new List<string>();
            foreach (var stopFromTimetable in StopsFromTimetable)
            {
                if (!StopsFromOtherFile.Contains(stopFromTimetable))
                {
                    noMatch.Add(stopFromTimetable);
                }
            }
            if (noMatch.Count != 0)
            {
                string path = Directory.GetCurrentDirectory() + @"\stops_noMatch.txt";

                if (File.Exists(path))
                {
                    if (IsFileLocked(new FileInfo(path)))
                    {
                        Interfejs.Message successMessage = new Interfejs.Message(mainWindowHandler, "Błąd", "Zamknij otwarty plik 'stops_noMatch.txt' masz na to kilka sekund");
                        successMessage.Owner = mainWindowHandler;
                        successMessage.Show();
                        successMessage.Topmost = true;
                        Task.Delay(5000);
                        if (IsFileLocked(new FileInfo(path)))
                        {
                            return false;
                        }
                        else File.Delete(path);
                    }
                    else File.Delete(path);
                }
                using (FileStream fs = new FileStream(path, FileMode.CreateNew))
                {
                    Byte[] text = new UTF8Encoding(true).GetBytes("Przystanki_bez_pokrycia:" + Environment.NewLine);
                    fs.Write(text, 0, text.Length);
                }
                foreach (var stop in noMatch)
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true))
                    {
                        string text = stop;
                        sw.WriteLine(text);
                    }
                }
                return false;
            }
            else return true;
        }

        private static bool AreAllServicesMatched(List<string> SymbolsFromTimetable, List<string> SymbolsFromSheet)
        {
            List<string> noMatch = new List<string>();
            foreach (var symbolFromTimetable in SymbolsFromTimetable)
            {
                if (!SymbolsFromSheet.Contains(symbolFromTimetable))
                {
                    noMatch.Add(symbolFromTimetable);
                }
            }
            if (noMatch.Count != 0)
            {
                mainWindowHandler.noMatchServices = noMatch;
                string path = Directory.GetCurrentDirectory() + @"\services_noMatch.txt";

                if (File.Exists(path))
                {
                    if (IsFileLocked(new FileInfo(path)))
                    {
                        Interfejs.Message successMessage = new Interfejs.Message(mainWindowHandler, "Błąd", "Zamknij otwarty plik 'services_noMatch.txt' masz na to kilka sekund");
                        successMessage.Owner = mainWindowHandler;
                        successMessage.Show();
                        successMessage.Topmost = true;
                        Task.Delay(5000);
                        if (IsFileLocked(new FileInfo(path)))
                        {
                            return false;
                        }
                        else File.Delete(path);
                    }
                    else File.Delete(path);
                }
                using (FileStream fs = new FileStream(path, FileMode.CreateNew))
                {
                    Byte[] text = new UTF8Encoding(true).GetBytes("Symbole_bez_pokrycia:" + Environment.NewLine);
                    fs.Write(text, 0, text.Length);
                }
                foreach (var stop in noMatch)
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true))
                    {
                        string text = stop;
                        sw.WriteLine(text);
                    }
                }
                return false;
            }
            else return true;
        }

        private static void TryToAddNewServiceToDictionary(string key, string value)
        {
            if (!mainWindowHandler.IsDictioranyContainingKey(mainWindowHandler.routesDictionary, key))
            {
                mainWindowHandler.routesDictionary.Add(key, value);
            }
        }

        public static bool CheckStopsMatching(MainWindow mainWindow)
        {
            mainWindowHandler = mainWindow;
            mainWindowHandler.servicesDictionary.Clear();
            List<string> stopNamesFromTimetable = new List<string> { };
            List<string> stopNamesFromStops = new List<string> { };
            List<string> stopLatFromStops = new List<string> { };
            List<string> stopLonFromStops = new List<string> { };
            using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(mainWindow.timetableFilePath)))
            {
                if (xlPackage.Workbook.Worksheets.Count != 0)
                {
                    for (int sheet = 0; sheet < xlPackage.Workbook.Worksheets.Count; sheet++)
                    {
                        var myWorksheet = xlPackage.Workbook.Worksheets.ElementAt(sheet);
                        if(myWorksheet.Name != "Stops" && myWorksheet.Name != "Services")
                        {
                            string lineNumber = myWorksheet.Name.Split(' ')[0];
                            var route = myWorksheet.Cells[1, 1].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                            TryToAddNewServiceToDictionary(lineNumber, route.First());

                            var totalRows = myWorksheet.Dimension.End.Row;
                            int kolumnaPrzystanek = 1;
                            var namesRow = myWorksheet.Cells[3, kolumnaPrzystanek, totalRows, kolumnaPrzystanek].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                            int indexer = 0;
                            while (!(indexer > (totalRows - 3)))
                            {
                                AddUniqeStopToList(stopNamesFromTimetable, namesRow.ElementAt(indexer));
                                indexer++;
                            }
                        }
                    }
                }
            }
            stopNamesFromTimetable.Sort();
            if(mainWindow.stopsFileExtension == "xlsx")
            {
                using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(mainWindow.stopsFilePath)))
                {
                    if (xlPackage.Workbook.Worksheets.Count != 0)
                    {
                        //for (int sheet = 0; sheet < xlPackage.Workbook.Worksheets.Count; sheet++)
                        //{
                            var myWorksheet = xlPackage.Workbook.Worksheets.First();
                            var totalRows = myWorksheet.Dimension.End.Row;
                            int kolumnaPrzystanek = 1;
                            var namesRow = myWorksheet.Cells[2, kolumnaPrzystanek, totalRows, kolumnaPrzystanek].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                            var LatRows = myWorksheet.Cells[2, kolumnaPrzystanek + 1, totalRows, kolumnaPrzystanek + 1].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                            var LonRows = myWorksheet.Cells[2, kolumnaPrzystanek + 2, totalRows, kolumnaPrzystanek + 2].Select(c => c.Value == null ? string.Empty : c.Value.ToString());

                            int indexer = 0;
                            while (!(indexer > (totalRows - 2)))
                            {
                                AddUniqeStopToList(stopNamesFromStops, namesRow.ElementAt(indexer));
                                double lat, lon;
                                if (Double.TryParse(LatRows.ElementAt(indexer), out lat) && (Double.TryParse(LonRows.ElementAt(indexer), out lon)))
                                {
                                    AddUniqeStopToList(stopLatFromStops, lat.ToString().Replace(',', '.'));
                                    AddUniqeStopToList(stopLonFromStops, lon.ToString().Replace(',', '.'));
                                }
                                indexer++;
                            }
                        //}
                    }
                }
            }
            else // TXT
            {
                using (var reader = new StreamReader(mainWindow.stopsFilePath))
                {
                    List<string> Names = new List<string>();
                    List<string> Lat = new List<string>();
                    List<string> Lon = new List<string>();
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        stopNamesFromStops.Add(values[0]);
                        stopLatFromStops.Add(values[1]);
                        stopLonFromStops.Add(values[2]);
                    }
                }
            }
            if (AreAllStopsMatched(stopNamesFromTimetable, stopNamesFromStops))
            {
                Route route = new Route(savingPath);
                MakeRoutesTXT();
                Stop stop = new Stop(savingPath);
                MakeStopsTXT(stopNamesFromStops,stopLatFromStops,stopLonFromStops);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool CheckServicesMatching(MainWindow mainWindow)
        {
            mainWindowHandler = mainWindow;
            List<string> servicesSymbolsFromTimetable = new List<string> { };
            List<string> servicesSymbolsFromSheet = new List<string> { };
            List<string> servicesMeaning = new List<string> { };
            using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(mainWindow.timetableFilePath)))
            {
                if (xlPackage.Workbook.Worksheets.Count != 0)
                {
                    for (int sheet = 0; sheet < xlPackage.Workbook.Worksheets.Count; sheet++)
                    {
                        var myWorksheet = xlPackage.Workbook.Worksheets.ElementAt(sheet);
                        if (myWorksheet.Name != "Services")
                        {
                            var totalColumns = myWorksheet.Dimension.End.Column;
                            var namesRow = myWorksheet.Cells[2, 2, 2, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                            int indexer = 0;
                            while (!(indexer > (totalColumns - 2)))
                            {
                                AddUniqeStopToList(servicesSymbolsFromTimetable, namesRow.ElementAt(indexer));
                                indexer++;
                            }
                        }
                        else // plik services
                        {
                            var totalRows = myWorksheet.Dimension.End.Row;
                            var symbolsRow = myWorksheet.Cells[2, 1, totalRows, 1].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                            var meanigRows = myWorksheet.Cells[2, 2, totalRows, 2].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                            int indexer = 0;
                            while (!(indexer > (totalRows - 2)))
                            {
                                AddUniqeStopToList(servicesSymbolsFromSheet, symbolsRow.ElementAt(indexer));
                                AddUniqeStopToList(servicesMeaning, meanigRows.ElementAt(indexer));
                                indexer++;
                            }
                        }
                    }
                }
            }
            if (AreAllServicesMatched(servicesSymbolsFromTimetable, servicesSymbolsFromSheet))
            {
                mainWindowHandler.servicesDictionary.Clear();
                for (int i = 0; i < servicesSymbolsFromSheet.Count; i++)
                { 
                    if (!mainWindowHandler.IsServicesDictioranyContainingKey(servicesSymbolsFromSheet[i]) && !mainWindowHandler.IsServicesDictioranyContainingValue(servicesMeaning[i]))
                    {
                        mainWindowHandler.servicesDictionary.Add(servicesSymbolsFromSheet[i], servicesMeaning[i]);
                    }
                }
                return true;
            }
            else
            {
                mainWindowHandler.servicesDictionary.Clear();
                for (int i = 0; i < servicesSymbolsFromSheet.Count; i++)
                {
                    if (!mainWindowHandler.IsServicesDictioranyContainingKey(servicesSymbolsFromSheet[i]) && !mainWindowHandler.IsServicesDictioranyContainingValue(servicesMeaning[i]))
                    {
                        mainWindowHandler.servicesDictionary.Add(servicesSymbolsFromSheet[i], servicesMeaning[i]);
                    }
                }
                return false;
            }
        }








        private static void MakeSlupki()
        {
            using (var reader = new StreamReader(@"C:\Users\Tunio\Desktop\Myszków\slupki.txt"))
            {
                List<string> Names = new List<string>();
                List<string> Lon = new List<string>();
                List<string> Lat = new List<string>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(';');
                    Names.Add(NormalizeStopsName(values[1]));
                    Lat.Add(values[2]);
                    Lon.Add(values[3]);
                }
                string recentName = "tmp";
                for (int i =0; i < Names.Count; i++)
                {
                    string next = "Brak";
                    if (i != (Names.Count - 1))
                    {
                        next = Names[i + 1];
                    }
                    else
                    {
                        next = "lastt";
                    }
                    if (Names[i] == next) // powtorka
                    {
                        double newLat = (Double.Parse(Lat[i].Replace('.', ',').Replace("\"", string.Empty)) + Double.Parse(Lat[i+1].Replace('.', ',').Replace("\"", string.Empty))) / 2;
                        double newLon = (Double.Parse(Lon[i].Replace('.', ',').Replace("\"", string.Empty)) + Double.Parse(Lon[i+1].Replace('.', ',').Replace("\"", string.Empty))) / 2;
                        WriteStopToFile(savingPath + "\\slupki_JAR.txt", "\"" + Names[i] + "\"", newLat.ToString().Replace(',','.'), newLon.ToString().Replace(',', '.'));
                        i++;
                    }
                    else
                    {
                        WriteStopToFile(savingPath + "\\slupki_JAR.txt", "\"" + Names[i] + "\"", Lat[i].Replace("\"", string.Empty), Lon[i].Replace("\"", string.Empty));
                    }
                    recentName = Names[i];

                }
            }
        }

        private static void SwapStopsLatnLon()
        {
            using (var reader = new StreamReader(@"C:\Users\Tunio\Desktop\Myszków\slupki.txt"))
            {
                List<string> noMatch = new List<string>();

                List<string> slupkiNames = new List<string>();
                List<string> slupkiLon = new List<string>();
                List<string> slupkiLat = new List<string>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    slupkiNames.Add(values[0]);
                    slupkiLat.Add(values[1]);
                    slupkiLon.Add(values[2]);
                }
                using (var reader2 = new StreamReader(@"C:\Users\Tunio\Desktop\Myszków\GTFS\stops.txt"))
                {
                    List<string> stopsID = new List<string>();
                    List<string> stopsCode = new List<string>();
                    List<string> stopsNames = new List<string>();
                    List<string> stopsLon = new List<string>();
                    List<string> stopsLat = new List<string>();
                    while (!reader2.EndOfStream)
                    {
                        var line = reader2.ReadLine();
                        var values = line.Split(',');
                        stopsID.Add(values[0]);
                        stopsCode.Add(values[1]);
                        stopsNames.Add(values[2]);
                        stopsLat.Add(values[3]);
                        stopsLon.Add(values[4]);
                    }
                    int tmpp = 0;
                    for (int j = 1; j < stopsNames.Count; j++)
                    {
                        for (int i =1; i < slupkiNames.Count; i++)
                        {

                            if (stopsNames[j].Replace("\"",string.Empty) == slupkiNames[i]) // pasuje przystanek, zmien wspolrzedne
                            {
                                stopsLat[j] = slupkiLat[i];
                                stopsLon[j] = slupkiLon[i];
                                stopsLon[j] += "             ZMIENIONE";
                                tmpp++;
                                break;
                            }
                            if (i == (slupkiNames.Count - 1))
                            {
                                noMatch.Add(stopsNames[j]);
                            }
                        }
                    }
                    Console.WriteLine("zmienilem az {0} z {1}", tmpp, stopsNames.Count);
                    for (int j = 1; j < stopsNames.Count; j++)
                    {
                        string path = @"C:\Users\Tunio\Desktop\Myszków\GTFS\stops2.txt";
                        using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true))
                        {
                            string text = (stopsID[j] + "," + stopsCode[j] + "," + stopsNames[j] + "," + stopsLat[j] + "," + stopsLon[j]);
                            sw.WriteLine(text);
                        }
                    }
                    for (int j = 0; j < noMatch.Count; j++)
                    {
                        string path = @"C:\Users\Tunio\Desktop\Myszków\GTFS\stops_noMatch.txt";
                        using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true))
                        {
                            string text = (noMatch[j]);
                            sw.WriteLine(text);
                        }
                    }

                }
            }
        }

        private static void ChangeStopsIDFromStop_Times()
        {
            using (var reader = new StreamReader(@"C:\Users\Tunio\Desktop\Parser_GTFS\Parser\Parser_GTFS_Oswiecim\Latest gtfs\stop_times.txt"))
            {
                List<string> stopID = new List<string>();
                List<string> tripID = new List<string>();
                List<string> Arrival = new List<string>();
                List<string> Departure = new List<string>();
                List<string> Sequence = new List<string>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    tripID.Add(values[0]);
                    Arrival.Add(values[1]);
                    Departure.Add(values[2]);
                    stopID.Add(values[3]);
                    Sequence.Add(values[4]);
                }
                for (int i = 1; i < stopID.Count; i++)
                {
                    Console.Clear();
                    double srakataka = (double)(i / stopID.Count);
                    Console.WriteLine("Napierdalam {0}, z {1} część pierwsza czyli jakie {2}%", i, stopID.Count, srakataka);
                    if (i < (stopID.Count - 2))
                    {
                        if (stopID[i] == "557" && stopID[i + 1] == "511" && stopID[i + 2] == "516")
                        {
                            stopID[i + 2] = "601";
                        }
                        if (stopID[i] == "516" && stopID[i + 1] == "511" && stopID[i + 2] == "557")
                        {
                            stopID[i] = "601";
                        }
                    }
                }
                for (int j = 0; j < stopID.Count; j++)
                {
                    Console.Clear();
                    Console.WriteLine("Napierdalam {0}, z {1} część druga", j, stopID.Count);
                    string path = @"C:\Users\Tunio\Desktop\Parser_GTFS\Parser\Parser_GTFS_Oswiecim\Latest gtfs\stop_times2.txt";
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true))
                    {
                        string text = (tripID[j] + "," + Arrival[j] + "," + Departure[j] + "," + stopID[j] + "," + Sequence[j]);
                        sw.WriteLine(text);
                    }
                }
            }
        }

        
    }
}

//MakeStopsTXT();
// Making stops.txt
//int stopsIndex = 0;
//int indeksingStatringValueStops = 7600;
//using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(@"C:\Users\Tunio\Desktop\Parser_GTFS\Mlawa\Współrzędne przystanków.xlsx")))
//{
//    var myWorksheet = xlPackage.Workbook.Worksheets.First(); //select sheet here
//    var totalRows = myWorksheet.Dimension.End.Row;
//    var totalColumns = myWorksheet.Dimension.End.Column;

//    var sb = new StringBuilder(); //this is your your data
//    for (int rowNum = 1; rowNum <= totalRows; rowNum++) //selet starting row here
//    {
//        var row = myWorksheet.Cells[rowNum, 1, rowNum, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
//        string name = row.ElementAt(0);
//        name = DelHiddenCharsFromString(name.Remove(name.Length - 3));
//        double lat = Convert.ToDouble(row.ElementAt(1).Replace(".", ","));
//        double lon = Convert.ToDouble(row.ElementAt(2).Replace(".", ","));
//        int counter = 1;
//        bool checkMore = ((rowNum + counter) < totalRows);
//        while (checkMore)
//        {
//            var nextrow = myWorksheet.Cells[rowNum + counter, 1, rowNum + counter, totalColumns].Select(tmpc => tmpc.Value == null ? string.Empty : tmpc.Value.ToString());
//            string nextName = nextrow.ElementAt(0);
//            nextName = DelHiddenCharsFromString(nextName.Remove(nextName.Length - 3)); // XX last numbers go out
//            if (name == nextName)
//            {
//                double nextLat = Convert.ToDouble(nextrow.ElementAt(1).Replace(".", ","));
//                double nextLon = Convert.ToDouble(nextrow.ElementAt(2).Replace(".", ","));
//                lat = (lat + nextLat) / 2;
//                lon = (lon + nextLon) / 2;
//                counter++;
//                checkMore = ((rowNum + counter) < totalRows);
//            }
//            else
//            {
//                counter--;
//                rowNum += counter;
//                checkMore = false;
//            }
//        }
//        Stop stopHandler = new Stop(indeksingStatringValueStops.ToString(), indeksingStatringValueStops.ToString(), name, lat.ToString(), lon.ToString(), savingPath);
//        indeksingStatringValueStops++;
//    }
//}
// Making stops.txt END

// Making routes.txt
//int routesIndex = 1;
//using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(@"C:\Users\Tunio\Desktop\Parser_GTFS\Mlawa\linie.xlsx")))
//{
//    var myWorksheet = xlPackage.Workbook.Worksheets.First(); //select sheet here
//    var totalRows = myWorksheet.Dimension.End.Row;
//    var totalColumns = myWorksheet.Dimension.End.Column;

//    //var sb = new StringBuilder(); //this is your your data
//    for (int rowNum = 1; rowNum <= totalRows; rowNum++) //selet starting row here
//    {
//        var row = myWorksheet.Cells[rowNum, 1, rowNum, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
//        string lineNumber = row.ElementAt(0);
//        string lineRoute = row.ElementAt(1);
//        int tmp1 = lineRoute.LastIndexOf('-');
//        string lastStation = lineRoute.Remove(0, tmp1 + 1);

//        tmp1 = lineRoute.IndexOf('-');
//        string fistStation = lineRoute.Remove(tmp1);
//        Route routeHandler = new Route(lineNumber, "0", "Line " + lineNumber, fistStation + "-" + lastStation, "3", savingPath);
//        routesIndex++;
//    }
//}
// Making routes.txt END

// Making trips.txt
//using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(@"C:\Users\Tunio\Desktop\Parser_GTFS\Mlawa\Rozklad_full.xlsx")))
//{
//    string serviceType = "Unknown";
//    int numberOfWorksheets = xlPackage.Workbook.Worksheets.Count();               
//    //var sb = new StringBuilder(); //this is your your data
//    for (int sheet = 0; sheet < numberOfWorksheets; sheet++) //selet starting row here
//    {
//        int tripsIndex = 1;
//        string headsign = " "; // bo jak puste to przechodzi do next :/

//        var myWorksheet = xlPackage.Workbook.Worksheets.ElementAt(sheet); //select sheet here
//        var totalRows = myWorksheet.Dimension.End.Row;
//        var totalColumns = myWorksheet.Dimension.End.Column;

//        string lineNumber = myWorksheet.Name.Remove(myWorksheet.Name.IndexOf("_"));
//        string worksheetName = myWorksheet.Name.Replace("_", string.Empty);

//        if (worksheetName.Contains("r")) { serviceType = "Robocze"; }
//        else if (worksheetName.Contains("s")) { serviceType = "Soboty"; }
//        else if (worksheetName.Contains("n")) { serviceType = "Niedziele"; }

//        var row = myWorksheet.Cells[totalRows, 1, totalRows, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString()); // [ Wiersze - OD np A:20, Kolumny | Od ktorej kolumny brac, Wiersze -  DO np S:20, Kolumny | Do której]
//        for (int column = 1; column < totalColumns; column++)
//        {
//            if (row.ElementAt(column) != " ")
//            {
//                headsign = row.ElementAt(0);
//            }
//            else
//            {
//                int i = 1;
//                string cellValue = " ";
//                do
//                {
//                    var tmpRow = myWorksheet.Cells[totalRows - i, 1, totalRows - i, column + 1].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
//                    headsign = tmpRow.ElementAt(0);
//                    cellValue = tmpRow.ElementAt(column);
//                    i++;
//                }
//                while (cellValue == " ");// zakladam ze nie ma przypadku ze dane są zle podane ... (zmniejszenie az to -1 indeksu)
//            }
//            // dodanie nowego tripa
//            Trip tripHandler = new Trip(lineNumber, serviceType, tripsIndex.ToString() + worksheetName, headsign, savingPath);
//            tripsIndex++;
//        }
//    }
//}
// Making trips.txt END
// Making stops_times.txt and trips 
//using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(@"C:\Users\Tunio\Desktop\Parser_GTFS\Mlawa\Rozklad_full.xlsx")))
//{
//    string serviceType = "Unknown";
//    int numberOfWorksheets = xlPackage.Workbook.Worksheets.Count();
//    //var sb = new StringBuilder(); //this is your your data
//    //for (int sheet = 0; sheet < 3; sheet++) //selet starting row here

//    for (int sheet = 0; sheet < numberOfWorksheets - 1; sheet++) //selet starting row here
//    {
//        Console.Clear();
//        Console.WriteLine("Aktualnie przerobiłem " + sheet.ToString() + " z " + (numberOfWorksheets - 1).ToString() + " arkuszy");
//        int tripsIndex = 1;
//        string headsign = " "; // bo jak puste to przechodzi do next :/
//        //List<string> sheetStopsNameTabe = new List<string> { };//
//        List<string> sheetStopsIDsTabe = new List<string> { };

//        var myWorksheet = xlPackage.Workbook.Worksheets.ElementAt(sheet); //select sheet here
//        var totalRows = myWorksheet.Dimension.End.Row;
//        var totalColumns = myWorksheet.Dimension.End.Column;
//        //
//        var namesRow = myWorksheet.Cells[2, 1, totalRows, 1].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
//        for (int i = 0; i < totalRows - 1; i++)
//        {
//            //sheetStopsNameTabe.Add(namesRow.ElementAt(i));//
//            sheetStopsIDsTabe.Add(GetStopIDFromName(namesRow.ElementAt(i).ToString()));
//        }//
//        string lineNumber = myWorksheet.Name.Remove(myWorksheet.Name.IndexOf("_"));
//        string worksheetName = myWorksheet.Name.Replace("_", string.Empty);

//        if (worksheetName.Contains("r")) { serviceType = "Robocze"; }
//        else if (worksheetName.Contains("s")) { serviceType = "Soboty"; }
//        else if (worksheetName.Contains("n")) { serviceType = "Niedziele"; }

//        var row = myWorksheet.Cells[totalRows, 1, totalRows, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString()); // [ Wiersze - OD np A:20, Kolumny | Od ktorej kolumny brac, Wiersze -  DO np S:20, Kolumny | Do której]
//        for (int column = 1; column < totalColumns; column++)
//        {
//            if (row.ElementAt(column) != " ")
//            {
//                headsign = row.ElementAt(0);
//            }
//            else
//            {

//                int i = 1;
//                string cellValue = " ";
//                do
//                {
//                    var tripRow = myWorksheet.Cells[totalRows - i, 1, totalRows - i, column + 1].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
//                    headsign = tripRow.ElementAt(0);
//                    cellValue = tripRow.ElementAt(column);
//                    i++;
//                }
//                while (cellValue == " ");// zakladam ze nie ma przypadku ze dane są zle podane ... (zmniejszenie az to -1 indeksu)
//            }
//            // stop_times
//            var stopsRow = myWorksheet.Cells[2, column + 1, totalRows, column + 1].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
//            int sequence = 0;
//            for (int rowNumber = 0; rowNumber < totalRows - 1; rowNumber++)
//            {
//                //string tppp = stopsRow.ElementAt(0).Remove(0, 11);
//                if (stopsRow.ElementAt(rowNumber) != " ")
//                {
//                    string time = stopsRow.ElementAt(rowNumber).Remove(0, 11); // trzeba sprawdzac czy nie jest po 24:00:00 jak cos to pierwsza to 25:00:00 itd...
//                    Stop_time stopTimeHandler = new Stop_time(tripsIndex.ToString() + worksheetName + worksheetName, time, time, sheetStopsIDsTabe[rowNumber], sequence.ToString(), savingPath);
//                    sequence++;
//                }
//            }
//            // stop_times
//            // dodanie nowego tripa
//            Trip tripHandler = new Trip(lineNumber, serviceType, tripsIndex.ToString() + worksheetName + worksheetName, headsign, savingPath); // dodac cos do trip id bo 11r1 i 11r1 to moze byc cos innegoxD
//            tripsIndex++;
//        }
//    }
//}
// Making stops_times.txt and trips END