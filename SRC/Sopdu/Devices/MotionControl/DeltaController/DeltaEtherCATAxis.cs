using Sopdu.Devices.MotionControl.Base;
using Sopdu.Devices.MotionControl.DeltaController;
using Sopdu.Devices.MotionControl.IAIController.PConAxis;
using Sopdu.helper;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Xml.Serialization;
using static Sopdu.Devices.MotionControl.DeltaController.DeltaEtherCAT;

namespace Sopdu.Devices.MotionControl.DeltaEtherCAT
{
    public class DeltaEtherCATAxis : Axis
    {
        [XmlIgnore]
        public const float G_CONSTANT = 9806.65F;

        //protected static const float G_CONSTANT = 9806.65F;

        private EStatus _rawStatus;

        private DeltaControllerChannel channel;

        [XmlIgnore]
        public bool bModbusActive;

        public DeltaEtherCATAxis(DeltaControllerChannel channel, byte axisNumber)
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
            get { return "DeltaControllerAxis_" + channel.Name + "_" + AxisNumber; }
        }

        [XmlIgnore]
        public EStatus RawStatus
        {
            get { return _rawStatus; }
            set
            {
                _rawStatus = value;
                // Also update CurrentStatus
                if (value.ERR_SERVOFF)
                {
                    CurrentStatus = AxisStatus.Alarm;
                }
                else if (value.IM_STOP)
                {
                    CurrentStatus = AxisStatus.EStopped;
                }
                else if (!value.SERVON_DONE)
                {
                    CurrentStatus = AxisStatus.ServoOff;
                }
                else if (!value.ALLOW_OP)
                {
                    CurrentStatus = AxisStatus.Uninitialized;
                }
                else if (!value.IN_POS)
                {
                    CurrentStatus = AxisStatus.Moving;
                }
                else
                {
                    CurrentStatus = AxisStatus.Ready;
                }
                if (value.IN_POS == true)
                    PositionEnd = true;
                if (value.IN_POS == false)
                    PositionEnd = false;
            }
        }

        public enum ControlWordBit : byte
        {
            ENABLE = 0,
            PWR = 1,
            IM_STOP = 2,
            SERVON = 3,
            HEND = 4,
            OP_MODE_RSV1 = 5,
            OP_MODE_RSV2 = 6,
            OP_MODE_RSV3 = 7,
            PAUSE = 8,
            RSV1 = 9,
            RSV2 = 10,
            RSV3 = 11,
            RSV4 = 12,
            RSV5 = 13,
            RSV6 = 14,
            RSV7 = 15,
        }
        public enum EStatusdBit : byte
        {
            EN_STATUS = 0,
            ALLOW_OP = 1,
            SERVON_DONE = 2,
            ERR_SERVOFF = 3,
            PWR = 4,
            IM_STOP = 5,
            NO_ALLOW_OP = 6,
            WARN = 7,
            RSV1 = 8,
            CONNECT = 9,
            IN_POS = 10,
            LMT_SOR = 11, //驅動器極限觸發狀態，台達驅動器不支援本功能
            OP_MODE_RSV1 = 12,
            OP_MODE_RSV2 = 13,
            RSV2 = 14,
            RSV3 = 15,
        }

        public override void ServoOn(bool bEMOExit, bool isAutoOp = false)
        {
            
            Console.WriteLine("ServoOn");

            bIsEnable = false;
            CommandDoneEvent.Reset();
            bool result = SetCommand(AxisCommand.ServoOn);
            result = WaitMsgRx(2000);
            CommandSetEvent.Reset();
            CommandDoneEvent.Reset();
            bIsEnable = true;
            if (!result)
            {
                throw new Exception("ServoOn Failed");
            }

            if (opBrake != null)
                opBrake.SetOutput(true);


        }

        //public override void AlarmReset(bool isAutoOp = false)
        //{
        //    Console.WriteLine("AlarmReset");
        //}

        //public override void ServoOff(bool isAutoOp = false)
        //{
        //    Console.WriteLine("ServoOff");
        //}

        //public override void StartHomeSearch(bool bEMOExit, bool isAutoOp = false)
        //{
        //    Console.WriteLine("StartHomeSearch");
        //}



        //public override void StartMove(int positionNumber, bool isAutoOp = false)
        //{
        //    Console.WriteLine($"StartMove positionNumber={positionNumber}");
        //}

        //public override void StartMove(AxisPosition position, bool isAutoOp = false)
        //{
        //    Console.WriteLine($"StartMove positionNumber={position.ToString()}");
        //}

        //public override bool StartMove_(AxisPosition position, bool isAutoOp = false)
        //{
        //    Console.WriteLine($"StartMove_ positionNumber={position.ToString()}");
        //    return true;
        //}

        //public override void SetModBusOn()
        //{
        //    Console.WriteLine("SetModBusOn");
        //    bIsEnable = false;
        //    bool result = SetCommand(AxisCommand.ModBusOn);
        //    bIsEnable = true;

            
        //}

        //public override void SetModBusOff()
        //{
        //    Console.WriteLine("SetModBusOff");
        //    bIsEnable = false;
        //    bool result = SetCommand(AxisCommand.ModBusOff);
        //    bIsEnable = true;
            
        //}
    }
}
