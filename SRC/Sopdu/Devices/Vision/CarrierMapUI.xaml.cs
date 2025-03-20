using Cognex.VisionPro;
using Cognex.VisionPro.ID;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.ToolBlock;
using Sopdu.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sopdu.Devices.Vision
{
    /// <summary>
    /// Interaction logic for CarrierMapUI.xaml
    /// </summary>
    public partial class CarrierMapUI : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _avePitch;
        public string avePitch { get { return _avePitch; } set { _avePitch = value; NotifyPropertyChanged("avePitch"); } }
        private string _minPitch;
        public string minPitch { get { return _minPitch; } set { _minPitch = value; NotifyPropertyChanged("minPitch"); } }
        private string _maxPitch;
        public string maxPitch { get { return _maxPitch; } set { _maxPitch = value; NotifyPropertyChanged("maxPitch"); } }
        private ObservableCollection<string> _pitch;
        public ObservableCollection<string> pitch { get { return _pitch; } set { _pitch = value; NotifyPropertyChanged("pitch"); } }

        private ObservableCollection<string> _position;
        public ObservableCollection<string> position { get { return _position; } set { _position = value; NotifyPropertyChanged("position"); } }

        private ObservableCollection<string> _trayidlist;
        public ObservableCollection<string> trayidlist { get { return _trayidlist; } set { _trayidlist = value; NotifyPropertyChanged("trayidlist"); } }

        private UsbCamera _cm;
        public UsbCamera cm { get { return _cm; } set { _cm = value; NotifyPropertyChanged("cm"); } }

        public CarrierMapUI()
        {
            InitializeComponent();
        }

        public void Init(UsbCamera camera)
        {
            cm = camera;
            this.toolblock = cm.tbCarrierTrayMap;
            try
            {
                if (toolblock != null)
                    toolblock.Ran += toolblock_Ran;
            }
            catch { }
            DataContext = this;
        }

        private void toolblock_Ran(object sender, EventArgs e)
        {
            ICogRecord topRecord = toolblock.CreateLastRunRecord();
            ICogRecord displayrecord = topRecord.SubRecords["CogIPOneImageTool1.OutputImage"];//CogIPOneImageTool1
            carriermapdisplay.Record = displayrecord;
            carriermapdisplay.Fit(true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (toolblock != null)
            {
                frmtoolgroup frm = new frmtoolgroup();
                frm.SetSubject(toolblock, (CogImage8Grey)toolblock.Inputs[0].Value, null, null);
                frm.ShowDialog();
                //            toolblock.Run();
            }
            else
                MessageBox.Show("Tool Block was not found, please check Camera Connect status!");
        }

        public CogToolBlock toolblock { get; set; }

        private void trayposgrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void trayidgrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void traypitchgrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            UsbCamera.SaveFile(@".\IPCamera.xml", cm);
        }
    }
}