using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PcaPNGTestV2
{
    public class Packet
    {
        public string StationName { get; set; }
        public List<Phasor> Phasors { get; set; }
        public double ActualFrequency { get; set; } 
        public double RateOfChangeOfFrequency { get; set; }
        public List<Analog> Analogs { get; set; }
        public DateTime PacketSendTime { get; set; }
        public Packet(List<Phasor> phasors,double actualFreq, double rateOfChange, List<Analog> analogs, DateTime packetSendTime, string stationName)
        {
            Phasors = phasors;
            ActualFrequency = actualFreq;
            RateOfChangeOfFrequency = rateOfChange;
            Analogs = analogs;
            PacketSendTime = packetSendTime;
            StationName = stationName;
        }
        

  
        
    }
}
