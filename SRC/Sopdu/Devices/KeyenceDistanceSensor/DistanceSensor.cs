using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sopdu.Devices.KeyenceDistanceSensor
{
    public class DistanceSensor
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        TcpClient mTcpClient;
        byte[] mRx;
        IPAddress _ipAddress;
        int _port;


        public DistanceSensor(IPAddress ipa, int nPort)
        {
            mTcpClient = new TcpClient() { ReceiveTimeout = 3000, SendTimeout = 3000 };
            _ipAddress = ipa;
            _port = nPort;
            mTcpClient.BeginConnect(_ipAddress, _port, onCompleteConnect, mTcpClient);
        }


        public bool SendCommand()
        {
            int i = 0;
            while (i < 3)
            {
                byte[] tx;
                tx = Encoding.ASCII.GetBytes("M0" + '\r' + '\n');
                try
                {
                    mTcpClient.GetStream().Write(tx, 0, tx.Length);
                    return true;
                }
                catch (Exception ex)
                {
                    mTcpClient.Close();
                    mTcpClient.Dispose();
                    mTcpClient = new TcpClient() { ReceiveTimeout = 3000, SendTimeout = 3000 };
                    mTcpClient.Connect(_ipAddress, _port);
                    i++;
                }
            }
            return false;
        }





        public void onCompleteConnect(IAsyncResult iar)
        {
            TcpClient tcpc;
            try
            {
                tcpc = (TcpClient)iar.AsyncState;
                tcpc.EndConnect(iar);
            }
            catch (Exception ex)
            {
                log.Error("Keyence distance sensor  connection error:" + ex.Message);
            }
        }
        public string strReceived = null;

        public bool ReadFromServer()
        {
            try
            {
                mRx = new byte[512];
                var byteNum = mTcpClient.GetStream().Read(mRx, 0, mRx.Length);
                if (byteNum == 0)
                {
                    throw new Exception(" Keyence distance sensor connection closed");
                }
                strReceived = Encoding.ASCII.GetString(mRx, 0, byteNum);
                return true;

            }
            catch (Exception ex)
            {
                log.Error("Connection error: " + ex.Message);
                return false;
            }
        }

    }
}
