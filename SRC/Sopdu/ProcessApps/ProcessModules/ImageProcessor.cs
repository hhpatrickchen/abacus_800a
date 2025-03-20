using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ImageProcessing;
using Dct.Models;
using Dct.Models.Repository;
using HandyControl.Controls;
using LogPanel;
using Sopdu.Devices.CameraLink;
using Sopdu.Devices.SecsGem;
using Sopdu.helper;
using Sopdu.ProcessApps.ImgProcessing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;

namespace Sopdu.ProcessApps.ProcessModules
{
    public class ImageProcessor : Process
    {
        LogTool<OutputStacker> logTool = new LogTool<OutputStacker>();
        [XmlIgnore]
        public ConcurrentBag<TrayImageInfo> fifotrayimginfoReq;
        public ConcurrentBag<TrayImageInfo> fifotrayimginfoCompete;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly log4net.ILog logvision = log4net.LogManager.GetLogger("AppenderLog");
        public string CoverTrayID;
        private ProductResultRepository _productResultRepository;
        public ImageProcessor()
        {
            EquipmentState = new GenericState();
            EquipmentState.SetState(MachineState.NotInit);
            _productResultRepository = DbManager.Instance.GetRepository<ProductResultRepository>();

            //init vision
        }
        private enum RunState { Start, AcquireImage }
        private RunState runstate;
        public override bool RunInitialization()
        {
            TrayImageInfo trayinfo1 = new TrayImageInfo();

            while (fifotrayimginfoReq.Count > 0)
            {
                fifotrayimginfoReq.TryTake(out trayinfo1);
            }

            return true;
        }

