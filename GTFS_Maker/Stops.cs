using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser_GTFS
{
    class Stop
    {
        private const string separator = ",";
        private string stop_id;
        private string stop_code;
        private string stop_name;
        private string stop_lat;
        private string stop_lon;
        private string path;
        public Stop(string new_stop_id, string new_stop_code, string new_stop_name, string new_stop_lat, string new_stop_lon, string fileSavingPath)
        {
            stop_id = new_stop_id + separator;
            stop_code = new_stop_code + separator;
            stop_name = "\"" + new_stop_name + "\"" + separator;
            stop_lat = new_stop_lat.Replace(",",".") + separator;
            stop_lon = new_stop_lon.Replace(",", ".");
            path = fileSavingPath + @"\stops.txt";
            WriteStopToFile();
        }

        public bool GenerateStopsFile()
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                using (FileStream fs = new FileStream(path, FileMode.CreateNew))
                {
                    Byte[] text = new UTF8Encoding(true).GetBytes("stop_id,stop_code,stop_name,stop_lat,stop_lon" + Environment.NewLine);
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

        public void WriteStopToFile()
        {
            //if (!(File.Exists(path)))
           // {
            if (!GenerateStopsFile())
            {
                Console.WriteLine("Error, cannot make Stops file");
            }
            //}
            else
            { 
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true))
                {
                    string text = (stop_id + stop_code + stop_name + stop_lat + stop_lon);
                    sw.WriteLine(text);
                }
            }
        }
    }
}
