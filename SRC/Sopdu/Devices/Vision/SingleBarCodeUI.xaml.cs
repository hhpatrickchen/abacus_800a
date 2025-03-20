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
    /// Interaction logic for SingleBarCodeUI.xaml
    /// </summary>
    public partial class SingleBarCodeUI : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public SingleBarCodeUI()
        {
            InitializeComponent();
        }

        private UsbCamera _cm;
        public UsbCamera cm { get { return _cm; } set { _cm = value; NotifyPropertyChanged("cm"); } }

        public void Init(UsbCamera camera)
        {
            cm = camera;
            try
            {
                if (cm.tbSingleID != null)
                    cm.tbSingleID.Ran += tbSingleID_Ran;
            }
            catch { }            
            DataContext = cm;
        }

        private void tbSingleID_Ran(object sender, EventArgs e)
        {
            ICogRecord topRecord = cm.tbSingleID.CreateLastRunRecord();
            ICogRecord displayrecord = topRecord.SubRecords["CogIPOneImageTool1.OutputImage"];//CogIPOneImageTool1
            singlebcrdisplay.Record = displayrecord;
            singlebcrdisplay.Fit(true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            frmtoolgroup frm = new frmtoolgroup();
            frm.SetSubject(cm.tbSingleID, (CogImage8Grey)cm.tbSingleID.Inputs[0].Value, null, null);
            frm.ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            UsbCamera.SaveFile(@".\IPCamera.xml", cm);
        }
    }
}