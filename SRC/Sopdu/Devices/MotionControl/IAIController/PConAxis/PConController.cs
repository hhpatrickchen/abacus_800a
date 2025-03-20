using Sopdu.Devices.MotionControl.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sopdu.Devices.MotionControl.IAIController.PConAxis
{
    public class PConController : INotifyPropertyChanged, ICloneable
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
        private PconModbus pcon;
        private string _Comport;
        private PconControllerAxis _MotorAxis;
        private bool _seqend;

        [XmlIgnore]
        public bool seqend { get { return _seqend; } set { _seqend = value; NotifyPropertyChanged("seqend"); } }

        [XmlIgnore]
        public PconControllerAxis MotorAxis { get { return _MotorAxis; } set { _MotorAxis = value; NotifyPropertyChanged("MotorAxis"); } }
        public string Comport { get { return _Comport; } set { _Comport = value; NotifyPropertyChanged("Comport"); } }

        private string _BrakeOP;

        public string BrakeOP { get { return _BrakeOP; } set { _BrakeOP = value; NotifyPropertyChanged("BrakeOP"); } }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public PConController()
        {
        }

        public int Init(string Cm = null)
        {
            if (Cm != null) Comport = Cm;
            pcon = new PconModbus(Comport);
            if (pcon.Open()<0) return -1;

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
            if (pcon != null)
            {
                pcon.Close();
                Thread.Sleep(1000);
            }
        }

        public void SetPConInit()
        {
            //by default comand poll while loop off
            MotorAxis.SetModBusOn();
            while (!MotorAxis.CommandCompleteEvent.WaitOne(50))//execute until command rx is cmd send if command is none; sendmsg will still remain the same
            {
                if (this.seqend) break;
                ExecuteCommand(MotorAxis, MotorAxis.Command);
                if (MotorAxis.sendmsg == MotorAxis.Rxmsg)
                {
                    MotorAxis.CommandCompleteEvent.Set(); MotorAxis.Command = AxisCommand.None;
                }
            }
            MotorAxis.bModbusActive = true;
            MotorAxis.Command = AxisCommand.None;
            MotorAxis.CommandCompleteEvent.Reset();
            MotorAxis.CommandDoneEvent.Reset();
            MotorAxis.CommandSetEvent.Reset();
            string message = String.Format("{0:X2}{1:X4}{2:X4}", (byte)PconModbus.Function.ReadHoldingRegisters, (ushort)PconModbus.Register.CurrentPositionMonitor, 10);
            int result = pcon.Write(MotorAxis.AxisNumber, message, out message);
            //wait command poll on
            MotorAxis.SkipcommandCompleteEvent.Reset();
            MotorAxis.SkipCommandEvent.Reset();
        }

        //Axis.....SetPConDiscon()
        //IO Sequence
        //...
        //,,
        //Axis --- SetPConInit()<--


        public void SetPConDicon()//in process sequence activate this
        {
            //set command poll while loop off
            MotorAxis.SkipCommandEvent.Set();
            //wait command poll while loop off
            if (!MotorAxis.SkipcommandCompleteEvent.WaitOne(500))
            //while(!MotorAxis.SkipcommandCompleteEvent.WaitOne(500))
            {
                log.Error("Unable to skip polling command");
                throw new Exception("Unable to skip polling command");
                
            }

            MotorAxis.SetModBusOff();
            while (!MotorAxis.CommandCompleteEvent.WaitOne(50))//execute until command rx is cmd send if command is none; sendmsg will still remain the same
            {
                if (this.seqend) break;
                ExecuteCommand(MotorAxis, MotorAxis.Command);
                if (MotorAxis.sendmsg == MotorAxis.Rxmsg)
                {
                    MotorAxis.CommandCompleteEvent.Set(); MotorAxis.Command = AxisCommand.None;
                }
            }
            MotorAxis.bModbusActive = false;
            MotorAxis.Command = AxisCommand.None;
            MotorAxis.CommandCompleteEvent.Reset();
            MotorAxis.CommandDoneEvent.Reset();
            MotorAxis.CommandSetEvent.Reset();
            //string message = String.Format("{0:X2}{1:X4}{2:X4}", (byte)PconModbus.Function.ReadHoldingRegisters, (ushort)PconModbus.Register.CurrentPositionMonitor, 10);
            //int result = pcon.Write(MotorAxis.AxisNumber, message, out message);
        }

        private void CmdSendThreadFn()
        {
            //  throw new NotImplementedException();
            //first command send
            //:01050427FF00D0
            //first startup
            bool reconnect = true;
            int numofretry = 0;
            while (reconnect)
            {
                //1 modbus connect using SetPconInit()
                //2 read motor current positon to indicate modbus connected

                if (numofretry > 10) break;

                SetPConInit();
                //update status cmds
                int i = 0;

                while (MotorAxis.RawStatus == null)
                {
                    //disable this when disable modbus
                    string message1 = String.Format("{0:X2}{1:X4}{2:X4}", (byte)PconModbus.Function.ReadHoldingRegisters, (ushort)PconModbus.Register.CurrentPositionMonitor, 10);
                    int result1 = pcon.Write(MotorAxis.AxisNumber, message1, out message1);
                    Thread.Sleep(100);
                    i++;
                    if (i > 5) break;
                }
                if (MotorAxis.RawStatus != null)
                {
                    reconnect = false;
                    break;
                }
                numofretry++;
            }
            while (!seqend)
            {

                try
                {

                    //off the loop
                    #region toTurnOffWhenMobusNotOn
                    if (MotorAxis.CommandSetEvent.WaitOne(100) == true)
                    {
                        // MotorAxis.Rxmsg = "";
                        //ExecuteCommand(MotorAxis, MotorAxis.Command);
                        while (!MotorAxis.CommandCompleteEvent.WaitOne(100))//execute until command rx is cmd send if command is none; sendmsg will still remain the same
                        {
                            if (this.seqend) throw new Exception();
                            try
                            {
                                ExecuteCommand(MotorAxis, MotorAxis.Command);//righfully when modbus discon.... 
                                Thread.Sleep(100);
                            }
                            catch (Exception ex) { }
                        }
                        //Axis ax = new Axis();

                        MotorAxis.Command = AxisCommand.None;
                        MotorAxis.CommandCompleteEvent.Reset();
                        MotorAxis.CommandSetEvent.Reset();
                    }
                    else
                    {
                        // do a try catch here

                        //update status cmds
                        string message = String.Format("{0:X2}{1:X4}{2:X4}", (byte)PconModbus.Function.ReadHoldingRegisters, (ushort)PconModbus.Register.CurrentPositionMonitor, 10);
                        int result = pcon.Write(MotorAxis.AxisNumber, message, out message);
                        
                        //end of try catch
                    }
                    #endregion
                    //end of off the loop

                    //turn off sequence loop
                    bool setskipeventrx = false;                    
                    while(!seqend)
                    {
                        if (MotorAxis.SkipCommandEvent.WaitOne(0))
                        {
                            Thread.Sleep(100);
                            if (!setskipeventrx)
                            {
                                MotorAxis.SkipcommandCompleteEvent.Set();
                                setskipeventrx = true;
                            }
                        }
                        else 
                            break;

                    }
                }
                catch (Exception ex)
                { }
                Thread.Sleep(50);
            }
        }

        private void MonitorThreadFn()
        {
            //throw new NotImplementedException();
            byte axis = 0;
            object value;
            string message;
            string msgheader;
            while (!seqend)
            {
                Thread.Sleep(100);
                int i =  pcon.Read(out message);
                if (i != 0) continue;
                try
                {
                    msgheader = message.Substring(3, 2);

                    switch (msgheader)
                    {
                        case "03"://status update string recieved
                            string strpos = message.Substring(7, 8);
                            MotorAxis.ErrorCode = message.Substring(15, 4);
                            MotorAxis.CurrentCoordinate = (double)int.Parse(strpos, System.Globalization.NumberStyles.HexNumber);
                           // string outputportvalue = message.Substring(23, 4);//changge to device 1
                            string device1str = message.Substring(27, 4);//changge to device 1
                            MotorAxis.RawStatus = new PconModbus.Status((ushort)Convert.ToUInt16(device1str, 16));
                            //MotorAxis.RawStatus = new PconModbus.Status((ushort)Convert.ToUInt16(outputportvalue, 16));
                            break;

                        case "10"://Direct Writing of Positioning Data
                            MotorAxis.Rxmsg = message;
                            if (message == @":0110990000094D")//temp hardcode assume motor axis is always 01
                                MotorAxis.Rxmsg = MotorAxis.sendmsg;
                            break;

                        case "05":
                            //check for start address
                            string startaddress = message.Substring(5, 4);
                            switch (startaddress)
                            {
                                case "0403"://servi on/off address
                                    MotorAxis.Rxmsg = message;
                                    //if (MotorAxis.sendmsg == MotorAxis.Rxmsg)
                                    //    MotorAxis.ResetCommand();
                                    break;

                                case "0407"://alarm reset
                                    string resettype = message.Substring(9, 2);
                                    if (resettype == "FF")
                                    {//alarm reset start
                                    }
                                    else
                                    {//alarm reset end
                                    }
                                    MotorAxis.Rxmsg = message;
                                    //if (MotorAxis.sendmsg == MotorAxis.Rxmsg)
                                    //    MotorAxis.ResetCommand();
                                    break;

                                case "040A"://pause on/off
                                    break;

                                case "040B"://home request
                                    MotorAxis.Rxmsg = message;
                                    //if (MotorAxis.sendmsg == MotorAxis.Rxmsg)
                                    //    MotorAxis.ResetCommand();
                                    break;

                                case "040C"://position start command
                                    break;

                                case "0411"://jog/inch switch
                                    break;

                                case "0414"://teach/operation mode
                                    break;

                                case "0416"://Jog +ve
                                    MotorAxis.Rxmsg = message;
                                    break;

                                case "0417"://Jog -ve
                                    MotorAxis.Rxmsg = message;
                                    break;

                                case "0427":
                                    MotorAxis.Rxmsg = message;
                                    //if (MotorAxis.sendmsg == MotorAxis.Rxmsg)
                                    //    MotorAxis.ResetCommand();
                                    break;
                            }
                            break;
                    }
                }
                catch (Exception ex) { }
            }
        }

        private int ExecuteCommand(PconControllerAxis axis, AxisCommand option)
        {
            int result = 0;
            // Execute the command
            switch (option)
            {
                case AxisCommand.ModBusOn:
                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.PIOOrModbusSwitchingSpecification, true);
                    axis.CommandDoneEvent.Set();
                    break;
                case AxisCommand.ModBusOff:
                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.PIOOrModbusSwitchingSpecification, false);
                    axis.CommandDoneEvent.Set();
                    break;
                case AxisCommand.ServoOn:
                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.ServoONCommand, true);
                    axis.CommandDoneEvent.Set();
                    break;

                case AxisCommand.ServoOff:
                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.ServoONCommand, false);
                    axis.CommandDoneEvent.Set();
                    break;

                case AxisCommand.HomeSearchStart:
                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.HomeReturnCommand, true);
                    axis.CommandDoneEvent.Set();

                    break;

                case AxisCommand.HomeSearchEnd:

                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.HomeReturnCommand, false);
                    axis.CommandDoneEvent.Set();
                    break;

                case AxisCommand.Move:
                    {
                        // Write position
                        axis.PositionEnd = false;
                        axis.RawStatus.PEND = false;
                        PconModbus.PositionDataDirectWritingTableEntry directEntry = new PconModbus.PositionDataDirectWritingTableEntry();
                        directEntry.TargetPosition = (int)axis.CurrentPosition.Coordinate;
                        directEntry.IsRelativePosition = axis.CurrentPosition.IsRelativePosition;
                        directEntry.SpeedCommand = (uint)axis.CurrentPosition.MaxVelocity;
                        directEntry.PositioningBand = axis.CurrentPosition.InPositionRange;
                        directEntry.PushCurrentLimitingValue = 0;
                        directEntry.AccelerationDecelerationCommand = (ushort)(axis.CurrentPosition.MaxVelocity / (axis.CurrentPosition.AccTime * PconControllerAxis.G_CONSTANT));
                        result = this.pcon.PresetMultipleRegisters(axis, PconModbus.Register.TargetPositionCoordinateSpecificationRegister, directEntry.ToData());
                        axis.CommandDoneEvent.Set();
                    }
                    break;

                case AxisCommand.JogPositive:
                    {
                        PconModbus.PositionDataDirectWritingTableEntry directEntry = new PconModbus.PositionDataDirectWritingTableEntry();
                        directEntry.TargetPosition = (int)axis.JogPosition.Coordinate;
                        directEntry.IsRelativePosition = true;
                        directEntry.SpeedCommand = (uint)axis.JogPosition.MaxVelocity;
                        directEntry.PositioningBand = axis.JogPosition.InPositionRange;
                        directEntry.PushCurrentLimitingValue = 0;
                        directEntry.AccelerationDecelerationCommand = (ushort)(axis.JogPosition.MaxVelocity / (axis.JogPosition.AccTime * PconControllerAxis.G_CONSTANT));
                        result = this.pcon.PresetMultipleRegisters(axis, PconModbus.Register.TargetPositionCoordinateSpecificationRegister, directEntry.ToData());
                        axis.CommandDoneEvent.Set();
                    }
                    break;

                case AxisCommand.JogNegative:
                    {
                        PconModbus.PositionDataDirectWritingTableEntry directEntry = new PconModbus.PositionDataDirectWritingTableEntry();
                        directEntry.TargetPosition = -(int)axis.JogPosition.Coordinate;
                        directEntry.IsRelativePosition = true;
                        directEntry.SpeedCommand = (uint)axis.JogPosition.MaxVelocity;
                        directEntry.PositioningBand = axis.JogPosition.InPositionRange;
                        directEntry.PushCurrentLimitingValue = 0;
                        directEntry.AccelerationDecelerationCommand = (ushort)(axis.JogPosition.MaxVelocity / (axis.JogPosition.AccTime * PconControllerAxis.G_CONSTANT));
                        result = this.pcon.PresetMultipleRegisters(axis, PconModbus.Register.TargetPositionCoordinateSpecificationRegister, directEntry.ToData());
                        axis.CommandDoneEvent.Set();
                    }
                    break;

                case AxisCommand.DecelerationStop:
                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.DecelerationStop, true);
                    if (result != 0)
                    {
                        break;
                    }
                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.DecelerationStop, false);
                    break;

                case AxisCommand.Stop:
                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.ServoONCommand, false);
                    if (result != 0)
                    {
                        break;
                    }
                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.ServoONCommand, true);
                    break;

                case AxisCommand.AlarmReset_Start:
                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.AlarmResetCommand, true);
                    axis.CommandDoneEvent.Set();
                    break;

                case AxisCommand.AlarmReset_End:

                    result = this.pcon.ForceSingleCoil(axis, PconModbus.Coil.AlarmResetCommand, false);
                    axis.CommandDoneEvent.Set();
                    break;
            }
            //axis.ResetCommand();
            if (result != 0)
            {
                return result;
            }
            return 0;
        }

        public string DisplayName { get; set; }
    }
}