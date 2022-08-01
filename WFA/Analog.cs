using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcaPNGTestV2
{
    public class Analog
    {
        public string Name { get; set; }
        public double Value { get; set; }

        public Analog (string name, double value)
        {
            Name = name; Value = value; 
        }
    }
}
