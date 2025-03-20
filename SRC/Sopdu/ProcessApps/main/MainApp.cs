using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.ToolBlock;
using Insphere.Connectivity.Application.SecsToHost;
using Sopdu.Devices;
using Sopdu.Devices.CameraLink;
using Sopdu.Devices.Cylinders;
using Sopdu.Devices.IOModule;
using Sopdu.Devices.KeyenceDistanceSensor;
using Sopdu.Devices.MotionControl.Base;
using Sopdu.Devices.SecsGem;
using Sopdu.Devices.Vision;
using Sopdu.helper;
using Sopdu.ProcessApps.ImgProcessing;
using Sopdu.ProcessApps.ProcessModules;
using Sopdu.StripMapVision;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml.Serialization;

namespace Sopdu.ProcessApps.main
{
    public class MainApp : GenericDevice
    {
        public MainApp()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            VersionInfo = "File ver : " + version + "   Assembly ver :" + assembly.GetName().Version.ToString();
        }
        [XmlIgnore]
        public ConcurrentQueue<string> shutterInitStartQuene = new ConcurrentQueue<string>();
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [XmlIgnore]
        private string _versionInfo;
        public string VersionInfo { get { return _versionInfo; } set { _versionInfo = value; NotifyPropertyChanged("VersionInfo"); } }

        [XmlIgnore]
        public IOController ioCtrl
        { get { return IOCtrl; } }
        private IOController IOCtrl;

        [XmlIgnore]
        public IOController opCtrl
        { get { return OutputCtrl; } }
        [XmlIgnore]
        public IOController OutputCtrl { get; set; }



        //test objects        
        public DiscreteIO Outputtest { get { return _Outputtest; } set { _Outputtest = value; NotifyPropertyChanged(); } }
        private DiscreteIO _Outputtest;
        public DiscreteIO Input { get { return _Input; } set { _Input = value; NotifyPropertyChanged(); } }
        private DiscreteIO _Input;
        //end of test objects

        public GenericEvents mcEvents { get { return _mcEvents; } set { _mcEvents = value; NotifyPropertyChanged("mcEvents"); } }
        public GenericEvents _mcEvents;

        public ProcessMaster pMaster { get { return _pMaster; } set { _pMaster = value; NotifyPropertyChanged("pMaster"); } }
        private ProcessMaster _pMaster;



        private DistanceSensor _keyenceS1;
        [XmlIgnore]
        public DistanceSensor KeyenceS1 { get { return _keyenceS1; } set { _keyenceS1 = value; NotifyPropertyChanged("KeyenceS1"); } }

        private DistanceSensor _keyenceS2;
        [XmlIgnore]
        public DistanceSensor KeyenceS2 { get { return _keyenceS2; } set { _keyenceS2 = value; NotifyPropertyChanged("KeyenceS2"); } }


        public Process IPModule;
        public Process TxferSys;
        public Process OPModule;

        public InputCV IPCvr;
        public InputStacker IPStkr;
        public OutputCV OPCvr;
        public OutputStacker OPStkr;
        public ShutterUnit Shut1, Shut2;

        private MenuObj _menu;
        public MenuObj menu { get { return _menu; } set { _menu = value; NotifyPropertyChanged("menu"); } }
        //public ObservableCollection<Process> Modules;

        private Axis _axis;
        public Axis axis { get { return _axis; } set { _axis = value; NotifyPropertyChanged("axis"); } }

        //[XmlIgnore]
        //public bool EnableVision { get; set; }
        [XmlIgnore]
        private bool _EnableVision = true;
        [XmlIgnore]
        public bool EnableVision
        {
            get { return _EnableVision; }
            set { _EnableVision = value; }
        }
        [XmlIgnore]
        private bool _EnableVisionDebug = true;
        [XmlIgnore]
        public bool EnableVisionDebug
        {
            get { return _EnableVisionDebug; }
            set { _EnableVisionDebug = value; }
        }
        //[XmlIgnore]
        //public bool EnableVisionDebug { get; set; }
        [XmlIgnore]
        public EqSecGem GemCtrl { get { return _GemCtrl; } set { _GemCtrl = value; NotifyPropertyChanged("GemCtrl"); } }
        private EqSecGem _GemCtrl;

        [XmlIgnore]
        public CogAcqFifoTool cogacqfifotool;
        [XmlIgnore]
        public TrayPktInspectionMgr TrayMgr01 { get; set; }
        [XmlIgnore]
        public TrayPktInspectionMgr TrayMgr02 { get; set; }
        [XmlIgnore]
        public List<TrayPktInspectionMgr> TrayMgrList { get; set; }

        private CogImage8Grey currentimg;
        private CogImage8Grey unprocessimg;

        //[XmlIgnore]
        //public Dictionary<string, Process> ModuleDirectory;
        private ObservableCollection<string> _RecipeList;
        [XmlIgnore]
        public ObservableCollection<string> RecipeList { get { return _RecipeList; } set { _RecipeList = value; NotifyPropertyChanged(); } }

        [XmlIgnore]
        public ConcurrentBag<TrayImageInfo> fifotrayimginfo;

        [XmlIgnore]
        public ConcurrentBag<TrayImageInfo> fifotrayimgcomplete;

        [XmlIgnore]
        public Substrate CurrSubstrateRecipe { get; set; }

        private UsbCamera _InputCVCamera;
        [XmlIgnore]
        public UsbCamera InputCVCamera { get { return _InputCVCamera; } set { _InputCVCamera = value; NotifyPropertyChanged("InputCVCamera"); } }

        /*
         * 
                            <Label Grid.Row="0" Grid.Column="2" Content="{Binding strCarrierID}"></Label>
                            <Label Grid.Row="1" Grid.Column="2" Content="{Binding CarrierHt}"></Label>
                            <Label Grid.Row="2" Grid.Column="2" Content="{Binding IpTrayCount}"></Label>
                            <Label Grid.Row="3" Grid.Column="2" Content="{Binding OpTrayCount}"></Label>
                            <Label Grid.Row="4" Grid.Column="2" Content="{Binding S01Code}"></Label>
                            <Label Grid.Row="5" Grid.Column="2" Content="{Binding S02Code}"></Label>
         * 
         * */

        private string _CurrentTrayID;
        [XmlIgnore]
        public string CurrentTrayID { get { return _CurrentTrayID; } set { _CurrentTrayID = value; NotifyPropertyChanged("CurrentTrayID"); } }


