using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using Dct.Models;
using Dct.Models.Repository;
using Insphere.Connectivity.Application.SecsToHost;
using LogPanel;
using Sopdu.Devices.Cylinders;
using Sopdu.Devices.IOModule;
using Sopdu.Devices.MotionControl.Base;
using Sopdu.Devices.SecsGem;
using Sopdu.Devices.Vision;
using Sopdu.helper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;


namespace Sopdu.ProcessApps.ProcessModules
{
    public class InputStacker : Process
    {
        LogTool<InputStacker> logTool = new LogTool<InputStacker>();
        private static string _currentRecipe;

        public static string CurrentRecipe
        {
            get { return _currentRecipe; }
            set { _currentRecipe = value; }
        }
        FingerEngagementRepository _fingerEngagementRepository;
        bool EnablePreEngageCheck = false;
        public InputStacker()
        {

            _fingerEngagementRepository = DbManager.Instance.GetRepository<FingerEngagementRepository>();
            ShutterUnit.CurrentShutterName += ReceiveCurrentShutterName;
            EquipmentState = new GenericState();
            EquipmentState.SetState(MachineState.NotInit);
            initstate = InitState.NotInit;
            runstate = RunState.Start;

            EnablePreEngageCheck = ConfigurationManager.AppSettings["EnablePreEngageCheck"] == "True";
        }

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region // Initial Run States
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
            //resetting all events
            evtIPStackerUnloadComplete.Reset();
            evtInit_InputStackerHomeComplete.Reset();
            evtIPStackerRdyUnload.Reset();
            evtReqIPStackerToUnload.Reset();
            posdictionary = new Dictionary<string, AxisPosition>();
            posdictionary1 = posdictionary;
            //load positions
            foreach (AxisPosition pos in AxisList[0].MotorAxis.PositionList)
                posdictionary.Add(pos.Name, pos);



            valvelist["BenchingBar"].Extend();


            valvelist["TrayLifterPlate"].Retract();
            valvelist["TrayLifterPlate"].WaitRetract();

            // 允许 一个shutter开始初始化, 如果 MP1 在Conveyor 位置， 并且 TrayLifterPlate 伸出状态， 同时 shutter 在Prep Pos位置 那么shutter 初始化时，会发生碰撞， 确保TrayLifterPlate 是缩回的即可
            mainapp.shutterInitStartQuene.Enqueue("");

            TrigShutDoor(true);
            //valvelist["Input Shutter Door"].Retract();
            //valvelist["Input Shutter Door"].WaitRetract();
            log.Debug("enter check");
            logTool.DebugLog("enter check");
            if (inputlist.IpDirectory["Input20"].evtOff.WaitOne(100))
            {
                pMode.SetError("Init Error, Tray At Input Stacker", false, "ER_IPST_E01");
                GemCtrl.SetAlarm("ER_IPST_E01");
                log.Debug("error set");
                logTool.DebugLog("error set");
                this.pMode.ChkProcessMode();
                return false;
            }
            MoveAxis("Conveyor", 1000);
            evtInit_InputStackerHomeComplete.Set();
            evtIPStackerRdyUnload.Reset();
            endseq = false;
            runstate = RunState.Start;

            return true;
        }

        private enum InitState { NotInit, ReverseTraylogic, GoInitPosition, InitComplete }
        private InitState initstate;
        #endregion

        #region // Auto Run States

        public override bool RunFunction()
        {
            switch (runstate)
            {
                case RunState.Start:
                    runstate = StartFn();
                    break;

                

                case RunState.TrayWaitPositionAfterMap:
                    runstate = TrayWaitPositionAfterMapFn();
                    break;

                case RunState.TrayWaitPosition:
                    runstate = TrayWaitPositionFn();
                    break;

                case RunState.WaitForShutter:
                    runstate = WaitForShutterFn();
                    break;

                case RunState.ToPreEngagePos:
                    runstate = ToPreEngagePosFn();
                    break;

                case RunState.ToEngagePos:
                    runstate = ToEngagePosFn();
                    break;

                case RunState.ToConveyorPosition:
                    runstate = ToConveyorPositionFn();
                    break;

                case RunState.WaitShutterAtReadyCondition:
                    runstate = WaitShutterAtReadyConditionFn();
                    break;

                case RunState.WaitForIPClearSensorOn:
                    runstate = WaitForIPClearSensorOnFn();
                    break;

                case RunState.BenchingSequence:
                    runstate = BenchingSequenceFn();
                    break;
                //case RunState.ReverseTrayCheck:
                //    runstate = ReverseTrayCheckFn();
                //    break;
                case RunState.ReverseTraySequence01:
                    runstate = ReverseTraySequence01Fn();
                    break;

                case RunState.ReverseTraySequence02:
                    runstate = ReverseTraySequence02Fn();
                    break;

                case RunState.ReverseTraySequence03:
                    runstate = ReverseTraySequence03Fn();
                    break;

                case RunState.ReverseTraySequence04:
                    runstate = ReverseTraySequence04Fn();
                    break;
            }

            return true;
        }

        private RunState ReverseTraySequence04Fn()
        {
            //WaitEvtOn(50000, CVInputClear.evtOff, "Tray Detected By Input Clear Sensor", "ER_IPST_E02");
            WaitEvtOn(50000, CVInputClear.evtOn, "Tray Detected By Input Clear Sensor", "ER_IPST_E02");
            pMode.SetInfoMsg("Tray detected by Input Clear Sensor");
            logTool.DebugLog("Tray detected by Input Clear Sensor");
            //have jittering issue
            //assume each loop is 100ms
            int loop = 0;
            int timeoutloop = 0;
            while (true)
            {
                timeoutloop++;
                if (!CVInputClear.Logic)
                {
                    Thread.Sleep(100);
                    loop++;
                    if (loop > 12) break;
                }
                else
                {
                    Thread.Sleep(100);
                    loop = 0;
                }
                if (timeoutloop > 100) { pMode.SetError("Time Out Error For CVInputClear Sensor", true, "ER_IPST_E03"); GemCtrl.SetAlarm("ER_IPST_E03"); pMode.ChkProcessMode(); }
                pMode.ChkProcessMode();
            }

            pMode.SetInfoMsg("Tray Clear Input Clear Sensor");
            M1Bwd.Logic = false;
            return RunState.Start;
        }
        private RunState ReverseTraySequence03Fn()
        {
            ManualResetEvent[] tmpevtlist = new ManualResetEvent[3];
            tmpevtlist[0] = CV_Slow.evtOn;
            tmpevtlist[1] = CV_Stop.evtOn;
            //tmpevtlist[2] = CVInputClear.evtOn;
            WaitEvtOn(10000, CV_Slow.evtOn, "Slow Clear", "ER_IPST_E06");
            //WaitEvtOn(10000, CV_Stop.evtOn, "Stop Clear", "ER_IPST_E07");
            return RunState.ReverseTraySequence04;
        }
        private RunState ReverseTraySequence02Fn()
        {
            evtRevCVRequest.Set();
            WaitEvtOn(1000, evtRevCVRequestAck, "Request IP CV Reverse", "ER_IPST_E08");
            evtRevCVRequestAck.Reset();
            CVMove(false, false);
            //M1Bwd.Logic = true;
            return RunState.ReverseTraySequence03;
        }
        private RunState ReverseTraySequence01Fn()
        {
            //add shutter door open
            //valvelist["Input Shutter Door"].Retract();
            valvelist["TrayLifterPlate"].Retract();
            TrigShutDoor(true);
            valvelist["TrayLifterPlate"].WaitRetract();
            //valvelist["Input Shutter Door"].WaitRetract();
            MoveAxis("Conveyor", 1000);
            Thread.Sleep(100);
            valvelist["BenchingBar"].Extend();
            valvelist["BenchingBar"].WaitExtend();
            return RunState.ReverseTraySequence02;
        }
        private RunState ReverseTrayCheckFn()
        {
            if (isReverse)
                return RunState.ReverseTraySequence01;
            else
            {
                evtIPStackerAllowBuffer.Set();
                log.Debug("2-D Vision Fail Code:" + iChkStatus.ToString());
                logTool.DebugLog("2-D Vision Fail Code:" + iChkStatus.ToString());
                pMode.SetError("Pitch detection failed, unable to get index position", true);
            }
            return runstate;
        }

        private RunState BenchingSequenceFn()
        {
            AxisPosition pos = new AxisPosition()
            {
                AccTime = posdictionary["Conveyor"].AccTime,
                Coordinate = posdictionary["Conveyor"].Coordinate - 16950,
                DecTime = posdictionary["Conveyor"].DecTime,
                InPositionRange = posdictionary["Conveyor"].InPositionRange,
                IsRelativePosition = false,
                MaxVelocity = 5000,
                StartVelocity = posdictionary["Conveyor"].StartVelocity,
                Name = "TrayVariablePos"
            };
            if ((!CV_Slow.Logic))//need to check cv of input cv also
            {
                valvelist["BenchingBar"].Retract();
                valvelist["BenchingBar"].WaitRetract();
                Thread.Sleep(500);
                valvelist["BenchingBar"].Extend();
                valvelist["BenchingBar"].WaitExtend();
                // MoveAxis(pos, 1000);
                MoveAxis("Conveyor", 1000);
                Thread.Sleep(500);

                pMode.SetInfoMsg("Benching Sequence Complete");
                //shutter door close
                //WaitEvtOn(10000, CVInputClear.evtOn, "IPStacker CV Clear Off", "ER_IPST_E10");
                WaitEvtOn(10000, CVInputClear.evtOff, "IPStacker CV Clear Off", "ER_IPST_E10");
                pMode.SetInfoMsg("IPStacker CV Clear Sensor Off");
                TrigShutDoor(false);
                //valvelist["Input Shutter Door"].Extend();
                //valvelist["Input Shutter Door"].WaitExtend();//change this to a sensor check? to have finer check

                while (!isShutDoorClosed)
                {
                    pMode.ChkProcessMode();
                    if (CVInputClear.Logic == true)
                    {
                        pMode.SetInfoMsg("IPStacker CV Clear Sensor is on, shutter door retract");
                        //valvelist["Input Shutter Door"].Retract();
                        //WaitEvtOnInfinite(CVInputClear.evtOn);
                        WaitEvtOnInfinite(CVInputClear.evtOff);
                        //valvelist["Input Shutter Door"].Extend();
                    }
                    Thread.Sleep(100);
                }
                return RunState.TrayWaitPositionAfterMap;
            }
            pMode.SetInfoMsg("Abnormal Sequence Stop");
            return RunState.BenchingSequence;
        }

        private RunState WaitForIPClearSensorOnFn()
        {
            //wait for input cv is ready

            //WaitEvtOn(10000, CVInputClear.evtOff, "IPStacker CV Clear On", "ER_IPST_E09");
            WaitEvtOn(10000, CVInputClear.evtOn, "IPStacker CV Clear On", "ER_IPST_E09");
            pMode.SetInfoMsg("IP Tray Activate IP Clear Sensor");
            logTool.InfoLog("IP Tray Activate IP Clear Sensor");
            //WaitEvtOn(10000, CVInputClear.evtOn, "IPStacker CV Clear Off", "ER_IPST_E10");
            WaitEvtOn(10000, CVInputClear.evtOff, "IPStacker CV Clear Off", "ER_IPST_E10");
            pMode.SetInfoMsg("IP Clear Sensor Off");
            logTool.InfoLog("IP Clear Sensor Off");
            WaitEvtOn(10000, CV_Slow.evtOff, "IPStacker Slow Sensor On", "ER_IPST_E11");
            pMode.SetInfoMsg("IPStacker Slow Sensor detected");
            logTool.InfoLog("IP Clear Sensor Off");
            CVMove(true, true);

            Thread.Sleep(100);
          
            CVStop();
            evtIPStackerAllowBuffer.Set();
            //M1FwdSlow.Logic = false;
            //M1Fwd.Logic = false;
            Thread.Sleep(100);
            evtStackerCVRunComplete.Set();
            pMode.SetInfoMsg("Tray Stack Benching Sequence Start");
            logTool.InfoLog("Tray Stack Benching Sequence Start");
            return RunState.BenchingSequence;
        }

