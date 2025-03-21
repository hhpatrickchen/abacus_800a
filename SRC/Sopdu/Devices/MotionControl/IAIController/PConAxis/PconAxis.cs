using Sopdu.Devices.MotionControl.Base;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sopdu.Devices.MotionControl.IAIController.PConAxis
{
    public static partial class Extensions
    {
        public static int GetSize(this PconModbus.Register register)
        {
            switch (register)
            {
                case PconModbus.Register.AlarmOccurrenceTime:
                case PconModbus.Register.DeviceControlRegister1:
                case PconModbus.Register.DeviceControlRegister2:
                case PconModbus.Register.PositionNumberSpecificationRegister:
                case PconModbus.Register.TotalMovingCount:
                case PconModbus.Register.TotalMovingDistance:
                case PconModbus.Register.PresentTime_SCON_CA:
                case PconModbus.Register.PresentTime_PCON_CA_CFA:
                case PconModbus.Register.TotalFanDrivingTime_PCON_CFA:
                case PconModbus.Register.CurrentPositionMonitor:
                case PconModbus.Register.SystemStatusQuery:
                case PconModbus.Register.CurrentSpeedMonitor:
                case PconModbus.Register.CurrentAmpereMonitor:
                case PconModbus.Register.DeviationMonitor:
                case PconModbus.Register.SystemTimerQuery:
                case PconModbus.Register.ForceFeedbackDataMonitor:
                case PconModbus.Register.PositionMovementCommandRegister:
                case PconModbus.Register.TargetPositionCoordinateSpecificationRegister:
                case PconModbus.Register.PositioningBandSpecificationRegister:
                case PconModbus.Register.SpeedSpecificationRegister:
                    return 2;

                default:
                    return 1;
            }
        }

        public static bool IsSigned(this PconModbus.Register register)
        {
            switch (register)
            {
                case PconModbus.Register.CurrentPositionMonitor:
                case PconModbus.Register.CurrentSpeedMonitor:
                case PconModbus.Register.CurrentAmpereMonitor:
                case PconModbus.Register.DeviationMonitor:
                case PconModbus.Register.ForceFeedbackDataMonitor:
                case PconModbus.Register.TargetPositionCoordinateSpecificationRegister:
                    return true;

                default:
                    return false;
            }
        }
    }

    public class PconControllerChannel
    {
        public PconControllerChannel(string comAddress)
            : base()
        {
            this.AxisList = new PconControllerAxis[16];
            this.COMAddress = comAddress;
        }

        public PconControllerAxis[] AxisList { get; private set; }
        public string COMAddress { get; private set; }

        public string Name
        {
            get { return "PconControllerChannel_" + COMAddress; }
        }

        public PconControllerAxis GetAxis(int axisNumber)
        {
            if (axisNumber < 0 || axisNumber > 15)
            {
                throw new ArgumentException("Invalid axisNumber");
            }
            if (AxisList[axisNumber] == null)
            {
                // Create PconControllerAxis, as it is not created yet.
                PconControllerAxis axis = new PconControllerAxis(this, (byte)axisNumber);
                AxisList[axisNumber] = axis;
            }
            return AxisList[axisNumber];
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class PconControllerAxis : Axis
    {
        [XmlIgnore]
        public const float G_CONSTANT = 9806.65F;

        //protected static const float G_CONSTANT = 9806.65F;

        private PconModbus.Status _rawStatus;

        private PconControllerChannel channel;

        [XmlIgnore]
        public bool bModbusActive;
        
        public PconControllerAxis()
            : base()
        { }

        public PconControllerAxis(PconControllerChannel channel, byte axisNumber)
            : base()
        {
            this.channel = channel;
            this.AxisNumber = axisNumber;
            bModbusActive = false;
        }

        [XmlIgnore]
        public byte AxisNumber { get; private set; }

        [XmlIgnore]
        public override string Name
        {
            get { return "PconControllerAxis_" + channel.COMAddress + "_" + AxisNumber; }
        }

        [XmlIgnore]
        public PconModbus.Status RawStatus
        {
            get { return _rawStatus; }
            set
            {
                _rawStatus = value;
                // Also update CurrentStatus
                if (value.ALMH)
                {
                    CurrentStatus = AxisStatus.Alarm;
                }
                else if (value.EMG)
                {
                    CurrentStatus = AxisStatus.EStopped;
                }
                else if (!value.SV)
                {
                    CurrentStatus = AxisStatus.ServoOff;
                }
                else if (!value.HEND)
                {
                    CurrentStatus = AxisStatus.Uninitialized;
                }
                else if (!value.PEND)
                {
                    CurrentStatus = AxisStatus.Moving;
                }
                else
                {
                    CurrentStatus = AxisStatus.Ready;
                }
                if (value.PEND == true)
                    PositionEnd = true;
                if (value.PEND == false)
                    PositionEnd = false;
            }
        }
    }

    public class PconModbus
    {
        public const int RETRY_ATTEMPTS = 2;

        private SerialPort serialPort;

        // serialPort1.DataBits = 8;
        // serialPort1.StopBits = System.IO.Ports.StopBits.One;
        // serialPort1.Parity = Parity.None;
        // serialPort1.ReadTimeout = 2000;
        public PconModbus(string comAddress)
        {
            this.comAddress = comAddress;
            serialPort = new SerialPort(comAddress);
            // Serial port configuration
            serialPort.BaudRate = 38400;
            serialPort.DataBits = 8;
            serialPort.Parity = Parity.None;
            serialPort.ReadTimeout = 2000;
            serialPort.StopBits = StopBits.One;
            serialPort.NewLine = "\r\n";
            //  serialPort.ReceivedBytesThreshold = 0;
            // serialPort.DataReceived += serialPort_DataReceived;
        }

        //private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    //throw new NotImplementedException();
        //    //string msg = serialPort.ReadExisting();
        //}

        // IAI PCON point type controller
        // RS485 port, Modbus-ASCII
        // ChunYang 13-March-2014
        ~PconModbus()
        {
            if ((this.serialPort != null) && this.serialPort.IsOpen)
            {
                Close();
            }
        }

        public enum Coil : ushort
        {
            SafetySpeedCommand = 0x0401,
            ServoONCommand = 0x0403,
            AlarmResetCommand = 0x0407,
            BrakeForcedReleaseCommand = 0x0408,
            PauseCommand = 0x040A,
            HomeReturnCommand = 0x040B,
            PositioningStartCommand = 0x040C,
            JogOrInchSwitching = 0x0411,
            TeachingModeCommand = 0x0414,
            PositionDataLoadCommand = 0x0415,
            JogPositiveCommand = 0x0416,
            JogNegativeCommand = 0x0417,
            StartPosition7 = 0x0418,
            StartPosition6 = 0x0419,
            StartPosition5 = 0x041A,
            StartPosition4 = 0x041B,
            StartPosition3 = 0x041C,
            StartPosition2 = 0x041D,
            StartPosition1 = 0x041E,
            StartPosition0 = 0x041F,
            LoadCellCalibrationCommand = 0x0426,
            PIOOrModbusSwitchingSpecification = 0x0427,
            DecelerationStop = 0x042C,
        }

        public enum Function : byte
        {
            ReadCoilStatus = 0x01,
            ReadInputStatus = 0x02,
            ReadHoldingRegisters = 0x03,
            ReadInputRegisters = 0x04,
            ForceSingleCoil = 0x05,
            PresetSingleRegister = 0x06,
            ReadExceptionStatus = 0x07,
            ForceMultipleCoils = 0x0F,
            PresetMultipleRegisters = 0x10,
            ReportSlaveID = 0x11,
            ReadOrWriteRegisters = 0x17,
        }

        public enum Register : ushort
        {
            AlarmDetailCode = 0x0500,
            AlarmAddress = 0x0501,
            AlarmCode = 0x0503,
            AlarmOccurrenceTime = 0x0504,
            DeviceControlRegister1 = 0x0D00,                              //06
            DeviceControlRegister2 = 0x0D01,                              //06
            PositionNumberSpecificationRegister = 0x0D03,                 //06
            TotalMovingCount = 0x8400,
            TotalMovingDistance = 0x8402,
            PresentTime_SCON_CA = 0x841A,
            PresentTime_PCON_CA_CFA = 0x8420,
            TotalFanDrivingTime_PCON_CFA = 0x842E,
            CurrentPositionMonitor = 0x9000,
            PresentAlarmCodeQuery = 0x9002,
            InputPortQuery = 0x9003,
            OutputPortMonitorQuery = 0x9004,
            DeviceStatusQuery1 = 0x9005,
            DeviceStatusQuery2 = 0x9006,
            ExpansionDeviceStatusQuery = 0x9007,
            SystemStatusQuery = 0x9008,
            CurrentSpeedMonitor = 0x900A,
            CurrentAmpereMonitor = 0x900C,
            DeviationMonitor = 0x900E,
            SystemTimerQuery = 0x9010,
            SpecialInputPortQuery = 0x9012,
            ZoneStatusQuery = 0x9013,
            PositionCompleteNumberStatusQuery = 0x9014,
            ExpansionSystemStatusRegister = 0x9015,
            ForceFeedbackDataMonitor = 0x901E,
            PositionMovementCommandRegister = 0x9800,                      //06
            TargetPositionCoordinateSpecificationRegister = 0x9900,        //10
            PositioningBandSpecificationRegister = 0x9902,                 //10
            SpeedSpecificationRegister = 0x9904,                           //10
            AccelerationDecelerationSpeedSpecificationRegister = 0x9906,   //10
            PushCurrentLimitingValue = 0x9907,                             //10
            ControlFlagSpecificationRegister = 0x9908,                     //10
        }
        /*
         * 
                     public bool _NA1;
            public bool CLBS;
            public bool CEND;
            public bool PEND;
            public bool HEND;
            public bool STP;
            public bool _NA2;
            public bool BKRL;
            public bool ABER;
            public bool ALML;
            public bool ALMH;
            public bool PSFL;
            public bool SV;
            public bool PWR;
            public bool SFTY;
            public bool EMG;
         * */
        public enum StatusBit : byte
        {
            _NA1 = 0,
            CLBS = 1,
            CEND = 2,
            PEND = 3,
            HEND = 4,
            STP = 5,
            _NA2 = 6,
            BKRL = 7,
            ABER = 8,
            ALML = 9,
            ALMH = 10,
            PSFL = 11,
            SV = 12,
            PWR = 13,
            SFTY = 14,
            EMG = 15,
        }

        public string comAddress { get; private set; }
        public Exception LastException { get; private set; }

        public int Close()
        {
            try
            {
                serialPort.Close();
            }
            catch (Exception ex)
            {
                LastException = ex;
                return -1;
            }
            LastException = null;
            return 0;
        }

        public int ForceSingleCoil(PconControllerAxis axis, Coil coil, bool value)
        {
            int result = 0;
            string message = String.Format("{0:X2}{1:X4}{2:X4}",
                (byte)Function.ForceSingleCoil, (ushort)coil, value ? 0xFF00 : 0x0000);
            result = Write(axis.AxisNumber, message, out message);
            axis.sendmsg = message;
            return result;
        }

        public int Open()
        {
            try
            {
                //serialPort.Close();
                int maxretry = 10;
                const int sleeptimems = 1000;
                while (maxretry > 0)
                {
                    try
                    {
                        MessageListener.Instance.ReceiveMessage(serialPort.PortName + " try to open..");
                        serialPort.Open();
                        MessageListener.Instance.ReceiveMessage(serialPort.PortName + " open successfully");
                        Thread.Sleep(sleeptimems);
                        break;
                    }
                    catch (Exception ex)
                    {
                        maxretry--; 
                        
                        MessageListener.Instance.ReceiveMessage( serialPort.PortName + " Fail to open, retry");
                        Thread.Sleep(sleeptimems);
                    }
                }

                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
            }
            catch (Exception ex)
            {
                LastException = ex;
                MessageListener.Instance.ReceiveMessage("COM PORT " + serialPort.PortName + " Fail to open");
                Thread.Sleep(1000);
                //serialPort.Close();
                //serialPort.Open();
                //serialPort.DiscardInBuffer();
                //serialPort.DiscardOutBuffer();
                return -1;
            }
            LastException = null;
            return 0;
        }

        public int PresetMultipleRegisters(PconControllerAxis axis, Register register, ushort[] value)
        {
            int result = 0;
            StringBuilder sb = new StringBuilder(value.Length + 8);
            sb.AppendFormat("{0:X2}{1:X4}{2:X4}{3:X2}", (byte)Function.PresetMultipleRegisters, (ushort)register, (ushort)value.Length, (ushort)value.Length * 2);
            for (int j = 0; j < value.Length; j++)
            {
                sb.AppendFormat("{0:X4}", value[j]);
            }
            string message;
            result = Write(axis.AxisNumber, sb.ToString(), out message);
            axis.sendmsg = message;
            return result;
        }

        //public int PresetSingleRegister(byte axis, Register register, object value)
        //{
        //    int result = 0;
        //    for (int i = 1; i <= RETRY_ATTEMPTS; i++)
        //    {
        //        string message = String.Format("{0:X2}{1:X4}{2:X4}", (byte)Function.PresetSingleRegister, (ushort)register, value);
        //        result = Write(axis, message, out message);
        //        if (result != 0)
        //        {
        //            LastException = new Exception("PresetSingleRegister Write", LastException);
        //            continue;
        //        }
        //        string readMessage;
        //        result = Read(out readMessage);
        //        if (result != 0)
        //        {
        //            LastException = new Exception("PresetSingleRegister Read", LastException);
        //            continue;
        //        }
        //        if (!message.Equals(readMessage))
        //        {
        //            result = -2001;
        //            // If the change is successful, the response message will be the same as the query.
        //            // If invalid data is sent, an exception response (refer to section 7) will be returned, or no response will be returned.
        //            LastException = new Exception("Invalid PresetSingleRegister Response");
        //            continue;
        //        }
        //        LastException = null;
        //        break;
        //    }
        //    return result;
        //}

        //public int ReadHoldingRegister(byte axis, Register register, out object value)
        //{
        //    int result = 0;
        //    value = -1;
        //    for (int i = 1; i <= RETRY_ATTEMPTS; i++)
        //    {
        //        string message = String.Format("{0:X2}{1:X4}{2:X4}", (byte)Function.ReadHoldingRegisters, (ushort)register, register.GetSize());
        //        result = Write(axis, message, out message);
        //        if (result != 0)
        //        {
        //            LastException = new Exception("ReadHoldingRegister Write", LastException);
        //            continue;
        //        }
        //        string readMessage;
        //        result = Read(out readMessage);
        //        if (result != 0)
        //        {
        //            LastException = new Exception("ReadHoldingRegister Read", LastException);
        //            continue;
        //        }
        //        // Parsing response
        //        try
        //        {
        //            int readLength = Convert.ToInt32(readMessage.Substring(5, 2), 16);
        //            if (readLength != (2 * register.GetSize()))
        //            {
        //                result = -2001;
        //                LastException = new Exception("ReadHoldingRegister Length Mismatch", LastException);
        //                continue;
        //            }
        //            string readValue = readMessage.Substring(7, 2 * readLength);
        //            if (register.GetSize() == 1)
        //            {
        //                if (register.IsSigned())
        //                {
        //                    value = Convert.ToInt16(readValue, 16);
        //                }
        //                else
        //                {
        //                    value = Convert.ToUInt16(readValue, 16);
        //                }
        //            }
        //            else if (register.GetSize() == 2)
        //            {
        //                if (register.IsSigned())
        //                {
        //                    value = Convert.ToInt32(readValue, 16);
        //                }
        //                else
        //                {
        //                    value = Convert.ToUInt32(readValue, 16);
        //                }
        //            }
        //            else
        //            {
        //                result = -2002;
        //                LastException = new ArgumentException("ReadHoldingRegister Register Assert");
        //                continue;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            result = -2003;
        //            LastException = new Exception("ReadHoldingRegister Parsing)", ex);
        //            continue;
        //        }
        //        LastException = null;
        //        break;
        //    }
        //    return result;
        //}

        //public int ReadMultipleRegisters(byte axis, Register register, ushort length, out ushort[] value)
        //{
        //    int result = 0;
        //    value = null;
        //    for (int i = 1; i <= RETRY_ATTEMPTS; i++)
        //    {
        //        string message = String.Format("{0:X2}{1:X4}{2:X4}", (byte)Function.ReadHoldingRegisters, (ushort)register, length);
        //        result = Write(axis, message, out message);
        //        if (result != 0)
        //        {
        //            LastException = new Exception("ReadMultipleRegisters Write", LastException);
        //            continue;
        //        }
        //        string readMessage;
        //        result = Read(out readMessage);
        //        if (result != 0)
        //        {
        //            LastException = new Exception("ReadMultipleRegisters Read", LastException);
        //            continue;
        //        }
        //        // Parsing response
        //        try
        //        {
        //            int readLength = Convert.ToInt32(readMessage.Substring(5, 2), 16);
        //            // readLength is count of bytes, length is count of words
        //            if (readLength != (2 * length))
        //            {
        //                result = -2001;
        //                LastException = new Exception("ReadMultipleRegisters Length Mismatch", LastException);
        //                continue;
        //            }
        //            ushort[] readData = new ushort[length];
        //            for (int j = 0; j < length; j++)
        //            {
        //                readData[j] = Convert.ToUInt16(readMessage.Substring(7 + (4 * j), 4), 16);
        //            }
        //            value = readData;
        //        }
        //        catch (Exception ex)
        //        {
        //            result = -2002;
        //            LastException = new Exception("ReadMultipleRegisters Parsing)", ex);
        //            continue;
        //        }
        //        LastException = null;
        //        break;
        //    }
        //    return result;
        //}

        public int Read(out string readMessage)
        {
            readMessage = null;

            // Check serial port status
            if (!serialPort.IsOpen)
            {
                LastException = new Exception("Port is Closed: " + serialPort.PortName);
                return -1;
            }

            try
            {
                readMessage = serialPort.ReadLine();
            }
            catch (Exception ex)
            {
                LastException = new Exception("SerialPort Read", ex);
                return -2;
            }

            try
            {
                // Validate opening
                if (readMessage[0] != ':')
                {
                    LastException = new Exception("Message Start Character Invalid");
                    return -3;
                }
                // Validate message length
                if ((readMessage.Length < 11) || (((readMessage.Length - 1) % 2) != 0))
                {
                    LastException = new Exception("InputMessage Length");
                    return -4;
                }

                // Validate LRC
                int lrc = 0;
                for (int i = 1; i < readMessage.Length - 2; i += 2)
                {
                    lrc = (lrc + Convert.ToByte(readMessage.Substring(i, 2), 16)) & 0xFF;
                }
                lrc = ((-lrc) & 0xFF);
                string calculatedLRCString = String.Format("{0:X2}", lrc);
                if (!calculatedLRCString.Equals(readMessage.Substring(readMessage.Length - 2, 2)))
                {
                    LastException = new Exception("LRC Validation");
                    return -5;
                }
            }
            catch (Exception ex)
            {
                LastException = new Exception("Message Validation", ex);
                return -6;
            }
            LastException = null;
            return 0;
        }

        public int Write(byte axis, string command, out string sentMessage)
        {
            sentMessage = null;

            // Check serial port status
            if (!serialPort.IsOpen)
            {
                LastException = new Exception("Port is Closed: " + serialPort.PortName);
                return -1;
            }
            // Clear incoming buffer
            //  serialPort.DiscardInBuffer();
            // Argument checks
            if ((axis > 15) || (axis < 0))
            {
                LastException = new ArgumentException("Axis");
                return -2;
            }
            if (((command.Length % 2) != 0) || (command.Length == 0))
            {
                // command.Length must be even, formed by multiple 2 digits hex string
                LastException = new ArgumentException("Command");
                return -3;
            }

            StringBuilder sb;
            try
            {
                // Initiate send parameters
                sb = new StringBuilder(command.Length + 5);
                sb.Append(':'); // StartCode
                sb.AppendFormat("{0:X2}", axis + 1); // Address
                //string FunctionCode = "00";
                //string Data = "X";
                //command = FunctionCode+Data;
                sb.Append(command);
                // LRC Calculation
                int lrc = 0;
                for (int i = 1; i < sb.Length; i += 2)
                {
                    lrc = (lrc + Convert.ToByte(sb.ToString(i, 2), 16)) & 0xFF;
                }
                lrc = ((-lrc) & 0xFF);
                sb.AppendFormat("{0:X2}", lrc); // LRC
                sentMessage = sb.ToString();
            }
            catch (Exception ex)
            {
                LastException = new Exception("Message Building", ex);
                return -4;
            }

            try
            {
                // Send command
                serialPort.WriteLine(sentMessage);
            }
            catch (Exception ex)
            {
                LastException = new Exception("SerialPort Write", ex);
                return -5;
            }
            LastException = null;
            return 0;
        }

        public class PositionDataDirectWritingTableEntry
        {
            public ushort AccelerationDecelerationCommand;
            public ushort ControlFlagSpecification;
            public uint PositioningBand;
            public ushort PushCurrentLimitingValue;
            public uint SpeedCommand;
            public int TargetPosition;

            public PositionDataDirectWritingTableEntry()
            {
                this.TargetPosition = 0;
                this.PositioningBand = 0;
                this.SpeedCommand = 0;
                this.AccelerationDecelerationCommand = 0;
                this.PushCurrentLimitingValue = 0;
                this.ControlFlagSpecification = 0;
            }

            public bool IsRelativePosition
            {
                get
                {
                    return (this.ControlFlagSpecification & 0x08) != 0;
                }
                set
                {
                    if (value)
                    {
                        this.ControlFlagSpecification = (ushort)(this.ControlFlagSpecification | 0x08);
                    }
                    else
                    {
                        this.ControlFlagSpecification = (ushort)(this.ControlFlagSpecification & ~(0x08));
                    }
                }
            }

            public ushort[] ToData()
            {
                ushort[] data = new ushort[9];
                data[0] = (ushort)((this.TargetPosition >> 16) & 0xFFFF);
                data[1] = (ushort)(this.TargetPosition & 0xFFFF);
                data[2] = (ushort)((this.PositioningBand >> 16) & 0xFFFF);
                data[3] = (ushort)(this.PositioningBand & 0xFFFF);
                data[4] = (ushort)((this.SpeedCommand >> 16) & 0xFFFF);
                data[5] = (ushort)(this.SpeedCommand & 0xFFFF);
                data[6] = this.AccelerationDecelerationCommand;
                data[7] = this.PushCurrentLimitingValue;
                data[8] = this.ControlFlagSpecification;
                return data;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("TargetPosition:");
                sb.Append(TargetPosition);
                sb.Append(";PositioningBand:");
                sb.Append(PositioningBand);
                sb.Append(";SpeedCommand:");
                sb.Append(SpeedCommand);
                sb.Append(";AccelerationDecelerationCommand:");
                sb.Append(AccelerationDecelerationCommand);
                sb.Append(";PushCurrentLimitingValue:");
                sb.Append(PushCurrentLimitingValue);
                sb.Append(";ControlFlagSpecification:");
                sb.Append(ControlFlagSpecification);
                return sb.ToString();
            }
        }

        public class PositionDataTableEntry
        {
            public ushort AccelerationCommand;
            public ushort ControlFlagSpecification;
            public ushort DecelerationCommand;
            public int IndividualZoneBoundaryNegative;
            public int IndividualZoneBoundaryPositive;
            public ushort LoadCurrentThreshold;
            public uint PositioningBand;
            public ushort PushCurrentLimitingValue;
            public uint SpeedCommand;
            public int TargetPosition;

            public PositionDataTableEntry()
            {
                this.TargetPosition = 0;
                this.PositioningBand = 0;
                this.SpeedCommand = 0;
                this.IndividualZoneBoundaryPositive = 0;
                this.IndividualZoneBoundaryNegative = 0;
                this.AccelerationCommand = 0;
                this.DecelerationCommand = 0;
                this.PushCurrentLimitingValue = 0;
                this.LoadCurrentThreshold = 0;
                this.ControlFlagSpecification = 0;
            }

            public bool IsRelativePosition
            {
                get
                {
                    return (this.ControlFlagSpecification & 0x08) != 0;
                }
                set
                {
                    if (value)
                    {
                        this.ControlFlagSpecification = (ushort)(this.ControlFlagSpecification | 0x08);
                    }
                    else
                    {
                        this.ControlFlagSpecification = (ushort)(this.ControlFlagSpecification & ~(0x08));
                    }
                }
            }

            public static PositionDataTableEntry GetData(ushort[] data)
            {
                if (data.Length == 16)
                {
                    PositionDataTableEntry entry = new PositionDataTableEntry();
                    entry.TargetPosition = (data[0] << 16) + data[1];
                    entry.PositioningBand = (uint)((data[2] << 16) + data[3]);
                    entry.SpeedCommand = (uint)((data[4] << 16) + data[5]);
                    entry.IndividualZoneBoundaryPositive = (data[6] << 16) + data[7];
                    entry.IndividualZoneBoundaryNegative = (data[8] << 16) + data[9];
                    entry.AccelerationCommand = data[10];
                    entry.DecelerationCommand = data[11];
                    entry.PushCurrentLimitingValue = data[12];
                    entry.LoadCurrentThreshold = data[13];
                    entry.ControlFlagSpecification = data[14];
                    return entry;
                }
                else
                {
                    throw new ArgumentException();
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("TargetPosition:");
                sb.Append(TargetPosition);
                sb.Append(";PositioningBand:");
                sb.Append(PositioningBand);
                sb.Append(";SpeedCommand:");
                sb.Append(SpeedCommand);
                sb.Append(";IndividualZoneBoundaryPositive:");
                sb.Append(IndividualZoneBoundaryPositive);
                sb.Append(";IndividualZoneBoundaryNegative:");
                sb.Append(IndividualZoneBoundaryNegative);
                sb.Append(";AccelerationCommand:");
                sb.Append(AccelerationCommand);
                sb.Append(";DecelerationCommand:");
                sb.Append(DecelerationCommand);
                sb.Append(";PushCurrentLimitingValue:");
                sb.Append(PushCurrentLimitingValue);
                sb.Append(";LoadCurrentThreshold:");
                sb.Append(LoadCurrentThreshold);
                sb.Append(";ControlFlagSpecification:");
                sb.Append(ControlFlagSpecification);
                return sb.ToString();
            }
        }

        public class Status
        {
            public bool _NA1;
            public bool CLBS;
            public bool CEND;
            public bool PEND;
            public bool HEND;
            public bool STP;
            public bool _NA2;
            public bool BKRL;
            public bool ABER;
            public bool ALML;
            public bool ALMH;
            public bool PSFL;
            public bool SV;
            public bool PWR;
            public bool SFTY;
            public bool EMG;

            public Status(ushort value)
            {
                this._NA1 = (value & (1 << (byte)StatusBit._NA1)) != 0;
                this.CLBS= (value & (1 << (byte)StatusBit.CLBS)) != 0;
                this.CEND= (value & (1 << (byte)StatusBit.CEND)) != 0;
                this.PEND= (value & (1 << (byte)StatusBit.PEND)) != 0;
                this.HEND = (value & (1 << (byte)StatusBit.HEND)) != 0;
                this.STP = (value & (1 << (byte)StatusBit.STP)) != 0;
                this._NA2 = (value & (1 << (byte)StatusBit._NA2)) != 0;
                this.BKRL= (value & (1 << (byte)StatusBit.BKRL)) != 0;
                this.ABER = (value & (1 << (byte)StatusBit.ABER)) != 0;
                this.ALML = (value & (1 << (byte)StatusBit.ALML)) != 0;
                this.ALMH= (value & (1 << (byte)StatusBit.ALMH)) != 0;
                this.PSFL = (value & (1 << (byte)StatusBit.PSFL)) != 0;
                this.SV = (value & (1 << (byte)StatusBit.SV)) != 0;
                this.PWR= (value & (1 << (byte)StatusBit.PWR)) != 0;
                this.SFTY = (value & (1 << (byte)StatusBit.SFTY)) != 0;
                this.EMG= (value & (1 << (byte)StatusBit.EMG)) != 0;
            }

            public static explicit operator Status(ushort value)
            {
                return new Status(value);
            }

            public static explicit operator ushort(Status value)
            {
                int result = 0;
                result |= (value._NA1 ? 1 : 0) << ((byte)StatusBit._NA1);
                result |= (value.CLBS ? 1 : 0) << ((byte)StatusBit.CLBS);
                result |= (value.CEND ? 1 : 0) << ((byte)StatusBit.CEND);
                result |= (value.PEND ? 1 : 0) << ((byte)StatusBit.PEND);
                result |= (value.HEND ? 1 : 0) << ((byte)StatusBit.HEND);
                result |= (value.STP ? 1 : 0) << ((byte)StatusBit.STP);
                result |= (value._NA2? 1 : 0) << ((byte)StatusBit._NA2);
                result |= (value.BKRL ? 1 : 0) << ((byte)StatusBit.BKRL);
                result |= (value.ABER ? 1 : 0) << ((byte)StatusBit.ABER);
                result |= (value.ALML ? 1 : 0) << ((byte)StatusBit.ALML);
                result |= (value.ALMH ? 1 : 0) << ((byte)StatusBit.ALMH);
                result |= (value.PSFL ? 1 : 0) << ((byte)StatusBit.PSFL);
                result |= (value.SV ? 1 : 0) << ((byte)StatusBit.SV);
                result |= (value.PWR? 1 : 0) << ((byte)StatusBit.PWR);
                result |= (value.SFTY? 1 : 0) << ((byte)StatusBit.SFTY);
                result |= (value.EMG? 1 : 0) << ((byte)StatusBit.EMG);
                return (ushort)result;
            }

            //public int GetPosition()
            //{
            //    return (PM32 ? 32 : 0) | (PM16 ? 16 : 0) | (PM8 ? 8 : 0) | (PM4 ? 4 : 0) | (PM2 ? 2 : 0) | (PM1 ? 1 : 0);
            //}

            public bool IsError()
            {
                return !ABER || !SV || !EMG || !ALMH||!ALML;
            }

            public bool IsMoving()
            {
                return (!PEND);
            }

            public bool IsReady()
            {
                return HEND && PEND && !IsError();
            }
            /*
             * 
            public bool _NA1;
            public bool CLBS;
            public bool CEND;
            public bool PEND;
            public bool HEND;
            public bool STP;
            public bool _NA2;
            public bool BKRL;
            public bool ABER;
            public bool ALML;
            public bool ALMH;
            public bool PSFL;
            public bool SV;
            public bool PWR;
            public bool SFTY;
            public bool EMG;            
             * */
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                if (_NA1) { sb.Append("_NA1;"); } else { sb.Append("_na1;"); }
                if (CLBS) { sb.Append("CLBS;"); } else { sb.Append("clbs;"); }
                if (CEND) { sb.Append("CEND;"); } else { sb.Append("cend;"); }
                if (PEND) { sb.Append("PEND;"); } else { sb.Append("pend;"); }
                if (HEND) { sb.Append("HEND;"); } else { sb.Append("hend;"); }
                if (STP) { sb.Append("STP;"); } else { sb.Append("stp;"); }
                if (_NA2) { sb.Append("_NA2;"); } else { sb.Append("_na2;"); }
                if (BKRL) { sb.Append("BKRL;"); } else { sb.Append("bkrl;"); }
                if (ABER) { sb.Append("ABER;"); } else { sb.Append("aber;"); }
                if (ALML) { sb.Append("ALML;"); } else { sb.Append("alml;"); }
                if (ALMH) { sb.Append("ALMH;"); } else { sb.Append("almh;"); }
                if (PSFL) { sb.Append("PSFL;"); } else { sb.Append("psfl;"); }
                if (SV) { sb.Append("SV;"); } else { sb.Append("sv;"); }
                if (PWR) { sb.Append("PWR;"); } else { sb.Append("pwr;"); }
                if (SFTY) { sb.Append("SFTY;"); } else { sb.Append("sfty;"); }
                if (EMG) { sb.Append("EMG;"); } else { sb.Append("emg;"); }
                return sb.ToString();
            }
        }

        internal void Readchar(out string message)
        {
            message = serialPort.ReadExisting();
            //throw new NotImplementedException();
        }
    }
}