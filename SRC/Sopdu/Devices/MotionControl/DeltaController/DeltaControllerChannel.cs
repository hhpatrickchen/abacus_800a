using Sopdu.Devices.MotionControl.DeltaEtherCAT;
using Sopdu.Devices.MotionControl.IAIController.PConAxis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopdu.Devices.MotionControl.DeltaController
{
    public class DeltaControllerChannel
    {
        public DeltaControllerChannel(ushort cardNo,ushort nodeID, ushort slotNo)
            : base()
        {
            this.AxisList = new DeltaEtherCATAxis[16];
            this.CardNo = cardNo;
            this.NodeID = nodeID;
            this.SlotNo = slotNo;
        }

        public DeltaEtherCATAxis[] AxisList { get; private set; }

        public ushort CardNo { set; get; }
        public ushort NodeID { set; get; }
        public ushort SlotNo { set; get; }


        public string Name
        {
            get
            { 
                string name=$"DeltaControllerChannel_{CardNo}_{NodeID}_{SlotNo}";
                return "DeltaControllerChannel_" + name; 
            }
        }

        public DeltaEtherCATAxis GetAxis(int axisNumber)
        {
            if (axisNumber < 0 || axisNumber > 15)
            {
                throw new ArgumentException("Invalid axisNumber");
            }
            if (AxisList[axisNumber] == null)
            {
                // Create PconControllerAxis, as it is not created yet.
                DeltaEtherCATAxis axis = new DeltaEtherCATAxis(this, (byte)axisNumber);
                AxisList[axisNumber] = axis;
            }
            return AxisList[axisNumber];
        }
    }
}
