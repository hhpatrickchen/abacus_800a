using Sopdu.helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace Sopdu.Devices.IOModule
{
    public class IOController : GenericDevice
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #region Enum Module Type

        public enum IOModuleType
        {
            WAGO,
            BECKHOFF
        }

        # endregion

        public string EMOInputName;
        private DiscreteIO[] ipEMO;//relate to EMOInputName
        private DiscreteIO DS;
        private DiscreteIO SafetyRelay;
        private DiscreteIO PressureSW;
        public IODirectories InputList;
        public IODirectories OutputList;

        public bool isConnected { get { return CheckConnected(); } }
        private bool CheckConnected()
        {
            try
            {
                if (modbus != null)
                    return modbus.connected;
                else
                    return false;
            }
            catch { return false; }
        }
       
        public string IPAddress { get { return _IPAddress; } set { _IPAddress = value; NotifyPropertyChanged(); } }
        private string _IPAddress;//192.168.1.3        
        public int port { get { return _port; } set { _port = value; NotifyPropertyChanged(); } }
        private int _port = 502;//modbus tcp is always 502

        private int _iDoStart;
        private int _iDiStart;
        private int _iDoTotal;
        private int _iDiTotal;

        private Thread IOCmdThread;
        private Thread DisplayThread;
        private GenericEvents mcevent;
        private IOModuleType _ModType;
        public IOModuleType ModType { get { return _ModType; } set { _ModType = value; NotifyPropertyChanged(); } }
        private ModbusTCP.Master modbus;
        private bool terminate;

        public ObservableCollection<DiscreteIO> _InputDisplayList;

        [XmlIgnore]
        public ObservableCollection<DiscreteIO> InputDisplayList { get { return _InputDisplayList; } set { _InputDisplayList = value; NotifyPropertyChanged(); } }

        public ObservableCollection<DiscreteIO> _OutputDisplayList;

        [XmlIgnore]
        public ObservableCollection<DiscreteIO> OutputDisplayList { get { return _OutputDisplayList; } set { _OutputDisplayList = value; NotifyPropertyChanged(); } }

        public void Init()
        {
            terminate = false;
            if (ModType == IOModuleType.BECKHOFF)
            {
                _iDoTotal = 16;
                _iDiTotal = 64;
                _iDiStart = 0;
                _iDoStart = 2048;
            }
            if (ModType == IOModuleType.WAGO)
            {
                _iDoTotal = 16;
                _iDiTotal = 64;
                _iDiStart = 0;
                _iDoStart = 512;
            }
            InputDisplayList = new ObservableCollection<DiscreteIO>();
            if (InputList != null)
                for (int i = 0; i < InputList.IOs.Count; i++)
                {
                    InputDisplayList.Add(new DiscreteIO() { IOName = InputList.IOs[i].IOName });
                }
            OutputDisplayList = new ObservableCollection<DiscreteIO>();
            if (OutputList != null)
                for (int i = 0; i < OutputList.IOs.Count; i++)
                {
                    OutputDisplayList.Add(new DiscreteIO() { IOName = OutputList.IOs[i].IOName });
                }
            //prepare directory; assume io list is in placed
            InputList.Init();
            OutputList.Init();
            if (InputList != null && InputList.IOs.Count > 0)
            {
                ipEMO = new DiscreteIO[2];
                ipEMO[0] = InputList.IpDirectory["Input05"];
                ipEMO[1] = InputList.IpDirectory["Input09"];
                DS = InputList.IpDirectory["Input06"];
                SafetyRelay = InputList.IpDirectory["Input08"];
                PressureSW = InputList.IpDirectory["Input07"];
            }
               
            try
            {
                System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
                System.Net.NetworkInformation.PingReply pingReply = ping.Send(IPAddress, 1000);
                Thread.Sleep(3000);
                if (pingReply.Status == System.Net.NetworkInformation.IPStatus.Success)
                { modbus = new ModbusTCP.Master(IPAddress, (ushort)port); }
                else
                {
                    Thread.Sleep(3000);
                    pingReply = ping.Send(IPAddress, 1000);
                    if (pingReply.Status == System.Net.NetworkInformation.IPStatus.Success)
                    { modbus = new ModbusTCP.Master(IPAddress, (ushort)port); }
                }
            }
            catch (Exception ex) { }
            //start thread

            IOCmdThread = new Thread(new ThreadStart(IOCmdThreadFn));
            IOCmdThread.Start();

            DisplayThread = new Thread(new ThreadStart(DisplayThreadFn));
            DisplayThread.Start();
        }

        public void SetGenericEvent(GenericEvents evt)
        {
            mcevent = evt;
        }

        private void DisplayThreadFn()
        {
            while (!terminate)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
                {
                    try
                    {
                        if (InputDisplayList != null)
                            for (int i = 0; i < InputList.IOs.Count; i++)
                            {
                                InputDisplayList[i].Logic = InputList.IOs[i].Logic;
                            }
                        //if maintainance state activate...allow Output to be manipulated
                        //
                    }
                    catch (Exception ex) { }
                });
                Thread.Sleep(100);
            }
        }

        private void modbus_OnResponseData(ushort id, byte unit, byte function, byte[] data)
        {
            if (id == 201)
            {
                for (int i = 0; i < InputList.IOs.Count; i++)
                {
                    int index = i / 8;
                    int byteindex = i - index * 8;
                    int remainder = (index + 1) % 2;
                    InputList.IOs[i].Logic = (data[index] & (1 << byteindex)) > 0 ? true : false;
                }
            }
        }

        public void Shutdown()
        {
            terminate = true;
            this.DisplayThread.Join();
            this.IOCmdThread.Join();
        }

        private void IOCmdThreadFn()
        {
            byte[] byteOutput = new byte[32];
            for (int i = 0; i < 32; i++)
                byteOutput[i] = 0;
            bool resetwatchdog = true;
            while (!terminate)
            {
                try
                {
                    //log.Debug("Start Cycle");
                    if (modbus.connected)
                    {
                        if (resetwatchdog)
                        {
                            //reset watchdog timer
                            log.Debug("recover output");
                            byte[] resetcode = new byte[] { 190, 207 };
                            modbus.WriteSingleRegister(18, 0, 4385, resetcode);
                            resetcode[0] = 175;
                            resetcode[1] = 254;
                            Thread.Sleep(100);
                            modbus.WriteSingleRegister(18, 0, 4385, resetcode);
                            resetcode[0] = 0;
                            resetcode[1] = 0;
                            Thread.Sleep(100);
                            modbus.WriteSingleRegister(18, 0, 4385, resetcode);
                            Thread.Sleep(100);
                            //end of watchdog reset
                            //output write process
                            resetwatchdog = false;
                        }
                        // Write Output
                        _iDoTotal = OutputList.IOs.Count;
                        int singleopbyte = 0;
                        for (int i = 0; i < OutputList.IOs.Count; i++)
                        {
                            int index = i / 8;
                            int byteindex = i - index * 8;
                            if (OutputList.IOs[i].Logic == true)
                            {
                                int maskbyte = 1 << byteindex;
                                singleopbyte = singleopbyte | maskbyte;
                            }
                            if (byteindex == 7)//1 complete byte
                            {
                                //index check
                                int remainder = (index + 1) % 2;

                                if (remainder != 1)
                                    byteOutput[index - 1] = (byte)singleopbyte;
                                else
                                    byteOutput[index + 1] = (byte)singleopbyte;
                                singleopbyte = 0;
                            }
                        }
                        byte[] rst = new byte[32];
                        modbus.WriteMultipleRegister((ushort)200, (byte)1, (ushort)_iDoStart, byteOutput, ref rst);
                        if (rst==null)
                        {
                            log.Debug("write result is null");

                            modbus.connect(IPAddress, (ushort)port);

                        }
                        // Read Input

                        byte[] data = new byte[32];
                        if (InputList != null && InputList.IOs.Count > 0)
                        {
                            _iDiTotal = InputList.IOs.Count;
                            modbus.ReadDiscreteInputs((ushort)201, 1, (ushort)_iDiStart, (ushort)_iDiTotal, ref data);
                            if (data == null)
                            {
                                log.Debug("read result is null");

                                modbus.connect(IPAddress, (ushort)port);
                            }
                            for (int i = 0; i < InputList.IOs.Count; i++)
                            {
                                int index = i / 8;
                                int byteindex = i - index * 8;
                                int remainder = (index + 1) % 2;
                                InputList.IOs[i].Logic = (data[index] & (1 << byteindex)) > 0 ? true : false;
                            }
                        }
                        Thread.Sleep(100);
                    }
                    else
                    {
                        log.Debug("connection lost");
                        modbus.connect(IPAddress, (ushort)port);
                        resetwatchdog = true;
                    }
                }
                catch (Exception ex)
                {
                    Thread.Sleep(100);
                    try
                    {
                        log.Debug("send error");
                        string sMsg = "Modbus TCP IP Address:" + IPAddress + " Reconnect!";
                        EventLog(sMsg);
                        resetwatchdog = true;
                        modbus.connect(IPAddress, (ushort)port);
                    }
                    catch (Exception ex1) { Thread.Sleep(100); }
                }
                Thread.Sleep(1);
            }
        }

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        private void EventLog(string sMsg)
        {
            MyLib.DataLog dataLog = new MyLib.DataLog();
            dataLog.LogEvent(sMsg);
        }
    }
}