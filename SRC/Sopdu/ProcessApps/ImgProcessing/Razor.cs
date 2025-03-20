using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.PixelMap;
using Cognex.VisionPro.QuickBuild;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using Cognex.VisionPro3D;
using Sopdu.Devices.CameraLink;
using Sopdu.helper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace Sopdu.ProcessApps.ImgProcessing
{
    public class Razor : Process
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #region Events

        public delegate void btnRunComplete();

        public event btnRunComplete btnruncomplete;

        [XmlIgnore]
        public ManualResetEvent StartProcessingEvt, ProcessCompleteEvt;

        #endregion Events

        #region RuntimeParameters

        private double _max;
        public double max { get { return _max; } set { _max = value; NotifyPropertyChanged("max"); } }
        private double _min;
        public double min { get { return _min; } set { _min = value; NotifyPropertyChanged("min"); } }
        private double _level;
        public double level { get { return _level; } set { _level = value; NotifyPropertyChanged("level"); } }
        private int _pocketperrow;
        public int pocketperrow { get { return _pocketperrow; } set { _pocketperrow = value; NotifyPropertyChanged("pocketperrow"); NotifyPropertyChanged("strpocketperrow"); } }

        //runtime generated data from visionpro recipe
        private int _pocketpercolum;

        public int pocketpercolumn { get { return _pocketpercolum; } set { _pocketpercolum = value; NotifyPropertyChanged("pocketpercolumn"); NotifyPropertyChanged("strpocketpercolumn"); } }

        public string strpocketperrow { get { return pocketperrow.ToString(); } }

        //runtime generated data from visionpro recipe
        public string strpocketpercolumn { get { return pocketpercolumn.ToString(); } }

        //display parameters
        private int _dispheight;

        public int dispheight { get { return _dispheight; } set { _dispheight = value; NotifyPropertyChanged("dispheight"); } }
        private int _dispZEmpty;
        public int dispZEmpty { get { return _dispZEmpty; } set { _dispZEmpty = value; NotifyPropertyChanged("dispZEmpty"); } }
        private int _dispZErr;
        public int dispZErr { get { return _dispZErr; } set { _dispZErr = value; NotifyPropertyChanged("dispZErr"); } }
        private int _dispZGood;
        public int dispZGood { get { return _dispZGood; } set { _dispZGood = value; NotifyPropertyChanged("dispZGood"); } }
        private int _engagePos;
        public int engagePos { get { return _engagePos; } set { _engagePos = value; NotifyPropertyChanged("engagePos"); } }
        private int _rcpPitch;
        public int rcpPitch { get { return _rcpPitch; } set { _rcpPitch = value; NotifyPropertyChanged("rcpPitch"); } }

        private int _SensorToS1Finger;
        public int SensorToS1Finger { get { return _SensorToS1Finger; } set { _SensorToS1Finger = value; NotifyPropertyChanged("SensorToS1Finger"); } }
        private int _SensorToS2Finger;
        public int SensorToS2Finger { get { return _SensorToS2Finger; } set { _SensorToS2Finger = value; NotifyPropertyChanged("SensorToS2Finger"); } }

        private int _gapRange;
        public int GapRange { get { return _gapRange; } set { _gapRange = value; NotifyPropertyChanged("GapRange"); } }


        #endregion RuntimeParameters

        #region Process Base class related members

        [XmlIgnore]
        public GenericEvents mcEvents;

        protected Thread CmdThread;

        #endregion Process Base class related members

        #region Cognex Stuff

        private CogStopwatch stopwatch;

        [XmlIgnore]
        public Cog3DDisplayV2 Display3d;

        private string _processtime;
        public string processtime { get { return _processtime; } set { _processtime = value; NotifyPropertyChanged("processtime"); } }
        private CogJobManager _cjm;

        [XmlIgnore]
        private CogJobManager _cjmtmp;

        [XmlIgnore]
        public CogJobManager cjmtmp
        {
            get { return _cjmtmp; }
            set
            {
                _cjmtmp = value;
                NotifyPropertyChanged("cjmtmp");
            }
        }

        private CogJob myJob01;
        private CogJobIndependent myIndependentJob01;

        [XmlIgnore]
        public CogToolGroup myTG;

        [XmlIgnore]
        public CogFixtureTool iptool1;

        [XmlIgnore]
        public CogIPOneImageTool iptool;
        [XmlIgnore]
        public CogPixelMapTool pixelmaptool;
        [XmlIgnore]
        public StripMapVision.StripBlock bk;

        [XmlIgnore]
        public CogImage16Range InputImage;

        //[XmlIgnore]
        //public List<CogRectangle> CellList;

        #endregion Cognex Stuff

        public void TriggerSearch(ProfileTrayImageInfo trayinfo)
        {
            currentrst = trayinfo;
            InputImage = new CogImage16Range(currentrst.trayimage);
            ProcessCompleteEvt.Reset();
            hacktrigger = true;
            StartProcessingEvt.Set();
            
        }

        public void TriggerSearchm(ProfileTrayImageInfo trayinfo)
        {
            currentrst = trayinfo;
            InputImage = new CogImage16Range(currentrst.trayimage);
            RunManual();

        }
        private bool hacktrigger = false;
        [XmlIgnore]
        public Dictionary<string, TrayInspectionRst> FinalInspectionRsts;
        public bool RunManual()//this is the same as the auto run.. 
        {      log.Debug("Vision Start Run");
                try
                {
                    FinalInspectionRsts = new Dictionary<string, TrayInspectionRst>();
                    CogStopwatch sw = new CogStopwatch();
                    sw.Start();
                    //StartProcessingEvt.Reset();
                    Run();


                    currentrst.trayresults = new Dictionary<string, string>();
                    foreach (ICogTool t in tb.Tools)
                    {
                        if (t.Name.Contains("Row"))
                        {
                            int colnum = int.Parse(t.Name.Remove(0, 3));
                            //int logicalcol = this.pocketpercolumn - colnum;
                            int logicalcol = colnum - 1;
                            string[] row = ((string)((CogToolBlock)t).Outputs["PktResult"].Value).Split(',');
                            int i = 0;

                            //need to change this part to just result
                            foreach (string s in row)
                            {
                                currentrst.trayresults.Add(logicalcol.ToString() + "," + (this.pocketperrow - i - 1).ToString(), s);
                                //currentrst.trayresults.Add(logicalcol.ToString() + "," + (this.pocketperrow - i).ToString(), s);
                                i++;
                            }
                        }
                    }
                    //push to fifo

                    //fiforesultlist.Add(currentrst);
                    ConcurrentBag<ProfileTrayImageInfo>  tt = new ConcurrentBag<ProfileTrayImageInfo>();
                    tt.Add(currentrst);
                    DisplayCycleManual(tt);
                    sw.Stop();
                    processtime = sw.Milliseconds.ToString("#.##");
                    myJob01_Stopped(null, null);
                    log.Debug("vision run complete");
                    return true;
                }
                catch (Exception ex)
                {
                    log.Debug("End Error " + ex.ToString());
                    return false;
                }
        }
        public override bool RunFunction()
        {
            //ie. wait for trigger event
            while (!StartProcessingEvt.WaitOne(100))
            {
                if (this.EquipmentState.GetState() == MachineState.EqResetState) throw new Exception("Seq Reset Detected");
                //Thread.Sleep(100);
                if (hacktrigger) { hacktrigger = false; StartProcessingEvt.Set(); break; }
                if (!pMode.gevt.GetTerminate())
                {
                    log.Debug(
                        "terminate signal rx");
                    throw new Exception("terminate");
                }
            }
            
            log.Debug("Vision Start Run");
            try
            {
                FinalInspectionRsts = new Dictionary<string, TrayInspectionRst>();
                CogStopwatch sw = new CogStopwatch();
                sw.Start();
                StartProcessingEvt.Reset();
                Run();

                currentrst.trayresults = new Dictionary<string, string>();
                foreach (ICogTool t in tb.Tools)
                {
                    if (t.Name.Contains("Row"))
                    {
                        int colnum = int.Parse(t.Name.Remove(0, 3));
                        //int logicalcol = this.pocketpercolumn - colnum;
                        int logicalcol = colnum - 1;
                        string[] row = ((string)((CogToolBlock)t).Outputs["PktResult"].Value).Split(',');
                        int i = 0;

                        //need to change this part to just result
                        foreach (string s in row)
                        {
                            currentrst.trayresults.Add(logicalcol.ToString() + "," + (this.pocketperrow - i - 1).ToString(), s);
                            //currentrst.trayresults.Add(logicalcol.ToString() + "," + (this.pocketperrow - i).ToString(), s);
                            i++;
                        }
                    }
                }
                //push to fifo

                fiforesultlist.Add(currentrst);

                sw.Stop();
                processtime = sw.Milliseconds.ToString("#.##");
                myJob01_Stopped(null, null);
                ProcessCompleteEvt.Set();
            }
            catch (Exception ex) { log.Debug(ex.ToString()); }
            log.Debug("Vision Run End");
            return true;
        }

        [XmlIgnore]
        public double overallsearchtime;

        public void Init(GenericEvents mcEvents)
        {
            fiforesultlist = new ConcurrentBag<ProfileTrayImageInfo>();
            processtime = "0.00";
            stopwatch = new CogStopwatch();
            CellDictionary = new Dictionary<string, CogRectangle>();
            imgfiletool = new CogImageFileTool();
            InitVisionFunction();
            StartProcessingEvt = new ManualResetEvent(false);
            ProcessCompleteEvt = new ManualResetEvent(false);
            EquipmentState = new GenericState();
            EquipmentState.SetState(MachineState.Run);
            pMode = new ProcessMode();
            this.mcEvents = mcEvents;
            pMode.Init(mcEvents);
            pMode.EquipmentState = EquipmentState;
            //load param to the job file
            updateparam();
            fixture = (CogFixtureTool)tb.Tools["CogFixtureTool1"];
            GenerateCellList(bk.Region);
            CmdThread = new Thread(new ThreadStart(CmdThreadFn));
            CmdThread.Start();
            DisplayThread = new Thread(new ThreadStart(DisplayThreadFn));
            DisplayThread.Start();
        }

        //update param in visionpro recipe
        public void updateparam()
        {
            foreach (ICogTool t in tb.Tools)
            {
                if (t.Name.Contains("Row"))
                {
                    int colnum = int.Parse(t.Name.Remove(0, 3));

                    ((CogToolBlock)t).Inputs["Min"].Value = min;
                    ((CogToolBlock)t).Inputs["Max"].Value = max;
                    ((CogToolBlock)t).Inputs["level"].Value = level;
                    ((CogToolBlock)t).Inputs["PocketPerRow"].Value = pocketperrow;
                }
                if (t.Name.Contains("CogFixtureTool2"))
                {
                    iptool1 = ((CogFixtureTool)t);
                }
                if (t.Name.Contains("CogIPOneImageTool1"))
                {
                    iptool = ((CogIPOneImageTool)t);
                }
                if (t.Name.Contains("CogPixelMapTool2"))
                {
                    pixelmaptool = ((CogPixelMapTool)t);
                }
            }
        }

        protected override void DisplayCycle()
        {
            bool canrun = false;
            ProfileTrayImageInfo k = null;
            if (fiforesultlist != null)//display system level message
            {
                if (fiforesultlist.Count() > 0)
                    if (this.fiforesultlist.TryTake(out k))
                    {
                        canrun = true;
                    }
                    else return;
                else return;
            }
            else
                return;
            //display data to be run on a seperate threads//
            if (!canrun) return;
            //get fifo result
            if (!maintainance) return;
            Cog3DRangeImageGraphic rImgG = new Cog3DRangeImageGraphic(k.trayimage);
            if (Display3d == null ) return;
            Display3d.Add(rImgG);
            Display3d.FitView();

            ICogTransform2D xform2D = iptool1.OutputImage.GetTransform("#", @"@\Sensor2D\Fixture02");
            //ICogTransform2D xform2D = iptool.OutputImage.GetTransform("#", ".");
            CogTransform2DRigid tx = new CogTransform2DRigid();
            CogRadian rd = new CogRadian(new CogDegree(90));
            tx.SetRotationTranslation(rd.Value, 0, 0);
            ICog3DTransform xform3D = InputImage.GetTransform3D("Sensor3D", "@");
            
            foreach (KeyValuePair<string, string> entry in k.trayresults)
            {
                if (CellDictionary.Count > 0)
                {
                    switch (entry.Value)
                    {
                        case "E":
                            CellDictionary[entry.Key].Color = CogColorConstants.Magenta;
                            break;

                        case "P":
                            CellDictionary[entry.Key].Color = CogColorConstants.Green;
                            break;

                        case "F":
                            CellDictionary[entry.Key].Color = CogColorConstants.Red;
                            break;
                    }
                }
            }

            foreach (KeyValuePair<string, CogRectangle> entry in CellDictionary)
            {
                // do something with entry.Value or entry.Key
                double mappedX, mappedY;
                bool visible;
                ushort heightVal;
                //figure out the result's coordinates in Sensor3D
                //#1: figure out the XY pixel coordinates from the XY mm values
                double ctrX = entry.Value.CenterX;
                double ctrY = entry.Value.CenterY;
                double refX = entry.Value.X + 5;
                double refY = entry.Value.Y + 5;
                xform2D.MapPoint(ctrX, ctrY, out mappedX, out mappedY);
                int x = (int)mappedX;
                int y = (int)mappedY;
                xform2D.MapPoint(refX, refY, out mappedX, out mappedY);
                int rx = (int)mappedX;
                int ry = (int)mappedY;

                try
                {
                    InputImage.GetPixel(rx, ry, out visible, out heightVal);
                    heightVal = (ushort)dispheight;//configurable in recipe
                    Cog3DVect3 centerOfResultInMM = xform3D.MapPoint(new Cog3DVect3(x, y, heightVal));
                    double boxSizeZ = 2; //some height for the bounding box just to make it 3D
                    if (entry.Value.Color == CogColorConstants.Magenta) boxSizeZ = this.dispZEmpty;//configurable in recipe
                    if (entry.Value.Color == CogColorConstants.Green) boxSizeZ = this.dispZGood;//configurable in recipe
                    if (entry.Value.Color == CogColorConstants.Red) boxSizeZ = this.dispZErr;//configurable in recipe

                    Cog3DBox b = new Cog3DBox();
                    b.SetOriginVertexXVectorYVectorZ(new Cog3DVect3(-entry.Value.Width / 2, -entry.Value.Height / 2, -boxSizeZ / 2), new Cog3DVect3(entry.Value.Width, 0, 0), new Cog3DVect3(0, entry.Value.Height, 0), boxSizeZ);
                    Cog3DTransformRotation rotation = new Cog3DTransformRotation(new Cog3DEulerXYZ(0, 0, 0));
                    b = b.MapShape(new Cog3DTransformRigid(rotation, centerOfResultInMM)) as Cog3DBox;

                    if (b != null)
                    {
                        Cog3DBoxGraphic bg = new Cog3DBoxGraphic(b);

                        bg.Opacity = 0.5;
                        bg.Color = entry.Value.Color;
                        bg.DisplayState = Cog3DGraphicDisplayStateConstants.SurfaceWithWireFrame;

                        Display3d.Add(bg, rImgG);
                    }
                }
                catch (Exception ex) { }
            }
            Display3d.SetView(new Cog3DVect3(0, 0, 0), new Cog3DVect3(100, 150, -150), new Cog3DVect3(0, 10, 100));
            Display3d.FitView();
        }

        protected  void DisplayCycleManual(ConcurrentBag<ProfileTrayImageInfo> resultlist)
        {
            bool canrun = false;
            ProfileTrayImageInfo k = null;
            if (resultlist != null)//display system level message
            {
                if (resultlist.Count() > 0)
                    if (resultlist.TryTake(out k))
                    {
                        canrun = true;
                    }
                    else return;
                else return;
            }
            else
                return;
            //display data to be run on a seperate threads//
            if (!canrun) return;
            //get fifo result
            if (!maintainance) return;
            Cog3DRangeImageGraphic rImgG = new Cog3DRangeImageGraphic(k.trayimage);
            if (Display3d == null) return;
            Display3d.Add(rImgG);
            Display3d.FitView();

            ICogTransform2D xform2D = iptool1.OutputImage.GetTransform("#", @"@\Sensor2D\Fixture02");
            //ICogTransform2D xform2D = iptool.OutputImage.GetTransform("#", ".");
            CogTransform2DRigid tx = new CogTransform2DRigid();
            CogRadian rd = new CogRadian(new CogDegree(90));
            tx.SetRotationTranslation(rd.Value, 0, 0);
            ICog3DTransform xform3D = InputImage.GetTransform3D("Sensor3D", "@");

            foreach (KeyValuePair<string, string> entry in k.trayresults)
            {
                if (CellDictionary.Count > 0)
                {
                    switch (entry.Value)
                    {
                        case "E":
                            CellDictionary[entry.Key].Color = CogColorConstants.Magenta;
                            break;

                        case "P":
                            CellDictionary[entry.Key].Color = CogColorConstants.Green;
                            break;

                        case "F":
                            CellDictionary[entry.Key].Color = CogColorConstants.Red;
                            break;
                    }
                }
            }

            foreach (KeyValuePair<string, CogRectangle> entry in CellDictionary)
            {
                // do something with entry.Value or entry.Key
                double mappedX, mappedY;
                bool visible;
                ushort heightVal;
                //figure out the result's coordinates in Sensor3D
                //#1: figure out the XY pixel coordinates from the XY mm values
                double ctrX = entry.Value.CenterX;
                double ctrY = entry.Value.CenterY;
                double refX = entry.Value.X + 5;
                double refY = entry.Value.Y + 5;
                xform2D.MapPoint(ctrX, ctrY, out mappedX, out mappedY);
                int x = (int)mappedX;
                int y = (int)mappedY;
                xform2D.MapPoint(refX, refY, out mappedX, out mappedY);
                int rx = (int)mappedX;
                int ry = (int)mappedY;

                try
                {
                    InputImage.GetPixel(rx, ry, out visible, out heightVal);
                    heightVal = (ushort)dispheight;//configurable in recipe
                    Cog3DVect3 centerOfResultInMM = xform3D.MapPoint(new Cog3DVect3(x, y, heightVal));
                    double boxSizeZ = 2; //some height for the bounding box just to make it 3D
                    if (entry.Value.Color == CogColorConstants.Magenta) boxSizeZ = this.dispZEmpty;//configurable in recipe
                    if (entry.Value.Color == CogColorConstants.Green) boxSizeZ = this.dispZGood;//configurable in recipe
                    if (entry.Value.Color == CogColorConstants.Red) boxSizeZ = this.dispZErr;//configurable in recipe

                    Cog3DBox b = new Cog3DBox();
                    b.SetOriginVertexXVectorYVectorZ(new Cog3DVect3(-entry.Value.Width / 2, -entry.Value.Height / 2, -boxSizeZ / 2), new Cog3DVect3(entry.Value.Width, 0, 0), new Cog3DVect3(0, entry.Value.Height, 0), boxSizeZ);
                    Cog3DTransformRotation rotation = new Cog3DTransformRotation(new Cog3DEulerXYZ(0, 0, 0));
                    b = b.MapShape(new Cog3DTransformRigid(rotation, centerOfResultInMM)) as Cog3DBox;

                    if (b != null)
                    {
                        Cog3DBoxGraphic bg = new Cog3DBoxGraphic(b);

                        bg.Opacity = 0.5;
                        bg.Color = entry.Value.Color;
                        bg.DisplayState = Cog3DGraphicDisplayStateConstants.SurfaceWithWireFrame;

                        Display3d.Add(bg, rImgG);
                    }
                }
                catch (Exception ex) { }
            }
            Display3d.SetView(new Cog3DVect3(0, 0, 0), new Cog3DVect3(100, 150, -150), new Cog3DVect3(0, 10, 100));
            Display3d.FitView();
        }

        public List<CogRectangle> GenerateCellList(CogRectangle searchreg)
        {
            TraySearchReg = searchreg;
            CogRectangle rect = new CogRectangle(searchreg);
            List<CogRectangle> celllist = new List<CogRectangle>();
            CellDictionary.Clear();
            double wpitch = rect.Width / this.pocketperrow;
            double hpitch = rect.Height / this.pocketpercolumn;
            for (int i = 0; i < pocketperrow; i++)
            {
                for (int j = 0; j < pocketpercolumn; j++)
                {
                    CogRectangle cellrect = new CogRectangle();
                    cellrect.LineWidthInScreenPixels = 10;
                    cellrect.SelectedLineWidthInScreenPixels = 10;
                    cellrect.Height = hpitch;
                    cellrect.Width = wpitch;
                    cellrect.X = rect.X + wpitch * i;
                    cellrect.Y = rect.Y + hpitch * j;
                    cellrect.Interactive = true;
                    //cellrect.TipText = (i.ToString() + "," + j.ToString());
                    cellrect.TipText = ((pocketpercolumn - j - 1).ToString() + "," + (pocketperrow - i - 1).ToString());
                    /*
                    cellrect.TipText = ((mainWindow.mainapp.razor.pocketpercolumn - j - 1).ToString() + "," + (mainWindow.mainapp.razor.pocketperrow-i-1).ToString());
                    cellrect.GraphicDOFEnable = CogRectangleDOFConstants.None;
                    cellrect.GraphicDOFEnableBase = CogGraphicDOFConstants.None;
                     * */

                    cellrect.GraphicDOFEnable = CogRectangleDOFConstants.None;
                    cellrect.GraphicDOFEnableBase = CogGraphicDOFConstants.None;
                    //cellrect.TipText = j.ToString() + "," + i.ToString();
                    celllist.Add(cellrect);
                    CellDictionary.Add(cellrect.TipText, cellrect);
                }
            }
            return celllist;
        }

        public Dictionary<string, CogRectangle> GenerateCellListDictionary(CogRectangle searchreg)
        {
            TraySearchReg = searchreg;
            CogRectangle rect = new CogRectangle(searchreg);
            List<CogRectangle> celllist = new List<CogRectangle>();
            Dictionary<string, CogRectangle> resturnCellDictionary = new Dictionary<string, CogRectangle>();
            double wpitch = rect.Width / this.pocketperrow;
            double hpitch = rect.Height / this.pocketpercolumn;
            for (int i = 0; i < pocketperrow; i++)
            {
                for (int j = 0; j < pocketpercolumn; j++)
                {
                    CogRectangle cellrect = new CogRectangle();
                    cellrect.LineWidthInScreenPixels = 10;
                    cellrect.SelectedLineWidthInScreenPixels = 10;
                    cellrect.Height = hpitch;
                    cellrect.Width = wpitch;
                    cellrect.X = rect.X + wpitch * i;
                    cellrect.Y = rect.Y + hpitch * j;
                    cellrect.Interactive = true;
                    //cellrect.TipText = (i.ToString() + "," + j.ToString());
                    cellrect.TipText = ((pocketpercolumn - j - 1).ToString() + "," + (pocketperrow - i - 1).ToString());
                    /*
                    cellrect.TipText = ((mainWindow.mainapp.razor.pocketpercolumn - j - 1).ToString() + "," + (mainWindow.mainapp.razor.pocketperrow-i-1).ToString());
                    cellrect.GraphicDOFEnable = CogRectangleDOFConstants.None;
                    cellrect.GraphicDOFEnableBase = CogGraphicDOFConstants.None;
                     * */

                    cellrect.GraphicDOFEnable = CogRectangleDOFConstants.None;
                    cellrect.GraphicDOFEnableBase = CogGraphicDOFConstants.None;
                    //cellrect.TipText = j.ToString() + "," + i.ToString();
                    celllist.Add(cellrect);
                    resturnCellDictionary.Add(cellrect.TipText, cellrect);
                }
            }
            return resturnCellDictionary;
        }

        private void InitVisionFunction()
        {
        }

        [XmlIgnore]
        public ConcurrentBag<ProfileTrayImageInfo> fiforesultlist;

        //[XmlIgnore]
        //public Dictionary<string, string> resultDictionary;

        [XmlIgnore]
        public ProfileTrayImageInfo currentrst;
        [XmlIgnore]
        public bool maintainance=false;
        private void myJob01_Stopped(object sender, CogJobActionEventArgs e)
        {
            if (maintainance) log.Debug("maintenance mode on");
            else log.Debug("maintenance mode off");
            try
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                Application.Current.Dispatcher.Invoke((Action)delegate// use begin invoke to avoid hung up
            {
                if (maintainance)
                {
                    log.Debug("maintenance mode on chunk");
                    if (fordisplay != null)
                    {
                        log.Debug("fordisplay not null");
                        fordisplay.Image = iptool.OutputImage;
                        fordisplay.Fit(true);
                    }
                    else { log.Debug("fordisplay null"); }
                    log.Debug("clear fordisplay");
                    if (secondDisplay != null)
                    {
                        log.Debug("secondDisplay not null");
                        secondDisplay.Image = iptool.OutputImage;
                        secondDisplay.Fit(true);
                    }
                    else log.Debug("secondDisplay null");
                    log.Debug("clear seconddisplay");
                    if (btnruncomplete != null)
                    {
                        log.Debug("run comeplte set");
                        btnruncomplete();
                        log.Debug("Application Exit");
                    }
                    else
                    { log.Debug("run comeplte not set"); }
                }
            });
            }
        }

        [XmlIgnore]
        public Dictionary<string, CogRectangle> CellDictionary;

        private CogDisplay fordisplay;
        private CogDisplay secondDisplay;

        [XmlIgnore]
        public ConcurrentBag<TrayImageInfo> fifotrayimgcomplete;//need to change this

        public void Run()
        {
            tb.Inputs["InputImage"].Value = InputImage;
            tb.Run();
        }

        internal void SetSecondaryDisplay(CogDisplay display, Cog3DDisplayV2 disp3d)
        {
            secondDisplay = display;
            Display3d = disp3d;
        }

        internal void SetImage(CogDisplay display)
        {
            fordisplay = display;
        }

        [XmlIgnore]
        public CogToolBlock tb { get; set; }

        [XmlIgnore]
        public CogImageFileTool imgfiletool { get; set; }
        [XmlIgnore]
        public CogRectangle TraySearchReg { get; set; }
        [XmlIgnore]
        public CogFixtureTool fixture { get; set; }
    }

    public class ProfileTrayImageInfo
    {
        public string serialnumber;
        public CogImage16Range trayimage;
        public CogImage16Range trayimagefordebug;
        public CogRectangle TraySearchReg;
        public Dictionary<string, string> trayresults;
    }
}