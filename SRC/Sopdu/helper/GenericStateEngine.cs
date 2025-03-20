using Sopdu.Devices;
using Sopdu.Devices.Cylinders;
using Sopdu.Devices.IOModule;
using Sopdu.Devices.MotionControl.Base;
using Sopdu.Devices.MotionControl.IAIController.PConAxis;
using Sopdu.ProcessApps.main;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using Dct.Models.Repository;
using Dct.Models;

namespace Sopdu.helper
{
    public enum EvtState
    {
        bevtNone,
        bevtStart,
        bevtStop,
        bevtPause,
        bevtReset,
        bevtResetSeq,
        bevtEMO_On,
        bevtEMO_Release,
        Abort
    }

    public class GenericEvents  //generic events such as start/stop/pause/abort/EMOs
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
       

        private bool _Terminate;

        public bool GetTerminate()
        {
            return !_Terminate;
        }

        public void SetTerminate(bool state)
        {
            _Terminate = state;
        }

        [XmlIgnore]
        public ManualResetEvent evtStart { get { return _evtStart; } set { _evtStart = value;/*update events*/ } }

        private ManualResetEvent _evtStart;

        [XmlIgnore]
        public ManualResetEvent evtStop { get { return _evtStop; } set { _evtStop = value;/*update events*/ } }

        private ManualResetEvent _evtStop;

        [XmlIgnore]
        public ManualResetEvent evtPause { get { return _evtPause; } set { _evtPause = value;/*update events*/ } }

        private ManualResetEvent _evtPause;

        [XmlIgnore]
        public ManualResetEvent evtReset { get { return _evtReset; } set { _evtReset = value;/*update events*/ } }

        private ManualResetEvent _evtReset;

        [XmlIgnore]
        public ManualResetEvent evtEMO_On { get { return _evtEMO_On; } set { _evtEMO_On = value;/*update events*/ } }

        private ManualResetEvent _evtEMO_On;

        [XmlIgnore]
        public ManualResetEvent evtEMO_Release { get { return _evtEMO_Release; } set { _evtEMO_Release = value;/*update events*/ } }

        private ManualResetEvent _evtEMO_Release;

        [XmlIgnore]
        public ManualResetEvent evtAbort { get { return _evtAbort_Release; } set { _evtAbort_Release = value;/*update events*/ } }

        private ManualResetEvent _evtAbort_Release;

        private ManualResetEvent[] evtList;

        [XmlIgnore]
        public ManualResetEvent evtDS_On { get { return _evtDS_On; } set { _evtDS_On = value;/*update events*/ } }

        private ManualResetEvent _evtDS_On;

        [XmlIgnore]
        public ManualResetEvent evtDS_Release { get { return _evtDS_Release; } set { _evtDS_Release = value;/*update events*/ } }

        private ManualResetEvent _evtDS_Release;

        [XmlIgnore]
        public ManualResetEvent evtSafetyRelay_On { get { return _evtSafetyRelay_On; } set { _evtSafetyRelay_On = value;/*update events*/ } }

        private ManualResetEvent _evtSafetyRelay_On;

        [XmlIgnore]
        public ManualResetEvent evtSafetyRelay_Release { get { return _evtSafetyRelay_Release; } set { _evtSafetyRelay_Release = value;/*update events*/ } }

        private ManualResetEvent _evtSafetyRelay_Release;

        [XmlIgnore]
        public ManualResetEvent evtPressureSW_On { get { return _evtPressureSW_On; } set { _evtPressureSW_On = value;/*update events*/ } }

        private ManualResetEvent _evtPressureSW_On;

        [XmlIgnore]
        public ManualResetEvent evtPressureSW_Release { get { return _evtPressureSW_Release; } set { _evtPressureSW_Release = value;/*update events*/ } }

        private ManualResetEvent _evtPressureSW_Release;

        [XmlIgnore]
        public ManualResetEvent evtPower_On { get { return _evtPower_On; } set { _evtPower_On = value;/*update events*/ } }

        private ManualResetEvent _evtPower_On;

