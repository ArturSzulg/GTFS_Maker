using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser_GTFS
{
    class Agency
    {
        private const string separator = ",";
        private string agency_id;
        private string agency_name;
        private string agency_url;
        private string agency_timezone = "Europe/Warsaw,";
        private string agency_lang = "pl,";
        private string path;
        public Agency(int new_agency_id,string new_agency_name, string new_agency_url,string fileSavingPath)
        {
            agency_id = new_agency_id.ToString() + separator;
            agency_name = new_agency_name + separator;
            agency_url = new_agency_url;
            path = fileSavingPath + @"\agency.txt";
            WriteAgencyToFile();
        }
        public Agency(string fileSavingPath)
        {
            // TO DO think about more clever solution
            path = fileSavingPath + @"\agency.txt";
            GenerateAgencyFile();
        }

        public bool GenerateAgencyFile()
        {
            try
            {
                if (File.Exists(path))
                {
                    //del existing one
                    File.Delete(path);
                }
                // now create new
                using (FileStream fs = File.Create(path))
                {
                    Byte[] text = new UTF8Encoding(true).GetBytes("agency_id,agency_name,agency_timezone,agency_lang,agency_url" + Environment.NewLine);
                    fs.Write(text, 0, text.Length);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public void WriteAgencyToFile()
        {
            if (!(File.Exists(path)))
            {
                if (!GenerateAgencyFile())
                {
                    Console.WriteLine("Error, cannot make Agency file");
                }
            }

            using (System.IO.StreamWriter fs = new System.IO.StreamWriter(path, true))
            {
                string text = (agency_id.ToString() + agency_name + agency_timezone + agency_lang + agency_url);
                fs.WriteLine(text);

            }
        }

    }
}

     