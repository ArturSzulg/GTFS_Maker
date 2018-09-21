using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser_GTFS
{
    class Calendar
    {
        private const string separator = ",";
        private string service_id;
        private string monday;
        private string tuesday;
        private string wednesday;
        private string thursday;
        private string friday;
        private string saturday;
        private string sunday;
        private string start_date;
        private string end_date;
        private string path;

        public Calendar(string new_service_id, string new_start_date, string new_end_date, string new_monday, string new_tuesday, string new_wednesday, string new_thursday, string new_friday, string new_saturday, string new_sunday,  string fileSavingPath)
        {
            service_id = new_service_id + separator;
            start_date = new_start_date + separator;
            end_date = new_end_date + separator;
            monday = new_monday + separator;
            tuesday = new_tuesday + separator;
            wednesday = new_wednesday + separator;
            thursday = new_thursday + separator;
            friday = new_friday + separator;
            saturday = new_saturday + separator;
            sunday = new_sunday;
            path = fileSavingPath + @"\calendar.txt";
            WriteCalendarToFile();
        }

        public Calendar(string fileSavingPath)
        {
            // TO DO think about more clever solution
            path = fileSavingPath + @"\calendar.txt";
            GenerateCalendarFile();
        }
        public bool GenerateCalendarFile()
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                using (FileStream fs = new FileStream(path, FileMode.CreateNew))
                {
                    Byte[] text = new UTF8Encoding(true).GetBytes("service_id,start_date,end_date,monday,tuesday,wednesday,thursday,friday,saturday,sunday" + Environment.NewLine);
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

        public void WriteCalendarToFile()
        {
            if (!(File.Exists(path)))
            {
                if (!GenerateCalendarFile())
                {
                    Console.WriteLine("Error, cannot make Calendar file");
                }
            }
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path, true))
            {
                string text = (service_id + start_date + end_date + monday + tuesday + wednesday + thursday + friday + saturday + sunday);
                sw.WriteLine(text);
            }
        }

    }
}
