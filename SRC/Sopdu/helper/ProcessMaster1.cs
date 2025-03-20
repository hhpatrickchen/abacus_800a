using Insphere.Connectivity.Application.SecsToHost;
using Sopdu.Devices.Cylinders;
using Sopdu.Devices.IOModule;
using Sopdu.Devices.MotionControl.IAIController.PConAxis;
using Sopdu.ProcessApps.main;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sopdu.helper
{

    public class ProcessMaster : GenericStateEngine
    {
        [XmlIgnore]
        public ManualResetEvent evtRstWarning;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Towerlight towerlight;
        private int cnt;
        private bool blink;
        private string[] aComPorts { get { return MyLib.MyLib.aAllPhysicPorts; } }
        [XmlIgnore]
        public bool bretry;
        public ProcessMaster()
        {
            EquipmentState = new GenericState();
            DisplayState = new GenericState();
            DisplayState.SetState(MachineState.NotInit);
            EquipmentState.SetState(MachineState.NotInit);
        }
        internal void Init(GenericEvents mcEvents, IOController ctrl, IOController Opctrl,
                            ObservableCollection<Process> processlist,  ObservableCollection<ProcessEvt> evtlist, MenuObj menu)
        {
            SetWarningDisplay(false);
            evtRstWarning = new ManualResetEvent(false);
            ProcessMasterEvtlist = evtlist;
            //other ip
            
            ////setup system inputs
            DisplayMsg = new ObservableCollection<CMsgClass>();
            DiscreteIO[] ip = new DiscreteIO[9];
            ip[0] = ipStart = ctrl.InputList.IpDirectory["Input01"];
            ip[1] = ipStop = ctrl.InputList.IpDirectory["Input02"];
            ip[2] = ipReset = ctrl.InputList.IpDirectory["Input03"];
            ip[3] = ipPower = ctrl.InputList.IpDirectory["Input04"];
            ip[4] = ipEMO01 = ctrl.InputList.IpDirectory["Input05"];
            ip[5] = ipDS1 = ctrl.InputList.IpDirectory["Input06"];
            ip[6] = ipPressureSW = ctrl.InputList.IpDirectory["Input07"];
            ip[7] = ipSafetyRelay = ctrl.InputList.IpDirectory["Input08"];
            ip[8] = ipEMO02 = ctrl.InputList.IpDirectory["Input09"];
            //opStartLED, opStopLED, opResetLED, opPowerLED, opMainValve, opDS1Lock;
            opStartLED = ctrl.OutputList.IpDirectory["Output01"];
            opStopLED = ctrl.OutputList.IpDirectory["Output02"];
            opResetLED = ctrl.OutputList.IpDirectory["Output03"];
            opPowerLED = ctrl.OutputList.IpDirectory["Output04"];
            opMainValve = ctrl.OutputList.IpDirectory["Output05"];
            opDS1Lock = ctrl.OutputList.IpDirectory["Output06"];
            processevt = new GenericEvents();//generic events
            pMode = new ProcessMode();//processmode
            pMode.Init(mcEvents);//assign generic event
            ProcessList = new ObservableCollection<Process>();//process list stored in processmaster
            DisplayProcessList = new ObservableCollection<Process>();//for display only
            for (int i = 0; i < processlist.Count; i++)//iterate through all the module
            {
                MessageListener.Instance.ReceiveMessage("Init Cylinders for " + processlist[i].ProcessName);
                Thread.Sleep(100);

                // init Cylinder list
                processlist[i].valvelist = new Dictionary<string, Isoloniod>();
                //dual actuated cyc
                for (int k = 0; k < processlist[i].dcyclist.Count; k++)
                {
                    MessageListener.Instance.ReceiveMessage("Init Cylinder - " + processlist[i].dcyclist[k].CycName);
                    Thread.Sleep(10);
                    processlist[i].dcyclist[k].Cyc_IP01 = ctrl.InputList.IpDirectory[processlist[i].dcyclist[k].Cyc_IP01_Name];
                    processlist[i].dcyclist[k].Cyc_IP02 = ctrl.InputList.IpDirectory[processlist[i].dcyclist[k].Cyc_IP02_Name];
                    processlist[i].dcyclist[k].Cyc_OP01 = Opctrl.OutputList.IpDirectory[processlist[i].dcyclist[k].Cyc_OP01_Name];
                    processlist[i].dcyclist[k].Cyc_OP02 = Opctrl.OutputList.IpDirectory[processlist[i].dcyclist[k].Cyc_OP02_Name];
                    processlist[i].valvelist.Add(processlist[i].dcyclist[k].CycName,
                        processlist[i].dcyclist[k]);
                }
                //single actuated cyc
                for (int k = 0; k < processlist[i].scyclist.Count; k++)
                {
                    MessageListener.Instance.ReceiveMessage("Init Cylinder - " + processlist[i].scyclist[k].CycName);
                    Thread.Sleep(10);
                    if (processlist[i].scyclist[k].Cyc_IP01_Name.Trim().Length > 0)
                    {
                        processlist[i].scyclist[k].Cyc_IP01 = ctrl.InputList.IpDirectory[processlist[i].scyclist[k].Cyc_IP01_Name];
                    }
                    if (processlist[i].scyclist[k].Cyc_IP02_Name.Trim().Length > 0)
                    {
                        processlist[i].scyclist[k].Cyc_IP02 = ctrl.InputList.IpDirectory[processlist[i].scyclist[k].Cyc_IP02_Name];
                    }
                    if (processlist[i].scyclist[k].Cyc_OP01_Name.Trim().Length > 0)
                    {
                        processlist[i].scyclist[k].Cyc_OP01 = Opctrl.OutputList.IpDirectory[processlist[i].scyclist[k].Cyc_OP01_Name];
                    }
                    if (processlist[i].scyclist[k].Cyc_OP02_Name.Trim().Length > 0)
                    {
                        processlist[i].scyclist[k].Cyc_OP02 = Opctrl.OutputList.IpDirectory[processlist[i].scyclist[k].Cyc_OP02_Name];
                    }
                    processlist[i].valvelist.Add(processlist[i].scyclist[k].CycName,
                        processlist[i].scyclist[k]);
                }

                //other op
                //init axis list
                MessageListener.Instance.ReceiveMessage("Init Axis for " + processlist[i].ProcessName);
                Thread.Sleep(100);
                foreach (PConController pcon in processlist[i].AxisList)
                {
                    int connectretry = 0;
                    MessageListener.Instance.ReceiveMessage("Init Position Motor " + pcon.DisplayName);
                    Thread.Sleep(100);
                    while (connectretry < 3)
                    {
                        try
                        {
                            //load axis files
                            pcon.MotorAxis = new PconControllerAxis(new PconControllerChannel(pcon.Comport), 0);
                            pcon.MotorAxis.DisplayName = pcon.DisplayName;
                            pcon.MotorAxis.PositionFilePath = @".\Positions\" + pcon.DisplayName + ".zip";
                            pcon.MotorAxis.ReadPositionFile();
                            pcon.MotorAxis.bIsEnable = true;
                            if (pcon.BrakeOP != null)//assign brake
                            {
                                //set output                                
                                pcon.MotorAxis.opBrake = ctrl.OutputList.IpDirectory[pcon.BrakeOP];
                            }
                            // break;//to be removed..
                            /* temp remove connectivity*/
                            if (pcon.Init() < 0) throw new Exception("unable to connect to " + pcon.DisplayName);
                            int cnt = 0;
                            Thread.Sleep(1000);
                            while (!pcon.MotorAxis.bModbusActive)//need a time out sequence here
                            {
                                if (cnt < 30)
                                    Thread.Sleep(200);
                                else
                                    break;
                                // throw new Exception("unable to connect to " + pcon.DisplayName);
                                cnt++;
                            }
                            if (!pcon.MotorAxis.bModbusActive) break;
                            cnt = 0;
                            MessageListener.Instance.ReceiveMessage("Waiting for Motor " + pcon.DisplayName + " status update");
                            Thread.Sleep(500);
                            while (pcon.MotorAxis.RawStatus == null)
                            {
                                if (cnt < 10)
                                {
                                    Thread.Sleep(200);
                                }
                                else
                                    throw new Exception("connection error to " + pcon.DisplayName);
                                cnt++;
                            }
                            MessageListener.Instance.ReceiveMessage(pcon.DisplayName + " Alarm Reset");
                            Thread.Sleep(500);
                            pcon.MotorAxis.AlarmReset(false);
                            MessageListener.Instance.ReceiveMessage(pcon.DisplayName + " Servo Off");
                            Thread.Sleep(500);
                            pcon.MotorAxis.ServoOff(false);
                            // processlist[i].AxisDispList.Add(pcon.MotorAxis);
                            cnt = 0;
                            while (pcon.MotorAxis.RawStatus.SV != false)
                            {
                                if (cnt < 30)
                                    Thread.Sleep(200);
                                else
                                    break;
                                //throw new Exception("unable to connect to " + pcon.DisplayName);
                                cnt++;
                            }
                            if (pcon.MotorAxis.RawStatus.SV != false) break;
                            MessageListener.Instance.ReceiveMessage(pcon.DisplayName + " Servo On");
                            Thread.Sleep(500);
                            pcon.MotorAxis.ServoOn(false);
                            cnt = 0;
                            while (pcon.MotorAxis.RawStatus.SV != true)
                            {
                                if (cnt < 30)
                                    Thread.Sleep(200);
                                else
                                    break;
                                //throw new Exception("unable to connect to " + pcon.DisplayName);
                                cnt++;
                            }
                            break;
                            /* * */
                        }
                        catch (Exception)
                        {
                            if (!pcon.MotorAxis.bModbusActive)
                                pcon.Shutdown();
                            // My add-in to check comport exist? for offline jump to save init time
                            if (!aComPorts.Contains(pcon.Comport))
                            {
                                connectretry = 3;
                                break;
                            }
                            else//*/
                                connectretry++;
                        }
                    }
                    if (connectretry > 2)
                    {
                        MessageListener.Instance.ReceiveMessage("Init Position Motor " + pcon.DisplayName + " Fail");
                        Thread.Sleep(100);
                        // throw new Exception("IAI motor setup error");
                    }
                }

                //end of axis list init
                MessageListener.Instance.ReceiveMessage("Generate Process Module for " + processlist[i].ProcessName);
                Thread.Sleep(100);

                Type t = Type.GetType(@"Sopdu.ProcessApps.ProcessModules." + processlist[i].ProcessName);
                object ProcessInstance = Activator.CreateInstance(t);
                ((Process)ProcessInstance).Init(mcEvents, processlist[i].scyclist, processlist[i].dcyclist,
                    processlist[i].valvelist, processlist[i].ProcessName);

                if (processlist[i].OPNonCritical)
                    ((Process)ProcessInstance).SetOutput(ctrl.OutputList, processlist[i].OutputNameList);
                else
                    ((Process)ProcessInstance).SetOutput(Opctrl.OutputList, processlist[i].OutputNameList);
                //((Process)ProcessInstance).SetOutput(Opctrl.OutputList, processlist[i].OutputNameList);
                ((Process)ProcessInstance).SetInput(ctrl.InputList, processlist[i].InputNameList);
                ((Process)ProcessInstance).SetEvtList(evtlist, processlist[i].EvtNameList);
                ((Process)ProcessInstance).SetMotorList(processlist[i].AxisList);
                ((Process)ProcessInstance).ProcessIdentifier = processlist[i].ProcessIdentifier;
                ((Process)ProcessInstance).pMode.ProcessIdentifier = processlist[i].ProcessIdentifier;
                ((Process)ProcessInstance).GemCtrl = GemCtrl;
                ((Process)ProcessInstance).pMode.GemCtrl = GemCtrl;
                ((Process)ProcessInstance).ignoreGemDisable = processlist[i].ignoreGemDisable;
                ((Process)ProcessInstance).sCoverTrayPrefix = processlist[i].sCoverTrayPrefix;
                ProcessList.Add((Process)ProcessInstance);
                if (processlist[i].HasDisplay == true)
                    DisplayProcessList.Add((Process)ProcessInstance);
                MessageListener.Instance.ReceiveMessage("Process Setup for " + processlist[i].ProcessName + "Completed");
                Thread.Sleep(100);
            }
            MessageListener.Instance.ReceiveMessage("All Process Completed");
            Thread.Sleep(100);

            GlobalVar.lstAllDI.AddRange(ctrl.InputList.IOs);

            GlobalVar.lstAllDO.AddRange(ctrl.OutputList.IOs);
            GlobalVar.lstAllDO.AddRange(Opctrl.OutputList.IOs);

            //assign pmode to discrete ios
            CmdThread = new Thread(new ThreadStart(CmdThreadFn));
            CmdThread.Start();
            // DisplayState = new GenericState();
            DisplayThread = new Thread(new ThreadStart(DisplayThreadFn));
            DisplayThread.Start();

        }

        # region EMO Handling

        public override MachineState EMOReleaseFn()
        {
            //make sure all state are ready
            System.Diagnostics.Trace.Write("EMO RELEASE");
            if (pMode.gevt.evtEMO_Release.WaitOne(0))
            {
                foreach (Process p in ProcessList)
                {
                    p.EquipmentState.SetState(MachineState.NotInit);
                }
                EquipmentState.SetState(MachineState.NotInit);
                pMode.gevt.ResetAllEvt();
                return MachineState.NotInit;
            }
            return MachineState.EMORelease;
        }

        public override MachineState EMOFn()
        {
            System.Diagnostics.Trace.Write("EMO Detected");
            foreach (Process p in ProcessList)
            {
                if (p.EquipmentState.GetState() != MachineState.EMO)
                {
                    return MachineState.EMO;
                }
            }
            if (pMode.gevt.evtEMO_Release.WaitOne(0))
            {
                EquipmentState.SetState(MachineState.EMORelease);
                foreach (Process p in ProcessList)
                {
                    p.EquipmentState.SetState(MachineState.EMORelease);
                }
                return MachineState.EMORelease;
            }

            return MachineState.EMO;
        }

        #endregion EMO Handling

        public void set_towerlight(Towerlight towerlite)
        {
            towerlight = towerlite;//init towerlite
        }

        #region Display Function include push buttons and Tower lights

        protected override void DisplayCycle()
        {
            towerlight.Blink();
            switch (EquipmentState.GetState())
            {
                case MachineState.WarnStop:
                    towerlight.Stop();
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    break;
                case MachineState.Warning:
                    towerlight.Warning();
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    break;
                case MachineState.NotInit:
                    towerlight.NotInit();
                    GemCtrl.SetProcessingState(ProcessingState.Idle);
                    break;

                case MachineState.Init:
                    towerlight.InitRun();
                    GemCtrl.SetProcessingState(ProcessingState.Idle);
                    break;

                case MachineState.InitRun:
                    towerlight.InitRun();
                    GemCtrl.SetProcessingState(ProcessingState.Idle);
                    break;

                case MachineState.InitRunErr:
                    towerlight.Error();
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    break;

                case MachineState.InitComplete:
                    towerlight.Initialized();
                    GemCtrl.SetProcessingState(ProcessingState.Ready);

                    break;

                case MachineState.Stopping:
                    //EquipmentState.SetState(StoppingFn());
                    break;

                case MachineState.Stop:
                    //EquipmentState.SetState(StopFn());
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    break;

                case MachineState.Run:                    
                    GemCtrl.SetProcessingState(ProcessingState.Executing);
                    towerlight.Run();
                    break;
                case MachineState.RunStop:
                    towerlight.Stop();
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    break;
                case MachineState.InitRunStop:
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    towerlight.Stop();
                    break;
                case MachineState.RunErr:
                    //EquipmentState.SetState(RunErrFn());
                    towerlight.Error();
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    break;

                case MachineState.Pause:
                    //EquipmentState.SetState(PauseFn());
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    break;

                case MachineState.Maintenance:
                    //EquipmentState.SetState(MaintenanceFn());
                    GemCtrl.SetProcessingState(ProcessingState.Setup);
                    towerlight.Maintenance();
                    break;

                case MachineState.ERRR_Recover:
                    //EquipmentState.SetState(ERR_RecoverFn());
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    towerlight.Error();
                    break;

                case MachineState.ERR:
                    //EquipmentState.SetState(ERRFn());
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    towerlight.Error();
                    break;

                case MachineState.EMO:
                    //EquipmentState.SetState(EMOFn());
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    towerlight.Error();
                    break;

                case MachineState.EMOWaitRelease:
                    //EquipmentState.SetState(EMOWaitReleaseFn());
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    towerlight.Error();
                    break;

                case MachineState.EMORelease:
                    //EquipmentState.SetState(EMOReleaseFn());
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    towerlight.Error();
                    break;

                case MachineState.Abort:
                    //EquipmentState.SetState(AbortFn());
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    towerlight.Error();
                    break;

                case MachineState.Aborted:
                    //EquipmentState.SetState(AbortedFn());
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    towerlight.Error();
                    break;

                case MachineState.RunInitAbort:
                    //EquipmentState.SetState(RunInitAbort());
                    GemCtrl.SetProcessingState(ProcessingState.Pause);
                    towerlight.Error();
                    break;
            }
        }

        #endregion

        #region run function
        static bool semo01, semo02;
        private string _WarningDisplayVisible;
        [XmlIgnore]
        public string WarningDisplayVisible { get { return _WarningDisplayVisible; } set { _WarningDisplayVisible = value; NotifyPropertyChanged(); } }
        private string _RetryBtnText;
        [XmlIgnore]
        public string RetryBtnText { get { return _RetryBtnText; } set { _RetryBtnText = value; NotifyPropertyChanged(); } }

        private string _IgnoreBtnText;
        [XmlIgnore]
        public string IgnoreBtnText { get { return _IgnoreBtnText; } set { _IgnoreBtnText = value; NotifyPropertyChanged(); } }

        private string _RetryBtnVisible;
        [XmlIgnore]
        public string RetryBtnVisible { get { return _RetryBtnVisible; } set { _RetryBtnVisible = value; NotifyPropertyChanged(); } }

        private string _IgnoreBtnVisible;
        [XmlIgnore]
        public string IgnoreBtnVisible { get { return _IgnoreBtnVisible; } set { _IgnoreBtnVisible = value; NotifyPropertyChanged(); } }

        private string _NormalDisplayVisible;
        [XmlIgnore]
        public string NormalDisplayVisible { get { return _NormalDisplayVisible; } set { _NormalDisplayVisible = value; NotifyPropertyChanged(); } }
        private string _sWarningMsg;
        [XmlIgnore]
        public string sWarningMsg { get { return _sWarningMsg; } set { _sWarningMsg = value; NotifyPropertyChanged(); } }

        public void SetWarningDisplay(bool visible)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                if (visible)
                {
                    evtRstWarning.Reset();
                    WarningDisplayVisible = "Visible";
                    NormalDisplayVisible = "Hidden";
                }
                else
                {
                    RetryBtnVisible = "Hidden";
                    IgnoreBtnVisible = "Hidden";
                    WarningDisplayVisible = "Hidden";
                    NormalDisplayVisible = "Visible";
                    sWarningMsg = "";
                    GemCtrl.ErrorDisplayMsg1 = "";
                }
            });
        }
        private void EMOEvt(bool emo01,bool emo02, GenericEvents mcevent, string message = null)
        {
            if (semo01 != emo01) { semo01 = emo01; if (emo01) pMode.SetInfoMsg("EMO01 Released"); else pMode.SetSystemError("EMO01 detected"); }
            if (semo01 != emo01)
            {
                semo01 = emo01;
                if (emo01)
                {
                    pMode.SetInfoMsg("EMO01 Released");
                    GemCtrl.ClearAlarm("ER_EMG");
                }
                else
                {
                    GemCtrl.SetAlarm("ER_EMG");
                    pMode.SetSystemError("EMO01 detected");
                }
            }
            if (semo02 != emo02)
            {
                semo02 = emo02;
                if (emo02)
                {
                    GemCtrl.ClearAlarm("ER_EMG");
                    pMode.SetInfoMsg("EMO02 Released");
                }

                else
                {
                    GemCtrl.SetAlarm("ER_EMG");
                    pMode.SetSystemError("EMO02 detected");
                }
            }
            if ((!(emo01 && emo02)) && (mcevent.evtEMO_Release.WaitOne(0)))
            {

                mcevent.evtEMO_On.Set();
                mcevent.evtEMO_Release.Reset();
            }
            else if ((emo01 && emo02) && (mcevent.evtEMO_On.WaitOne(0)))
            {

                mcevent.evtEMO_On.Reset();
                mcevent.evtEMO_Release.Set();
            }
            //return false;
        }

        private void PressureSWEvt(bool logic, GenericEvents mcevent, string message = null)
        {
            if ((!logic) && (!mcevent.evtPressureSW_Release.WaitOne(0)))
            {
                mcevent.evtPressureSW_On.Reset();
                mcevent.evtPressureSW_Release.Set();
                opMainValve.SetOutput(false);
                pMode.SetSystemError("Pressure valve release, incoming pressure not detected");
                GemCtrl.SetAlarm("ER_MAIN_AIR");
                //return false;
            }
            if ((logic) && (!mcevent.evtPressureSW_On.WaitOne(0)))
            {
                GemCtrl.ClearAlarm("ER_MAIN_AIR");
                mcevent.evtPressureSW_On.Set();
                mcevent.evtPressureSW_Release.Reset();
                opMainValve.SetOutput(true);
                pMode.SetInfoMsg("Pressure valve turn on, detect incoming pressure");
            }
        }

        private void DSEvt(bool logic, GenericEvents mcevent, string message = null)
        {
            if ((logic) && (mcevent.evtDS_Release.WaitOne(0)))
            {
                mcevent.evtDS_On.Set();
                mcevent.evtDS_Release.Reset();
                pMode.SetInfoMsg("Door Switch Closed");
                GemCtrl.ClearAlarm("ER_DOORSW");
            }
            else if ((!logic) && (mcevent.evtDS_On.WaitOne(0)))
            {
                GemCtrl.SetAlarm("ER_DOORSW");

                mcevent.evtDS_On.Reset();
                mcevent.evtDS_Release.Set();
                log.Debug("DS Release");
                pMode.SetSystemError("Door Switch Opened");
            }
        }

        private void SafetyRelayEvt(bool logic, GenericEvents mcevent, string message = null)
        {
            if ((logic) && (mcevent.evtSafetyRelay_Release.WaitOne(0)))
            {
                mcevent.evtSafetyRelay_On.Set();
                mcevent.evtSafetyRelay_Release.Reset();
                pMode.SetInfoMsg("Safety relay turn on");
            }
            else if ((!logic) && (mcevent.evtSafetyRelay_On.WaitOne(0)))
            {
                mcevent.evtSafetyRelay_On.Reset();
                mcevent.evtSafetyRelay_Release.Set();
                pMode.SetSystemError("Safety reply tripped");
            }
            if (!logic)
            {
                if ((ipEMO01.Logic) && (ipEMO02.Logic))
                {
                    cnt++;
                    if (cnt > 10)
                    {
                        cnt = 0;
                        blink = !blink;
                    }
                    opPowerLED.SetOutput(blink);
                }
                else
                    opPowerLED.SetOutput(false);
            }
            else
                opPowerLED.SetOutput(true);
        }

        private void PowerEvt(bool logic, GenericEvents mcevent, string message = null)
        {
            if ((logic) && (mcevent.evtPower_Off.WaitOne(0)))
            {
                mcevent.evtPower_On.Set();
                mcevent.evtPower_Off.Reset();
                log.Debug("Control Power On");
            }
            else if ((!logic) && (mcevent.evtSafetyRelay_On.WaitOne(0)))
            {
                mcevent.evtPower_On.Reset();
                mcevent.evtPower_Off.Set();
                log.Debug("Control Power Off");
            }
        }

        public override bool RunTimeFunction()
        {
            //System IO updates
            EMOEvt(ipEMO01.Logic, ipEMO02.Logic, pMode.gevt );
            DSEvt(ipDS1.Logic, pMode.gevt);
            PressureSWEvt(ipPressureSW.Logic, pMode.gevt);
            SafetyRelayEvt(ipSafetyRelay.Logic, pMode.gevt);
            MachineState machineState = EquipmentState.GetState();
            if ((machineState != MachineState.Maintenance)&&(machineState != MachineState.EnterMaintainance))
            {
                bool bEvtEMO = pMode.gevt.evtEMO_On.WaitOne(0);
                bool bEvtDSW = pMode.gevt.evtDS_Release.WaitOne(0);
                bool bEvtPSW = pMode.gevt.evtPressureSW_Release.WaitOne(0);
                bool bEvtSRly = pMode.gevt.evtSafetyRelay_Release.WaitOne(0);
                if ((pMode.gevt.evtEMO_On.WaitOne(0) ||  pMode.gevt.evtDS_Release.WaitOne(0) ||
                    (pMode.gevt.evtPressureSW_Release.WaitOne(0)) || pMode.gevt.evtSafetyRelay_Release.WaitOne(0) ) &&
                    ((machineState != MachineState.Abort) && (machineState != MachineState.Aborted)))
                {
                    //change current state to abort
                    EquipmentState.SetState(MachineState.Abort);//abort include emo;safety relay;pressure sw;etc etc...
                    //set all equipment to abort state
                    foreach (Process p in ProcessList)
                        p.pMode.SetAbort();//set all process to abort
                }
            }
            //end of gating state check
            
            switch (machineState)
            {
                case MachineState.NotInit:
                    EquipmentState.SetState(NotInitFn());
                    break;

                case MachineState.Init:
                    EquipmentState.SetState(InitFn());
                    break;

                case MachineState.InitRun:
                    EquipmentState.SetState(InitRunFn());
                    break;

                case MachineState.InitRunErr:
                    EquipmentState.SetState(InitRunErrFn());
                    break;

                case MachineState.InitComplete:
                    EquipmentState.SetState(InitCompleteFn());
                    break;

                case MachineState.Stopping:
                    EquipmentState.SetState(StoppingFn());
                    break;

                case MachineState.Stop:
                    EquipmentState.SetState(StopFn());
                    break;

                case MachineState.Run:
                    EquipmentState.SetState(RunFn());
                    break;

                case MachineState.RunErr:
                    EquipmentState.SetState(RunErrFn());
                    break;

                case MachineState.Pause:
                    EquipmentState.SetState(PauseFn());
                    break;
                case MachineState.EnterMaintainance:
                    EquipmentState.SetState(EnterMaintainanceFn());
                    break;
                case MachineState.Maintenance:
                    EquipmentState.SetState(MaintenanceFn());
                    break;

                case MachineState.ERRR_Recover:
                    EquipmentState.SetState(ERR_RecoverFn());
                    break;

                case MachineState.ERR:
                    EquipmentState.SetState(ERRFn());
                    break;

                case MachineState.EMO:
                    EquipmentState.SetState(EMOFn());
                    break;

                case MachineState.EMOWaitRelease:
                    EquipmentState.SetState(EMOWaitReleaseFn());
                    break;

                case MachineState.EMORelease:
                    EquipmentState.SetState(EMOReleaseFn());
                    break;

                case MachineState.Warning:
                    EquipmentState.SetState(WarningFn());
                    break;
                case MachineState.Abort:
                    EquipmentState.SetState(AbortFn());
                    break;

                case MachineState.Aborted:
                    EquipmentState.SetState(AbortedFn());
                    break;

                case MachineState.RunInitAbort:
                    EquipmentState.SetState(RunInitAbort());
                    break;
                case MachineState.InitRunStop:
                    EquipmentState.SetState(InitRunStopFn());
                    break;
                case MachineState.RunStop:
                    EquipmentState.SetState(RunStopFn());
                    break;
                case MachineState.WarnStop:
                    EquipmentState.SetState(WarnStopFn());
                    break;
            }
            return true;
        }
        private MachineState WarnStopFn()
        {
            bool warning = false;
            foreach (Process p in ProcessList)
            {
                if (p.pMode.getErrorState())//constantly look for error even at stop state
                {
                    EquipmentState.SetState(MachineState.RunErr);
                    return MachineState.RunErr;
                }
            }
            foreach (Process p in ProcessList)
            {
                if (p.pMode.getWarningState())//constantly look for \ing
                {
                    warning = true;
                    break;
                }
            }
            if (!warning)
            {
                SetWarningDisplay(false);//check for which warning to attend to
                return MachineState.RunStop;
            }
            //wait for warning reset
            if (evtRstWarning.WaitOne(0))
            {
                foreach (Process p in ProcessList)
                {
                    if (p.pMode.getWarningState())//constantly look for warning
                    {
                        p.pMode.bretry = bretry;
                        p.pMode.ResetWarning();
                    }
                }
                SetWarningDisplay(false);//check for which warning to attend to
                return MachineState.RunStop;
            }
            //end warning reset
            if (ipStart.Logic == true)
            {
                //release stop
                foreach (Process p in ProcessList)
                {
                    p.pMode.ResetStop();
                    p.RecoverFromStop();
                }
                return MachineState.Warning;
            }
            return MachineState.WarnStop;
        }
        private MachineState RunStopFn()
        {
            foreach (Process p in ProcessList)
            {
                if (p.pMode.getErrorState())//constantly look for error even at stop state
                {
                    EquipmentState.SetState(MachineState.RunErr);
                    return MachineState.RunErr;
                }
            }
            if (ipStart.Logic == true)
            {
                //release stop
                foreach (Process p in ProcessList)
                {
                    p.pMode.ResetStop();
                    p.RecoverFromStop();
                }
                return MachineState.Run;
            }
            return MachineState.RunStop;
        }

        private MachineState InitRunStopFn()
        {
            foreach (Process p in ProcessList)
            {
                if (p.pMode.getErrorState())    //constantly look for error even at stop state
                {
                    EquipmentState.SetState(MachineState.InitRunErr);
                    return MachineState.InitRunErr;
                }
            }
            if (ipStart.Logic == true)
            {
                //release stop
                foreach (Process p in ProcessList)
                {
                    p.pMode.ResetStop();
                    p.RecoverFromStop();
                }
                return MachineState.InitRun;
            }
            return MachineState.InitRunStop;
        }

        private MachineState EnterMaintainanceFn()
        {
            foreach (Process p in ProcessList)
            {
                if (p.EquipmentState.GetState() != MachineState.Aborted)//wait for all equipment to abort
                    return MachineState.EnterMaintainance;
            }
            //recover pmode for process
            foreach (Process p in ProcessList)
            {
                p.pMode.ClearAbort();
                p.pMode.ResetError();
                p.EquipmentState.SetState(MachineState.NotInit);
            }
            log.Debug("All process thread abort");
            Thread.Sleep(100);
            return MachineState.Maintenance;
        }

        public MachineState AbortedFn()
        {
            bool test = pMode.gevt.evtEMO_Release.WaitOne(0);
            test = pMode.gevt.evtDS_On.WaitOne(0);
            test = pMode.gevt.evtPressureSW_On.WaitOne(0);
            test = pMode.gevt.evtSafetyRelay_On.WaitOne(0);
            if (pMode.gevt.evtEMO_Release.WaitOne(0) &&
                pMode.gevt.evtDS_On.WaitOne(0) &&
                pMode.gevt.evtPressureSW_On.WaitOne(0) &&
                pMode.gevt.evtSafetyRelay_On.WaitOne(0))
            {
                Thread.Sleep(100);
                log.Debug("System Input recovered, go to not init mode");
                return MachineState.NotInit;
            }
            return MachineState.Aborted;
        }

        public MachineState RunInitAbort()
        {
            foreach (Process p in ProcessList)
            {
                if (p.EquipmentState.GetState() != MachineState.Aborted)//wait for all equipment to abort
                    return MachineState.RunInitAbort;
            }
            //recover pmode for process
            foreach (Process p in ProcessList)
            {
                p.pMode.ClearAbort();
                p.pMode.ResetError();
                p.EquipmentState.SetState(MachineState.NotInit);
            }
            log.Debug("All process thread abort");
            Thread.Sleep(100);
            return MachineState.NotInit;
        }

        public override MachineState AbortFn()
        {
            foreach (Process p in ProcessList)
            {
                //if (p.EquipmentState.GetState() != MachineState.Abort)//wait for all equipment to abort
                //    return MachineState.Abort;
                if (p.EquipmentState.GetState() != MachineState.Aborted)//wait for all equipment to complete abort
                    return MachineState.Abort;
            }
            //recover pmode for process
            foreach (Process p in ProcessList)
            {
                p.pMode.ClearAbort();
            }
            log.Debug("All process thread abort");
            Thread.Sleep(100);
            return MachineState.Aborted;
        }

        public override MachineState WarningFn()
        {
            bool warning = false;
            foreach (Process p in ProcessList)
            {
                if (p.pMode.getErrorState())//constantly look for error
                {
                    EquipmentState.SetState(MachineState.RunErr);
                    return MachineState.RunErr;
                }
            }
            foreach (Process p in ProcessList)
            {
                if (p.pMode.getWarningState())//constantly look for warning
                {
                    warning = true;
                    break;
                }
            }
            if (!warning)
            {
                SetWarningDisplay(false);//check for which warning to attent to                
                //return MachineState.Run;//return to run state
            }
            //have to add this to warning
            if (evtRstWarning.WaitOne(0))
            {
                foreach (Process p in ProcessList)
                {
                    if (p.pMode.getWarningState())//constantly look for warning
                    {
                        p.pMode.bretry = bretry;
                        p.pMode.ResetWarning();
                    }
                }
                SetWarningDisplay(false);//check for which warning to attent to
                return MachineState.Run;
            }
            if (ipStop.Logic == false)
            {
                foreach (Process p in ProcessList)
                {
                    p.pMode.SetStop();
                    p.StoppingLogic();
                }
                return MachineState.WarnStop;
            }
            return MachineState.Warning;
        }

        public override MachineState RunFn()
        {
            foreach (Process p in ProcessList)
            {
                if (p.pMode.getErrorState())//constantly look for error
                {
                    EquipmentState.SetState(MachineState.RunErr);
                    return MachineState.RunErr;
                }
                if (p.pMode.getWarningState())//constantly look for warning
                {
                    this.sWarningMsg = p.pMode.sWarningMsg;
                    EquipmentState.SetState(MachineState.Warning);
                    SetWarningDisplay(true,p.pMode._bIgnoreBtnVisible,p.pMode._bretryBtnVisible,p.pMode.retryBtnText,p.pMode.ignoreBtnText);//reset warning event
                    return MachineState.Warning;
                }
            }
            if (ipStop.Logic == false)
            {
                foreach (Process p in ProcessList)
                {
                    p.pMode.SetStop();
                    p.StoppingLogic();
                }
                RunTimeData.isCycleStopping = false;
                return MachineState.RunStop;
            }
            if (ipStart.Logic && ipReset.Logic)
                RunTimeData.isCycleStopping = true;
            if (RunTimeData.isCycleStopping && RunTimeData.isPartCleared)
            {
                foreach (Process p in ProcessList)
                {
                    p.pMode.SetStop();
                    p.StoppingLogic();
                }
                RunTimeData.isCycleStopping = false;
                return MachineState.RunStop;
            }
            return MachineState.Run;
        }

        public void SetWarningDisplay(bool visible ,bool bIgnoreBtnVisible,bool _bretryBtnVisible,string retryBtnText,string ignoreBtnText)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                if (visible)
                {
                    evtRstWarning.Reset();
                    WarningDisplayVisible = "Visible";
                    NormalDisplayVisible = "Hidden";
                    if (bIgnoreBtnVisible)
                    {
                        IgnoreBtnVisible = "Visible";
                        IgnoreBtnText = ignoreBtnText;                        
                    }
                    if (_bretryBtnVisible)
                    {
                        RetryBtnVisible = "Visible";
                        RetryBtnText = retryBtnText;
                    }
                }
                else
                {
                    RetryBtnVisible = "Hidden";
                    IgnoreBtnVisible = "Hidden";
                    WarningDisplayVisible = "Hidden";
                    NormalDisplayVisible = "Visible";
                    sWarningMsg = "";
                    GemCtrl.ErrorDisplayMsg1 = "";
                }
            });
        }

        public override MachineState RunErrFn()
        {
            foreach (Process p in ProcessList)
            {
                p.ResetOutput();
            }
            if (ipReset.evtOn.WaitOne(0) == true)// if reset button is pressed
            {
                foreach (Process p in ProcessList)
                {
                    p.pMode.SetAbort();//abort all current sequence
                }
                log.Debug("Run Error Reset, going to Not Init State");
                return MachineState.RunInitAbort;
            }

            foreach (Process p in ProcessList)
            {
                if (p.pMode.getErrorState())//check if error state have been recovered; process should still be in run state
                {
                    EquipmentState.SetState(MachineState.RunErr);
                    return MachineState.RunErr;
                }
            }
            EquipmentState.SetState(MachineState.Run);
            return MachineState.Run;
        }

        public override MachineState ERR_RecoverFn()
        {
            return MachineState.ERRR_Recover;
        }

        #endregion run function

        public override MachineState MaintenanceFn()
        {
            /**
             * A bunch of collision check during maintenance state
             *
             * */
            return MachineState.Maintenance;
        }

        public override MachineState PauseFn()//Sequence Stop
        {
            //change from run state to pause state
            return MachineState.Pause;
        }

        public override MachineState StoppingFn()
        {
            foreach (Process p in ProcessList)
            {
                if (p.pMode.getErrorState())    //constantly look for error
                {
                    EquipmentState.SetState(MachineState.RunErr);
                    return MachineState.RunErr;
                }
                if (p.EquipmentState.GetState() != MachineState.Stop)
                    return MachineState.Stopping;
                if (pMode.getStopState() == false)  //if stop mode is deactivated go back to run mode
                {
                    EquipmentState.SetState(MachineState.Run);
                    return MachineState.Run;
                }
            }
            EquipmentState.SetState(MachineState.Stop);
            return MachineState.Stop;
        }

        public override MachineState StopFn()   //cycle stop
        {
            return MachineState.Stop;
        }

        #region Init Functions

        public override MachineState InitRunFn()
        {
            int cnt = 0;
            if (ipStop.Logic == false)
            {
                foreach (Process p in ProcessList)
                {
                    p.pMode.SetStop();
                    p.StoppingLogic();
                }
                return MachineState.InitRunStop;
            }
            foreach (Process p in ProcessList)
             {
                MachineState mcState = p.EquipmentState.GetState();
                if (p.EquipmentState.GetState() != MachineState.InitComplete)
                {
                    cnt++;
                }
                if (p.pMode.getErrorState())
                {
                    EquipmentState.SetState(MachineState.InitRunErr);
                    log.Debug("Init Error Detected");
                    return MachineState.InitRunErr;
                }
            }
            if (cnt > 0)
            {
                EquipmentState.SetState(MachineState.InitRun);
                return MachineState.InitRun;
            }
            EquipmentState.SetState(MachineState.InitComplete);
            log.Debug("Init Completed");
            return MachineState.InitComplete;
        }

        public override MachineState InitFn()
        {
            return MachineState.Init;
        }

        public void InitAllEvents()
        {
            foreach (ProcessEvt pevt in ProcessMasterEvtlist)
            {
                pevt.evt.Reset();
            }
        }
        public override MachineState NotInitFn()
        {
            //if (pMode.gevt.evtStart.WaitOne(0) == true)
            GemCtrl.ClearAllAlarm();
            if (ipStart.evtOn.WaitOne(0) == true)
            {
                //reset all events
                InitAllEvents();
                //set all to InitRun mode
                foreach (Process p in ProcessList)
                {
                    p.EquipmentState.SetState(MachineState.InitRun);
                }
                //pMode.gevt.evtStart.Reset();//not necessary as we are using hard button only
                log.Debug("Init Run Started");
                SetWarningDisplay(false);//remove display message
                return MachineState.InitRun;
            }
            return MachineState.NotInit;
        }

        public override MachineState InitCompleteFn()
        {
            if (ipStart.evtOn.WaitOne(0) == true)
            {
                //set all to InitRun mode
                foreach (Process p in ProcessList)
                {
                    p.EquipmentState.SetState(MachineState.Run);
                }
                //pMode.gevt.evtStart.Reset();
                return MachineState.Run;
            }
            return MachineState.InitComplete;
        }

        public override MachineState InitRunErrFn()
        {
            foreach (Process p in ProcessList)
            {
                p.ResetOutput();
            }
            if (ipReset.evtOn.WaitOne(0) == true)// if reset button is pressed
            {
                //foreach (Process p in ProcessList)
                //{
                //    p.pMode.ClearAbort();
                //    p.pMode.ResetError();
                //    p.EquipmentState.SetState(MachineState.NotInit);
                //}
                foreach (Process p in ProcessList)
                {
                    p.pMode.SetAbort();//abort all current sequence
                }
                log.Debug("Init Error Reset, going to Not Init State");
                return MachineState.RunInitAbort;
            }

            //machine at initialization run error
            foreach (Process p in ProcessList)//scan through the error state
            {
                if (p.pMode.getErrorState())
                {
                    EquipmentState.SetState(MachineState.InitRunErr);
                    return MachineState.InitRunErr;
                }
            }
            //EquipmentState.SetState(MachineState.InitRun);
            return MachineState.InitRunErr;
        }

        # endregion Init Functions

        private string _ErrorDisplayMsg;
        public string ErrorDisplayMsg { get { return _ErrorDisplayMsg; } set { _ErrorDisplayMsg = value; NotifyPropertyChanged("ErrorDisplayMsg"); } }

        internal bool RequestMaintenanceMode()
        {
            try
            {
                foreach (Process p in ProcessList)
                {
                    p.pMode.SetAbort();//abort all current sequence
                }
                EquipmentState.SetState(MachineState.EnterMaintainance);
                //throw new NotImplementedException();
                return true;
            }
            catch (Exception ex)
            {
            }
            return false;
        }

        [XmlIgnore]
        public Devices.SecsGem.EqSecGem GemCtrl { get; set; }
    }
}