        private string _strCarrierID;
        [XmlIgnore]
        public string strCarrierID { get { return _strCarrierID; } set { _strCarrierID = value; NotifyPropertyChanged("strCarrierID"); } }

        private string _CarrierHt;
        [XmlIgnore]
        public string CarrierHt { get { return _CarrierHt; } set { _CarrierHt = value; NotifyPropertyChanged("CarrierHt"); } }

        private string _NextID;
        [XmlIgnore]
        public string NextID { get { return _NextID; } set { _NextID = value; NotifyPropertyChanged("NextID"); } }
        private string _IpTrayCount;
        [XmlIgnore]
        public string IpTrayCount { get { return _IpTrayCount; } set { _IpTrayCount = value; NotifyPropertyChanged("IpTrayCount"); } }

        private string _OpTrayCount;
        [XmlIgnore]
        public string OpTrayCount { get { return _OpTrayCount; } set { _OpTrayCount = value; NotifyPropertyChanged("OpTrayCount"); } }

        private string _GoodUnitCnt;
        [XmlIgnore]
        public string GoodUnitCnt { get { return _GoodUnitCnt; } set { _GoodUnitCnt = value; NotifyPropertyChanged("GoodUnitCnt"); } }

        private string _EmptyUnitCnt;
        [XmlIgnore]
        public string EmptyUnitCnt { get { return _EmptyUnitCnt; } set { _EmptyUnitCnt = value; NotifyPropertyChanged("EmptyUnitCnt"); } }

        public int gdUnit;
        public int emptyUnit;
        public int XUnit;

        private string _XUnitCnt;
        [XmlIgnore]
        public string XUnitCnt { get { return _XUnitCnt; } set { _XUnitCnt = value; NotifyPropertyChanged("XUnitCnt"); } }

        private string _S01Code;
        [XmlIgnore]
        public string S01Code { get { return _S01Code; } set { _S01Code = value; NotifyPropertyChanged("S01Code"); } }

        private string _S02Code;
        [XmlIgnore]
        public string S02Code { get { return _S02Code; } set { _S02Code = value; NotifyPropertyChanged("S02Code"); } }

        private Razor _razor;
        [XmlIgnore]
        public Razor razor { get { return _razor; } set { _razor = value; NotifyPropertyChanged("razor"); } }

        private Razor _razor1;
        [XmlIgnore]
        public Razor razor1 { get { return _razor1; } set { _razor1 = value; NotifyPropertyChanged("razor"); } }