        public override void ShutdownOverride()
        {

            base.ShutdownOverride();

        }
        //private TrayImageInfo trayinfo, trayinfodisplay;
        public CogDisplay TrayDisp;
        public CogDisplay TrayDisppro;
        public override bool RunFunction()
        {
            if (fifotrayimginfoReq.Count() > 0)
            {
                try
                {
                    TrayImageInfo trayinfo = new TrayImageInfo();
                    if (fifotrayimginfoReq.TryTake(out trayinfo))
                    {
                        logTool.DebugLog("tray sn " + trayinfo.serialnumber + "incoming to image processor");
                        log.Debug("tray sn " + trayinfo.serialnumber + "incoming to image processor");
                        CogStopwatch sw = new CogStopwatch();
                        sw.Start();

                        if (!trayinfo.serialnumber.Contains(sCoverTrayPrefix))
                        //run image processing here
                        {
                            log.Debug("Non Cover Tray detected");
                            logTool.DebugLog("Non Cover Tray detected");
                            if (main.EnableVision)
                            {
                                log.Debug("Vision Enabled");
                                logTool.DebugLog("Vision Enabled");
                               
                                try
                                {
                                    //update map//
                                    //trayinfo.mapdata
                                    GemCtrl.TrayInspectionStart_END(true, trayinfo.serialnumber, trayinfo.mapdata, trayinfo.inspectionid);
                                }
                                catch (Exception ex)
                                {
                                    log.Debug("CEID 601 Send Error");
                                    logTool.DebugLog("CEID 601 Send Error");
                                    log.Debug(ex.ToString());
                                }
                                log.Debug("Trigger search");
                                //set search
                                ProfileTrayImageInfo rst = new ProfileTrayImageInfo();
                                pMode.SetInfoMsg("Processing Tray " + trayinfo.serialnumber);
                                logTool.DebugLog("Processing Tray " + trayinfo.serialnumber);
                                rst.trayimage = new CogImage16Range((CogImage16Range)trayinfo.tray3Dimage);
                                razor3D.TriggerSearch(rst);

                                log.Debug("Trigger search complete");
                                logTool.DebugLog("Trigger search complete");
                                //while (!traymanager.ProcessCompleteEvt.WaitOne(100))//issue location
                                while (!razor3D.ProcessCompleteEvt.WaitOne(100))//issue location
                                {
                                    pMode.ChkProcessMode();
                                }
                                log.Debug("Inpsection Complete");
                                logTool.DebugLog("Inpsection Complete");
                                if (razor3D.tb.RunStatus.Result != CogToolResultConstants.Accept)
                                {
                                    //save current error image
                                    trayinfo.serialnumber = trayinfo.serialnumber + "ER";
                                    pMode.SetError("Error on Tray Inspection");
                                    logTool.ErrorLog("Error on Tray Inspection");
                                    RunTimeData.kvpOPStkr.Add(trayinfo.serialnumber, false);
                                    log.Debug("Un-Accept Tray ID:" + trayinfo.serialnumber + "-Count:" + RunTimeData.kvpOPStkr.Count.ToString());
                                    logTool.DebugLog("Un-Accept Tray ID:" + trayinfo.serialnumber + "-Count:" + RunTimeData.kvpOPStkr.Count.ToString());
                                }
                                else
                                {
                                    pMode.SetInfoMsg("Inpsection Successful");
                                    logTool.InfoLog("Inpsection Successful");
                                    trayinfo.Result3D = rst.trayresults;//got 3D result
                                    try
                                    {
                                        pMode.SetInfoMsg("Generating CEID 602");
                                        logTool.InfoLog("Generating CEID 602");
                                        //CEID 602
                                        int iTotal = 0, iFail = 0;
                                        log.Debug("Start Logging result");
                                        logTool.DebugLog("Start Logging result");
                                        foreach (KeyValuePair<string, string> element in trayinfo.Result3D)
                                        {
                                            //trayinfo
                                            string inspectrst = element.Value;
                                            log.Debug(inspectrst);
                                            iTotal += 1;
                                            if (element.Value == "F")
                                                iFail += 1;
                                        }
                                        log.Debug("End loggin result");
                                        logTool.DebugLog("End loggin result");
                                        log.Debug("Failed Tray ID:" + trayinfo.serialnumber + "-Count:" + iFail.ToString());
                                        logTool.DebugLog("Failed Tray ID:" + trayinfo.serialnumber + "-Count:" + iFail.ToString());
                                        double failRate = iFail * 100.0 / iTotal;
                                        if (failRate >= GlobalVar.iFailRate)
                                        {
                                            GemCtrl.SetAlarm("ER_IM_E01");
                                            pMode.SetError($"Fail rate is over {GlobalVar.iFailRate}%", true);
                                            logTool.ErrorLog($"Fail rate is over {GlobalVar.iFailRate}%");
                                        }
                                          

                                        int iempty, ipresent, ierr;
                                        iempty = 0; ipresent = 0;
                                        ierr = 0;
                                        if (trayinfo.mapdata!=null)
                                        {
                                            int totalcol = trayinfo.mapdata.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinCode.Count();      // NullReferenceException here
                                            foreach (KeyValuePair<string, string> element in trayinfo.Result3D)
                                            {
                                                //trayinfo
                                                string inspectrst = element.Value;
                                                string[] slist = element.Key.Split(',');
                                                int column = int.Parse(slist[0]);
                                                int row = int.Parse(slist[1]);
                                                StringBuilder sb = new StringBuilder(trayinfo.mapdata.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinCode[totalcol - column - 1]);
                                                if (inspectrst == "E")
                                                {
                                                    sb[sb.Length - 1 - row] = '.';
                                                    trayinfo.mapdata.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinCode[totalcol - column - 1] = sb.ToString();
                                                    iempty++;
                                                }
                                                if (inspectrst == "P")
                                                {
                                                    sb[sb.Length - 1 - row] = 'X';
                                                    trayinfo.mapdata.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinCode[totalcol - column - 1] = sb.ToString();
                                                    ipresent++;
                                                }
                                                if ((inspectrst != "P") && (inspectrst != "E"))
                                                {
                                                    sb[sb.Length - 1 - row] = 'D';
                                                    trayinfo.mapdata.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinCode[totalcol - column - 1] = sb.ToString();
                                                    ierr++;
                                                }
                                            }

                                            log.Info($"iempty: {iempty}, iTotal:{iTotal}");
                                            // 如果监测出来全是空的，但是host返回的并不全是空的，需要报警
                                            if (GetHostData(trayinfo, out var hostEmptyNum))
                                            {
                                                log.Info($"hostEmptyNum: {hostEmptyNum}, iTotal:{iTotal}");
                                                if (iempty == iTotal && hostEmptyNum != iTotal)
                                                {
                                                }
                                            }
                                            
                                            //need to verify if this is correct
                                            trayinfo.mapdata.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinDefinitions = new MapDataSubstrateMapsSubstrateMapOverlayBinCodeMapBinDefinition[]
                                            {
                                            new MapDataSubstrateMapsSubstrateMapOverlayBinCodeMapBinDefinition(){BinCode = ".", BinDescription = "Empty", Pick = "false"},
                                            new MapDataSubstrateMapsSubstrateMapOverlayBinCodeMapBinDefinition(){BinCode = "X", BinDescription = "Present", Pick = "true"},
                                            new MapDataSubstrateMapsSubstrateMapOverlayBinCodeMapBinDefinition(){BinCode = "D", BinDescription = @"Double Stack/Unseated/Error", Pick = "false"}
                                            };
                                            trayinfo.mapdata.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinDefinitions[0].BinCount = (byte)iempty;//"." empty
                                            trayinfo.mapdata.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinDefinitions[1].BinCount = (byte)ipresent;//"X" present
                                            trayinfo.mapdata.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinDefinitions[2].BinCount = (byte)ierr;// "error
                                                                                                                                                   //CEID602    



                                            if (ierr > 0)
                                                RunTimeData.kvpOPStkr.Add(trayinfo.serialnumber, false);
                                            try
                                            {
                                                main.gdUnit = main.gdUnit + ipresent;
                                                main.emptyUnit = main.emptyUnit + iempty;
                                                main.XUnit = main.XUnit + ierr;
                                                Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
                                                {
                                                    main.GoodUnitCnt = main.gdUnit.ToString();
                                                    main.EmptyUnitCnt = main.emptyUnit.ToString();
                                                    main.XUnitCnt = main.XUnit.ToString();
                                                });
                                            }
                                            catch (Exception ex)
                                            { 
                                                log.Error("Count Display Update Error"); log.Error(ex.ToString());
                                                logTool.ErrorLog("Count Display Update Error");
                                            }

                                            //log.Debug("PUnit=" + main.gdUnit.ToString() + "EUnit=" + main.emptyUnit.ToString() + "XUnit=" + main.XUnit.ToString());
                                            //trayinfo.mapdata.Layouts[0].ChildLayouts =
                                            //   new Devices.SecsGem.MapDataLayoutChildLayouts()
                                            //   {
                                            //       ChildLayouts = new Devices.SecsGem.MapDataLayoutChildLayoutsChildLayouts() { LayoutId = trayinfo.mapdata.Layouts[1].LayoutId }
                                            //   };
                                            //trayinfo.mapdata.Layouts[1].ChildLayouts = 
                                            //    new Devices.SecsGem.MapDataLayoutChildLayouts() { ChildLayouts = new Devices.SecsGem.MapDataLayoutChildLayoutsChildLayouts() 
                                            //        { LayoutId = trayinfo.mapdata.Layouts[1].LayoutId } };
                                            //trayinfo.mapdata.Layouts[0].DefaultUnits = "mm";
                                            //trayinfo.mapdata.Layouts[1].DefaultUnits = "mm";
                                            //GemCtrl.TrayMapResult(trayinfo.serialnumber, trayinfo.mapdata, main.strCarrierID, trayinfo.inspectionid);  

                                            GemCtrl.TrayMapResult(trayinfo.serialnumber, trayinfo.mapdata, main.strCarrierID, trayinfo.inspectionid);

                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        RunTimeData.kvpOPStkr.Add(trayinfo.serialnumber, false);
                                        //log.Debug("Failed Tray ID:" + trayinfo.serialnumber + "-Count:" + RunTimeData.kvpOPStkr.Count.ToString());
                                        log.Debug("E142 Map Generated complete");
                                        logTool.DebugLog("E142 Map Generated complete");
                                        log.Error(ex.ToString());
                                        logTool.ErrorLog(ex.ToString());
                                    }
                                }

                                try
                                {
                                    //CEID 603
                                    GemCtrl.TrayInspectionStart_END(false, trayinfo.serialnumber, trayinfo.mapdata, trayinfo.inspectionid);
                                }
                                catch (Exception ex)
                                {
                                    log.Debug("CEID 603 error");
                                    log.Debug(ex.ToString());
                                }
                                trayinfo.TraySearchReg = new CogRectangle(razor3D.TraySearchReg);
                                trayinfo.disrst = new List<CogCompositeShape>();
                                trayinfo.trayimage = new CogImage8Grey((CogImage8Grey)razor3D.iptool.OutputImage);//tray images for display
                                trayinfo.trayrstgraphic = new List<TrayInspectionRst>();
                                trayinfo.Result3D = rst.trayresults;//got 3D result
                                pMode.SetInfoMsg("Total Object display " + trayinfo.Result3D.Count());
                                logTool.InfoLog("Total Object display " + trayinfo.Result3D.Count());
                                sw.Stop();
                                pMode.SetInfoMsg("Image Processing Time : " + sw.Milliseconds.ToString());
                                logTool.InfoLog("Image Processing Time : " + sw.Milliseconds.ToString());
                                //end of image processing
                            }
                            else
                            {
                                log.Debug("Vision Disabled");
                                logTool.DebugLog("Vision Disabled");
                                razor3D.pixelmaptool.InputImage = (CogImage16Range)trayinfo.tray3Dimage;
                                razor3D.pixelmaptool.Run();
                                razor3D.iptool.InputImage = (CogImage8Grey)razor3D.pixelmaptool.OutputImage;
                                razor3D.iptool.Run();
                                trayinfo.Result3D = new Dictionary<string, string>();
                                trayinfo.trayimage = new CogImage8Grey((CogImage8Grey)razor3D.iptool.OutputImage);
                            }
                            fifotrayimginfoCompete.Add(trayinfo);
                        }
                        else
                        {
                            log.Debug("Cover Tray detected");
                            logTool.DebugLog("Cover Tray detected");
                            //razor3D.fixture.InputImage = (CogImage16Range)trayinfo.tray3Dimage;
                            //razor3D.fixture.Run();
                            razor3D.pixelmaptool.InputImage = (CogImage16Range)trayinfo.tray3Dimage;
                            razor3D.pixelmaptool.Run();
                            razor3D.iptool.InputImage = (CogImage8Grey)razor3D.pixelmaptool.OutputImage;
                            razor3D.iptool.Run();
                            trayinfo.trayimage = new CogImage8Grey((CogImage8Grey)razor3D.iptool.OutputImage);
                            trayinfo.Result3D = new Dictionary<string, string>();
                            fifotrayimginfoCompete.Add(trayinfo);
                        }
                    }
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    pMode.SetWarningMsg("image processing error " + ex.ToString());
                    logTool.WarnLog("image processing error " + ex.ToString());
                }
            }
            return true;
        }