        [XmlIgnore]
        public ManualResetEvent evtPower_Off { get { return _evtPower_Off; } set { _evtPower_Off = value;/*update events*/ } }

        private ManualResetEvent _evtPower_Off;

        public GenericEvents()
        {
            evtStart = new ManualResetEvent(false);
            evtStop = new ManualResetEvent(false);
            evtPause = new ManualResetEvent(false);
            evtReset = new ManualResetEvent(false);
            evtAbort = new ManualResetEvent(false);

            evtEMO_On = new ManualResetEvent(true);
            evtEMO_Release = new ManualResetEvent(false);

            evtDS_On = new ManualResetEvent(false);
            evtDS_Release = new ManualResetEvent(true);

            evtPressureSW_On = new ManualResetEvent(false);
            evtPressureSW_Release = new ManualResetEvent(true);

            evtPower_On = new ManualResetEvent(false);
            evtPower_Off = new ManualResetEvent(true);

            evtSafetyRelay_On = new ManualResetEvent(false);
            evtSafetyRelay_Release = new ManualResetEvent(true);

            evtList = new ManualResetEvent[6];
            evtList[0] = evtStart;
            evtList[1] = evtStop;
            evtList[2] = evtPause;
            evtList[3] = evtReset;
            evtList[4] = evtEMO_On;
            evtList[5] = evtEMO_Release;
        }

        public EvtState WaitEquipmentEvtn()
        {
            int i = ManualResetEvent.WaitAny(evtList, 100);
            //  if (_Terminate) throw new Exception("End of Application Acception");
            switch (i)
            {
                case 1:
                    return EvtState.bevtStart;

                case 2:
                    return EvtState.bevtStop;

                case 3:
                    return EvtState.bevtPause;

                case 4:
                    return EvtState.bevtReset;

                case 5:
                    return EvtState.bevtEMO_On;

                case 6:
                    return EvtState.bevtEMO_Release;
            }
            if (evtAbort.WaitOne(0) == true) return EvtState.Abort;
            return EvtState.bevtNone;
        }

        public EvtState WaitEquipmentEvt()
        {
            int i = ManualResetEvent.WaitAny(evtList, 100);
            if (_Terminate) throw new Exception("End of Application Acception");
            switch (i)
            {
                case 0:
                    return EvtState.bevtStart;

                case 1:
                    return EvtState.bevtStop;

                case 2:
                    return EvtState.bevtPause;

                case 3:
                    return EvtState.bevtReset;

                case 4:
                    return EvtState.bevtEMO_On;

                case 5:
                    return EvtState.bevtEMO_Release;
            }
            if (evtAbort.WaitOne(0) == true) return EvtState.Abort;
            return EvtState.bevtNone;
        }

