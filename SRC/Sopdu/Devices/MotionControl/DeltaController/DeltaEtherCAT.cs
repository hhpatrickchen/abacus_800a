using EtherCAT_DLL_Err;
using EtherCAT_DLL_x64;
using Sopdu.Devices.MotionControl.DeltaEtherCAT;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using static Sopdu.Devices.MotionControl.DeltaEtherCAT.DeltaEtherCATAxis;


namespace Sopdu.Devices.MotionControl.DeltaController
{
    public class DeltaEtherCAT
    {

        bool g_bInitialFlag = false;
        ushort g_nESCExistCards = 0, g_uESCCardNo = 0, g_uESCNodeID = 0, g_uESCSlotID;
        ushort g_uRet = 0;
        ushort[] g_uESCCardNoList = new ushort[32];
        public Exception LastException { get; private set; }

        public bool Initial_Card()
        {
            ushort uCount = 0, uCardNo = 0;

            MessageListener.Instance.ReceiveMessage($"g_nESCExistCards={g_nESCExistCards}" + " try to open..");
            g_uRet = CEtherCAT_DLL.CS_ECAT_Master_Open(ref g_nESCExistCards);
            g_bInitialFlag = false;
            if (g_nESCExistCards == 0)
            {
                MessageListener.Instance.ReceiveMessage("No EtherCat can be found!");
                return false;
            }
            else
            {
                for (uCount = 0; uCount < 32; uCount++)
                {
                    g_uESCCardNoList[uCount] = 99;
                }

                for (uCount = 0; uCount < g_nESCExistCards; uCount++)
                {
                    g_uRet = CEtherCAT_DLL.CS_ECAT_Master_Get_CardSeq(uCount, ref uCardNo);
                    g_uRet = CEtherCAT_DLL.CS_ECAT_Master_Initial(uCardNo);
                    if (g_uRet != 0)
                    {
                        MessageListener.Instance.ReceiveMessage("_ECAT_Master_Initial, ErrorCode = " + g_uRet.ToString());
                    }
                    else
                    {
                        g_uESCCardNoList[uCount] = uCardNo;
                        //CmbCardNo.Items.Add(uCardNo.ToString());
                        g_bInitialFlag = true;
                    }
                }

                if (g_bInitialFlag == true)
                {
                    //CmbCardNo.SelectedIndex = 0;
                    g_uESCCardNo = g_uESCCardNoList[0];
                }
            }

            return g_bInitialFlag;

        }
        public DeltaEtherCAT(ushort cardNo, ushort nodeID, ushort slotNo)
        {
            g_uESCCardNo = cardNo;
            g_uESCNodeID = nodeID;
            g_uESCSlotID = slotNo;

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
            try
            {
                short nSID = 0, Cnt = 0;
                ushort uNID = 0, uSlaveNum = 0, uReMapNodeID = 0;
                uint uVendorID = 0, uProductCode = 0, uRevisionNo = 0, uSlaveDCTime = 0;
                //serialPort.Close();
                int maxretry = 10;
                const int sleeptimems = 1000;
                while (maxretry > 0)
                {
                    try
                    {
                        MessageListener.Instance.ReceiveMessage($"g_nESCExistCards={g_nESCExistCards}" + " try to open..");
                        //todo need to modify
                     
                        MessageListener.Instance.ReceiveMessage($"g_nESCExistCards={g_nESCExistCards}" + " open successfully");
                        Thread.Sleep(sleeptimems);
                        break;
                    }
                    catch (Exception ex)
                    {
                        maxretry--;

                        MessageListener.Instance.ReceiveMessage($"CardNo={g_uESCCardNo},NodeID={g_uESCNodeID},SlotNo={g_uESCSlotID}" + " Fail to open, retry");
                        Thread.Sleep(sleeptimems);
                    }
                }


                //serialPort.DiscardInBuffer();
                //serialPort.DiscardOutBuffer();
            }
            catch (Exception ex)
            {
                LastException = ex;

                MessageListener.Instance.ReceiveMessage($"CardNo={g_uESCCardNo}" + " Fail to open, retry");
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

        public int Close()
        {
            try
            {
                if (g_nESCExistCards > 0)
                {
                    for (int i = 0; i < g_nESCExistCards; i++)
                    {
                        if (g_uESCCardNoList[i] != 99)
                            CEtherCAT_DLL.CS_ECAT_Master_Reset(g_uESCCardNoList[i]);
                    }
                    CEtherCAT_DLL.CS_ECAT_Master_Close();
                }
            }
            catch (Exception ex)
            {
                LastException = ex;
                return -1;
            }
            LastException = null;
            return 0;
        }

        internal int ModBusOn(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal int ModBusOff(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal int ServoOn(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal int ServoOff(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal int HomeSearchStart(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal int HomeSearchEnd(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal int Move(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal int JogPositive(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal int JogNegative(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal int DecelerationStop(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal int Stop(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal int AlarmReset(DeltaEtherCATAxis axis)
        {
            throw new NotImplementedException();
        }

        internal ushort GetControlStatus()
        {
            throw new NotImplementedException();
        }

        internal double GetCurrentPosition()
        {
            int nPos = 0;
            CEtherCAT_DLL.CS_ECAT_Slave_Motion_Get_Position(g_uESCCardNo, g_uESCNodeID, g_uESCSlotID, ref nPos);
            return nPos;
        }

        public class EStatus
        {
            public bool EN_STATUS;
            public bool ALLOW_OP;
            public bool SERVON_DONE;
            public bool ERR_SERVOFF;
            public bool PWR;
            public bool IM_STOP;
            public bool NO_ALLOW_OP;
            public bool WARN;
            public bool RSV1;
            public bool CONNECT;
            public bool IN_POS;
            public bool LMT_SOR;
            public bool OP_MODE_RSV1;
            public bool OP_MODE_RSV2;
            public bool RSV2;
            public bool RSV3;

            public EStatus(ushort value)
            {
                this.EN_STATUS = (value & (1 << (byte)EStatusdBit.EN_STATUS)) != 0;
                this.ALLOW_OP = (value & (1 << (byte)EStatusdBit.ALLOW_OP)) != 0;
                this.SERVON_DONE = (value & (1 << (byte)EStatusdBit.SERVON_DONE)) != 0;
                this.ERR_SERVOFF = (value & (1 << (byte)EStatusdBit.ERR_SERVOFF)) != 0;
                this.PWR = (value & (1 << (byte)EStatusdBit.PWR)) != 0;
                this.IM_STOP = (value & (1 << (byte)EStatusdBit.IM_STOP)) != 0;
                this.NO_ALLOW_OP = (value & (1 << (byte)EStatusdBit.NO_ALLOW_OP)) != 0;
                this.RSV1 = (value & (1 << (byte)EStatusdBit.RSV1)) != 0;
                this.WARN = (value & (1 << (byte)EStatusdBit.WARN)) != 0;
                this.CONNECT = (value & (1 << (byte)EStatusdBit.CONNECT)) != 0;
                this.IN_POS = (value & (1 << (byte)EStatusdBit.IN_POS)) != 0;
                this.LMT_SOR = (value & (1 << (byte)EStatusdBit.LMT_SOR)) != 0;
                this.OP_MODE_RSV1 = (value & (1 << (byte)EStatusdBit.OP_MODE_RSV1)) != 0;
                this.OP_MODE_RSV2 = (value & (1 << (byte)EStatusdBit.OP_MODE_RSV2)) != 0;
                this.RSV2 = (value & (1 << (byte)EStatusdBit.RSV2)) != 0;
                this.RSV3 = (value & (1 << (byte)EStatusdBit.RSV3)) != 0;
            }

            public static explicit operator EStatus(ushort value)
            {
                return new EStatus(value);
            }

            public static explicit operator ushort(EStatus value)
            {
                int result = 0;
                result |= (value.EN_STATUS ? 1 : 0) << ((byte)EStatusdBit.EN_STATUS);
                result |= (value.ALLOW_OP ? 1 : 0) << ((byte)EStatusdBit.ALLOW_OP);
                result |= (value.SERVON_DONE ? 1 : 0) << ((byte)EStatusdBit.SERVON_DONE);
                result |= (value.ERR_SERVOFF ? 1 : 0) << ((byte)EStatusdBit.ERR_SERVOFF);
                result |= (value.PWR ? 1 : 0) << ((byte)EStatusdBit.PWR);
                result |= (value.IM_STOP ? 1 : 0) << ((byte)EStatusdBit.IM_STOP);
                result |= (value.NO_ALLOW_OP ? 1 : 0) << ((byte)EStatusdBit.NO_ALLOW_OP);
                result |= (value.RSV1 ? 1 : 0) << ((byte)EStatusdBit.RSV1);
                result |= (value.WARN ? 1 : 0) << ((byte)EStatusdBit.WARN);
                result |= (value.CONNECT ? 1 : 0) << ((byte)EStatusdBit.CONNECT);
                result |= (value.IN_POS ? 1 : 0) << ((byte)EStatusdBit.IN_POS);
                result |= (value.LMT_SOR ? 1 : 0) << ((byte)EStatusdBit.LMT_SOR);
                result |= (value.OP_MODE_RSV1 ? 1 : 0) << ((byte)EStatusdBit.OP_MODE_RSV1);
                result |= (value.OP_MODE_RSV2 ? 1 : 0) << ((byte)EStatusdBit.OP_MODE_RSV2);
                result |= (value.RSV2 ? 1 : 0) << ((byte)EStatusdBit.RSV2);
                result |= (value.RSV3 ? 1 : 0) << ((byte)EStatusdBit.RSV3);
                return (ushort)result;
            }

            //public int GetPosition()
            //{
            //    return (PM32 ? 32 : 0) | (PM16 ? 16 : 0) | (PM8 ? 8 : 0) | (PM4 ? 4 : 0) | (PM2 ? 2 : 0) | (PM1 ? 1 : 0);
            //}

            public bool IsError()
            {
                return ERR_SERVOFF;
            }

            public bool IsMoving()
            {
                return IN_POS;
            }

            //public bool IsReady()
            //{
            //    return HEND && PEND && !IsError();
            //}
                
          
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                if (EN_STATUS) { sb.Append("EN_STATUS;"); } else { sb.Append("en_status;"); }
                if (ALLOW_OP) { sb.Append("ALLOW_OP;"); } else { sb.Append("allow_op;"); }
                if (SERVON_DONE) { sb.Append("SERVON_DONE;"); } else { sb.Append("servon_done;"); }
                if (ERR_SERVOFF) { sb.Append("ERR_SERVOFF;"); } else { sb.Append("err_servoff;"); }
                if (PWR) { sb.Append("PWR;"); } else { sb.Append("pwr;"); }
                if (IM_STOP) { sb.Append("IM_STOP;"); } else { sb.Append("im_stop;"); }
                if (NO_ALLOW_OP) { sb.Append("NO_ALLOW_OP;"); } else { sb.Append("no_allow_op;"); }
                if (RSV1) { sb.Append("RSV1;"); } else { sb.Append("rsv1;"); }
                if (WARN) { sb.Append("WARN;"); } else { sb.Append("warn;"); }
                if (CONNECT) { sb.Append("CONNECT;"); } else { sb.Append("connect;"); }
                if (IN_POS) { sb.Append("IN_POS;"); } else { sb.Append("in_pos;"); }
                if (LMT_SOR) { sb.Append("LMT_SOR;"); } else { sb.Append("lmt_sor;"); }
                if (OP_MODE_RSV1) { sb.Append("OP_MODE_RSV1;"); } else { sb.Append("op_mode_rsv1;"); }
                if (OP_MODE_RSV2) { sb.Append("OP_MODE_RSV2;"); } else { sb.Append("op_mode_rsv1;"); }
                if (RSV2) { sb.Append("RSV2;"); } else { sb.Append("rsv2;"); }
                if (RSV3) { sb.Append("RSV3;"); } else { sb.Append("rsv3;"); }
                return sb.ToString();
            }
        }
    }
}
