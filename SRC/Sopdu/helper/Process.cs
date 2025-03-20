using Sopdu.Devices.Cylinders;
using Sopdu.Devices.IOModule;
using Sopdu.Devices.MotionControl.IAIController.PConAxis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace Sopdu.helper
{
    public class Process : GenericStateEngine
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ObservableCollection<SingleActuatedCyc> scyclist;
        public ObservableCollection<DualActuatedCyc> dcyclist;
        public ObservableCollection<string> InputNameList;
        public ObservableCollection<string> OutputNameList;
        public ObservableCollection<string> EvtNameList;
        public bool ignoreGemDisable;
        public string sCoverTrayPrefix;
        public bool HasDisplay;
        public bool OPNonCritical;

        //private ObservableCollection<PConController> _AxisList;
        public ObservableCollection<PConController> AxisList { get; set; }
        [XmlIgnore]
        public ObservableCollection<DiscreteIO> OutputList { get; set; }
        [XmlIgnore]
        public ObservableCollection<DiscreteIO> InputList { get; set; }
        [XmlIgnore]
        public ObservableCollection<Isoloniod> CycList { get; set; }

        public string ProcessIdentifier { get; set; }

        public void Init(GenericEvents mcEvents, ObservableCollection<SingleActuatedCyc> _scycl,
            ObservableCollection<DualActuatedCyc> _dcycl, Dictionary<string, Isoloniod> _soloniodlist, string processname)
        {
            ProcessName = processname;
            dcyclist = _dcycl;
            scyclist = _scycl;
            valvelist = _soloniodlist;

            pMode = new ProcessMode();

            pMode.Init(mcEvents);
            pMode.EquipmentState = this.EquipmentState;

            if (dcyclist != null)
                for (int k = 0; k < dcyclist.Count; k++)
                {
                    dcyclist[k].pmode = pMode;
                    dcyclist[k].Cyc_IP01.pMode = pMode;
                    dcyclist[k].Cyc_IP02.pMode = pMode;
                    dcyclist[k].Cyc_OP01.pMode = pMode;
                    dcyclist[k].Cyc_OP02.pMode = pMode;
                }
            //single actuated cyc
            if (scyclist != null)
                for (int k = 0; k < scyclist.Count; k++)
                {
                    if (scyclist[k].Cyc_IP01_Name.Trim().Length > 0)
                    {
                        scyclist[k].Cyc_IP01.pMode = pMode;
                    }
                    if (scyclist[k].Cyc_IP02_Name.Trim().Length > 0)
                    {
                        scyclist[k].Cyc_IP02.pMode = pMode;
                    }
                    if (scyclist[k].Cyc_OP01_Name.Trim().Length > 0)
                    {
                        scyclist[k].Cyc_OP01.pMode = pMode;
                    }
                    if (scyclist[k].Cyc_OP02_Name.Trim().Length > 0)
                    {
                        scyclist[k].Cyc_OP02.pMode = pMode;
                    }
                    scyclist[k].pmode = pMode;
                }
            CycList = new ObservableCollection<Isoloniod>();
            foreach (Isoloniod s in scyclist)
                CycList.Add(s);
            foreach (Isoloniod s in dcyclist)
                CycList.Add(s);
            CmdThread = new Thread(new ThreadStart(CmdThreadFn));
            CmdThread.Start();
            DisplayThread = new Thread(new ThreadStart(DisplayThreadFn));
            DisplayThread.Start();

        }
        protected virtual void DisplayThreadFn()//this thread is used solely for display
        {
            while (pMode.gevt.GetTerminate())//this is to kill the entire application
            {
                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    DisplayCycle();
                }); Thread.Sleep(100);
            }
        }
        private void HardwarePollingFn()
        {
            while (pMode.gevt.GetTerminate())
            {
                RunPolling();
                Thread.Sleep(100);
            }
            
        }

        public override bool RunTimeFunction()
        {
            //IO updates
            //end of IO updates
            //update pmode
            //removed on 27th Feb
            MachineState machineStatus = EquipmentState.GetState();
            bool chk = true;
            if (machineStatus == MachineState.Aborted) chk = false;
            //pMode.ChkException( chk);
            //end of pmode update
            //Thread.Sleep(100);
            switch (machineStatus)
            {
                case MachineState.NotInit:
                    NotInitFn();
                    break;

                case MachineState.Init:
                    InitFn();
                    break;

                case MachineState.InitRun:
                    InitRunFn();
                    break;

                case MachineState.InitRunErr:
                    InitRunErrFn();
                    break;

                case MachineState.InitComplete:
                    InitCompleteFn();
                    break;

                case MachineState.Stopping:
                    StoppingFn();
                    break;

                case MachineState.Stop:
                    StopFn();
                    break;

                case MachineState.Run:
                    RunFn();
                    break;

                case MachineState.RunErr:
                    RunErrFn();
                    break;

                case MachineState.Pause:
                    PauseFn();
                    break;

                case MachineState.Maintenance:
                    MaintenanceFn();
                    break;

                case MachineState.ERRR_Recover:
                    ERR_RecoverFn();
                    break;

                case MachineState.ERR:
                    ERRFn();
                    break;

                case MachineState.EMO:
                    EMOFn();
                    break;

                case MachineState.EMOWaitRelease:
                    EMOWaitReleaseFn();
                    break;

                case MachineState.EMORelease:
                    EMOReleaseFn();
                    break;

                case MachineState.Abort:
                    AbortFn();
                    break;
                case MachineState.Aborted:
                    AbortedFn();
                    break;
            }
            if (threadHWPolling == null || !threadHWPolling.IsAlive)
            {
                threadHWPolling = new Thread(new ThreadStart(HardwarePollingFn));
                threadHWPolling.Start();
            }
            
            return true;
        }

        public MachineState AbortedFn()
        {
            //throw new NotImplementedException();
            return MachineState.Aborted;

        }
        public override MachineState AbortFn()
        {
            try
            {
                if (this.Evtlist != null)
                foreach (ProcessEvt evt in this.Evtlist)
                {
                    evt.evt.Reset();//reset all events
                }
                if(OutputList != null)
                foreach (DiscreteIO op in OutputList)
                {
                    op.Logic = false;
                }
                EquipmentState.SetState(MachineState.Aborted);
            }
            catch (Exception ex)
            {   
                log.Debug(ProcessName + " Abort Error " + ex.ToString()); 
            }
                log.Debug(ProcessName + " event reset!");
            return MachineState.Aborted;
        }
        public override MachineState EMOReleaseFn()
        {
            return MachineState.EMORelease;
        }
        public override MachineState EMOFn()
        {
            return MachineState.EMO;
        }
        public override MachineState ERRFn()
        {
            return MachineState.ERR;
        }
        public override MachineState ERR_RecoverFn()
        {
            return MachineState.ERRR_Recover;
        }
        public override MachineState MaintenanceFn()
        {
            return MachineState.Maintenance;
        }
        public override MachineState RunFn()
        {
            RunFunction();
            //remove this
            //if (pMode.getStopState() == true)
            //    EquipmentState.SetState(MachineState.Stop);
            pMode.ChkProcessMode();
            return MachineState.Run;
        }
        public override MachineState InitCompleteFn()
        {
            pMode.ChkProcessMode();
            return MachineState.InitComplete;
        }
        public override MachineState InitRunFn()
        {
            pMode.ChkProcessMode();
            if (RunInitialization())
                EquipmentState.SetState(MachineState.InitComplete);
            //else
            //   EquipmentState.SetState(MachineState.InitRunErr);
            return MachineState.InitRun;
        }
        public override MachineState InitFn()
        {
            pMode.ChkProcessMode();
            return MachineState.Init;
        }
        public override MachineState NotInitFn()
        {
            pMode.ChkProcessMode();
            if (RunNotInitSequence())
            {
                if (pMode.gevt.evtStart.WaitOne(0))
                {
                    //NotInitResetFn();
                    return MachineState.InitRun;
                }
            }
            return MachineState.NotInit;
        }

        public virtual bool RunFunction()
        {
            return false;
        }
        public virtual bool RunInitialization()
        {
            return false;
        }
        public virtual bool RunNotInitSequence()
        {
            return true;
        }
        public virtual void RunPolling()
        {
            //return false;
            return;
        }

        public bool WaitTime(int ms)
        {
            int unitwaittime = 100;
            int waitcount;
            waitcount = ms / 100;
            if (waitcount == 0)
                unitwaittime = ms;
            while (waitcount > -1)
            {
                //if (this.EquipmentState.GetState() == MachineState.EqResetState) throw new Exception("Seq Reset Detected");
                pMode.ChkProcessMode();
                if (waitcount == 0) unitwaittime = ms % 100;
                Thread.Sleep(unitwaittime);
                waitcount--;
            }
            return true;
        }

        public bool WaitIOEvent(int ms, ManualResetEvent evt, DiscreteIO io)//?? This may not be right...
        {
            int unitwaittime = 100;
            int waitcount;
            waitcount = ms / 100;
            if (waitcount > 0)
                unitwaittime = ms % 100;
            else
                unitwaittime = ms;
            while (!evt.WaitOne(ms))
            {
                if (this.EquipmentState.GetState() == MachineState.EqResetState) throw new Exception("Seq Reset Detected");
                pMode.ChkProcessMode();
                waitcount--;
                if (waitcount < 0)
                {
                    string errstr = io.IOName + $"[{io.DeviceID}]" + "-" + io.DisplayName + " Time Out Error, Lapased " + ms.ToString() + "ms ";
                    log.Debug(errstr);
                    if (FireError(errstr))
                    {
                        waitcount = ms / 100;
                        continue;
                    }
                    return false;//time out
                }
            }

            return true;
        }

        public bool WaitIOEventWithoutError(int ms, ManualResetEvent evt, DiscreteIO io)//?? This may not be right...
        {
            int unitwaittime = 100;
            int waitcount;
            waitcount = ms / 100;
            if (waitcount > 0)
                unitwaittime = ms % 100;
            else
                unitwaittime = ms;
            while (!evt.WaitOne(ms))
            {
                if (this.EquipmentState.GetState() == MachineState.EqResetState) throw new Exception("Seq Reset Detected");
                pMode.ChkProcessMode();
                waitcount--;
                if (waitcount < 0)
                {
                    return false;//time out
                }
            }

            return true;
        }
        private bool FireWarning(string errstr)
        {
            log.Debug(errstr);
            pMode.SetWarning(errstr, true);
            pMode.ChkProcessMode();
            return false;
        }
        public bool WaitEvtOnWarn(int ms, ManualResetEvent evt, out bool bWarningOn, string eventname = null, 
            string gemErrorCode = null,bool bretrybtn=true,bool bignorebtn=true,string retrytxt="Retry",string ignoretxt="Ignore All")
        {
            bWarningOn = false;
            int unitwaittime = 100;
            int waitcount;
            waitcount = ms / 100;
            if (waitcount == 0)
                //    unitwaittime = ms % 100;
                //else
                unitwaittime = ms;
            if (ms == 0) unitwaittime = 100;
            while (!evt.WaitOne(unitwaittime))
            {
                pMode.ChkProcessMode();
                if (ms != 0)//if ms is 0 wait till you get the event
                {
                    waitcount--;
                    if (waitcount < 0)
                    {
                        string errstr = eventname + " Time Out Error, Lapased " + ms.ToString() + "ms ";
                        bWarningOn = true;
                        if (gemErrorCode != null)
                            GemCtrl.SetAlarm(gemErrorCode);
                        pMode.ignoreBtnText = ignoretxt;
                        pMode.retryBtnText = retrytxt;
                        pMode._bIgnoreBtnVisible = bignorebtn;
                        pMode._bretryBtnVisible = bretrybtn;
                        FireWarning(errstr);
                        return false;//time out
                    }
                }
            }
            return true;
        }
      
        public bool WaitEvtOnInfinite(ManualResetEvent evt, string eventname = null)
        {
            while (!evt.WaitOne(10))
            {
                pMode.ChkProcessMode();
            }
            return true;
        }

        public bool WaitAnyOnInfinite(ManualResetEvent evt1, ManualResetEvent evt2, string eventname = null)
        {
            while ((!evt1.WaitOne(100)) && (!evt2.WaitOne(100)))
            {
                pMode.ChkProcessMode();
            }
            return true;
        }

        public bool WaitEvtOn(int ms, ManualResetEvent evt, string eventname = null, string gemErrorCode = null)
        {
            int unitwaittime = 100;
            int waitcount;
            waitcount = ms / 100;
            if (waitcount == 0)
                //    unitwaittime = ms % 100;
                //else
                unitwaittime = ms;
            if (ms == 0) unitwaittime = 100;
            while (!evt.WaitOne(unitwaittime))
            {
                pMode.ChkProcessMode();
                if (ms != 0)//if ms is 0 wait till you get the event
                {
                    waitcount--;
                    if (waitcount < 0)
                    {
                        string errstr = eventname + " Time Out Error, Lapased " + ms.ToString() + "ms ";
                        if (gemErrorCode != null)
                            GemCtrl.SetAlarm(gemErrorCode);
                        FireError(errstr);
                        return false;//time out
                    }
                }
            }

            return true;
        }
        public bool CheckEvent(ManualResetEvent evt)
        {
            return evt.WaitOne(0);
        }
        public bool WaitEvtOnWithoutErrorFire(int ms, ManualResetEvent evt, string eventname = null)
        {
            int unitwaittime = 100;
            int waitcount;
            waitcount = ms / 100;
            if (waitcount > 0)
                unitwaittime = 100;
            else
                unitwaittime = ms;
            if (ms == 0) unitwaittime = 100;
            while (!evt.WaitOne(unitwaittime))
            {
                pMode.ChkProcessMode();
                if (ms != 0)//if ms is 0 wait till you get the event
                {
                    waitcount--;
                    if (waitcount < 0)
                    {
                        //string errstr = eventname + " Time Out Error, Lapased " + ms.ToString() + "ms ";
                        //FireError(errstr);
                        return false;//time out
                    }
                }
            }

            return true;
        }
        public bool WaitAllEvtOn(int ms, ManualResetEvent[] evtlist, string eventname = null)
        {
            int unitwaittime = 100;
            int waitcount;
            waitcount = ms / 100;
            if (waitcount < 1)
                //    unitwaittime = ms % 100;
                //else
                unitwaittime = ms;
            while (ManualResetEvent.WaitAll(evtlist, ms))
            {
                pMode.ChkProcessMode();
                waitcount--;
                if (waitcount < 0)
                {
                    string errstr = eventname + " Time Out Error, Lapased " + ms.ToString() + "ms ";
                    FireError(errstr);
                    return false;//time out
                }
            }

            return true;
        }

        private bool FireError(string errstr)//?? This may not be right
        {
            log.Debug(errstr);
            pMode.SetError(errstr, true);
            pMode.ChkProcessMode();
            return false;
        }

        public string ProcessName { get; set; }

        [XmlIgnore]
        public Dictionary<string, Isoloniod> valvelist;
        [XmlIgnore]
        public IODirectories inputlist;
        [XmlIgnore]
        public IODirectories outputlist;
        [XmlIgnore]
        public ObservableCollection<ProcessEvt> Evtlist { get; set; }
        [XmlIgnore]
        public Devices.SecsGem.EqSecGem GemCtrl { get; set; }

        virtual protected void InitOutput()
        {
            //throw new NotImplementedException();
        }
        virtual protected void InitInput()
        {
        }
        virtual protected void InitEvt()
        {
        }
        virtual public void ShutdownOverride()
        {

        }
        virtual protected void StoppingLogicFn()
        {
            //throw new NotImplementedException();
        }
        virtual protected void RecoverFromStopFn()
        {

        }

        internal void SetOutput(IODirectories oplist, ObservableCollection<string> opnamelist)
        {
            outputlist = oplist;
            OutputNameList = opnamelist;
            OutputList = new ObservableCollection<DiscreteIO>();
            foreach (string str in opnamelist)
                OutputList.Add(oplist.IpDirectory[str]);
            InitOutput();
        }
        internal void SetInput(IODirectories iplist, ObservableCollection<string> ipnamelist)
        {
            inputlist = iplist;
            InputNameList = ipnamelist;
            InputList = new ObservableCollection<DiscreteIO>();

            foreach (string str in ipnamelist)
            {
                InputList.Add(iplist.IpDirectory[str]);
            }
            InitInput();
        }
        internal void SetEvtList(ObservableCollection<ProcessEvt> evtlist, ObservableCollection<string> evtnlist)
        {
            //throw new NotImplementedException();
            Evtlist = evtlist;
            EvtNameList = evtnlist;
            InitEvt();
        }
        internal void SetMotorList(ObservableCollection<PConController> motorlist)
        {
            //throw new NotImplementedException();
            AxisList = motorlist;
        }

        internal void ResetOutput()
        {
            foreach (DiscreteIO op in OutputList)
            {
                op.Logic = false;
            }
        }
        internal void StoppingLogic()
        {
            StoppingLogicFn();
        }
        internal void RecoverFromStop()
        {
            RecoverFromStopFn();
        }
        
    }
}
