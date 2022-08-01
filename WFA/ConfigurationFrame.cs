using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WFA
{
    public class ConfigurationFrame
    {
        public int ID;
        public string stationName;
        public int NumberOfPhasors;
        public int NumberOfAnalogValues;
        public string IP;
        public string Longitude;
        public string Latitude;
        public List<string> NamesOfPhasors = new List<string>();
        public List<string> NamesOfAnalogs = new List<string>();
    }
}