        private RunState StartFn()  //seq01
        {
            LoadPortAssocState currentstate = (LoadPortAssocState)int.Parse(GemCtrl.GetCurrentSvValue("LoadPortAssociationState1"));
            if (currentstate == LoadPortAssocState.Associated)
                GemCtrl.SetLoadPortAssociateState01(LoadPortAssocState.NotAssociated, "loadPortAssociationStateAssociatedNotAssociated1");
            evtIPStackerAllowBuffer.Set();
            evtIPStackerRdyToLoad.Set();
            pMode.SetInfoMsg("Enter Start FN");
            logTool.InfoLog("Enter Start FN");
            WaitEvtOnInfinite(evtIPCVRequestStackerCVRun);
            GemCtrl.ClearDisplayMsg();
            //if ((!CV_Slow.Logic) || (!CV_Stop.Logic) || (!CVInputClear.Logic))
            if ((!CV_Slow.Logic) || (!CVInputClear.Logic))
            {
                pMode.SetError("Input Stacker Conveyor Not Cleared", true, "ER_IPST_E04"); GemCtrl.SetAlarm("ER_IPST_E04");
                pMode.ChkProcessMode();
            }
            evtIPStackerAllowBuffer.Reset();
            evtIPStackerRdyToLoad.Reset();
            evtIPCVRequestStackerCVRun.Reset();
            pMode.SetInfoMsg("Input Conveyor Request Stacker Conveyor Move");
            logTool.DebugLog("Input Conveyor Request Stacker Conveyor Move");
            //Conveyor run forward at fast speed
            //M1Fwd.Logic = true;
            CVMove(true, false);
            Thread.Sleep(100);
            evtIPCVRequestStackerCVRunAck.Set();
            return RunState.WaitForIPClearSensorOn;
        }

        //private RunState GotoPitchDetectionFn() //seq02
        //{
        //    valvelist["TrayLifterPlate"].Extend();
        //    valvelist["TrayLifterPlate"].WaitExtend();
        //    MoveAxis("LastTrayCheck", 1000);
        //    return RunState.PitchDetection;
        //}

        //private RunState PitchDetectionFn() //seq03
        //{
        //    Thread.Sleep(300);
        //    try
        //    {

        //        //关上反光镜，拍照
        //        valvelist["MirrorDoor"].Retract();
        //        valvelist["MirrorDoor"].WaitRetract();
        //        Thread.Sleep(1000);

        //        usbCamera.su_SetupSelectedEvent(usbCamera.Setups[1]);

        //        //Thread.Sleep(1000);
        //        ////拍照结束，打开反光镜
        //        //valvelist["MirrorDoor"].Extend();
        //        //valvelist["MirrorDoor"].WaitExtend();
        //    }
        //    catch (Exception ex)
        //    {
        //        pMode.SetInfoMsg("Exception on Vision on Tray Pitch Detection");
        //        logTool.InfoLog("Exception on Vision on Tray Pitch Detection");
        //        //shutter door open
        //        TrigShutDoor(true);
        //        //valvelist["Input Shutter Door"].Retract();
        //        //valvelist["Input Shutter Door"].WaitRetract();
        //        //end of shutter door open
        //        iChkStatus = 1;
        //        return RunState.ReverseTrayCheck;
        //    }
        //    return RunState.MoveToTrayMap;
        //}

        //private RunState MoveToTrayMapFn()  //seq 04
        //{
        //    MoveAxis("Tray Scan", 1000);
        //    Thread.Sleep(100);
        //    return RunState.TrayMapInpsection;
        //}

        //private RunState TrayMapInspectionFn()  //seq 05
        //{
        //    try
        //    {
        //        usbCamera.sCoverTrayPrefix = sCoverTrayPrefix;
        //        usbCamera.su_SetupSelectedEvent(usbCamera.Setups[0]);
        //        int numoftry = 5;
        //        if ((usbCamera.__position.Count > 30) || (usbCamera.__trayidlist.Count > 30))
        //        {
        //            pMode.SetError("Tray number over than 30 error", true);
        //        }
        //        while (usbCamera.__position.Count != usbCamera.__trayidlist.Count)
        //        {
        //            usbCamera.su_SetupSelectedEvent(usbCamera.Setups[0]);
        //            numoftry--;
        //            if (numoftry < 0) //throw new Exception("Label detect error");
        //            {
        //                pMode.SetError("Label detect error", true, "ER_IPST_E22");
        //                GemCtrl.SetAlarm("ER_IPST_E22");
        //                pMode.ChkProcessMode();
        //            }
        //        }

        //        Thread.Sleep(1000);
        //        //拍照结束，打开反光镜
        //        valvelist["MirrorDoor"].Extend();
        //        valvelist["MirrorDoor"].WaitExtend();

        //        double MaxPitch = 0;
        //        double lasttraypos = 0;
        //        if (usbCamera._IndexValue == 565)
        //        {
        //            pMode.SetInfoMsg("Pitch Detected is 565");
        //            logTool.InfoLog("Pitch Detected is 565");
        //            MaxPitch = usbCamera.T556_Set;
        //            lasttraypos = usbCamera.LastTrayPos_T556;
        //        }
        //        if (usbCamera._IndexValue == 635)
        //        {
        //            pMode.SetInfoMsg("Pitch Detected is 635");
        //            logTool.InfoLog("Pitch Detected is 635");
        //            MaxPitch = usbCamera.T635_Set;
        //            lasttraypos = usbCamera.LastTrayPos_T635;

        //        }
        //        if (usbCamera._IndexValue == 1005)
        //        {
        //            pMode.SetInfoMsg("Pitch Detected is 1005");
        //            logTool.InfoLog("Pitch Detected is 1005");
        //            MaxPitch = usbCamera.T1016_Set;
        //            lasttraypos = usbCamera.LastTrayPos_T1016;
        //        }

        //        //check for inverted
        //        //if (Double.Parse(usbCamera.__maxPitch) > MaxPitch)
        //        //{
        //        //    pMode.SetWarningMsg("Tray Wrong Orientation Max Pitch : " + usbCamera.__maxPitch);
        //        //    logTool.InfoLog("Tray Wrong Orientation Max Pitch : " + usbCamera.__maxPitch);
        //        //    //check if its all tpk trays
        //        //    bool iscarriertray = true;
        //        //    foreach (string str in usbCamera.__trayidlist)
        //        //    {
        //        //        if (!str.Contains(sCoverTrayPrefix)) iscarriertray = false;
        //        //    }
        //        //    if (!iscarriertray)
        //        //    {
        //        //        this.GemCtrl.SetAlarm("ER_IPST_E19");
        //        //        throw new Exception("Pitch Error");
        //        //    }
        //        //}
        //        string lastpos = usbCamera.__position.Last();
        //        if (Double.Parse(lastpos) < lasttraypos)
        //        {
        //            pMode.SetWarningMsg("Last Tray Wrong Orientation Pos value " + lastpos);
        //            logTool.WarnLog("Last Tray Wrong Orientation Pos value " + lastpos);
        //            throw new Exception("Last Tray Inverted");
        //        }

        //        //set # of Trays
        //        //do CEID 81 here
        //        TrayIDList = new List<string>();
        //        foreach (string str in usbCamera.__trayidlist)
        //        {
        //            TrayIDList.Add(str);
        //        }
        //        sendlist = new List<string>();
        //        //kk
        //        int intCoverTray = 0;
        //        foreach (string s in TrayIDList)
        //        {
        //            sendlist.Add(s);
        //            if (s.Contains(sCoverTrayPrefix))
        //            {
        //                intCoverTray++;
        //                log.Debug("CoverTaryNo =" + intCoverTray);
        //                logTool.DebugLog("CoverTaryNo =" + intCoverTray);
        //            }
        //        }
        //        if (intCoverTray >= 2)
        //        {
        //            log.Debug("CoverTrayMorethanOne=" + intCoverTray);
        //            logTool.DebugLog("CoverTrayMorethanOne=" + intCoverTray);
        //        }
        //        if (sendlist.Count < 3)
        //            throw new Exception("Less Than 3 Trays");
        //        sendlist.RemoveAt(0);
        //        carrierid = TrayIDList[0];
        //        GlobalVar.carrierids.Add(carrierid);
        //        string state = GemCtrl.GetCurrentSvValue("EquipmentState");
        //        GemCtrl.cmdS3F17evt.Reset();
        //        GemCtrl.SetCarrierStatus(CarrierIDStatus.IdNotRead, TrayIDList[0], sendlist, "CarrierIdStatusIdNotRead1");
        //        //CEID 62

        //        GemCtrl.SetCarrierStatus(CarrierIDStatus.WaitingForHost, TrayIDList[0], sendlist, "CarrierIdStatusWaitingForHost1");
        //        //wait for host to provide proceed with setup
        //        if ((state == "1") && (GemCtrl.gemController.CommunicationState != CommunicationState.Disabled))
        //        {
        //            log.Debug("Equipment state In production mode");
        //            logTool.DebugLog("Equipment state In production mode");
        //            // WaitEvtOn(60000, GemCtrl.cmdS3F17evt, "Wait For Host To Proceed With Carrier I", "ER_IPST_E13");
        //            if (!WaitEvtOnWithoutErrorFire(60000, GemCtrl.cmdS3F17evt, "ER_IPST_E13"))
        //                throw new Exception("Host Deny Carrier Process-1");
        //            log.Debug("Reply For S3F17 " + GemCtrl.strS3F17SendString);
        //            log.Debug("Reply For S3F17 " + GemCtrl.strS3F17SendString);
        //            if (GemCtrl.strS3F17SendString.Trim() != "ProceedWithCarrier")
        //            {
        //                log.Debug("Cannot Proceed With Carrier");
        //                logTool.DebugLog("Cannot Proceed With Carrier");
        //                //CarrierSlotMapStatusWaitingForHostSlotMapVerificationFailed1
        //                //CarrierSlotMapStatusWaitingForHostSlotMapVerificationOK1
        //                GemCtrl.SetCarrierStatus(CarrierIDStatus.IdVerificationFailed, TrayIDList[0], sendlist, "CarrierIdStatusIdVerificationFail1");
        //                throw new Exception("Host Deny Carrier Process :" + GemCtrl.strS3F17SendString.Trim());
        //            }
        //        }
        //        else
        //            log.Debug("Equipment state Not In production mode, not waiting for host verificaiton on CEID62");
        //        GemCtrl.cmdS3F17evt.Reset();
        //        logTool.DebugLog("Equipment state Not In production mode, not waiting for host verificaiton on CEID62");
        //        //get current status
        //        //CEID 67
        //        GemCtrl.SetLoadPortAssociateState01(LoadPortAssocState.Associated, "LoadPortAssociationStateNotAssociatedAssociated1");
        //        //CEID 81
        //        GemCtrl.SetCarrierStatus(CarrierIDStatus.IdVerificationOK, TrayIDList[0], sendlist, "CarrierIdStatusIdVerificationOK1");
        //        //CEID 82
        //        GemCtrl.SetSlotMapStatus(CarrierSlotMapStatus.SlotMapNotRead, "CarrierSlotMapStatusSlotMapNotRead1", TrayIDList[0], sendlist);
        //        //CEID 83
        //        GemCtrl.SetSlotMapStatus(CarrierSlotMapStatus.WaitingForHost, "CarrierSlotMapStatusSlotMapNotReadWaitingForHost1", TrayIDList[0], sendlist);
        //        //CEID 84
        //        GemCtrl.SetSlotMapStatus(CarrierSlotMapStatus.SlotMapVerificationOK, "CarrierSlotMapStatusWaitingForHostSlotMapVerificationOK1", TrayIDList[0], sendlist);
        //        //wait for host to provide proceed with setup
        //        if ((state == "1") && (GemCtrl.gemController.CommunicationState != CommunicationState.Disabled))
        //        {
        //            log.Debug("Equipment state In production mode");
        //            logTool.DebugLog("Equipment state In production mode");
        //            // WaitEvtOn(60000, GemCtrl.cmdS3F17evt, "Wait For Host To Proceed With Carrier","ER_IPST_E14");
        //            if (!WaitEvtOnWithoutErrorFire(60000, GemCtrl.cmdS3F17evt, "ER_IPST_E13"))
        //                throw new Exception("Host Deny Carrier Process-2");
        //            log.Debug("Reply For S3F17 " + GemCtrl.strS3F17SendString);
        //            logTool.DebugLog("Reply For S3F17 " + GemCtrl.strS3F17SendString);
        //            if (GemCtrl.strS3F17SendString.Trim() != "ProceedWithCarrier")
        //            {
        //                log.Debug("Cannot Proceed With Carrier");
        //                logTool.DebugLog("Cannot Proceed With Carrier");
        //                throw new Exception("Host Deny Carrier Process :" + GemCtrl.strS3F17SendString.Trim());
        //            }
        //        }
        //        else
        //            log.Debug("Equipment state Not In production mode, not waitingfor host verification on CEID83");
        //        //CEID606
        //        GemCtrl.CarrierReadStart(TrayIDList[0], 1, "CarrierReadToStart");
        //        logTool.DebugLog("Equipment state Not In production mode, not waitingfor host verification on CEID83");
        //        //wait for pp-select
        //        string currentrecipe = "On local";
        //        bool allcovertray = false;
        //        if (usbCamera.itpk == usbCamera.__position.Count)
        //        {
        //            allcovertray = true;
        //        }