        public void WriteBaseFile()
        {
            XmlSerializer tserializer;
            tserializer = new XmlSerializer(typeof(MenuObj));
            TextWriter wrt = new StreamWriter(@".\base.xml");
            tserializer.Serialize(wrt, menu);
        }
        public void ReadBaseFile()
        {
            XmlSerializer tserializer;
            tserializer = new XmlSerializer(typeof(MenuObj));
            TextReader wrt = new StreamReader(@".\base.xml");
            menu = (MenuObj)tserializer.Deserialize(wrt);
        }
        internal void saveVisionRecipe()
        {
            string bkdirectory = AppDomain.CurrentDomain.BaseDirectory + @"Recipe\" + this.menu.DefaultRecipe;  //generate physical directory for reference
            XmlSerializer tserializer;
            tserializer = new XmlSerializer(typeof(Razor));
            TextWriter wrt = new StreamWriter(bkdirectory + @"\razor.xml");
            tserializer.Serialize(wrt, razor);
            wrt.Close();

            CogSerializer.SaveObjectToFile(razor.bk.Region, bkdirectory + @"\Block0" + @"\region.vpp");
            CogSerializer.SaveObjectToFile(razor.tb, bkdirectory + @"\3Drecipe.vpp");
            CogSerializer.SaveObjectToFile(cogacqfifotool, bkdirectory + @"\ACQ.vpp");

            UpdateEngagePos();

        }
        public void ReadVisionFile(string recipe)
        {
            menu.DefaultRecipe = recipe;
            string bkdirectory = AppDomain.CurrentDomain.BaseDirectory + @"Recipe\" + this.menu.DefaultRecipe;  //generate physical directory for reference
            XmlSerializer tserializer;
            tserializer = new XmlSerializer(typeof(Razor));
            TextReader wrt = new StreamReader(bkdirectory + @"\razor.xml");
            razor = (Razor)tserializer.Deserialize(wrt);
            wrt.Close();
            //string bkdirectory = AppDomain.CurrentDomain.BaseDirectory;
            //bkdirectory = bkdirectory + @"\Recipe\" + this.menu.DefaultRecipe;//generate physical directory for reference
            StripBlock bk = new StripBlock()
            {
                BlockNumber = 0,
                column = razor.pocketpercolumn,
                row = razor.pocketperrow,
                Region = (CogRectangle)CogSerializer.LoadObjectFromFile(bkdirectory + @"\Block0" + @"\region.vpp")
            };
            try
            {
                razor.tb = (CogToolBlock)CogSerializer.LoadObjectFromFile(bkdirectory + @"\3Drecipe.vpp");
            }
            catch (Exception ex) { log.Error(ex.ToString()); }
            bk.Region.SelectedLineWidthInScreenPixels = 5;
            bk.Region.Color = Cognex.VisionPro.CogColorConstants.Red;
            bk.Region.TipText = "Block 0";
            razor.bk = bk;
            cogacqfifotool = (CogAcqFifoTool)CogSerializer.LoadObjectFromFile(bkdirectory + @"\ACQ.vpp");

            UpdateEngagePos();
        }
        public void ReadVisionFileCvrTray(string recipe)
        {
            menu.DefaultRecipe = recipe;
            string bkdirectory1 = AppDomain.CurrentDomain.BaseDirectory + @"Recipe\" + this.menu.DefaultRecipe;  //generate physical directory for reference
            XmlSerializer tserializer1;
            tserializer1 = new XmlSerializer(typeof(Razor));
            TextReader wrt1 = new StreamReader(bkdirectory1 + @"\razor.xml");
            razor1 = (Razor)tserializer1.Deserialize(wrt1);
            wrt1.Close();
            //string bkdirectory = AppDomain.CurrentDomain.BaseDirectory;
            //bkdirectory = bkdirectory + @"\Recipe\" + this.menu.DefaultRecipe;//generate physical directory for reference
            //StripBlock bk = new StripBlock()
            //{
            //    BlockNumber = 0,
            //    column = razor.pocketpercolumn,
            //    row = razor.pocketperrow,
            //    Region = (CogRectangle)CogSerializer.LoadObjectFromFile(bkdirectory + @"\Block0" + @"\region.vpp")
            //};
            //try
            //{
            //    razor.tb = (CogToolBlock)CogSerializer.LoadObjectFromFile(bkdirectory + @"\3Drecipe.vpp");
            //}
            //catch (Exception ex) { log.Error(ex.ToString()); }
            //bk.Region.SelectedLineWidthInScreenPixels = 5;
            //bk.Region.Color = Cognex.VisionPro.CogColorConstants.Red;
            //bk.Region.TipText = "Block 0";
            //razor.bk = bk;
            //cogacqfifotool = (CogAcqFifoTool)CogSerializer.LoadObjectFromFile(bkdirectory + @"\ACQ.vpp");

            UpdateEngagePoscvrtray();
        }
        public void Init()
        {
            //test code
            //string testmap = @"<MapData><Layouts><Layout LayoutId=""JEDEC TRAY"" DefaultUnits=""mm"" TopLevel=""true""><Dimension X=""1"" Y=""1"" /><ChildLayouts><ChildLayouts LayoutId=""13.41X14.21S7X17"" /></ChildLayouts></Layout><Layout LayoutId=""13.41X14.21S7X17"" DefaultUnits=""mm"" TopLevel=""true""><Dimension X=""7"" Y=""17"" /><ChildLayouts><ChildLayouts LayoutId=""13.41X14.21S7X17"" /></ChildLayouts></Layout></Layouts><Substrates><Substrate SubstrateType=""JEDEC TRAY"" SubstrateId=""0068500561"" /></Substrates><SubstrateMaps><SubstrateMap SubstrateType=""JEDEC TRAY"" SubstrateId=""0068500561"" LayoutSpecifier=""JEDEC TRAY/13.41X14.21S7X17"" Orientation=""270"" OriginLocation=""LowerLeft"" AxisDirection=""UpRight"" SubstrateSide=""TopSide""><Overlay MapName=""DeviceBinMap"" MapVersion=""1""><BinCodeMap NullBin=""0""><BinDefinitions><BinDefinition BinCode=""."" BinCount=""119"" BinDescription=""EMPTY"" Pick=""false"" /><BinDefinition BinCode=""X"" BinCount=""0"" BinDescription=""Present"" Pick="""" /></BinDefinitions><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode><BinCode>.......</BinCode></BinCodeMap></Overlay></SubstrateMap></SubstrateMaps></MapData>";

            //MapData mp = (MapData)XMLConverter.FromXml(testmap, typeof(MapData));
            //mp.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinDefinitions = new MapDataSubstrateMapsSubstrateMapOverlayBinCodeMapBinDefinition[]
            //                            { 
            //                                new MapDataSubstrateMapsSubstrateMapOverlayBinCodeMapBinDefinition(){BinCode = ".", BinDescription = "Empty", Pick = "false"},
            //                                new MapDataSubstrateMapsSubstrateMapOverlayBinCodeMapBinDefinition(){BinCode = "X", BinDescription = "Present", Pick = "true"},
            //                                new MapDataSubstrateMapsSubstrateMapOverlayBinCodeMapBinDefinition(){BinCode = "D", BinDescription = @"Double Stack/Unseated/Error", Pick = "false"}
            //                            };
            //mp.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinDefinitions[0].BinCount = (byte)1;//"." empty
            //mp.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinDefinitions[1].BinCount = (byte)2;//"X" present
            //mp.SubstrateMaps.SubstrateMap.Overlay.BinCodeMap.BinDefinitions[2].BinCount = (byte)3;// "error
            ////end test code

            fifotrayimginfo = new ConcurrentBag<TrayImageInfo>();
            fifotrayimgcomplete = new ConcurrentBag<TrayImageInfo>();
            MessageListener.Instance.ReceiveMessage("Attempt to setup Input Stacker Camera");
            InputCVCamera = UsbCamera.loadFile(@".\IPCamera.xml");
            InputCVCamera.Init();

            if (ConfigurationManager.AppSettings["EnableKeyence"] == "True")
            {
                int KeyenceS1Port = 0;
                int KeyenceS2Port = 0;
                string KeyenceS1IpAddress = null;
                string KeyenceS2IpAddress = null;
                try
                {
                    KeyenceS1Port = int.Parse(ConfigurationManager.AppSettings["KeyenceS1Port"]);
                    KeyenceS2Port = int.Parse(ConfigurationManager.AppSettings["KeyenceS2Port"]);
                    KeyenceS1IpAddress = ConfigurationManager.AppSettings["KeyenceS1IpAddress"];
                    KeyenceS2IpAddress = ConfigurationManager.AppSettings["KeyenceS2IpAddress"];
                    KeyenceS1 = new DistanceSensor(IPAddress.Parse(KeyenceS1IpAddress), KeyenceS1Port);
                    KeyenceS2 = new DistanceSensor(IPAddress.Parse(KeyenceS2IpAddress), KeyenceS2Port);
                }
                catch (Exception ex)
                {
                    log.Error("connection parameter error:" + ex.Message);
                }
            }
            
            if (ConfigurationManager.AppSettings["EnablePlasmaFan"] == "True")
            {
                GlobalVar.EnablePlasmaFan = true;
            }
                
            MessageListener.Instance.ReceiveMessage("Init Sec Gem");
            //*** secs gem init
            GemCtrl = new EqSecGem();
            GemCtrl.SetCommunicationState(true);
            GemCtrl.SetControlState(true);
            GemCtrl.SetProcessingState(ProcessingState.Idle);
            GemCtrl.SetEquipmentState(GemEquipmentState.Standby, "EquipmentState");
            GemCtrl.SetLoadPortReservationState01(LoadPortReservState.NotReserved, "LoadPortReservationStateNotReserved1");
            GemCtrl.SetLoadPortReservationState02(LoadPortReservState.NotReserved, "LoadPortReservationStateNotReserved2");
            if (GlobalVar.lstIPMode.Contains(EIPMode.OHT.ToString()))
            {
                // SetOHT to Auto
                GemCtrl.SetLoadPortAccessMode01(LoadPortAccessMode.Auto, "LoadPortAccessModeStateAuto1");
                //                    main.mainapp.GemCtrl.SetLoadPortAccessMode02(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual2");

            }
            else
            {
                // SetOHT to Manual
                GemCtrl.SetLoadPortAccessMode01(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual1");
                //                    main.mainapp.GemCtrl.SetLoadPortAccessMode02(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual2");


            }
            if (GlobalVar.eOPMode == EOPMode.OHT)
            {
                GemCtrl.SetLoadPortAccessMode02(LoadPortAccessMode.Auto, "LoadPortAccessModeStateAuto2");
            }
            else
            {
                GemCtrl.SetLoadPortAccessMode02(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual2");
            }
            // GemCtrl.SetLoadPortAccessMode01(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual1");
            // GemCtrl.SetLoadPortAccessMode02(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual2");
            //  GemCtrl.SetLoadPortTxferState02(LoadPortTxferState.TransferReady, "LoadPortTransferStateInServiceTransferReady2");
            //  GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.TransferReady, "LoadPortTransferStateInServiceTransferReady1");
            GemCtrl.SetLoadPortAssociateState01(LoadPortAssocState.NotAssociated, "LoadPortAssociationStateNotAssociated1");
            GemCtrl.SetLoadPortAssociateState02(LoadPortAssocState.NotAssociated, "LoadPortAssociationStateNotAssociated2");
            if (GemCtrl.gemComstate == null && GemCtrl.gemControlstate == "EQUIPMENT OFFLINE")
                GemCtrl.cmdHostSetCompleteEvt.WaitOne(2000);
            else
                GemCtrl.cmdHostSetCompleteEvt.WaitOne(20000);
            GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.InService, "LoadPortTransferStateInService1");
            Thread.Sleep(100);
            GemCtrl.SetLoadPortTxferState02(LoadPortTxferState.InService, "LoadPortTransferStateInService2");
            Thread.Sleep(100);
            GemCtrl.SetLoadPortTxferState02(LoadPortTxferState.TransferReady, "LoadPortTransferStateInServiceTransferReady2");
            Thread.Sleep(100);
            GemCtrl.SetLoadPortTxferState01(LoadPortTxferState.TransferReady, "LoadPortTransferStateInServiceTransferReady1");
            Thread.Sleep(100);
            //added new
            //*** end of secs gem init
            MessageListener.Instance.ReceiveMessage("Reading Base File");
            ReadBaseFile();
            MessageListener.Instance.ReceiveMessage("End of Reading Base File");
            Thread.Sleep(100);
            RecipeList = new ObservableCollection<string>();//dont need database. host suppose to tell me which recipe to use
            string[] stringlist = Directory.GetDirectories(@".\Recipe\");
            foreach (string str in stringlist)
            {
                string[] str11 = str.Split('\\');
                RecipeList.Add(str11[2]);
            }

            //load recipe
            string dir = @".\" + menu.DefaultRecipe + @"\";
            MessageListener.Instance.ReceiveMessage("Default Recipe Set as : " + menu.DefaultRecipe);
            Thread.Sleep(100);
            MessageListener.Instance.ReceiveMessage("Loading IO Configuration 01");
            Thread.Sleep(100);

            XmlSerializer recipeSerializer;
            recipeSerializer = new XmlSerializer(typeof(IOController));
            TextReader reader = new StreamReader(@".\IOConfig.xml");
            IOCtrl = (IOController)recipeSerializer.Deserialize(reader);
            reader.Close();
            MessageListener.Instance.ReceiveMessage("Loading IO Configuration 02");
            Thread.Sleep(100);

            reader = new StreamReader(@".\OutputConfig.xml");
            OutputCtrl = (IOController)recipeSerializer.Deserialize(reader);

            MessageListener.Instance.ReceiveMessage("Init IO Configuration 01");
            Thread.Sleep(100);
            IOCtrl.Init();
            //IOCtrl.OutputList.IOs[4].SetOutput(true);//softstart valve
            MessageListener.Instance.ReceiveMessage("Init IO Configuration 02");
            Thread.Sleep(100);
            OutputCtrl.Init();

            MessageListener.Instance.ReceiveMessage("Assigning Critical IOs");
            Thread.Sleep(100);
            Input = IOCtrl.InputList.IpDirectory["Input01"];
            //enf of test obj
            Towerlight tl;//= new Towerlight();
            XmlSerializer xmlreader = new XmlSerializer(typeof(Towerlight));
            TextReader rt = new StreamReader(@".\towerlite.xml");
            tl = (Towerlight)xmlreader.Deserialize(rt);
            AmberLed = IOCtrl.OutputList.IpDirectory["Output08"];
            Buzzer = IOCtrl.OutputList.IpDirectory["Output11"];
            rt.Close();
            tl.redop = IOCtrl.OutputList.IpDirectory["Output07"];
            tl.amberop = AmberLed;
            tl.amberop = IOCtrl.OutputList.IpDirectory["Output08"];
            tl.greenop = IOCtrl.OutputList.IpDirectory["Output09"];
            tl.blueop = IOCtrl.OutputList.IpDirectory["Output10"];
            tl.buzzerop = Buzzer;
            tl.buzzerop = IOCtrl.OutputList.IpDirectory["Output11"];
            tl.opResetLed = IOCtrl.OutputList.IpDirectory["Output03"];
            tl.opStartLed = IOCtrl.OutputList.IpDirectory["Output01"];
            tl.opStopLed = IOCtrl.OutputList.IpDirectory["Output02"];
            tl.ipResetBtn = IOCtrl.InputList.IpDirectory["Input03"];
            safetydoorlock = IOCtrl.OutputList.IpDirectory["Output06"];
            cstp = IOCtrl.OutputList.IpDirectory["Output15"];
            EMO = IOCtrl.OutputList.IpDirectory["Output12"];
            tl.BlinkCnt = 12;
            safetydoorlock.SetOutput(true);
            MessageListener.Instance.ReceiveMessage("Reading Event List");
            Thread.Sleep(100);
            ObservableCollection<ProcessEvt> EvtList = new ObservableCollection<ProcessEvt>();//only need this tmp for reading module file
            recipeSerializer = new XmlSerializer(typeof(ObservableCollection<ProcessEvt>));
            TextReader rd = new StreamReader(@".\ProcessEvt.xml");
            EvtList = (ObservableCollection<ProcessEvt>)recipeSerializer.Deserialize(rd);
            rd.Close();
            foreach (ProcessEvt evt in EvtList)
            {
                evt.evt = new ManualResetEvent(false);
            }
            MessageListener.Instance.ReceiveMessage("Read Event List Completed");
            Thread.Sleep(100);

            mcEvents = new GenericEvents();
            IOCtrl.SetGenericEvent(mcEvents);
            OutputCtrl.SetGenericEvent(mcEvents);

            MessageListener.Instance.ReceiveMessage("Reading Process List");
            Thread.Sleep(100);
            ObservableCollection<Process> Modules = new ObservableCollection<Process>();//only need this tmp for reading module file
            recipeSerializer = new XmlSerializer(typeof(ObservableCollection<Process>));
            reader = new StreamReader(@".\ProcessList.xml");
            Modules = (ObservableCollection<Process>)recipeSerializer.Deserialize(reader);//read all process modules
            reader.Close();
            MessageListener.Instance.ReceiveMessage("Process List Read Complete");
            Thread.Sleep(100);
            MessageListener.Instance.ReceiveMessage("Initiate All Processes");
            Thread.Sleep(100);
            pMaster = new ProcessMaster();
            pMaster.set_towerlight(tl);
            pMaster.GemCtrl = GemCtrl;
            pMaster.Init(mcEvents, IOCtrl, OutputCtrl, Modules, EvtList, menu);//may need to move this infront.... to init the process module


            InputCV ipcv = (InputCV)pMaster.ProcessList[0];
            ipcv.usbCamera = InputCVCamera;
            ipcv.mainapp = this;

            //custom name that need to be changed if swap over as gem based
            InputStacker ipstacker = (InputStacker)pMaster.ProcessList[1];//location at 1
            ipstacker.usbCamera = InputCVCamera;
            ipstacker.sCoverTrayPrefix = ipstacker.usbCamera.sCoverTrayPrefix;
            ipstacker.mainapp = this;
            //custom name that need to be changed if swap over as gem based
            OutputStacker opstacker = (OutputStacker)pMaster.ProcessList[4];//location at 

            opstacker.KeyenceS1 = KeyenceS1;
            opstacker.KeyenceS2 = KeyenceS2;
            opstacker.EnableKeyence = ConfigurationManager.AppSettings["EnableKeyence"] == "True";

            linescanprocess = (LineScanAcqProcess)pMaster.ProcessList[6];
            linescanprocess.pMaster = pMaster;
            //steps needed to load a new recipe
            ReadVisionFile(menu.DefaultRecipe);
            try
            {
                razor.Init(new GenericEvents());
            }
            catch { }

            linescanprocess.acqfifo = cogacqfifotool;
            //end of recipe load

            linescanprocess.fifotrayimginfoReq = fifotrayimginfo;
            linescanprocess.fifotrayimginfoCompete = fifotrayimgcomplete;
            //custom name that need to be changed if swap over as gem based
            imgprocessor = (ImageProcessor)pMaster.ProcessList[7];
            imgprocessor.fifotrayimginfoReq = fifotrayimgcomplete;
            imgprocessor.fifotrayimginfoCompete = new ConcurrentBag<TrayImageInfo>();
            imgprocessor.sCoverTrayPrefix = ipstacker.usbCamera.sCoverTrayPrefix;
            imgprocessor.main = this;

            IPCvr = (InputCV)pMaster.ProcessList[0];
            IPStkr = (InputStacker)pMaster.ProcessList[1];
            Shut1 = (ShutterUnit)pMaster.ProcessList[2];
            Shut2 = (ShutterUnit)pMaster.ProcessList[3];
            OPStkr = (OutputStacker)pMaster.ProcessList[4];
            OPCvr = (OutputCV)pMaster.ProcessList[5];

            OPCvr.mainapp = this;
            //opstacker.usbCamera = InputStackerCamera;
            opstacker.mainapp = this;
            //custom name that need to be changed if swap over as gem based check class type?
            ShutterUnit s = (ShutterUnit)pMaster.ProcessList[2];//location at 
            s.fifodata = fifotrayimginfo;
            s.ipstacker = ipstacker;
            s.opstacker = opstacker;
            s.mainapp = this;
            //custom name that need to be changed if swap over as gem based.. check class type?
            s = (ShutterUnit)pMaster.ProcessList[3];//location at 
            s.fifodata = fifotrayimginfo;
            s.ipstacker = ipstacker;
            s.opstacker = opstacker;
            s.mainapp = this;
            evtStart = ipstacker.evtRevCVRequest;
            TmpDebugEventFire = opstacker.evtTmpDebugEvent;
            MessageListener.Instance.ReceiveMessage("Processes Init Complete");
            //loading vision files//
            tmpWaitHandler = new ManualResetEvent(false);
            Thread tmpThread = new Thread(new ThreadStart(TmpThreadFn));
            tmpThread.Start();
            int i = 0;
            string[] tmpstr = new string[] { @"|", @"/", @"-", @"\" };
            while (!tmpWaitHandler.WaitOne(100))
            {
                MessageListener.Instance.ReceiveMessage("Loading Vision Files " + tmpstr[i]);
                if (i == 3) i = 0;
                else i++;
            }

            // imgprocessor.LoadVisionObject(TrayMgr01);//can only load TrayMgr01 here
            imgprocessor.Load3DVisionObject(razor);
            //end of loading vision files//
            GemCtrl.SetEquipmentState(GemEquipmentState.Engineering, "EquipmentState");
            pMaster.WarningDisplayVisible = "Hidden";
            pMaster.NormalDisplayVisible = "Visible";

            UpdateEngagePos();
        }

        private void TmpThreadFn()
        {
            VisionLoad2(menu.DefaultRecipe);
            tmpWaitHandler.Set();
        }
        private void UpdateEngagePos()
        {
            if (GlobalVar.isEngagePos)
            {
                if (IPStkr != null)
                {
                    for (int i = 0; i < IPStkr.AxisList[0].MotorAxis.PositionList.Count; i++)
                    {
                        if (IPStkr.AxisList[0].MotorAxis.PositionList[i].Name == sNameEngagePos)
                        {
                            IPStkr.AxisList[0].MotorAxis.PositionList[i].Coordinate = razor.engagePos;
                            //  break;
                        }
                        if (IPStkr.AxisList[0].MotorAxis.PositionList[i].Name == "SensorToS1Finger")
                        {
                            IPStkr.AxisList[0].MotorAxis.PositionList[i].Coordinate = razor.SensorToS1Finger;
                        }
                        if (IPStkr.AxisList[0].MotorAxis.PositionList[i].Name == "SensorToS2Finger")
                        {
                            IPStkr.AxisList[0].MotorAxis.PositionList[i].Coordinate = razor.SensorToS2Finger;
                        }
                    }
                }
            }
        }

        private void UpdateEngagePoscvrtray()
        {
            if (GlobalVar.isEngagePos)
            {
                if (IPStkr != null)
                {
                    for (int i = 0; i < IPStkr.AxisList[0].MotorAxis.PositionList.Count; i++)
                    {
                        if (IPStkr.AxisList[0].MotorAxis.PositionList[i].Name == sNameEngagePos)
                        {
                            IPStkr.AxisList[0].MotorAxis.PositionList[i].Coordinate = razor1.engagePos;
                            break;
                        }
                    }
                }
            }
        }

        public void loadvisiontoprocessor()
        {
            imgprocessor.LoadVisionObject(TrayMgr01);//can only load TrayMgr01 here
            //end of loading vision files//
        }
        public void VisionLoad(string recipe)   //loading of recipe
        {
            log.Debug("Vision Load :" + recipe);
            try
            {
                if (!Directory.Exists(@".\Recipe\" + recipe)) throw new Exception("Requested Recipe Not available");
                //will need to load a couple of instance for the vision
                XmlSerializer tserializer;
                menu.DefaultRecipe = recipe;
                tserializer = new XmlSerializer(typeof(Substrate));
                TextReader rt = new StreamReader(@".\Recipe\" + recipe + @"\traymap.xml");
                CurrSubstrateRecipe = (Substrate)tserializer.Deserialize(rt);
                rt.Close();
                string bkdirectory = @".\Recipe\" + recipe + @"\Block";//generate physical directory for reference
                CurrSubstrateRecipe.Blocks = new Dictionary<string, StripBlock>();
                CurrSubstrateRecipe.RecipeName = recipe;
                for (int i = 0; i < CurrSubstrateRecipe.numBlock; i++)//this have no use at this moment...
                {
                    StripBlock bk = new StripBlock()
                    {
                        BlockNumber = i,
                        column = CurrSubstrateRecipe.column,
                        row = CurrSubstrateRecipe.row,
                        Region = (CogRectangle)CogSerializer.LoadObjectFromFile(bkdirectory + i.ToString() + @"\region.vpp")
                    };
                    bk.Region.SelectedLineWidthInScreenPixels = 5;
                    bk.Region.Color = Cognex.VisionPro.CogColorConstants.Red;
                    bk.Region.TipText = "Block " + i.ToString();
                    CurrSubstrateRecipe.Blocks.Add(i.ToString(), bk);
                }
                //string runparam = String.Format("{0:0.##}", CurrSubstrateRecipe.Angleupper) + "," +
                //                  String.Format("{0:0.##}", CurrSubstrateRecipe.Anglelower) + "," +
                //                  String.Format("{0:0.##}", CurrSubstrateRecipe.Distanceupper) + "," +
                //                  String.Format("{0:0.##}", CurrSubstrateRecipe.Distancelower);
                string runparam = "0,0,0,0";
                CogRectangle SearchRegion = CurrSubstrateRecipe.Blocks["0"].Region;
                CogImageFileTool ftool = new CogImageFileTool();
                //need to change this to a permenant image
                bool testimgfileexist = true;
                try
                {
                    if (File.Exists(@".\Recipe\" + recipe + @"\TrainImage.bmp"))
                    {
                        ftool.Operator.Open(@".\Recipe\" + recipe + @"\TrainImage.bmp", CogImageFileModeConstants.Read);
                        ftool.Run();
                    }
                    else
                        testimgfileexist = false;
                }
                catch (Exception ex)
                {
                }
                if (testimgfileexist)
                {
                    currentimg = (CogImage8Grey)ftool.OutputImage;
                    unprocessimg = new CogImage8Grey((CogImage8Grey)ftool.OutputImage);
                }
                //end of default image
                log.Debug("Show Recipe Name : " + CurrSubstrateRecipe.RecipeName);
                System.Windows.MessageBox.Show("Show Recipe Name : " + CurrSubstrateRecipe.RecipeName);
                CurrSubstrateRecipe.cb = (CogToolBlock)CogSerializer.
                    LoadObjectFromFile(@".\Recipe\" + CurrSubstrateRecipe.RecipeName + @"\SearchPatternCB.vpp");

                CogToolBlock tbPartLocate = (CogToolBlock)CurrSubstrateRecipe.cb.Tools["PartLocate"];
                CogToolBlock tbDetectOffPocket = (CogToolBlock)CurrSubstrateRecipe.cb.Tools["DetectoffPocket"];
                CogToolBlock tbDeathBug = (CogToolBlock)CurrSubstrateRecipe.cb.Tools["DeathBug"];
                CogToolBlock tbFindTray = (CogToolBlock)CurrSubstrateRecipe.cb.Tools["FindTray"];//need to solve this.. we need to have more than 1 instance
                if (TrayMgr01 != null)
                {
                    TrayMgr01.mcEvents.SetTerminate(true);
                    if (TrayMgr01.BlockInpsection != null)
                        foreach (TrayPocketInpsection blk in TrayMgr01.BlockInpsection)
                        {
                            blk.Shutdown();
                        }
                    TrayMgr01.Shutdown();
                    log.Debug("TrayMgr01 Shutdown");
                }

                if (TrayMgr02 != null)
                {
                    TrayMgr02.mcEvents.SetTerminate(true);
                    if (TrayMgr02.BlockInpsection != null)
                        foreach (TrayPocketInpsection blk in TrayMgr02.BlockInpsection)
                        {
                            blk.Shutdown();
                        }
                    TrayMgr02.Shutdown();
                    log.Debug("TrayMgr02 Shutdown");
                }

                TrayMgr01 = new TrayPktInspectionMgr();
                TrayMgr01.ProcessIdentifier = "MyVision";
                //must use a different event...
                TrayMgr01.Init(new GenericEvents(), CurrSubstrateRecipe.yield, CurrSubstrateRecipe.numBlock,
                    tbFindTray,
                    tbPartLocate,
                    tbDetectOffPocket,
                    tbDeathBug,
                    SearchRegion,
                    CurrSubstrateRecipe.row,
                    CurrSubstrateRecipe.column,
                    CurrSubstrateRecipe.RecipeName, runparam);
                TrayMgr01.SetRegionValue(CurrSubstrateRecipe.XAllowance, CurrSubstrateRecipe.YAllowance);
                //TrayMgr02 = new TrayPktInspectionMgr();

                //TrayMgr02.Init(new GenericEvents(), CurrSubstrateRecipe.yield, CurrSubstrateRecipe.numBlock,
                //    (CogToolBlock)CogSerializer.DeepCopyObject(tbFindTray),
                //    tbPartLocate,
                //    tbDetectOffPocket,
                //    tbDeathBug,
                //    SearchRegion,
                //    CurrSubstrateRecipe.row,
                //    CurrSubstrateRecipe.column,
                //    CurrSubstrateRecipe.RecipeName, runparam);

                TrayMgrList = new List<TrayPktInspectionMgr>();
                TrayMgrList.Add(TrayMgr01);
                // TrayMgrList.Add(TrayMgr02);

                CogAffineTransformTool atf_tool = (CogAffineTransformTool)tbFindTray.Tools["CogAffineTransformTool1"];// will need to provide a transform tool for both empty pocket and pattern search
                atf_tool.RunParams.ScalingY = CurrSubstrateRecipe.yield;
                atf_tool.RunParams.ScalingX = CurrSubstrateRecipe.yield;
                atf_tool.Region = null;
                if (testimgfileexist)
                {
                    try
                    {
                        atf_tool.InputImage = (CogImage8Grey)ftool.OutputImage;
                        atf_tool.Run();//get txformed image
                        currentimg = (CogImage8Grey)atf_tool.OutputImage;
                        //test code here
                        CogPMAlignTool tool = (CogPMAlignTool)tbFindTray.Tools["FindTray"];
                        tool.InputImage = currentimg;
                        tool.Run();
                        TrayMgr01.fixture = (CogFixtureTool)tbFindTray.Tools["TrayFixture"];
                        TrayMgr01.fixture.InputImage = currentimg;
                        TrayMgr01.fixture.RunParams.UnfixturedFromFixturedTransform = tool.Results[0].GetPose();
                        TrayMgr01.fixture.Run();
                        TrayMgr01.InputImage = unprocessimg;
                    }
                    catch (Exception ex) { }
                }
            }
            catch (Exception ex)
            {
                log.Debug(ex.ToString());
            }
        }
        public void VisionLoad2(string recipe)  //loading of recipe
        {
            try
            {
                //will need to load a couple of instance for the vision
                XmlSerializer tserializer;
                menu.DefaultRecipe = recipe;
                tserializer = new XmlSerializer(typeof(Substrate));
                TextReader rt = new StreamReader(@".\Recipe\" + recipe + @"\traymap.xml");
                CurrSubstrateRecipe = (Substrate)tserializer.Deserialize(rt);
                rt.Close();
                string bkdirectory = @".\Recipe\" + recipe + @"\Block";//generate physical directory for reference
                CurrSubstrateRecipe.Blocks = new Dictionary<string, StripBlock>();
                CurrSubstrateRecipe.RecipeName = recipe;
                for (int i = 0; i < CurrSubstrateRecipe.numBlock; i++)//this have no use at this moment...
                {
                    StripBlock bk = new StripBlock()
                    {
                        BlockNumber = i,
                        column = CurrSubstrateRecipe.column,
                        row = CurrSubstrateRecipe.row,
                        Region = (CogRectangle)CogSerializer.LoadObjectFromFile(bkdirectory + i.ToString() + @"\region.vpp")
                    };
                    bk.Region.SelectedLineWidthInScreenPixels = 5;
                    bk.Region.Color = Cognex.VisionPro.CogColorConstants.Red;
                    bk.Region.TipText = "Block " + i.ToString();
                    CurrSubstrateRecipe.Blocks.Add(i.ToString(), bk);
                }
                //string runparam = String.Format("{0:0.##}", CurrSubstrateRecipe.Angleupper) + "," +
                //                  String.Format("{0:0.##}", CurrSubstrateRecipe.Anglelower) + "," +
                //                  String.Format("{0:0.##}", CurrSubstrateRecipe.Distanceupper) + "," +
                //                  String.Format("{0:0.##}", CurrSubstrateRecipe.Distancelower);
                string runparam = "0,0,0,0";
                CogRectangle SearchRegion = CurrSubstrateRecipe.Blocks["0"].Region;
                CogImageFileTool ftool = new CogImageFileTool();
                //need to change this to a permenant image
                bool testimgfileexist = true;
                try
                {
                    if (File.Exists(@".\Recipe\" + recipe + @"\TrainImage.bmp"))
                    {
                        ftool.Operator.Open(@".\Recipe\" + recipe + @"\TrainImage.bmp", CogImageFileModeConstants.Read);
                        ftool.Run();
                    }
                    else
                        testimgfileexist = false;
                }
                catch (Exception ex)
                {
                }
                if (testimgfileexist)
                {
                    currentimg = (CogImage8Grey)ftool.OutputImage;
                    unprocessimg = new CogImage8Grey((CogImage8Grey)ftool.OutputImage);
                }
                //end of default image

                //CurrSubstrateRecipe.cb = (CogToolBlock)CogSerializer.
                //    LoadObjectFromFile(@".\Recipe\" + CurrSubstrateRecipe.RecipeName + @"\SearchPatternCB.vpp");

                //  CogToolBlock tbPartLocate = (CogToolBlock)CurrSubstrateRecipe.cb.Tools["PartLocate"];
                //    CogToolBlock tbDetectOffPocket = (CogToolBlock)CurrSubstrateRecipe.cb.Tools["DetectoffPocket"];
                //    CogToolBlock tbDeathBug = (CogToolBlock)CurrSubstrateRecipe.cb.Tools["DeathBug"];
                //    CogToolBlock tbFindTray = (CogToolBlock)CurrSubstrateRecipe.cb.Tools["FindTray"];//need to solve this.. we need to have more than 1 instance
                if (TrayMgr01 != null)
                {
                    TrayMgr01.mcEvents.SetTerminate(true);
                    if (TrayMgr01.BlockInpsection != null)
                        foreach (TrayPocketInpsection blk in TrayMgr01.BlockInpsection)
                        {
                            blk.Shutdown();
                        }
                    TrayMgr01.Shutdown();
                    log.Debug("TrayMgr01 Shutdown");
                }

                if (TrayMgr02 != null)
                {
                    TrayMgr02.mcEvents.SetTerminate(true);
                    if (TrayMgr02.BlockInpsection != null)
                        foreach (TrayPocketInpsection blk in TrayMgr02.BlockInpsection)
                        {
                            blk.Shutdown();
                        }
                    TrayMgr02.Shutdown();
                    log.Debug("TrayMgr02 Shutdown");
                }

                // TrayMgr01 = new TrayPktInspectionMgr();
                //  TrayMgr01.ProcessIdentifier = "MyVision";
                //must use a different event...
                //TrayMgr01.Init(new GenericEvents(), CurrSubstrateRecipe.yield, CurrSubstrateRecipe.numBlock,
                //    tbFindTray,
                //    tbPartLocate,
                //    tbDetectOffPocket,
                //    tbDeathBug,
                //    SearchRegion,
                //    CurrSubstrateRecipe.row,
                //    CurrSubstrateRecipe.column,
                //    CurrSubstrateRecipe.RecipeName, runparam);
                //TrayMgr01.SetRegionValue(CurrSubstrateRecipe.XAllowance, CurrSubstrateRecipe.YAllowance);
                //TrayMgr02 = new TrayPktInspectionMgr();

                //TrayMgr02.Init(new GenericEvents(), CurrSubstrateRecipe.yield, CurrSubstrateRecipe.numBlock,
                //    (CogToolBlock)CogSerializer.DeepCopyObject(tbFindTray),
                //    tbPartLocate,
                //    tbDetectOffPocket,
                //    tbDeathBug,
                //    SearchRegion,
                //    CurrSubstrateRecipe.row,
                //    CurrSubstrateRecipe.column,
                //    CurrSubstrateRecipe.RecipeName, runparam);

                TrayMgrList = new List<TrayPktInspectionMgr>();
                // TrayMgrList.Add(TrayMgr01);
                // TrayMgrList.Add(TrayMgr02);

                //CogAffineTransformTool atf_tool = (CogAffineTransformTool)tbFindTray.Tools["CogAffineTransformTool1"];// will need to provide a transform tool for both empty pocket and pattern search
                //atf_tool.RunParams.ScalingY = CurrSubstrateRecipe.yield;
                //atf_tool.RunParams.ScalingX = CurrSubstrateRecipe.yield;
                //atf_tool.Region = null;
                if (testimgfileexist)
                {
                    try
                    {
                        //atf_tool.InputImage = (CogImage8Grey)ftool.OutputImage;
                        //atf_tool.Run();//get txformed image
                        //currentimg = (CogImage8Grey)atf_tool.OutputImage;
                        ////test code here
                        //CogPMAlignTool tool = (CogPMAlignTool)tbFindTray.Tools["FindTray"];
                        //tool.InputImage = currentimg;
                        //tool.Run();
                        //TrayMgr01.fixture = (CogFixtureTool)tbFindTray.Tools["TrayFixture"];
                        //TrayMgr01.fixture.InputImage = currentimg;
                        //TrayMgr01.fixture.RunParams.UnfixturedFromFixturedTransform = tool.Results[0].GetPose();
                        //TrayMgr01.fixture.Run();
                        //TrayMgr01.InputImage = unprocessimg;
                    }
                    catch (Exception ex) { }
                }
            }
            catch (Exception ex)
            {
                log.Debug("Load Recipe Error " + ex.ToString());
            }
        }

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public void Shutdown()
        {
            if (TrayMgr01 != null)
                TrayMgr01.mcEvents.SetTerminate(true);
            if (TrayMgr02 != null)
                TrayMgr02.mcEvents.SetTerminate(true);
            mcEvents.SetTerminate(true);
            pMaster.Shutdown();
            OutputCtrl.Shutdown();
            IOCtrl.Shutdown();
            if (this.TrayMgr01 != null)
            {
                foreach (TrayPocketInpsection blk in TrayMgr01.BlockInpsection)
                {
                    blk.Shutdown();
                }
                TrayMgr01.Shutdown();
            }

            if (this.TrayMgr02 != null)
            {
                foreach (TrayPocketInpsection blk in TrayMgr02.BlockInpsection)
                {
                    blk.Shutdown();
                }
                TrayMgr02.Shutdown();
            }
            try
            {
                InputCVCamera.Shutdown();
            }
            catch (Exception ex) { }
            if (razor != null)
            {
                //razor.cjm.Shutdown();
                razor.mcEvents.SetTerminate(true);
                razor.Shutdown();
            }

        }

        internal void ResetErr()
        {
            pMaster.ResetError();
        }

        internal void UpdateDisplay()
        {
            //throw new NotImplementedException();
            //    TrayDisp.Image = TrayMgr01.InputImage;
            //       TrayDisp.Fit(true);
            //setup for display
            ImageProcessor p = (ImageProcessor)pMaster.ProcessList[7];
            p.main = this;
            p.TrayDisp = TrayDisp;
            p.TrayDisppro = TrayDisppro;


        }

        internal void SetDisplay(CogRecordDisplay DispStack)
        {
            //throw new NotImplementedException();
            this.InputCVCamera.CarrierTrayDisplay = DispStack;
        }

        [XmlIgnore]
        public ManualResetEvent tmpWaitHandler;
        [XmlIgnore]
        public ImageProcessor imgprocessor;
        [XmlIgnore]
        public Cognex.VisionPro.ToolBlock.CogToolBlock algo { get; set; }
        [XmlIgnore]
        public Cognex.VisionPro.Display.CogDisplay TrayDisp { get; set; }

        [XmlIgnore]
        public Cognex.VisionPro.Display.CogDisplay TrayDisppro { get; set; }
        [XmlIgnore]
        public ManualResetEvent evtStart { get; set; }
        [XmlIgnore]
        public ManualResetEvent TmpDebugEventFire { get; set; }
        [XmlIgnore]
        internal LineScanAcqProcess linescanprocess { get; set; }
        [XmlIgnore]
        public DiscreteIO safetydoorlock { get; set; }
        public static DiscreteIO cstp { get; set; }
        public static DiscreteIO EMO { get; set; }
        public static DiscreteIO AmberLed { get; set; }
        public static DiscreteIO Buzzer { get; set; }
        [XmlIgnore]
        public string CurrentUserName { get; set; }

        private string sNameEngagePos = "Engage";
    }
}