using Sopdu.Devices.IOModule;
using Sopdu.helper;
using Sopdu.ProcessApps.ProcessModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sopdu.Devices.Cylinders
{
    public abstract class absoloniod : GenericDevice
    {
        public ProcessMode pmode;

        public string _ErrMsg;
        public string ErrMsg { get { return _ErrMsg; } set { _ErrMsg = value; } }

        public string _CycName;

        public string CycName
        { get { return _CycName; } set { _CycName = value; } }

        public string _AlarmLogicalName;

        public string AlarmLogicalName
        { get { return _AlarmLogicalName; } set { _AlarmLogicalName = value; } }

        public string _Cyc_OP01_Name;

        public string Cyc_OP01_Name
        { get { return _Cyc_OP01_Name; } set { _Cyc_OP01_Name = value; } }

        public string _Cyc_OP02_Name;

        public string Cyc_OP02_Name
        { get { return _Cyc_OP02_Name; } set { _Cyc_OP02_Name = value; } }

        public string _Cyc_IP01_Name;

        public string Cyc_IP01_Name
        { get { return _Cyc_IP01_Name; } set { _Cyc_IP01_Name = value; } }

        public string _Cyc_IP02_Name;

        public string Cyc_IP02_Name
        { get { return _Cyc_IP02_Name; } set { _Cyc_IP02_Name = value; } }

        public DiscreteIO _Cyc_OP01;

        public int _Ip01Timeout;
        public int IP01Timeout { get { return _Ip01Timeout; } set { _Ip01Timeout = value; } }

        public int _Ip02Timeout;
        public int IP02Timeout { get { return _Ip02Timeout; } set { _Ip02Timeout = value; } }

        [XmlIgnore]
        public DiscreteIO Cyc_OP01 { get { return _Cyc_OP01; } set { _Cyc_OP01 = value; } }

        public DiscreteIO _Cyc_OP02;

        [XmlIgnore]
        public DiscreteIO Cyc_OP02 { get { return _Cyc_OP02; } set { _Cyc_OP02 = value; } }

        public DiscreteIO _Cyc_IP01;

        [XmlIgnore]
        public DiscreteIO Cyc_IP01 { get { return _Cyc_IP01; } set { _Cyc_IP01 = value; NotifyPropertyChanged(); } }

        public DiscreteIO _Cyc_IP02;

        [XmlIgnore]
        public DiscreteIO Cyc_IP02 { get { return _Cyc_IP02; } set { _Cyc_IP02 = value; NotifyPropertyChanged(); } }

        [XmlIgnore]
        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        [XmlIgnore]
        public virtual bool bRetract { get; set; }

        [XmlIgnore]
        public virtual bool bExtend { get; set; }
    }

    public interface Isoloniod
    {
        bool Extend();

        bool Retract();

        bool WaitExtend();

        bool WaitRetract();

        int GetTimeoutExt();

        int GetTimeoutRtn();
    }

    public class SingleActuatedCyc : absoloniod, Isoloniod
    {
        public virtual bool Extend()
        {
            if (Cyc_OP01 != null)
            {
                //use cyc 01
                Cyc_OP01.SetOutput(true);
            }
            else
            {
                Cyc_OP02.SetOutput(false);
            }
            return true;
        }

        public virtual bool Retract()
        {
            if (Cyc_OP01 != null)
            {
                //use cyc 01
                Cyc_OP01.SetOutput(false);
            }
            else
            {
                Cyc_OP02.SetOutput(true);
            }

            return true;
        }

        public virtual bool WaitExtend()
        {
            if (Cyc_IP01 != null)
            {
                int cnt = IP01Timeout / 100;
                int rm = IP01Timeout % 100;
                int cntdown = cnt;
                while (true)
                {
                    pmode.ChkProcessMode();
                    if (cntdown >= 0)
                    {
                        if (cntdown == 0)
                            if (Cyc_IP01.evtOn.WaitOne(rm) == true)
                                return true;
                        if (cntdown > 0)
                            if (Cyc_IP01.evtOn.WaitOne(100) == true)
                                return true;
                        cntdown--;
                    }
                    else
                    {
                        int pos = Cyc_IP01.IOName.IndexOf("Input");
                        string ErrCode = "CE" + Cyc_IP01.IOName.Remove(pos);
                        ErrMsg = this._CycName + ": " + Cyc_IP01.IOName + " Wait On timeout : " + IP01Timeout.ToString() + "ms WaitExtend exceeded ";
                        pmode.SetError(ErrMsg, false, ErrCode);
                        try
                        {
                            pmode.GemCtrl.SetAlarm(this._CycName+ " Extend Error");
                        }
                        catch (Exception ex)
                        {
                            pmode.SetInfoMsg(ex.ToString());
                        }
                        pmode.ChkProcessMode();//<== wait for retry?
                        cnt = IP01Timeout / 100;
                        rm = IP01Timeout % 100;
                        cntdown = cnt;
                    }
                }
            }
            else
            {
                int cnt = IP02Timeout / 100;
                int rm = IP02Timeout % 100;
                int cntdown = cnt;
                while (true)
                {
                    pmode.ChkProcessMode();
                    if (cntdown >= 0)
                    {
                        if (cntdown == 0)
                        {
                            if (Cyc_IP02.evtOff.WaitOne(rm) == true)
                                return true;
                        }
                        if (cntdown > 0)
                        {
                            if (Cyc_IP02.evtOff.WaitOne(100) == true)
                                return true;
                        }
                        cntdown--;
                    }
                    else
                    {
                        int pos = Cyc_IP01.IOName.IndexOf("Input");
                        string ErrCode = "CE" + Cyc_IP01.IOName.Remove(pos);
                        ErrMsg = this._CycName + ": " + Cyc_IP02.IOName + " Wait Off timeout : " + IP02Timeout.ToString() + " ms WaitExtend exceeded";
                        pmode.SetError(ErrMsg, true, ErrCode);
                        try
                        {
                            //  pmode.GemCtrl.SetAlarm(this.AlarmLogicalName);
                            pmode.GemCtrl.SetAlarm(this._CycName + " Extend Error");
                        }
                        catch (Exception ex)
                        {
                            pmode.SetInfoMsg(ex.ToString());
                        }
                        pmode.ChkProcessMode();//<== wait for retry?
                        cnt = IP01Timeout / 100;
                        rm = IP01Timeout % 100;
                        cntdown = cnt;
                    }
                }
            }
            return false;
        }

        public virtual bool WaitRetract()
        {
            if (Cyc_IP02 != null)
            {
                int cnt = IP02Timeout / 100;
                int rm = IP02Timeout % 100;
                int cntdown = cnt;
                while (true)
                {
                    pmode.ChkProcessMode();
                    if (cntdown >= 0)
                    {
                        if (cntdown == 0)
                            if (Cyc_IP02.evtOn.WaitOne(rm) == true)
                                return true;
                        if (cntdown > 0)
                            if (Cyc_IP02.evtOn.WaitOne(100) == true)
                                return true;
                        cntdown--;
                    }
                    else
                    {
                        int pos = Cyc_IP02.IOName.IndexOf("Input");
                        string ErrCode = "CR" + Cyc_IP02.IOName.Remove(pos);
                        ErrMsg = this._CycName + ": " + Cyc_IP02.IOName + " Wait On timeout : " + IP02Timeout.ToString() + " ms WaitRetract exceeded";
                        pmode.SetError(ErrMsg, true, ErrCode);
                        try
                        {
                            //  pmode.GemCtrl.SetAlarm(this.AlarmLogicalName);
                            pmode.GemCtrl.SetAlarm(this._CycName+ " Retract Error");
                        }
                        catch (Exception ex)
                        {
                            pmode.SetInfoMsg(ex.ToString());
                        }
                        while (pmode.getErrorState())
                        {
                            Thread.Sleep(100);
                            pmode.ChkProcessMode();//<== wait for retry?
                        }
                        //reset counter
                        cnt = IP02Timeout / 100;
                        rm = IP02Timeout % 100;
                        cntdown = cnt;
                    }
                }
            }
            else
            {
                int cnt = IP01Timeout / 100;
                int rm = IP01Timeout % 100;
                int cntdown = cnt;
                while (true)
                {
                    pmode.ChkProcessMode();
                    if (cntdown >= 0)
                    {
                        if (cntdown == 0)
                            if (Cyc_IP01.evtOff.WaitOne(rm) == true)
                                return true;
                        if (cntdown > 0)
                            if (Cyc_IP01.evtOff.WaitOne(100) == true)
                                return true;
                        cntdown--;
                    }
                    else
                    {
                        int pos = Cyc_IP01.IOName.IndexOf("Input");
                        string ErrCode = "CR" + Cyc_IP01.IOName.Remove(pos);
                        ErrMsg = this._CycName + ": " + Cyc_IP01.IOName + " Wait Off timeout : " + IP01Timeout.ToString() + " ms WaitRetract exceeded";
                        pmode.SetError(ErrMsg, true, ErrCode);
                        try
                        {
                            pmode.GemCtrl.SetAlarm(this._CycName + " Retract Error");
                        }
                        catch (Exception ex)
                        {
                            pmode.SetInfoMsg(ex.ToString());
                        }
                        pmode.ChkProcessMode();//<== wait for retry? only exit is exception if cannot turn on
                        //reset counter
                        cnt = IP01Timeout / 100;
                        rm = IP01Timeout % 100;
                        cntdown = cnt;
                    }
                }
            }
            return false;
        }

        public virtual int GetTimeoutExt()
        {
            return IP01Timeout;
        }

        public virtual int GetTimeoutRtn()
        {
            return IP02Timeout;
        }
    }

    public class DualActuatedCyc : absoloniod, Isoloniod
    {
        public virtual bool Extend()
        {
            Cyc_OP01.SetOutput(true);
            Cyc_OP02.SetOutput(false);
            return true;
        }

        public virtual bool Retract()
        {
            Cyc_OP01.SetOutput(false);
            Cyc_OP02.SetOutput(true);

            return true;
        }

        public virtual bool WaitExtend()
        {
            int cnt = IP01Timeout / 100;
            int rm = IP01Timeout % 100;
            int cntdown = cnt;
            while (true)
            {
                pmode.ChkProcessMode();
                if (cntdown >= 0)
                {
                    if (cntdown == 0)
                        if (Cyc_IP01.evtOn.WaitOne(rm) == true)
                            return true;
                    if (cntdown > 0)
                        if (Cyc_IP01.evtOn.WaitOne(100) == true)
                            return true;
                    cntdown--;
                }
                else
                {
                    ErrMsg = this._CycName + ": " + Cyc_IP01.IOName + " Wait On timeout : " + IP01Timeout.ToString() + " ms WaitExtend Exceeded";
                    pmode.SetError(ErrMsg, true, AlarmLogicalName);
                    string alarmName = null;
                    switch (this._CycName)
                    {
                        case "IPCVRotator":
                            alarmName = "ER_CYC01Error";
                            break;
                        case "IPCVExtension":
                            alarmName = "ER_CYC02Error";
                            break;
                        case "IPCVLifter":
                            alarmName = "ER_CYC03Error";
                            break;
                        case "InputShutterDoor":
                            alarmName = "ER_CYC04Error";
                            break;
                        case "TrayLifterPlate":
                            alarmName = "ER_CYC05Error";
                            break;
                        case "BenchingBar":
                            alarmName = "ER_CYC06Error";
                            break;
                        case "Shutter Plate CYL":
                            if (Cyc_IP01.IOName == "Input42")
                                alarmName = "ER_CYC09Error";
                            if (Cyc_IP01.IOName == "Input26")
                                alarmName = "ER_CYC07Error";
                            break;
                        case "Shutter Singulator Pin CYL":
                            if (Cyc_IP01.IOName == "Input27" || Cyc_IP01.IOName == "Input28")
                            alarmName = "ER_CYC08Error";
                            else alarmName = "ER_CYC10Error";
                            break;
                        case "Output Singulator":
                            alarmName = "ER_CYC11Error";
                            break;
                        case "Output Benching Bar":
                            alarmName = "ER_CYC12Error";
                            break;
                        case "OPCVRotator":
                            alarmName = "ER_CYC13Error";
                            break;
                        case "OPCVExtension":
                            alarmName = "ER_CYC14Error";
                            break;
                        case "OPCVLifter":
                            alarmName = "ER_CYC15Error";
                            break;
                        case "OPCVTailGate":
                            alarmName = "ER_CYC16Error";
                            break;
                        case "OutputShutterDoor":
                            alarmName = "ER_CYC17Error";
                            break;
                    }
                    
                    try
                    {
                        pmode.GemCtrl.SetAlarm(alarmName);
                    }
                    catch (Exception ex)
                    {
                        pmode.SetInfoMsg(ex.ToString());
                    }
                    pmode.ChkProcessMode();//<== wait for retry?
                    cnt = IP01Timeout / 100;
                    rm = IP01Timeout % 100;
                    cntdown = cnt;
                }
            }

            return false;
        }

        public virtual bool WaitRetract()
        {
            int cnt = IP02Timeout / 100;
            int rm = IP02Timeout % 100;
            int cntdown = cnt;
            while (true)
            {
                pmode.ChkProcessMode();
                if (cntdown >= 0)
                {
                    if (cntdown == 0)
                        if (Cyc_IP02.evtOn.WaitOne(rm) == true) return true;
                    if (cntdown > 0)
                        if (Cyc_IP02.evtOn.WaitOne(100) == true) return true;
                    cntdown--;
                }
                else
                {
                    ErrMsg = this._CycName + ": " + Cyc_IP02.IOName + " Wait On timeout : " + IP02Timeout.ToString() + " ms WaitRetract Exceeded";
                    pmode.SetError(ErrMsg, true, AlarmLogicalName);
                    string alarmName = null;
                    switch (this._CycName)
                    {
                        case "IPCVRotator":
                            alarmName = "ER_CYC01Error";
                            break;
                        case "IPCVExtension":
                            alarmName = "ER_CYC02Error";
                            break;
                        case "IPCVLifter":
                            alarmName = "ER_CYC03Error";
                            break;
                        case "InputShutterDoor":
                            alarmName = "ER_CYC04Error";
                            break;
                        case "TrayLifterPlate":
                            alarmName = "ER_CYC05Error";
                            break;
                        case "BenchingBar":
                            alarmName = "ER_CYC06Error";
                            break;
                        case "Shutter Plate CYL":
                            if (Cyc_IP02.IOName == "Input41" )
                                alarmName = "ER_CYC09Error";
                            if (Cyc_IP02.IOName == "Input25")
                                alarmName = "ER_CYC07Error";
                            break;
                        case "Shutter Singulator Pin CYL":
                            if (Cyc_IP02.IOName == "Input27" || Cyc_IP02.IOName == "Input28")
                                alarmName = "ER_CYC08Error";
                            else alarmName = "ER_CYC10Error";
                            break;
                        case "Output Singulator":
                            alarmName = "ER_CYC11Error";
                            break;
                        case "Output Benching Bar":
                            alarmName = "ER_CYC12Error";
                            break;
                        case "OPCVRotator":
                            alarmName = "ER_CYC13Error";
                            break;
                        case "OPCVExtension":
                            alarmName = "ER_CYC14Error";
                            break;
                        case "OPCVLifter":
                            alarmName = "ER_CYC15Error";
                            break;
                        case "OPCVTailGate":
                            alarmName = "ER_CYC16Error";
                            break;
                        case "OutputShutterDoor":
                            alarmName = "ER_CYC17Error";
                            break;
                    }
                    //if (this._CycName == "BenchingBar" && (Cyc_IP02.IOName == "Input13"))
                    //{
                    //    alarmName = "ER_CYC01Error";
                    //}
                    //if (this._CycName == "BenchingBar" && (Cyc_IP02.IOName == "Input15"))
                    //{
                    //    alarmName = "ER_CYC02Error";
                    //}
                    //if (this._CycName == "Output Benching Bar" && (Cyc_IP02.IOName == "Input61"))
                    //{
                    //    alarmName = "ER_CYC07Error";
                    //}
                    //if (this._CycName == "Output Benching Bar" && (Cyc_IP02.IOName == "Input63"))
                    //{
                    //    alarmName = "ER_CYC08Error";
                    //}
                    try
                    {
                        pmode.GemCtrl.SetAlarm(alarmName);
                    }
                    catch (Exception ex)
                    {
                        pmode.SetInfoMsg(ex.ToString());
                    }
                    pmode.ChkProcessMode();//<== wait for retry?
                    cnt = IP01Timeout / 100;
                    rm = IP01Timeout % 100;
                    cntdown = cnt;
                }
            }
            return false;
        }

        public virtual int GetTimeoutExt()
        {
            return IP01Timeout;
        }

        public virtual int GetTimeoutRtn()
        {
            return IP02Timeout;
        }
    }

    public class ProcessEvt
    {
        public string Name
        { get; set; }

        private ManualResetEvent _evt;

        [XmlIgnore]
        public ManualResetEvent evt { get { return _evt; } set { _evt = value; } }
    }
}