        //        if ((GemCtrl.gemController.CommunicationState != CommunicationState.Disabled))
        //        {
        //            WaitEvtOn(600000, GemCtrl.cmdS2F41evt, "Wait For Host PP-SELECT", "ER_IPST_E15");
        //            //end of pp-select
        //            currentrecipe = GemCtrl.GetCurrentSvValue("PPExecName");
        //            sCurrentRecipe = currentrecipe;
        //            CurrentRecipe = sCurrentRecipe;
        //            log.Debug("PP Select Recipe Request : " + currentrecipe);
        //            log.Debug("Current Recipe : " + mainapp.menu.DefaultRecipe);
        //            logTool.DebugLog("Current Recipe : " + mainapp.menu.DefaultRecipe);
        //        }
        //        else
        //        {
        //            log.Debug("Equipment on local, no recipe set");
        //            log.Debug("Use Current Recipe");
        //            logTool.DebugLog("Use Current Recipe");
        //            currentrecipe = mainapp.menu.DefaultRecipe;
        //            sCurrentRecipe = currentrecipe;
        //            CurrentRecipe = sCurrentRecipe;
        //        }
        //        Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
        //        {
        //            if (currentrecipe != "SkipInspection")
        //                GemCtrl.gemPP_Recipename = currentrecipe;

        //        });
        //        //load recipe
        //        try
        //        {
        //            // enable auto recipe load by doing this...
        //            if ((mainapp.menu.DefaultRecipe != currentrecipe) && (currentrecipe != "SkipInspection"))//tmp remove
        //            {
        //                log.Debug("Recipe Load");
        //                logTool.DebugLog("Recipe Load");
        //                pMode.SetInfoMsg("Loading Recipe on PP Select Request");
        //                //mainapp.VisionLoad(currentrecipe);
        //                //mainapp.loadvisiontoprocessor();

        //                mainapp.ReadVisionFile(currentrecipe);
        //                mainapp.razor.Init(new helper.GenericEvents());
        //                mainapp.imgprocessor.Load3DVisionObject(mainapp.razor);
        //                usbCamera._IndexValue = mainapp.razor.rcpPitch;
        //                pMode.SetInfoMsg("Loading Recipe on PP Select Request Complete");
        //                log.Debug("Recipe Load successful");
        //                logTool.DebugLog("Recipe Load successful");
        //            }
        //            else
        //            {
        //                usbCamera._IndexValue = mainapp.razor.rcpPitch;
        //                if (currentrecipe != "SkipInspection")
        //                {
        //                    pMode.SetInfoMsg("Current Recipe Same as PP Select Request");
        //                    logTool.InfoLog("Current Recipe Same as PP Select Request");
        //                }
        //                else
        //                {
        //                    pMode.SetInfoMsg("PP Select Skip Inspection");
        //                    logTool.InfoLog("PP Select Skip Inspection");
        //                }
        //                // not full prove if non carrier tray skip inspection

        //            }
        //            if (intCoverTray >= 2)
        //            {
        //                string strCovertray1 = "covertray";
        //                log.Debug("CoverTrayOut" + intCoverTray);
        //                mainapp.ReadVisionFile(strCovertray1);
        //                mainapp.razor.Init(new helper.GenericEvents());
        //                mainapp.imgprocessor.Load3DVisionObject(mainapp.razor);
        //                usbCamera._IndexValue = mainapp.razor.rcpPitch;
        //                //posdictionary["Engage"].InPositionRange
        //            }
        //            //end testing
        //            GemCtrl.cmdS2F41evt.Reset();
        //            GemCtrl.strS2F42Reststring = "0";
        //            GemCtrl.cmdS2F42evt.Set();
        //        }
        //        catch (Exception ex)
        //        {
        //            GemCtrl.cmdS2F41evt.Reset();
        //            GemCtrl.cmdS2F42evt.Set();
        //            //pMode.SetError("PP SELECT Recipe Load Fail Recipe Name Requested : <" + currentrecipe + ">", true);
        //            //GemCtrl.SetAlarm("ER_IPST_E17");
        //            pMode.ChkProcessMode();
        //            log.Debug(ex.ToString());
        //            throw ex;

        //        }
        //        //end of recipe load
        //        //wait for remote start command
        //        //wait for pp-select
        //        if (GemCtrl.gemController.CommunicationState != CommunicationState.Disabled)
        //        {
        //            WaitEvtOn(10000, GemCtrl.cmdS2F41evt, "Wait For Host Remote Start Command", "ER_IPST_E16");
        //        }
        //        //end

        //        IndexList = new List<int>();
        //        CalibratedList = new List<int>();
        //        //get base value
        //        long StackBase = posdictionary["StackBase"].Coordinate;
        //        long Stack5mm = posdictionary["Stack5mm"].Coordinate;
        //        long Stack6mm = posdictionary["Stack6mm"].Coordinate;
        //        long Stack10mm = posdictionary["Stack10mm"].Coordinate;
        //        long stak562mm = posdictionary["Stack562mm"].Coordinate;
        //        long StackCarrier = posdictionary["StackCover"].Coordinate;
        //        //int mmC_pitch = 556;//tmp
        //        int mmC_pitch = (int)((StackBase - StackCarrier) / 29);
        //        int mm5_pitch = (int)((StackBase - Stack5mm) / 29);
        //        int mm6_pitch = (int)((StackBase - Stack6mm) / 29);
        //        int mm10_pitch = (int)((StackBase - Stack10mm) / 18);
        //        int mm562_pitch = (int)((StackBase - stak562mm) / 29);
        //        log.Debug("calibrated cover pitch " + mmC_pitch.ToString());
        //        log.Debug("5mm cover pitch " + Stack5mm.ToString());
        //        log.Debug("6mm cover pitch " + Stack6mm.ToString());
        //        log.Debug("10mm cover pitch " + Stack10mm.ToString());
        //        log.Debug("StackBase " + StackBase.ToString());

                
        //        int k = 0;
        //        if (TrayIDList[0].Contains(sCoverTrayPrefix))
        //        {
        //            if (usbCamera.itpk != usbCamera.__position.Count)//different so expect first tray to be cover tray
        //                                                             //did not check if itpk == count may be a problem later...
        //            {
        //                k = 1;
        //                IndexList.Add(556); //add index tray
        //                CalibratedList.Add(mmC_pitch);//this is wrong.. if not calibrated
        //                for (int i = 0; i < (usbCamera.__position.Count - 1); i++)//assuming all trays are the same
        //                {
        //                    if (TrayIDList[i + 1].Contains(sCoverTrayPrefix))
        //                    {
        //                        //should not be the case.. if yes.. reject tray stacks
        //                        pMode.SetWarningMsg("Invalid Tray Stack with additional cover trays");
        //                        logTool.WarnLog("Invalid Tray Stack with additional cover trays");
        //                        GemCtrl.cmdS2F42evt.Set();
        //                        Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
        //                        {
        //                            GemCtrl.MachineMsg = "Invalid Tray Stack with additional cover trays";
        //                            GemCtrl.SetDisplay(true);
        //                        });
        //                        iChkStatus = 2;
        //                        return RunState.ReverseTrayCheck;
        //                    }
        //                    IndexList.Add(usbCamera._IndexValue);
        //                    //new calibrated algorithm
        //                    if (usbCamera._IndexValue == 1005)
        //                    {
        //                        CalibratedList.Add(mm10_pitch);
        //                    }
        //                    if (usbCamera._IndexValue == 565)
        //                    {
        //                        CalibratedList.Add(mm5_pitch);
        //                    }
        //                    if (usbCamera._IndexValue == 635)
        //                    {
        //                        CalibratedList.Add(mm6_pitch);
        //                    }
        //                    if (usbCamera._IndexValue == 562)
        //                    {
        //                        CalibratedList.Add(mm562_pitch);
        //                    }
        //                }
        //            }
        //            else//all trays are expected to be cover trays
        //            {
        //                for (int i = 0; i < (usbCamera.__position.Count); i++)//assuming all trays are the same
        //                {
        //                    IndexList.Add(556);
        //                    // CalibratedList.Add(mainapp.razor.rcpPitch);//this is wrong
        //                    CalibratedList.Add(mmC_pitch);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            //if its not cover tray
        //            if (usbCamera.itpk == 0)
        //            {
        //                //assuming all trays are the same
        //                for (int i = 0; i < (usbCamera.__position.Count); i++)
        //                {
        //                    IndexList.Add(usbCamera._IndexValue);
        //                    //new calibrated algorithm
        //                    if (usbCamera._IndexValue == 1005)
        //                    {
        //                        CalibratedList.Add(mm10_pitch);
        //                    }
        //                    if (usbCamera._IndexValue == 565)
        //                    {
        //                        CalibratedList.Add(mm5_pitch);
        //                    }
        //                    if (usbCamera._IndexValue == 635)
        //                    {
        //                        CalibratedList.Add(mm6_pitch);
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                //there is a cover tray inside.. reject tray stacks
        //                //should not be the case.. if yes.. reject tray stacks
        //                pMode.SetInfoMsg("Invalid Tray Stack with additional cover trays");
        //                logTool.InfoLog("Invalid Tray Stack with additional cover trays");
        //                GemCtrl.cmdS2F42evt.Set();
        //                Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
        //                {
        //                    GemCtrl.MachineMsg = "Invalid Tray Stack with additional cover trays";
        //                    GemCtrl.SetDisplay(true);
        //                });
        //                iChkStatus = 3;
        //                return RunState.ReverseTrayCheck;
        //            }
        //        }

        //        // removed on 13th May
        //        // if (!TrayIDList[0].Contains("TPK")) throw new Exception("Label detect error");
        //        //do the secs/gem request here... now that we make new assumption that cover tray does not necessary appear

        //        Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
        //        {
        //            mainapp.CarrierHt = IndexList[0].ToString() + "0 um";
        //            mainapp.strCarrierID = TrayIDList[0];//this is the carrier ID
        //            mainapp.IpTrayCount = IndexList.Count.ToString();
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        pMode.SetWarningMsg("Exception on Vision Label Error :" + ex.Message);
        //        logTool.WarnLog("Exception on Vision Label Error :" + ex.Message);
        //        GemCtrl.cmdS2F42evt.Set();
        //        Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
        //        {
        //            GemCtrl.MachineMsg = ex.Message;
        //            GemCtrl.SetDisplay(true);
        //        });
        //        iChkStatus = 4;
        //        return RunState.ReverseTrayCheck;
        //    }
        //    //tray map successful
        //    GemCtrl.strS2F42Reststring = "0";
        //    GemCtrl.cmdS2F41evt.Reset();
        //    GemCtrl.cmdS2F42evt.Set();
        //    evtIPStackerAllowBuffer.Set();  //ready to accept buffer
        //    return RunState.TrayWaitPositionAfterMap;
        //}

