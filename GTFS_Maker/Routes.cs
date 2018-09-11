using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser_GTFS
{
    class Route
    {
        private const string separator = ",";
        private string route_id;
        private string agency_id;
        private string route_short_name;
        private string route_long_name;
        private string route_type;
        private string path;
        public Route(string new_route_id, string new_agency_id, string new_route_short_name, string new_route_long_name, string new_route_type, string fileSavingPath)
        {
            route_id = new_route_id + separator;
            agency_id = new_agency_id + separator;
            route_short_name = "\"" + new_route_short_name + "\"" + separator;
            route_long_name = "\"" + new_route_long_name + "\"" + separator;
            route_type = new_route_type;
            path = fileSavingPath + @"\routes.txt";
            WriteRouteToFile();
        }

        public bool GenerateRoutesFile()
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                using (FileStream fs = new FileStream(path, FileMode.CreateNew))
                {

                    Byte[] text = new UTF8Encoding(true).GetBytes("route_id,agency_id,route_short_name,route_long_name,route_type" + Environment.NewLine);
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

        public void WriteRouteToFile()
        {
            if (!(File.Exists(path)))
            {
                if (!GenerateRoutesFile())
                {
                    Console.WriteLine("Error, cannot make Routes file");
                }
            }
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true))
            {
                string text = (route_id + agency_id + route_short_name + route_long_name + route_type);
                sw.WriteLine(text);
            }
        }
    }
}
