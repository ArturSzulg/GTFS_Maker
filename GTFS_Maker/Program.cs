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

        private static void AddUniqeToList(List<string> uniqeStops, string stopName)
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
            using (var reader = new StreamReader(savingPath + @"\stops.txt"))
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

        private static bool MakeRoutesTXT()
        {
            try
            { 
                foreach (var route in mainWindowHandler.routesDictionary)
                {
                    Route routeHandler = new Route(route.Key, "0", route.Key, route.Value, mainWindowHandler.typeOfRoute, savingPath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool MakeStopsTXT(List<string> stopNames, List<string> stopLat, List<string> stopLon)
        {
            try
            { 
                int cityIDValue = char.ConvertToUtf32(mainWindowHandler.CityName.Text.ElementAt(0).ToString(), 0) + char.ConvertToUtf32("S", 0) + char.ConvertToUtf32(mainWindowHandler.CityName.Text.ElementAt(1).ToString(), 0) + char.ConvertToUtf32(mainWindowHandler.CityName.Text.ElementAt(2).ToString(), 0); // Letters: City 1st, File 1st, City second, county 1st (PL:powiat)
                for (int row = 0; row < stopNames.Count; row++) // 1st is name line
                {
                    int stopID = cityIDValue + row;
                    string goodName = DelHiddenCharsFromString(stopNames[row]);
                    Stop stopHandler = new Stop(stopID.ToString(), stopID.ToString(), stopNames[row].ToString().Replace("\"", string.Empty), stopLat[row].ToString(), stopLon[row].ToString(), savingPath);
                    stopID++;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool MakeAgencyTXT()
        {
            try
            {
                Agency agencyHandler = new Agency(0, mainWindowHandler.Agency.Text, mainWindowHandler.Site.Text, savingPath);
                return true;
            }
            catch
            {
                return false;
            }
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
                                AddUniqeToList(stopNamesFromTimetable, namesRow.ElementAt(indexer));
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
                                AddUniqeToList(stopNamesFromStops, namesRow.ElementAt(indexer));
                                double lat, lon;
                                if (Double.TryParse(LatRows.ElementAt(indexer), out lat) && (Double.TryParse(LonRows.ElementAt(indexer), out lon)))
                                {
                                    AddUniqeToList(stopLatFromStops, lat.ToString().Replace(',', '.'));
                                    AddUniqeToList(stopLonFromStops, lon.ToString().Replace(',', '.'));
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
                Stop stop = new Stop(savingPath);
                if (MakeRoutesTXT() && MakeStopsTXT(stopNamesFromStops, stopLatFromStops, stopLonFromStops))
                {
                    return true;
                }
                return false;
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
                                AddUniqeToList(servicesSymbolsFromTimetable, namesRow.ElementAt(indexer));
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
                                AddUniqeToList(servicesSymbolsFromSheet, symbolsRow.ElementAt(indexer));
                                AddUniqeToList(servicesMeaning, meanigRows.ElementAt(indexer));
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

        private static bool IsTheDayGone(string beginningTime, string endTime) // HH:MM:SS
        {
            beginningTime = beginningTime.Remove(2);
            endTime = endTime.Remove(2);
            return (Int32.Parse(beginningTime) > Int32.Parse(endTime)); // 
        }

        private static string DayGoneTimeChanger(string time, int begginingHour)
        {
            string minutesNSeconds = time.Remove(0, 2);
            int hour = Int32.Parse(time.Remove(2));
            if (hour < begginingHour)
            {
                return ((hour + 24).ToString() + minutesNSeconds);
            }
            else return time; // all ok
        }

        public static bool MakeTripsNStopTimes() // When xlsx contains empty cells, you need to change them from nothing to fe. "-", if not you ll be skipping them automatically
        {
            try { 
                using (ExcelPackage xlPackage = new ExcelPackage(new FileInfo(mainWindowHandler.timetableFilePath)))
                {
                    int numberOfWorksheets = xlPackage.Workbook.Worksheets.Count();
                    for (int sheet = 0; sheet < numberOfWorksheets; sheet++)
                    {
                        var myWorksheet = xlPackage.Workbook.Worksheets.ElementAt(sheet);
                        if (myWorksheet.Name != "Services")
                        {
                            var totalRows = myWorksheet.Dimension.End.Row;
                            var totalColumns = myWorksheet.Dimension.End.Column;

                            List<string> sheetStopsNameTabe = new List<string> { };
                            List<string> sheetStopsIDsTabe = new List<string> { };
                            List<int> repeatedStationArrivalDeparture = new List<int> { };

                            string lineNumber = myWorksheet.Name.Split(' ')[0];

                            var namesRow = myWorksheet.Cells[3, 1, totalRows, 1].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                            // sprawdz czy nie ma pustych
                            string recentStation = "NotThisSation";
                            for (int i = 0; i < totalRows - 2; i++)
                            {
                                if (namesRow.ElementAt(i) == recentStation)
                                {
                                    repeatedStationArrivalDeparture.Add(i - 1); // chcemy tego 1szego itd
                                }
                                recentStation = namesRow.ElementAt(i);
                                sheetStopsNameTabe.Add(namesRow.ElementAt(i));
                                sheetStopsIDsTabe.Add(GetStopIDFromName(namesRow.ElementAt(i).ToString()));
                            }
                            string headsign = "HEADSIGN";
                            int tripsIndex = 1;
                            string serviceType;
                            for (int column = 2; column <= totalColumns; column++) // od 2 col włącznie
                            {
                                var serviceRows = myWorksheet.Cells[2, 2, 2, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString()); // odwrotnie bo bierze B2:B34
                                mainWindowHandler.servicesDictionary.TryGetValue((serviceRows.ElementAt(column - 2)), out serviceType);

                                // stop_times
                                string tripStartTime = "00";
                                string tripEndTime = "23";
                                var scheduleRows = myWorksheet.Cells[3, column, totalRows, column].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                                for (int rowNumber = totalRows - 3; rowNumber >= 0; rowNumber--) // spr stacje docelową (jak są dziury)
                                {
                                    if (scheduleRows.ElementAt(rowNumber) != "-" && scheduleRows.ElementAt(rowNumber) != "")
                                    {
                                        // last stop time 
                                        tripEndTime = scheduleRows.ElementAt(rowNumber).Remove(0,11);
                                        headsign = namesRow.ElementAt(rowNumber);
                                        break;
                                    }
                                }
                                int sequence = 0;
                                bool DayPassed = false;
                                for (int rowNumber = 0; rowNumber < totalRows - 2; rowNumber++)
                                {
                                    //string tppp = scheduleRows.ElementAt(rowNumber); // breakpointa i spr jak wyglada czas...
                                    if (scheduleRows.ElementAt(rowNumber) != "-" && scheduleRows.ElementAt(rowNumber) != "") //pozostalość po braku wartości w srodku rozkladu
                                    {
                                        if(sequence == 0)// pierwszy czas
                                        {
                                            tripStartTime = scheduleRows.ElementAt(rowNumber).Remove(0,11);
                                            DayPassed= IsTheDayGone(tripStartTime, tripEndTime);
                                        }
                                        if (repeatedStationArrivalDeparture.Count == 0)
                                        {
                                            string time = scheduleRows.ElementAt(rowNumber).Remove(0, 11);
                                            if (DayPassed) time = DayGoneTimeChanger(time, Int32.Parse(tripStartTime.Remove(2)));
                                            Stop_time stopTimeHandler = new Stop_time(tripsIndex.ToString() + lineNumber + "M" + lineNumber + sheet.ToString() + lineNumber + lineNumber, time, time, sheetStopsIDsTabe[rowNumber], sequence.ToString(), savingPath);
                                            sequence++;
                                        }
                                        else
                                        {
                                            bool notThisIndex = true;
                                            foreach (int rep in repeatedStationArrivalDeparture)
                                            {
                                            
                                                if (rowNumber == rep && (scheduleRows.ElementAt(rowNumber + 1) != "" ) && (scheduleRows.ElementAt(rowNumber + 1) != "-"))
                                                {
                                                    string arrivalTime = scheduleRows.ElementAt(rowNumber).Remove(0, 11);
                                                    string departureTime = scheduleRows.ElementAt(rowNumber + 1).Remove(0, 11);
                                                    if (DayPassed) arrivalTime = DayGoneTimeChanger(arrivalTime, Int32.Parse(tripStartTime.Remove(2)));
                                                    if (DayPassed) departureTime = DayGoneTimeChanger(departureTime, Int32.Parse(tripStartTime.Remove(2)));
                                                    Stop_time stopTimeHandler = new Stop_time(tripsIndex.ToString() + lineNumber + mainWindowHandler.CityName.Text[0] + lineNumber + sheet.ToString() + lineNumber + lineNumber, arrivalTime, departureTime, sheetStopsIDsTabe[rowNumber], sequence.ToString(), savingPath);
                                                    sequence++;
                                                    rowNumber++;
                                                    notThisIndex = false;
                                                    break;
                                                }
                                            }
                                            if (notThisIndex)
                                            {
                                                string time = scheduleRows.ElementAt(rowNumber).Remove(0, 11); 
                                                if (DayPassed) time = DayGoneTimeChanger(time, Int32.Parse(tripStartTime.Remove(2)));
                                                Stop_time stopTimeHandler = new Stop_time(tripsIndex.ToString() + lineNumber + mainWindowHandler.CityName.Text[0] + lineNumber + sheet.ToString() + lineNumber + lineNumber, time, time, sheetStopsIDsTabe[rowNumber], sequence.ToString(), savingPath);
                                                sequence++;
                                            }
                                        }
                                    }
                                }
                                // stop_times
                                Trip tripHandler = new Trip(lineNumber, serviceType, tripsIndex.ToString() + lineNumber + mainWindowHandler.CityName.Text[0] + lineNumber + sheet.ToString() + lineNumber + lineNumber, headsign, savingPath);
                                tripsIndex++;
                            }
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}