        private RunState TrayWaitPositionAfterMapFn()   //seq 06
        {

            /*gdUnit;
            public int emptyUnit;
            public int XUnit;*/
            //did not check carrier tray yet
            log.Debug("Tray Position After Map");
            log.Debug("Total Index " + InputCV.IndexList.Count);
            logTool.DebugLog("Total Index " + InputCV.IndexList.Count);
            //int totaltoindexdown = 0;
            foreach (int i in InputCV.IndexList)
            {
                log.Debug("index " + i.ToString());
                logTool.DebugLog("index " + i.ToString());
                //totaltoindexdown = totaltoindexdown + i;
            }
            //totaltoindexdown = totaltoindexdown + 5000;
            int calibratedtotaltoindexdown = 0;
            foreach (int j in InputCV.CalibratedList)
            {
                log.Debug("calibrated index" + j.ToString());
                logTool.DebugLog("calibrated index" + j.ToString());
                calibratedtotaltoindexdown = calibratedtotaltoindexdown + j;
            }
            //totaltoindexdown = totaltoindexdown + 4500;
            //log.Debug("total number Down Position include cyc : " + totaltoindexdown.ToString());
            log.Debug("total number Calibrated Down Position include cyc : " + calibratedtotaltoindexdown.ToString());
            log.Debug("Stacker base " + posdictionary["StackBase"].Coordinate.ToString());
            log.Debug("Coveyor Pos " + posdictionary["Conveyor"].Coordinate.ToString());
            //logTool.DebugLog("total number Down Position include cyc : " + totaltoindexdown.ToString());
            logTool.DebugLog("total number Calibrated Down Position include cyc : " + calibratedtotaltoindexdown.ToString());
            logTool.DebugLog("Stacker base " + posdictionary["StackBase"].Coordinate.ToString());
            logTool.DebugLog("Coveyor Pos " + posdictionary["Conveyor"].Coordinate.ToString());
            //AxisPosition pos = new AxisPosition()
            //{
            //    AccTime = posdictionary["Conveyor"].AccTime,
            //    Coordinate = posdictionary["Conveyor"].Coordinate,
            //    DecTime = posdictionary["Conveyor"].DecTime,
            //    InPositionRange = posdictionary["Conveyor"].InPositionRange,
            //    IsRelativePosition = posdictionary["Conveyor"].IsRelativePosition,
            //    MaxVelocity = posdictionary["Conveyor"].MaxVelocity,
            //    StartVelocity = posdictionary["Conveyor"].StartVelocity,
            //    Name = "TrayVariablePos"
            //};
            //pos.Coordinate = pos.Coordinate - totaltoindexdown;
            //MoveAxis(pos, 1000);

            AxisPosition stackbaserefpos = new AxisPosition()
            {
                //StackBaseRef
                AccTime = posdictionary["StackBaseRef"].AccTime,
                Coordinate = posdictionary["StackBaseRef"].Coordinate,
                DecTime = posdictionary["StackBaseRef"].DecTime,
                InPositionRange = posdictionary["StackBaseRef"].InPositionRange,
                IsRelativePosition = posdictionary["StackBaseRef"].IsRelativePosition,
                MaxVelocity = posdictionary["StackBaseRef"].MaxVelocity,
                StartVelocity = posdictionary["StackBaseRef"].StartVelocity,
                Name = "TrayBaseRefVariablePos"
            };
            AxisPosition poscalibrated = new AxisPosition()
            {
                AccTime = posdictionary["StackBase"].AccTime,
                Coordinate = posdictionary["StackBase"].Coordinate,
                DecTime = posdictionary["StackBase"].DecTime,
                InPositionRange = posdictionary["StackBase"].InPositionRange,
                IsRelativePosition = posdictionary["StackBase"].IsRelativePosition,
                MaxVelocity = posdictionary["StackBase"].MaxVelocity,
                StartVelocity = posdictionary["StackBase"].StartVelocity,
                Name = "TrayVariablePos"
            };
            poscalibrated.Coordinate = poscalibrated.Coordinate - calibratedtotaltoindexdown;
            stackbaserefpos.Coordinate = stackbaserefpos.Coordinate - calibratedtotaltoindexdown;
            log.Debug("Stack ref Down Pos : " + stackbaserefpos.Coordinate.ToString());
            log.Debug("*Stack Down Pos : " + poscalibrated.Coordinate.ToString());
            logTool.DebugLog("Stack ref Down Pos : " + stackbaserefpos.Coordinate.ToString());
            logTool.DebugLog("*Stack Down Pos : " + poscalibrated.Coordinate.ToString());
            //MoveAxis(pos, 1000);
            MoveAxis(stackbaserefpos, 1000);
            //cyclinder down//
            log.Debug("Stacker Cylinder retract");
            logTool.DebugLog("Stacker Cylinder retract");
            valvelist["TrayLifterPlate"].Retract();
            valvelist["TrayLifterPlate"].WaitRetract();
            log.Debug("Stacker Cylinder retracted, sensor check");
            logTool.DebugLog("Stacker Cylinder retracted, sensor check");
            //WaitEvtOn(5000, CV_Stop.evtOn, "Stack Not Clear1", "ER_IPST_E07");//check if sensor is cleared

            log.Debug("Stacker Cylinder extend");
            logTool.DebugLog("Stacker Cylinder extend");
            valvelist["TrayLifterPlate"].Extend();
            valvelist["TrayLifterPlate"].WaitExtend();
            log.Debug("Stacker Cylinder extended");
            log.Debug("Stacker move to calibrated pos");
            logTool.DebugLog("Stacker Cylinder extended");
            logTool.DebugLog("Stacker move to calibrated pos");
            MoveAxis(poscalibrated, 1000);
            // WaitEvtOn(5000, CV_Stop.evtOn, "Stack Not Clear2", "ER_IPST_E07");//check if sensor is cleared
            log.Debug("Stacker move to calibrated pos completed");
            logTool.DebugLog("Stacker move to calibrated pos completed");
            //cyclinder up
            log.Debug("Go to Standby Position");
            logTool.DebugLog("Stacker move to calibrated pos completed");
            //
            if (endseq == true)
            {
                endseq = false;
                WaitEvtOnInfinite(evtOutPutStackerSignalShutter01OPStackerSeqEndComplete);
                evtOutPutStackerSignalShutter01OPStackerSeqEndComplete.Reset();
                WaitEvtOnInfinite(evtOutPutStackerSignalShutter02OPStackerSeqEndComplete);
                evtOutPutStackerSignalShutter02OPStackerSeqEndComplete.Reset();
            }
            //
            GemCtrl.SetLoadPortAssociateState01(LoadPortAssocState.NotAssociated, "loadPortAssociationStateAssociatedNotAssociated1");
            //loadPortAssociationStateAssociatedNotAssociated1
            //LoadPortAssociationStateNotAssociatedAssociated1
            //inspection start
            //CEID600
            GemCtrl.InspectionStartStartEvent(carrierid, InputCV.sendlist);

            try
            {
                mainapp.gdUnit = 0;
                mainapp.emptyUnit = 0;
                mainapp.XUnit = 0;
                Application.Current.Dispatcher.BeginInvoke((Action)delegate // use begin invoke to avoid hung up
                {
                    mainapp.GoodUnitCnt = "0";
                    mainapp.EmptyUnitCnt = "0";
                    mainapp.XUnitCnt = "0";
                });
            }
            catch (Exception ex)
            {
                { log.Error("Count Display Update Error"); log.Error(ex.ToString()); }
                logTool.DebugLog("Count Display Update Error");
                logTool.DebugLog(ex.ToString());
            }
            bypass = false;
            return RunState.WaitForShutter;
        }

