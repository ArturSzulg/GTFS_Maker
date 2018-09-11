using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser_GTFS
{
    class Trip
    {
        private const string separator = ",";
        private string route_id;
        private string service_id;
        private string trip_id;
        private string trip_headsign;
        private string direction_id;
        //private string exceptional;
        private string path;
        public Trip(string new_route_id, string new_service_id, string new_trip_id, string new_trip_headsign, string fileSavingPath, string new_direction_id = null)
        {
            route_id = new_route_id + separator;
            service_id = new_service_id + separator;
            trip_id = new_trip_id + separator;
            trip_headsign = new_trip_headsign;// + separator;
            direction_id = new_direction_id + separator;
            path = fileSavingPath + @"\trips.txt";
            WriteTripToFile();
        }

        public bool GenerateTripsFile()
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                using (FileStream fs = new FileStream(path, FileMode.CreateNew))
                {
                    Byte[] text = new UTF8Encoding(true).GetBytes("route_id,service_id,trip_id,trip_headsign" + Environment.NewLine);
                    fs.Write(text, 0, text.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public void WriteTripToFile()
        {
            if (!(File.Exists(path)))
            {
                if (!GenerateTripsFile())
                {
                    Console.WriteLine("Error, cannot make Trips file");
                }
            }
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true))
            {
                string text = (route_id + service_id + trip_id + trip_headsign /*+ direction_id*/);
                sw.WriteLine(text);
            }
        }
    }
}