        internal void ResetAllEvt()
        {
            evtStart.Reset();
            evtReset.Reset();
            evtPause.Reset();
            evtStop.Reset();
        }
    }

    public class GenericStateEngine : NotifyPropertyChangedObject
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected Thread CmdThread;
        public GenericState _EquipmentState;
        public GenericState EquipmentState { get { return _EquipmentState; } set { _EquipmentState = value; NotifyPropertyChanged("EquipmentState"); } }

        private GenericState _DisplayState;
        public GenericState DisplayState { get { return _DisplayState; } set { _DisplayState = value; NotifyPropertyChanged(); } }

        public ProcessMode pMode;

        private ObservableCollection<Process> _ProcessList;

        public ObservableCollection<Process> ProcessList { get { return _ProcessList; } set { _ProcessList = value; NotifyPropertyChanged(); } }

        private ObservableCollection<Process> _DisplayProcessList;
        public ObservableCollection<Process> DisplayProcessList { get { return _DisplayProcessList; } set { _DisplayProcessList = value; NotifyPropertyChanged(); } }

        public ObservableCollection<CMsgClass> _DisplayMsg;
        public ObservableCollection<CMsgClass> DisplayMsg {get{return _DisplayMsg;} set{_DisplayMsg=value;NotifyPropertyChanged("DisplayMsg");}}
        // private GenericEvents _EqEvt;
        private EvtState StateEvtChk;

        protected Thread threadHWPolling;
        protected Thread DisplayThread;
        protected DiscreteIO ipStart;
        protected DiscreteIO ipStop;
        protected DiscreteIO ipReset;
        protected DiscreteIO ipPower;
        protected DiscreteIO ipEMO01;
        protected DiscreteIO ipDS1;
        protected DiscreteIO ipPressureSW;
        protected DiscreteIO ipSafetyRelay;
        //
        protected DiscreteIO opStartLED, opStopLED, opResetLED, opPowerLED, opMainValve, opDS1Lock,opRed;
        protected GenericEvents processevt;
        protected bool isTerminate { get { return pMode.gevt.GetTerminate(); } }

        //    [XmlIgnore]
        // public GenericEvents EqEvt { get { return _EqEvt; } set { _EqEvt = value; } }
        public void Shutdown()
        {
            if (ProcessList != null)
                foreach (Process p in ProcessList)
                {
                    foreach (PConController pc in p.AxisList)
                    {
                        pc.Shutdown();
                    }
                    p.ShutdownOverride();
                }
            if (CmdThread != null)
                CmdThread.Join(1000);
            if (DisplayThread != null) DisplayThread.Join(1000);
        }

        public void ResetError()
        {
            foreach (Process p in ProcessList)
            {
                p.pMode.ResetError();
            }
        }

        public void Init()
        {
            pMode = new ProcessMode();
            pMode.Init();
            CmdThread = new Thread(new ThreadStart(CmdThreadFn));
            CmdThread.Start();
        }
        [XmlIgnore]
        public ObservableCollection<ProcessEvt> ProcessMasterEvtlist;

        protected virtual void DisplayThreadFn()//this thread is used solely for display
        {
            while (pMode.gevt.GetTerminate())//this is to kill the entire application
            {
                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                                     {
                                         MachineState machineState = EquipmentState.GetState();
                                         if (machineState == MachineState.Run)
                                         {
                                             if (RunTimeData.isCycleStopping)
                                                 DisplayState.CurrentState = MachineState.Stopping;
                                             else
                                                 DisplayState.CurrentState = machineState;
                                         }
                                         else
                                             DisplayState.CurrentState = machineState;
                                         //DisplayState.CurrentState = EquipmentState.GetState();
                                         DisplayCycle();
                                         MsgDisplay();
                                     }); Thread.Sleep(100);
            }
        }

        protected virtual void MsgDisplay()
        {
            //run msg
            if (DisplayMsg != null)
            {
                if(pMode.fifoMsg != null)//display system level message
                {
                    CMsgClass k;
                    if(pMode.fifoMsg.TryTake(out k))
                    {
                        //add to observable class
                        if (DisplayMsg.Count == 20)
                        {
                            DisplayMsg.RemoveAt(19);
                            DisplayMsg.Insert(0, k);
                        }
                        else
                            DisplayMsg.Insert(0, k);
                        //end of observable class
                    }
                }
                foreach (Process p in ProcessList)
                {
                    CMsgClass m;
                    if (p.pMode.fifoMsg != null)
                    {
                        if (p.pMode.fifoMsg.TryTake(out m))
                        {
                            //add to observable class
                            if (DisplayMsg.Count == 20)
                            {
                                DisplayMsg.RemoveAt(19);
                                DisplayMsg.Insert(0, m);
                            }
                            else
                                DisplayMsg.Insert(0, m);
                            //end of observable class
                        }
                    }
                }
            }
        }

        protected virtual void DisplayCycle()
        {
        }

        protected void CmdThreadFn()
        {
            while (pMode.gevt.GetTerminate())//this is to kill the entire application
            {
                try
                {
                    RunTimeFunction();
                }
                catch (Exception ex)
                {
                    //if there is exception case set to abort
                    if (ex.Message == "Sequence Abort detected")
                        EquipmentState.SetState(MachineState.Aborted);
                    else
                        EquipmentState.SetState(MachineState.Abort);
                    //if (pMode!=null)
                    //pMode.SetError("Sequence Error, abort detected", true);//if there is an exception thrown program will jam
                    //EquipmentState.SetState(MachineState.Abort);
                    log.Error(this.pMode.ProcessIdentifier+ "-" + ex.ToString());
                }
                Thread.Sleep(100);
            }
        }

        public virtual bool RunTimeFunction()
        {
            //IO updates

            //end of IO updates

            //update pmode

            //end of pmode update
            MachineState machineState = EquipmentState.GetState();
            switch (machineState)
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
            }
            return true;
        }
         public virtual MachineState WarningFn()
        {
            throw new NotImplementedException();
         }
        public virtual MachineState RunErrFn()
        {
            throw new NotImplementedException();
        }

        public virtual MachineState StoppingFn()
        {
            throw new NotImplementedException();
        }

        public virtual MachineState AbortFn()
        {
            // throw new NotImplementedException();
            return MachineState.Abort;
        }

        public MachineState EMOWaitReleaseFn()
        {
            throw new NotImplementedException();
        }

        public virtual MachineState EMOReleaseFn()
        {
            return MachineState.EMORelease;
        }

        public virtual MachineState EMOFn()
        {
            StateEvtChk = pMode.gevt.WaitEquipmentEvt();//provide current eq event
            if (StateEvtChk == EvtState.bevtEMO_Release) return MachineState.EMORelease;
            return MachineState.EMO;
        }
        //public virtual MachineState WarningFn()
        //{
        //    return MachineState.Warning;
        //}
        public virtual MachineState InitRunErrFn()
        {
            return MachineState.InitRunErr;
        }

        public virtual MachineState ERRFn()
        {
            return MachineState.ERR;
        }

        public virtual MachineState ERR_RecoverFn()
        {
            return MachineState.ERRR_Recover;
        }

        public virtual MachineState MaintenanceFn()
        {
            return MachineState.Maintenance;
        }

        public virtual MachineState PauseFn()
        {
            return MachineState.Pause;
        }

        public virtual MachineState RunFn()
        {
            return MachineState.Run;
        }

        public virtual MachineState StopFn()
        {
            return MachineState.Stop;
        }

        public virtual MachineState InitCompleteFn()
        {
            return MachineState.InitComplete;
        }

        public virtual MachineState InitRunFn()
        {
            return MachineState.InitRun;
        }

        public virtual MachineState InitFn()
        {
            return MachineState.Init;
        }

        public virtual MachineState NotInitFn()
        {
            return MachineState.NotInit;
        }

        public DiscreteIO ipEMO02 { get; set; }
    }

    public class LightSet
    {
        public bool red { get; set; }
        public bool redblink { get; set; }
        public bool amber { get; set; }
        public bool amberblink { get; set; }
        public bool green { get; set; }
        public bool greenblink { get; set; }
        public bool blue { get; set; }
        public bool blueblink { get; set; }
        public bool buzzer { get; set; }
        public bool buzzerblink { get; set; }
    }

    public class Towerlight
    {
        public LightSet NotInitLite { get; set; }
        public LightSet InitRunLite { get; set; }
        public LightSet InitializedLite { get; set; }

        public LightSet ErrorLite { get; set; }
        public LightSet RunLite { get; set; }
        public LightSet MaintenanceLite { get; set; }
        public LightSet StopLite { get; set; }
        public LightSet WarningLite { get; set; }

        [XmlIgnore]
        public DiscreteIO redop;

        [XmlIgnore]
        public DiscreteIO amberop;

        [XmlIgnore]
        public DiscreteIO greenop;

        [XmlIgnore]
        public DiscreteIO blueop;

        [XmlIgnore]
        public DiscreteIO buzzerop;

        [XmlIgnore]
        public bool blink;

        [XmlIgnore]
        public int cnt;

        [XmlIgnore]
        public int BlinkCnt;

        [XmlIgnore]
        public DiscreteIO opStartLed;

        [XmlIgnore]
        public DiscreteIO opResetLed;

        [XmlIgnore]
        public DiscreteIO opStopLed;

        [XmlIgnore]
        public DiscreteIO ipResetBtn;

        public Towerlight()
        {
            cnt = 0;
            NotInitLite = new LightSet();
            InitRunLite = new LightSet();
            InitializedLite = new LightSet();
            ErrorLite = new LightSet();
            RunLite = new LightSet();
            MaintenanceLite = new LightSet();
            WarningLite = new LightSet();
        }

        public void Blink()
        {
            cnt++;
            if (cnt > BlinkCnt)
            {
                cnt = 0;
                blink = !blink;
            }
        }

        public void Warning()
        {
            redop.SetOutput(WarningLite.red && (!(WarningLite.redblink && blink)));
            amberop.SetOutput(WarningLite.amber && (!(WarningLite.amberblink && blink)));
            greenop.SetOutput(WarningLite.green && (!(WarningLite.greenblink && blink)));
            blueop.SetOutput(WarningLite.blue && (!(WarningLite.blueblink && blink)));
            buzzerop.SetOutput(WarningLite.buzzer && (!(WarningLite.buzzerblink && blink)));
            opStartLed.SetOutput(blink);
            opResetLed.SetOutput(false);
        }

        public void NotInit()
        {
            redop.SetOutput(NotInitLite.red && (!(NotInitLite.redblink && blink)));
            amberop.SetOutput(NotInitLite.amber && (!(NotInitLite.amberblink && blink)));
            greenop.SetOutput(NotInitLite.green && (!(NotInitLite.greenblink && blink)));
            blueop.SetOutput(NotInitLite.blue && (!(NotInitLite.blueblink && blink)));
            buzzerop.SetOutput(NotInitLite.buzzer && (!(NotInitLite.buzzerblink && blink)));
            opStartLed.SetOutput(blink);
            opResetLed.SetOutput(false);
        }

        internal void InitRun()
        {
            redop.SetOutput(InitRunLite.red && (!(InitRunLite.redblink && blink)));
            amberop.SetOutput(InitRunLite.amber && (!(InitRunLite.amberblink && blink)));
            greenop.SetOutput(InitRunLite.green && (!(InitRunLite.greenblink && blink)));
            blueop.SetOutput(InitRunLite.blue && (!(InitRunLite.blueblink && blink)));
            buzzerop.SetOutput(InitRunLite.buzzer && (!(InitRunLite.buzzerblink && blink)));
            opResetLed.SetOutput(false);
            opStartLed.SetOutput(false);
            opStopLed.SetOutput(blink);
        }

        internal void Initialized()
        {
            redop.SetOutput(InitializedLite.red && (!(InitializedLite.redblink && blink)));
            amberop.SetOutput(InitializedLite.amber && (!(InitializedLite.amberblink && blink)));
            greenop.SetOutput(InitializedLite.green && (!(InitializedLite.greenblink && blink)));
            blueop.SetOutput(InitializedLite.blue && (!(InitializedLite.blueblink && blink)));
            buzzerop.SetOutput(InitializedLite.buzzer && (!(InitializedLite.buzzerblink && blink)));
            opStartLed.SetOutput(blink);
            opResetLed.SetOutput(false);
        }

        internal void Error()
        {
            redop.SetOutput(ErrorLite.red && (!(ErrorLite.redblink && blink)));
            amberop.SetOutput(ErrorLite.amber && (!(ErrorLite.amberblink && blink)));
            greenop.SetOutput(ErrorLite.green && (!(ErrorLite.greenblink && blink)));
            blueop.SetOutput(ErrorLite.blue && (!(ErrorLite.blueblink && blink)));
            buzzerop.SetOutput(ErrorLite.buzzer && (!(ErrorLite.buzzerblink && blink)));
            opResetLed.SetOutput(blink);
            opStartLed.SetOutput(false);
        }

        internal void Run(bool bypass)
        {
            if(!bypass)
            {
                amberop.SetOutput(RunLite.amber && (!(RunLite.amberblink && blink)));
                buzzerop.SetOutput(RunLite.buzzer && (!(RunLite.buzzerblink && blink)));
            }
            redop.SetOutput(RunLite.red && (!(RunLite.redblink && blink)));
            
            greenop.SetOutput(RunLite.green && (!(RunLite.greenblink && blink)));
            blueop.SetOutput(RunLite.blue && (!(RunLite.blueblink && blink)));
            
            
            opResetLed.SetOutput(false);
            opStartLed.SetOutput(false);
            opStopLed.SetOutput(blink);
        }

        internal void Stop()
        {
            redop.SetOutput(StopLite.red && (!(StopLite.redblink && blink)));
            amberop.SetOutput(StopLite.amber && (!(StopLite.amberblink && blink)));
            greenop.SetOutput(StopLite.green && (!(StopLite.greenblink && blink)));
            blueop.SetOutput(StopLite.blue && (!(StopLite.blueblink && blink)));
            buzzerop.SetOutput(StopLite.buzzer && (!(StopLite.buzzerblink && blink)));
            opResetLed.SetOutput(false);
            opStopLed.SetOutput(blink);
            opStartLed.SetOutput(blink);
        }

        internal void Maintenance()
        {
            redop.SetOutput(MaintenanceLite.red && (!(MaintenanceLite.redblink && blink)));
            amberop.SetOutput(MaintenanceLite.amber && (!(MaintenanceLite.amberblink && blink)));
            greenop.SetOutput(MaintenanceLite.green && (!(MaintenanceLite.greenblink && blink)));
            blueop.SetOutput(MaintenanceLite.blue && (!(MaintenanceLite.blueblink && blink)));
            buzzerop.SetOutput(MaintenanceLite.buzzer && (!(MaintenanceLite.buzzerblink && blink)));
            opStopLed.SetOutput(false);
            opResetLed.SetOutput(false);
            opStartLed.SetOutput(false);
        }
    }

    public enum PMode
    {
        Run,
        Stop,
        Pause,
        Reset
    }

    public class ProcessMode //every process will have one of this. to facilite program flow of stop,pause,emo,abort,error messages
    {
        private static readonly log4net.ILog Errorlog = log4net.LogManager.GetLogger("ErrorLog");
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ManualResetEvent _AtStopState;
        private ManualResetEvent _AtPauseState;
        private ManualResetEvent _AtErrorState;
        private ManualResetEvent _AtWarningState;
        public GenericEvents gevt;
        public EvtState evstate;//current state
        public string sErrMsg;
        public string sWarningMsg;
        private bool bErr = false;
        private bool bWarn = false;
        private Thread IOStatusThread;
        private bool terminate = false;
        private DiscreteIO ipEMO01, ipEMO02, ipDS1, ipSafetyRelay, ipPressureSW, ipStart, ipStop, ipReset, ipPower;
        private DiscreteIO opStartLED, opStopLED, opResetLED, opPowerLED, opMainValve, opDS1Lock, optowerRed, optowerAmber, optowerGreen, optowerBlue, optowerbuzzer;
        [XmlIgnore]
        public ConcurrentBag<CMsgClass> fifoMsg;
        private MachineState LastEQState;

        AlarmHistoryRepository alarmHistoryRepository;
        public ProcessMode ()
        {
            alarmHistoryRepository = DbManager.Instance.GetRepository<AlarmHistoryRepository>();
        }

        public void SetIP(DiscreteIO[] ip)
        {
            ////setup inputs
            ipStart = ip[0];
            ipStop = ip[1];
            ipReset = ip[2];
            ipPower = ip[3];
            ipEMO01 = ip[4];
            ipDS1 = ip[5];
            ipPressureSW = ip[6];
            ipSafetyRelay = ip[7];
            ipEMO02 = ip[8];
        }

        internal void Init()
        {
            _AtStopState = new ManualResetEvent(false);

            _AtPauseState = new ManualResetEvent(false);

            _AtErrorState = new ManualResetEvent(false);
            _AtWarningState = new ManualResetEvent(false);
            fifoMsg = new ConcurrentBag<CMsgClass>();
            gevt = new GenericEvents();
            evstate = EvtState.bevtNone;
        }

        internal void Init(GenericEvents evt)
        {
            fifoMsg = new ConcurrentBag<CMsgClass>();
            _AtStopState = new ManualResetEvent(false);
            _AtPauseState = new ManualResetEvent(false);
            _AtErrorState = new ManualResetEvent(false);
            _AtWarningState = new ManualResetEvent(false);
            gevt = evt;// reference external gevt mainly for abort sequence
        }

        //public void ChkException()
        //{
        //    if (evstate == EvtState.Abort)// all other state not really important
        //        throw new Exception("Sequence Abort detected");
        //    if (terminate)
        //        throw new Exception("Sequence terminated");
        //}
        public void ChkException(bool check = true)
        {
            if (check)
                if (evstate == EvtState.Abort)// all other state not really important
                    throw new Exception("Sequence Abort detected");
            if (!gevt.GetTerminate())
                throw new Exception("Sequence terminated");
        }

        //    if (pMode.getStopState() == true)
        //EquipmentState.SetState(MachineState.Stop);
        public void ChkProcessMode()//blocking sequence if theres an error
        {
            int cNotAtStop = 0;
            int cNotAtError = 0;
            int cNotAtWarning = 0;
            ChkException();// if state is abort exit

            while (_AtPauseState.WaitOne(0))
            {
                Thread.Sleep(100);
                ChkException();//check for abort and EMO
                if (!gevt.GetTerminate()) throw new Exception("Terminate detected");
            }
            while (_AtStopState.WaitOne(0))
            {
                {//make the msg fire only once
                    if (cNotAtStop == 0)
                    {
                        //save last equipment state
                        LastEQState = EquipmentState.GetState();
                        EquipmentState.SetState(MachineState.Stop);
                        log.Debug("Stop Detected");
                    }
                    Thread.Sleep(100);
                    cNotAtStop++;
                    if (cNotAtStop > 100) cNotAtStop = 1;
                }
                ChkException();
                if (!gevt.GetTerminate())
                {
                    throw new Exception("Terminate detected");
                }

                while (_AtErrorState.WaitOne(0))//hold the sequence if theres an error
                {
                    {//make the msg fire only once
                        if (cNotAtError == 0)
                        {
                            if (bErr)
                            Errorlog.Debug("Error Detected : " + sErrMsg);
                            log.Debug("Error Detected : " + sErrMsg);
                            CMsgClass msg = new CMsgClass() { time = DateTime.Now.ToString(), Level = "ERROR", Msg = sErrMsg };
                            fifoMsg.Add(msg);
                        }//put error code read here
                        cNotAtError++;
                        if (cNotAtError > 100) cNotAtError = 1;
                        Thread.Sleep(100);
                    }
                    ChkException();//check for abort and EMO
                    if (!gevt.GetTerminate()) throw new Exception("Terminate detected");
                }
            }
            if (EquipmentState.GetState() == MachineState.Stop)
            {
                //recover last state
                EquipmentState.SetState(LastEQState);
            }
            while(_AtWarningState.WaitOne(0))
            {
                {
                    if(cNotAtWarning == 0)
                    {
                        log.Debug("Warning Detected : " + sWarningMsg);
                        CMsgClass msg = new CMsgClass() { time = DateTime.Now.ToString(), Level = "WARN", Msg = sWarningMsg };
                        fifoMsg.Add(msg);
                    }
                    cNotAtWarning++;
                    if (cNotAtWarning > 100) cNotAtWarning = 1;
                }
                ChkException();//check for abort and EMO
                if (!gevt.GetTerminate()) throw new Exception("Terminate detected");
            }
            while (_AtErrorState.WaitOne(0))//hold the sequence if theres an error
            {
                {//make the msg fire only once
                    if (cNotAtError == 0)
                    {
                        if(bErr)
                         Errorlog.Debug("Error Detected : " + sErrMsg);
                        log.Debug("Error Detected : " + sErrMsg);
                        CMsgClass msg = new CMsgClass() { time = DateTime.Now.ToString(), Level = "ERROR", Msg = sErrMsg };
                        fifoMsg.Add(msg);
                    }//put error code read here
                    cNotAtError++;
                    if (cNotAtError > 100) cNotAtError = 1;
                }
                ChkException();//check for abort and EMO
                if (!gevt.GetTerminate()) throw new Exception("Terminate detected");
            }
        }

        public void SetInfoMsg(string smsg)
        {
            log.Debug("Info : " + smsg);
            CMsgClass msg = new CMsgClass() { time = DateTime.Now.ToString(), Level = "INFO", Msg = smsg };
            fifoMsg.Add(msg);
        }
        public void SetSystemError(string smsg)
        {
            log.Debug("Error : " + smsg);
            CMsgClass msg = new CMsgClass() { time = DateTime.Now.ToString(), Level = "ERROR", Msg = smsg };
            fifoMsg.Add(msg);
        }
        public void SetWarningMsg(string smsg)
        {
            this.sWarningMsg = smsg;
            log.Debug("Warning : " + smsg);
            CMsgClass msg = new CMsgClass() { time = DateTime.Now.ToString(), Level = "WARNING", Msg = smsg };
            fifoMsg.Add(msg);
        }

        public  bool getWarningState()
        {
            return _AtWarningState.WaitOne(0);
        }
        public bool getStopState()
        {
            return _AtStopState.WaitOne(0);
        }
        public bool getPauseState()
        {
            return _AtPauseState.WaitOne(0);
        }
        public bool getErrorState()
        {
            return _AtErrorState.WaitOne(0);
        }
        public bool GetErrorStatus()
        {
            return bErr;
        }

        public void SetWarning(string WarnMsg = "", bool Warn= false,string WarnCode = null)
        {
            bretry = true;
            sWarningMsg = WarnMsg;
            bWarn = Warn;
            _AtWarningState.Set();
        }
        public void ResetWarning()
        {
            sWarningMsg = "";
            _AtWarningState.Reset();
        }
        public void SetError(string ErrMsg = "", bool Err = true, string ErrorCode = null, bool SaveAlarm = true)
        {
            if (Err && !string.IsNullOrEmpty(ErrMsg) && true)
            {
                alarmHistoryRepository.ClearAllAlarm(out _);
                alarmHistoryRepository.SetAlarm(new Dct.Models.Entity.AlarmHistoryEntity() { Code = ErrorCode, Description = ErrMsg, Type = "Alarm", StationID = "1" }, out _);

            }
            sErrMsg = ErrMsg;
            bErr = Err;
            _AtErrorState.Set();
        }
        public void ResetError()
        {
            alarmHistoryRepository.ClearAllAlarm(out _);
            sErrMsg = "";
            _AtErrorState.Reset();
        }

        public void SetStop()
        {
            _AtStopState.Set();
        }
        public void ResetStop()
        {
            _AtStopState.Reset();
        }
        public void SetPause()
        {
            _AtPauseState.Set();
        }
        public void ResetPause()
        {
            _AtPauseState.Reset();
        }

        public void SetAbort()
        {
            evstate = EvtState.Abort;
            gevt.evtAbort.Set();
        }
        public void ClearAbort()
        {
            evstate = EvtState.bevtNone;
            gevt.evtAbort.Reset();
            _AtErrorState.Reset();
            _AtStopState.Reset();
            _AtPauseState.Reset();
            _AtWarningState.Reset();
        }

        public string ProcessIdentifier { get; set; }

        public GenericState EquipmentState { get; set; }

        [XmlIgnore]
        public Devices.SecsGem.EqSecGem GemCtrl { get; set; }
        [XmlIgnore]
        public bool bretry { get; set; }

        [XmlIgnore]
        public bool _bretryBtnVisible;
        [XmlIgnore]
        public bool _bIgnoreBtnVisible;
        [XmlIgnore]
        public string retryBtnText;
        [XmlIgnore]
        public string ignoreBtnText;

        //public bool getWarningState()
        //{
        //    return _AtWarningState.WaitOne(0);
        //}


    }

}