        private RunState WaitForShutterFn() //seq 07
        {
            log.Debug("Wait For Shutter Fn");
            logTool.DebugLog("Wait For Shutter Fn");
            evtIPStackerUnloadComplete.Reset();

            var posTrayGapStartCoordinate = AxisList[0].MotorAxis.CurrentCoordinate;

            Thread.Sleep(100);
            //if ((CV_Check.Logic != true)|| (!CV_Stop.Logic))
            if ((CV_Check.Logic != true))
            {
                Sopdu.ProcessApps.main.MainApp.EMO.SetOutput(true);
                pMode.SetError("CV_check sensor is not clear");
            }
            CV_Check.evtOn.WaitOne();
            evtIPStackerRdyUnload.Set();

            try
            {
                if (InputCV.TrayIDList != null && InputCV.TrayIDList.Count > 0 && !string.IsNullOrEmpty(InputCV.TrayIDList[0]))
                {
                    GemCtrl.GetMapData(InputCV. TrayIDList[0]);
                }
            }
            catch { }

            bool specialRecipe = sCurrentRecipe != "8X14S12X17REVA" && sCurrentRecipe != "8X14S12X17REVAX1.2" && sCurrentRecipe != "8X14S12X17REVAX1" &&
                sCurrentRecipe != "8X12.5S12X19RevB" && sCurrentRecipe != "8X12.5S12X19RevBX1.2" && sCurrentRecipe != "8X10.5S12X20" && sCurrentRecipe != "8X10.5S12X20X1.2";

            long shortCurrentPosition = (long)(AxisList[0].MotorAxis.CurrentCoordinate);
            // 短边gap检测
            if (specialRecipe)
            {
                AxisList[0].SetPConDicon();
                log.Debug("Modbus discon successful");
                logTool.DebugLog("Modbus discon successful");
                Sopdu.ProcessApps.main.MainApp.cstp.SetOutput(true);

                logTool.DebugLog("test1");
                if (!CV_Check.evtOff.WaitOne(3500))
                {
                    Sopdu.ProcessApps.main.MainApp.cstp.SetOutput(false);
                    AxisList[0].SetPConInit();
                    pMode.SetError("Trigger CV_Check sensor overtime", true);
                    pMode.ChkProcessMode();
                }

                logTool.DebugLog("test2");
                Sopdu.ProcessApps.main.MainApp.cstp.SetOutput(false);
                AxisList[0].SetPConInit();
                Thread.Sleep(1000);
                log.Debug($"Trigger cv_check sensor position is{AxisList[0].MotorAxis.CurrentCoordinate}");
                shortCurrentPosition = (long)(AxisList[0].MotorAxis.CurrentCoordinate);
                AxisPosition pos = new AxisPosition()//setup preengage position
                {
                    AccTime = posdictionary["PreEngage"].AccTime,
                    Coordinate = mainapp.razor.GapRange + (long)(AxisList[0].MotorAxis.CurrentCoordinate),
                    DecTime = posdictionary["PreEngage"].DecTime,
                    InPositionRange = posdictionary["PreEngage"].InPositionRange,
                    IsRelativePosition = false,
                    MaxVelocity = posdictionary["PreEngage"].MaxVelocity,
                    StartVelocity = posdictionary["PreEngage"].StartVelocity,
                    Name = "VariablePreEngagePos"
                };
                AxisPosition pos2 = new AxisPosition()//setup preengage position
                {
                    AccTime = posdictionary["PreEngage"].AccTime,
                    Coordinate = shortCurrentPosition,
                    DecTime = posdictionary["PreEngage"].DecTime,
                    InPositionRange = posdictionary["PreEngage"].InPositionRange,
                    IsRelativePosition = false,
                    MaxVelocity = posdictionary["PreEngage"].MaxVelocity,
                    StartVelocity = posdictionary["PreEngage"].StartVelocity,
                    Name = "VariablePreEngagePos"
                };
                MoveAxis(pos, 3000);

                if (!(!CV_Check.Logic && !CV_Stop.Logic))
                {
                    Thread.Sleep(300);
                    log.Debug("First time,Gap detected up direction");
                    if (!(!CV_Check.Logic && !CV_Stop.Logic))
                    {
                        if (usbCamera.itpk != usbCamera.__position.Count)
                        {
                            MoveAxis(pos2, 3000);
                            MoveAxis(pos, 3000);
                            log.Debug("First time,Gap detected up direction");
                            if (!(!CV_Check.Logic && !CV_Stop.Logic))
                            {
                                this.GemCtrl.SetAlarm("ER_IPST_E20");
                                pMode.SetError("Gap detected up direction", true, "ER_IPST_E20");
                                logTool.ErrorLog("Gap detected up direction");
                                pMode.ChkProcessMode();
                            }
                        }
                    }
                }
                log.Debug($"Up position is{AxisList[0].MotorAxis.CurrentCoordinate}");

                AxisPosition pos1 = new AxisPosition()//setup preengage position
                {
                    AccTime = posdictionary["PreEngage"].AccTime,
                    Coordinate = (long)(AxisList[0].MotorAxis.CurrentCoordinate) - mainapp.razor.GapRange * 2,
                    DecTime = posdictionary["PreEngage"].DecTime,
                    InPositionRange = posdictionary["PreEngage"].InPositionRange,
                    IsRelativePosition = false,
                    MaxVelocity = posdictionary["PreEngage"].MaxVelocity,
                    StartVelocity = posdictionary["PreEngage"].StartVelocity,
                    Name = "VariablePreEngagePos"
                };
                shortCurrentPosition = (long)(AxisList[0].MotorAxis.CurrentCoordinate);
                AxisPosition pos4 = new AxisPosition()//setup preengage position
                {
                    AccTime = posdictionary["PreEngage"].AccTime,
                    Coordinate = shortCurrentPosition,
                    DecTime = posdictionary["PreEngage"].DecTime,
                    InPositionRange = posdictionary["PreEngage"].InPositionRange,
                    IsRelativePosition = false,
                    MaxVelocity = posdictionary["PreEngage"].MaxVelocity,
                    StartVelocity = posdictionary["PreEngage"].StartVelocity,
                    Name = "VariablePreEngagePos"
                };
                MoveAxis(pos1, 3000);
                log.Debug($"Down position is{AxisList[0].MotorAxis.CurrentCoordinate}");
                Thread.Sleep(100);
                if (!CV_Stop.Logic)
                {
                    Thread.Sleep(300);
                    log.Debug("First time, Short side Gap detected down direction");
                    if (!CV_Stop.Logic)
                    {
                        if (usbCamera.itpk != usbCamera.__position.Count)
                        {
                            MoveAxis(pos4, 3000);
                            MoveAxis(pos1, 3000);
                            log.Debug("First time, Short side Gap detected down direction");
                            if (!CV_Stop.Logic)
                            {
                                this.GemCtrl.SetAlarm("ER_IPST_E21");
                                pMode.SetError("Gap detected down direction", true, "ER_IPST_E21");
                                logTool.DebugLog("Short side Gap detected down direction");
                                pMode.ChkProcessMode();
                            }
                        }
                    }
                }

            }


            long longcurrentPosition = (long)(AxisList[0].MotorAxis.CurrentCoordinate);
            //长边gap检测
            if (specialRecipe && GlobalVar.isCheckTrayGapV2)
            {
                var posTrayGapStart = new AxisPosition()
                {
                    AccTime = posdictionary["StackBase"].AccTime,
                    Coordinate = (long)posTrayGapStartCoordinate,
                    DecTime = posdictionary["StackBase"].DecTime,
                    InPositionRange = posdictionary["StackBase"].InPositionRange,
                    IsRelativePosition = posdictionary["StackBase"].IsRelativePosition,
                    MaxVelocity = posdictionary["StackBase"].MaxVelocity,
                    StartVelocity = posdictionary["StackBase"].StartVelocity,
                    Name = "TrayVariablePos"
                };
                MoveAxis(posTrayGapStart, 3000);

                if ((BalanceCheckA1.Logic != true))
                {
                    Sopdu.ProcessApps.main.MainApp.EMO.SetOutput(true);
                    pMode.SetError("Long side Tray gap sensor is not clear");
                }
                BalanceCheckA1.evtOn.WaitOne();

                AxisList[0].SetPConDicon();
                log.Debug("Modbus discon successful");
                logTool.DebugLog("Modbus discon successful");
                Sopdu.ProcessApps.main.MainApp.cstp.SetOutput(true);

                logTool.DebugLog("test1");
                if (!BalanceCheckA1.evtOff.WaitOne(3500))
                {
                    Sopdu.ProcessApps.main.MainApp.cstp.SetOutput(false);
                    AxisList[0].SetPConInit();
                    pMode.SetError("Trigger Long side Tray gap sensor A overtime", true);
                    pMode.ChkProcessMode();
                }

                logTool.DebugLog("test2");
                Sopdu.ProcessApps.main.MainApp.cstp.SetOutput(false);
                AxisList[0].SetPConInit();
                Thread.Sleep(1000);
                log.Debug($"Trigger cv_check sensor position is{AxisList[0].MotorAxis.CurrentCoordinate}");
                longcurrentPosition = (long)(AxisList[0].MotorAxis.CurrentCoordinate);

                AxisPosition pos = new AxisPosition()//setup preengage position
                {
                    AccTime = posdictionary["PreEngage"].AccTime,
                    Coordinate = mainapp.razor.GapRange + (long)(AxisList[0].MotorAxis.CurrentCoordinate),
                    DecTime = posdictionary["PreEngage"].DecTime,
                    InPositionRange = posdictionary["PreEngage"].InPositionRange,
                    IsRelativePosition = false,
                    MaxVelocity = posdictionary["PreEngage"].MaxVelocity,
                    StartVelocity = posdictionary["PreEngage"].StartVelocity,
                    Name = "VariablePreEngagePos"
                };
                AxisPosition pos2 = new AxisPosition()//setup preengage position
                {
                    AccTime = posdictionary["PreEngage"].AccTime,
                    Coordinate = longcurrentPosition,
                    DecTime = posdictionary["PreEngage"].DecTime,
                    InPositionRange = posdictionary["PreEngage"].InPositionRange,
                    IsRelativePosition = false,
                    MaxVelocity = posdictionary["PreEngage"].MaxVelocity,
                    StartVelocity = posdictionary["PreEngage"].StartVelocity,
                    Name = "VariablePreEngagePos"
                };
                MoveAxis(pos, 3000);

                if (!(!BalanceCheckA1.Logic && !BalanceCheckA2.Logic))
                {
                    Thread.Sleep(300);
                    log.Debug("First time,Gap detected up direction");
                    if (!(!BalanceCheckA1.Logic && !BalanceCheckA2.Logic))
                    {
                        if (usbCamera.itpk != usbCamera.__position.Count)
                        {
                            MoveAxis(pos2, 3000);
                            MoveAxis(pos, 3000);
                            log.Debug("First time, Long side Gap detected up direction");
                            if (!(!BalanceCheckA1.Logic && !BalanceCheckA2.Logic))
                            {
                                this.GemCtrl.SetAlarm("ER_IPST_E20");
                                pMode.SetError("Gap detected up direction", true, "ER_IPST_E20");
                                logTool.ErrorLog("Long side Gap detected up direction");
                                pMode.ChkProcessMode();
                            }
                        }
                    }
                }
                log.Debug($"Up position is{AxisList[0].MotorAxis.CurrentCoordinate}");

                AxisPosition pos1 = new AxisPosition()//setup preengage position
                {
                    AccTime = posdictionary["PreEngage"].AccTime,
                    Coordinate = (long)(AxisList[0].MotorAxis.CurrentCoordinate) - mainapp.razor.GapRange * 2,
                    DecTime = posdictionary["PreEngage"].DecTime,
                    InPositionRange = posdictionary["PreEngage"].InPositionRange,
                    IsRelativePosition = false,
                    MaxVelocity = posdictionary["PreEngage"].MaxVelocity,
                    StartVelocity = posdictionary["PreEngage"].StartVelocity,
                    Name = "VariablePreEngagePos"
                };
                longcurrentPosition = (long)(AxisList[0].MotorAxis.CurrentCoordinate);
                AxisPosition pos4 = new AxisPosition()//setup preengage position
                {
                    AccTime = posdictionary["PreEngage"].AccTime,
                    Coordinate = longcurrentPosition,
                    DecTime = posdictionary["PreEngage"].DecTime,
                    InPositionRange = posdictionary["PreEngage"].InPositionRange,
                    IsRelativePosition = false,
                    MaxVelocity = posdictionary["PreEngage"].MaxVelocity,
                    StartVelocity = posdictionary["PreEngage"].StartVelocity,
                    Name = "VariablePreEngagePos"
                };
                MoveAxis(pos1, 3000);
                log.Debug($"Down position is{AxisList[0].MotorAxis.CurrentCoordinate}");
                Thread.Sleep(100);
                if (!BalanceCheckA2.Logic)
                {
                    Thread.Sleep(300);
                    log.Debug("First time,Gap detected down direction");
                    if (!BalanceCheckA2.Logic)
                    {
                        if (usbCamera.itpk != usbCamera.__position.Count)
                        {
                            MoveAxis(pos4, 3000);
                            MoveAxis(pos1, 3000);
                            log.Debug("First time,Gap detected down direction");
                            if (!BalanceCheckA2.Logic)
                            {
                                this.GemCtrl.SetAlarm("ER_IPST_E21");
                                pMode.SetError("Gap detected down direction", true, "ER_IPST_E21");
                                logTool.DebugLog("Gap detected down direction");
                                pMode.ChkProcessMode();
                            }
                        }
                    }
                }

            }

            //end of tray map request; do not know if this also cover the carrier tray
            WaitEvtOnInfinite(evtReqIPStackerToUnload);
            while ((mainapp.pMaster.EquipmentState.CurrentState == MachineState.WarnStop) || (mainapp.pMaster.EquipmentState.CurrentState == MachineState.Warning))
            {
                pMode.ChkProcessMode();
                Thread.Sleep(100);
            }
            bool iswarning = false;
            if ((GemCtrl.gemController.CommunicationState == CommunicationState.Disabled)) bypass = true;
            if (!bypass)
                while (!iswarning)
                {
                    //int times = 0;
                    WaitEvtOnWarn(60000, GemCtrl.cmdS14F2evt, out iswarning, "TrayMap Request Timeout", "ER_IPST_E05", true, true, "Retry", "Ignore All");
                    //if( iswarning)
                    //{
                    //     GemCtrl.GetMapData(TrayIDList[0]);//send again
                    //     times++;
                    //    if(times<3)
                    //     continue;
                    //}
                    //times = 0;
                    if (!iswarning) break;//no warning message
                    pMode.ChkProcessMode();
                    //if resend,
                    //set warning to false
                    //continue
                    if (pMode.bretry)//retry mode on warning
                    {
                        iswarning = false;
                        GemCtrl.GetMapData(InputCV.TrayIDList[0]);//send again
                        continue;
                    }
                    else
                    {
                        //if ignore all
                        //set all map to ######                    
                        CurrentMapData = null;
                        bypass = true;
                        break;
                    }

                }
            else
            {
                //bypass activated
                CurrentMapData = null;
            }
            if ((!iswarning) && (!bypass))//no warning
                this.CurrentMapData = GemCtrl.mapData;
            evtIPStackerRdyUnload.Reset();
            evtReqIPStackerToUnload.Reset();
            //add following sequence there
            RunState currentstate = ToEngagePosFn();
            if (currentstate == RunState.ToConveyorPosition)
                return RunState.ToConveyorPosition;
            return TrayWaitPositionFn();
        }

        private RunState ToPreEngagePosFn() //seq 08
        {
            ////did not check carrier tray yet;
            //int totaltoindexdown = 0;
            //foreach (int i in InputCV.IndexList)//do not need to add cyc height
            //{
            //    totaltoindexdown = totaltoindexdown + i;
            //}

            int calibratedtotalindexdwn = 0;
            foreach (int j in InputCV.CalibratedList)//do not need to add cyc height
            {
                calibratedtotalindexdwn = calibratedtotalindexdwn + j;
            }
            AxisPosition pos = new AxisPosition()//setup preengage position
            {
                AccTime = posdictionary["PreEngage"].AccTime,
                Coordinate = posdictionary["PreEngage"].Coordinate,
                DecTime = posdictionary["PreEngage"].DecTime,
                InPositionRange = posdictionary["PreEngage"].InPositionRange,
                IsRelativePosition = posdictionary["PreEngage"].IsRelativePosition,
                MaxVelocity = posdictionary["PreEngage"].MaxVelocity,
                StartVelocity = posdictionary["PreEngage"].StartVelocity,
                Name = "VariablePreEngagePos"
            };

            long calibratedcoordinate = pos.Coordinate - calibratedtotalindexdwn;
            //pos.Coordinate = pos.Coordinate - totaltoindexdown;
            pos.Coordinate = calibratedcoordinate;
            log.Debug("ToEngagePosFn pos vs calibratedpos : " + pos.Coordinate.ToString() + " : " + calibratedcoordinate.ToString());
            logTool.DebugLog("ToEngagePosFn pos vs calibratedpos : " + pos.Coordinate.ToString() + " : " + calibratedcoordinate.ToString());
            MoveAxis(pos, 1000);
            return RunState.ToEngagePos;
        }

