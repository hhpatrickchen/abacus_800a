using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sopdu.Devices.Base
{
    //public class TCPTIPTelnetClient : GenericDevice
    //{
    //    private Socket senderSock;
    //    private string _IPAddress;
    //    public string IPAddress { get { return _IPAddress; } set { _IPAddress = value; NotifyPropertyChanged(); } }

    //    private int _port;
    //    public int port { get { return _port; } set { _port = value; NotifyPropertyChanged(); } }

    //    public void Init()
    //    {
    //        ModbusTCP.Master modbus = new ModbusTCP.Master("192.168.1.3", 502);
    //        byte[] byteout = new byte[2];
    //        byteout[0] = 0;
    //        byteout[1] = 0;
    //        byte[] byteresult = new byte[2];
    //        modbus.WriteSingleRegister(200, 1, 2048, byteout, ref byteresult);
    //        //modbus.ReadWriteMultipleRegister()
    //        //modbus.WriteMultipleRegister(200,1,)
    //    }

    //    public override string Name
    //    {
    //        get { throw new NotImplementedException(); }
    //    }
    //}
}