        private bool GetHostData(TrayImageInfo trayinfo, out int EmpytCount)
        {
            EmpytCount = 0;
            try
            {
                var mapData = trayinfo.mapdata;
                var emptyNum = mapData.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinDefinitions.Where(a => a.BinDescription == "EMPTY").FirstOrDefault();

                EmpytCount = emptyNum.BinCount;
                return true;
            }
            catch (Exception ex) 
            {
                return false;
            }
           
        }

        int i = 0;
        CogIPOneImageTool rotate;
        Image img1;
        Image img2;
        private ImgProcessing.TrayPktInspectionMgr traymanager;
        int n = 0;
        protected override void DisplayCycle()
        {
            base.DisplayCycle();
            try
            {
                int numpass = 0;
                int numfail = 0;
                int numempty = 0;
                bool failtray = false;
                if (fifotrayimginfoCompete == null) return;
                if (fifotrayimginfoCompete.Count() > 0)
                {
                    n++;
                    if (n == 2)
                    {
                        n = 0;
                        GC.Collect();
                    }
                    try
                    {

                        TrayImageInfo trayinfodisplay = new TrayImageInfo();
                        if (fifotrayimginfoCompete.TryTake(out trayinfodisplay))
                        {

                            main.CurrentTrayID = trayinfodisplay.serialnumber;
                            TrayDisp.Image = trayinfodisplay.trayimage;
                            TrayDisppro.Image = trayinfodisplay.trayimage;
                            TrayDisp.InteractiveGraphics.Clear();
                           
                            if (trayinfodisplay.serialnumber.Contains(sCoverTrayPrefix))
                            {
                                CoverTrayID = trayinfodisplay.serialnumber;
                            }
                            if (!trayinfodisplay.serialnumber.Contains(sCoverTrayPrefix))// not full prove... sometimes will have skip inspection command
                            {
                                //generate rectangles

                                Dictionary<string, CogRectangle> rectlist = new Dictionary<string, CogRectangle>();
                                if (trayinfodisplay.TraySearchReg != null)
                                    rectlist = razor3D.GenerateCellListDictionary(trayinfodisplay.TraySearchReg);
                                //end of rectangle generation

                                foreach (KeyValuePair<string, CogRectangle> entry in rectlist)
                                {
                                    string rst = trayinfodisplay.Result3D[entry.Key];
                                    CogGraphicLabel label = new CogGraphicLabel();
                                    label.Alignment = CogGraphicLabelAlignmentConstants.BottomCenter;
                                    label.X = rectlist[entry.Key].CenterX;
                                    label.Y = rectlist[entry.Key].CenterY;
                                    switch (rst)
                                    {
                                        case "E":
                                            rectlist[entry.Key].Color = CogColorConstants.Magenta;
                                            label.Text = "E";
                                            numempty++;
                                            break;

                                        case "P":
                                            rectlist[entry.Key].Color = CogColorConstants.Green;
                                            label.Text = "P";
                                            numpass++;
                                            break;

                                        case "F":
                                            rectlist[entry.Key].Color = CogColorConstants.Red;
                                            label.Text = "X";
                                            numfail++;
                                            break;
                                    }
                                    TrayDisp.InteractiveGraphics.Add(rectlist[entry.Key], "s", true);
                                    TrayDisp.InteractiveGraphics.Add(label, "s", true);

                                }
                                if ((numfail > 0) || (numempty > 0)) failtray = true;
                                TrayDisp.StaticGraphics.Clear();
                                if (trayinfodisplay.TraySearchReg != null)
                                    TrayDisp.StaticGraphics.Add(trayinfodisplay.TraySearchReg, "z");
                                try
                                {

                                    TrayDisppro.Fit();
                                    img1 = TrayDisp.CreateContentBitmap(CogDisplayContentBitmapConstants.Display);
                                    img2 = TrayDisppro.CreateContentBitmap(CogDisplayContentBitmapConstants.Display);

                                    try
                                    {
                                        main.gdUnit = numpass;
                                        main.emptyUnit = numempty;
                                        main.XUnit = numfail;
                                        Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
                                        {
                                            main.GoodUnitCnt = main.gdUnit.ToString();
                                            main.EmptyUnitCnt = main.emptyUnit.ToString();
                                            main.XUnitCnt = main.XUnit.ToString();
                                        });
                                    }
                                    catch (Exception ex) 
                                    { 
                                        log.Error("Count Display Update Error");
                                        log.Error(ex.ToString());
                                        logTool.ErrorLog("Count Display Update Error");
                                        logTool.ErrorLog(ex.ToString());
                                    }
                                    logTool.DebugLog(trayinfodisplay.serialnumber + "," + numpass.ToString() + "," + numfail.ToString() + "," + numempty.ToString());
                                    logvision.Debug(trayinfodisplay.serialnumber + "," + CoverTrayID+","+ InputStacker.CurrentRecipe+","+numpass.ToString() + "," + numfail.ToString() + "," + numempty.ToString());
                                    _productResultRepository.Insert(new Dct.Models.Entity.ProductResultEntity() { Code = CoverTrayID, CoverTrayID = CoverTrayID, StartTime=DateTime.Now, EndTime = DateTime.Now, Recipe = InputStacker.CurrentRecipe, Result = failtray?"Fail":"Pass", TrayType = InputStacker.CurrentRecipe }, out _);
                                    string hdisk = AppDomain.CurrentDomain.BaseDirectory.Substring(0, 1);
                                    string savepath = $"{hdisk}:\\ImageFile";
                                    if ((this.main.EnableVisionDebug) || failtray)
                                    {
                                        if (Directory.Exists(savepath))
                                        {
                                            img1.Save(savepath + @"\" + trayinfodisplay.serialnumber + "-"
                                                + DateTime.Now.ToString("MMddThhmmss") + "-a.bmp");
                                        }
                                        else
                                        {
                                            Directory.CreateDirectory(savepath);
                                            img1.Save(savepath + @"\" + trayinfodisplay.serialnumber + "-"
                                              + DateTime.Now.ToString("MMddThhmmss") + "-a.bmp");

                                        }

                                    }

                                    string savepath1 = $"{hdisk}:\\ImageFile1";
                                    if ((this.main.EnableVisionDebug) || failtray)
                                    {
                                        if (!Directory.Exists(savepath1))
                                        {

                                            Directory.CreateDirectory(savepath1);
                                        }
                                        img2.Save(savepath1 + @"\" + trayinfodisplay.serialnumber + "-"
                                                + DateTime.Now.ToString("MMddThhmmss") + "-a.bmp");
                                    }
                                       

                                }

                                catch (Exception ex)
                                {
                                    logvision.Debug(ex.Message);
                                    logTool.DebugLog(ex.Message);
                                }
                            }
                            if ((this.main.EnableVisionDebug) || failtray)
                            {                                
                                //

                                //string savepath = AppDomain.CurrentDomain.BaseDirectory + @"\failimages";
                                string hdisk = AppDomain.CurrentDomain.BaseDirectory.Substring(0, 1);
                                string savepath = $"{hdisk}:\\ImageFile";

                                log.Debug($"failtray={failtray},savepath={savepath}");

                                if (Directory.Exists(savepath))
                                {
                                    CogImageFileTool ftool = new CogImageFileTool();
                                    ftool.Operator.Open(savepath + @"\" + trayinfodisplay.serialnumber
                                        + "-" + DateTime.Now.ToString("MMddThhmmss") + "-b.idb", CogImageFileModeConstants.Write);
                                    ftool.InputImage = trayinfodisplay.tray3Dimage;
                                    ftool.Run();
                                    ftool.Operator.Close();
                                }
                                else
                                {
                                    Directory.CreateDirectory(savepath);
                                    CogImageFileTool ftool = new CogImageFileTool();
                                    ftool.Operator.Open(savepath + @"\" + trayinfodisplay.serialnumber
                                        + "-" + DateTime.Now.ToString("MMddThhmmss") + "-b.idb", CogImageFileModeConstants.Write);
                                    ftool.InputImage = trayinfodisplay.tray3Dimage;
                                    ftool.Run();
                                    ftool.Operator.Close();

                                }

                                log.Debug($"failtray={failtray},fail image saved dnoe");
                            }
                            TrayDisp.Fit();
                        }
                    }
                    catch (Exception ex) { logvision.Error(ex.Message); }

                }
            }
            catch (Exception ex) { logvision.Error(ex.Message); }

        }

        [XmlIgnore]
        public main.MainApp main { get; set; }

        internal void LoadVisionObject(ImgProcessing.TrayPktInspectionMgr TrayMgr)
        {
            //throw new NotImplementedException();
            //remove this due to new algo
            //traymanager = TrayMgr;
            //traymanager.ReassignProcessBlockList();
            rotate = new CogIPOneImageTool();
            CogIPOneImageFlipRotate flip = new CogIPOneImageFlipRotate();
            flip.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate90Deg;
            rotate.Operators.Add(flip);

        }

        internal void Load3DVisionObject(Razor razor)
        {
            //throw new NotImplementedException();
            razor3D = razor;
        }

        public Razor razor3D { get; set; }

    }
}
