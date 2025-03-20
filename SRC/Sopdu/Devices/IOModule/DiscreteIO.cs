using Sopdu.helper;
using System.Threading;
using System.Xml.Serialization;

namespace Sopdu.Devices.IOModule
{
    public class DiscreteIO : GenericDevice
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ProcessMode pMode;

        public DiscreteIO()
        {
            evtOn = new ManualResetEvent(false);
            evtOff = new ManualResetEvent(false);
            Logic = false;
        }

        private string _IOName;

        public string IOName
        { get { return _IOName; } set { _IOName = value; } }


        private string _DeviceID;

        public string DeviceID
        { get { return _DeviceID; } set { _DeviceID = value; } }


        private string _DisplayName;

        public string DisplayName
        { get { return _DisplayName; } set { _DisplayName = value; } }

        public override string Name
        {
            get { return IOName; }
        }

        public string ShowID
        {
            get {
                var result = IOName;
                if (!string.IsNullOrEmpty(DeviceID)) 
                {
                    result = $"{result} [{DeviceID}]";
                } 

                return result;
            }
        }

        public void SetOutput(bool logic)
        {
            //pMode.ChkProcessMode();
            Logic = logic;
        }

        private bool _Logic;

        [XmlIgnore]
        public bool Logic
        {
            get { return _Logic; }
            set
            {
                _Logic = value;
                if (_Logic == true)
                {
                    evtOff.Reset();
                    evtOn.Set();
                }
                else
                {
                    evtOff.Set();
                    evtOn.Reset();
                } /*update events*/
                NotifyPropertyChanged();
            }
        }

        private ManualResetEvent _evtOn;

        [XmlIgnore]
        public ManualResetEvent evtOn { get { return _evtOn; } set { _evtOn = value;/*update events*/ } }

        private ManualResetEvent _evtOff;

        [XmlIgnore]
        public ManualResetEvent evtOff { get { return _evtOff; } set { _evtOff = value;/*update events*/ } }
    }
}