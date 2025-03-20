using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using Sopdu.Devices.CameraLink;
using Sopdu.Devices.Cylinders;
using Sopdu.helper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Cognex.VisionPro.Implementation.Internal;
using LogPanel;
using Sopdu.ProcessApps.main;
using ModbusTCP;

namespace Sopdu.ProcessApps.ProcessModules
{
    class LineScanAcqProcess : Process
    {
        LogTool<OutputStacker> logTool = new LogTool<OutputStacker>();
        [XmlIgnore]
        public ConcurrentBag<TrayImageInfo> fifotrayimginfoReq;
        public ConcurrentBag<TrayImageInfo> fifotrayimginfoCompete;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ManualResetEvent evtShutter01RequestAcq, evtShutter02RequestAcq, evtShutter01RequestAcqAck, evtShutter02RequestAcqAck;
        public DalsaCameraLink linescan;
        public CogAcqFifoTool scanner;
        public ProcessMaster pMaster;

        public LineScanAcqProcess()
        {
           
            EquipmentState = new GenericState();
            EquipmentState.SetState(MachineState.NotInit);
            //linescan = new DalsaCameraLink();
        }
        private enum RunState { Start,   AcquireImage }
        private RunState runstate;
        public override bool RunInitialization()
        {
            acqfifo.Operator.Flush();
            TrayImageInfo acqinfo1 = new TrayImageInfo();
            while (fifotrayimginfoReq.Count>0)
            {
                fifotrayimginfoReq.TryTake(out acqinfo1);
            }
            i = 0;
            //linescan.Abort();
            pMode.SetInfoMsg("Init Completed");
            logTool.InfoLog("Init Completed");
            runstate = RunState.Start;
            return true;
        }

        public override void ShutdownOverride()
        {
            base.ShutdownOverride();
           // linescan.Shutdown();
        }
        public override bool RunFunction()
        {
            switch (runstate)
            {
                case RunState.Start:
                   runstate= StartFn();
                   break;
                case RunState.AcquireImage:
                   runstate = AcquireImageFn();
                   break;                
            }
            return true;
        }
       

        int k = 0;
        int n = 0;
        private RunState AcquireImageFn()
        {
            if (k == 0)
            {
                pMode.SetInfoMsg("Wait for Image");
                logTool.DebugLog("Wait for Image");
                Thread.Sleep(1500);
             
            }
            k++;

            log.Debug("acqfifo.RunStatus.Result=" + acqfifo.RunStatus.Result.ToString());
            logTool.DebugLog("acqfifo.RunStatus.Result=" + acqfifo.RunStatus.Result.ToString());

            if (acqfifo.RunStatus.Result == CogToolResultConstants.Accept)
            {
                log.Debug("encoderAccept=" + acqfifo.Operator.OwnedLineScanParams.CurrentEncoderCount.ToString());
                log.Debug(acqfifo.RunStatus.Message);
                logTool.DebugLog(acqfifo.RunStatus.Message);
                acqfifo.GarbageCollectionEnabled = true;
                acqfifo.GarbageCollectionFrequency = 5;
                pMode.SetInfoMsg("Rdy to image acquired");
                logTool.DebugLog("Rdy to image acquired");
                acqinfo.tray3Dimage = new CogImage16Range((CogImage16Range)acqfifo.OutputImage);
                acqinfo.tray3Dimagefordebug = new CogImage16Range(acqinfo.tray3Dimage);
                fifotrayimginfoCompete.Add(acqinfo);
                //CogImageFileTool tool = new CogImageFileTool();
                //string datetime = DateTime.Now.ToString("MMddThhmmss");
                //bool exists = System.IO.Directory.Exists(@".\testimage\");
                //if (!exists) System.IO.Directory.CreateDirectory(@".\testimage\");
                //tool.Operator.Open(@".\testimage\" + datetime + @".idb", CogImageFileModeConstants.Write);
                //tool.InputImage = acqinfo.trayimage;
                //tool.Run();
                pMode.SetInfoMsg("image acquired");
                logTool.DebugLog("image acquired");
                i = 0;
                return RunState.Start;
        }
            else
            {
                log.Debug(" ----max frequency" + acqfifo.Operator.OwnedLineScanParams.MaximumLineFrequency.ToString());
                log.Debug("encoderError Message = " + acqfifo.RunStatus.Message);
                log.Debug("encoderError=" + acqfifo.Operator.OwnedLineScanParams.CurrentEncoderCount.ToString());
                logTool.DebugLog(" ----max frequency" + acqfifo.Operator.OwnedLineScanParams.MaximumLineFrequency.ToString());
                logTool.DebugLog("encoderError Message = " + acqfifo.RunStatus.Message);
                logTool.DebugLog("encoderError=" + acqfifo.Operator.OwnedLineScanParams.CurrentEncoderCount.ToString());
                //8march2022
                acqfifo.Operator.OwnedTriggerParams.TriggerEnabled = false;
                acqfifo.Operator.OwnedTriggerParams.TriggerModel = CogAcqTriggerModelConstants.FreeRun;
                acqfifo.Operator.OwnedTriggerParams.TriggerEnabled = true;
                acqfifo.Operator.OwnedTriggerParams.TriggerModel = CogAcqTriggerModelConstants.Manual;
                //pMode.SetInfoMsg("image acquired fail : " + acqfifo.RunStatus.Message);
                GemCtrl.SetAlarm("ER_LS_E01");
                pMode.SetError("image acquired fail : " + acqfifo.RunStatus.Message);
                pMaster.SetError("image acquired fail : " + acqfifo.RunStatus.Message);
                logTool.ErrorLog("image acquired fail : " + acqfifo.RunStatus.Message);
                
                acqinfo.tray3Dimage = new CogImage16Range((CogImage16Range) acqfifo.OutputImage);
                fifotrayimginfoCompete.Add(acqinfo);
                pMode.SetInfoMsg("image acquired aborted");
                logTool.InfoLog("image acquired aborted");
                i = 0;
                return RunState.Start;
            }

            return runstate;
        }
        private TrayImageInfo acqinfo;