        private RunState ToEngagePosFn()    //seq 09
        {
            log.Debug("Tray # " + InputCV.IndexList.Count.ToString());
            log.Debug("Tray ID # " + InputCV.TrayIDList.Count.ToString());
            logTool.DebugLog("Tray # " + InputCV.IndexList.Count.ToString());
            logTool.DebugLog("Tray ID # " + InputCV.TrayIDList.Count.ToString());
            CurrentTrayHeight = InputCV.IndexList[0];
            CurrentTrayID = InputCV.TrayIDList[0];
            CurrentCalibratedTrayHeight = InputCV.CalibratedList[0];
            log.Debug("Tray SN " + CurrentTrayID);
            logTool.DebugLog("Tray SN " + CurrentTrayID);
            InputCV.TrayIDList.RemoveAt(0);
            InputCV.IndexList.RemoveAt(0);  //remove top 1 tray
            InputCV.CalibratedList.RemoveAt(0); //add in 19th Jan 2021
            Application.Current.Dispatcher.BeginInvoke((Action)delegate // use begin invoke to avoid hung up
            {
                mainapp.CarrierHt = CurrentTrayHeight.ToString() + "0 um";
                if (InputCV.TrayIDList.Count > 0)
                {
                    mainapp.NextID = InputCV.TrayIDList[0];
                    mainapp.IpTrayCount = InputCV.TrayIDList.Count.ToString();
                }
                else
                {
                    mainapp.IpTrayCount = "0";
                    mainapp.NextID = "NA";
                }
            });
            //int totaltoindexdown = 0;
            //foreach (int i in InputCV.IndexList)    //do not need to add cyc height
            //{
            //    totaltoindexdown = totaltoindexdown + i;
            //}
            int calibratedtotalindexdwn = 0;
            foreach (int j in InputCV.CalibratedList)   //do not need to add cyc height
            {
                calibratedtotalindexdwn = calibratedtotalindexdwn + j;
            }
            AxisPosition pos = new AxisPosition()   //setup preengage position
            {
                AccTime = posdictionary["Engage"].AccTime,
                Coordinate = posdictionary["Engage"].Coordinate,
                DecTime = posdictionary["Engage"].DecTime,
                InPositionRange = posdictionary["Engage"].InPositionRange,
                IsRelativePosition = posdictionary["Engage"].IsRelativePosition,
                MaxVelocity = posdictionary["Engage"].MaxVelocity,
                StartVelocity = posdictionary["Engage"].StartVelocity,
                Name = "VariablePreEngagePos"
            };
            long calibratedcoordinate = pos.Coordinate - calibratedtotalindexdwn;
            //pos.Coordinate = pos.Coordinate - totaltoindexdown;
            pos.Coordinate = calibratedcoordinate;
            bool specialRecipe = sCurrentRecipe != "8X14S12X17REVA" && sCurrentRecipe != "8X14S12X17REVAX1.2" && sCurrentRecipe != "8X14S12X17REVAX1" &&
               sCurrentRecipe != "8X12.5S12X19RevB" && sCurrentRecipe != "8X12.5S12X19RevBX1.2" && sCurrentRecipe != "8X10.5S12X20" && sCurrentRecipe != "8X10.5S12X20X1.2";
            if (specialRecipe)
            {
                while (true)
                {
                    Thread.Sleep(200);
                    if (AxisList[0].MotorAxis.CurrentStatus == AxisStatus.Ready)
                    { break; }
                }
                logTool.DebugLog("ToEngagePosFn pos vs calibratedpos : " + pos.Coordinate.ToString() + " : " + calibratedcoordinate.ToString());
                log.Debug("ToEngagePosFn pos vs calibratedpos : " + pos.Coordinate.ToString() + " : " + calibratedcoordinate.ToString());
                long targetAbsolutePosition = (long)Math.Round(AxisList[0].MotorAxis.CurrentCoordinate, 0) + GoToRealEngagePosition(CurrentCalibratedTrayHeight).Coordinate;
                log.Debug($"Target engage position is {targetAbsolutePosition}");
                if (Math.Abs(targetAbsolutePosition - pos.Coordinate) > 500)
                {
                    pMode.SetError("Engage position not match", true);
                    logTool.DebugLog("Engage position not match");
                    pMode.ChkProcessMode();
                }
                AxisPosition pos1 = new AxisPosition()   //setup preengage position
                {
                    AccTime = posdictionary["Engage"].AccTime,
                    Coordinate = targetAbsolutePosition,
                    DecTime = posdictionary["Engage"].DecTime,
                    InPositionRange = posdictionary["Engage"].InPositionRange,
                    IsRelativePosition = posdictionary["Engage"].IsRelativePosition,
                    MaxVelocity = posdictionary["Engage"].MaxVelocity,
                    StartVelocity = posdictionary["Engage"].StartVelocity,
                    Name = "VariablePreEngagePos"
                };
                MoveAxis(pos1, 3000);
                log.Debug($"Arrive target engage position ,cuttent position is {AxisList[0].MotorAxis.CurrentCoordinate}");
                logTool.DebugLog($"Arrive target engage position ,cuttent position is {AxisList[0].MotorAxis.CurrentCoordinate}");

                if (CurrentShutterName == "ShutterUnit01")
                {
                    mainapp.Shut1.ShutterSingulatorPinCYL();
                } else if (CurrentShutterName == "ShutterUnit02")
                {
                    mainapp.Shut2.ShutterSingulatorPinCYL();
                }

                pMode.ChkProcessMode();
                if (CurrentShutterName == "ShutterUnit01")
                {
                    int t = 0;
                    while (!(S1CHK1.Logic == S1CHK2.Logic == S1CHK3.Logic == S1CHK4.Logic))
                    {
                        t++;
                        Thread.Sleep(100);
                        if (t > 20)
                        {
                            break;
                        }
                    }
                    if (t > 20)
                    {
                        pMode.SetError($"ShutterUnit01 finger trigger status is different CHK1:{S1CHK1.Logic},CHK2:{S1CHK2.Logic},CHK3:{S1CHK3.Logic},CHK4:{S1CHK4.Logic}", true);
                        pMode.ChkProcessMode();
                    }
                }
                else
                {
                    int m = 0;
                    while (!(S2CHK1.Logic == S2CHK2.Logic == S2CHK3.Logic == S2CHK4.Logic))
                    {
                        m++;
                        Thread.Sleep(100);
                        if (m > 20)
                        {
                            break;
                        }
                    }
                    if (m > 20)
                    {
                        pMode.SetError($"ShutterUnit02 finger trigger status is different CHK1:{S2CHK1.Logic},CHK2:{S2CHK2.Logic},CHK3:{S2CHK3.Logic},CHK4:{S2CHK4.Logic}", true);
                        pMode.ChkProcessMode();
                    }
                }

            }
            else
            {
                MoveAxis(pos, 3000);

                if (CurrentShutterName == "ShutterUnit01")
                {
                    int t = 0;
                    while (!(S1CHK1.Logic == S1CHK2.Logic == S1CHK3.Logic == S1CHK4.Logic))
                    {
                        t++;
                        Thread.Sleep(100);
                        if (t > 20)
                        {
                            break;
                        }
                    }
                    if (t > 20)
                    {
                        pMode.SetError($"ShutterUnit01 finger trigger status is different CHK1:{S1CHK1.Logic},CHK2:{S1CHK2.Logic},CHK3:{S1CHK3.Logic},CHK4:{S1CHK4.Logic}", true);
                        pMode.ChkProcessMode();
                    }
                }
                else
                {
                    int m = 0;
                    while (!(S2CHK1.Logic == S2CHK2.Logic == S2CHK3.Logic == S2CHK4.Logic))
                    {
                        m++;
                        Thread.Sleep(100);
                        if (m > 20)
                        {
                            break;
                        }
                    }
                    if (m > 20)
                    {
                        pMode.SetError($"ShutterUnit02 finger trigger status is different CHK1:{S2CHK1.Logic},CHK2:{S2CHK2.Logic},CHK3:{S2CHK3.Logic},CHK4:{S2CHK4.Logic}", true);
                        pMode.ChkProcessMode();
                    }
                }

            }

            log.Debug($"currentposition is :{pos.Coordinate.ToString()},go to position: {GoToRealEngagePosition(CurrentCalibratedTrayHeight)}");
            log.Debug("Move to EngagePos : count = " + InputCV.IndexList.Count.ToString());
            log.Debug($"{CurrentShutterName} ready to take tray: {CurrentTrayID}");
            logTool.DebugLog($"currentposition is :{pos.Coordinate.ToString()},go to position: {GoToRealEngagePosition(CurrentCalibratedTrayHeight)}");
            logTool.DebugLog("Move to EngagePos : count = " + InputCV.IndexList.Count.ToString());
            logTool.DebugLog($"{CurrentShutterName} ready to take tray: {CurrentTrayID}");

            if (InputCV.IndexList.Count > 0)
            {
                log.Debug("Move to Tray Wait Position");
                logTool.DebugLog("Move to Tray Wait Position");
                return RunState.TrayWaitPosition;
            }
            else
            {
                log.Debug("Move to Conveyor Position");
                logTool.DebugLog("Move to Conveyor Position");
                return RunState.ToConveyorPosition;
            }
        }

