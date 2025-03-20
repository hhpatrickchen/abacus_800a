using HslCommunication;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication.ModBus;
using System.Diagnostics;

namespace ModbusTCP
{
    public class Master
    {
        private string _ip;
        private int _port;

        private bool _connectStatus = false;

        private ModbusTcpNet modbusTcpNet = null;

        public bool connected  => _connectStatus;
        public Master() { }
        public Master(string ip, ushort port)
        {
            _ip = ip;
            _port = port;
            connect(ip, port);
        }

        public bool connect(string ip, ushort port) 
        {
            try
            {
                modbusTcpNet = new ModbusTcpNet(_ip, _port, 1);
                OperateResult operateResult = modbusTcpNet.ConnectServer();
                if (!operateResult.IsSuccess)
                {
                    _connectStatus = false;
                    return false;
                }

                _connectStatus = true;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool WriteCompleteSignal(string strPlcAd, byte[] result)
        {
            if (!_connectStatus)
                return false;
           
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();
            try
            {
               
                HslCommunication.OperateResult operateResult = modbusTcpNet.Write(strPlcAd, result);

                int iCount = 0;
                if (operateResult.IsSuccess == false)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        operateResult = modbusTcpNet.Write(strPlcAd, result);
                        if (operateResult.IsSuccess == true)
                        {
                            break;
                        }
                        else
                        {
                            iCount++;
                        }
                    }
                }

                return operateResult.IsSuccess;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public bool WriteSingleRegister(ushort id, byte unit, ushort startAddress, byte[] values)
        {
            try
            {
                return WriteCompleteSignal(startAddress.ToString(), values);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"异常发生：{ex.Message}");
                return false;
            }
        }

        public bool WriteMultipleRegister(ushort id, byte unit, ushort startAddress, byte[] values, ref byte[] result)
        {
            try
            {
                if ( WriteCompleteSignal(startAddress.ToString(), values))
                {
                    return true;
                } else
                {
                    result = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                result = null;
                return false;
            }
        }

        public bool ReadDiscreteInputs(ushort id, byte unit, ushort startAddress, ushort numInputs, ref byte[] values)
        {
            try
            {
                if (!_connectStatus)
                {
                    return false;
                }
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                try
                {
                    var num = Math.Ceiling(numInputs / 16f);
                    OperateResult<byte[]> operateResult = modbusTcpNet.Read(startAddress.ToString(), (ushort)num);


                    if (operateResult.IsSuccess)
                    {
                        var bytenum = Math.Ceiling(numInputs / 8f);
                        values = HslCommunication.BasicFramework.SoftBasic.BytesReverseByWord(operateResult.Content);
                        values = values.Take((int)bytenum).ToArray();
                        return true;

                    }
                    else
                    {

                        return false;
                    }
                }
                catch
                {
                    return false;
                }
                finally
                {
                    stopwatch.Stop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"异常发生：{ex.Message}");
                return false;
            }
        }
    }
}