        int i = 0;
        private RunState StartFn()
        {
            if (i==0) 
            {
                pMode.SetInfoMsg("Enter Start Fn In Linescan Module");
                logTool.InfoLog("Enter Start Fn In Linescan Module");
                Thread.Sleep(100);
                i++;
            }
            k = 0;
            //if (!linescan.m_IsSignalDetected)
            //{
            //    //make sure theres signal before acqusition
            //    Thread.Sleep(100);
            //    return runstate;
            //}
            // pMode.SetInfoMsg("Line Scan Signal Detected");
            if (WaitEvtOnWithoutErrorFire(100, evtShutter01RequestAcq))
            { //pop fifo request reply with event
                pMode.SetInfoMsg("Shutter 01 Acq Req Rx at linescan module");
                logTool.DebugLog("Shutter 01 Acq Req Rx at linescan module");
                acqinfo = new TrayImageInfo();
                evtShutter01RequestAcq.Reset();
                fifotrayimginfoReq.TryTake(out acqinfo);
                //if (!linescan.StartGrabImage()) pMode.SetInfoMsg("Snap Fail");
                //Thread.Sleep(100);
                
                evtShutter01RequestAcqAck.Set();
                if (acqfifo.RunStatus.Result != CogToolResultConstants.Accept)
                {
                    log.Debug("acqfifo error" + acqfifo.RunStatus.Message);
                    logTool.DebugLog("acqfifo error" + acqfifo.RunStatus.Message);
                }
                            
                acqfifo.Operator.OwnedLineScanParams.ResetCounter();

                // ICogAcqLineScan.ResetCounterOnHardwareTrigger();

                acqfifo.Operator.Flush();
                log.Debug("encode01r=" + acqfifo.Operator.OwnedLineScanParams.CurrentEncoderCount.ToString());
                logTool.DebugLog("encode01r=" + acqfifo.Operator.OwnedLineScanParams.CurrentEncoderCount.ToString());
                acqfifo.Run();
                log.Debug("max speed-----" + acqfifo.Operator.OwnedLineScanParams.MaximumMotionSpeed.ToString() + " ----max frequency" + acqfifo.Operator.OwnedLineScanParams.MaximumLineFrequency.ToString());
                log.Debug("encode01r=" + acqfifo.Operator.OwnedLineScanParams.CurrentEncoderCount.ToString());
                logTool.DebugLog("encode01r=" + acqfifo.Operator.OwnedLineScanParams.CurrentEncoderCount.ToString());
                logTool.DebugLog("max speed-----" + acqfifo.Operator.OwnedLineScanParams.MaximumMotionSpeed.ToString() + " ----max frequency" + acqfifo.Operator.OwnedLineScanParams.MaximumLineFrequency.ToString());
                return RunState.AcquireImage;
            }

            if (WaitEvtOnWithoutErrorFire(100, evtShutter02RequestAcq))
            { //pop fifo request reply with event
                pMode.SetInfoMsg("Shutter 02 Acq Req Rx at linescan module");
                logTool.InfoLog("Shutter 02 Acq Req Rx at linescan module");
                acqinfo = new TrayImageInfo();
                evtShutter02RequestAcq.Reset();
                fifotrayimginfoReq.TryTake(out acqinfo);
                //if (!linescan.StartGrabImage()) pMode.SetInfoMsg("Snap Fail");
                //Thread.Sleep(100);
               
                evtShutter02RequestAcqAck.Set();
                acqfifo.Operator.Flush();
                acqfifo.Operator.OwnedLineScanParams.ResetCounter();
                acqfifo.Run();
                log.Debug("encoder02=" + acqfifo.Operator.OwnedLineScanParams.CurrentEncoderCount.ToString());
                logTool.DebugLog("encoder02=" + acqfifo.Operator.OwnedLineScanParams.CurrentEncoderCount.ToString());
                return RunState.AcquireImage;
            }
            return runstate;
        }
        protected override void StoppingLogicFn()
        {            
        }
        protected override void RecoverFromStopFn()
        {
            base.RecoverFromStopFn();
        }

        protected override void InitEvt()//initialize events
        {
            base.InitEvt();
            Dictionary<string, ProcessEvt> evtdict = new Dictionary<string, ProcessEvt>();
            foreach (ProcessEvt e in Evtlist)
            {
                evtdict.Add(e.Name, e);
            }
            evtShutter01RequestAcq = evtdict[EvtNameList[0]].evt;
            evtShutter02RequestAcq = evtdict[EvtNameList[1]].evt;
            evtShutter01RequestAcqAck = evtdict[EvtNameList[2]].evt;
            evtShutter02RequestAcqAck = evtdict[EvtNameList[3]].evt;
        }

        [XmlIgnore]
        public Cognex.VisionPro.CogAcqFifoTool acqfifo { get; set; }
        


    }
}