        private RunState TrayWaitPositionFn()   //seq 10-1
        {
            //did not check carrier tray yet;
            int totaltoindexdown = 0;
            foreach (int i in InputCV.IndexList)
            {
                totaltoindexdown = totaltoindexdown + i;
            }

            //move axis to preengage position
            //make use of preengage position with slow down velocity
            AxisPosition postshutterpos = new AxisPosition()
            {
                AccTime = posdictionary["PreEngage"].AccTime,
                Coordinate = posdictionary["PreEngage"].Coordinate,
                DecTime = posdictionary["PreEngage"].DecTime,
                InPositionRange = posdictionary["PreEngage"].InPositionRange,
                IsRelativePosition = posdictionary["PreEngage"].IsRelativePosition,
                MaxVelocity = 2000,//this is the lowest speed it can go
                StartVelocity = 100,
                Name = "Postshutterpos"
            };
            postshutterpos.Coordinate = postshutterpos.Coordinate - totaltoindexdown;
            MoveAxis(postshutterpos, 1000);

            //totaltoindexdown = totaltoindexdown + 5000;

            int calibratedtotaltoindexdown = 0;
            foreach (int j in InputCV.CalibratedList)
            {
                log.Debug("calibrated index" + j.ToString());
                logTool.DebugLog("calibrated index" + j.ToString());
                calibratedtotaltoindexdown = calibratedtotaltoindexdown + j;
            }
            //17th Jan.
            totaltoindexdown = totaltoindexdown + 4500;
            // totaltoindexdown = totaltoindexdown + 6500;
            log.Debug("total number Down Position include cyc" + totaltoindexdown.ToString());
            log.Debug("total number Calibrated Down Position include cyc : " + calibratedtotaltoindexdown.ToString());
            log.Debug("Stacker base " + posdictionary["StackBase"].Coordinate.ToString());
            log.Debug("Coveyor Pos " + posdictionary["Conveyor"].Coordinate.ToString());
            logTool.DebugLog("total number Down Position include cyc" + totaltoindexdown.ToString());
            logTool.DebugLog("total number Calibrated Down Position include cyc : " + calibratedtotaltoindexdown.ToString());
            logTool.DebugLog("Stacker base " + posdictionary["StackBase"].Coordinate.ToString());
            logTool.DebugLog("Coveyor Pos " + posdictionary["Conveyor"].Coordinate.ToString());
            //AxisPosition pos = new AxisPosition()
            //{
            //    AccTime = posdictionary["Conveyor"].AccTime,
            //    Coordinate = posdictionary["Conveyor"].Coordinate,
            //    DecTime = posdictionary["Conveyor"].DecTime,
            //    InPositionRange = posdictionary["Conveyor"].InPositionRange,
            //    IsRelativePosition = posdictionary["Conveyor"].IsRelativePosition,
            //    MaxVelocity = posdictionary["Conveyor"].MaxVelocity,
            //    StartVelocity = posdictionary["Conveyor"].StartVelocity,
            //    Name = "TrayVariablePos"
            //};

            AxisPosition poscalibrated = new AxisPosition()
            {
                AccTime = posdictionary["StackBase"].AccTime,
                Coordinate = posdictionary["StackBase"].Coordinate,
                DecTime = posdictionary["StackBase"].DecTime,
                InPositionRange = posdictionary["StackBase"].InPositionRange,
                IsRelativePosition = posdictionary["StackBase"].IsRelativePosition,
                MaxVelocity = posdictionary["StackBase"].MaxVelocity,
                StartVelocity = posdictionary["StackBase"].StartVelocity,
                Name = "TrayVariablePos"
            };

            //pos.Coordinate = pos.Coordinate - totaltoindexdown;
            poscalibrated.Coordinate = poscalibrated.Coordinate - calibratedtotaltoindexdown;

            evtIPStackerUnloadComplete.Set();//release shutter
            log.Debug("Fire IPStackerUnloadComplete event");
            logTool.DebugLog("Fire IPStackerUnloadComplete event");

            long PreEngageCheckCoordinate;
            if (!posdictionary.ContainsKey("PreEngageCheck")) 
            {
                PreEngageCheckCoordinate = 500;
            } else
            {
                PreEngageCheckCoordinate = posdictionary["PreEngageCheck"].Coordinate;
            }
            
            if (EnablePreEngageCheck)
            {
                bool isCheckTray = carrierid.Contains(sCoverTrayPrefix);
                //
                AxisPosition midCheckPosition = new AxisPosition()
                {
                    AccTime = posdictionary["PreEngage"].AccTime,
                    Coordinate = (long)AxisList[0].MotorAxis.CurrentCoordinate - PreEngageCheckCoordinate,
                    DecTime = posdictionary["PreEngage"].DecTime,
                    InPositionRange = posdictionary["PreEngage"].InPositionRange,
                    IsRelativePosition = posdictionary["PreEngage"].IsRelativePosition,
                    MaxVelocity = posdictionary["PreEngage"].MaxVelocity,
                    StartVelocity = posdictionary["PreEngage"].StartVelocity,
                    Name = "MidCheck"
                };

                MoveAxis(midCheckPosition, 1000);
                Thread.Sleep(100);
                var errmsg = "";
                log.Debug("Stack Down midCheckPosition : " + midCheckPosition.Coordinate.ToString());
                string loginfo = $"currnet TrayID is {carrierid}, ";
                bool isCoverTray = carrierid?.Contains(sCoverTrayPrefix) ?? false;
                if (isCoverTray)
                {
                    loginfo += "is CoverTray";
                }
                else
                {
                    loginfo += "is JedecTray ";
                }
                if (CurrentShutterName == "ShutterUnit01")
                {
                    if (!mainapp.Shut1.CheckTray(out errmsg))
                    {
                        usbCamera.SaveImage();
                        _fingerEngagementRepository.Insert(new Dct.Models.Entity.FingerEngagementEntity() { Name = $"Shutter01 {errmsg}", ShutterName= "Shutter01", StartTime = DateTime.Now }, out _);
                        log.Debug($"The device has pause. The ShutterUnit01 sensor '{errmsg}' is abnormal");
                        log.Debug($"currnet Recipe is {CurrentRecipe}");
                        log.Debug($"{loginfo}");
                        mainapp.pMaster.SetPause();
                        if (MessageBox.Show("The device has been paused, click OK to continue running", "tips", MessageBoxButton.OK, MessageBoxImage.Warning) == MessageBoxResult.OK)
                        {
                            mainapp.pMaster.ResetPause();

                            if (!mainapp.Shut1.CheckTray(out errmsg))
                            {
                                _fingerEngagementRepository.Insert(new Dct.Models.Entity.FingerEngagementEntity() { Name = $"Shutter01 {errmsg}", ShutterName = "Shutter01", StartTime = DateTime.Now }, out _);
                                log.Debug($"The device has down. The ShutterUnit01 sensor '{errmsg}' is abnormal");
                                log.Debug($"currnet Recipe is {CurrentRecipe}");
                                log.Debug($"{loginfo}");
                                pMode.SetError("Tray is abnormal still on shutter 1, please check...", true);
                            }
                        }
                    }
                }
                else
                {
                    if (!mainapp.Shut2.CheckTray(out errmsg))
                    {
                        usbCamera.SaveImage();
                        _fingerEngagementRepository.Insert(new Dct.Models.Entity.FingerEngagementEntity() { Name = $"Shutter02 {errmsg}", ShutterName = "Shutter02", StartTime = DateTime.Now }, out _);
                        log.Debug($"The device has pause. The ShutterUnit02 sensor '{errmsg}' is abnormal");
                        log.Debug($"currnet Recipe is {CurrentRecipe}");
                        log.Debug($"{loginfo}");
                        mainapp.pMaster.SetPause();
                        if (MessageBox.Show("The device has been paused, click OK to continue running", "tips", MessageBoxButton.OK, MessageBoxImage.Warning) == MessageBoxResult.OK)
                        {
                            mainapp.pMaster.ResetPause();
                            if (!mainapp.Shut2.CheckTray(out errmsg))
                            {
                                _fingerEngagementRepository.Insert(new Dct.Models.Entity.FingerEngagementEntity() { Name = $"Shutter02 {errmsg}", ShutterName = "Shutter02", StartTime = DateTime.Now }, out _);
                                log.Debug($"The device has cdown. The ShutterUnit02 sensor '{errmsg}' is abnormal");
                                log.Debug($"currnet Recipe is {CurrentRecipe}");
                                log.Debug($"{loginfo}");
                                pMode.SetError("Tray is abnormal still on shutter 2, please check...", true);
                            }
                        }

                    }
                }
                pMode.ChkProcessMode();

            }

            //MoveAxis(pos, 1000);
            log.Debug("Stack Down Pos : " + poscalibrated.Coordinate.ToString());
            logTool.DebugLog("Stack Down Pos : " + poscalibrated.Coordinate.ToString());
            MoveAxis(poscalibrated, 1000);
            //WaitEvtOn(5000, CV_Stop.evtOn, "Stack Not Clear", "ER_IPST_E07");//check if sensor is cleared
            Thread.Sleep(100);
            //if ((CV_Check.Logic != true) || (!CV_Stop.Logic))
            if ((CV_Check.Logic != true))
            {
                Sopdu.ProcessApps.main.MainApp.EMO.SetOutput(true);
                pMode.SetError("CV_check sensor is not clear");
            }
            log.Debug("Go to Standby Position");
            logTool.DebugLog("Go to Standby Position");
            return RunState.WaitForShutter;
        }
        public AxisPosition GoToRealEngagePosition(long CurrentTrayHeight)
        {
            AxisPosition pos = new AxisPosition()   //setup preengage position
            {
                AccTime = posdictionary["Engage"].AccTime,
                Coordinate = posdictionary["Engage"].Coordinate,
                DecTime = posdictionary["Engage"].DecTime,
                InPositionRange = posdictionary["Engage"].InPositionRange,
                IsRelativePosition = true,
                MaxVelocity = posdictionary["Engage"].MaxVelocity,
                StartVelocity = posdictionary["Engage"].StartVelocity,
                Name = "VariablePreEngagePos"
            };
            AxisPosition pos1 = new AxisPosition()   //setup preengage position
            {
                AccTime = posdictionary["SensorToS1Finger"].AccTime,
                Coordinate = posdictionary["SensorToS1Finger"].Coordinate,
                DecTime = posdictionary["SensorToS1Finger"].DecTime,
                InPositionRange = posdictionary["SensorToS1Finger"].InPositionRange,
                IsRelativePosition = true,
                MaxVelocity = posdictionary["SensorToS1Finger"].MaxVelocity,
                StartVelocity = posdictionary["SensorToS1Finger"].StartVelocity,
                Name = "VariablePreEngagePos"
            };

            AxisPosition pos2 = new AxisPosition()   //setup preengage position
            {
                AccTime = posdictionary["SensorToS2Finger"].AccTime,
                Coordinate = posdictionary["SensorToS2Finger"].Coordinate,
                DecTime = posdictionary["SensorToS2Finger"].DecTime,
                InPositionRange = posdictionary["SensorToS2Finger"].InPositionRange,
                IsRelativePosition = true,
                MaxVelocity = posdictionary["SensorToS2Finger"].MaxVelocity,
                StartVelocity = posdictionary["SensorToS2Finger"].StartVelocity,
                Name = "VariablePreEngagePos"
            };
            pos.Coordinate = CurrentTrayHeight + (CurrentShutterName == "ShutterUnit01" ? pos1.Coordinate : pos2.Coordinate);
            return pos;
        }
        private RunState ToConveyorPositionFn() //seq 10-2
        {

            //move axis to preengage position
            //make use of preengage position with slow down velocity
            AxisPosition postshutterpos = new AxisPosition()
            {
                AccTime = posdictionary["PreEngage"].AccTime,
                Coordinate = posdictionary["PreEngage"].Coordinate,
                DecTime = posdictionary["PreEngage"].DecTime,
                InPositionRange = posdictionary["PreEngage"].InPositionRange,
                IsRelativePosition = posdictionary["PreEngage"].IsRelativePosition,
                MaxVelocity = 2000,//this is the lowest speed it can go
                StartVelocity = 100,
                Name = "Postshutterpos"
            };
            //postshutterpos.Coordinate = postshutterpos.Coordinate - 100;
            MoveAxis(postshutterpos, 1000);
            log.Debug("evtIPStackerUnloadComplete set");
            logTool.DebugLog("evtIPStackerUnloadComplete set");
            evtIPStackerUnloadComplete.Set();//release shutter  
            //Thread.Sleep(100);
            valvelist["BenchingBar"].Extend();
            valvelist["TrayLifterPlate"].Retract();
            valvelist["TrayLifterPlate"].WaitRetract();
            MoveAxis("Conveyor", 1000);

            valvelist["TrayLifterPlate"].WaitRetract();
            //evtIPStackerRdyUnload.Reset();
            //request both shutter to go to IP Location
            evtIPRequestShutter01MoveToIPLocation.Set();
            evtIPRequestShutter02MoveToIPLocation.Set();
            log.Debug("Shutter to IP set");
            logTool.DebugLog("Shutter to IP set");
            return RunState.WaitShutterAtReadyCondition;
        }

        private RunState WaitShutterAtReadyConditionFn()
        {
            //log.Debug("Wait For Shutter Ready");
            bool bshutter01rdy = WaitEvtOnWithoutErrorFire(100, evtShutter01Rdy);
            bool bshutter02rdy = WaitEvtOnWithoutErrorFire(100, evtShutter02Rdy);

            if (bshutter01rdy && bshutter02rdy)
            {
                evtShutter01Rdy.Reset();
                evtShutter02Rdy.Reset();
                valvelist["BenchingBar"].WaitExtend();//make sure benching bar is open
                // Trigger Shutter Door open
                TrigShutDoor(true);
                //valvelist["Input Shutter Door"].Retract();
                //valvelist["Input Shutter Door"].WaitRetract();
                log.Debug("Seq End Complete, IP Stacker go to start seq");
                logTool.DebugLog("Seq End Complete, IP Stacker go to start seq");
                if (!bypass)
                    GemCtrl.InspectionComplete(mainapp.strCarrierID, InputCV.sendlist);
                bypass = false;//reset bypass
                endseq = true;
                return RunState.Start;
            }
            else
                return RunState.WaitShutterAtReadyCondition;
        }

        protected override void StoppingLogicFn()
        {
            //set all motor current state
            bM1Bwdbackup = M1Bwd.Logic;
            bM1Fwdbackup = M1Fwd.Logic;
            bM1FwdSlow = M1FwdSlow.Logic;
            //set all motor to stop
            M1Fwd.Logic = false;
            M1Bwd.Logic = false;
            M1FwdSlow.Logic = false;
        }

        protected override void RecoverFromStopFn()
        {
            base.RecoverFromStopFn();
            M1Fwd.Logic = bM1Fwdbackup;
            M1Bwd.Logic = bM1Bwdbackup;
            M1FwdSlow.Logic = bM1FwdSlow;
        }

        private enum RunState
        {
            Start, MoveToTrayMap, TrayMapInpsection, GotoPitchDetection, PitchDetection, TrayWaitPosition, WaitForShutter, ToPreEngagePos, ToEngagePos, ToConveyorPosition,
            TrayWaitPositionAfterMap, WaitShutterAtReadyCondition, WaitForIPClearSensorOn, DebugEvt, BenchingSequence,
            ReverseTrayCheck, ReverseTraySequence01, ReverseTraySequence02, ReverseTraySequence03, ReverseTraySequence04
        }
        private RunState runstate;
        #endregion

