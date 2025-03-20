using Insphere.Connectivity.Application.SecsToHost;
using LogPanel;
using Sopdu.Devices.Cylinders;
using Sopdu.Devices.IOModule;
using Sopdu.Devices.MotionControl.Base;
using Sopdu.Devices.SecsGem;
using Sopdu.Devices.Vision;
using Sopdu.helper;
using Sopdu.ProcessApps.main;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
//using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using static System.Windows.Forms.AxHost;

namespace Sopdu.ProcessApps.ProcessModules
{
    public class InputCV : Process
    {
        LogTool<OutputStacker> logTool = new LogTool<OutputStacker>();
        public static List<string> TrayIDList;
        public static List<string> sendlist;
        public static List<int> CalibratedList;
        public static List<int> IndexList;
        public Dictionary<string, AxisPosition> posdictionary;
        private string carrierid;
        public string sCurrentRecipe = null;
        private static string _currentRecipe;
        private int iChkStatus = 0;
        bool isOK1DCamera = false;
        [XmlIgnore]
        public main.MainApp mainapp;

        public static string CurrentRecipe
        {
            get { return _currentRecipe; }
            set { _currentRecipe = value; }
        }
        public UsbCamera usbCamera { get { return _usbCamera; } set { _usbCamera = value; NotifyPropertyChanged("usbCamera"); } }
        private UsbCamera _usbCamera;

        public InputCV()
        {
            EquipmentState = new GenericState();
            EquipmentState.SetState(MachineState.NotInit);
            initstate = InitState.NotInit;
            runstate = RunState.Start;
            //RunTimeData.mainApp.IPCvr = this;
            twoFSW = new MyLib.TwoFingerSW();
            isOK1DCamera = false;
        }

        #region // Variable definition and property
        private bool bHasExcuted = false;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        public static System.Diagnostics.Stopwatch sw1 = new System.Diagnostics.Stopwatch();
        public static System.Diagnostics.Stopwatch sw2 = new System.Diagnostics.Stopwatch();
        public static Action LogoutUserAction;
        private Timer timer = new Timer(CancelAppointment,null,0,2000);
        public static UIModel uIModel=new UIModel();
        private Thread thread1;
        private MyLib.TwoFingerSW twoFSW;
        public bool isDIOK = false, isDOOK = false;
        private int initblink = 0, iBlinkCntGrn = 0, iBlinkCntRed = 0, iCnt = 0;
        private bool bM3Bwdbackup, bM3Fwdbackup, isPartMM, isReverseTray, isHSCvrDone, is2FSWOn;
        //public bool isOHTReady { get { return isOHTAlive; } }
        private bool isAutoMode { get { return GlobalVar.isIPCVAuto; } }
        private bool isOHTDetect { get { return CheckOHTDetect(); } }
        public bool isPartPresent { get { return CheckPartPresent(); } }
        public bool isPartAbsent { get { return CheckPartAbsent(); } }
        private bool isPresentSensor { get { return CheckPresentSensor(); } }
        private bool isClearSensor { get { return CheckPartClear(); } }
        private bool isHeightSensor { get { return CheckHeightSensor(); } }
        private bool isFSWOn { get { return is2FSWOn; } }       // Two finger switch or two touch button or foot switch
        private bool isCurtainTrig { get { return !CheckCurtainSensor(); } }
        private bool isOwnDoor { get { return GlobalVar.isDoorAtCV; } }
        private bool isReverse { get { return GlobalVar.isReverse; } }
        private int iCntLimt { get { return GlobalVar.iCntLimt; } }
        private int iTimeOut { get { return GlobalVar.iTimeOut; } }
        private string sModeOHT { get { return EIPMode.OHT.ToString(); } }
        private string sModeCvr { get { return EIPMode.Conveyor.ToString(); } }
        private string sModeMnl { get { return EIPMode.Manual.ToString(); } }
        private EIPMode eIPMode { get { return GlobalVar.eIPMode; } }
        private int initHandshakeSignalCyle = 0;


        //Plasma fan
        private bool isPlasmaFanAlarm { get { return CheckPlasmaFanSensor(); } }

        #endregion

        #region // Run polling       
        public override void RunPolling()
        {
            if (isDIOK && isDOOK)
            {
                is2FSWOn = twoFSW.CheckTwoFingerSWOn(dITouchButton1.Logic, dITouchButton2.Logic);
                if (isAutoMode)
                {
                    dOPB1Led.Logic = dITouchButton1.Logic;
                    dOPB2Led.Logic = dITouchButton2.Logic;
                }

            }
            if (initHandshakeSignalCyle ==0)
            {
                if (pMode.getStopState())
                {
                   
                    TrigCtrAlive(false);
                    TrigCtrReady(false);
                    TrigOHTLReq(false);
                    TrigOHTRdy(false);
                    initHandshakeSignalCyle++;
                }
                else 
                {
                    initHandshakeSignalCyle=0;
                }
            }
            if (!pMode.getStopState())
            {
                initHandshakeSignalCyle = 0;
            }

            if(GlobalVar.EnablePlasmaFan)
            {
                if (isPlasmaFanAlarm)
                {
                    //stop or alarm
                    TrigPlasmaFan(false);
                    log.Debug("Plasma Fan alarm");
                    pMode.SetInfoMsg("Plasma Fan alarm");                    
                    
                }
            }
            
        }
        #endregion

        #region // Initial Run Sequence

        public override MachineState NotInitFn()
        {
            //assume cycle fast enough to do at least a single pass or
            //we can explictly call before change state by creating another function
            initstate = InitState.NotInit;
            runstate = RunState.Start;
            isOK1DCamera = false;

            return base.NotInitFn();
        }

        public override bool RunInitialization()
        {
            posdictionary = InputStacker.posdictionary1;
            //return true;
            switch (initstate)
            {
                case InitState.NotInit:
                    initstate = NotInit0Fn();
                    break;
                case InitState.CheckCVClear:
                    initstate = CheckCVClearFn();
                    break;
                case InitState.WaitCVClear://if cv not clear, operator have to remove part and press btn to ack clear
                    initstate = WaitCVClearFn();
                    break;
                case InitState.Ready:
                    initstate = ReadyFn();
                    break;
                case InitState.InitComplete:
                    initstate = InitComplete0Fn();
                    return true;
                    //break;
            }
            return false;
        }
        private InitState NotInit0Fn()
        {
            if (!isPresentSensor && isClearSensor)
                isPartMM = false;
            return InitState.CheckCVClear;
        }
        private InitState CheckCVClearFn()
        {
            iCnt = 0;
            sw.Stop();
            sw.Reset();
            if (isPartPresent || !isClearSensor) //tray present
            {
                //initstate = InitState.WaitCVClear;
                //pMode.SetInfoMsg("Input Conveyor Part Not Clear, remove part and press Tray Load Ack Button");
                initstate = InitState.NotInit;
                pMode.SetError("Input Conveyor Part Not Clear!", false);
            }
            else
                initstate = InitState.Ready;
            return initstate;
        }
        private InitState WaitCVClearFn()
        {
            TrigLedBlinkGrn(5);
            if (isPartPresent && !isDoorOpened)
                TrigDoorOpen();
            if (isPartAbsent)
            {
                TrigLedGrn(false);
                TrigLedRed(false);
                initstate = InitState.Ready;
            }

            return initstate;
        }
        private InitState ReadyFn()
        {
            //make sure cv is always clear even at ready state
            if (isPartPresent) //tray present
                initstate = InitState.CheckCVClear;
            if (isPartAbsent)
            {
                if (isCurtainTrig)
                {
                    if (!isDoorClosed)
                    {
                        TrigDoorStop();
                        initstate = InitState.NotInit;
                        pMode.SetError("Curtain Sensor blocked while door closing!", false);
                        logTool.DebugLog("Curtain Sensor blocked while door closing!");
                    }
                }
                else
                {
                    if (doDoorClose.Logic)
                    {
                        if (sw.IsRunning)
                        {
                            if (sw.ElapsedMilliseconds > iTimeoutClos)
                            {
                                sw.Stop();
                                TrigDoorStop();
                                initstate = InitState.NotInit;
                                pMode.SetError("Input Shutter Door Close Timeout!", false, doDoorClose.DisplayName);
                                logTool.ErrorLog("Input Shutter Door Close Timeout!");
                            }
                        }
                        else
                            sw.Start();
                    }
                    else
                        TrigDoorClose();
                }

                if (isDoorClosed)
                {
                    InitCylinder();

                    //two case here... ip stacker request for tray rev or ip stacker home complete
                    if (evtInit_InputStackerHomeComplete.WaitOne(0))
                    {
                        initstate = InitState.InitComplete;
                        // evtInit_InputStackerHomeComplete.Reset();
                    }
                }
            }

            return initstate;
        }
        private InitState InitComplete0Fn()
        {
            InitHandshake();
            //set state for inputcv runtime
            log.Debug("Input CV Init Complete");
            pMode.SetInfoMsg("Input CV Init Complete");
            logTool.InfoLog("Input CV Init Complete");
            runstate = RunState.Start;
            GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.InService, "LoadPortTransferStateOutOfServiceInService1");
            return initstate;
        }

        private InitState initstate;
        private enum InitState { NotInit, CheckCVClear, Ready, WaitCVClear, InitComplete, RevSequenceStart, WaitTrayAtCV }
        #endregion

