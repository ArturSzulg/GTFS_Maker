using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser_GTFS
{
    class Stop_time
    {
        private const string separator = ",";
        private string trip_id;
        private string arrival_time;
        private string departure_time;
        private string stop_id;
        private string stop_sequence;
        private string path;
        public Stop_time(string new_trip_id, string new_arrival_time, string new_departure_time, string new_stop_id, string new_stop_sequence, string fileSavingPath)
        {
            trip_id = new_trip_id + separator;
            arrival_time = new_arrival_time + separator;
            departure_time = new_departure_time + separator;
            stop_id = new_stop_id + separator;
            stop_sequence = new_stop_sequence;
            path = fileSavingPath + @"\stop_times.txt";
            WriteStopTimesToFile();
        }
        public Stop_time(string fileSavingPath)
        {
            // TO DO think about more clever solution
            path = fileSavingPath + @"\stop_times.txt";
            GenerateStopTimesFile();
        }

        public bool GenerateStopTimesFile()
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                using (FileStream fs = new FileStream(path, FileMode.CreateNew))
                {
                    Byte[] text = new UTF8Encoding(true).GetBytes("trip_id,arrival_time,departure_time,stop_id,stop_sequence" + Environment.NewLine);
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

        public void WriteStopTimesToFile()
        {
            if (!(File.Exists(path)))
            {
                if (!GenerateStopTimesFile())
                {
                    Console.WriteLine("Error, cannot make Stop_times file");
                }
            }
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true))
            {
                string text = (trip_id + arrival_time + departure_time + stop_id + stop_sequence);
                sw.WriteLine(text);
            }
        }
    }
}
