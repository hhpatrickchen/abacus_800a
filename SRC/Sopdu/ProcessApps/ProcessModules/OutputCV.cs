using LogPanel;
using Sopdu.Devices.Cylinders;
using Sopdu.Devices.SecsGem;
using Sopdu.helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sopdu.ProcessApps.ProcessModules
{
    //*  this portion was not used replaced by output communication
    public class OutputCV : Process
    {
        LogTool<OutputStacker> logTool = new LogTool<OutputStacker>();
        public OutputCV()
        {
            EquipmentState = new GenericState();
            EquipmentState.SetState(MachineState.NotInit);
            twoFSW = new MyLib.TwoFingerSW();
        }

        #region // Variable definition and property
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private int iBlinkCnt = 0, iBlinkCntGrn = 0, iBlinkCntRed = 0, iCnt = 0;
        public bool isDIOK = false, isDOOK = false;
        private bool bM4Fwdbackup, isPartMM, isHSCvrDone, is2FSWOn;
        private int initHandshakeSignalCyle = 0;
        //public bool isOHTReady { get { return isOHTAlive; } }
        private bool isAutoMode { get { return GlobalVar.isOPCVAuto; } }
        private bool isOHTDetect { get { return CheckOHTDetect(); } }
        public bool isPartPresent { get { return CheckPartPresent(); } }
        public bool isPartAbsent { get { return CheckPartAbsent(); } }
        private bool isPresentSensor { get { return CheckPresentSensor(); } }//Inpos Sensor no11 input73  X105（中间Inpos）
        private bool isInposSensor { get { return CheckInposSensor(); } }//CV Position no13 input52  X84（靠近output stacker Clear）
        private bool isClearSensor { get { return CheckPartClear(); } }//CV Clear Sensor no12 inputA4  X136(最边上Clear)
        private bool isFSWOn { get { return is2FSWOn; } }       // Two finger switch or two touch button or foot switch
        private bool isCurtainTrig { get { return !CheckCurtainSensor(); } }
        private bool isSkipFailTray { get { return CheckResult(); } }
        private bool isOwnDoor { get { return GlobalVar.isDoorAtCV; } }
        private int iCntLimt { get { return GlobalVar.iCntLimt; } }
        private int iTimeOut { get { return GlobalVar.iTimeOut; } }
        private EOPMode eOPMode { get { return GlobalVar.eOPMode; } }
        private MyLib.TwoFingerSW twoFSW;
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
            if (initHandshakeSignalCyle == 0)
            {
                if (pMode.getStopState())
                {
                    TrigCtrAlive(false);
                    TrigCtrReady(false);
                    TrigOHTUReq(false);
                    TrigOHTRdy(false);

                    initHandshakeSignalCyle++;
                }
                else
                {
                    initHandshakeSignalCyle = 0;
                }
            }
            if (!pMode.getStopState())
            {
                initHandshakeSignalCyle = 0;
            }

        }

        #endregion

        #region // Initial Running Sequence

        public override MachineState NotInitFn()
        {
            //assume cycle fast enough to do at least a single pass or
            //we can explictly call before change state by creating another function
            initstate = InitState.NotInit;
            runstate = RunState.Start;
            return base.NotInitFn();
        }

        public override bool RunInitialization()
        {
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

            return InitState.CheckCVClear;
        }
        private InitState CheckCVClearFn()
        {
            iCnt = 0;
            sw.Stop();
            sw.Reset();
            if (isPartPresent || isInposSensor || !isClearSensor) //tray present
            {
                //initstate = InitState.WaitCVClear;
                //pMode.SetInfoMsg("Input Conveyor Part Not Clear, remove part and press Tray Load Ack Button");
                initstate = InitState.NotInit;
                pMode.SetError("Output Conveyor Part Not Clear!", false);
                logTool.ErrorLog("Output Conveyor Part Not Clear!");
            }
            else
                initstate = InitState.Ready;
            return initstate;
        }
        private InitState WaitCVClearFn()
        {
            if (isPartPresent)
            {
                TrigLedBlinkGrn(5);
                if (!isDoorOpened)
                    TrigDoorOpen();
            }
            if (isPartAbsent)
            {
                TrigLedGrn(false);
                TrigLedRed(false);
                initstate = InitState.Ready;
                if (isFSWOn)
                {

                }
            }

            return initstate;
        }
        private InitState ReadyFn()
        {
            //make sure cv is always clear even at ready state
            if (isPartPresent) //tray present
                initstate = InitState.CheckCVClear;

            //two case here... ip stacker request for tray rev or ip stacker home complete
            if (isPartAbsent)
            {
                if (isCurtainTrig)
                {
                    //TrigDoorOpen();
                    TrigDoorStop();
                    initstate = InitState.NotInit;
                    pMode.SetError("Curtain sensor blocked while Output shutter door closing!", false);
                    logTool.DebugLog("Curtain sensor blocked while Output shutter door closing!");
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
                    initstate = InitState.InitComplete;
                    // evtInit_InputStackerHomeComplete.Reset();
                }
            }

            return initstate;
        }
        private InitState InitComplete0Fn()
        {
            InitHandshake();
            RunTimeData.kvpOPCvr.Clear();
            //set state for inputcv runtime
            log.Debug("Output CV Init Complete");
            logTool.DebugLog("Output CV Init Complete");
            pMode.SetInfoMsg("Output CV Init Complete");
            runstate = RunState.Start;
            //GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.InService, "LoadPortTransferStateOutOfServiceInService1");
            GemCtrl.SetLoadPortAccessMode02(LoadPortAccessMode.Auto, "LoadPortAccessModeStateAuto2");
            GemCtrl.SetLoadPortTxferState02(LoadPortTxferState.InService, "LoadPortTransferStateOutOfServiceInService2");
            GemCtrl.SetLoadPortTxferState02(LoadPortTxferState.TransferBlocked, "LoadPortTransferStateReadyToUnloadTransferBlocked2");
            GemCtrl.SetLoadPortAssociateState02(LoadPortAssocState.NotAssociated, "loadPortAssociationStateAssociatedNotAssociated2");
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
                    runstate = StartFn();
                    break;
                case RunState.ReqTrayIn:
                    runstate = ReqTrayInFn();
                    break;
                case RunState.WaitForTrayArrival:
                    runstate = WaitForTrayArrivalFn();
                    break;
                case RunState.Judgement:
                    runstate = JudgementFn();
                    break;
                case RunState.HSCvr:
                    runstate = HSCvrFn();
                    break;
                case RunState.RunCvrOut:
                    runstate = RunCvrOutFn();
                    break;
                case RunState.HSOHT:
                    runstate = HSOTHFn();
                    break;
                case RunState.RunOHTOut:
                    runstate = RunOHTOutFn();
                    break;
                case RunState.WaitDoorOpen:
                    runstate = WaitDoorOpenFn();
                    break;
                case RunState.ManulOut:
                    runstate = ManualOutFn();
                    break;
                case RunState.WaitOPBtn:
                    runstate = WaitOPBtnFn();
                    break;
                case RunState.EndofCycle:
                    runstate = EndofCycleFn();
                    break;
            }
            return true;
        }

        private RunState StartFn()
        {
            CVStop();
            iCnt = 0;
            sw.Stop();
            sw.Reset();
            if (!isPresentSensor)
                isPartMM = false;
            if (isPartPresent)
                return RunState.Judgement;
            if (isPartAbsent)
            {
                TrigExtendBWD();
                if (isExtendBWD)
                    TrigTurretRemote();
                return RunState.ReqTrayIn;
            }
            return runstate;
        }
        private RunState ReqTrayInFn()
        {
            if (isTurretAtRm)
            {
                if (!isLifterAtDn)
                    TrigLifterDown();
                pMode.SetInfoMsg("Output CV Transfer Tray in");
                logTool.InfoLog("Output CV Transfer Tray in");
                WaitEvtOnInfinite(evtOPStackerRequestOPCVRun);

                evtOPStackerRequestOPCVRun.Reset();
                if (!isGateClosed)
                    TrigTailGateClose();
                if (!isExtendFWD)
                    TrigExtendFWD();
                if (isExtendFWD && isGateClosed)
                {
                    if (isPartPresent)
                    {
                        pMode.SetError("OutputCV tray present sensor error", true);
                        logTool.ErrorLog("OutputCV tray present sensor error");
                    }
                    CVMove(true, false);
                    //check if tray at output stacker
                    //if (isPartPresent) { pMode.SetInfoMsg("Tray At Ouput Conveyor"); }
                    //while (isPartPresent) { pMode.ChkProcessMode(); Thread.Sleep(100); }

                    evtOPStackerRequestOPCVRunAck.Set();
                    return RunState.WaitForTrayArrival;
                }
            }
            return runstate;
        }
        private RunState WaitForTrayArrivalFn()
        {
            int deboucing = 0;
            int timeout = iCntLimt;
            int allcleardebouncing = 0;

            while (true)
            {
                Thread.Sleep(100);
                timeout--;
                if (timeout < 0)
                {
                    pMode.SetError("Tray Arrive at Output Conveyor Time Out", true);
                    logTool.ErrorLog("Tray Arrive at Output Conveyor Time Out");
                    pMode.ChkProcessMode();
                }
                pMode.ChkProcessMode();
                if (isClearSensor)
                    isPartMM = true;
                if (!isClearSensor && isInposSensor)
                    CVMove(true, true);

                if (isPartAbsent)
                {
                    deboucing++;
                    if (deboucing > 320)
                    {
                        //pMode.SetError("Tray Arrive at Output Conveyor Time Out", true); pMode.ChkProcessMode();
                        //break;
                    }
                }
                else
                    deboucing = 0;
                //*
                if (isPartPresent)//conveyor all clear
                {

                    allcleardebouncing++;
                    if (allcleardebouncing > 15)
                    {
                        CVStop();
                        pMode.SetInfoMsg("Detect Output Coveyor Clear of Tray, Sequence completed");
                        logTool.DebugLog("Detect Output Coveyor Clear of Tray, Sequence completed");
                        break;
                    }
                }
                else
                    allcleardebouncing = 0;
                //*/
            }
            CVStop();
            if (isInposSensor|| !isPartPresent||mainapp.OPStkr.isStopSensorOn)
            {
                pMode.SetError("OutputCV Tray in wrong position ", true);
                logTool.DebugLog("OutputCV Tray in wrong position ");
            }
            TrigExtendBWD();
            DataTransfer();
            evtOPCVRunComplete.Set();
            return RunState.Judgement;
        }
        bool runoncehack = false;
        private RunState JudgementFn()
        {
            iCnt = 0;
            sw.Stop();
            sw.Reset();
            if (eOPMode == EOPMode.Conveyor)
                return RunState.HSCvr;
            else if (eOPMode == EOPMode.OHT)
            {
                runoncehack = true;
                return RunState.HSOHT;
            }
            else if (eOPMode == EOPMode.Manual)
                return RunState.WaitDoorOpen;
            return runstate;
        }
        private RunState HSCvrFn()
        {
            isHSCvrDone = false;
            iCnt = 0;
            sw.Stop();
            sw.Reset();
            if (!isExtendBWD)
                TrigExtendBWD();
            if (isExtendBWD)
            {
                if (!isTurretAtHm)
                    TrigTurretHome();
                if (isTurretAtHm && isPartPresent)
                {
                    if (!isGateOpened)
                        TrigTailGateOpen();

                    if (!isCtrAlive)
                        TrigCtrAlive(true);
                    if (!isCtrReady)
                        TrigCtrReady(true);
                    if (isCtrAlive && isCtrReady && isSFAAlive && isSFAReady && isGateOpened)
                    {
                        CVMove(true, false);
                        return RunState.RunCvrOut;
                    }
                }
            }
            return runstate;
        }
        private RunState RunCvrOutFn()
        {
            if (isPresentSensor || !isClearSensor)
            {
                if (!sw.IsRunning)
                {
                    sw.Reset();
                    sw.Start();
                }
                if (sw.ElapsedMilliseconds > iTimeOut)
                {
                    pMode.SetError("Tray Jam between Counter and SFA Conveyor!", true);
                    logTool.DebugLog("Tray Jam between Counter and SFA Conveyor!");
                    //sw.Stop();
                }

            }
            if (!isPresentSensor && !isInposSensor)
                isPartMM = false;
            if (isPartAbsent && !isInposSensor && isClearSensor)
            {
                CVStop();

                if (isSFADone && !isHSCvrDone)
                {
                    isHSCvrDone = true;
                    TrigCtrDone(true);
                }

                if (iCnt < iCntLimt)
                {
                    iCnt++;
                    if (isHSCvrDone && !isSFAReady && !isSFADone)
                    {
                        TrigCtrDone(false);
                        TrigCtrReady(false);
                        //TrigCtrAlive(false);       
                        if (!isCtrReady && !isCtrDone)
                            return RunState.EndofCycle;
                    }

                }
                else
                {
                    pMode.SetError("SFA Conveyor no complete signal!", true);
                    logTool.ErrorLog("SFA Conveyor no complete signal!");
                }


            }

            return runstate;
        }
        private void RunTxferStateEngineForUnload()
        {
            //set carrier id to port
            string carrierid = "";
            bool rst = GlobalVar.carrierids.TryTake(out carrierid);
            if (!rst)
            { log.Error("no Carrier ID in FIFO buffer"); };//will need to throw an error 
            log.Info("Carrier ID : " + carrierid);
            logTool.InfoLog("Carrier ID : " + carrierid);
            GemCtrl.UpdateSV("Port2CarrierID", carrierid);
            GemCtrl.SetLoadPortAssociateState02(LoadPortAssocState.Associated, "LoadPortAssociationStateNotAssociatedAssociated2");
            GemCtrl.SetLoadPortTxferState02(LoadPortTxferState.TransferReady, "LoadPortTransferStateInServiceTransferReady2");//LoadPortTransferStateInServiceTransferReady2
            GemCtrl.SetLoadPortTxferState02(LoadPortTxferState.ReadyToUnload, "LoadPortTransferStateTransferReadyReadyToUnload2");//will only happen when is a part?

            //associate

        }
        private void RunTxferStateEngineUnloadComplete()
        {
            GemCtrl.SetLoadPortTxferState02(LoadPortTxferState.TransferBlocked, "LoadPortTransferStateReadyToUnloadTransferBlocked2");
            GemCtrl.SetLoadPortAssociateState02(LoadPortAssocState.NotAssociated, "loadPortAssociationStateAssociatedNotAssociated2");
        }
        private RunState HSOTHFn()
        {
            iCnt = 0;
            if (!isExtendBWD)
                TrigExtendBWD();
            if (isExtendBWD)
            {
                if (!isTurretAtHm)
                    TrigTurretHome();
                if (!isLifterAtUp)
                    TrigLifterUp();
            }
            if (runoncehack)
            {
                RunTxferStateEngineForUnload();
                runoncehack = false;
            }

            if (isExtendBWD && isTurretAtHm && isLifterAtUp)
            {
                if (isOHTSelect)
                {
                    if (isOHTAlive)
                    {
                        TrigOHTUReq(true);
                        if (isOHTRequest)
                            TrigOHTRdy(true);
                        if (isOHTBusy)
                        {
                            //isPartMM = fa
                            runoncehack = true;
                            return RunState.RunOHTOut;
                        }
                    }
                }
            }

            return runstate;
        }
        private RunState RunOHTOutFn()
        {
            if (isOHTDetect)
            {
                isPartMM = false;
                TrigOHTUReq(false);
            }
            if (iCnt < iCntLimt)
                iCnt += 1;
            else
            {
                pMode.SetError("Tray Jam between Counter and SFA Conveyor!", true);
                logTool.ErrorLog("Tray Jam between Counter and SFA Conveyor!");
            }


            if (!isOHTBusy && isOHTComplete)
                TrigOHTRdy(false);
            if (!isPresentSensor && !isOHTBusy && !isOHTSelect && !isOHTAlive && !isOHTComplete && !dOOHT4Rdy.Logic)
            {
                if (isLifterAtUp)
                    TrigLifterDown();
                if (isLifterAtDn)
                {
                    RunTxferStateEngineUnloadComplete();
                    return RunState.EndofCycle;
                }

            }
            return runstate;
        }
        private RunState WaitDoorOpenFn()
        {
            TrigLedGrn(true);
            if (!isDoorOpened)
            {
                if (isCurtainTrig)
                {
                    sw.Stop();
                    sw.Reset();
                    if (doDoorOpen.Logic)
                    {
                        pMode.SetInfoMsg("Curtain Sensor Blocked while output shutter door Opening!");
                        logTool.InfoLog("Curtain Sensor Blocked while output shutter door Opening!");
                    }
                    TrigDoorStop();
                }
                else if (!isFSWOn)
                {
                    sw.Stop();
                    sw.Reset();
                    if (doDoorOpen.Logic)
                    {
                        pMode.SetInfoMsg("Two Press Button released while output shutter door Opening!");
                        logTool.InfoLog("Two Press Button released while output shutter door Opening!");
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
                                pMode.SetError("Output Shutter Door Open Time out", true, doDoorOpen.DisplayName);
                                logTool.ErrorLog("Output Shutter Door Open Time out");
                            }
                        }
                        else
                            sw.Start();
                    }
                }
            }
            if (isDoorOpened)
                return RunState.ManulOut;
            return runstate;
        }
        private RunState ManualOutFn()
        {
            TrigLedBlinkGrn(5);
            if (!isPresentSensor && !isInposSensor)
            {
                isPartMM = false;
                return RunState.WaitOPBtn;
            }
            return runstate;
        }
        private RunState WaitOPBtnFn()
        {
            if (isPartPresent)
            {
                TrigLedBlinkGrn(5);
                return RunState.ManulOut;
            }

            if (isPartAbsent && !isInposSensor)
            {
                if (!isDoorClosed)
                {
                    if (isCurtainTrig)
                    {
                        sw.Stop();
                        sw.Reset();
                        if (doDoorClose.Logic)
                        {
                            pMode.SetInfoMsg("Curtain Sensor Blocked while Output shutter door Closing!");
                            logTool.InfoLog("Curtain Sensor Blocked while Output shutter door Closing!");
                        }

                        TrigDoorStop();
                    }
                    else if (!isFSWOn)
                    {
                        sw.Stop();
                        sw.Reset();
                        if (doDoorClose.Logic)
                        {
                            pMode.SetInfoMsg("Two Press Button released while Output shutter door Closing!");
                            logTool.InfoLog("Two Press Button released while Output shutter door Closing!");
                        }

                        TrigDoorStop();
                    }
                    else
                    {
                        if (doDoorClose.Logic == false)
                            TrigDoorClose();
                        else
                        {
                            if (sw.IsRunning)
                            {
                                if (sw.ElapsedMilliseconds > iTimeoutClos)
                                {
                                    sw.Stop();
                                    pMode.SetError("Output Shutter Door Close Time out", true, doDoorClose.DisplayName);
                                    logTool.ErrorLog("Output Shutter Door Close Time out");
                                }
                            }
                            else
                                sw.Start();
                        }
                    }
                }
                if (isDoorClosed)
                    return RunState.EndofCycle;
            }
            return runstate;
        }
        private RunState EndofCycleFn()
        {
            sw.Stop();
            TrigLedGrn(false);
            TrigLedRed(false);
            return RunState.Start;
        }

        protected override void StoppingLogicFn()
        {
            //set all motor current state
            bM4Fwdbackup = MxFwd.Logic;

            //set all motor to stop
            CVStop();
        }
        protected override void RecoverFromStopFn()
        {
            base.RecoverFromStopFn();
            MxFwd.Logic = bM4Fwdbackup;
        }

        private RunState runstate;
        private enum RunState
        {
            Start, ReqTrayIn, WaitForTrayArrival, Judgement,                            //Tray coming in from Output Stacker
            HSCvr, RunCvrOut, HSOHT, RunOHTOut, WaitDoorOpen, ManulOut, WaitOPBtn,                    //Tray out to conveyor, OHT or Manual take away
            TrayClear, EndofCycle                                                       //Check tray clear and end of cycle
        }
        #endregion

        #region // IO and Event Configuration

        /// <summary>
        /// Discrete Input
        /// </summary>
        private Devices.IOModule.DiscreteIO TrayPresentSensor, dIPosSensor, dIClearSensor, dIFootSW, dIHandSensor, dITouchButton1, dITouchButton2, dICurtain1, dICurtain2, dICurtainErr;

        /// <summary>
        /// Discrete Output
        /// </summary>
        private Devices.IOModule.DiscreteIO MxFwd, MxFwdSlow, MxBwd, PBLedGrn, PBLedRed, dOPB1Led, dOPB2Led, dOCurtainRst;

        /// <summary>
        /// Conveyor tray stack transfer smema
        /// </summary>
        private Devices.IOModule.DiscreteIO dISFAValid, dISFAReady, dISFADone;
        private Devices.IOModule.DiscreteIO dOCtrValid, dOCtrReady, dOCtrDone;

        /// <summary>
        /// OHT is active, and counter is passive
        /// </summary>
        private Devices.IOModule.DiscreteIO dIOHT1Valid, dIOHT2CS0, dIOHT3CS1, dIOHT4Avbl, dIOHT5Req, dIOHT6Busy, dIOHT7Done, dIOHT8Cntnu;
        private Devices.IOModule.DiscreteIO dOOHT1LReq, dOOHT2UReq, dOOHT3VA, dOOHT4Rdy, dOOHT5VS0, dOOHT6VS1, dOOHT7Avbl, dOOHT8ESTP;

        /// <summary>
        /// Digital IO for cylinder
        /// </summary>
        private Devices.IOModule.DiscreteIO diDoorOpen, diDoorClose, diTurretHm, diTurretRm, diLifterDn, diLifterUp, diExtendRtn, diExtendExt, diGateOpen, diGateClose;
        private Devices.IOModule.DiscreteIO doDoorOpen, doDoorClose, doTurretHm, doTurretRm, doLifterDn, doLifterUp, doExtendRtn, doExtendExt, doGateOpen, doGateClose;

        private ManualResetEvent evtOPStackerRequestOPCVRun, evtOPStackerRequestOPCVRunAck, evtOPCVRunComplete;

        protected override void InitEvt()       //initialize events
        {
            base.InitEvt();

            Dictionary<string, ProcessEvt> evtdict = new Dictionary<string, ProcessEvt>();
            foreach (ProcessEvt e in Evtlist)
            {
                evtdict.Add(e.Name, e);
            }
            //<string>evtOPStackerRequestOPCVRun</string>
            //<string>evtOPStackerRequestOPCVRunAck</string>
            //<string>evtOPCVRunComplete</string>
            evtOPStackerRequestOPCVRun = evtdict[EvtNameList[0]].evt;
            evtOPStackerRequestOPCVRunAck = evtdict[EvtNameList[1]].evt;
            evtOPCVRunComplete = evtdict[EvtNameList[2]].evt;

        }
        protected override void InitInput()     //initialize input
        {
            base.InitInput();

            //<string>Input73</string>	<!--Tray At OP C/V Inpos Sensor-->
            //<string>Input74</string>	<!--Ready-->
            //<string>Input75</string>	<!--Run-->	
            //<string>Input76</string>	<!--Part Inpos-->			
            //<string>Input77</string>	<!--Send Part-->

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
            dIPosSensor = this.inputlist.IpDirectory[InputNameList[13]];
            //dIHandSensor = this.inputlist.IpDirectory[InputNameList[15]];

            diTurretHm = this.inputlist.IpDirectory[InputNameList[15]];
            diTurretRm = this.inputlist.IpDirectory[InputNameList[14]];
            diExtendRtn = this.inputlist.IpDirectory[InputNameList[17]];
            diExtendExt = this.inputlist.IpDirectory[InputNameList[16]];
            diLifterUp = this.inputlist.IpDirectory[InputNameList[18]];
            diLifterDn = this.inputlist.IpDirectory[InputNameList[19]];
            diGateOpen = this.inputlist.IpDirectory[InputNameList[20]];
            diGateClose = this.inputlist.IpDirectory[InputNameList[21]];
            if (isOwnDoor)
            {
                diDoorClose = this.inputlist.IpDirectory[InputNameList[22]];
                diDoorOpen = this.inputlist.IpDirectory[InputNameList[23]];
                dITouchButton1 = this.inputlist.IpDirectory[InputNameList[24]];
                dITouchButton2 = this.inputlist.IpDirectory[InputNameList[25]];
                dICurtain1 = this.inputlist.IpDirectory[InputNameList[26]];
                dICurtain2 = this.inputlist.IpDirectory[InputNameList[27]];
                dICurtainErr = this.inputlist.IpDirectory[InputNameList[28]];
            }
            isDIOK = true;
        }
        protected override void InitOutput()    //initialize output
        {
            base.InitOutput();

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
            doGateOpen = this.outputlist.IpDirectory[OutputNameList[22]];
            doGateClose = this.outputlist.IpDirectory[OutputNameList[23]];
            if (isOwnDoor)
            {
                doDoorClose = this.outputlist.IpDirectory[OutputNameList[24]];
                doDoorOpen = this.outputlist.IpDirectory[OutputNameList[25]];
                dOPB1Led = this.outputlist.IpDirectory[OutputNameList[26]];
                dOPB2Led = this.outputlist.IpDirectory[OutputNameList[27]];
                dOCurtainRst = this.outputlist.IpDirectory[OutputNameList[28]];
            }
            isDOOK = true;
        }
        #endregion

        #region // Common Method and function
        private bool CheckOHTDetect()
        {
            return !isPresentSensor;
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
        private bool CheckInposSensor()
        {
            return !dIPosSensor.Logic;
        }
        private bool CheckPartClear()
        {
            return dIClearSensor.Logic;
        }
        private bool CheckCurtainSensor()
        {
            //return true;
            return dICurtain1.Logic & dICurtain2.Logic;
        }
        private void CVMove(bool fwd, bool slow)
        {
            if (fwd)
            {
                if (slow)
                {
                    MxBwd.Logic = false;
                    MxFwd.Logic = false;
                    MxFwdSlow.Logic = true;
                }
                else
                {
                    MxBwd.Logic = false;
                    MxFwd.Logic = true;
                    MxFwdSlow.Logic = false;
                }
            }
            else
            {
                MxBwd.Logic = true;
                MxFwd.Logic = false;
                MxFwdSlow.Logic = false;
            }
        }
        private void CVStop()
        {
            MxBwd.Logic = false;
            MxFwd.Logic = false;
            MxFwdSlow.Logic = false;
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
            if (eOPMode == EOPMode.Conveyor)
            {
                TrigCtrAlive(true);
                TrigOHTAvbl(false);
                TrigOHTESTP(false);
            }
            else if (eOPMode == EOPMode.OHT)
            {
                TrigCtrAlive(false);
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
            TrigOHTLReq(false);
            TrigOHTRdy(false);
        }
        private void DataTransfer()
        {
            RunTimeData.kvpOPCvr.Clear();
            if (RunTimeData.kvpOPStkr.Count > 0)
            {
                foreach (KeyValuePair<string, bool> kvp in RunTimeData.kvpOPStkr)
                {
                    RunTimeData.kvpOPCvr.Add(kvp.Key, kvp.Value);
                }
            }
            RunTimeData.kvpOPStkr.Clear();
        }
        private bool CheckResult()
        {
            if (!GlobalVar.isBypassFail && RunTimeData.kvpOPCvr.Count > 0)
                return false;
            else
                return true;
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
        public bool isGateOpened { get { return CheckTailGateOpen(); } }
        public bool isGateClosed { get { return CheckTailGateClose(); } }
        private bool isTurretSafe { get { return CheckTurretSafe(); } }
        private int iTimeoutOpen { get { return GetDoorOpenTimeout(); } }
        private int iTimeoutClos { get { return GetDoorCloseTimeout(); } }

        private string sCylDoor = "OutputShutterDoor";
        private string sCylTurret = "OPCVRotator";
        private string sCylExtend = "OPCVExtension";
        private string sCylLifter = "OPCVLifter";
        private string sCylGate = "OPCVTailGate";

        [XmlIgnore]
        public main.MainApp mainapp;
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
                return doDoorOpen.Logic & diDoorOpen.Logic;
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
            return diTurretRm.Logic & doTurretRm.Logic;
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
        private bool CheckTailGateOpen()
        {
            //return valvelist[sCylGate].WaitRetract();
            return doGateOpen.Logic & diGateOpen.Logic;
        }
        private bool CheckTailGateClose()
        {
            //return valvelist[sCylGate].WaitExtend();
            return doGateClose.Logic & diGateClose.Logic;
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
                valvelist[sCylTurret].Retract();    // Tray exit position - link SFA Conveyor
                valvelist[sCylTurret].WaitRetract();
            }

        }
        private void TrigTurretRemote()
        {
            if (isTurretSafe)
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
        private void TrigTailGateOpen()
        {
            valvelist[sCylGate].Extend();
            valvelist[sCylGate].WaitExtend();

        }
        private void TrigTailGateClose()
        {
            valvelist[sCylGate].Retract();
            valvelist[sCylGate].WaitRetract();
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

            if (!doGateClose.Logic &&!doGateOpen.Logic)
            {
                if (diGateClose.Logic)
                    TrigTailGateClose();
                else
                    TrigTailGateOpen();
            }
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
        private bool isCVRunIn { get { return MxFwd.Logic; } }
        private bool isCVStopped { get { return !MxFwd.Logic; } }

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
        private bool isOHTAlive { get { return dIOHT1Valid.Logic; } }
        private bool isOHTSelect { get { return dIOHT2CS0.Logic | dIOHT3CS1.Logic; } }
        private bool isOHTAvailable { get { return dIOHT4Avbl.Logic; } }
        private bool isOHTRequest { get { return dIOHT5Req.Logic; } }
        private bool isOHTBusy { get { return dIOHT6Busy.Logic; } }
        private bool isOHTComplete { get { return dIOHT7Done.Logic; } }
        private bool isOHTContinue { get { return dIOHT8Cntnu.Logic; } }
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
            dOOHT3VA.Logic = isTorF;     // VA - Vehicle Arrived not used for counter
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

    }
    //*/
}