        #region // Auto Running Sequence
        public override bool RunFunction()
        {
            switch (runstate)
            {
                case RunState.Start:
                    //check part present
                    runstate = StartFn();
                    break;
                case RunState.Judgement:
                    runstate = JudgementFn();
                    break;
                case RunState.HSCvr:
                    runstate = HSCvrFn();
                    break;
                case RunState.RunCVIn:
                    runstate = RunCVInFn();
                    break;
                case RunState.PitchDetection:
                    runstate = PitchDetectionFn();
                    break;
                case RunState.TrayMapInspection:
                    runstate = TrayMapInspectionFn();
                    break;
                case RunState.CameraResultError:
                    runstate = CameraResultErrorFn();
                    break;
                case RunState.MoveTurretRemotePosition:
                    runstate = TurretRemoteFn();
                    break;
                case RunState.MoveTurretHomePosition:
                    runstate = TurretHomeFn();
                    break;
                case RunState.HSOHT:
                    runstate = HSOHTFn();
                    break;
                case RunState.RunOHTIn:
                    runstate = RunOHTInFn();
                    break;
                case RunState.WaitDoorOpen:
                    runstate = WaitDoorOpenFn();
                    break;
                case RunState.WaitDoorClose:
                    log.Debug("InputCV StartFn WaitDoorCloseFn");
                    runstate = WaitDoorCloseFn();
                    break;
                case RunState.WaitManulLoad:
                    runstate = WaitManualLoadFn();
                    break;
                case RunState.WaitForIPBtn:
                    runstate = WaitForIPBtnFn();
                    break;
                case RunState.RequestIPStackerCVMove:
                    runstate = RequestIPStackerCVMoveFn();
                    break;
                case RunState.RunCVOut:
                    runstate = RunCVOutFn();
                    break;
                case RunState.WaitStackerFreeIPCV:
                    runstate = WaitStackerFreeIPCVFn();
                    break;
                case RunState.WaitForBufferAllow:
                    runstate = WaitForBufferAllowFn();
                    break;
                case RunState.WaitForIPStackerRdyToLoad:
                    runstate = WaitForIPStackerRdyToLoadFn();
                    break;
                case RunState.CVRevSequence:
                    runstate = CVRevSequenceFn();
                    break;
                case RunState.FailTrayClear:
                    runstate = FailTrayClearFn();
                    break;
                case RunState.EndofCycle:
                    runstate = EndofCycleFn();
                    break;
            }
            try { }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.ToString()); }
            return true;
        }

        private RunState StartFn()
        {
            iCnt = 0;
            sw.Stop();
            sw.Reset();
            pMode.ChkProcessMode();
            TrigLedGrn(true);
            TrigLedRed(false);

            if (GlobalVar.EnablePlasmaFan)
            {
                TrigPlasmaFan(true);
            }
                

            if (isReverse)
            {
                if (!evtIPStackerAllowBuffer.WaitOne(0))
                {
                    return RunState.WaitForBufferAllow;
                }
            }

            //get current transfer state//
            LoadPortTxferState currentstate = (LoadPortTxferState)int.Parse(GemCtrl.GetCurrentSvValue("LoadPortTransferState1"));
            switch (currentstate)
            {
                case LoadPortTxferState.TransferBlocked:
                    GemCtrl.BufferQtyChanged(1, 0);
                    GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.ReadyToLoad, "LoadPortTransferStateTransferBlockedReadyToLoad1");
                    break;

                case LoadPortTxferState.TransferReady:
                    GemCtrl.BufferQtyChanged(1, 0);
                    GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.ReadyToLoad, "LoadPortTransferStateTranferReadyReadyToLoad1");
                    //LoadPortTransferStateTranferReadyReadyToLoad1
                    //LoadPortTransferStateTransferReadyReadyToLoad1
                    //LoadPortTransferStateTransferReadyReadyToLoad1
                    break;

                case LoadPortTxferState.ReadyToUnload:

                    GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.TransferBlocked, "LoadPortTransferStateReadyToUnloadTransferBlocked1");
                    GemCtrl.SetLoadPortAssociateState01(LoadPortAssocState.NotAssociated, "loadPortAssociationStateAssociatedNotAssociated1");
                    GemCtrl.BufferQtyChanged(1, 0);
                    GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.ReadyToLoad, "LoadPortTransferStateTransferBlockedReadyToLoad1");
                    break;

                case LoadPortTxferState.InService://LoadPortTransferStateInServiceTransferReady1
                    GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.TransferReady, "LoadPortTransferStateInServiceTransferReady1");//LoadPortTransferStateInServiceTransferReady1
                    GemCtrl.BufferQtyChanged(1, 0);
                    GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.ReadyToLoad, "LoadPortTransferStateTranferReadyReadyToLoad1");
                    break;
            }

            //TrigLedGrn(true);   // light on if allow buffer
            if (isPartPresent)  //tray present
            {
                pMode.SetInfoMsg("IP CV detect part");
                logTool.InfoLog("IP CV detect part");
                // GemCtrl.BufferQtyChanged(0, 1);
                bHasExcuted = false;
                return RunState.WaitForIPStackerRdyToLoad;
            }
            if (isPartAbsent)
                return RunState.Judgement;
            return runstate;
        }
        private RunState WaitForBufferAllowFn()
        {
            if (evtIPStackerAllowBuffer.WaitOne(100))
                return RunState.Judgement;
            else
                return runstate;
        }
        private RunState JudgementFn()
        {
            iCnt = 0;
            sw.Stop();
            sw.Reset();
            if (GlobalVar.lstIPMode == null || GlobalVar.lstIPMode.Count <= 0)
            {
                pMode.SetError("Input Conveyer Loading Mode was not set!", true);
                logTool.DebugLog("Input Conveyer Loading Mode was not set!");
            }
            else
            {
                if (RunTimeData.isCycleStopping == false)
                {
                    if (GlobalVar.lstIPMode.Contains(sModeOHT))
                    {
                        if (isOHTAlive && isOHTSelect && !TriggerAppointment)
                        {
                            recoverCondition = true;
                            log.Debug($"During Judgement step isOHTAlive is {isOHTAlive},isOHTSelect is {isOHTSelect},triggerAppointment is {TriggerAppointment}");
                           logTool.DebugLog($"During Judgement step isOHTAlive is {isOHTAlive},isOHTSelect is {isOHTSelect},triggerAppointment is {TriggerAppointment}");
                            return RunState.HSOHT;
                        }
                    }
                    if (GlobalVar.lstIPMode.Contains(sModeCvr))
                    {
                        if (isSFAAlive && isSFAReady && !TriggerAppointment)
                        {
                            log.Debug($"During Judgement step isSFAAlive is {isSFAAlive},isSFAReady is {isSFAReady},triggerAppointment is {TriggerAppointment}");
                            logTool.DebugLog($"During Judgement step isSFAAlive is {isSFAAlive},isSFAReady is {isSFAReady},triggerAppointment is {TriggerAppointment}");
                            return RunState.HSCvr;
                        }
                    }
                    if (GlobalVar.lstIPMode.Contains(sModeMnl))
                    {
                        if (isFSWOn)
                            return RunState.WaitDoorOpen;
                    }
                }

            }
            return runstate;
        }
        private RunState HSCvrFn()
        {

            isHSCvrDone = false;
            if (isPartAbsent) //tray present
            {
                if (!isLifterAtDn)
                    TrigLifterDown();
                if (!isExtendBWD)
                    TrigExtendBWD();
                if (!isTurretAtHm)
                {
                    TrigLedGrn(false);
                    TrigLedRed(true);
                    TrigTurretHome();
                }
                    
                if (isTurretAtHm)
                {
                    if (isSFAAlive && isSFAReady)
                    {
                        log.Debug($"in HSCvrFn step,isSFAAlive is {isSFAAlive},isSFAReady is {isSFAReady}");
                        logTool.DebugLog($"in HSCvrFn step,isSFAAlive is {isSFAAlive},isSFAReady is {isSFAReady}");
                        if (!isCtrAlive)
                            TrigCtrAlive(true);
                        if (!isCtrReady)
                            TrigCtrReady(true);
                        if (isCVStopped)
                            CVMove(true, false);
                        else
                        {
                            pMode.SetError("CVstopSensorError", true);
                            logTool.DebugLog("CVstopSensorError");
                        }
                        //else
                        return RunState.RunCVIn;
                    }
                }

            }
            return runstate;
        }
        private RunState RunCVInFn()
        {
            if (!isPresentSensor)
            {
                if (!sw.IsRunning)
                {
                    sw.Reset();
                    sw.Start();
                }
                else
                {
                    if (sw.ElapsedMilliseconds >= iTimeOut)
                    {
                        pMode.SetError("SFA Tray in Time out", true);
                        logTool.ErrorLog("SFA Tray in Time out");
                        sw.Stop();
                    }
                }
            }

            if (!isClearSensor)
                isPartMM = true;
            if (isPartMM && isClearSensor)
                CVMove(true, true);
            if (isPartPresent) //tray present
            {

                if (iCnt < 10)
                    iCnt++;
                else
                {
                    if (!isCVStopped)
                    {
                        CVStop();
                        TrigExtendBWD();
                    }
                    if (!isCtrDone && !isHSCvrDone)
                    {
                        log.Debug($"in RunCVInFn step ,isHSCvrDone is{isHSCvrDone}");
                        logTool.DebugLog($"in RunCVInFn step ,isHSCvrDone is{isHSCvrDone}");
                        TrigCtrDone(true);
                        isHSCvrDone = true;
                    }

                    if (isSFADone)
                    {
                        TrigCtrDone(false);
                        TrigCtrReady(false);
                        log.Debug($"isSFADone is {isSFADone}");
                        logTool.DebugLog($"isSFADone is {isSFADone}");
                    }
                    if (!isSFAReady && !isCtrReady && isCVStopped && !isCtrReady && isHSCvrDone)
                    {
                        log.Debug($"isSFAReady is {isSFAReady}");
                        logTool.DebugLog($"isSFAReady is {isSFAReady}");
                        TrigTurretRemote();
                        bHasExcuted = false;
                        return RunState.PitchDetection;
                    }
                }
            }

            return runstate;
        }
        private RunState PitchDetectionFn()
        {
            Thread.Sleep(300);
            //if (!evtIPStackerRdyToLoad.WaitOne(1))
            //{
            //    return RunState.PitchDetection;
            //}

            try
            {
                usbCamera.su_SetupSelectedEvent(usbCamera.Setups[1]);
                return RunState.TrayMapInspection;
            }
            catch (Exception ex)
            {
                pMode.SetWarningMsg("Exception on Vision on Tray Pitch Detection");
                logTool.InfoLog("Exception on Vision on Tray Pitch Detection");
                return RunState.CameraResultError;
            }
        }
        
        private RunState TrayMapInspectionFn()  //seq 05
        {

            try
            {
                if(!isOK1DCamera)
                {
                    //usbCamera.sCoverTrayPrefix = "TPK";
                    usbCamera.su_SetupSelectedEvent(usbCamera.Setups[0]);
                    int numoftry = 5;
                    if ((usbCamera.__position.Count > 30) || (usbCamera.__trayidlist.Count > 30))
                    {
                        throw new Exception("Tray number over than 30 error");
                    }
                    while (usbCamera.__position.Count != usbCamera.__trayidlist.Count)
                    {
                        usbCamera.su_SetupSelectedEvent(usbCamera.Setups[0]);
                        numoftry--;
                        if (numoftry < 0)
                        {

                            isOK1DCamera = false;
                            throw new Exception("Label detect error");

                            //log.Debug($"Label detect error:doDoorOpen isOwnDoor={isOwnDoor},Logic={doDoorOpen.Logic}");

                            //TrigTurretRemote(); //go back to original

                            //log.Debug($"TrigDoorOpen start");
                            //TrigDoorOpen();
                            //valvelist[sCylDoor].WaitRetract();
                            //log.Debug($"TrigDoorOpen completed");

                            ////throw new Exception("Label detect error");
                            //pMode.SetError("Label detect error", true, "ER_IPST_E22");
                            //GemCtrl.SetAlarm("ER_IPST_E22");


                            //pMode.ChkProcessMode();



                        }
                    }
                    isOK1DCamera = true;
                }
                
                
                //double MaxPitch = 0;
                //double lasttraypos = 0;
                //if (usbCamera._IndexValue == 565)
                //{
                //    pMode.SetInfoMsg("Pitch Detected is 565");
                //    logTool.InfoLog("Pitch Detected is 565");
                //    MaxPitch = usbCamera.T556_Set;
                //    lasttraypos = usbCamera.LastTrayPos_T556;
                //}
                //if (usbCamera._IndexValue == 635)
                //{
                //    pMode.SetInfoMsg("Pitch Detected is 635");
                //    logTool.InfoLog("Pitch Detected is 635");
                //    MaxPitch = usbCamera.T635_Set;
                //    lasttraypos = usbCamera.LastTrayPos_T635;

                //}
                //if (usbCamera._IndexValue == 1005)
                //{
                //    pMode.SetInfoMsg("Pitch Detected is 1005");
                //    logTool.InfoLog("Pitch Detected is 1005");
                //    MaxPitch = usbCamera.T1016_Set;
                //    lasttraypos = usbCamera.LastTrayPos_T1016;
                //}

                ////check for inverted
                //if (Double.Parse(usbCamera.__maxPitch) > MaxPitch)
                //{
                //    pMode.SetWarningMsg("Tray Wrong Orientation Max Pitch : " + usbCamera.__maxPitch);
                //    logTool.InfoLog("Tray Wrong Orientation Max Pitch : " + usbCamera.__maxPitch);
                //    //check if its all tpk trays
                //    bool iscarriertray = true;
                //    foreach (string str in usbCamera.__trayidlist)
                //    {
                //        if (!str.Contains("TPK")) iscarriertray = false;
                //    }
                //    if (!iscarriertray)
                //    {
                //        this.GemCtrl.SetAlarm("ER_IPST_E19");
                //        throw new Exception("Pitch Error");
                //    }

                //}
                //string lastpos = usbCamera.__position.Last();
                //if (Double.Parse(lastpos) < lasttraypos)
                //{
                //    logTool.WarnLog("Last Tray Wrong Orientation Pos value " + lastpos);
                //    throw new Exception("Last Tray Wrong Orientation Pos value " + lastpos);
                //}

                if (!evtIPStackerRdyToLoad.WaitOne(1))
                {
                    return RunState.TrayMapInspection;
                }

                isOK1DCamera = false;

                //set # of Trays
                //do CEID 81 here
                TrayIDList = new List<string>();
                foreach (string str in usbCamera.__trayidlist)
                {
                    TrayIDList.Add(str);
                }
                sendlist = new List<string>();
                //kk
                int intCoverTray = 0;
                foreach (string s in TrayIDList)
                {
                    sendlist.Add(s);
                    if (s.Contains("TPK"))
                    {
                        intCoverTray++;
                        log.Debug("CoverTaryNo =" + intCoverTray);
                        logTool.DebugLog("CoverTaryNo =" + intCoverTray);
                    }
                }
                if (intCoverTray >= 2)
                {
                    log.Debug("CoverTrayMorethanOne=" + intCoverTray);
                    logTool.DebugLog("CoverTrayMorethanOne=" + intCoverTray);
                }
                if (sendlist.Count < 3)
                {
                    throw new Exception("Less Than 3 Trays ");
                }
                sendlist.RemoveAt(0);
                carrierid = TrayIDList[0];
                GlobalVar.carrierids.Add(carrierid);
                string state = GemCtrl.GetCurrentSvValue("EquipmentState");
                GemCtrl.cmdS3F17evt.Reset();
                GemCtrl.SetCarrierStatus(CarrierIDStatus.IdNotRead, TrayIDList[0], sendlist, "CarrierIdStatusIdNotRead1");
                //CEID 62

                GemCtrl.SetCarrierStatus(CarrierIDStatus.WaitingForHost, TrayIDList[0], sendlist, "CarrierIdStatusWaitingForHost1");
                //wait for host to provide proceed with setup
                if ((state == "1") && (GemCtrl.gemController.CommunicationState != CommunicationState.Disabled))
                {
                    log.Debug("Equipment state In production mode");
                    logTool.DebugLog("Equipment state In production mode");
                    // WaitEvtOn(60000, GemCtrl.cmdS3F17evt, "Wait For Host To Proceed With Carrier I", "ER_IPST_E13");
                    if (!WaitEvtOnWithoutErrorFire(60000, GemCtrl.cmdS3F17evt, "ER_IPST_E13"))
                        throw new Exception("Host Deny Carrier Process-1");
                    log.Debug("Reply For S3F17 " + GemCtrl.strS3F17SendString);
                    log.Debug("Reply For S3F17 " + GemCtrl.strS3F17SendString);
                    if (GemCtrl.strS3F17SendString.Trim() != "ProceedWithCarrier")
                    {
                        log.Debug("Cannot Proceed With Carrier");
                        logTool.DebugLog("Cannot Proceed With Carrier");
                        //CarrierSlotMapStatusWaitingForHostSlotMapVerificationFailed1
                        //CarrierSlotMapStatusWaitingForHostSlotMapVerificationOK1
                        GemCtrl.SetCarrierStatus(CarrierIDStatus.IdVerificationFailed, TrayIDList[0], sendlist, "CarrierIdStatusIdVerificationFail1");
                        throw new Exception("Host Deny Carrier Process :" + GemCtrl.strS3F17SendString.Trim());
                    }
                }
                else
                    log.Debug("Equipment state Not In production mode, not waiting for host verificaiton on CEID62");
                GemCtrl.cmdS3F17evt.Reset();
                logTool.DebugLog("Equipment state Not In production mode, not waiting for host verificaiton on CEID62");
                //get current status
                //CEID 67
                GemCtrl.SetLoadPortAssociateState01(LoadPortAssocState.Associated, "LoadPortAssociationStateNotAssociatedAssociated1");
                //CEID 81
                GemCtrl.SetCarrierStatus(CarrierIDStatus.IdVerificationOK, TrayIDList[0], sendlist, "CarrierIdStatusIdVerificationOK1");
                //CEID 82
                GemCtrl.SetSlotMapStatus(CarrierSlotMapStatus.SlotMapNotRead, "CarrierSlotMapStatusSlotMapNotRead1", TrayIDList[0], sendlist);
                //CEID 83
                GemCtrl.SetSlotMapStatus(CarrierSlotMapStatus.WaitingForHost, "CarrierSlotMapStatusSlotMapNotReadWaitingForHost1", TrayIDList[0], sendlist);
                //CEID 84
                GemCtrl.SetSlotMapStatus(CarrierSlotMapStatus.SlotMapVerificationOK, "CarrierSlotMapStatusWaitingForHostSlotMapVerificationOK1", TrayIDList[0], sendlist);
                //wait for host to provide proceed with setup
                if ((state == "1") && (GemCtrl.gemController.CommunicationState != CommunicationState.Disabled))
                {
                    log.Debug("Equipment state In production mode");
                    logTool.DebugLog("Equipment state In production mode");
                    // WaitEvtOn(60000, GemCtrl.cmdS3F17evt, "Wait For Host To Proceed With Carrier","ER_IPST_E14");
                    if (!WaitEvtOnWithoutErrorFire(60000, GemCtrl.cmdS3F17evt, "ER_IPST_E13"))
                        throw new Exception("Host Deny Carrier Process-2");
                    log.Debug("Reply For S3F17 " + GemCtrl.strS3F17SendString);
                    logTool.DebugLog("Reply For S3F17 " + GemCtrl.strS3F17SendString);
                    if (GemCtrl.strS3F17SendString.Trim() != "ProceedWithCarrier")
                    {
                        log.Debug("Cannot Proceed With Carrier");
                        logTool.DebugLog("Cannot Proceed With Carrier");
                        throw new Exception("Host Deny Carrier Process :" + GemCtrl.strS3F17SendString.Trim());
                    }
                }
                else
                    log.Debug("Equipment state Not In production mode, not waitingfor host verification on CEID83");
                //CEID606
                GemCtrl.CarrierReadStart(TrayIDList[0], 1, "CarrierReadToStart");
                logTool.DebugLog("Equipment state Not In production mode, not waitingfor host verification on CEID83");
                //wait for pp-select
                string currentrecipe = "On local";
                bool allcovertray = false;
                if (usbCamera.itpk == usbCamera.__position.Count)
                {
                    allcovertray = true;
                }

                if ((GemCtrl.gemController.CommunicationState != CommunicationState.Disabled))
                {
                    WaitEvtOn(600000, GemCtrl.cmdS2F41evt, "Wait For Host PP-SELECT", "ER_IPST_E15");
                    //end of pp-select
                    currentrecipe = GemCtrl.GetCurrentSvValue("PPExecName");
                    sCurrentRecipe = currentrecipe;
                    CurrentRecipe = sCurrentRecipe;
                    log.Debug("PP Select Recipe Request : " + currentrecipe);
                    log.Debug("Current Recipe : " + mainapp.menu.DefaultRecipe);
                    logTool.DebugLog("Current Recipe : " + mainapp.menu.DefaultRecipe);
                }
                else
                {
                    log.Debug("Equipment on local, no recipe set");
                    log.Debug("Use Current Recipe");
                    logTool.DebugLog("Use Current Recipe");
                    currentrecipe = mainapp.menu.DefaultRecipe;
                    sCurrentRecipe = currentrecipe;
                    CurrentRecipe = sCurrentRecipe;
                }
                Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
                {
                    if (currentrecipe != "SkipInspection")
                        GemCtrl.gemPP_Recipename = currentrecipe;

                });
                //load recipe
                try
                {
                    // enable auto recipe load by doing this...
                    if ((mainapp.menu.DefaultRecipe != currentrecipe) && (currentrecipe != "SkipInspection"))//tmp remove
                    {
                        log.Debug("Recipe Load");
                        logTool.DebugLog("Recipe Load");
                        pMode.SetInfoMsg("Loading Recipe on PP Select Request");
                        //mainapp.VisionLoad(currentrecipe);
                        //mainapp.loadvisiontoprocessor();

                        mainapp.ReadVisionFile(currentrecipe);
                        mainapp.razor.Init(new helper.GenericEvents());
                        mainapp.imgprocessor.Load3DVisionObject(mainapp.razor);
                        usbCamera._IndexValue = mainapp.razor.rcpPitch;
                        pMode.SetInfoMsg("Loading Recipe on PP Select Request Complete");
                        log.Debug("Recipe Load successful");
                        logTool.DebugLog("Recipe Load successful");
                    }
                    else
                    {
                        usbCamera._IndexValue = mainapp.razor.rcpPitch;
                        if (currentrecipe != "SkipInspection")
                        {
                            pMode.SetInfoMsg("Current Recipe Same as PP Select Request");
                            logTool.InfoLog("Current Recipe Same as PP Select Request");
                        }
                        else
                        {
                            pMode.SetInfoMsg("PP Select Skip Inspection");
                            logTool.InfoLog("PP Select Skip Inspection");
                        }
                        // not full prove if non carrier tray skip inspection

                    }
                    if (intCoverTray >= 2)
                    {
                        string strCovertray1 = "covertray";
                        log.Debug("CoverTrayOut" + intCoverTray);
                        mainapp.ReadVisionFile(strCovertray1);
                        mainapp.razor.Init(new helper.GenericEvents());
                        mainapp.imgprocessor.Load3DVisionObject(mainapp.razor);
                        usbCamera._IndexValue = mainapp.razor.rcpPitch;
                        //posdictionary["Engage"].InPositionRange
                    }

                    double MaxPitch = 0;
                    double lasttraypos = 0;
                    if (usbCamera._IndexValue == 565)
                    {
                        pMode.SetInfoMsg("Pitch Detected is 565");
                        logTool.InfoLog("Pitch Detected is 565");
                        MaxPitch = usbCamera.T556_Set;
                        lasttraypos = usbCamera.LastTrayPos_T556;
                    }
                    if (usbCamera._IndexValue == 635)
                    {
                        pMode.SetInfoMsg("Pitch Detected is 635");
                        logTool.InfoLog("Pitch Detected is 635");
                        MaxPitch = usbCamera.T635_Set;
                        lasttraypos = usbCamera.LastTrayPos_T635;

                    }
                    if (usbCamera._IndexValue == 1005)
                    {
                        pMode.SetInfoMsg("Pitch Detected is 1005");
                        logTool.InfoLog("Pitch Detected is 1005");
                        MaxPitch = usbCamera.T1016_Set;
                        lasttraypos = usbCamera.LastTrayPos_T1016;
                    }

                    if (usbCamera._IndexValue == 556)
                    {
                        pMode.SetInfoMsg("Pitch Detected is 556");
                        logTool.InfoLog("Pitch Detected is 556");
                        MaxPitch = usbCamera.T556_Set;
                        lasttraypos = usbCamera.LastTrayPos_T556;
                    }


                    //check for inverted
                    if (Double.Parse(usbCamera.__maxPitch) > MaxPitch)
                    {
                        this.GemCtrl.SetAlarm("ER_IPST_E19");
                        throw new Exception("Pitch Error");
                    }
                    string lastpos = usbCamera.__position.Last();
                    if (Double.Parse(lastpos) < lasttraypos)
                    {
                        logTool.WarnLog("Last Tray Wrong Orientation Pos value " + lastpos);
                        throw new Exception("Last Tray Wrong Orientation Pos value " + lastpos);
                    }


                    //end testing
                    GemCtrl.cmdS2F41evt.Reset();
                    GemCtrl.strS2F42Reststring = "0";
                    GemCtrl.cmdS2F42evt.Set();
                }
                catch (Exception ex)
                {
                    GemCtrl.cmdS2F41evt.Reset();
                    GemCtrl.cmdS2F42evt.Set();
                    //pMode.SetError("PP SELECT Recipe Load Fail Recipe Name Requested : <" + currentrecipe + ">", true);
                    //GemCtrl.SetAlarm("ER_IPST_E17");
                    pMode.ChkProcessMode();
                    log.Debug(ex.ToString());
                    throw ex;

                }
                //end of recipe load
                //wait for remote start command
                //wait for pp-select
                if (GemCtrl.gemController.CommunicationState != CommunicationState.Disabled)
                {
                    WaitEvtOn(10000, GemCtrl.cmdS2F41evt, "Wait For Host Remote Start Command", "ER_IPST_E16");
                }
                //end

                IndexList = new List<int>();
                CalibratedList = new List<int>();
                //get base value
                long StackBase = posdictionary["StackBase"].Coordinate;
                long Stack5mm = posdictionary["Stack5mm"].Coordinate;
                long Stack6mm = posdictionary["Stack6mm"].Coordinate;
                long Stack10mm = posdictionary["Stack10mm"].Coordinate;
                long stak562mm = posdictionary["Stack562mm"].Coordinate;
                long StackCarrier = posdictionary["StackCover"].Coordinate;
                //int mmC_pitch = 556;//tmp
                int mmC_pitch = (int)((StackBase - StackCarrier) / 29);
                int mm5_pitch = (int)((StackBase - Stack5mm) / 29);
                int mm6_pitch = (int)((StackBase - Stack6mm) / 29);
                int mm10_pitch = (int)((StackBase - Stack10mm) / 18);
                int mm562_pitch = (int)((StackBase - stak562mm) / 29);
                log.Debug("calibrated cover pitch " + mmC_pitch.ToString());
                log.Debug("5mm cover pitch " + Stack5mm.ToString());
                log.Debug("6mm cover pitch " + Stack6mm.ToString());
                log.Debug("10mm cover pitch " + Stack10mm.ToString());
                log.Debug("StackBase " + StackBase.ToString());

                mainapp.IpTrayCount = IndexList.Count.ToString();
                //});
                int k = 0;
                if (TrayIDList[0].Contains("TPK"))
                {
                    if (usbCamera.itpk != usbCamera.__position.Count)//different so expect first tray to be cover tray
                                                                     //did not check if itpk == count may be a problem later...
                    {
                        k = 1;
                        IndexList.Add(556); //add index tray
                        CalibratedList.Add(mmC_pitch);//this is wrong.. if not calibrated
                        for (int i = 0; i < (usbCamera.__position.Count - 1); i++)//assuming all trays are the same
                        {
                            if (TrayIDList[i + 1].Contains("TPK"))
                            {
                                //should not be the case.. if yes.. reject tray stacks
                                pMode.SetWarningMsg("Invalid Tray Stack with additional cover trays");
                                GemCtrl.cmdS2F42evt.Set();
                                Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
                                {
                                    GemCtrl.MachineMsg = "Invalid Tray Stack with additional cover trays";
                                    GemCtrl.SetDisplay(true);
                                });
                                iChkStatus = 2;
                                throw new Exception("nvalid Tray Stack with additional cover trays");
                            }
                            IndexList.Add(usbCamera._IndexValue);
                            //new calibrated algorithm
                            if (usbCamera._IndexValue == 1005)
                            {
                                CalibratedList.Add(mm10_pitch);
                            }
                            if (usbCamera._IndexValue == 565)
                            {
                                CalibratedList.Add(mm5_pitch);
                            }
                            if (usbCamera._IndexValue == 635)
                            {
                                CalibratedList.Add(mm6_pitch);
                            }
                            if (usbCamera._IndexValue == 562)
                            {
                                CalibratedList.Add(mm562_pitch);
                            }
                        }
                    }
                    else//all trays are expected to be cover trays
                    {
                        for (int i = 0; i < (usbCamera.__position.Count); i++)//assuming all trays are the same
                        {
                            IndexList.Add(556);
                            // CalibratedList.Add(mainapp.razor.rcpPitch);//this is wrong
                            CalibratedList.Add(mmC_pitch);
                        }
                    }
                }
                else
                {
                    //if its not cover tray
                    if (usbCamera.itpk == 0)
                    {
                        //assuming all trays are the same
                        for (int i = 0; i < (usbCamera.__position.Count); i++)
                        {
                            IndexList.Add(usbCamera._IndexValue);
                            //new calibrated algorithm
                            if (usbCamera._IndexValue == 1005)
                            {
                                CalibratedList.Add(mm10_pitch);
                            }
                            if (usbCamera._IndexValue == 565)
                            {
                                CalibratedList.Add(mm5_pitch);
                            }
                            if (usbCamera._IndexValue == 635)
                            {
                                CalibratedList.Add(mm6_pitch);
                            }
                        }
                    }
                    else
                    {
                        //there is a cover tray inside.. reject tray stacks
                        //should not be the case.. if yes.. reject tray stacks
                        pMode.SetInfoMsg("Invalid Tray Stack with additional cover trays");
                        logTool.InfoLog("Invalid Tray Stack with additional cover trays");
                        GemCtrl.cmdS2F42evt.Set();
                        Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
                        {
                            GemCtrl.MachineMsg = "Invalid Tray Stack with additional cover trays";
                            GemCtrl.SetDisplay(true);
                        });
                        iChkStatus = 3;
                        pMode.SetWarningMsg("nvalid Tray Stack with additional cover trays");
                        return RunState.CameraResultError;

                    }
                }

                // removed on 13th May
                // if (!TrayIDList[0].Contains("TPK")) throw new Exception("Label detect error");
                //do the secs/gem request here... now that we make new assumption that cover tray does not necessary appear

                Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
                {
                    mainapp.CarrierHt = IndexList[0].ToString() + "0 um";
                    mainapp.strCarrierID = TrayIDList[0];//this is the carrier ID
                    mainapp.IpTrayCount = IndexList.Count.ToString();
                });
            }
            catch (Exception ex)
            {
                pMode.SetWarningMsg("Exception on Vision Label Error :" + ex.Message);
                logTool.WarnLog("Exception on Vision Label Error :" + ex.Message);
                GemCtrl.cmdS2F42evt.Set();
                Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
                {
                    GemCtrl.MachineMsg = ex.Message;
                    GemCtrl.SetDisplay(true);
                });
                iChkStatus = 4;
                return RunState.CameraResultError;
            }
            //tray map successful
            GemCtrl.strS2F42Reststring = "0";
            GemCtrl.cmdS2F41evt.Reset();
            GemCtrl.cmdS2F42evt.Set();
            // evtIPStackerAllowBuffer.Set(); //ready to accept buffer
            return RunState.MoveTurretRemotePosition;
        }
        private RunState CameraResultErrorFn()
        {            
            Sopdu.ProcessApps.main.MainApp.AmberLed.SetOutput(true);
            Sopdu.ProcessApps.main.MainApp.Buzzer.SetOutput(true);
            ProcessMaster.redLedBuzzerBypass = true;
            if (isExtendBWD && !isTurretAtRm)
            {
                TrigTurretRemote();
            }
            if (isTurretAtRm)
            {
                TrigLedGrn(true);
                TrigLedRed(false);
                return RunState.WaitDoorOpen;
            }
            return runstate;
        }

        private RunState TurretRemoteFn()
        {
            if (isExtendBWD && !isTurretAtRm)
            {
                TrigTurretRemote();
                bHasExcuted = false;
            }
            if (isTurretAtRm)
            {
                return RunState.WaitForIPStackerRdyToLoad;
            }
            return runstate;
        }
        public bool recoverCondition = true;
        private RunState HSOHTFn()
        {
           
            if (!isExtendBWD)
                TrigExtendBWD();
            if (isExtendBWD)
            {
                if (!isTurretAtHm)
                {
                    TrigLedGrn(false);
                    TrigLedRed(true);
                    TrigTurretHome();
                }
                    
                if (!isLifterAtUp)
                    TrigLifterUp();
            }
            if (isExtendBWD && isTurretAtHm && isLifterAtUp)
            {
                if (isOHTAlive && isOHTSelect)
                {
                    logTool.DebugLog($"In HSOHT Step ,isOHTAlive signal is: {isOHTAlive},isOHTSelect signal is:{isOHTSelect}");
                    log.Debug($"In HSOHT Step ,isOHTAlive signal is: {isOHTAlive},isOHTSelect signal is:{isOHTSelect}");
                    TrigOHTLReq(true);
                    log.Debug("set TrigOHTLReq with true ");
                    logTool.DebugLog("set TrigOHTLReq with true ");
                    if (isOHTRequest)
                    { 
                        TrigOHTRdy(true);
                        recoverCondition = false;
                        log.Debug("isOHTRequest is ture, set TrigOHTRdy with true");
                        logTool.DebugLog("isOHTRequest is ture, set TrigOHTRdy with true");
                    }
                    if (isOHTBusy)
                    {
                        recoverCondition = false;
                        log.Debug("isOHTBusy is ture");
                        logTool.DebugLog("isOHTBusy is ture");
                        sw.Stop();
                        sw.Reset();
                        return RunState.RunOHTIn;
                    }
                }
                else
                {
                    if (recoverCondition==true)
                    {
                        TrigOHTLReq(false);
                        TrigOHTRdy(false);
                        TrigLifterDown();
                        if (!isExtendBWD)
                            TrigExtendBWD();
                        if (isExtendBWD)
                        {
                            TrigTurretRemote();
                        }
                        if (isExtendBWD && isLifterAtDn && isTurretAtRm)
                        {
                            sw.Stop();
                            sw.Reset();
                            return RunState.Start;
                        }
                    }
                }
            }
            if (!sw.IsRunning)
            {
                sw.Reset();
                sw.Start();
            }
            if (sw.ElapsedMilliseconds > iTimeOut)
            {
                pMode.SetError("OHT put the tray in Time out", true);
                logTool.DebugLog("OHT put the tray in Time out");
            }
               
            return runstate;
        }
        private RunState RunOHTInFn()
        {
            recoverCondition = true;
            if (isPresentSensor)
                isPartMM = true;
            if (isOHTDetect)
            { 
                TrigOHTLReq(false);
                log.Debug("In runOHTInFn step,set TrigOHTLReq with false");
                logTool.DebugLog("In runOHTInFn step,set TrigOHTLReq with false");
            }
            if (!isOHTBusy && isOHTComplete)
            { 
                TrigOHTRdy(false);
                log.Debug($"In runOHTInFn step,set TrigOHTRdy with false,isOHTBusy is:{isOHTBusy},isOHTComplete is {isOHTComplete}");
                logTool.DebugLog($"In runOHTInFn step,set TrigOHTRdy with false,isOHTBusy is:{isOHTBusy},isOHTComplete is {isOHTComplete}");
            }
            if (!sw.IsRunning)
            {
                sw.Reset();
                sw.Start();
            }
            if (sw.ElapsedMilliseconds > iTimeOut)
            { 
                pMode.SetError("OHT Tray in Time out", true);
                logTool.DebugLog("OHT Tray in Time out");
            }
               
            if (isPartPresent && !isOHTBusy && !isOHTSelect && !isOHTAlive && !isOHTComplete && !dOOHT4Rdy.Logic)
            {
                log.Debug($"isOHTBusy is{isOHTBusy},isOHTSelect is {isOHTSelect},isOHTAlive is {isOHTAlive},isOHTComplete is {isOHTComplete}");
                logTool.DebugLog($"isOHTBusy is{isOHTBusy},isOHTSelect is {isOHTSelect},isOHTAlive is {isOHTAlive},isOHTComplete is {isOHTComplete}");
                if (!isLifterAtDn)
                    TrigLifterDown();
                if (!isTurretAtRm)
                    TrigTurretRemote();
                if (isLifterAtDn && isTurretAtRm)
                {
                    bHasExcuted = false;
                    return RunState.MoveTurretHomePosition;
                    //return RunState.WaitForIPStackerRdyToLoad;
                }
            }

            return runstate;
        }
        private RunState WaitDoorOpenFn()
        {
            //int iTimeout = valvelist[sCylDoor].i
            if (!isLifterAtDn)
                TrigLifterDown();
            if (!isExtendBWD)
                TrigExtendBWD();
            if (isExtendBWD)
            {
                if (!isTurretAtRm)
                    TrigTurretRemote();
            }
            if (!isDoorOpened)
            {
                if (isCurtainTrig)
                {
                    sw.Stop();
                    sw.Reset();
                    if (doDoorOpen.Logic)
                    { 
                        pMode.SetInfoMsg("Curtain Sensor Blocked while Input shutter door Opening!");
                        logTool.InfoLog("Curtain Sensor Blocked while Input shutter door Opening!");
                    }
                    TrigDoorStop();
                }
                else if (!isFSWOn)
                {
                    sw.Stop();
                    sw.Reset();
                    if (doDoorOpen.Logic)
                    {
                        pMode.SetInfoMsg("Two Press Button released while Input shutter door Opening!");
                        logTool.InfoLog("Two Press Button released while Input shutter door Opening!");
                    }
                       
                    TrigDoorStop();
                }
                else
                {
                    if (doDoorOpen.Logic == false)
                        TrigDoorOpen();
                    else
                    {
                        if (sw.IsRunning)
                        {
                            if (sw.ElapsedMilliseconds > iTimeoutOpen)
                            {
                                sw.Stop();
                                logTool.DebugLog("Input Shutter Door Open Time out");
                                pMode.SetError("Input Shutter Door Open Time out", true, doDoorOpen.DisplayName);
                            }
                        }
                        else
                            sw.Start();
                    }
                }
            }
            //if (isDoorOpened && isLifterAtDn && isExtendBWD)
            //    return RunState.WaitManulLoad;
            //if (isDoorOpened && isLifterAtDn && isExtendBWD)
            if (isDoorOpened && isLifterAtDn && isExtendBWD && isPresentSensor)
            {
                pMode.SetInfoMsg("WaitManulLoadOpen");
                logTool.InfoLog("WaitManulLoadOpen");
                Sopdu.ProcessApps.main.MainApp.AmberLed.SetOutput(false);
                Sopdu.ProcessApps.main.MainApp.Buzzer.SetOutput(false);
                ProcessMaster.redLedBuzzerBypass = false;
                return RunState.WaitManulLoad;
            }

            if (!isFSWOn && isDoorOpened)
            {
                pMode.SetInfoMsg("WaitDoorClose");
                logTool.InfoLog("WaitDoorClose");
                Sopdu.ProcessApps.main.MainApp.AmberLed.SetOutput(false);
                Sopdu.ProcessApps.main.MainApp.Buzzer.SetOutput(false);
                ProcessMaster.redLedBuzzerBypass = false;
                return RunState.WaitDoorClose;
            }
            return runstate;
        }

        private RunState WaitDoorCloseFn()
        {
            //int iTimeout = valvelist[sCylDoor].i
            if (!isLifterAtDn)
                TrigLifterDown();
            if (!isExtendBWD)
                TrigExtendBWD();
            if (isExtendBWD)
            {
                if (!isTurretAtRm)
                    TrigTurretRemote();
            }

            if (!isDoorClosed)
            {
                if (isCurtainTrig)
                {
                    sw.Stop();
                    sw.Reset();
                    if (doDoorClose.Logic)
                    { 
                        pMode.SetInfoMsg("Curtain Sensor Blocked while Input shutter door Closing!");
                        logTool.InfoLog("Curtain Sensor Blocked while Input shutter door Closing!");
                    }
                       
                    if (!diDoorOpen.Logic)
                    {
                        TrigDoorStop();
                    }
                }
                else if (!isFSWOn)
                {
                    sw.Stop();
                    sw.Reset();
                    if (doDoorClose.Logic)
                    { 
                        pMode.SetInfoMsg("Two Press Button released while Input shutter door Closing!");
                        logTool.InfoLog("Two Press Button released while Input shutter door Closing!");
                    }
                       
                    if (!diDoorOpen.Logic)
                    {
                        TrigDoorStop();
                    }
                }
                else
                {
                    if (doDoorClose.Logic == false)
                    {
                        TrigDoorClose();
                        
                    }
                    else
                    {
                        if (sw.IsRunning)
                        {
                            if (sw.ElapsedMilliseconds > iTimeoutOpen)
                            {
                                sw.Stop();
                                pMode.SetError("Input Shutter Door Closing Time out", true, doDoorOpen.DisplayName);
                                logTool.ErrorLog("Input Shutter Door Closing Time out");
                            }
                        }
                        else
                            sw.Start();
                    }
                }
            }

            //if (isDoorOpened && isLifterAtDn && isExtendBWD)

            //if (isDoorOpened && isLifterAtDn && isExtendBWD && isPresentSensor)
            if (isLifterAtDn && isExtendBWD && isPresentSensor)
            {
                pMode.SetInfoMsg("WaitManulLoadCls");
                logTool.InfoLog("WaitManulLoadCls");
                return RunState.WaitManulLoad;
            }

            if (!isFSWOn && isDoorClosed)
            {
                pMode.SetInfoMsg("Judgementcls");
                logTool.InfoLog("Judgementcls");
                return RunState.Judgement;
            }

            return runstate;
        }

        private RunState WaitManualLoadFn()
        {
            iCnt = 0;
            sw.Stop();
            sw.Reset();
            pMode.ChkProcessMode();

            if (isReverse)
            {
                if (!evtIPStackerAllowBuffer.WaitOne(0))
                {
                    log.Debug("evtIPStackerAllowBuffer waiting...");
                    return RunState.WaitForBufferAllow;
                }
                    
            }

            TrigLedBlinkGrn(3);

            //if (isPresentSensor)
            //{
            //    isPartMM = true;

            //    return RunState.WaitForIPBtn;
            //}            
            if (!isFSWOn)
            {
                return RunState.WaitForIPBtn;
            }


            return runstate;
        }
        private RunState WaitForIPBtnFn()
        {
           // if (isPartAbsent) return RunState.WaitManulLoad;

            //if (isPartPresent)
            //{

                if (!isDoorClosed)
                {
                    if (isCurtainTrig)
                    {
                        sw.Stop();
                        sw.Reset();
                        if (doDoorClose.Logic)
                        { 
                            pMode.SetInfoMsg("Curtain Sensor Blocked while Input shutter door Closing!");
                            logTool.InfoLog("Curtain Sensor Blocked while Input shutter door Closing!");
                        }
                           
                        if (!diDoorOpen.Logic)
                        {
                            TrigDoorStop();
                        }
                    }
                    else if (!isFSWOn)
                    {
                        sw.Stop();
                        sw.Reset();
                        if (doDoorClose.Logic)
                        {
                            pMode.SetInfoMsg("Two Press Button released while Input shutter door Closing!");
                            logTool.InfoLog("Two Press Button released while Input shutter door Closing!");
                        }
                           
                        if (!diDoorOpen.Logic)
                        {
                            TrigDoorStop();
                        }
                    }
                    else
                    {
                        if (doDoorClose.Logic == false)
                        { 
                            TrigDoorClose();
                           
                        }
                            
                        else
                        {
                            if (sw.IsRunning)
                            {
                                if (sw.ElapsedMilliseconds > iTimeoutClos)
                                {
                                    sw.Stop();
                                    pMode.SetError("Input Shutter Door Close Time out", true, doDoorClose.DisplayName);
                                    logTool.ErrorLog("Input Shutter Door Close Time out");
                                }
                            }
                            else
                                sw.Start();
                        }
                    }
                }

                if (isDoorClosed)
                {
                    //if (isPartAbsent) return RunState.Start;
                    if (isPartAbsent && !isFSWOn) return RunState.Start;
                    //button pressed//
                    if (isPresentSensor)
                    {
                        TrigLedGrn(false);
                        TrigLedRed(true);
                        GemCtrl.BufferQtyChanged(1, 0);
                        GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.TransferBlocked, "LoadPortTransferStateReadyToLoadTransferBlocked1");//change state
                        return RunState.MoveTurretHomePosition;
                    }

                    
                    
                    
                    //if (evtIPStackerRdyToLoad.WaitOne(0))
                    //{
                    //    return RunState.RequestIPStackerCVMove;
                    //}
                    //else
                    //{
                    //    bHasExcuted = false;
                    //    return RunState.WaitForIPStackerRdyToLoad;
                    //}
                }
            //}
            return runstate;
        }
        private RunState TurretHomeFn()
        {
            if (!isTurretAtHm && isExtendBWD)
                TrigTurretHome();
            if (isTurretAtHm)
            {
                return RunState.PitchDetection;
            }
            return runstate;
        }

        private RunState WaitForIPStackerRdyToLoadFn()
        {
            pMode.ChkProcessMode();
            initblink++;
            if (initblink > 10) //Slow blinking of 1~2 sec ie. 100ms per cycleWaitStackerFreeIPCVFn
            {
                //PBLed.Logic = !PBLed.Logic;
                PBLedRed.Logic = true;
                initblink = 0;
            }
            if (isPartPresent)
            {
                if (bHasExcuted == false)
                {
                    GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.TransferBlocked, "LoadPortTransferStateReadyToLoadTransferBlocked1");
                    bHasExcuted = true;
                }
                if (evtIPStackerRdyToLoad.WaitOne(100))
                    return RunState.RequestIPStackerCVMove;
                else
                    return runstate;
            }
            else
                return RunState.EndofCycle;
        }
        private RunState RequestIPStackerCVMoveFn()
        {
            if (isPartPresent)
            {
                if (isHeightSensor)
                {
                    evtIPCVRequestStackerCVRun.Set();
                    WaitEvtOn(10000, evtIPCVRequestStackerCVRunAck, "IPCV Ack Time Out", "ER_IPCV_E01");
                    evtIPCVRequestStackerCVRunAck.Reset();
                    return RunState.RunCVOut;
                }
                else
                {
                    pMode.SetError("IPCV Tray Height Exceed Limit", true); GemCtrl.SetAlarm("ER_IPCV_E06"); pMode.ChkProcessMode();
                    logTool.DebugLog("IPCV Tray Height Exceed Limit");
                }
            }

            return runstate;
        }
        private RunState RunCVOutFn()
        {
            pMode.ChkProcessMode();
            if (!isTurretAtRm)
                TrigTurretRemote();
            if (isTurretAtRm)
            {
                if (!isExtendFWD)
                    TrigExtendFWD();
                if (isExtendFWD)
                {
                    CVMove(false, false);
                    //M3Fwd.Logic = true;
                    isPartMM = false;
                    WaitEvtOn(15000, evtStackerCVRunComplete, "IPCV Wait For IP Stacke Run Complete time out", "ER_IPCV_E02");
                    evtStackerCVRunComplete.Reset();
                    //M3Fwd.Logic = false;
                    CVStop();
                    pMode.SetInfoMsg("Wait For InputStacker To Free IP CV");
                    logTool.InfoLog("Wait For InputStacker To Free IP CV");
                    return RunState.WaitStackerFreeIPCV;
                }
            }
            return runstate;
        }
        private RunState WaitStackerFreeIPCVFn()
        {
            pMode.ChkProcessMode();

            if (evtIPStackerAllowBuffer.WaitOne(0))
            {
                TrigExtendBWD();
                pMode.SetInfoMsg("Input Stacker Ready To Accept Buffer");
                logTool.InfoLog("Input Stacker Ready To Accept Buffer");
                return RunState.EndofCycle;
            }
            if (evtRevCVRequest.WaitOne(0))
            {
                pMode.SetInfoMsg("Reverse Sequence Recieved from Input Stacker");
                logTool.InfoLog("Reverse Sequence Recieved from Input Stacker");
                evtRevCVRequest.Reset();
                //check for sensor
                if (isPartPresent) { pMode.SetError("IPCV Not Cleared", true); GemCtrl.SetAlarm("ER_IPCV_E03"); pMode.ChkProcessMode(); }
                return RunState.CVRevSequence;
            }
            return runstate;
        }
        private RunState CVRevSequenceFn()
        {
            pMode.SetInfoMsg("Reverse Sequence Recieved");
            TrigExtendFWD();
            CVMove(true, false);// motor reverse
            evtRevCVRequestAck.Set();
            WaitEvtOn(20000, evtIPStackerRdyToLoad, "Wait For Tray Clear Input Stacker Conveyor");
            WaitEvtOn(10000, TrayPresentSensor.evtOff, "Wait For Tray Presence at Input Conveyor");
            isPartMM = true;
            isReverseTray = true;
            CVStop();// motor reverse
            return RunState.FailTrayClear;
        }
        private RunState FailTrayClearFn()
        {
            if (isPartPresent && isReverseTray)
            {
                TrigLedBlinkGrn(5);
                if (!isExtendBWD)
                    TrigExtendBWD();
                if (!isDoorOpened)
                    TrigDoorOpen();
            }
            if (isDoorOpened)
            {
                if (!isPresentSensor)
                {
                    TrigLedBlinkRed(5);
                    if (!sw.IsRunning)
                    {
                        sw.Reset();
                        sw.Start();
                    }
                    isPartMM = false;
                    isReverseTray = false;
                    pMode.SetInfoMsg("Manul Remove Failed Tray Required");
                    logTool.DebugLog("Manul Remove Failed Tray Required");
                    if (isFSWOn)
                    {
                        isPartMM = false;
                        isReverseTray = false;
                    }
                }
            }
            if (isPartAbsent && isFSWOn)
            {
                if (isCurtainTrig)
                {
                    if (!isDoorClosed)
                    { 
                        TrigDoorClose();
                    }
                        
                }
                else
                {
                    TrigDoorOpen();
                }

                if (isDoorClosed)
                {
                    TrigLedGrn(false);
                    TrigLedRed(false);
                    return RunState.EndofCycle;
                }
            }
            return runstate;
        }
        private RunState EndofCycleFn()
        {
            if (isExtendFWD)
                TrigExtendBWD();
            if (isExtendBWD)
                return RunState.Start;
            return runstate;
        }

        protected override void StoppingLogicFn()
        {
            //set all motor current state
            bM3Bwdbackup = MxBwd.Logic;
            bM3Fwdbackup = MxFwd.Logic;
            //set all motor to stop
            CVStop();
        }
        protected override void RecoverFromStopFn()
        {
            base.RecoverFromStopFn();
            MxFwd.Logic = bM3Fwdbackup;
            MxBwd.Logic = bM3Bwdbackup;
        }

        private RunState runstate;
        private enum RunState
        {
            Start, Judgement,               // start and option based on manul load and auto-load(SFA Conveyor or OHT)
            WaitDoorOpen, WaitManulLoad, WaitForIPBtn, HSCvr, RunCVIn, HSOHT, RunOHTIn,  //Tray stack loading
            RequestIPStackerCVMove, RunCVOut, WaitStackerFreeIPCV, WaitForBufferAllow, PitchDetection, TrayMapInspection, CameraResultError, MoveTurretRemotePosition, MoveTurretHomePosition, WaitForIPStackerRdyToLoad,
            CVRevSequence, FailTrayClear,   // Reverse 2D scanning failed tray stacker and manual remove
            EndofCycle, WaitDoorClose
        }

        #endregion

        #region // IO and Event Configuration

        //55	X87	    Tray At IP C/V Inpos Sensor	    Sensor	                   IP C/V Module
        //56	X88	    IP C/V Start Push Button 	    PB 	                       IP C/V Module
        private DiscreteIO TrayPresentSensor, dIClearSensor, dIFootSW, dIHandSensor, dIHeightSensor, dITouchButton1, dITouchButton2, dICurtain1, dICurtain2, dICurtainErr;

        //17	Y17	IP C/V Module	                M3 FWD	    IP C/V Module
        //18	Y18	IP C/V Module	                M3 BWD      IP C/V Module
        //19	Y19	IP C/V Start Push Button LED	PB LED      IP C/V Module
        //26    Y77 IP C/V Plasma fan
        private DiscreteIO MxFwd, MxFwdSlow, MxBwd, PBLedGrn, PBLedRed, dOPB1Led, dOPB2Led, dOPlasmafan;

        /// <summary>
        /// Conveyor tray stack transfer smema
        /// </summary>
        private DiscreteIO dISFAValid, dISFAReady, dISFADone;
        private DiscreteIO dOCtrValid, dOCtrReady, dOCtrDone;

        /// <summary>
        /// OHT is active, and counter is passive
        /// </summary>
        private DiscreteIO dIOHT1Valid, dIOHT2CS0, dIOHT3CS1, dIOHT4Avbl, dIOHT5Req, dIOHT6Busy, dIOHT7Done, dIOHT8Cntnu;
        private DiscreteIO dOOHT1LReq, dOOHT2UReq, dOOHT3VA, dOOHT4Rdy, dOOHT5VS0, dOOHT6VS1, dOOHT7Avbl, dOOHT8ESTP;

        /// <summary>
        /// Digital IO for cylinder
        /// </summary>
        private DiscreteIO diDoorOpen, diDoorClose, diTurretHm, diTurretRm, diLifterDn, diLifterUp, diExtendRtn, diExtendExt;
        private DiscreteIO doDoorOpen, doDoorClose, doTurretHm, doTurretRm, doLifterDn, doLifterUp, doExtendRtn, doExtendExt;

        private ManualResetEvent evtIPStackerAllowBuffer, evtIPStackerRdyToLoad, evtStackerCVRunComplete, evtIPCVRequestStackerCVRunAck, evtIPCVRequestStackerCVRun,
                                 evtInit_InputStackerHomeComplete, evtRevCVRequest, evtRevCVRequestAck, evtRevMotorStopped;

        protected override void InitInput()     //initialize input
        {
            base.InitInput();
            //assigning inputs
            //<string>Input55</string>	<!--TrayInPosSensor-->
            //<string>Input56</string>	<!--IP_PushBtn-->

            dISFAValid = this.inputlist.IpDirectory[InputNameList[0]];
            dISFAReady = this.inputlist.IpDirectory[InputNameList[1]];
            dISFADone = this.inputlist.IpDirectory[InputNameList[2]];

            dIOHT1Valid = this.inputlist.IpDirectory[InputNameList[3]];
            dIOHT2CS0 = this.inputlist.IpDirectory[InputNameList[4]];
            dIOHT3CS1 = this.inputlist.IpDirectory[InputNameList[5]];
            dIOHT4Avbl = this.inputlist.IpDirectory[InputNameList[6]];
            dIOHT5Req = this.inputlist.IpDirectory[InputNameList[7]];
            dIOHT6Busy = this.inputlist.IpDirectory[InputNameList[8]];
            dIOHT7Done = this.inputlist.IpDirectory[InputNameList[9]];
            dIOHT8Cntnu = this.inputlist.IpDirectory[InputNameList[10]];

            TrayPresentSensor = this.inputlist.IpDirectory[InputNameList[11]];
            dIClearSensor = this.inputlist.IpDirectory[InputNameList[12]];
            //dIFootSW = this.inputlist.IpDirectory[InputNameList[13]];
            dIHeightSensor = this.inputlist.IpDirectory[InputNameList[13]];
            //dIHandSensor = this.inputlist.IpDirectory[InputNameList[15]];

            diTurretHm = this.inputlist.IpDirectory[InputNameList[15]];
            diTurretRm = this.inputlist.IpDirectory[InputNameList[14]];
            diExtendRtn = this.inputlist.IpDirectory[InputNameList[17]];
            diExtendExt = this.inputlist.IpDirectory[InputNameList[16]];
            diLifterUp = this.inputlist.IpDirectory[InputNameList[18]];
            diLifterDn = this.inputlist.IpDirectory[InputNameList[19]];
            if (isOwnDoor)
            {
                diDoorClose = this.inputlist.IpDirectory[InputNameList[20]];
                diDoorOpen = this.inputlist.IpDirectory[InputNameList[21]];
                dITouchButton1 = this.inputlist.IpDirectory[InputNameList[22]];
                dITouchButton2 = this.inputlist.IpDirectory[InputNameList[23]];
                dICurtain1 = this.inputlist.IpDirectory[InputNameList[24]];
                dICurtain2 = this.inputlist.IpDirectory[InputNameList[25]];
                dICurtainErr = this.inputlist.IpDirectory[InputNameList[26]];
            }
            isDIOK = true;
        }
        protected override void InitEvt()       //initialize events
        {
            base.InitEvt();
            Dictionary<string, ProcessEvt> evtdict = new Dictionary<string, ProcessEvt>();
            foreach (ProcessEvt e in Evtlist)
            {
                evtdict.Add(e.Name, e);
            }
            evtInit_InputStackerHomeComplete = evtdict[EvtNameList[0]].evt;
            evtRevCVRequest = evtdict[EvtNameList[1]].evt;
            evtRevCVRequestAck = evtdict[EvtNameList[2]].evt;
            evtRevMotorStopped = evtdict[EvtNameList[3]].evt;
            evtIPCVRequestStackerCVRun = evtdict[EvtNameList[4]].evt;
            evtIPCVRequestStackerCVRunAck = evtdict[EvtNameList[5]].evt;
            evtStackerCVRunComplete = evtdict[EvtNameList[6]].evt;
            evtIPStackerRdyToLoad = evtdict[EvtNameList[7]].evt;
            evtIPStackerAllowBuffer = evtdict[EvtNameList[8]].evt;
        }
        protected override void InitOutput()    //initialize output
        {
            base.InitOutput();
            //assigning outputs
            /*
            <string>Output29</string>	<!--M3 BWD-->
		    <string>Output30</string>	<!--M3 FWD-->
		    <string>Output31</string>	<!--M3 LED-->	
             */
            dOCtrValid = this.outputlist.IpDirectory[OutputNameList[0]];
            dOCtrReady = this.outputlist.IpDirectory[OutputNameList[1]];
            dOCtrDone = this.outputlist.IpDirectory[OutputNameList[2]];

            dOOHT1LReq = this.outputlist.IpDirectory[OutputNameList[3]];
            dOOHT2UReq = this.outputlist.IpDirectory[OutputNameList[4]];
            dOOHT3VA = this.outputlist.IpDirectory[OutputNameList[5]];
            dOOHT4Rdy = this.outputlist.IpDirectory[OutputNameList[6]];
            dOOHT5VS0 = this.outputlist.IpDirectory[OutputNameList[7]];
            dOOHT6VS1 = this.outputlist.IpDirectory[OutputNameList[8]];
            dOOHT7Avbl = this.outputlist.IpDirectory[OutputNameList[9]];
            dOOHT8ESTP = this.outputlist.IpDirectory[OutputNameList[10]];

            MxBwd = this.outputlist.IpDirectory[OutputNameList[11]];
            MxFwd = this.outputlist.IpDirectory[OutputNameList[12]];
            MxFwdSlow = this.outputlist.IpDirectory[OutputNameList[13]];
            PBLedGrn = this.outputlist.IpDirectory[OutputNameList[14]];
            PBLedRed = this.outputlist.IpDirectory[OutputNameList[15]];

            doTurretHm = this.outputlist.IpDirectory[OutputNameList[17]];
            doTurretRm = this.outputlist.IpDirectory[OutputNameList[16]];
            doExtendRtn = this.outputlist.IpDirectory[OutputNameList[19]];
            doExtendExt = this.outputlist.IpDirectory[OutputNameList[18]];
            doLifterUp = this.outputlist.IpDirectory[OutputNameList[20]];
            doLifterDn = this.outputlist.IpDirectory[OutputNameList[21]];
            if (isOwnDoor)
            {
                doDoorClose = this.outputlist.IpDirectory[OutputNameList[22]];
                doDoorOpen = this.outputlist.IpDirectory[OutputNameList[23]];
                dOPB1Led = this.outputlist.IpDirectory[OutputNameList[24]];
                dOPB2Led = this.outputlist.IpDirectory[OutputNameList[25]];
                //dOCurtainRst = this.outputlist.IpDirectory[OutputNameList[26]];
            }

            //Replace dOCurtainRst
            if(true)
            {
                dOPlasmafan = this.outputlist.IpDirectory[OutputNameList[26]];
            }
            
            isDOOK = true;
        }

        #endregion

        #region // Common Method & function
        public void CVMove(bool fwd, bool slow)
        {
            if (fwd)
            {
                if (slow)
                {
                    MxBwd.Logic = false;
                    MxFwd.Logic = true;
                    MxFwdSlow.Logic = true;
                }
                else
                {
                    MxFwdSlow.Logic = false;
                    MxFwd.Logic = true;
                    MxBwd.Logic = false;
                }
            }
            else
            {
                MxFwdSlow.Logic = false;
                MxFwd.Logic = false;
                MxBwd.Logic = true;
            }
        }
        public void CVStop()
        {
            MxBwd.Logic = false;
            MxFwd.Logic = false;
            MxFwdSlow.Logic = false;
        }
        private bool CheckPartPresent()
        {
            if (isPresentSensor)
                isPartMM = true;
            else
                isPartMM = false;
            if (isPartMM && isPresentSensor)
                return true;
            else
                return false;
        }
        private bool CheckPartAbsent()
        {
            if (!isPresentSensor)
                isPartMM = false;
            if (!isPartMM && !isPresentSensor)
                return true;
            else
                return false;
        }
        private bool CheckPresentSensor()
        {
            return !TrayPresentSensor.Logic;
        }
        private bool CheckPartClear()
        {
            return dIClearSensor.Logic;
        }
        private bool CheckHeightSensor()
        {
            return dIHeightSensor.Logic;
        }
        private bool CheckOHTDetect()
        {
            return !isHeightSensor | isPresentSensor;
        }
        private bool CheckCurtainSensor()
        {
            //return true;
            return dICurtain1.Logic & dICurtain2.Logic;
        }
        private void TrigLedRed(bool isOn)
        {
            PBLedRed.Logic = isOn;
        }
        private void TrigLedBlinkRed(int iRate)
        {
            iBlinkCntRed++;
            if (iBlinkCntRed > iRate)
            {
                PBLedRed.Logic = !PBLedRed.Logic;
                iBlinkCntRed = 0;
            }
        }
        private void TrigLedGrn(bool isOn)
        {
            PBLedGrn.Logic = isOn;
        }
        private void TrigLedBlinkGrn(int iRate)
        {
            iBlinkCntGrn++;
            if (iBlinkCntGrn > iRate)
            {
                PBLedGrn.Logic = !PBLedGrn.Logic;
                iBlinkCntGrn = 0;
            }
        }
        private void InitHandshake()
        {
            if (eIPMode == EIPMode.Manual)
            {
                TrigCtrAlive(true);
                TrigOHTAvbl(true);
                TrigOHTESTP(true);
            }
            else
            {
                TrigCtrAlive(false);
                TrigOHTAvbl(false);
                TrigOHTESTP(false);
            }
            TrigCtrReady(false);
            TrigCtrDone(false);
            TrigOHTUReq(false);
            TrigOHTRdy(false);

        }
        #endregion

        #region // Add Cylinder
        public bool isDoorOpened { get { return CheckDoorOpen(); } }
        public bool isDoorClosed { get { return CheckDoorClose(); } }
        public bool isTurretAtHm { get { return CheckTurretHome(); } }
        public bool isTurretAtRm { get { return CheckTurretRemote(); } }
        public bool isExtendBWD { get { return CheckExtendBWD(); } }
        public bool isExtendFWD { get { return CheckExtendFWD(); } }
        public bool isLifterAtDn { get { return CheckLifterDown(); } }
        public bool isLifterAtUp { get { return CheckLifterUp(); } }
        private bool isTurretSafe { get { return CheckTurretSafe(); } }
        private int iTimeoutOpen { get { return GetDoorOpenTimeout(); } }
        private int iTimeoutClos { get { return GetDoorCloseTimeout(); } }

        private string sCylDoor = "InputShutterDoor";
        private string sCylTurret = "IPCVRotator";
        private string sCylExtend = "IPCVExtension";
        private string sCylLifter = "IPCVLifter";

        private bool CheckTurretSafe()
        {
            if (doExtendRtn.Logic && diExtendRtn.Logic && isClearSensor)
                return true;
            else
                return false;
        }
        private bool CheckDoorOpen()
        {
            if (isOwnDoor)
            {
                //return valvelist[sCylDoor].WaitRetract();
                return diDoorOpen.Logic & doDoorOpen.Logic;
            }
            else
                return true;
        }
        private bool CheckDoorClose()
        {
            if (isOwnDoor)
            {
                //return valvelist[sCylDoor].WaitExtend();
                return doDoorClose.Logic & diDoorClose.Logic;
            }
            else
                return true;
        }
        private bool CheckTurretHome()
        {
            //return valvelist[sCylTurret].WaitRetract();
            return doTurretHm.Logic & diTurretHm.Logic;
        }
        private bool CheckTurretRemote()
        {
            //return valvelist[sCylTurret].WaitExtend();
            return diTurretRm.Logic /*& doTurretRm.Logic*/;
        }
        private bool CheckExtendBWD()
        {
            //return valvelist[sCylExtend].WaitRetract();
            return diExtendRtn.Logic & doExtendRtn.Logic;
        }
        private bool CheckExtendFWD()
        {
            //return valvelist[sCylExtend].WaitExtend();
            return doExtendExt.Logic & diExtendExt.Logic;
        }
        private bool CheckLifterDown()
        {
            //return valvelist[sCylLifter].WaitRetract();
            return doLifterDn.Logic & diLifterDn.Logic;
        }
        private bool CheckLifterUp()
        {
            //return valvelist[sCylLifter].WaitExtend();
            return doLifterUp.Logic & diLifterUp.Logic;
        }
        private int GetDoorOpenTimeout()
        {
            return valvelist[sCylDoor].GetTimeoutRtn();
        }
        private int GetDoorCloseTimeout()
        {
            return valvelist[sCylDoor].GetTimeoutExt();
        }

        private void TrigDoorOpen()
        {
            if (isOwnDoor)
            {
                valvelist[sCylDoor].Retract();
                //valvelist[sCylDoor].WaitRetract();
            }

        }
        private void TrigDoorClose()
        {
            if (isOwnDoor)
            {
                log.Debug("ABCD");
                valvelist[sCylDoor].Extend();
               
                //valvelist[sCylDoor].WaitExtend();
            }

        }
        private void TrigDoorStop()
        {
            doDoorOpen.Logic = false;
            doDoorClose.Logic = false;
        }
        private void TrigTurretHome()
        {
            if (isTurretSafe)
            {
                valvelist[sCylTurret].Retract();    // Tray inlet position - link to SFA conveyor
                valvelist[sCylTurret].WaitRetract();
            }

        }
        private void TrigTurretRemote()
        {
            if (isTurretSafe && !doTurretRm.Logic)
            {
                valvelist[sCylTurret].Extend();
                valvelist[sCylTurret].WaitExtend();
            }

        }
        private void TrigExtendBWD()
        {
            valvelist[sCylExtend].Retract();
            valvelist[sCylExtend].WaitRetract();
        }
        private void TrigExtendFWD()
        {
            valvelist[sCylExtend].Extend();
            valvelist[sCylExtend].WaitExtend();
        }
        private void TrigLifterDown()
        {
            valvelist[sCylLifter].Retract();
            valvelist[sCylLifter].WaitRetract();
        }
        private void TrigLifterUp()
        {
            valvelist[sCylLifter].Extend();
            valvelist[sCylLifter].WaitExtend();
        }

        private void InitCylinder()
        {
            //if (!doDoorClose.Logic && !doDoorOpen.Logic)
            {
                //if (diDoorClose.Logic)
                //TrigDoorClose();
                //else
                //    TrigDoorOpen();
            }
            if (!doExtendExt.Logic && !doExtendRtn.Logic)
            {
                //if (diExtendExt.Logic)
                //    TrigExtendFWD();
                //else
                TrigExtendBWD();
            }
            if (!doTurretHm.Logic && !doTurretRm.Logic)
            {
                if (diTurretRm.Logic)
                    TrigTurretRemote();
                else
                    TrigTurretHome();
            }
            if (!doLifterDn.Logic && !doLifterUp.Logic)
            {
                if (diLifterUp.Logic)
                    TrigLifterUp();
                else
                    TrigLifterDown();
            }

        }
        public bool CheckPlasmaFanSensor()
        {            
            //return diPlasmafan.Logic & dOPlasmafan.Logic;
            return false;
        }
        private void TrigPlasmaFan(bool on)
        {
            dOPlasmafan.Logic = on;
        }

        #endregion

        #region // Add SFA Conveyor tray stack transfer
        public bool isSFATrayRdy { get { return isSFAAlive & isSFAReady; } }
        private bool isSFAAlive { get { return dISFAValid.Logic; } }
        private bool isSFAReady { get { return dISFAReady.Logic; } }
        private bool isSFADone { get { return dISFADone.Logic; } }
        private bool isCtrAlive { get { return dOCtrValid.Logic; } }
        private bool isCtrReady { get { return dOCtrReady.Logic; } }
        private bool isCtrDone { get { return dOCtrDone.Logic; } }
        private bool isCVRunIn { get { return MxBwd.Logic; } }
        private bool isCVStopped { get { return !MxBwd.Logic & !MxFwd.Logic; } }

        private void TrigCtrAlive(bool isTorF)
        {
            dOCtrValid.Logic = isTorF;
        }
        private void TrigCtrReady(bool isTorF)
        {
            dOCtrReady.Logic = isTorF;
        }
        private void TrigCtrDone(bool isTorF)
        {
            dOCtrDone.Logic = isTorF;
        }
        #endregion

        #region // Add OHT tray stack transfer
        public bool isOHTRdy { get { return isOHTAlive & isOHTSelect; } }
        private bool isInCV = true;
        private bool isOHTAlive { get { return dIOHT1Valid.Logic; } }       // OHT is valid(Alive)
        private bool isOHTSelect { get { return dIOHT2CS0.Logic | dIOHT3CS1.Logic; } }  //OHT port selection
        private bool isOHTAvailable { get { return dIOHT4Avbl.Logic; } }
        private bool isOHTRequest { get { return dIOHT5Req.Logic; } }
        private bool isOHTBusy { get { return dIOHT6Busy.Logic; } }
        private bool isOHTComplete { get { return dIOHT7Done.Logic; } }
        private bool isOHTContinue { get { return dIOHT8Cntnu.Logic; } }
        private static bool _triggerAppointment=false;
	    public static  bool TriggerAppointment
	    {
		    get { return _triggerAppointment;}
		    set { _triggerAppointment = value;}
	    }

        private void TrigOHTLReq(bool isTorF)
        {
            dOOHT1LReq.Logic = isTorF;       // LReq - Load Request
        }
        private void TrigOHTUReq(bool isTorF)
        {
            dOOHT2UReq.Logic = isTorF;       // UReq - Unload Request
        }
        private void TrigOHTVA(bool isTorF)
        {
            dOOHT3VA.Logic = isTorF;     // VA - Vehicle Arrived
        }
        private void TrigOHTRdy(bool isTorF)
        {
            dOOHT4Rdy.Logic = isTorF;
        }
        private void TrigOHTVS0(bool isTorF)
        {
            dOOHT5VS0.Logic = isTorF;        // VS0 -  Carries stage 0 from passive OHS Vehicle          
        }
        private void TrigOHTVS10(bool isTorF)
        {
            dOOHT6VS1.Logic = isTorF;        // VS1 -   Carries stage 1 from passive OHS Vehicle      
        }
        private void TrigOHTAvbl(bool isTorF)
        {
            dOOHT7Avbl.Logic = isTorF;        // Avbl -   Handoff available    
        }
        private void TrigOHTESTP(bool isTorF)
        {
            dOOHT8ESTP.Logic = isTorF;        // ESTP -   Emergency stop     
        }
        #endregion
        #region CancelAppointment
        public static void  CancelAppointment(object state)
        {
            if (GlobalVar.isManualMode ==true)
            {
                sw2.Restart();
            }
            if (sw1.ElapsedMilliseconds > 300000)
            {
                TriggerAppointment = false;
                uIModel.ForegroundColor = "greay";
                sw1.Stop(); 
                sw1.Reset();
            }
            if (sw2.ElapsedMilliseconds >300000)
            {
                if (GlobalVar.bHasPassword)
                {
                    LogoutUserAction.Invoke();
                }
                sw2.Reset();
            }
          
        }
        #endregion

    }
    public class UIModel:INotifyPropertyChanged
    {
        private string  _foregroundColor= "#FF005DAA";

        public string  ForegroundColor
        {
            get { return _foregroundColor; }
            set { _foregroundColor = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ForegroundColor"));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}