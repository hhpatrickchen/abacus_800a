using HandyControl.Controls;
using Sopdu.Devices.MotionControl.DeltaEtherCAT;
using Sopdu.Devices.MotionControl.IAIController.PConAxis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sopdu.Devices.MotionControl.DeltaController
{
    public class DeltaController : INotifyPropertyChanged, ICloneable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private Thread MonitorThread;
        private Thread CmdSendThread;

        private DeltaEtherCAT deltaEthercat;
        ushort _CardNo;
        ushort _NodeID;
        ushort _SlotNo;
        private DeltaEtherCATAxis _MotorAxis;
        private bool _seqend;

        [XmlIgnore]
        public bool seqend { get { return _seqend; } set { _seqend = value; NotifyPropertyChanged("seqend"); } }

        [XmlIgnore]
        public DeltaEtherCATAxis MotorAxis { get { return _MotorAxis; } set { _MotorAxis = value; NotifyPropertyChanged("MotorAxis"); } }
        public ushort CardNo { get { return _CardNo; } set { _CardNo = value; NotifyPropertyChanged("CardNo"); } }
        public ushort NodeID { get { return _NodeID; } set { _NodeID = value; NotifyPropertyChanged("NodeID"); } }
        public ushort SlotNo { get { return _SlotNo; } set { _SlotNo = value; NotifyPropertyChanged("SlotNo"); } }

        private string _BrakeOP;

        public string BrakeOP { get { return _BrakeOP; } set { _BrakeOP = value; NotifyPropertyChanged("BrakeOP"); } }


        public string DisplayName { get; set; }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public DeltaController()
        {
        }

        public int Init(ushort cardNo, ushort nodeID, ushort slotNo)
        {
            CardNo = cardNo;
            NodeID = nodeID;
            SlotNo = slotNo;
            deltaEthercat = new DeltaEtherCAT(CardNo, NodeID, SlotNo);
            if (deltaEthercat.Open() < 0) return -1;

            //start thread//
            seqend = false;

            MonitorThread = new Thread(new ThreadStart(MonitorThreadFn));
            MonitorThread.Start();
            CmdSendThread = new Thread(new ThreadStart(CmdSendThreadFn));
            CmdSendThread.Start();
            return 1;
        }
        public void Shutdown()
        {
            seqend = true;
            if (MonitorThread != null)
                if (!MonitorThread.Join(5000)) MonitorThread.Abort();
            if (CmdSendThread != null)
                if (!CmdSendThread.Join(5000)) CmdSendThread.Abort();
            Thread.Sleep(1000);
            if (deltaEthercat != null)
            {
                deltaEthercat.Close();
                Thread.Sleep(1000);
            }
        }

        private void MonitorThreadFn()
        { 
        }
        private void CmdSendThreadFn()
        {
        }
    }
}
