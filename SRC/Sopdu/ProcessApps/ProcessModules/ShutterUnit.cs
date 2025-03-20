using Basler.Pylon;
using LogPanel;
using Sopdu.Devices.CameraLink;
using Sopdu.Devices.Cylinders;
using Sopdu.Devices.IOModule;
using Sopdu.Devices.MotionControl.Base;
using Sopdu.helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace Sopdu.ProcessApps.ProcessModules
{
    public class ShutterUnit : Process
    {
        //test
        LogTool<ShutterUnit> logTool = new LogTool<ShutterUnit>();
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private DiscreteIO TraySnr01, TraySnr02, TraySnr03, TraySnr04, ShutterInPos, ShutterTrayClear, MagneticEncoderRYON;
        private ManualResetEvent evtShutterIPStackerLowerPositionClear, evtShutterIPStackerUpperPositionClear,
                                 evtShutterOPStackerLowerPositionClear, evtShutterOPStackerUpperPositionClear,
                                 evtShutterClearCameraUpperLocation,    evtShutterClearCameraLowerLocation,
                                 evtOtherShutterClearCameraUpperLocation,
                                 evtOtherShutterClearCameraLowerLocation, evtReqAcq,evtReqAcqAck,
                                 evtOtherShutterIPStackerLowerPositionClear, evtOtherShutterIPStackerUpperPositionClear,
                                 evtOtherShutterOPStackerLowerPositionClear, evtOtherShutterOPStackerUpperPositionClear,
                                 evtOutputStackerStartSeq, evtOutputStackerClearShutterPlate ,
                                 evtOutputStackerClearShutterToMove,
                                 evtShutterRdy, evtShutterSignalOPStackerEndSeq,  evtIPRequestShutterMoveToIPLocation,
                                 evtShutterRdyToCallOPStackerEndSeq,              evtOtherShutterRdyToCallOPStackerEndSeq,
                                 evtOutPutStackerSignalShutterOPStackerSeqEndComplete,
                                 evtIPStackerUnloadComplete, evtReqIPStackerToUnload, 
                                 evtIPStackerRdyUnload, evtInit_InputStackerHomeComplete,
                                 evtOutputStackerAtSafeLocation, evtShutterLowerSeqComplete,evtTheOtherShutterLowerClear, evtCurrentShutterLowerClear, evtShutterHomeComplete;
        
        private Mutex ReserveIPStackPos, InitServeShutterPlateMove;
        private InitState initstate;
        private RunState runstate;
        public bool isPartAbsent { get { return TrayInPos.Logic; } }
        public static event Action<string> CurrentShutterName;

        private enum RunState
        { Start, WaitForIPStackerReadyUnload, FirstLowerOfShutterPlate, MoveToIPStackerPos, WaitForIPStackerUnloadComplete, IPLocationLifterPlateUp, MoveToAcqPosition, MoveToOPStackerPos, WaitForOPStackerSeqComplete, CallOPStackerSeq, TmpLocation, EndSeq_MoveToIPStackerPos, EndSeqWaitForOutputStackerCompleteAtUpper, EndSeqWaitForOutputStackerCompleteAtLower,  EndSeqUpper, EndSeqLower }

        private enum InitState
        { NotInit, CheckShutterPlate, WaitInputStackerHomeComplete }

        public ShutterUnit()
        {
            EquipmentState = new GenericState();
            EquipmentState.SetState(MachineState.NotInit);
            ReserveIPStackPos = new Mutex(false, "ReserveIPStackPos");
            InitServeShutterPlateMove = new Mutex(false, "InitServeShutterPlateMove");
            initstate = InitState.NotInit;
            runstate = RunState.Start;

        }
       
        public override void RunPolling()
        {
            if (EquipmentState.CurrentState == MachineState.Run && ((runstate == RunState.MoveToAcqPosition) || (runstate == RunState.MoveToOPStackerPos)))
            {
                if (TrayInPos.Logic != false || TrayAtShutterClear.Logic != true)
                {
                    Sopdu.ProcessApps.main.MainApp.EMO.SetOutput(true);
                    this.pMode.SetError("Tray lost", true);
                }
            }
            //if (runstate == RunState.MoveToIPStackerPos&& EquipmentState.CurrentState == MachineState.Run)
            //{
            //    if ((CV_Check.Logic != true) || (!CV_Stop.Logic))
            //    {
            //        Sopdu.ProcessApps.main.MainApp.EMO.SetOutput(true);
            //        this.pMode.SetError("Inputstaker not clear", true);
            //    }
            //}
            if (Sopdu.ProcessApps.main.MainApp.EMO.Logic)
            {
                Thread.Sleep(500);
                Sopdu.ProcessApps.main.MainApp.EMO.SetOutput(false);
            }

        }
        public void MoveAxis(string PosName, int timeout, int percnt = 100)//100ms per count
        {
            try
            {
                AxisList[0].MotorAxis.StartMove(posdictionary[PosName]);
                if ("Final Pos"== PosName)
                {
                    log.Debug("line scan speed is:" + posdictionary[PosName].MaxVelocity.ToString());
                    logTool.DebugLog("line scan speed is:" + posdictionary[PosName].MaxVelocity.ToString());
                }
               
            }
            catch (Exception ex)
            {
                log.Debug("Move Error " + ProcessIdentifier + " ex: " + ex.ToString());
                logTool.DebugLog("Move Error " + ProcessIdentifier + " ex: " + ex.ToString());
            }
            int timeoutcnt = 0;
            while (!AxisList[0].MotorAxis.PositionEnd)
            {
                WaitTime(percnt);
                timeoutcnt++;
                if (timeoutcnt > timeout)
                {
                    //set error
                    //set error
                    this.pMode.SetError(AxisList[0].DisplayName + " Move " + PosName + " Timeout!", true, "ER_" + AxisList[0].MotorAxis.DisplayName + "Error");
                    this.GemCtrl.SetAlarm("ER_" + AxisList[0].MotorAxis.DisplayName + "Error");
                    log.Debug("timeout error for " + AxisList[0].DisplayName);
                    logTool.DebugLog("timeout error for " + AxisList[0].DisplayName);
                }
                //if (GlobalVar.isTrayLost)
                //{
                //    Thread.Sleep(100);
                //    AxisList[0].MotorAxis.Stop();
                //    // this.pMode.SetError("Tray lost during Moving", true);
                //}
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

        public AxisPosition MaxCoordinate
        {
            get 
            {
                return posdictionary.Values.OrderByDescending(a=>a.Coordinate).First();
            }
        }

        public void MoveAxisToMax()
        {
            MoveAxis(MaxCoordinate, 5000);
        }

        public override bool RunInitialization()
        {

            if (!mainapp.shutterInitStartQuene.TryDequeue(out _))
            {
                return false;
            }

            log.Debug($"Current Shutter {ProcessIdentifier}");
            logTool.DebugLog($"Current Shutter {ProcessIdentifier}");

            Sopdu.ProcessApps.main.MainApp.EMO.SetOutput(false);
            AxisList[0].MotorAxis.ResetCommand();
            bool plateatlowerpos = false;
            posdictionary = new Dictionary<string, AxisPosition>();
            //load positions
            foreach (AxisPosition pos in AxisList[0].MotorAxis.PositionList)
                posdictionary.Add(pos.Name, pos);
            //check motor position
            //check tray presence
            //this.WaitIOEvent(500, TrayInPos.evtOn, TrayInPos);
            MagneticEncoderRYON.Logic = false;

            this.WaitEvtOn(500, TrayInPos.evtOn, $"{ProcessIdentifier} TrayInPos Sensor Error", "ER_SH0C_E02");

            //// OtherShutterPlateDwn，OtherShutterPlateUp，CurrentShutterPlateUp，CurrentShutterPlateDwn 都为False的话， 那无法判断两个shutter的状态。 
            //if (!CurrentShutterPlateDwn.Logic && !CurrentShutterPlateUp.Logic && !OtherShutterPlateDwn.Logic && !OtherShutterPlateUp.Logic)
            //{
            //    //error
            //    log.Debug("Set Error");
            //    logTool.DebugLog("Set Error");
            //    pMode.SetError("All Shutter's Status Error", false);
            //    GemCtrl.SetAlarm("ER_SH0C_E01");
            //    this.pMode.ChkProcessMode();
            //    log.Debug("Exit Set Error");
            //    logTool.DebugLog("Exit Set Error");
            //    return false;

            //}

            if (!CurrentShutterPlateDwn.Logic && !CurrentShutterPlateUp.Logic && !OtherShutterPlateDwn.Logic && !OtherShutterPlateUp.Logic)
            {
                log.Debug($"shutter 01 and shtter02 ShutterPlateUp have no status");
                logTool.DebugLog($"shutter 01 and shtter02 ShutterPlateUp have no status");

                ShutterUnit otherShutterUnit = mainapp.Shut2;
                if (ProcessIdentifier == "ShutterUnit02")
                {
                    otherShutterUnit = mainapp.Shut1;
                }

                var currentShutterCurrentCoordinate = this.AxisList[0].MotorAxis.CurrentCoordinate;
                var otherShutterCurrentCoordinate = otherShutterUnit.AxisList[0].MotorAxis.CurrentCoordinate;
                var diff = currentShutterCurrentCoordinate - (otherShutterUnit.MaxCoordinate.Coordinate - otherShutterCurrentCoordinate);

                if (Math.Abs(diff) < GlobalVar.ShutterSaveDistance) 
                {
                    logTool.DebugLog("Shutter On Same Side And Shutters Are Not Save");
                    pMode.SetError("Shutter On Same Side And Shutters Are Not Save", false);
                    GemCtrl.SetAlarm("ER_SH0C_E01");
                    this.pMode.ChkProcessMode();

                    return false;
                }
                if (diff < 0)// 假设当前MP 的 Home点在左侧， MP当前位置相对另一个MP更小， 那当前的MP回到0点， 另一个回到最大点， 反之相反
                {
                    MoveAxis("Home", 5000);
                    otherShutterUnit.MoveAxis("Home", 5000);
                }
                else
                {
                    MoveAxisToMax();
                    otherShutterUnit.MoveAxisToMax();
                }

                valvelist["Shutter Plate CYL"].Extend();
                valvelist["Shutter Plate CYL"].WaitExtend();

                valvelist["Other Shutter Plate CYL"].Retract();
                valvelist["Other Shutter Plate CYL"].WaitRetract();
            }



            //另一个shutter 在下面， 当前shutter 不在上面， 当前shutter 上顶
            if (OtherShutterPlateDwn.Logic && !CurrentShutterPlateUp.Logic)
            {
                log.Debug($"Current Shutter Extend");
                logTool.DebugLog($"Current Shutter Extend");
                valvelist["Shutter Plate CYL"].Extend();
                valvelist["Shutter Plate CYL"].WaitExtend();
            }

            //另一个shutter 在上面， 当前shutter 不在下面面， 当前shutter 下降
            if (OtherShutterPlateUp.Logic && !CurrentShutterPlateDwn.Logic)
            {
                log.Debug($"Current Shutter Retract");
                logTool.DebugLog($"Current Shutter Retract");
                valvelist["Shutter Plate CYL"].Retract();
                valvelist["Shutter Plate CYL"].WaitRetract();
            }

            //当前shutter 在下面， 另一个shutter不在上面， 另一个shutter 上顶
            if (CurrentShutterPlateDwn.Logic && !OtherShutterPlateUp.Logic)
            {
                log.Debug($"Other Shutter Extend");
                logTool.DebugLog($"Other Shutter Extend");
                valvelist["Other Shutter Plate CYL"].Extend();
                valvelist["Other Shutter Plate CYL"].WaitExtend();
            }

            //当前shutter 在上面， 领域给shutter不在下面， 另一个shutter 下降
            if (CurrentShutterPlateUp.Logic && !OtherShutterPlateDwn.Logic)
            {
                log.Debug($"Other Shutter Retract");
                logTool.DebugLog($"Other Shutter Retract");
                valvelist["Other Shutter Plate CYL"].Retract();
                valvelist["Other Shutter Plate CYL"].WaitRetract();
            }

            if(CurrentShutterPlateUp.Logic == OtherShutterPlateUp.Logic)
            {
                //error
                log.Debug("Set Error");
                logTool.DebugLog("Set Error");
                pMode.SetError("Shutter On Same Side", false);
                GemCtrl.SetAlarm("ER_SH0C_E01");
                this.pMode.ChkProcessMode();
                log.Debug("Exit Set Error");
                logTool.DebugLog("Exit Set Error");
                return false;
            }
            if (OtherShutterPlateDwn.Logic == CurrentShutterPlateDwn.Logic)
            {
                //error
                logTool.DebugLog("Shutter On Same Side");
                pMode.SetError("Shutter On Same Side", false);
                GemCtrl.SetAlarm("ER_SH0C_E01");
                this.pMode.ChkProcessMode();
                return false;
            }

            if (CurrentShutterPlateUp.Logic == CurrentShutterPlateDwn.Logic)
            {
                logTool.DebugLog("Shutter Sensor Error");
               pMode.SetError("Shutter Sensor Error", false);
                GemCtrl.SetAlarm("ER_SH0C_E02");
                this.pMode.ChkProcessMode();
                return false;
            }
            log.Debug($"Shutter Plate CYL Check Finished");
            logTool.DebugLog($"Shutter Plate CYL Check Finished");
            valvelist["Shutter Singulator Pin CYL"].Retract() ;
            //if no error continue//
            if(CurrentShutterPlateUp.Logic)
            {
                valvelist["Shutter Plate CYL"].Extend();
                valvelist["Shutter Plate CYL"].WaitExtend();
                //set clear path for lower shutter
                this.evtShutterIPStackerLowerPositionClear.Set();
                this.evtShutterIPStackerUpperPositionClear.Set();
                this.evtShutterClearCameraLowerLocation.Set();
                this.evtShutterClearCameraUpperLocation.Set();
                this.evtShutterOPStackerLowerPositionClear.Reset(); 
                this.evtShutterOPStackerUpperPositionClear.Reset();
                evtCurrentShutterLowerClear.Reset();
            }
            else
            {
                valvelist["Shutter Plate CYL"].Retract();
                valvelist["Shutter Plate CYL"].WaitRetract();
                //set blockage to shutter that is at upper position
                this.evtShutterIPStackerLowerPositionClear.Reset();
                this.evtShutterIPStackerUpperPositionClear.Reset();
                this.evtShutterOPStackerLowerPositionClear.Reset();
                this.evtShutterOPStackerUpperPositionClear.Reset();
                this.evtShutterClearCameraLowerLocation.Reset();
                this.evtShutterClearCameraUpperLocation.Reset();
                evtCurrentShutterLowerClear.Reset();
                plateatlowerpos = true;
                
            }
            log.Debug("reach here for shutter");
            logTool.DebugLog("reach here for shutter");
            WaitEvtOnInfinite(evtOutputStackerAtSafeLocation);
            log.Debug("Shutter Wait Complete");
            logTool.DebugLog("Shutter Wait Complete");
            WaitEvtOnInfinite(evtInit_InputStackerHomeComplete);
            //MoveAxis("Final Pos", 500);
            MoveAxis("Lower PreInputStack", 5000);
            Thread.Sleep(100);
            evtShutterHomeComplete.Set();
            mainapp.shutterInitStartQuene.Enqueue("");
            if (plateatlowerpos)
                runstate = RunState.WaitForIPStackerReadyUnload;
            else
                runstate = RunState.FirstLowerOfShutterPlate;
            return true;
        }
        private Devices.SecsGem.MapData mpMapData;
        public override bool RunFunction()
        {
            //use mutex to reserve rights to go to input stacker
            switch(runstate)
            {
                case RunState.WaitForIPStackerReadyUnload:
                    runstate = WaitForIPStackerReadyUnloadFn();
                    break;
                case RunState.FirstLowerOfShutterPlate:
                    runstate = FirstLowerOfShutterPlateFn();
                    break;
                case RunState.MoveToIPStackerPos:
                    runstate = MoveToIPStackerPosFn();
                    break;
                case RunState.WaitForIPStackerUnloadComplete:
                    runstate = WaitForIPStackerUnloadCompleteFn();
                    break;
                case RunState.IPLocationLifterPlateUp:
                    runstate = IPLocationLifterPlateUpFn();
                    break;
                case RunState.MoveToAcqPosition:
                    runstate = MoveToAcqPositionFn();
                    break;
                case RunState.MoveToOPStackerPos:
                    runstate = MoveToOPStackerPosFn();
                    break;

                case RunState.CallOPStackerSeq:
                    runstate = CallOPStackerSeqFn();
                    break;
                case RunState.WaitForOPStackerSeqComplete:
                    runstate = WaitForOPStackerSeqCompleteFn();
                    break;
                case RunState.EndSeq_MoveToIPStackerPos://end seq01
                    runstate = EndSeq_MoveToIPStackerPosFn();
                    break;
                case RunState.EndSeqWaitForOutputStackerCompleteAtUpper:
                    runstate = EndSeqWaitForOutputStackerCompleteAtUpperFn();
                    break;
                case RunState.EndSeqWaitForOutputStackerCompleteAtLower:
                    runstate = EndSeqWaitForOutputStackerCompleteAtLowerFn();
                    break;
                case RunState.EndSeqUpper:
                    runstate = EndSeqUpperFn();
                    break;
                case RunState.EndSeqLower:
                    runstate = EndSeqLowerFn();
                    break;
                case RunState.TmpLocation:
                    runstate = TmpLocationFn();
                    break;
            }
            return true;
        }

        private RunState EndSeqLowerFn()
        {
            //reset seq for end lower
            this.evtShutterIPStackerLowerPositionClear.Reset();
            this.evtShutterIPStackerUpperPositionClear.Reset();
            this.evtShutterOPStackerLowerPositionClear.Reset();
            this.evtShutterOPStackerUpperPositionClear.Reset();
            evtShutterLowerSeqComplete.Set();
            evtShutterRdy.Set();
            return RunState.WaitForIPStackerReadyUnload;
        }

        private RunState EndSeqUpperFn()
        {

            this.WaitEvtOnInfinite(evtShutterLowerSeqComplete);
            evtShutterLowerSeqComplete.Reset();
 
            this.evtShutterIPStackerLowerPositionClear.Set();
            this.evtShutterIPStackerUpperPositionClear.Set();            
            this.evtShutterClearCameraLowerLocation.Set();
            this.evtShutterClearCameraUpperLocation.Set();
            this.evtShutterOPStackerLowerPositionClear.Reset();
            this.evtShutterOPStackerUpperPositionClear.Reset();
            evtShutterRdy.Set();            
            return RunState.FirstLowerOfShutterPlate;
        }

        private RunState EndSeqWaitForOutputStackerCompleteAtLowerFn()
        {
            log.Debug("Shutter End Seq Wait At Input Lower Position");
            logTool.DebugLog("Shutter End Seq Wait At Input Lower Position");
            evtShutterSignalOPStackerEndSeq.Set();
            
            //WaitEvtOnInfinite(evtOutPutStackerSignalShutterOPStackerSeqEndComplete);
            evtCurrentShutterLowerClear.Reset();

            //evtOutPutStackerSignalShutterOPStackerSeqEndComplete.Reset();
            //move to outputstacker location

            /* reset all events*/
            //need to reset to prevent upper stacker to move down
            this.evtShutterIPStackerLowerPositionClear.Reset();
            this.evtShutterIPStackerUpperPositionClear.Reset();
            this.evtShutterOPStackerLowerPositionClear.Reset();
            this.evtShutterOPStackerUpperPositionClear.Reset();
            //end event reset//
            log.Debug("Lower Shutter Move To Output Stacker Pos");
            logTool.DebugLog("Lower Shutter Move To Output Stacker Pos");
            //WaitEvtOnInfinite(evtOtherShutterOPStackerLowerPositionClear);//check lower position clear at output stacker//wait for wrong event
            //evtOtherShutterOPStackerUpperPositionClear.Reset();
            //MoveAxis("Final Pos", 5000);
            log.Debug("End Seq Lower");
            return RunState.EndSeqLower;
        }

        private RunState EndSeqWaitForOutputStackerCompleteAtUpperFn()
        {
            log.Debug("Shutter End Seq Wait At Input Upper Position");
            logTool.DebugLog("Shutter End Seq Wait At Input Upper Position");
            evtCurrentShutterLowerClear.Reset();
            evtTheOtherShutterLowerClear.Reset();
            evtShutterSignalOPStackerEndSeq.Set();//signal output stacker to engage in removal of output tray stack
           // WaitEvtOnInfinite(evtOutPutStackerSignalShutterOPStackerSeqEndComplete);//wait for traystack removal complete
            //evtOutPutStackerSignalShutterOPStackerSeqEndComplete.Reset();
            //move to outputstacker location
            log.Debug("Upper Shutter Move To Output Stacker Pos");
            logTool.DebugLog("Upper Shutter Move To Output Stacker Pos");
            //do not need this
           // WaitEvtOnInfinite(evtOtherShutterOPStackerUpperPositionClear);//check upper position clear at output stacker
           // evtOtherShutterOPStackerUpperPositionClear.Reset();
            //MoveAxis("Final Pos", 5000);
            log.Debug("End Seq Upper");
            logTool.DebugLog("End Seq Upper");
            return RunState.EndSeqUpper;
        }
        public bool CheckTray(out string errmsg)
        {
            errmsg = "";
            if (!this.WaitIOEventWithoutError(200, TrayInPos.evtOff, TrayInPos))
            {
                errmsg = "TrayInPos";
                return false;
            }

            if (!this.WaitIOEventWithoutError(200, CHK1.evtOn, CHK1))
            {
                errmsg = "CHK1";
                return false;
            }
            if (!this.WaitIOEventWithoutError(200, CHK2.evtOn, CHK1))
            {
                errmsg = "CHK2";
                return false;
            }
            if (!this.WaitIOEventWithoutError(200, CHK3.evtOn, CHK1))
            {
                errmsg = "CHK3";
                return false;
            }
            if (!this.WaitIOEventWithoutError(200, CHK4.evtOn, CHK1))
            {
                errmsg = "CHK4";
                return false;
            }
            if (!this.WaitIOEventWithoutError(200, TrayAtShutterClear.evtOn, TrayAtShutterClear))
            {
                errmsg = "TrayAtShutterClear";
                return false;
            }

            return true;
        }

        private RunState EndSeq_MoveToIPStackerPosFn()//end seq01
        {
            //check if other shutter is ready to end seq or ipstackertoppos is available
            //will need to reset all shutter path once end seq complete
            //all clear to move
            if (ProcessIdentifier == "ShutterUnit01")
            {
                while (AxisList[0].MotorAxis.CurrentCoordinate < posdictionary["Clear OutputStacker"].Coordinate)
                {
                    Thread.Sleep(5);
                    //do this temporary... may need new event for this
                   // log.Debug("Shutter 01 Motor Current Position " + AxisList[0].MotorAxis.CurrentCoordinate.ToString());
                }
                this.evtShutterOPStackerLowerPositionClear.Set();//clear for next shutter
                this.evtShutterClearCameraLowerLocation.Set();
                log.Debug("Shutter 01 Clear End SEQ");
                logTool.DebugLog("Shutter 01 Clear End SEQ");
            }
            else
            {
                while (this.AxisList[0].MotorAxis.CurrentCoordinate > posdictionary["Clear OutputStacker"].Coordinate)
                {
                    Thread.Sleep(5);
                   // log.Debug("Shutter 02 Motor Current Position " + AxisList[0].MotorAxis.CurrentCoordinate.ToString());
                }
                this.evtShutterOPStackerLowerPositionClear.Set();//clear for next shutter
                this.evtShutterClearCameraLowerLocation.Set();
                log.Debug("Shutter 02 Clear End SEQ");
                logTool.DebugLog("Shutter 02 Clear End SEQ");

            }

            //MoveAxis("Prep Pos", 10000);
            log.Debug("Lower PreInputStack Pos Move Start" + ProcessIdentifier);
            logTool.DebugLog("Lower PreInputStack Pos Move Start" + ProcessIdentifier);
            MoveAxis("Lower PreInputStack", 5000);
            log.Debug("Lower PreInputStack Pos Move Complete " + ProcessIdentifier);
            logTool.DebugLog("Lower PreInputStack Pos Move Complete " + ProcessIdentifier);
            this.evtShutterOPStackerLowerPositionClear.Set();//clear for next shutter
            this.evtShutterClearCameraLowerLocation.Set();
            //request to ip shutter for end seq... but physical motion not run yet
            bool bShutterUpAvailable = false;
            bool bOtherShutterReadyToCallOPStackerSeq = false;
            
            while(!(bShutterUpAvailable||bOtherShutterReadyToCallOPStackerSeq))
            {
                bShutterUpAvailable = WaitEvtOnWithoutErrorFire(100, evtOtherShutterIPStackerUpperPositionClear);
                bOtherShutterReadyToCallOPStackerSeq = WaitEvtOnWithoutErrorFire(100, evtOtherShutterRdyToCallOPStackerEndSeq);
                if (bOtherShutterReadyToCallOPStackerSeq)
                {
                    log.Debug("Shutter Ready to call op stacker seq");
                    logTool.DebugLog("Shutter Ready to call op stacker seq");
                    evtOtherShutterRdyToCallOPStackerEndSeq.Reset();//dont have to do anything
                    return RunState.EndSeqWaitForOutputStackerCompleteAtLower;
                }
                if (bShutterUpAvailable)
                {
                    evtOtherShutterIPStackerUpperPositionClear.Reset();
                    valvelist["Shutter Plate CYL"].Extend();
                    valvelist["Shutter Plate CYL"].WaitExtend();
                    evtShutterIPStackerLowerPositionClear.Set();
                    evtShutterRdyToCallOPStackerEndSeq.Set();
                    log.Debug("Shutter end sequence plate extend");
                    logTool.DebugLog("Shutter end sequence plate extend");
                    return RunState.EndSeqWaitForOutputStackerCompleteAtUpper;
                }


            }
            throw new Exception("unknow problem at EndSeqMoveToIPStackerPosFn");
        }

        private RunState TmpLocationFn()
        {
            return RunState.TmpLocation;
        }

        private RunState WaitForOPStackerSeqCompleteFn()
        {
            //WaitForInfinite(evt_OpStackerSeqComplete);
            //evt_OpStackerSeqComplete.Reset();
            //evt_ShutterClearOPStackerUpperLocation.Set();
            return RunState.WaitForOPStackerSeqComplete;// go to FirstLowerOfShutterPlate;
        }

        private RunState WaitForIPStackerUnloadCompleteFn()
        {
           //fire ipstacker unload sequence

            MoveAxis("Final Pos", 10000);
            return RunState.WaitForIPStackerReadyUnload;
        }

        private RunState FirstLowerOfShutterPlateFn()
        {
            //if (!CheckEvent(evtOtherShutterOPStackerLowerPositionClear))    
            log.Debug("Wait At First Lower of Shutter Plate Fn " + ProcessIdentifier);
            logTool.DebugLog("Wait At First Lower of Shutter Plate Fn " + ProcessIdentifier);
            MoveAxis("Lower PreInputStack",5000);
            WaitEvtOnInfinite(evtOtherShutterOPStackerLowerPositionClear);
            WaitEvtOnInfinite(evtTheOtherShutterLowerClear);
            evtTheOtherShutterLowerClear.Reset();
            evtOtherShutterOPStackerLowerPositionClear.Reset();
            //lifter down
            valvelist["Shutter Plate CYL"].Retract();
            valvelist["Shutter Plate CYL"].WaitRetract();
            evtShutterOPStackerUpperPositionClear.Set();
            return RunState.WaitForIPStackerReadyUnload;
        }

        private RunState WaitForIPStackerReadyUnloadFn()//seq 01
        {
            log.Debug("Shutter Enter WaitForIPSTackerReadyUnloadSEQ: " + ProcessIdentifier);
            logTool.DebugLog("Shutter Enter WaitForIPSTackerReadyUnloadSEQ: " + ProcessIdentifier);
            WaitEvtOnInfinite(evtOtherShutterClearCameraLowerLocation);
            log.Debug("Shutter Enter WaitForIPSTackerReadyUnloadSEQ evt other shutter clear: " + ProcessIdentifier);
            logTool.DebugLog("Shutter Enter WaitForIPSTackerReadyUnloadSEQ evt other shutter clear: " + ProcessIdentifier);
            AxisList[0].MotorAxis.StartMove_(posdictionary["Lower PreInputStack"]);
            evtOtherShutterClearCameraLowerLocation.Reset();
            WaitEvtOnInfinite(evtOtherShutterIPStackerLowerPositionClear);    
            evtOtherShutterIPStackerLowerPositionClear.Reset();
            log.Debug("IPStacker Lower position Check Clear " + ProcessIdentifier);
            logTool.DebugLog("IPStacker Lower position Check Clear " + ProcessIdentifier);
            //decided if its a stacking seq or end seq
            bool bstackseq = CheckEvent(evtIPStackerRdyUnload);
            if (bstackseq) 
            { 
                log.Debug("IP Stacker Ready to unload evt " + ProcessIdentifier);
                logTool.DebugLog("IP Stacker Ready to unload evt " + ProcessIdentifier);
            }
            //InputStacker Request shutter to move to ipstacker position
            bool bendseq = CheckEvent(evtIPRequestShutterMoveToIPLocation);

            while (!(bstackseq || bendseq))
            {
                bstackseq = WaitEvtOnWithoutErrorFire(100, evtIPStackerRdyUnload);
                if (bstackseq)
                {
                    log.Debug("IP Stacker evtIPStackerRdyUnload Rx " + ProcessIdentifier);
                    logTool.DebugLog("IP Stacker evtIPStackerRdyUnload Rx " + ProcessIdentifier);
                } 
                //InputStacker Request shutter to move to ipstacker position
                bendseq = WaitEvtOnWithoutErrorFire(100, evtIPRequestShutterMoveToIPLocation);
                if (bstackseq)
                { 
                    log.Debug("IP Stacker evtIPRequestShutterMoveToIPLocation Rx " + ProcessIdentifier);
                    logTool.DebugLog("IP Stacker evtIPRequestShutterMoveToIPLocation Rx " + ProcessIdentifier);
                }
                //WaitEvtOnInfinite(evtIPStackerRdyUnload);
            }
            bool movesuccess = false;
            if (bstackseq)
            {
                log.Debug("IPStackerRdyUnload Event Rx " + ProcessIdentifier);
                logTool.DebugLog("IPStackerRdyUnload Event Rx " + ProcessIdentifier);
                while (!movesuccess)
                {

                    movesuccess  = AxisList[0].MotorAxis.StartMove_(posdictionary["Prep Pos"]);
                    if (!movesuccess)
                    {
                        log.Debug("Move Cmd Fail @ " + this.ProcessIdentifier);
                        logTool.DebugLog("Move Cmd Fail @ " + this.ProcessIdentifier);
                    }
                    pMode.ChkException();
                }
                evtIPStackerRdyUnload.Reset();
                
                return RunState.MoveToIPStackerPos;
            }
            while (!movesuccess)
            {
                //movesuccess = AxisList[0].MotorAxis.StartMove_(posdictionary["Prep Pos"]);MoveAxis("Lower PreInputStack", 5000);
                movesuccess = AxisList[0].MotorAxis.StartMove_(posdictionary["Lower PreInputStack"]);
                if (!movesuccess)
                { 
                    log.Debug("Move Cmd Fail @ " + this.ProcessIdentifier);
                    logTool.DebugLog("Move Cmd Fail @ " + this.ProcessIdentifier);
                }
                pMode.ChkException();
            }
            evtIPRequestShutterMoveToIPLocation.Reset();
            log.Debug("IPStacker Request Shutter to Go IP Location For End Seq : " + ProcessIdentifier);
            logTool.DebugLog("IPStacker Request Shutter to Go IP Location For End Seq : " + ProcessIdentifier);
            return RunState.EndSeq_MoveToIPStackerPos;
        }
        private RunState MoveToIPStackerPosFn() //seq 02
        {

            if (ProcessIdentifier == "ShutterUnit01")
            {
                while (AxisList[0].MotorAxis.CurrentCoordinate < posdictionary["Clear OutputStacker"].Coordinate)
                {
                    Thread.Sleep(100);
                }
                this.evtShutterOPStackerLowerPositionClear.Set();//clear for next shutter
                this.evtShutterClearCameraLowerLocation.Set();
            }
            else
            {
                while (this.AxisList[0].MotorAxis.CurrentCoordinate > posdictionary["Clear OutputStacker"].Coordinate)
                {
                    Thread.Sleep(100);
                }
                this.evtShutterOPStackerLowerPositionClear.Set();//clear for next shutter
                this.evtShutterClearCameraLowerLocation.Set();

            }
            logTool.DebugLog("move to prep pos");
            MoveAxis("Prep Pos", 10000);
            if (!TrayInPos.Logic) pMode.SetError("TrayInPos Sensor Error", true);
            CurrentShutterName.Invoke(ProcessIdentifier);
            this.evtShutterOPStackerLowerPositionClear.Set();
            this.evtShutterClearCameraLowerLocation.Set();
            evtCurrentShutterLowerClear.Set();
            evtReqIPStackerToUnload.Set();
            return RunState.IPLocationLifterPlateUp;
        }

        private RunState IPLocationLifterPlateUpFn()//seq 03
        {
            log.Debug("wait for IPStacker unload complete event fire event fire");
            logTool.DebugLog("wait for IPStacker unload complete event fire event fire");
            WaitEvtOnInfinite(evtIPStackerUnloadComplete);
            evtIPStackerUnloadComplete.Reset();
            TrayHeight = ipstacker.CurrentTrayHeight;
            TrayID = ipstacker.CurrentTrayID;
            mpMapData = ipstacker.CurrentMapData;
            Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                if (ProcessIdentifier == "ShutterUnit01")
                    mainapp.S01Code = TrayID;
                if (ProcessIdentifier == "ShutterUnit02")
                    mainapp.S02Code = TrayID;
            });
            log.Debug("wait for IPStacker unload complete event fire event fire recieved");
            logTool.DebugLog("wait for IPStacker unload complete event fire event fire recieved");
            //this.WaitIOEvent(1500, TrayInPos.evtOff, TrayInPos);
            //this.WaitIOEvent(1500, CHK1.evtOn, CHK1);
            //this.WaitIOEvent(1500, CHK2.evtOn, CHK2);
            //this.WaitIOEvent(1500, CHK3.evtOn, CHK3);
            //this.WaitIOEvent(1500, CHK4.evtOn, CHK4);
            //this.WaitIOEvent(1500, TrayAtShutterClear.evtOn, TrayAtShutterClear);
            this.WaitEvtOn(1500, TrayInPos.evtOff, $"{ProcessIdentifier} TrayInPos Sensor Error", "ER_SH0C_E02");
            this.WaitEvtOn(1500, CHK1.evtOn, $"{ProcessIdentifier} CHK1 Sensor Error", "ER_SH0C_E02");
            this.WaitEvtOn(1500, CHK2.evtOn, $"{ProcessIdentifier} CHK2 Sensor Error", "ER_SH0C_E02");
            this.WaitEvtOn(1500, CHK3.evtOn, $"{ProcessIdentifier} CHK3 Sensor Error", "ER_SH0C_E02");
            this.WaitEvtOn(1500, CHK4.evtOn, $"{ProcessIdentifier} CHK4 Sensor Error", "ER_SH0C_E02");
            this.WaitEvtOn(1500, TrayAtShutterClear.evtOn, $"{ProcessIdentifier} TrayAtShutterClear Sensor Error", "ER_SH0C_E02");
            log.Debug("primary check on shutter complete");
            logTool.DebugLog("primary check on shutter complete");
            WaitEvtOnInfinite(evtOtherShutterIPStackerUpperPositionClear);
            evtOtherShutterIPStackerUpperPositionClear.Reset();
            valvelist["Shutter Plate CYL"].Extend();
            valvelist["Shutter Plate CYL"].WaitExtend();
           
            //if ((CV_Check.Logic != true) || (!CV_Stop.Logic))
            //{
            //    Sopdu.ProcessApps.main.MainApp.EMO.SetOutput(true);
            //    this.pMode.SetError("Inputstaker not clear", true);
            //}
            evtShutterIPStackerLowerPositionClear.Set();
            //WaitTime(100);

            valvelist["Shutter Singulator Pin CYL"].Extend();
            log.Debug("Shutter Singulator Extend");
            logTool.DebugLog("Shutter Singulator Extend");
            return RunState.MoveToAcqPosition;
        }

        private RunState MoveToAcqPositionFn()//seq 04
        {

            WaitEvtOnInfinite(evtOtherShutterClearCameraUpperLocation);
            evtOtherShutterClearCameraUpperLocation.Reset();
            TrayImageInfo info = new TrayImageInfo();
            pMode.SetInfoMsg("Set Tray ID For Acq");
            info.serialnumber = TrayID;
            info.mapdata = mpMapData;
            if (ProcessIdentifier == "ShutterUnit01")
            {
                info.inspectionid = "1";
            }
            if (ProcessIdentifier == "ShutterUnit02")
            {
                info.inspectionid = "2";
            }
          
            fifodata.Add(info);
            MagneticEncoderRYON.Logic = true;
            pMode.SetInfoMsg($"Shutter {info.inspectionid} MagneticEncoderRYON :{MagneticEncoderRYON.Logic}");
            logTool.DebugLog($"Shutter {info.inspectionid} MagneticEncoderRYON :{MagneticEncoderRYON.Logic}");
            evtReqAcq.Set();
            //set image acq
            //this.WaitIOEvent(1500, TrayInPos.evtOff, TrayInPos);
            //this.WaitIOEvent(1500, CHK1.evtOn, CHK1);
            //this.WaitIOEvent(1500, CHK2.evtOn, CHK2);
            //this.WaitIOEvent(1500, CHK3.evtOn, CHK3);
            //this.WaitIOEvent(1500, CHK4.evtOn, CHK4);
            //this.WaitIOEvent(1500, TrayAtShutterClear.evtOn, TrayAtShutterClear);
            this.WaitEvtOn(1500, TrayInPos.evtOff, $"{ProcessIdentifier} TrayInPos Sensor Error", "ER_SH0C_E02");
            this.WaitEvtOn(1500, TrayAtShutterClear.evtOn, $"{ProcessIdentifier} TrayAtShutterClear Sensor Error", "ER_SH0C_E02");
            bool movesuccess = false;
            if (!CheckEvent(evtOtherShutterOPStackerUpperPositionClear))
            {
                pMode.SetInfoMsg("Output Stacker Not Clear Shutter Go to Acq Postion****************1");
                logTool.DebugLog("Output Stacker Not Clear Shutter Go to Acq Postion");
                AxisList[0].MotorAxis.StartMove(posdictionary["Acq Position"]);
               // MoveAxis("Acq Position", 1000);
                WaitEvtOnInfinite(evtOtherShutterOPStackerUpperPositionClear);
                pMode.SetInfoMsg("Output Stacker Clear Shutter Go to Final Postion****************1");
                pMode.SetInfoMsg("Shutter Wait for Output Shutter Clear");
                logTool.DebugLog("Shutter Wait for Output Shutter Clear");
                WaitEvtOnInfinite(evtOutputStackerClearShutterToMove);
                evtOutputStackerClearShutterToMove.Reset();
                pMode.SetInfoMsg("Output Shutter Clear for Shutter****************1");
                logTool.DebugLog("Output Shutter Clear for Shutter");
                WaitEvtOnInfinite(evtReqAcqAck);
                evtReqAcqAck.Reset();
                pMode.SetInfoMsg("Acq Ack Rx****************1");
                logTool.DebugLog("Acq Ack Rx");

                while (!movesuccess)
                {
                        movesuccess = AxisList[0].MotorAxis.StartMove_(posdictionary["Final Pos"]);
                    if (!movesuccess)
                    { 
                        log.Debug("Move Cmd Fail @ " + this.ProcessIdentifier);
                        logTool.DebugLog("Move Cmd Fail @ " + this.ProcessIdentifier);
                    }
                        pMode.ChkException();
                }
                evtOtherShutterOPStackerUpperPositionClear.Reset();
            }
            else
            {
                pMode.SetInfoMsg("Output Stacker Clear Shutter Go to Final Postion****************2");
                pMode.SetInfoMsg("Shutter Wait for Output Stacker Clear");
                logTool.DebugLog("Shutter Wait for Output Stacker Clear");
                 AxisList[0].MotorAxis.StartMove(posdictionary["Acq Position"]);
               // MoveAxis("Acq Position", 1000);
                WaitEvtOnInfinite(evtOtherShutterOPStackerUpperPositionClear);
                pMode.SetInfoMsg("12345****************2");
                WaitEvtOnInfinite(evtReqAcqAck);
                evtReqAcqAck.Reset();
                pMode.SetInfoMsg("Acq Ack Rx****************2");
                WaitEvtOnInfinite(evtOutputStackerClearShutterToMove);
                evtOutputStackerClearShutterToMove.Reset();
                pMode.SetInfoMsg("Output Stacker Clear for Shutter****************2");
                logTool.DebugLog("Output Stacker Clear for Shutter");
                while (!movesuccess)
                {
                    movesuccess = AxisList[0].MotorAxis.StartMove_(posdictionary["Final Pos"]);
                    if (!movesuccess) 
                    {
                        log.Debug("Move Cmd Fail @ " + this.ProcessIdentifier);
                        logTool.DebugLog("Move Cmd Fail @ " + this.ProcessIdentifier);
                    }
                    pMode.ChkException();
                } 
                evtOtherShutterOPStackerUpperPositionClear.Reset(); 
            }
            return RunState.MoveToOPStackerPos;
        }

        private RunState MoveToOPStackerPosFn()//seq 05
        {
            //check for clear shutter to move
            //this.WaitIOEvent(1500, TrayInPos.evtOff, TrayInPos);
            //this.WaitIOEvent(1500, CHK1.evtOn, CHK1);
            //this.WaitIOEvent(1500, CHK2.evtOn, CHK2);
            //this.WaitIOEvent(1500, CHK3.evtOn, CHK3);
            //this.WaitIOEvent(1500, CHK4.evtOn, CHK4);
            // this.WaitIOEvent(1500, TrayAtShutterClear.evtOn, TrayAtShutterClear);
            this.WaitEvtOn(1500, TrayAtShutterClear.evtOn, $"{ProcessIdentifier} TrayAtShutterClear  Sensor Error", "ER_SH0C_E02");
            this.WaitEvtOn(1500, TrayInPos.evtOff, $"{ProcessIdentifier} TrayInPos Sensor Error", "ER_SH0C_E02");
            MoveAxis("Final Pos", 1000);
            MagneticEncoderRYON.Logic = false;

            pMode.SetInfoMsg($"MagneticEncoderRYON :{MagneticEncoderRYON.Logic}");
            logTool.DebugLog($"MagneticEncoderRYON :{MagneticEncoderRYON.Logic}");
            log.Debug("shutter speed set"+posdictionary["Final Pos"].MaxVelocity.ToString());
            logTool.DebugLog("shutter speed set" + posdictionary["Final Pos"].MaxVelocity.ToString());
            evtShutterClearCameraUpperLocation.Set();
            evtShutterIPStackerUpperPositionClear.Set();
            valvelist["Shutter Singulator Pin CYL"].Retract();
            WaitTime(100);
            log.Debug("Tray In Position");
            logTool.DebugLog("Tray In Position");
            return RunState.CallOPStackerSeq;
        }
        private RunState CallOPStackerSeqFn()//seq 06
        {
            //this.WaitIOEvent(1500, TrayInPos.evtOff, TrayInPos);
            //this.WaitIOEvent(1500, CHK1.evtOn, CHK1);
            //this.WaitIOEvent(1500, CHK2.evtOn, CHK2);
            //this.WaitIOEvent(1500, CHK3.evtOn, CHK3);
            //this.WaitIOEvent(1500, CHK4.evtOn, CHK4);
           // this.WaitIOEvent(1500, TrayAtShutterClear.evtOn, TrayAtShutterClear);
            this.WaitEvtOn(1500, TrayAtShutterClear.evtOn, $"{ProcessIdentifier} TrayAtShutterClear Sensor Error", "ER_SH0C_E02");
            this.WaitEvtOn(1500, TrayInPos.evtOff, $"{ProcessIdentifier} TrayInPos Sensor Error", "ER_SH0C_E02");
            //at output stacker location
            log.Debug("Call Output Stacker Seq");
            logTool.DebugLog("Call Output Stacker Seq");
            opstacker.TrayHeight = TrayHeight;
            //currently need to check for lower position clear before call op stacker
            //if shutter able to move freely we do not need to do this check
            /*old code to check shutter clear before calling stacker*/
            /*
            if (CheckEvent(evtOtherShutterOPStackerLowerPositionClear))
            {
                evtOutputStackerStartSeq.Set();//this will need to be remove if lower plate can be move freely
                evtOtherShutterOPStackerLowerPositionClear.Reset();
            }
            else
            {
                WaitEvtOnInfinite(evtOtherShutterOPStackerLowerPositionClear);
                evtOutputStackerStartSeq.Set();//this will need to be remove if lower plate can be move freely
                log.Debug("OtherShutterOPStacker Clear");
                evtOtherShutterOPStackerLowerPositionClear.Reset();
            }
            */


            /*new code to call for output shutter*/
            evtOutputStackerStartSeq.Set();//call for output stacker to move
            WaitEvtOnInfinite(evtOutputStackerClearShutterPlate);
            evtOutputStackerClearShutterPlate.Reset();
            /*new code to call output stacker before lower shutter clear*/
            WaitEvtOnInfinite(evtOtherShutterOPStackerLowerPositionClear);
            evtOtherShutterOPStackerLowerPositionClear.Reset();
            /*new code to check other shutter clear lower position*/
            valvelist["Shutter Plate CYL"].Retract();
            valvelist["Shutter Plate CYL"].WaitRetract();            
            //WaitEvtOnInfinite(evtOutputStackerClearShutterToMove);
            evtShutterOPStackerUpperPositionClear.Set();
            
            return RunState.WaitForIPStackerReadyUnload;
        }

        private DiscreteIO CHK1, CHK2, CHK3, CHK4, TrayInPos, CV_Stop, CV_Check,
            TrayAtShutterClear, SingulatorPinFwd,SingulatorPinBwd,
            OtherShutterPlateUp, OtherShutterPlateDwn,
            CurrentShutterPlateUp, CurrentShutterPlateDwn,TrayEnterSensor;
        private Dictionary<string, AxisPosition> posdictionary;
        public InputStacker ipstacker;
        public OutputStacker opstacker;
        private string TrayID;
        private int TrayHeight;
        [XmlIgnore]
        public main.MainApp mainapp;
        [XmlIgnore]
        public System.Collections.Concurrent.ConcurrentBag<Devices.CameraLink.TrayImageInfo> fifodata;

        protected override void InitInput()//initialize input
        {
            base.InitInput();
            CHK1 = this.inputlist.IpDirectory[InputNameList[6]];
            CHK2 = this.inputlist.IpDirectory[InputNameList[7]];
            CHK3 = this.inputlist.IpDirectory[InputNameList[8]];
            CHK4 = this.inputlist.IpDirectory[InputNameList[9]];
            TrayInPos = this.inputlist.IpDirectory[InputNameList[10]];
            TrayAtShutterClear = this.inputlist.IpDirectory[InputNameList[11]];
            SingulatorPinBwd = this.inputlist.IpDirectory[InputNameList[5]];
            SingulatorPinFwd = this.inputlist.IpDirectory[InputNameList[4]];
            OtherShutterPlateUp = this.inputlist.IpDirectory[InputNameList[3]];
            OtherShutterPlateDwn = this.inputlist.IpDirectory[InputNameList[2]];
            CurrentShutterPlateUp = this.inputlist.IpDirectory[InputNameList[1]];
            CurrentShutterPlateDwn = this.inputlist.IpDirectory[InputNameList[0]];
            TrayEnterSensor = this.inputlist.IpDirectory[InputNameList[12]];
            CV_Stop = this.inputlist.IpDirectory["Input21"];
            CV_Check = this.inputlist.IpDirectory["Input10"];

        }
        
        protected override void InitEvt()//initialize events
        {
            base.InitEvt();
            Dictionary<string, ProcessEvt> evtdict = new Dictionary<string, ProcessEvt>();
            foreach (ProcessEvt e in Evtlist)
            {
                evtdict.Add(e.Name, e);
            }
            evtOutputStackerAtSafeLocation = evtdict[EvtNameList[0]].evt;
            evtInit_InputStackerHomeComplete = evtdict[EvtNameList[1]].evt;
            evtIPStackerRdyUnload = evtdict[EvtNameList[2]].evt;
            evtReqIPStackerToUnload = evtdict[EvtNameList[3]].evt;
            evtIPStackerUnloadComplete = evtdict[EvtNameList[4]].evt;

            evtShutterIPStackerLowerPositionClear = evtdict[EvtNameList[5]].evt; 
            evtShutterIPStackerUpperPositionClear = evtdict[EvtNameList[6]].evt; 
            evtShutterOPStackerLowerPositionClear = evtdict[EvtNameList[7]].evt;
            evtShutterOPStackerUpperPositionClear = evtdict[EvtNameList[8]].evt;

            evtOtherShutterIPStackerLowerPositionClear = evtdict[EvtNameList[9]].evt;
            evtOtherShutterIPStackerUpperPositionClear = evtdict[EvtNameList[10]].evt;
            evtOtherShutterOPStackerLowerPositionClear = evtdict[EvtNameList[11]].evt;
            evtOtherShutterOPStackerUpperPositionClear = evtdict[EvtNameList[12]].evt;

            evtOutputStackerStartSeq = evtdict[EvtNameList[13]].evt;
            evtOutputStackerClearShutterPlate = evtdict[EvtNameList[14]].evt;
            evtOutputStackerClearShutterToMove = evtdict[EvtNameList[15]].evt;
            evtShutterRdy = evtdict[EvtNameList[16]].evt;
            evtShutterSignalOPStackerEndSeq = evtdict[EvtNameList[17]].evt;
            evtIPRequestShutterMoveToIPLocation = evtdict[EvtNameList[18]].evt;
            evtShutterRdyToCallOPStackerEndSeq = evtdict[EvtNameList[19]].evt;
            evtOtherShutterRdyToCallOPStackerEndSeq = evtdict[EvtNameList[20]].evt;
            evtOutPutStackerSignalShutterOPStackerSeqEndComplete = evtdict[EvtNameList[21]].evt;
            evtShutterClearCameraUpperLocation = evtdict[EvtNameList[22]].evt;
            evtShutterClearCameraLowerLocation = evtdict[EvtNameList[23]].evt;
            evtOtherShutterClearCameraUpperLocation = evtdict[EvtNameList[24]].evt;
            evtOtherShutterClearCameraLowerLocation = evtdict[EvtNameList[25]].evt;
            evtReqAcq = evtdict[EvtNameList[26]].evt;
            evtReqAcqAck = evtdict[EvtNameList[27]].evt;
            evtShutterHomeComplete = evtdict[EvtNameList[28]].evt;
            evtCurrentShutterLowerClear = evtdict[EvtNameList[29]].evt;
            evtTheOtherShutterLowerClear = evtdict[EvtNameList[30]].evt;
            evtShutterLowerSeqComplete = evtdict[EvtNameList[31]].evt;
        }

        protected override void InitOutput()//initialize output
        {
            base.InitOutput();

            MagneticEncoderRYON = this.outputlist.IpDirectory[OutputNameList[4]];
        }


        public void ShutterSingulatorPinCYL()
        {
            valvelist["Shutter Singulator Pin CYL"].Extend();
            //valvelist["Shutter Singulator Pin CYL"].WaitExtend();

            Thread.Sleep(600);

            valvelist["Shutter Singulator Pin CYL"].Retract();
            valvelist["Shutter Singulator Pin CYL"].WaitRetract();
            Thread.Sleep(300);
        }
    }
}