        #region // Common Method and function
        public void MoveAxis(string PosName, int timeout, int percnt = 100)     //100ms per count
        {
            AxisList[0].MotorAxis.StartMove(posdictionary[PosName]);
            int timeoutcnt = 0;
            while (!AxisList[0].MotorAxis.PositionEnd)
            {
                WaitTime(percnt);
                timeoutcnt++;
                if (timeoutcnt > timeout)
                {
                    //set error
                    this.pMode.SetError("MP1 Move " + PosName + " Timeout!", true, "ER_" + AxisList[0].MotorAxis.DisplayName + "Error");
                    this.GemCtrl.SetAlarm("ER_" + AxisList[0].MotorAxis.DisplayName + "Error");
                    log.Debug("timeout error for MP1");
                    logTool.DebugLog("timeout error for MP1");
                }
                this.pMode.ChkProcessMode();
            }
        }
        public void MoveAxis(AxisPosition pos, int timeout, int percnt = 100)   //100ms per count
        {
            AxisList[0].MotorAxis.StartMove(pos);
            int timeoutcnt = 0;
            while (!AxisList[0].MotorAxis.PositionEnd)
            {
                WaitTime(percnt);
                timeoutcnt++;
                if (timeoutcnt > timeout)
                {
                    //set error
                    this.pMode.SetError("MP1 Move " + pos.Name + " Timeout!", true, "ER_" + AxisList[0].MotorAxis.DisplayName + "Error");
                    this.GemCtrl.SetAlarm("ER_" + AxisList[0].MotorAxis.DisplayName + "Error");
                    log.Debug("timeout error for MP1");
                    logTool.DebugLog("timeout error for MP1");
                }
                this.pMode.ChkProcessMode();
            }
        }
        public void MoveAxis1(AxisPosition pos, int timeout, int percnt = 100)   //100ms per count
        {
            System.Diagnostics.Stopwatch sw1 = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            AxisList[0].MotorAxis.StartMove(pos);
            sw1.Start();
            while (!AxisList[0].MotorAxis.PositionEnd)
            {
                if (CV_Check.Logic != CV_Stop.Logic)
                {
                    if (!sw.IsRunning)
                    {
                        sw.Start();
                    }
                }

                if (CV_Check.Logic == CV_Stop.Logic)
                {
                    if (sw.IsRunning)
                    {
                        sw.Stop();
                        sw.Reset();
                    }
                }
                if (sw.ElapsedMilliseconds > 500)
                {
                    this.pMode.SetError("Tray gap discovered", true);
                    logTool.ErrorLog("Tray gap discovered");
                    this.pMode.ChkProcessMode();
                }

                if (sw1.ElapsedMilliseconds > 10000)
                {
                    sw1.Stop();
                    this.pMode.SetError("MP1 Move " + pos.Name + " Timeout!", true);
                    logTool.ErrorLog("MP1 Move " + pos.Name + " Timeout!");
                    this.pMode.ChkProcessMode();
                }

            }

        }

        protected void BenchingBar(bool open)
        {
            if (open)
            {
                ManualResetEvent[] evtarray = new ManualResetEvent[] { CYL1BenchingBarClose.evtOn, CYL2BenchingBarClose.evtOn };
                BenchingBarOpen.Logic = true;
                BenchingBarClose.Logic = false;
                WaitAllEvtOn(2000, evtarray, "Input Stacker Benching Bar Open");
            }
            else
            {
                ManualResetEvent[] evtarray = new ManualResetEvent[] { CYL1BenchingBarClose.evtOff, CYL2BenchingBarClose.evtOff };
                BenchingBarOpen.Logic = true;
                BenchingBarClose.Logic = false;
                WaitAllEvtOn(2000, evtarray, "Input Stacker Benching Bar Close");
            }
        }

        protected void CVStop()
        {
            M1Fwd.Logic = false;
            M1FwdSlow.Logic = false;
            M1Bwd.Logic = false;
        }

        protected void CVSlow()
        {
            M1Fwd.Logic = false;
            M1FwdSlow.Logic = true;
            M1Bwd.Logic = false;
        }

        protected void CVMove(bool isFwd, bool isSlow)
        {
            if (isFwd)
            {
                if (isSlow)
                {
                    M1Fwd.Logic = true;
                    M1FwdSlow.Logic = true;
                    M1Bwd.Logic = false;
                }
                else
                {
                    M1Fwd.Logic = true;
                    M1FwdSlow.Logic = false;
                    M1Bwd.Logic = false;
                }
            }
            else
            {
                M1Fwd.Logic = false;
                M1FwdSlow.Logic = false;
                M1Bwd.Logic = true;
            }
        }

        #endregion

        #region Set Input Output and Event list


        public override void RunPolling()
        {

        }
        private DiscreteIO ShutterDoorClose, CYL1BenchingBarClose, CYL1BenchingBarOpen, CYL2BenchingBarClose, CYL2BenchingBarOpen, CVInputClear, CV_Slow, CV_Stop, CV_Check, S1CHK1, S1CHK2, S1CHK3, S1CHK4, S2CHK1, S2CHK2, S2CHK3, S2CHK4;

        private DiscreteIO BalanceCheckA1, BalanceCheckA2;
        public bool isDIOK = false, isDOOK = false;

        protected override void InitInput()//initialize input
        {
            //assigning inputs
            base.InitOutput();
            CYL1BenchingBarClose = this.inputlist.IpDirectory[InputNameList[0]];
            CYL1BenchingBarOpen = this.inputlist.IpDirectory[InputNameList[1]];
            CYL2BenchingBarClose = this.inputlist.IpDirectory[InputNameList[2]];
            CYL2BenchingBarOpen = this.inputlist.IpDirectory[InputNameList[3]];
            CVInputClear = this.inputlist.IpDirectory[InputNameList[6]];
            CV_Slow = this.inputlist.IpDirectory[InputNameList[7]];

            CV_Stop = this.inputlist.IpDirectory[InputNameList[8]];
            CV_Check = this.inputlist.IpDirectory[InputNameList[9]];

            S1CHK1 = this.inputlist.IpDirectory[InputNameList[10]];
            S1CHK2 = this.inputlist.IpDirectory[InputNameList[11]];
            S1CHK3 = this.inputlist.IpDirectory[InputNameList[12]];
            S1CHK4 = this.inputlist.IpDirectory[InputNameList[13]];
            S2CHK1 = this.inputlist.IpDirectory[InputNameList[14]];
            S2CHK2 = this.inputlist.IpDirectory[InputNameList[15]];
            S2CHK3 = this.inputlist.IpDirectory[InputNameList[16]];
            S2CHK4 = this.inputlist.IpDirectory[InputNameList[17]];
            if (isOwnShutDoor)
                ShutterDoorClose = this.inputlist.IpDirectory[InputNameList[9]];

            if (GlobalVar.isCheckTrayGapV2)
            {
                BalanceCheckA1 = this.inputlist.IpDirectory[InputNameList[18]];
                BalanceCheckA2 = this.inputlist.IpDirectory[InputNameList[19]];
            }
            isDIOK = true;
        }

        public ManualResetEvent evtOutPutStackerSignalShutter01OPStackerSeqEndComplete, evtOutPutStackerSignalShutter02OPStackerSeqEndComplete, evtIPStackerAllowBuffer,
                                evtIPStackerRdyToLoad, evtStackerCVRunComplete, evtIPCVRequestStackerCVRunAck, evtIPCVRequestStackerCVRun, evtIPRequestShutter01MoveToIPLocation,
                                evtIPRequestShutter02MoveToIPLocation, evtShutter01Rdy, evtShutter02Rdy, evtIPStackerUnloadComplete, evtInit_InputStackerHomeComplete,
                                evtReqIPStackerToUnload, evtRevCVRequest, evtRevCVRequestAck, evtRevMotorStopped, evtIPStackerRdyUnload;

        protected override void InitEvt()//initialize events
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
            evtIPStackerRdyUnload = evtdict[EvtNameList[4]].evt;
            evtReqIPStackerToUnload = evtdict[EvtNameList[5]].evt;
            evtIPStackerUnloadComplete = evtdict[EvtNameList[6]].evt;
            evtShutter01Rdy = evtdict[EvtNameList[7]].evt;
            evtShutter02Rdy = evtdict[EvtNameList[8]].evt;
            evtIPRequestShutter01MoveToIPLocation = evtdict[EvtNameList[9]].evt;
            evtIPRequestShutter02MoveToIPLocation = evtdict[EvtNameList[10]].evt;
            evtIPCVRequestStackerCVRun = evtdict[EvtNameList[11]].evt;
            evtIPCVRequestStackerCVRunAck = evtdict[EvtNameList[12]].evt;
            evtStackerCVRunComplete = evtdict[EvtNameList[13]].evt;
            evtIPStackerRdyToLoad = evtdict[EvtNameList[14]].evt;
            evtIPStackerAllowBuffer = evtdict[EvtNameList[15]].evt;
            evtOutPutStackerSignalShutter01OPStackerSeqEndComplete = evtdict[EvtNameList[16]].evt;
            evtOutPutStackerSignalShutter02OPStackerSeqEndComplete = evtdict[EvtNameList[17]].evt;
        }

        private DiscreteIO BenchingBarOpen, BenchingBarClose, M1Fwd, M1FwdSlow, M1Bwd;
        private Dictionary<string, AxisPosition> posdictionary;
        public static Dictionary<string, AxisPosition> posdictionary1;

        protected override void InitOutput()//initialize output
        {
            //assigning outputs
            base.InitOutput();
            BenchingBarOpen = this.outputlist.IpDirectory[OutputNameList[0]];
            BenchingBarClose = this.outputlist.IpDirectory[OutputNameList[1]];
            M1Fwd = this.outputlist.IpDirectory[OutputNameList[6]];
            M1FwdSlow = this.outputlist.IpDirectory[OutputNameList[5]];
            M1Bwd = this.outputlist.IpDirectory[OutputNameList[4]];

            isDOOK = true;
        }

        #endregion Set Input Output and Event list


        private string sCylDoor = "Input Shutter Door";
        private string carrierid;
        private int iChkStatus = 0;     // 1-Pitch detect fail, 2- , 3- , 4-Exception
        private string CurrentShutterName;
        private bool endseq = false;
        private bool bypass = false;
        private bool bM1Bwdbackup, bM1Fwdbackup, bM1FwdSlow;

        public UsbCamera usbCamera { get { return _usbCamera; } set { _usbCamera = value; NotifyPropertyChanged("usbCamera"); } }
        private UsbCamera _usbCamera;

        [XmlIgnore]
        public main.MainApp mainapp;
        [XmlIgnore]
        public int CurrentTrayHeight { get; set; }
        [XmlIgnore]
        public string CurrentTrayID { get; set; }
        [XmlIgnore]
        public MapData CurrentMapData { get; set; }
        [XmlIgnore]
        public int CurrentCalibratedTrayHeight { get; set; }

        private bool isReverse { get { return GlobalVar.isReverse; } }
        private bool isOwnShutDoor { get { return !GlobalVar.isDoorAtCV; } }
        private bool isShutDoorClosed { get { return CheckDoorClosed(); } }
        public bool isPartPresent { get { return CheckPartPresent(); } }
        public bool isPartAbsent { get { return CheckPartAbsent(); } }


        public string sCurrentRecipe = null;


        private bool CheckPartAbsent()
        {
            //return CVInputClear.Logic & CV_Slow.Logic & CV_Stop.Logic;
            return !CVInputClear.Logic & CV_Slow.Logic;
        }
        private bool CheckPartPresent()
        {
            //return !CVInputClear.Logic | !CV_Slow.Logic | !CV_Stop.Logic;
            return CVInputClear.Logic | !CV_Slow.Logic;
        }
        private bool CheckDoorClosed()
        {
            if (isOwnShutDoor)
                return ShutterDoorClose.Logic;
            else
                return true;
        }
        private void TrigShutDoor(bool isOpen)
        {
            if (isOwnShutDoor)
            {
                try
                {
                    if (isOpen)
                    {
                        valvelist[sCylDoor].Retract();
                        valvelist[sCylDoor].WaitRetract();
                    }
                    else
                    {
                        valvelist[sCylDoor].Extend();
                        valvelist[sCylDoor].WaitExtend();
                    }
                }
                catch { }
            }
        }

        private enum EVsnChkCode
        {
            Pass,
            PitchFail,
            ImageFail
        }

        public void ReceiveCurrentShutterName(string str)
        { CurrentShutterName = str; }
    }
}