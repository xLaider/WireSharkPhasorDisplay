using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcaPNGTestV2
{
    public class Phasor
    {
        public string Name { get; set; }
        public double Magnitude { get; set; }
        public double Angle { get; set; }

        public Phasor(string name, double magnitude, double angle)
        {
            Name = name;
            Magnitude = magnitude;
            Angle = angle;
        }
    }
}
