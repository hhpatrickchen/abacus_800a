using Sopdu.Devices.MotionControl.Base;
using Sopdu.Devices.MotionControl.IAIController.PConAxis;
using Sopdu.helper;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Xml.Serialization;

namespace Sopdu.Devices.MotionControl.DeltaEtherCAT
{
    public class DeltaEtherCATAxis : Axis
    {
        [XmlIgnore]
        public const float G_CONSTANT = 9806.65F;

        //protected static const float G_CONSTANT = 9806.65F;

        private PconModbus.Status _rawStatus;

        private PconControllerChannel channel;

        [XmlIgnore]
        public bool bModbusActive;

        public DeltaEtherCATAxis(PconControllerChannel channel, byte axisNumber)
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
            get { return "DeltaControllerAxis_" + channel.COMAddress + "_" + AxisNumber; }
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
}
