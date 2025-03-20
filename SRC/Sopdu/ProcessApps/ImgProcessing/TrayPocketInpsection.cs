using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.Caliper;
using Cognex.VisionPro.Dimensioning;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.ToolBlock;
using Sopdu.helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sopdu.ProcessApps.ImgProcessing
{
    public class TrayPktInspectionMgr : Process
    {
        private string _ProcessName;
        public string ProcessName { get { return _ProcessName; } set { _ProcessName = value; NotifyPropertyChanged(); } }
        private int _TrayCol, _TrayRow, _BlockNo, BlockCol;
        public int TrayCol { get { return _TrayCol; } set { _TrayCol = value; NotifyPropertyChanged(); } }
        public int TrayRow { get { return _TrayRow; } set { _TrayRow = value; NotifyPropertyChanged(); } }
        public int BlockNo { get { return _BlockNo; } set { _BlockNo = value; NotifyPropertyChanged(); } }

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private CogPMAlignTool fsearch_tray, fsearch_0, fsearch_180, fsearch_DB, fsearch_EmptyPkt;
        private CogHistogramTool LightSensor;

        private double XAllowance;
        private double YAllowance;
        [XmlIgnore]
        public CogImage8Grey InputImage;

        [XmlIgnore]
        public double pocketprocesstime;

        [XmlIgnore]
        public double overallsearchtime;

        private List<CogRectangle> RegionList;
        private CogCopyRegionTool regcopytool;

        [XmlIgnore]
        public CogCopyRegionTool maskcopytool;

        [XmlIgnore]
        public List<CogCompositeShape> disrst;
        [XmlIgnore]
        public List<CogCompositeShape> disp;
        private List<CogRectangle> CellList;
        public CogRectangle TraySearchReg;

        [XmlIgnore]
        public List<TrayPocketInpsection> BlockInpsection;

        private List<TrayPocketInpsection> WaitBlockList;
        private CogAffineTransformTool txformtool;
        public CogFixtureTool fixture;

        [XmlIgnore]
        public GenericEvents mcEvents;

        [XmlIgnore]
        public ManualResetEvent StartProcessingEvt, ProcessCompleteEvt;

        [XmlIgnore]
        public CogImage8Grey OutputMaskImage;

        [XmlIgnore]
        public List<CogRectangleAffine> MaskDspReg;

        private int numofBlock;
        private string runtimeparam;

        public TrayPktInspectionMgr()
        {
            EquipmentState = new GenericState();
            DisplayState = new GenericState();
            DisplayState.SetState(MachineState.Run);
            EquipmentState.SetState(MachineState.Run);
        }

        public void SetRegionValue(double xallow,double yallow)
        {
            XAllowance = xallow; YAllowance = yallow;
        }
        public void Init(GenericEvents mcEvents,
          double yieldfactor,//txform the resolution
          int blockno,
          CogPMAlignTool TrayLocate,
          CogPMAlignTool fiducialsearch_0,
          CogPMAlignTool fiducialsearch_180,//change this into an array
          CogPMAlignTool DeathBug,
          CogPMAlignTool EmptyPocket,
          CogHistogramTool BrighnessTool,
          CogRectangle TraySearchRegion,
          int Row,
          int Column,
          string processname)
        {
            disrst = new List<CogCompositeShape>();
            disp = new List<CogCompositeShape>();
            fixture = new CogFixtureTool();
            ProcessName = processname;
            this.mcEvents = mcEvents;
            //mask setup
            maskcopytool = new CogCopyRegionTool();
            //maskcopytool.RunParams.FillRegion = true;
            maskcopytool.RunParams.FillRegionValue = 0;
            maskcopytool.RunParams.FillBoundingBoxValue = 0;
            maskcopytool.RunParams.ImageAlignmentEnabled = true;
            //end of mask setup
            StartProcessingEvt = new ManualResetEvent(false);
            ProcessCompleteEvt = new ManualResetEvent(false);

            fsearch_0 = fiducialsearch_0; //only search in pocket
            fsearch_180 = fiducialsearch_180; //search everything include 180 degree
            fsearch_tray = TrayLocate;
            fsearch_DB = DeathBug;//search death bug
            fsearch_EmptyPkt = EmptyPocket;
            LightSensor = BrighnessTool;
            txformtool = new CogAffineTransformTool();
            txformtool.Region = null;
            txformtool.RunParams.ScalingX = yieldfactor;
            txformtool.RunParams.ScalingY = yieldfactor;
            //setting up search regions in tray
            TrayCol = Column; TrayRow = Row;
            TraySearchReg = TraySearchRegion;
            CellList = GenerateCellList(TraySearchRegion);
            //create block system
            BlockInpsection = new List<TrayPocketInpsection>();
            BlockCol = TrayCol / blockno;
            int cellblockno = BlockCol * TrayRow;
            int k = 0;
            for (int i = 0; i < blockno; i++)
            {
                //assigning cell block to individual block process
                List<CogRectangle> blkcellist = new List<CogRectangle>();
                for (int j = i * cellblockno;
                         j < (i * cellblockno + cellblockno);
                         j++)
                {
                    blkcellist.Add(CellList[j]);
                    k++;
                }
                if ((k < CellList.Count) && (i == (blockno - 1)))//make sure all cells are accounted for
                {
                    for (int j = k; k < CellList.Count; j++)
                    {
                        blkcellist.Add(CellList[j]);
                        k++;
                    }
                }
                //end of cell assignment
                //instantiate inpsectionblock
                TrayPocketInpsection Inspectionblock = new TrayPocketInpsection();
                Inspectionblock.Init(mcEvents, fsearch_0, fsearch_180, fsearch_DB,
                    fsearch_EmptyPkt, null, blkcellist, "ProcessBlock" + i.ToString());

                BlockInpsection.Add(Inspectionblock);
                //end of inspection blocks instatitate
            }
            fsearch_180 = new CogPMAlignTool(fiducialsearch_180);
            fsearch_180.SearchRegion = null;
            fsearch_180.RunParams.ApproximateNumberToFind = 5;
            pMode = new ProcessMode();
            pMode.Init(mcEvents);//set the state for the process
            CmdThread = new Thread(new ThreadStart(CmdThreadFn));
            CmdThread.Start();
        }

        public List<CogRectangle> GenerateCellList(CogRectangle searchreg)
        {
  
            CogRectangle rect = new CogRectangle(searchreg);
            List<CogRectangle> celllist = new List<CogRectangle>();

   
            double wpitch = rect.Width / TrayRow;
            double hpitch = rect.Height / TrayCol;
            for (int i = 0; i < TrayRow; i++)
            {
                for (int j = 0; j < TrayCol; j++)
                {
                    CogRectangle cellrect = new CogRectangle();
                    cellrect.LineWidthInScreenPixels = 10;
                    cellrect.SelectedLineWidthInScreenPixels = 10;
                    cellrect.Height = hpitch+YAllowance;
                    cellrect.Width = wpitch+XAllowance;
                    cellrect.X = rect.X + wpitch * i-XAllowance/2;
                    cellrect.Y = rect.Y + hpitch * j-YAllowance/2;
                    cellrect.Interactive = true;
                    cellrect.TipText = (i.ToString() + "," + j.ToString());
                    cellrect.GraphicDOFEnable = CogRectangleDOFConstants.None;
                    cellrect.GraphicDOFEnableBase = CogGraphicDOFConstants.None;
                    cellrect.TipText = j.ToString() + "," + i.ToString();
                    celllist.Add(cellrect);
                }
            }
            return celllist;
        }

        public override bool RunInitialization()
        {
            //run this once when the module is initialized
            return true;
        }

        private void MaskRegionImage(CogRectangle mreg, CogImage8Grey inputmskimage)
        {
            maskcopytool.InputImage = inputmskimage;
            maskcopytool.DestinationImage = OutputMaskImage;
            maskcopytool.Region = mreg;
            maskcopytool.RunParams.FillRegion = false;
            maskcopytool.Region.SelectedSpaceName = ".";
            maskcopytool.Run();
        }

        private void MaskRegion(CogCompositeShape mreg)
        {
            maskcopytool.RunParams.FillRegion = true;
            maskcopytool.RunParams.FillRegionValue = 60;
            maskcopytool.RunParams.FillBoundingBox = false;
            maskcopytool.InputImage = IPImage;            
            maskcopytool.DestinationImage = IPImage;
            mreg.SelectedSpaceName = ".";
            maskcopytool.Region = mreg;
            maskcopytool.Run();
        }

        private bool GetSearchResult(CogImage8Grey img, CogToolBlock searchtool, bool check, ICogRegion region, List<CogCompositeShape> shapelist, CogColorConstants indicator = CogColorConstants.Green, string param = null)
        {
            if (searchtool == null) return false;
            searchtool.Inputs["Input"].Value = img;
            searchtool.Inputs["Region"].Value = region;
            searchtool.Inputs["Param"].Value = param;
            searchtool.Run();
            if (searchtool.Outputs["MaskRegion2"].Value != null)
            {
                shapelist.Add((CogCompositeShape)searchtool.Outputs["MaskRegion2"].Value);
                disrst.Add((CogCompositeShape)searchtool.Outputs["MaskRegion2"].Value);//add shapes
                //MaskDspReg.Add((CogCompositeShape)searchtool.Outputs["MaskRegion"].Value);//adding mask region for later display
            }
            if (searchtool.RunStatus.Result == CogToolResultConstants.Accept)
            {
                return true;
            }

            return false;
        }
        public Dictionary<string, TrayInspectionRst> FinalInspectionRsts;
        public override bool RunFunction()
        {
            //ie. wait for trigger event
            while (!StartProcessingEvt.WaitOne(50))
            {
                if (this.EquipmentState.GetState() == MachineState.EqResetState) throw new Exception("Seq Reset Detected");
                Thread.Sleep(100);
                if (!pMode.gevt.GetTerminate())
                    throw new Exception("terminate");
            }
            try
            {
                FinalInspectionRsts = new Dictionary<string, TrayInspectionRst>();
                CogStopwatch sw = new CogStopwatch();
                sw.Start();
                disrst.Clear();
                disp.Clear();
                StartProcessingEvt.Reset();
                tbFindTray.Inputs["Input"].Value = InputImage;
                tbFindTray.Run();

                if (tbFindTray.RunStatus.Result == CogToolResultConstants.Accept)
                {
                    //int i = 0;
                    OutputMaskImage = new CogImage8Grey((CogImage8Grey)tbFindTray.Outputs["Output"].Value);
                    IPImage = new CogImage8Grey(OutputMaskImage);
                    foreach (TrayPocketInpsection inspect in BlockInpsection)
                    {
                        inspect.enablevisiondebug = this.visiondebug;
                        inspect.TriggerSearch(IPImage, XAllowance, YAllowance);
                    }

                    //wait for process complete
                    List<ManualResetEvent> evtlist = new List<ManualResetEvent>();
                    foreach (TrayPocketInpsection inspect in BlockInpsection)
                    {
                        //inspect.WaitProcessComplete();//wait for process complete
                        evtlist.Add(inspect.ProcessCompleteEvt);
                    }
                    ManualResetEvent[] evtarray = evtlist.ToArray();
                    log.Debug("total time before wait" + sw.Seconds.ToString());
                    ManualResetEvent.WaitAll(evtarray);

                    log.Debug("total time " + sw.Seconds.ToString());
                    //masking process
                    for (int i = 0; i < this.BlockInpsection.Count; i++)
                    {
                        foreach (CogCompositeShape s in BlockInpsection[i].MaskDspReg)
                        {
                            disrst.Add(s);//add shapes
                            MaskRegion(s);
                        }
                        foreach (CogCompositeShape s in BlockInpsection[i].DspRegshape)
                        {
                            disp.Add(s);
                        }
                        foreach (KeyValuePair<string, TrayInspectionRst> e in BlockInpsection[i].InspectionRsts)
                        {
                            FinalInspectionRsts.Add(e.Key, e.Value);
                        }
                    }
                    //end of masking
                    //final inspection
                    /*may not need this final inspection anymore
                    List<CogCompositeShape> slist = new List<CogCompositeShape>();
                    GetSearchResult(IPImage, tbPartLocate, true, TraySearchReg, slist, CogColorConstants.Red, null);
                    foreach (CogCompositeShape s in slist)
                    {
                        MaskRegion(s);
                        BlockInpsection[BlockInpsection.Count - 1].MaskDspReg.Add(s);
                    }
                    slist.Clear();
                    GetSearchResult(IPImage, tbDeathBug, true, TraySearchReg, slist, CogColorConstants.Red, null);//dont think it is used in tbdeathbug.. need to add code in
                    foreach (CogCompositeShape s in slist)
                    {
                        MaskRegion(s);
                        BlockInpsection[BlockInpsection.Count - 1].MaskDspReg.Add(s);
                    }
                     * */

                }
                ProcessCompleteEvt.Set();
                sw.Stop();
                overallsearchtime = sw.Seconds;
            }
            catch (Exception ex) { log.Debug(ex.ToString()); }
            return true;
        }

        public CogImage8Grey GetRegImage(CogRectangle rect, CogImage8Grey img)
        {
            regcopytool = new CogCopyRegionTool();
            regcopytool.Region = rect;
            regcopytool.InputImage = img;
            regcopytool.Run();
            return (CogImage8Grey)regcopytool.OutputImage;
        }

        public void TriggerSearch(CogImage8Grey img)
        {
            // txformtool.InputImage = img;
            // txformtool.Run();
            //InputImage = (CogImage8Grey)txformtool.OutputImage;
            InputImage = new CogImage8Grey(img);
            ProcessCompleteEvt.Reset();
            StartProcessingEvt.Set();
        }

        public CogImage8Grey IPImage { get; set; }

        public void Update()
        {
            foreach (TrayPocketInpsection t in BlockInpsection)
            {
                t.Update(this.tbPartLocate, this.tbDetectOffPocket, this.tbDeathBug);
            }
        }

        internal void Init(GenericEvents mcEvents, double yieldfactor,//txform the resolution
          int blockno, CogToolBlock tbFindTray,
                       CogToolBlock tbPartLocate,
                       CogToolBlock tbDetectOffPocket,
                       CogToolBlock tbDeathBug,
                       CogRectangle TraySearchRegion,
                       int Row,
                       int Column,
                       string processname, string param)
        {            
            runtimeparam = param;
            disrst = new List<CogCompositeShape>();
            disp = new List<CogCompositeShape>();
            fixture = new CogFixtureTool();
            ProcessName = processname;
            this.mcEvents = mcEvents;
            //mask setup
            maskcopytool = new CogCopyRegionTool();
            //maskcopytool.RunParams.FillRegion = true;
            maskcopytool.RunParams.FillRegionValue = 0;
            maskcopytool.RunParams.FillBoundingBoxValue = 0;
            maskcopytool.RunParams.ImageAlignmentEnabled = true;
            //end of mask setup
            StartProcessingEvt = new ManualResetEvent(false);
            ProcessCompleteEvt = new ManualResetEvent(false);
            //need to create a script for tbFindTray so that it is not dependent on the connecting lines
            this.tbFindTray = tbFindTray;//need to do a deepcopy this.tbFindTray = (CogToolBlock)CogSerializer.DeepCopyObject(tbFindTray);
            this.tbPartLocate = tbPartLocate;
            this.tbDetectOffPocket = tbDetectOffPocket;
            this.tbDeathBug = tbDeathBug;

            txformtool = (CogAffineTransformTool)this.tbFindTray.Tools["CogAffineTransformTool1"];//set transform factor
            txformtool.Region = null;
            txformtool.RunParams.ScalingX = yieldfactor;
            txformtool.RunParams.ScalingY = yieldfactor;
            fixture = (CogFixtureTool)tbFindTray.Tools["TrayFixture"];
            //setting up search regions in tray
            TrayCol = Column; TrayRow = Row;
            TraySearchReg = TraySearchRegion;
            CellList = GenerateCellList(TraySearchRegion);
            //create block system
            BlockInpsection = new List<TrayPocketInpsection>();
            numofBlock = blockno;
            BlockCol = TrayCol / blockno;
            int cellblockno = BlockCol * TrayRow;
            int k = 0;
            for (int i = 0; i < blockno; i++)
            {
                //assigning cell block to individual block process
                List<CogRectangle> blkcellist = new List<CogRectangle>();
                for (int j = i * cellblockno;
                         j < (i * cellblockno + cellblockno);
                         j++)
                {
                    blkcellist.Add(CellList[j]);
                    k++;
                }
                if ((k < CellList.Count) && (i == (blockno - 1)))//make sure all cells are accounted for
                {
                    for (int j = k; k < CellList.Count; j++)
                    {
                        blkcellist.Add(CellList[j]);
                        k++;
                    }
                }
                //end of cell assignment
                //instantiate inpsectionblock
                TrayPocketInpsection Inspectionblock = new TrayPocketInpsection();
                Inspectionblock.Init(mcEvents, tbPartLocate, tbDetectOffPocket, tbDeathBug, blkcellist, "ProcessBlock" + i.ToString());//this need to be changed
                Inspectionblock.runparam = param;
                BlockInpsection.Add(Inspectionblock);
                //end of inspection blocks instatitate
            }
            EquipmentState = new GenericState();
            EquipmentState.SetState(MachineState.Run);
            pMode = new ProcessMode();
            pMode.Init(mcEvents);//set the state for the process
            pMode.EquipmentState = EquipmentState;
            CmdThread = new Thread(new ThreadStart(CmdThreadFn));
            CmdThread.Start();
        }

        public void ReassignProcessBlockList()
        {
            CellList = GenerateCellList(TraySearchReg);
            BlockCol = TrayCol / numofBlock;
            int cellblockno = BlockCol * TrayRow;
            int k = 0;
            for (int i = 0; i < numofBlock; i++)
            {
                //assigning cell block to individual block process
                List<CogRectangle> blkcellist = new List<CogRectangle>();
                for (int j = i * cellblockno;
                         j < (i * cellblockno + cellblockno);
                         j++)
                {
                    blkcellist.Add(CellList[j]);
                    k++;
                }
                if ((k < CellList.Count) && (i == (numofBlock - 1)))//make sure all cells are accounted for
                {
                    for (int j = k; k < CellList.Count; j++)
                    {
                        blkcellist.Add(CellList[j]);
                        k++;
                    }
                }
                BlockInpsection[i].SetRegionList(blkcellist);
                BlockInpsection[i].runparam = runtimeparam;
            }
        }

        public CogToolBlock tbFindTray { get; set; }

        public CogToolBlock tbPartLocate { get; set; }

        public CogToolBlock tbDetectOffPocket { get; set; }

        public CogToolBlock tbDeathBug { get; set; }

        public bool visiondebug { get; set; }
    }
    public class TrayInspectionRst
    {
        public CogRectangle rec;
        public CogGraphicLabel label; 
        public string rststring;

        public string detectcode { get; set; }
    }
    public class TrayPocketInpsection : Process
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private CogPMAlignTool fsearch_0, fsearch_180, fsearch_DB, fsearch_EmptyPkt;
        private CogHistogramTool LightSensor;
        private CogImage8Grey InputImage;
        public List<CogRectangle> RegionList;
        public ManualResetEvent StartProcessingEvt, ProcessCompleteEvt;
        private CogCopyRegionTool regcopytool;
        private CogCopyRegionTool maskcopytool;
        public CogImage8Grey OutputMaskImage;

        // public List<CogRectangleAffine> MaskDspReg;
        public List<CogCompositeShape> MaskDspReg;//DspRegshape
        public List<CogCompositeShape> DspRegshape;//DspRegshape
        public string ProcessName;

        public Dictionary<string, TrayInspectionRst> InspectionRsts;

        public TrayPocketInpsection()
        {
            EquipmentState = new GenericState();
            DisplayState = new GenericState();
            DisplayState.SetState(MachineState.Run);
            EquipmentState.SetState(MachineState.Run);
        }

        public void Init(GenericEvents mcEvents,
          CogPMAlignTool fiducialsearch_0,
          CogPMAlignTool fiducialsearch_180,//change this into an array
          CogPMAlignTool DeathBug,
          CogPMAlignTool EmptyPocket,
          CogHistogramTool BrighnessTool,
          List<CogRectangle> reglist,
          string processname)
        {
            ProcessName = processname;
            MaskDspReg = new List<CogCompositeShape>();
            DspRegshape = new List<CogCompositeShape>();
            regcopytool = new CogCopyRegionTool();
            //mask setup
            maskcopytool = new CogCopyRegionTool();
            maskcopytool.RunParams.FillRegion = true;
            maskcopytool.RunParams.FillRegionValue = 80;
            maskcopytool.RunParams.FillBoundingBoxValue = 80;
            maskcopytool.RunParams.ImageAlignmentEnabled = true;
            //end of mask setup
            StartProcessingEvt = new ManualResetEvent(false);
            ProcessCompleteEvt = new ManualResetEvent(false);
            if (fiducialsearch_0 != null)
                fsearch_0 = new CogPMAlignTool(fiducialsearch_0); //only search in pocket
            if (fiducialsearch_180 != null)
                fsearch_180 = new CogPMAlignTool(fiducialsearch_180); //search everything include 180 degree
            if (DeathBug != null)
                fsearch_DB = new CogPMAlignTool(DeathBug);//search death bug
            if (EmptyPocket != null)
                fsearch_EmptyPkt = new CogPMAlignTool(EmptyPocket);
            if (BrighnessTool != null)
                LightSensor = new CogHistogramTool(BrighnessTool);
            RegionList = reglist;
            pMode = new ProcessMode();
            pMode.Init(mcEvents);//set the state for the process
            CmdThread = new Thread(new ThreadStart(CmdThreadFn));
            CmdThread.Start();
        }

        public void SetRegionList(List<CogRectangle> reglist)
        {
            RegionList = reglist;
        }

        public override bool RunInitialization()
        {
            //run this once when the module is initialized
            Thread.Sleep(100);
            return true;
        }

        public override bool RunFunction()
        {
            //ie. wait for trigger event
            while (!StartProcessingEvt.WaitOne(50))
            {
                if (this.EquipmentState.GetState() == MachineState.EqResetState) throw new Exception("Seq Reset Detected");
                if (!pMode.gevt.GetTerminate())
                    throw new Exception("terminate");
                Thread.Sleep(100);
            }
            InspectionRsts = new Dictionary<string, TrayInspectionRst>();
            StartProcessingEvt.Reset();
            MaskDspReg.Clear();
            DspRegshape.Clear();
            OutputMaskImage = new CogImage8Grey(InputImage);
            maskcopytool.InputImage = InputImage;
            maskcopytool.DestinationImage = OutputMaskImage;
            foreach (CogRectangle rect in RegionList)//iterate through every pocket assigned
            {
                Thread.Sleep(10);
                CogImage8Grey img = GetRegImage(rect);
                runparam = rect.TipText + "," + (enablevisiondebug? "1":"0");
                string process = ProcessName;

                //1. do fiducual search with limited region
                //2. do fiducial search 180 degree and bigger region within pocket
                //3. do Death Bug search [search for array]
                //4. do empty pocket search
                //5. Mask found search location
                //6. set result
                //first search
                string outparam = null;
                CogImage8Grey outputimage = null;
                CogImage8Grey dummyimage = null;
                ICogRegion outputregion = null;
                ICogRegion dummyregion = null;
                try
                {
                    CogStopwatch sw = new CogStopwatch();
                    sw.Start();
                    
                   // tbDeathBug.Inputs["Input"].Value = img;
                  //  log.Debug("deathbug " + sw.Seconds.ToString());
                    if (GetSearchResult(img, tbPartLocate, true, null, out outparam, out outputimage, out outputregion, runparam, CogColorConstants.Green))
                    {
                        
                        ////graphics takes up 0.5 sec
                        TrayInspectionRst rst = new TrayInspectionRst() { rec = new CogRectangle(rect), rststring = outparam };
                        rst.rec.X = rst.rec.X + YAllowance / 2 + 50;
                        rst.rec.Y = rst.rec.Y + XAllowance / 2 + 50;
                        rst.rec.Height = rst.rec.Height - YAllowance - 100;//10 pixel allowance
                        rst.rec.Width = rst.rec.Width - XAllowance - 100;//10 pixel allowance
                        rst.rec.LineWidthInScreenPixels = 5;
                        rst.label = new CogGraphicLabel();
                        string[] rstarry = outparam.Split('-');
                        if (rstarry[0] == "EP33")
                        {
                            rst.label.Text = "E";
                            rst.label.Color = CogColorConstants.Magenta;
                        }
                        else
                            if (rstarry[0] == "PP")
                            {
                                rst.label.Text = "P";
                                rst.label.Color = CogColorConstants.Green;
                            }
                            else
                            {
                                rst.label.Text = "X";
                                if (rstarry[0] == "EP88") rst.detectcode = rstarry[1];
                                rst.label.Color = CogColorConstants.Red;
                            }
                        rst.rec.Color = rst.label.Color;
                        rst.label.Rotation = Math.PI * 270 / 180.0;
                        rst.label.Alignment = CogGraphicLabelAlignmentConstants.BottomCenter;
                        rst.label.X = rst.rec.CenterX;
                        rst.label.Y = rst.rec.CenterY;
                        InspectionRsts.Add(rect.TipText, rst);
                        sw.Stop();
                        log.Debug("packagetime " + sw.Seconds.ToString());
                        continue;
                    }
                    TrayInspectionRst rst1 = new TrayInspectionRst() { rec = new CogRectangle(rect), rststring = outparam };
                    rst1.rec.X = rst1.rec.X + YAllowance / 2 + 50;
                    rst1.rec.Y = rst1.rec.Y + XAllowance / 2 + 50;
                    rst1.rec.Height = rst1.rec.Height - YAllowance - 100;//10 pixel allowance
                    rst1.rec.Width = rst1.rec.Width - XAllowance - 100;//10 pixel allowance
                    rst1.rec.LineWidthInScreenPixels = 5;
                    rst1.label = new CogGraphicLabel();
                    string[] rstarry1 = outparam.Split('-');
                    if (rstarry1[0] == "EP33")
                    {
                        rst1.label.Text = "E";
                        
                        rst1.label.Color = CogColorConstants.Magenta;
                    }
                    else
                        if (rstarry1[0] == "PP")
                        {
                            rst1.label.Text = "P";
                            rst1.label.Color = CogColorConstants.Green;

                        }
                        else
                        {
                            rst1.label.Text = "X";
                            if (rstarry1[0] == "EP88") rst1.detectcode = rstarry1[1];
                            rst1.label.Color = CogColorConstants.Red;
                        }
                    rst1.rststring = rst1.label.Text;
                    rst1.rec.Color = rst1.label.Color;
                    rst1.label.Alignment = CogGraphicLabelAlignmentConstants.BottomCenter;
                    rst1.label.X = rst1.rec.CenterX;
                    rst1.label.Y = rst1.rec.CenterY;
                    rst1.label.Rotation = Math.PI * 270 / 180.0;
                    
                    InspectionRsts.Add(rect.TipText, rst1);
                    /*if (GetSearchResult(img, tbDetectOffPocket, true, CogColorConstants.Green, runparam))
                        continue;
                     * */
                    runparam = outparam;//use is use for looking deathbug
                    if (outputimage != null)
                    {
                        GetSearchResult(outputimage, tbDeathBug, true, outputregion, out outparam, out dummyimage, out dummyregion, runparam, CogColorConstants.Green);
                        
                    }
                    else
                    {
                        GetSearchResult(img, tbDeathBug, true, outputregion, out outparam, out dummyimage, out dummyregion, runparam, CogColorConstants.Green);
                    }

                   
                }
                catch (Exception ex)
                {
                    int i = 0;
                }
                Thread.Sleep(10);
            }
            //completed
            ProcessCompleteEvt.Set();
            return true;
        }

        private double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        private bool GetSearchResult(CogImage8Grey img, CogToolBlock searchtool, bool check,ICogRegion inputregion  ,out string outparam, out CogImage8Grey outputimage,out ICogRegion outputregion,
            string param ,CogColorConstants indicator = CogColorConstants.Green)
        {
            if (searchtool == null)
            {
                outparam = null;
                outputimage = null;
                outputregion = null;
                return false;
            }
            outputimage = null;
            outparam = null;
            outputregion = null;
                //start test
            CogStopwatch sw = new CogStopwatch();
             sw.Start();
                string[] strarry = param.Split(',');
                int column = Int32.Parse(strarry[0]);
                int row = Int32.Parse(strarry[1]);
                CogToolBlock finalsearch = null;
                foreach (CogToolBlock tool in tblist)
                {
                    if ((tool.Name == "0,0,*,*") || (tool.Name == "AllRoundSearch2"))
                    {
                        if (tool.Name == "0,0,*,*")
                            finalsearch = tool;
                        continue;
                    }

                    //do the search here


                    string[] toolstr = tool.Name.Split(',');
                    int toolColmin = Int32.Parse(toolstr[0]);
                    int toolRowmin = Int32.Parse(toolstr[1]);
                    int toolColmax = 99999;
                    if (toolstr[2] != "*")
                        toolColmax = Int32.Parse(toolstr[2]);
                    int toolRowmax = 99999;
                    if (toolstr[3] != "*")
                        toolRowmax = Int32.Parse(toolstr[3]);


                    if ((toolstr[0] == toolstr[2]) && (toolstr[1] == toolstr[3]))//explicit cell is mentioned
                    {
                        if (row == toolRowmin)
                            if (row == toolRowmax)
                                if (column == toolColmin)
                                    if (column == toolColmax)
                                    {
                                        tool.Inputs[0].Value = img;
                                        tool.Inputs["Region"].Value = inputregion;
                                         tool.Inputs["Param"].Value = param;
                                        tool.Run();
                                       // tool.Outputs["MaskRegion"].Value = ((CogToolBlock)tool).Outputs["MaskRegion"].Value;
                                      //  tool.Outputs["MaskRegion2"].Value = ((CogToolBlock)tool).Outputs["MaskRegion2"].Value;
                                      //  tool.Outputs["OutputImage"].Value = ((CogToolBlock)tool).Outputs["OutputImage"].Value;
                                      //  tool.Outputs["OutputRegion"].Value = ((CogToolBlock)tool).Outputs["OutputRegion"].Value;
                                        outparam= (string)tool.Outputs["RstString"].Value;
                                        //tool.Outputs["disp"].Value = ((CogToolBlock)tool).Outputs["disp"].Value;

                                        return true;
                                    }
                    }
                    //search start
                    if (row >= toolRowmin)
                        if (row <= toolRowmax)
                            if (column >= toolColmin)
                                if (column <= toolColmax)
                                {
                                    tool.Inputs[0].Value = img;
                                    tool.Inputs["Region"].Value = inputregion;
                                    tool.Inputs["Param"].Value = param;
                                    //MessageBox.Show(row.ToString()+","+column.ToString());
                                    tool.Run();
                                    //tool.Outputs["MaskRegion"].Value = ((CogToolBlock)tool).Outputs["MaskRegion"].Value;
                                    //tool.Outputs["MaskRegion2"].Value = ((CogToolBlock)tool).Outputs["MaskRegion2"].Value;
                                    //tool.Outputs["OutputImage"].Value = ((CogToolBlock)tool).Outputs["OutputImage"].Value;
                                    //tool.Outputs["OutputRegion"].Value = ((CogToolBlock)tool).Outputs["OutputRegion"].Value;
                                    //tool.Outputs["RstString"].Value = ((CogToolBlock)tool).Outputs["RstString"].Value;
                                    //tool.Outputs["disp"].Value = ((CogToolBlock)tool).Outputs["disp"].Value;
                                    outparam = (string)tool.Outputs["RstString"].Value;
                                    return true;
                                }

                }

            if (outparam == null)
                {//cannot find any filter, run default
                    finalsearch.Inputs[0].Value = img;
                    finalsearch.Inputs["Region"].Value = inputregion;
                    finalsearch.Inputs["Param"].Value = param;
                    finalsearch.Run();
                    outparam = (string)finalsearch.Outputs["RstString"].Value;
              }


             log.Debug("new timing : " + sw.Seconds.ToString());

            sw.Stop();


            //end of test
           // CogStopwatch sw = new CogStopwatch();
           // sw.Start();
           //searchtool.Inputs[0].Value = img;
            //foreach (ICogTool tool in searchtool.Tools)
            //{
            //    if (tool.Name == "0,0,*,*")
            //    {
            //        ((CogToolBlock)tool).Inputs["Input"].Value = img;
            //        break;
            //    }
            //}
           // searchtool.Inputs["Region"].Value = inputregion;
           // searchtool.Inputs["Param"].Value = param;
          
           // log.Debug("cogtool time1 : " + sw.Seconds.ToString());
           // searchtool.Run();
           // log.Debug("cogtool run time2 : " + sw.Seconds.ToString());
            
           //sw.Stop();


         //   foreach (CogToolBlock tb in tblist)
         //   {
         //       if (tb.Name == "0,0,*,*")
         //       {
         //           sw.Reset();
         //           sw.Start();
         //           tb.Inputs[0].Value = img;
         //           tb.Inputs["Region"].Value = inputregion;
         //           tb.Inputs["Param"].Value = param;
         //           log.Debug("cogtool time with list1 : " + sw.Seconds.ToString());
         //           tb.Run();
         //           outparam = (string)tb.Outputs["RstString"].Value;
         //           log.Debug("cogtool time with list 2: " + sw.Seconds.ToString());
         //           sw.Stop();
         //       }
         //   }
         //outparam = (string)searchtool.Outputs["RstString"].Value ;
         //   outputimage = (CogImage8Grey)searchtool.Outputs["OutputImage"].Value;
         //   outputregion = (ICogRegion)searchtool.Outputs["OutputRegion"].Value;


            /*
            if (searchtool.Outputs["MaskRegion"].Value != null)
            {
                MaskDspReg.Add((CogCompositeShape)searchtool.Outputs["MaskRegion"].Value);//adding mask region for later display
            }
            try
            {
                if (searchtool.Outputs["MaskRegion2"].Value != null)
                {
                    MaskDspReg.Add((CogCompositeShape)searchtool.Outputs["MaskRegion2"].Value);//adding mask region for later display
                    DspRegshape.Add((CogCompositeShape)searchtool.Outputs["MaskRegion2"].Value);//adding mask region for later display
                }
            }
            catch (Exception ex) { }
            try 
            {
                if (searchtool.Outputs["disp"].Value != null)
                {
                   // DspRegshape.Add((CogCompositeShape)searchtool.Outputs["disp"].Value);//adding mask region for later display
                }

            }
            catch (Exception ex) { }
            if (searchtool.RunStatus.Result == CogToolResultConstants.Accept)
            {
                return true;
            }*/

           // return false;
            return true;
        }

        private bool GetSearchResult(CogImage8Grey img, CogPMAlignTool searchtool, bool check, CogColorConstants indicator = CogColorConstants.Green)
        {
            //if (searchtool == null) return false;
            //searchtool.InputImage = img;
            //searchtool.Run();
            //if (searchtool.Results != null)
            //{
            //    if (searchtool.Results.Count > 0)
            //    {
            //        CogDegreeTypeConverter cvt = new CogDegreeTypeConverter();

            //        double angle = searchtool.Results[0].GetPose().Rotation * (180.0 / Math.PI);
            //        angle = Math.Abs(angle);
            //        if ((angle > 2.3) && (check)) return false;
            //        CogRectangleAffine maskreg = ((CogRectangle)fsearch_0.SearchRegion).
            //            MapLinear((CogTransform2DLinear)searchtool.InputImage.GetTransform("@\\Fixture", "#"),
            //            CogCopyShapeConstants.All);
            //        CogTransform2DLinear txform = searchtool.Results[0].GetPose();
            //        maskreg.Rotation = txform.Rotation;
            //        maskreg.CenterX = txform.TranslationX;
            //        maskreg.CenterY = txform.TranslationY;
            //        CogCompositeShape shape = new CogCompositeShape();
            //        maskreg.SelectedSpaceName = ".";
            //        //maskreg.SelectedSpaceName = "@\\Fixture";
            //        maskreg.Color = indicator;
            //        maskreg.SelectedColor = indicator;
            //        MaskDspReg.Add(maskreg);//adding mask region for later display
            //        //           MaskRegion(maskreg);// problem with this technique
            //        return true;
            //    }
            //}
            return false;
        }

        private void MaskRegion(CogRectangleAffine mreg)
        {
            maskcopytool.InputImage = InputImage;
            maskcopytool.DestinationImage = OutputMaskImage;
            maskcopytool.Region = mreg;
            maskcopytool.Region.SelectedSpaceName = ".";
            maskcopytool.Run();
        }

        public CogImage8Grey GetRegImage(CogRectangle rect)
        {
            regcopytool.Region = rect;
            regcopytool.InputImage = InputImage;
            regcopytool.Run();
            return (CogImage8Grey)regcopytool.OutputImage;
        }
        List<CogToolBlock> tblist;
        public void TriggerSearch(CogImage8Grey img, double xallowance,double yallowance)
        {

            XAllowance = xallowance;
            YAllowance = yallowance;
            InputImage = new CogImage8Grey(img);
            OutputMaskImage = new CogImage8Grey(img);
            ProcessCompleteEvt.Reset();
            StartProcessingEvt.Set();
        }

        public void WaitProcessComplete()
        {
            while (!ProcessCompleteEvt.WaitOne(100))
            {
                if (this.EquipmentState.GetState() == MachineState.EqResetState) throw new Exception("Seq Reset Detected");
            }
            ProcessCompleteEvt.Reset();
        }

        public void Update(CogToolBlock tbPartLocate, CogToolBlock tbDetectOffPocket, CogToolBlock tbDeathBug)
        {
            if (this.tbPartLocate != null)
                this.tbPartLocate.Dispose();
            if (this.tbDetectOffPocket != null)
                this.tbDetectOffPocket.Dispose();
            if (this.tbDeathBug != null)
                this.tbDeathBug.Dispose();
            this.tbPartLocate = (CogToolBlock)CogSerializer.DeepCopyObject(tbPartLocate);
            this.tbDetectOffPocket = (CogToolBlock)CogSerializer.DeepCopyObject(tbDetectOffPocket);
            this.tbDeathBug = (CogToolBlock)CogSerializer.DeepCopyObject(tbDeathBug);
            tblist = new List<CogToolBlock>();
            foreach (ICogTool tool in this.tbPartLocate.Tools)
            {
                if (tool.Name != "AllRoundSearch2")
                {
                    tblist.Add((CogToolBlock)tool);
                }
            }
        }

        internal void Init(GenericEvents mcEvents, CogToolBlock tbPartLocate, CogToolBlock tbDetectOffPocket, CogToolBlock tbDeathBug, List<CogRectangle> reglist, string processname)
        {
            ProcessName = processname;
            EquipmentState = new GenericState();
            EquipmentState.SetState(MachineState.Run);
            MaskDspReg = new List<CogCompositeShape>();
            DspRegshape = new List<CogCompositeShape>();
            regcopytool = new CogCopyRegionTool();
            //mask setup
            maskcopytool = new CogCopyRegionTool();
            maskcopytool.RunParams.FillRegion = true;
            maskcopytool.RunParams.FillRegionValue = 80;
            maskcopytool.RunParams.FillBoundingBoxValue = 80;
            maskcopytool.RunParams.ImageAlignmentEnabled = true;
            //end of mask setup
            StartProcessingEvt = new ManualResetEvent(false);
            ProcessCompleteEvt = new ManualResetEvent(false);
            //create a deep copy of the object
            this.tbPartLocate = (CogToolBlock)CogSerializer.DeepCopyObject(tbPartLocate);
            this.tbDetectOffPocket = (CogToolBlock)CogSerializer.DeepCopyObject(tbDetectOffPocket);
            this.tbDeathBug = (CogToolBlock)CogSerializer.DeepCopyObject(tbDeathBug);
            RegionList = reglist;
            tblist = new List<CogToolBlock>();
            foreach (ICogTool tool in this.tbPartLocate.Tools)
            {
                if (tool.Name != "AllRoundSearch2")
                {
                    tblist.Add((CogToolBlock)tool);
                }
            }
            pMode = new ProcessMode();
            pMode.EquipmentState = EquipmentState;
            pMode.ProcessIdentifier = processname;
            pMode.Init(mcEvents);//set the state for the process
            CmdThread = new Thread(new ThreadStart(CmdThreadFn));
            CmdThread.Start();
        }

        public CogToolBlock tbPartLocate { get; set; }

        public CogToolBlock tbDetectOffPocket { get; set; }

        public CogToolBlock tbDeathBug { get; set; }

        public string runparam { get; set; }

        public bool enablevisiondebug { get; set; }

        public double YAllowance { get; set; }

        public double XAllowance { get; set; }
    }
}