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

        private ControlWordStatus _rawStatus;

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
        public ControlWordStatus RawStatus
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

        public override void AlarmReset(bool isAutoOp = false)
        {
            Console.WriteLine("AlarmReset");
        }

        public override void ServoOff(bool isAutoOp = false)
        {
            Console.WriteLine("ServoOff");
        }

        public override void StartHomeSearch(bool bEMOExit, bool isAutoOp = false)
        {
            Console.WriteLine("StartHomeSearch");
        }



        public override void StartMove(int positionNumber, bool isAutoOp = false)
        {
            Console.WriteLine($"StartMove positionNumber={positionNumber}");
        }

        public override void StartMove(AxisPosition position, bool isAutoOp = false)
        {
            Console.WriteLine($"StartMove positionNumber={position.ToString()}");
        }

        public override bool StartMove_(AxisPosition position, bool isAutoOp = false)
        {
            Console.WriteLine($"StartMove_ positionNumber={position.ToString()}");
            return true;
        }

        public override void SetModBusOn()
        {
            Console.WriteLine("SetModBusOn");
        }

        public override void SetModBusOff()
        {
            Console.WriteLine("SetModBusOff");
        }
    }
}
