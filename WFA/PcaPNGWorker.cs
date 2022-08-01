using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PcapngUtils;
using PcapngUtils.Common;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WFA;

namespace PcaPNGTestV2
{
    public class PcaPNGWorker
    {
        public string filename;
        public CancellationToken token;
        public List<Packet> packets = new List<Packet>();
        public List<ConfigurationFrame> configrationFrames = new List<ConfigurationFrame>();

        public PcaPNGWorker(string filename, CancellationToken token)
        {
            this.filename = filename;
            this.token = token;
        }


        public void OpenPcapORPcapNFFile()
        {
            using (var reader = IReaderFactory.GetReader(filename))
            {
                reader.OnReadPacketEvent += reader_OnReadPacketEvent;
                reader.ReadPackets(token);
                reader.OnReadPacketEvent -= reader_OnReadPacketEvent;
            }
        }
        public void reader_OnReadPacketEvent(object context, IPacket packet)
        {
            Console.WriteLine(string.Format("Packet received {0}.{1}", packet.Seconds, packet.Microseconds));
            if (packet.Data[42] == 170 && packet.Data[43] == 49)
            {
                ConfigureWorker(packet);
            }
            else if (null != configrationFrames.FirstOrDefault(x => x.ID == packet.Data[47]))
            {
                ParsePacket(packet);
            }

        }

        public async void ConfigureWorker(IPacket packet)
        {
            var stationName = TranslateName(packet, 62, 78);
            var NumberOfPhasors = packet.Data[(int)BitParserEnum.NUM_OF_PHASORS]+ packet.Data[(int)BitParserEnum.NUM_OF_PHASORS_END];
            var NumberOfAnalogValues = packet.Data[(int)BitParserEnum.NUM_OF_ANALOG_VALUE] + packet.Data[(int)BitParserEnum.NUM_OF_ANALOG_VALUE_END];
            var NamesOfPhasors = new List<string>();
            for (int i=0; i<NumberOfPhasors; i++)
            {
                NamesOfPhasors.Add(TranslateName(packet, (int)BitParserEnum.PHASOR_NAMES+ i*(int)BitParserEnum.SHIFT_16_BYTES, (int)BitParserEnum.PHASOR_NAMES_END+ i*(int)BitParserEnum.SHIFT_16_BYTES));
            }
            var NamesOfAnalogs = new List<string>();
            for (int i = 0; i < NumberOfAnalogValues; i++)
            {
                NamesOfAnalogs.Add(TranslateName(packet, (int)BitParserEnum.PHASOR_NAMES + i * (int)BitParserEnum.SHIFT_16_BYTES+NumberOfPhasors*(int)BitParserEnum.SHIFT_16_BYTES,
                    (int)BitParserEnum.PHASOR_NAMES_END + i * (int)BitParserEnum.SHIFT_16_BYTES + NumberOfPhasors * (int)BitParserEnum.SHIFT_16_BYTES));
            }
            var ID = packet.Data[47];

            var IP = GetSourceIP(packet, 26, 29);

            var client = new HttpClient();
            var httpResponse = client.GetAsync("https://ipinfo.io/"+IP+"?token=a060072a3ce4be");
            httpResponse.Wait();
            var data = await httpResponse.Result.Content.ReadAsStringAsync();
            dynamic finalData = JsonConvert.DeserializeObject<ExpandoObject>(data, new ExpandoObjectConverter());
            string locationData = finalData.loc;
            string[] locationArray = locationData.Split(',');


            if (!configrationFrames.Any(x=>x.ID == ID))
            {
                configrationFrames.Add(new ConfigurationFrame
                {
                    ID = ID,
                    stationName = stationName,
                    NumberOfPhasors = NumberOfPhasors,
                    NumberOfAnalogValues = NumberOfAnalogValues,
                    NamesOfAnalogs = NamesOfAnalogs,
                    NamesOfPhasors = NamesOfPhasors,
                    Latitude = locationArray[0],
                    Longitude = locationArray[1]
                });
            }
        }
        
