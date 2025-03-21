using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Sopdu.Devices.MotionControl.IAIController.PConAxis.PconModbus;

namespace Sopdu.Devices.MotionControl.DeltaController
{
    public class DeltaEtherCAT
    {
        ushort CardNo;
        ushort NodeID;
        ushort SlotNo;

        public Exception LastException { get; private set; }
        public DeltaEtherCAT(ushort cardNo, ushort nodeID, ushort slotNo)
        {
            CardNo = cardNo;
            NodeID = nodeID;
            SlotNo = slotNo;

            //initialize Delta EtherCAT
        }

        //include Delta dll

        ~DeltaEtherCAT()
        {
            //if ((this.serialPort != null) && this.serialPort.IsOpen)
            //{
            //    Close();
            //}
        }

        public int Open()
        {
            //try
            //{
            //    //serialPort.Close();
            //    int maxretry = 10;
            //    const int sleeptimems = 1000;
            //    while (maxretry > 0)
            //    {
            //        try
            //        {
            //            MessageListener.Instance.ReceiveMessage(serialPort.PortName + " try to open..");
            //            serialPort.Open();
            //            MessageListener.Instance.ReceiveMessage(serialPort.PortName + " open successfully");
            //            Thread.Sleep(sleeptimems);
            //            break;
            //        }
            //        catch (Exception ex)
            //        {
            //            maxretry--;

            //            MessageListener.Instance.ReceiveMessage(serialPort.PortName + " Fail to open, retry");
            //            Thread.Sleep(sleeptimems);
            //        }
            //    }

            //    serialPort.DiscardInBuffer();
            //    serialPort.DiscardOutBuffer();
            //}
            //catch (Exception ex)
            //{
            //    LastException = ex;
            //    MessageListener.Instance.ReceiveMessage("COM PORT " + serialPort.PortName + " Fail to open");
            //    Thread.Sleep(1000);
            //    //serialPort.Close();
            //    //serialPort.Open();
            //    //serialPort.DiscardInBuffer();
            //    //serialPort.DiscardOutBuffer();
            //    return -1;
            //}
            //LastException = null;
            return 0;
        }

        public int Close()
        {
            //try
            //{
            //    serialPort.Close();
            //}
            //catch (Exception ex)
            //{
            //    LastException = ex;
            //    return -1;
            //}
            LastException = null;
            return 0;
        }

        public class ControlWordStatus
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

            public ControlWordStatus(ushort value)
            {
                this._NA1 = (value & (1 << (byte)StatusBit._NA1)) != 0;
                this.CLBS = (value & (1 << (byte)StatusBit.CLBS)) != 0;
                this.CEND = (value & (1 << (byte)StatusBit.CEND)) != 0;
                this.PEND = (value & (1 << (byte)StatusBit.PEND)) != 0;
                this.HEND = (value & (1 << (byte)StatusBit.HEND)) != 0;
                this.STP = (value & (1 << (byte)StatusBit.STP)) != 0;
                this._NA2 = (value & (1 << (byte)StatusBit._NA2)) != 0;
                this.BKRL = (value & (1 << (byte)StatusBit.BKRL)) != 0;
                this.ABER = (value & (1 << (byte)StatusBit.ABER)) != 0;
                this.ALML = (value & (1 << (byte)StatusBit.ALML)) != 0;
                this.ALMH = (value & (1 << (byte)StatusBit.ALMH)) != 0;
                this.PSFL = (value & (1 << (byte)StatusBit.PSFL)) != 0;
                this.SV = (value & (1 << (byte)StatusBit.SV)) != 0;
                this.PWR = (value & (1 << (byte)StatusBit.PWR)) != 0;
                this.SFTY = (value & (1 << (byte)StatusBit.SFTY)) != 0;
                this.EMG = (value & (1 << (byte)StatusBit.EMG)) != 0;
            }

            public static explicit operator ControlWordStatus(ushort value)
            {
                return new ControlWordStatus(value);
            }

            public static explicit operator ushort(ControlWordStatus value)
            {
                int result = 0;
                result |= (value._NA1 ? 1 : 0) << ((byte)StatusBit._NA1);
                result |= (value.CLBS ? 1 : 0) << ((byte)StatusBit.CLBS);
                result |= (value.CEND ? 1 : 0) << ((byte)StatusBit.CEND);
                result |= (value.PEND ? 1 : 0) << ((byte)StatusBit.PEND);
                result |= (value.HEND ? 1 : 0) << ((byte)StatusBit.HEND);
                result |= (value.STP ? 1 : 0) << ((byte)StatusBit.STP);
                result |= (value._NA2 ? 1 : 0) << ((byte)StatusBit._NA2);
                result |= (value.BKRL ? 1 : 0) << ((byte)StatusBit.BKRL);
                result |= (value.ABER ? 1 : 0) << ((byte)StatusBit.ABER);
                result |= (value.ALML ? 1 : 0) << ((byte)StatusBit.ALML);
                result |= (value.ALMH ? 1 : 0) << ((byte)StatusBit.ALMH);
                result |= (value.PSFL ? 1 : 0) << ((byte)StatusBit.PSFL);
                result |= (value.SV ? 1 : 0) << ((byte)StatusBit.SV);
                result |= (value.PWR ? 1 : 0) << ((byte)StatusBit.PWR);
                result |= (value.SFTY ? 1 : 0) << ((byte)StatusBit.SFTY);
                result |= (value.EMG ? 1 : 0) << ((byte)StatusBit.EMG);
                return (ushort)result;
            }

            //public int GetPosition()
            //{
            //    return (PM32 ? 32 : 0) | (PM16 ? 16 : 0) | (PM8 ? 8 : 0) | (PM4 ? 4 : 0) | (PM2 ? 2 : 0) | (PM1 ? 1 : 0);
            //}

            public bool IsError()
            {
                return !ABER || !SV || !EMG || !ALMH || !ALML;
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
    }
}
