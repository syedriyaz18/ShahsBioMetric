using System;

namespace ShahsBioMetric
{
    public class MachineInfo
    {
        public int MachineNumber { get; set; }
        public int IndRegID { get; set; }
        public string DateTimeRecord { get; set; }

        public DateTime DateOnlyRecord
        {
            //get { return DateTime.Parse(DateTime.Parse(DateTimeRecord).ToString("dd/MM/yyyy")); }
            get { return DateTime.Parse(DateTimeRecord).Date; }
        }
        public DateTime TimeOnlyRecord
        {
            get { return DateTime.Parse(DateTime.Parse(DateTimeRecord).ToString("hh:mm:ss tt")); }
        }

    }
}