        public void ParsePacket(IPacket packet)
        {
            var currentConfFrame = configrationFrames.FirstOrDefault(x => x.ID == packet.Data[47]);
            DateTime packetSendTime = CalculateTime(packet, 48, 51);
            List<Phasor> phasors = new List<Phasor>();
            for (int i = 0; i < currentConfFrame.NumberOfPhasors; i++)
            {
                phasors.Add(new Phasor(currentConfFrame.NamesOfPhasors[i], Calculate32BitFloatingPoint(packet, 58 + 8 * i, 61 + 8 * i), Calculate32BitFloatingPoint(packet, 62 + 8 * i, 65 + 8 * i) * 180 / Math.PI));
            }
            var frequency = Calculate32BitFloatingPoint(packet, 58 + 8 * currentConfFrame.NumberOfPhasors, 61 + 8 * currentConfFrame.NumberOfPhasors);
            var rateOfChangeOfFrequency = Calculate32BitFloatingPoint(packet, 58 + 8 * currentConfFrame.NumberOfPhasors + 4, 61 + 8 * currentConfFrame.NumberOfPhasors + 4);
            List<Analog> analogs = new List<Analog>();
            for (int i = 0; i < currentConfFrame.NumberOfAnalogValues; i++)
            {
                analogs.Add(new Analog(currentConfFrame.NamesOfAnalogs[i], Calculate32BitFloatingPoint(packet, 58 + 8 * currentConfFrame.NumberOfPhasors + 8 + i * 4, 61 + 8 * currentConfFrame.NumberOfPhasors + 8 + i * 4)));
            }
            var finalPacket = new Packet(phasors, frequency, rateOfChangeOfFrequency, analogs, packetSendTime, currentConfFrame.stationName);
            packets.Add(finalPacket);
        }

        public string TranslateName(IPacket packet, int start, int end)
        {
            string name = string.Empty;
            for (int i= start; i < end; i++)
            {
                var temp = Convert.ToChar(packet.Data[i]);
                if (temp.ToString() != "\0")
                {
                    name += temp;
                }
                
            }
            return name;
        }
        
        public double Calculate32BitFloatingPoint(IPacket packet, int start, int end)
        {
            string binary = "";
            for (int i = start; i <= end; i++)
            {
                var temp = ConvertToBinary(packet.Data[i]);
                if (temp.Length < 8)
                {
                    temp = String.Concat(Enumerable.Repeat("0", (8 - temp.Length))) + temp;
                }
                binary += temp;
            }
            if (binary.Length < 32)
            {
                binary = binary + String.Concat(Enumerable.Repeat("0", (32 - binary.Length)));
            }
            return ConvertFloatingPointToDouble(binary);
        }


        public string ConvertToBinary(int numberToConvert)
        {
            return Convert.ToString(numberToConvert, 2);
        }

        public double ConvertFloatingPointToDouble(string binary)
        {
            uint fb = Convert.ToUInt32(binary, 2);
            double ret = BitConverter.ToSingle(BitConverter.GetBytes(fb), 0);
            return ret;
        }

        public string GetSourceIP(IPacket packet, int start, int end)
        {
            string IP = string.Empty;
            for (int i = start; i <= end; i++)
            {
                IP += packet.Data[i].ToString()+".";

            }
            return IP.Remove(IP.Length - 1);
        }

        public DateTime CalculateTime(IPacket packet, int start, int end)
        {
            string hexValue = string.Empty;
            for (int i=start; i <= end; i++)
            {
                hexValue += packet.Data[i].ToString("X");
            }
            long secondsValue = long.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
            hexValue = string.Empty;
            for (int i = start+5; i <= end+4; i++)
            {
                hexValue += packet.Data[i].ToString("X");
            }
            long milisecondsValue = long.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
            var date = (new DateTime(1970, 1, 1)).AddSeconds(secondsValue).AddMilliseconds(milisecondsValue);
            return date;
        }
    }
}
