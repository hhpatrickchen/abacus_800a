using Insphere.Connectivity.Application.SecsToHost;
using LogPanel;
using Sopdu.Devices.Cylinders;
using Sopdu.Devices.KeyenceDistanceSensor;
using Sopdu.Devices.MotionControl.Base;
using Sopdu.Devices.MotionControl.IAIController.PConAxis;
using Sopdu.Devices.Vision;
using Sopdu.helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace Sopdu.ProcessApps.ProcessModules
{
    public class OutputStacker : Process
    {
        LogTool<OutputStacker> logTool = new LogTool<OutputStacker>();
        public OutputStacker()
        {
            EquipmentState = new GenericState();
            EquipmentState.SetState(MachineState.NotInit);
        }

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public override bool RunNotInitSequence()
        {
            if (evtOutputStackerAtSafeLocation != null)
            {
                evtOutputStackerAtSafeLocation.Reset();//resetting all events
                return true;
            }
            return false;
        }


        public override bool RunInitialization()
        {
            StackCount = 0;
            Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                mainapp.OpTrayCount = StackCount.ToString();
            });

            posdictionary = new Dictionary<string, AxisPosition>();
            //load positions
            foreach (AxisPosition pos in AxisList[0].MotorAxis.PositionList)
                posdictionary.Add(pos.Name, pos);

            MoveAxis("Home", 5000);
            //check motor position
            this.pMode.SetInfoMsg("Check Tray Clear");
            logTool.InfoLog("Check Tray Clear");
            CheckTrayClear();
            this.pMode.SetInfoMsg("End of Check Tray Clear");
            logTool.InfoLog("End of Check Tray Clear");
            valvelist["Output Benching Bar"].Retract();
            valvelist["Output Benching Bar"].WaitRetract();
            valvelist["Output Singulator"].Extend();
            valvelist["Output Singulator"].WaitExtend();
            TrigShutDoor(false);
            //valvelist["Output Shutter Door"].Extend();
            //valvelist["Output Shutter Door"].WaitExtend();
            if (AxisList[0].MotorAxis.CurrentCoordinate <= AxisList[0].MotorAxis.PositionList[1].Coordinate)
            {
                string str = AxisList[0].MotorAxis.ErrorCode;
                if (str == "0000")
                    evtOutputStackerAtSafeLocation.Set();
                else
                {
                    pMode.SetError("Output Stacker Servo Error", false);
                    logTool.ErrorLog("Output Stacker Servo Error");
                    this.GemCtrl.SetAlarm("ER_" + AxisList[0].MotorAxis.DisplayName + "Error");
                    pMode.ChkProcessMode();
                }
            }
            else
            {
                pMode.SetError("Output Stacker Not At Safe Location for Home", false);
                logTool.ErrorLog("Output Stacker Not At Safe Location for Home");
                GemCtrl.SetAlarm("ER_OPST_E02");
                pMode.ChkProcessMode();
            }
            WaitEvtOnInfinite(evtShutter01HomeComplete);
            evtShutter01HomeComplete.Reset();
            WaitEvtOnInfinite(evtShutter02HomeComplete);
            evtShutter02HomeComplete.Reset();

            // 两个shutter 都初始化OK，清空队列
            while (!mainapp.shutterInitStartQuene.IsEmpty)
            {
                mainapp.shutterInitStartQuene.TryDequeue(out _);
            }
            this.pMode.SetInfoMsg("Stacker go to Pre Engage");
            logTool.InfoLog("Stacker go to Pre Engage");
            MoveAxis("Shutter PreEngage", 500);
            evtOutputStackerClearShutterToMove.Set();
            RunTimeData.kvpOPStkr.Clear();
            runstate = RunState.WaitForStackRequest;
            return true;
        }

        public void CheckTrayClear()
        {
            if (TrayClearSensor.Logic) pMode.SetInfoMsg("Tray Clear Sensor On"); else pMode.SetInfoMsg("Tray Clear Sensor OFf");
            if (TrayStopSensor.Logic) pMode.SetInfoMsg("Tray stop Sensor On"); else pMode.SetInfoMsg("Tray stop Sensor OFf");
            if (!(TrayClearSensor.Logic && TrayStopSensor.Logic))
            {
                this.pMode.SetError("Tray Not Clear When Home", false);
                logTool.ErrorLog("Tray Not Clear When Home");
                GemCtrl.SetAlarm("ER_OPST_E01");
                this.pMode.ChkProcessMode();
            }
        }
        public void MoveAxis(string PosName, int timeout, int percnt = 100)//100ms per count
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
                    this.pMode.SetError("MP4 Move " + PosName + " Timeout!", true);
                    this.GemCtrl.SetAlarm("ER_" + AxisList[0].MotorAxis.DisplayName + "Error");
                    log.Debug("timeout error for MP4");
                    logTool.DebugLog("timeout error for MP4");
                }
                this.pMode.ChkProcessMode();
            }
        }
        public void MoveAxis(AxisPosition pos, int timeout, int percnt = 100)//100ms per count
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
                    this.pMode.SetError("MP4 Move " + pos.Name + " Timeout!", true);
                    this.GemCtrl.SetAlarm("ER_" + AxisList[0].MotorAxis.DisplayName + "Error");
                    log.Debug("timeout error for MP4");
                    logTool.DebugLog("timeout error for MP4");
                }
                this.pMode.ChkProcessMode();
            }
        }

        private RunState runstate;
        private enum RunState
        { WaitForStackRequest, MoveToTrayPreEngagePos, MoveToTrayEngagePos, MoveToPreStackPos, MoveToStackUpPos, MoveToFinalStackPos, MoveToHomePosition, StartEndSeq, tmpPosition }

        public override bool RunFunction()
        {
            log.Debug("OutputCV at " +runstate);
            switch (runstate)
            {
                case RunState.WaitForStackRequest:
                    runstate = WaitForStackRequestFn();
                    break;
                case RunState.MoveToTrayPreEngagePos:
                    runstate = MoveToTrayPreEnagePosFn();
                    break;
                case RunState.MoveToTrayEngagePos:
                    runstate = MoveToTrayEngagePosFn();
                    break;
                case RunState.MoveToPreStackPos:
                    runstate = MoveToPreStackPosFn();
                    break;
                case RunState.MoveToStackUpPos:
                    runstate = MoveToStackUpPosFn();
                    break;
                case RunState.MoveToFinalStackPos:
                    runstate = MoveToFinalStackPosFn();
                    break;
                case RunState.MoveToHomePosition:
                    runstate = MoveToHomePositionFn();
                    break;
                case RunState.StartEndSeq:
                    runstate = StartEndSeqFn();
                    break;
            }
            return true;
        }

        protected override void StoppingLogicFn()
        {
            //set all motor current state
            bM2Fwdbackup = M2Fwd.Logic;
            //set all motor to stop
            M2Fwd.Logic = false;
        }
        protected override void RecoverFromStopFn()
        {
            base.RecoverFromStopFn();
            M2Fwd.Logic = bM2Fwdbackup;
        }

        private RunState StartEndSeqFn()
        {
            //move axis to stack preengage
            MoveAxis("Stack PreEngaged", 1000);
            log.Debug("Output Stacker to Stack PreEngage");
            logTool.DebugLog("Output Stacker to Stack PreEngage");
            //move axis to stack engage
            MoveAxis("Stack Engaged", 1000);
            log.Debug("Output Stacker to Stack Engage");
            logTool.DebugLog("Output Stacker to Stack Engage");
            //singulator open
            valvelist["Output Singulator"].Retract();
            valvelist["Output Singulator"].WaitRetract();
            log.Debug("Output singualtor retracted");
            logTool.DebugLog("Output singualtor retracted");
            //move axis to preconveyor position
            valvelist["Output Benching Bar"].Extend();
            valvelist["Output Benching Bar"].WaitExtend();
            MoveAxis("Conveyor PreEngaged", 1000);
            log.Debug("Output Stacker at Conveyor PreEngage Position");
            logTool.DebugLog("Output Stacker at Conveyor PreEngage Position");
            //move axis to home            
            //valvelist["Output Singulator"].Extend();
            //log.Debug("Output singualtor Extend");
            MoveAxis("Conveyor Engaged", 1000);
            // valvelist["Output Singulator"].WaitExtend();
            log.Debug("Home Complete");
            logTool.DebugLog("Home Complete");
            TrigShutDoor(true);
            //valvelist["Output Shutter Door"].Retract();
            //valvelist["Output Shutter Door"].WaitRetract();
            //request output cv to run
            pMode.SetInfoMsg("Output Stacker Trigger Output CV");
            evtOPStackerRequestOPCVRun.Set();
            WaitEvtOnInfinite(evtOPStackerRequestOPCVRunAck);//infinite because there may be a tray at output stacker
            evtOPStackerRequestOPCVRunAck.Reset();
            //check for onlineoffline
            bool bTrayClear = false;
            bool bTrayStop = false;

            /*
            if ((GemCtrl.gemController.CommunicationState != CommunicationState.Disabled)||ignoreGemDisable)
            {
                M2Fwd.Logic = true;
                WaitEvtOn(50000, evtOPCVRunComplete, "Wait For Output Conveyor Run Complete Event", "ER_OPST_E03");
                evtOPCVRunComplete.Reset();
            }
            else
            {
                bool bescape = false;
                //do the retry here.. any event will do
                while ((mainapp.pMaster.EquipmentState.CurrentState == MachineState.WarnStop) || (mainapp.pMaster.EquipmentState.CurrentState == MachineState.Warning))
                {
                    pMode.ChkProcessMode();
                    Thread.Sleep(100);
                }
                while (!bescape)
                {
                    bool bAckRx = true;
                    WaitEvtOnWarn(1, evtOPStackerRequestOPCVRunAck, out bAckRx,
                        "Manually Remove The Stack, Equipment not online",
                        "ER_OPSTACKERWARN", true, false,
                        "Tray Remove Ack");
                    pMode.ChkProcessMode();
                    //check all sensors
                    bTrayClear = WaitEvtOnWithoutErrorFire(100, TrayClearSensor.evtOn);
                    bTrayStop = WaitEvtOnWithoutErrorFire(100, TrayStopSensor.evtOn);
                    if (bTrayClear && bTrayStop) break;
                }

            }            
            //*/

            M2Fwd.Logic = true;
            WaitEvtOn(50000, evtOPCVRunComplete, "Wait For Output Conveyor Run Complete Event", "ER_OPST_E03");
            evtOPCVRunComplete.Reset();
            M2Fwd.Logic = false;

            if ((TrayClearSensor.Logic== false) || (TrayStopSensor.Logic == false)||(PresenceCHK2Sensor.Logic== false) ||(TrayEnterSensor.Logic== false))
            {
                pMode.SetError("OputStacker tray transfer wrong", true);
                logTool.ErrorLog("OputStacker tray transfer wrong");
            }
            TrigShutDoor(false);
            //valvelist["Output Shutter Door"].Extend();
            ///alvelist["Output Shutter Door"].WaitExtend();
            bTrayClear = false;
            bTrayStop = false;

            while (!(bTrayClear && bTrayStop))
            {
                bTrayClear = WaitEvtOnWithoutErrorFire(100, TrayClearSensor.evtOn);
                bTrayStop = WaitEvtOnWithoutErrorFire(100, TrayStopSensor.evtOn);
            }

            valvelist["Output Benching Bar"].Retract();
            valvelist["Output Singulator"].Extend();

            valvelist["Output Benching Bar"].WaitRetract();
            valvelist["Output Singulator"].WaitExtend();
            StackCount = 0;
            Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                mainapp.OpTrayCount = StackCount.ToString();
            });
            //fire to request shutter to move back
            MoveAxis("Shutter PreEngage", 500);
            logTool.DebugLog("Shutter PreEngage");
            evtOutPutStackerSignalShutter01OPStackerSeqEndComplete.Set();
            evtOutPutStackerSignalShutter02OPStackerSeqEndComplete.Set();
            evtOutputStackerClearShutterToMove.Set();
            return RunState.WaitForStackRequest;
            //throw new NotImplementedException();
        }

        private RunState MoveToHomePositionFn()
        {
            StackCount = StackCount + 1;
            Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                mainapp.OpTrayCount = StackCount.ToString();
            });
            MoveAxis("Shutter PreEngage", 10000);
            logTool.DebugLog("Shutter PreEngage");
            evtOutputStackerClearShutterToMove.Set();
            return RunState.WaitForStackRequest;
        }

        private RunState MoveToFinalStackPosFn()
        {
            MoveAxis("Stack PreEngaged", 1000);
            logTool.DebugLog("Stack PreEngaged");
            return RunState.MoveToHomePosition;
        }

        private RunState MoveToStackUpPosFn()
        {
            log.Debug("OutputStacker Tray Height " + TrayHeight.ToString());
            logTool.DebugLog("OutputStacker Tray Height " + TrayHeight.ToString());

            //AxisPosition pos = new AxisPosition()//setup preengage position
            //{
            //    AccTime = posdictionary["Stack Engaged"].AccTime,
            //    Coordinate = posdictionary["Stack Engaged"].Coordinate,
            //    DecTime = posdictionary["Stack Engaged"].DecTime,
            //    InPositionRange = posdictionary["Stack Engaged"].InPositionRange,
            //    IsRelativePosition = posdictionary["Stack Engaged"].IsRelativePosition,
            //    MaxVelocity = posdictionary["Stack Engaged"].MaxVelocity,
            //    StartVelocity = posdictionary["Stack Engaged"].StartVelocity,
            //    Name = "VariableEngagePos"
            //};

            /* tmp remove 1st April
            AxisPosition pos = new AxisPosition()//setup preengage position
            {
                AccTime = posdictionary["Stack Engaged"].AccTime,
                Coordinate = posdictionary["Stack Engaged"].Coordinate,
                DecTime = posdictionary["Stack Engaged"].DecTime,
                InPositionRange = posdictionary["Stack Engaged"].InPositionRange,
                IsRelativePosition = posdictionary["Stack Engaged"].IsRelativePosition,
                MaxVelocity = posdictionary["Stack Engaged"].MaxVelocity,
                StartVelocity = posdictionary["Stack Engaged"].StartVelocity,
                Name = "VariableEngagePos"
            };
            pos.Coordinate = pos.Coordinate - TrayHeight;
            MoveAxis(pos, 1000);
            */

            return RunState.MoveToFinalStackPos;

        }

        private RunState MoveToPreStackPosFn()
        {

            //AxisPosition pos = new AxisPosition()//setup preengage position
            //{
            //    AccTime = posdictionary["Stack PreEngaged"].AccTime,
            //    Coordinate = posdictionary["Stack PreEngaged"].Coordinate,
            //    DecTime = posdictionary["Stack PreEngaged"].DecTime,
            //    InPositionRange = posdictionary["Stack PreEngaged"].InPositionRange,
            //    IsRelativePosition = posdictionary["Stack PreEngaged"].IsRelativePosition,
            //    MaxVelocity = posdictionary["Stack PreEngaged"].MaxVelocity,
            //    StartVelocity = posdictionary["Stack PreEngaged"].StartVelocity,
            //    Name = "VariablePreEngagePos"
            //};

            AxisPosition pos = new AxisPosition()//setup preengage position
            {
                AccTime = posdictionary["Stack Engaged"].AccTime,
                Coordinate = posdictionary["Stack Engaged"].Coordinate,
                DecTime = posdictionary["Stack Engaged"].DecTime,
                InPositionRange = posdictionary["Stack Engaged"].InPositionRange,
                IsRelativePosition = posdictionary["Stack Engaged"].IsRelativePosition,
                MaxVelocity = posdictionary["Stack Engaged"].MaxVelocity,
                StartVelocity = posdictionary["Stack Engaged"].StartVelocity,
                Name = "VariableEngagePos"
            };
            pos.Coordinate = pos.Coordinate - TrayHeight;
            MoveAxis(pos, 1000);
            //allow shutter to move lifter down
            evtOutputStackerClearShutterPlate.Set();
            MoveAxis("Stack PreEngaged", 1000);
            logTool.DebugLog("Stack PreEngaged");

            return RunState.MoveToStackUpPos;
        }

        private RunState MoveToTrayEngagePosFn()
        {
            //throw new NotImplementedException();
            MoveAxis("Shutter Engaged", 1000);
            return RunState.MoveToPreStackPos;
        }

        private RunState MoveToTrayPreEnagePosFn()
        {
            // throw new NotImplementedException();
            //MoveAxis("Shutter PreEngage", 1000);//Shutter PreEngage
            return RunState.MoveToTrayEngagePos;
        }

        private RunState WaitForStackRequestFn()
        {
            bool bOutputStackerStartSeq = false;
            bool bOutputStackerEndSeqShutter01Req = false;
            bool bOutputStackerEndSeqShutter02Req = false;
            log.Debug("Output Stacker Enter Wait For Stack Req");
            logTool.DebugLog("Output Stacker Enter Wait For Stack Req");
            while (!(bOutputStackerStartSeq || (bOutputStackerEndSeqShutter01Req && bOutputStackerEndSeqShutter02Req)))
            {
                bOutputStackerEndSeqShutter01Req = WaitEvtOnWithoutErrorFire(100, evtShutter01SignalOPStackerEndSeq);
                bOutputStackerEndSeqShutter02Req = WaitEvtOnWithoutErrorFire(100, evtShutter02SignalOPStackerEndSeq);
                if (bOutputStackerEndSeqShutter01Req && bOutputStackerEndSeqShutter02Req)
                {
                    evtShutter01SignalOPStackerEndSeq.Reset();
                    evtShutter02SignalOPStackerEndSeq.Reset();
                    log.Debug("Output Stacker Go to Start End Seq");
                    logTool.DebugLog("Output Stacker Go to Start End Seq");
                    return RunState.StartEndSeq;
                }

                bOutputStackerStartSeq = WaitEvtOnWithoutErrorFire(100, evtOutputStackerStartSeq);
                if (bOutputStackerStartSeq)
                {
                    evtOutputStackerStartSeq.Reset();
                    log.Debug("Output Stacker Req Recieved Move To Tray Pre-Engaged");
                    logTool.DebugLog("Output Stacker Req Recieved Move To Tray Pre-Engaged");
                    //return RunState.MoveToTrayPreEngagePos;
                    break;
                }
            }
            MoveAxis("Shutter Engaged", 1000);
            logTool.DebugLog("Move To Shutter Engaged");
            evtOutputStackerClearShutterPlate.Set();
            //move to pre-engage position
            AxisPosition pos = new AxisPosition()//setup preengage position
            {
                AccTime = posdictionary["Stack PreEngaged"].AccTime,
                Coordinate = posdictionary["Stack PreEngaged"].Coordinate,
                DecTime = posdictionary["Stack PreEngaged"].DecTime,
                InPositionRange = posdictionary["Stack PreEngaged"].InPositionRange,
                IsRelativePosition = posdictionary["Stack PreEngaged"].IsRelativePosition,
                MaxVelocity = posdictionary["Stack PreEngaged"].MaxVelocity,
                StartVelocity = posdictionary["Stack PreEngaged"].StartVelocity,
                Name = "VariablePreEngagePos"
            };
            pos.Coordinate = pos.Coordinate - TrayHeight;
            MoveAxis(pos, 1000);

            //end
            pos = new AxisPosition()//setup preengage position
            {
                AccTime = posdictionary["Stack Engaged"].AccTime,
                Coordinate = posdictionary["Stack Engaged"].Coordinate,
                DecTime = posdictionary["Stack Engaged"].DecTime,
                InPositionRange = posdictionary["Stack Engaged"].InPositionRange,
                IsRelativePosition = posdictionary["Stack Engaged"].IsRelativePosition,
                MaxVelocity = posdictionary["Stack Engaged"].MaxVelocity,
                StartVelocity = posdictionary["Stack Engaged"].StartVelocity,
                Name = "VariableEngagePos"
            };
            pos.Coordinate = pos.Coordinate - TrayHeight;
            MoveAxis(pos, 1000);
            //allow shutter to move lifter down
            //old clear shutter plate event
            //evtOutputStackerClearShutterPlate.Set();
            //this.WaitIOEvent(1000, CHK1.evtOn, CHK1);
            //this.WaitIOEvent(1000, CHK2.evtOn, CHK2);
            //this.WaitIOEvent(1000, CHK3.evtOn, CHK3);
            //this.WaitIOEvent(1000, CHK4.evtOn, CHK4);
            this.WaitEvtOn(1000, CHK1.evtOn, "OutputStacker CHK1  Sensor Error", "ER_OPST_E04");
            this.WaitEvtOn(1000, CHK2.evtOn, "OutputStacker CHK2 Sensor Error", "ER_OPST_E04");
            this.WaitEvtOn(1000, CHK3.evtOn, "OutputStacker CHK3  Sensor Error", "ER_OPST_E04");
            this.WaitEvtOn(1000, CHK4.evtOn, "OutputStacker CHK4 Sensor Error", "ER_OPST_E04");
            MoveAxis("Stack PreEngaged", 1000);
            logTool.DebugLog("Move To Stack PreEngaged");

            StackCount = StackCount + 1;
            Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                mainapp.OpTrayCount = StackCount.ToString();
            });
            MoveAxis("Shutter PreEngage", 10000);
            logTool.DebugLog("Move to Shutter PreEngage");
            //this.WaitIOEvent(1000, CHK1.evtOn, CHK1);
            //this.WaitIOEvent(1000, CHK2.evtOn, CHK2);
            //this.WaitIOEvent(1000, CHK3.evtOn, CHK3);
            //this.WaitIOEvent(1000, CHK4.evtOn, CHK4);
            //this.WaitIOEvent(1000, TrayEnterSensor.evtOff, TrayEnterSensor);
            //this.WaitIOEvent(1000, PresenceCHK2Sensor.evtOff, PresenceCHK2Sensor);
            this.WaitEvtOn(1000, CHK1.evtOn, "OutputStacker CHK1 Sensor Error", "ER_OPST_E04");
            this.WaitEvtOn(1000, CHK2.evtOn, "OutputStacker CHK2 Sensor Error", "ER_OPST_E04");
            this.WaitEvtOn(1000, CHK3.evtOn, "OutputStacker CHK3 Sensor Error", "ER_OPST_E04");
            this.WaitEvtOn(1000, CHK4.evtOn, "OutputStacker CHK4 Sensor Error", "ER_OPST_E04");
            this.WaitEvtOn(1000, TrayEnterSensor.evtOff, "OutputStacker TrayEnterSensor  Error", "ER_OPST_E04");
            this.WaitEvtOn(1000, PresenceCHK2Sensor.evtOff, "OutputStacker PresenceCHK2Sensor Error", "ER_OPST_E04");

            if (EnableKeyence)
            {
                var result1 = KeyenceS1.SendCommand();
                var result2 = KeyenceS2.SendCommand();
                var result3 = KeyenceS1.ReadFromServer();
                var result4 = KeyenceS2.ReadFromServer();
                var result5 = int.TryParse(KeyenceS1.strReceived, out int s1Received);
                var result6 = int.TryParse(KeyenceS2.strReceived, out int s2Received);

                log.Debug($"KeyenceS1.strReceived={KeyenceS1.strReceived}");
                log.Debug($"KeyenceS2.strReceived={KeyenceS2.strReceived}");
                if (result1 && result2 && result3 && result4 && result5 && result6)
                {
                    if (Math.Abs(s1Received - s2Received) > 1000)
                    {
                        pMode.SetError("The tray is tilted status", true);
                        log.Error("The tray is tilted status");
                    }
                }
                else
                {
                    pMode.SetError("Read Keyence sensor distance error ", true);
                    log.Error("Read Keyence sensor distance error ");
                }
            }
            

            evtOutputStackerClearShutterToMove.Set();
            return RunState.WaitForStackRequest;
            //return RunState.MoveToTrayPreEngagePos;//remove this sequence
        }

        //<string>evtOPStackerRequestOPCVRun</string>
        //<string>evtOPStackerRequestOPCVRunAck</string>
        //<string>evtOPCVRunComplete</string>
        public ManualResetEvent
            evtOutputStackerAtSafeLocation, evtOutputStackerStartSeq, evtOutputStackerClearShutterPlate,
            evtOutputStackerClearShutterToMove, evtShutter01SignalOPStackerEndSeq, evtShutter02SignalOPStackerEndSeq,
            evtOutPutStackerSignalShutter01OPStackerSeqEndComplete, evtOutPutStackerSignalShutter02OPStackerSeqEndComplete,
            evtTmpDebugEvent, evtOPStackerRequestOPCVRun, evtOPStackerRequestOPCVRunAck, evtOPCVRunComplete, evtShutter01HomeComplete, evtShutter02HomeComplete;
        private Dictionary<string, AxisPosition> posdictionary;
        private Devices.IOModule.DiscreteIO TrayClearSensor;
        private Devices.IOModule.DiscreteIO TrayStopSensor;
        private Devices.IOModule.DiscreteIO CHK1;
        private Devices.IOModule.DiscreteIO CHK2;
        private Devices.IOModule.DiscreteIO CHK3;
        private Devices.IOModule.DiscreteIO CHK4;
        private Devices.IOModule.DiscreteIO TrayEnterSensor;
        private Devices.IOModule.DiscreteIO PresenceCHK2Sensor;
        private int StackCount;
        private bool bM2Fwdbackup;
        protected override void InitEvt()//initialize events
        {
            base.InitEvt();
            Dictionary<string, ProcessEvt> evtdict = new Dictionary<string, ProcessEvt>();
            foreach (ProcessEvt e in Evtlist)
            {
                evtdict.Add(e.Name, e);
            }
            evtOutputStackerAtSafeLocation = evtdict[EvtNameList[0]].evt;
            evtOutputStackerStartSeq = evtdict[EvtNameList[1]].evt;// output stacker start seqence
            evtOutputStackerClearShutterPlate = evtdict[EvtNameList[2]].evt;
            evtOutputStackerClearShutterToMove = evtdict[EvtNameList[3]].evt;
            evtShutter01SignalOPStackerEndSeq = evtdict[EvtNameList[4]].evt;
            evtShutter02SignalOPStackerEndSeq = evtdict[EvtNameList[5]].evt;
            evtOutPutStackerSignalShutter01OPStackerSeqEndComplete = evtdict[EvtNameList[6]].evt;
            evtOutPutStackerSignalShutter02OPStackerSeqEndComplete = evtdict[EvtNameList[7]].evt;
            evtTmpDebugEvent= evtdict[EvtNameList[8]].evt;
            //<string>evtOPStackerRequestOPCVRun</string>
            //<string>evtOPStackerRequestOPCVRunAck</string>
            //<string>evtOPCVRunComplete</string>
            evtOPStackerRequestOPCVRun = evtdict[EvtNameList[9]].evt;
            evtOPStackerRequestOPCVRunAck = evtdict[EvtNameList[10]].evt;
            evtOPCVRunComplete = evtdict[EvtNameList[11]].evt;
            evtShutter01HomeComplete = evtdict[EvtNameList[12]].evt;
            evtShutter02HomeComplete = evtdict[EvtNameList[13]].evt;
        }

        protected override void InitInput()//initialize input
        {
            base.InitInput();
            TrayClearSensor = this.inputlist.IpDirectory[InputNameList[8]];
            TrayStopSensor = this.inputlist.IpDirectory[InputNameList[9]];
            CHK1 = this.inputlist.IpDirectory["Input67"];
            CHK2 = this.inputlist.IpDirectory["Input68"];
            CHK3 = this.inputlist.IpDirectory["Input69"];
            CHK4 = this.inputlist.IpDirectory["Input70"];
            TrayEnterSensor= this.inputlist.IpDirectory[InputNameList[14]];
            PresenceCHK2Sensor = this.inputlist.IpDirectory[InputNameList[15]];
        }
        protected override void InitOutput()//initialize output
        {
            base.InitOutput();
            //assigning outputs
            //<string>Output22</string>	<!--Output Singulator extend-->
            //<string>Output23</string>	<!--Output Singulator retract-->
            //<string>Output24</string>	<!--Output Benching Bar Close-->
            //<string>Output25</string>	<!--Output Benching Bar Open-->
            //<string>Output26</string>	<!--Output Shutter Door Close-->
            //<string>Output27</string>	<!--Output Shutter Door Open-->		
            //<string>Output28</string>	<!--M2 FWD-->	
            M2Fwd = this.outputlist.IpDirectory[OutputNameList[4]];
        }
        [XmlIgnore]
        public int TrayHeight;
        [XmlIgnore]
        public main.MainApp mainapp;
        private Devices.IOModule.DiscreteIO M2Fwd;
        public DistanceSensor KeyenceS1 { get { return _keyenceS1; } set { _keyenceS1 = value; NotifyPropertyChanged("KeyenceS1"); } }
        private DistanceSensor _keyenceS1;
        public DistanceSensor KeyenceS2 { get { return _keyenceS2; } set { _keyenceS2 = value; NotifyPropertyChanged("KeyenceS2"); } }
        private DistanceSensor _keyenceS2;

        public bool EnableKeyence { get; set; }

        private bool isOwnShutDoor { get { return !GlobalVar.isDoorAtCV; } }
        private string sCylDoor = "Output Shutter Door";
        public bool isPartPresent { get { return CheckPartPresent(); } }
        public bool isPartAbsent { get { return CheckPartAbsent(); } }
        public bool isStopSensorOn { get { return !TrayStopSensor.Logic; } }//on-没有产品 off-有感应
        private bool CheckPartAbsent()
        {
            return TrayClearSensor.Logic & TrayStopSensor.Logic;
        }
        private bool CheckPartPresent()
        {
            return !TrayClearSensor.Logic | !TrayStopSensor.Logic;
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
    }
}