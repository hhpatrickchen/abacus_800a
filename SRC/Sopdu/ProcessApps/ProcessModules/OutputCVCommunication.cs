using Insphere.Connectivity.Application.SecsToHost;
using LogPanel;
using Sopdu.Devices.Cylinders;
using Sopdu.helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sopdu.ProcessApps.ProcessModules
{
    public class OutputCVCommunication : Process
    {
        LogTool<OutputStacker> logTool = new LogTool<OutputStacker>();
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ManualResetEvent evtOPStackerRequestOPCVRun, evtOPStackerRequestOPCVRunAck, evtOPCVRunComplete;

        public OutputCVCommunication()
        {
            EquipmentState = new GenericState();
            EquipmentState.SetState(MachineState.NotInit);
        }

        public override bool RunInitialization()
        {
            UpStreamTrayAvailable.Logic = false;
            runstate = RunState.Start;
            return true;
        }

        private enum RunState { Start, WaitForTrayExit }
        private RunState runstate;
        public override bool RunFunction()
        {
            switch (runstate)
            {
                case RunState.Start:
                    runstate = StartFn();
                    break;
                case RunState.WaitForTrayExit:
                    runstate = WaitForTrayArrivalFn();
                    break;

            }
            return true;
        }

        private RunState WaitForTrayArrivalFn()
        {
            int deboucing = 0;
            int timeout = 500;

            while (true)
            {
                Thread.Sleep(100);
                timeout--;
                if (timeout < 0)
                { pMode.SetError("Tray Send to Downstream Conveyor Time Out", true);
                    logTool.ErrorLog("Tray Send to Downstream Conveyor Time Out");
                    pMode.ChkProcessMode(); }
                pMode.ChkProcessMode();
                if ((!DownStreamReadyToRxTray.Logic) && ((TrayClearOutputStacker.Logic) && (TrayClearOutputStackerStop.Logic)))
                {
                    deboucing++;
                    if (deboucing > 12)
                    {
                        break;
                    }
                }

            }
            pMode.SetInfoMsg("Tray Cleared From Output Stacker to CV DownStream");
            logTool.DebugLog("Tray Cleared From Output Stacker to CV DownStream");
            evtOPCVRunComplete.Set();
            return RunState.Start;
        }

        private RunState StartFn()
        {
            UpStreamTrayAvailable.Logic = false;
            pMode.SetInfoMsg("SEMA CV At Initial State");
            logTool.InfoLog("SEMA CV At Initial State");
            WaitEvtOnInfinite(evtOPStackerRequestOPCVRun);//wait for output stacker request
            evtOPStackerRequestOPCVRun.Reset();            
            UpStreamTrayAvailable.Logic = true;
            //check if system is online
            if (GemCtrl.gemController.CommunicationState == CommunicationState.Disabled)//if com state is disabled we call for manual removal
            {
                pMode.SetInfoMsg("System offline, manual override and wait for next tray");
                logTool.DebugLog("System offline, manual override and wait for next tray");
                evtOPStackerRequestOPCVRunAck.Set();
                return RunState.Start;
            }
            //end of removal
            //check if theres tray at output stacker
            if (DownStreamReadyToRxTray.Logic)
            { 
                pMode.SetInfoMsg("Tray Request At DownStream Conveyor");
                logTool.InfoLog("Tray Request At DownStream Conveyor");
            }
            int k = 0;
            while (!DownStreamReadyToRxTray.Logic) { 
                if (k==0)
                {
                    k++;
                    pMode.SetInfoMsg("Wait for Tray Request At DownStream Conveyor");
                    logTool.InfoLog("Wait for Tray Request At DownStream Conveyor");
                }
                pMode.ChkProcessMode(); 
                Thread.Sleep(100); }
            pMode.SetInfoMsg("Tray Request At DownStream Conveyor RX");
            logTool.InfoLog("Tray Request At DownStream Conveyor RX");
            evtOPStackerRequestOPCVRunAck.Set();
            return RunState.WaitForTrayExit;
        }
        protected override void InitEvt()//initialize events
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
        private Devices.IOModule.DiscreteIO DownStreamReadyToRxTray, TrayClearOutputStacker, TrayClearOutputStackerStop;
        private Devices.IOModule.DiscreteIO UpStreamTrayAvailable;
        protected override void InitInput()//initialize input
        {
            //<string>Input73</string>	<!--Tray At OP C/V Inpos Sensor-->
            //<string>Input74</string>	<!--DownStreamReadyToRxTray-->
            //<string>Input75</string>	<!--Run-->	
            //<string>Input76</string>	<!--Part Inpos-->			
            //<string>Input77</string>	<!--Send Part-->
            //<string>Input65</string>
            //<string>Input66</string>
            //<string>Input52</string>	<!--OP CV Clear InPos-->            
            base.InitInput();
            DownStreamReadyToRxTray = this.inputlist.IpDirectory[InputNameList[1]];
            TrayClearOutputStacker = this.inputlist.IpDirectory[InputNameList[5]];
            TrayClearOutputStackerStop = this.inputlist.IpDirectory[InputNameList[6]];
        }

        protected override void InitOutput()//initialize output
        {

            base.InitOutput();
            UpStreamTrayAvailable  = this.outputlist.IpDirectory[OutputNameList[0]];
        }
    }
}
