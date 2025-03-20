using Cognex.VisionPro;
using Sopdu.UI;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for TrayPositionUI.xaml
    /// </summary>
    public partial class TrayPositionUI : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private UsbCamera _cm;
        public UsbCamera cm { get { return _cm; } set { _cm = value; NotifyPropertyChanged("cm"); } }

        public TrayPositionUI()
        {
            InitializeComponent();
        }

        public void Init(UsbCamera camera)
        {
            cm = camera;
            try
            {
                if (cm.tbFirstLocation != null)
                    cm.tbFirstLocation.Ran += tbFirstLocation_Ran;
            }
            catch { }
            DataContext = cm;
        }

        private void tbFirstLocation_Ran(object sender, EventArgs e)
        {
            ICogRecord topRecord = cm.tbFirstLocation.CreateLastRunRecord();
            ICogRecord displayrecord = topRecord.SubRecords["CogCaliperTool1.InputImage"];
            trayposdisplay.Record = displayrecord;
            trayposdisplay.Fit(true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            frmtoolgroup frm = new frmtoolgroup();
            frm.SetSubject(cm.tbFirstLocation, (CogImage8Grey)cm.tbFirstLocation.Inputs[0].Value, null, null);
            frm.ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            UsbCamera.SaveFile(@".\IPCamera.xml", cm);
        }
    }
}