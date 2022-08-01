using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PcaPNGTestV2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace WFA
{
    public partial class Form1 : Form
    {
        public static IList<Packet> packetsToDisplay = new List<Packet>();
        PcaPNGWorker pcaPNGWorker;
        private Thread thread;
        private Thread thread1;
        private string selectedStation = string.Empty;
        private string selectedPhasor = string.Empty;
        TimeSpan ts = new TimeSpan (10000);
        public Form1()
        {
            InitializeComponent();

        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            thread1 = new Thread(timer);
            thread1.Start();

        }
        public void timer()
        {
            trackBar1.Invoke((MethodInvoker)(() => ts = TimeSpan.FromMilliseconds(trackBar1.Value)));
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            AngleChart.Series.Clear();
            AnalogChart.Series.Clear();
            if (thread != null)
            {
                thread.Abort();
            }
            phasorsList.SelectedItems.Clear();
            if (phasorsList.SelectedItem == null)
            {
                btnStart.Enabled = false;
            }
            selectedStation = listBox1.SelectedItem.ToString();
            var tempPacket = packetsToDisplay.FirstOrDefault(x=>x.StationName == selectedStation);
            List<string> phasorNames = new List<string>();
            foreach (var phasor in tempPacket.Phasors)
            {
                phasorNames.Add(phasor.Name);
            }
            phasorsList.Items.Clear();
            
            foreach (var name in phasorNames)
            {
                phasorsList.Items.Add(name);
            }
            var marker = MainMap.Overlays.FirstOrDefault().Markers.FirstOrDefault(x=>x.ToolTipText == selectedStation);
            MainMap.Position = new PointLatLng(marker.Position.Lat, marker.Position.Lng);


        }
        private void runSimulation()
        {
            int counter = 0;
            for (int i = 0; i < packetsToDisplay.Count; i++)
            {
                counter++;
                if (packetsToDisplay[i].StationName == selectedStation)
                {
                    var currentTime = DateTime.Now;
                    Thread.Sleep(ts);
                    chart1.Invoke(new Action(delegate ()
                    {
                        chart1.Series[selectedPhasor].Points.Clear();
                        Phasor phasorToDisplay = packetsToDisplay[i].Phasors.FirstOrDefault(x => x.Name == selectedPhasor);
                        chart1.Series[selectedPhasor].Points.AddXY(phasorToDisplay.Angle, phasorToDisplay.Magnitude);
                    }));
                    textBox1.Invoke(new Action(delegate ()
                    {
                        var text = "Station name: " + packetsToDisplay[i].StationName + "\r\n"
                        + "Packet send time: " + packetsToDisplay[i].PacketSendTime + "\r\n"
                        + "Actual frequency: " + packetsToDisplay[i].ActualFrequency + "\r\n"
                        + "Frequency change rate: " + packetsToDisplay[i].RateOfChangeOfFrequency + "\r\n";
                        foreach (var analog in packetsToDisplay[i].Analogs)
                        {
                            text = text + "Analog " + analog.Name + " value: " + analog.Value + "\r\n";
                        }
                        textBox1.Text = (text);
                    }));
                    if(counter > 25) { 
                    AnalogChart.Invoke(new Action(delegate ()
                    {
                        foreach(var series in AnalogChart.Series){
                            series.Values.Add(packetsToDisplay[i].Analogs.FirstOrDefault(x => x.Name == series.Title).Value);
                            if(series.Values.Count == 50)
                            series.Values.RemoveAt(0);
                            counter = 0;
                        }
                    }));
                    AngleChart.Invoke(new Action(delegate ()
                        {
                            foreach (var series in AngleChart.Series)
                            {
                                series.Values.Add(packetsToDisplay[i].Phasors.FirstOrDefault(x => x.Name == series.Title).Angle);
                                if (series.Values.Count == 50)
                                    series.Values.RemoveAt(0);
                                counter = 0;
                            }
                        }));
                    }
                }
                

            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            thread = new Thread(runSimulation);
            chart1.Series.Clear();
            chart1.Series.Add(selectedPhasor);
            chart1.Series[selectedPhasor].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Polar;
            chart1.Series[selectedPhasor].MarkerSize = 15;
            chart1.Series[selectedPhasor].MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;
            thread.Start();
        }

        private void phasorsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (phasorsList.SelectedItem != null)
            {
                btnStart.Enabled = true;
            }
            if (phasorsList.SelectedItem != null)
            {
                selectedPhasor = phasorsList.SelectedItem.ToString();
            }
            SeriesCollection series = new SeriesCollection();
            SeriesCollection series2 = new SeriesCollection();
            foreach (var analog in pcaPNGWorker.configrationFrames.FirstOrDefault(x => x.stationName == selectedStation).NamesOfAnalogs){
                series.Add(
                    new LineSeries
                    {
                        Title = analog,
                        Values = new ChartValues<double> { }
                    }
                );
            }
            foreach (var phasor in pcaPNGWorker.configrationFrames.FirstOrDefault(x => x.stationName == selectedStation).NamesOfPhasors)
            {
                series2.Add(
                    new LineSeries
                    {
                        Title = phasor,
                        Values = new ChartValues<double> { }
                    }
                );
            }
            AnalogChart.Series.AddRange(series);
            AngleChart.Series.AddRange(series2);
        }

        private async void btnSelectFile_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            phasorsList.Items.Clear();
            Thread t = new Thread((ThreadStart)(() => {
                OpenFileDialog saveFileDialog1 = new OpenFileDialog();
                saveFileDialog1.Filter = "Pacapng|*.pcapng;*.pca";
                saveFileDialog1.RestoreDirectory = true;
                
       
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    var filePath = saveFileDialog1.FileName;
                    pcaPNGWorker = new PcaPNGWorker(filePath, new CancellationToken());
                    pcaPNGWorker.OpenPcapORPcapNFFile();
                    packetsToDisplay = pcaPNGWorker.packets;
                    
                }
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            
            foreach (var configurationFrame in pcaPNGWorker.configrationFrames)
            {
                this.listBox1.Items.Add(configurationFrame.stationName);
            }

            if (phasorsList.SelectedItem == null)
            {
                btnStart.Enabled = false;
            }
            GMapOverlay markers = new GMapOverlay("markers");
            foreach (var frame in pcaPNGWorker.configrationFrames)
            {

                
                    double longitude = double.Parse(frame.Longitude, CultureInfo.InvariantCulture);
                    double latitude = double.Parse(frame.Latitude, CultureInfo.InvariantCulture);


                    GMapMarker marker = new GMarkerGoogle(
                    new PointLatLng(latitude, longitude),
                    GMarkerGoogleType.blue_pushpin);
                    markers.Markers.Add(marker);
                    marker.ToolTipText = frame.stationName;
                    

            }
            MainMap.Overlays.Add(markers);
            MainMap.ZoomAndCenterMarkers(MainMap.Overlays.FirstOrDefault().Id);

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            MainMap.MapProvider = GMap.NET.MapProviders.BingMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;
            MainMap.Position = new PointLatLng(52.237049, 21.017532);
            MainMap.ShowCenter = false;
        }

        private void MainMap_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            
        }

        private void zoomIn_Click(object sender, EventArgs e)
        {
            if (MainMap.Zoom < 18) MainMap.Zoom++;
        }

        private void zoomOut_Click(object sender, EventArgs e)
        {
            if (MainMap.Zoom > 2) MainMap.Zoom--;
        }
    }
}
