using Basler.Pylon;
using Cognex.VisionPro;
using Cognex.VisionPro.Caliper;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.ID;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.ToolBlock;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;

namespace Sopdu.Devices.Vision
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class UsbCamera : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [XmlIgnore]
        public CogToolBlock tbCarrierTrayMap { get; set; }

        [XmlIgnore]
        public CogToolBlock tbSingleID { get; set; }

        [XmlIgnore]
        public CogToolBlock tbFirstLocation { get; set; }

        private string serialNumberField;
        private ObservableCollection<UsbCameraSetup> setupsField;
        private Camera camera;
        //	  <T556>73</T556>
	  //<T635>76</T635>
	  //<T1016>120</T1016>
        private double _T556;
        public double T556 { get { return _T556; } set { _T556 = value; NotifyPropertyChanged("T556"); } }
        private double _T635;
        public double T635 { get { return _T635; } set { _T635 = value; NotifyPropertyChanged("T635"); } }
        private double _T1016;
        public double T1016 { get { return _T1016; } set { _T1016 = value; NotifyPropertyChanged("T1016"); } }


        private double  _T556_Set;
        public double T556_Set { get { return _T556_Set; } set { _T556_Set = value; NotifyPropertyChanged("T556_Set"); } }
        private double _T635_Set;
        public double T635_Set { get { return _T635_Set; } set { _T635_Set = value; NotifyPropertyChanged("T635_Set"); } }
        private double _T1016_Set;
        public double T1016_Set { get { return _T1016_Set; } set { _T1016_Set = value; NotifyPropertyChanged("T1016_Set"); } }

        private double _LastTrayPos_T556;
        public double LastTrayPos_T556 { get { return _LastTrayPos_T556; } set { _LastTrayPos_T556 = value; NotifyPropertyChanged("LastTrayPos_T556"); } }
        private double _LastTrayPos_T635;
        public double LastTrayPos_T635 { get { return _LastTrayPos_T635; } set { _LastTrayPos_T635 = value; NotifyPropertyChanged("LastTrayPos_T635"); } }
        private double _LastTrayPos_T1016;
        public double LastTrayPos_T1016 { get { return _LastTrayPos_T1016; } set { _LastTrayPos_T1016 = value; NotifyPropertyChanged("LastTrayPos_T1016"); } }


        //private int _IndexValue;
        [XmlIgnore]
        public int _IndexValue{get;set;}        

        private int _displayIndexValue;
        public int displayIndexValue { get { return _displayIndexValue; } set { _displayIndexValue = value; NotifyPropertyChanged("displayIndexValue"); } }

        private double _CarrierPitchLimit;
        private string _traysn;
        private double _traypos;
        private string _avePitch;
        private double _PitchConstant;
        public double PitchConstant { get { return _PitchConstant; } set { _PitchConstant = value; NotifyPropertyChanged("PitchConstant"); } }

        private double _PitchOffSet;
        [XmlIgnore]
        public double PitchOffSet { get { return _PitchOffSet; } set { _PitchOffSet = value; NotifyPropertyChanged("PitchOffSet"); } }
        [XmlIgnore]
        public string avePitch { get { return _avePitch; } set { _avePitch = value; NotifyPropertyChanged("avePitch"); } }
        [XmlIgnore]
        public string __avePitch { get; set; }
        private string _minPitch;

        [XmlIgnore]
        public string minPitch { get { return _minPitch; } set { _minPitch = value; NotifyPropertyChanged("minPitch"); } }
        [XmlIgnore]
        public string __minPitch;
        private string _maxPitch;

        [XmlIgnore]
        public string maxPitch { get { return _maxPitch; } set { _maxPitch = value; NotifyPropertyChanged("maxPitch"); } }
        [XmlIgnore]
        public string __maxPitch { get; set; }

        private ObservableCollection<string> _pitch;

        [XmlIgnore]
        public ObservableCollection<string> pitch { get { return _pitch; } set { _pitch = value; NotifyPropertyChanged("pitch"); } }
        [XmlIgnore]
        public ObservableCollection<string> __pitch { get; set; }

        private ObservableCollection<string> _position;

        [XmlIgnore]
        public ObservableCollection<string> position { get { return _position; } set { _position = value; NotifyPropertyChanged("position"); } }
        [XmlIgnore]        
        public ObservableCollection<string> __position { get; set; }


        private ObservableCollection<string> _trayidlist;
        public string sCoverTrayPrefix;

        [XmlIgnore]
        public ObservableCollection<string> trayidlist { get { return _trayidlist; } set { _trayidlist = value; NotifyPropertyChanged("trayidlist"); } }
        [XmlIgnore]
        public ObservableCollection<string> __trayidlist { get; set; }
        [XmlIgnore]
        public double traypos { get { return _traypos; } set { _traypos = value; NotifyPropertyChanged("traypos"); } }

        [XmlIgnore]
        public string traysn { get { return _traysn; } set { _traysn = value; NotifyPropertyChanged("traysn"); } }

        public double CarrierPitchLimit
        {
            get { return _CarrierPitchLimit; }
            set { _CarrierPitchLimit = value; NotifyPropertyChanged("CarrierPitchLimit"); }
        }

        /// <remarks/>
        public string SerialNumber
        {
            get
            {
                return this.serialNumberField;
            }
            set
            {
                this.serialNumberField = value; NotifyPropertyChanged("SerialNumber");
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Setup", IsNullable = false)]
        public ObservableCollection<UsbCameraSetup> Setups
        {
            get
            {
                return this.setupsField;
            }
            set
            {
                this.setupsField = value; NotifyPropertyChanged("Setups");
            }
        }
        [XmlIgnore]
        public int itpk { get; set; }
        public void Init()
        {
            try
            {
                MessageListener.Instance.ReceiveMessage("Connecting to Camera SN " + SerialNumber);
                camera = new Camera(this.SerialNumber);
                camera.Open();
                MessageListener.Instance.ReceiveMessage("Connected to Camera SN " + SerialNumber);
                camera.CameraOpened += Configuration.AcquireSingleFrame;
                camera.Parameters[PLCameraInstance.MaxNumBuffer].SetValue(1);
                foreach (UsbCameraSetup su in Setups)
                {
                    su.SetupSelectedEvent += su_SetupSelectedEvent;
                }
                MessageListener.Instance.ReceiveMessage("Loading Inputstacker vision files");
                tbCarrierTrayMap = (CogToolBlock)CogSerializer.LoadObjectFromFile(@".\InputStacker\carriermap.vpp");
                tbSingleID = (CogToolBlock)CogSerializer.LoadObjectFromFile(@".\InputStacker\SingleID.vpp");
                tbFirstLocation = (CogToolBlock)CogSerializer.LoadObjectFromFile(@".\InputStacker\TrayPos.vpp");
                MessageListener.Instance.ReceiveMessage("Loading Inputstacker Vision File Completed");
            }
            catch { }

        }

        public void Shutdown()
        {
            camera.Close();
        }
        CogImage8Grey current2dImg;
        DateTime current2dTime;
        public void su_SetupSelectedEvent(UsbCameraSetup setup)
        {
            itpk = -1;
            camera.Parameters[PLCamera.ExposureTime].SetValue(setup.Exposure);
            camera.Parameters[PLCamera.Gain].SetValue(setup.Gain);
            camera.Parameters[PLCamera.BinningHorizontal].SetValue(setup.Hbin);
            camera.Parameters[PLCamera.BinningVertical].SetValue(setup.Vbin);
            CogImage8Grey img = GrabImage();
           // CogImageFileTool ftool = new CogImageFileTool();

            switch (setup.Name)
            {
                case "ReadCarrierMap":
                    tbCarrierTrayMap.Inputs[0].Value = img;
                    tbCarrierTrayMap.Run();
                    CarrierMapProcessing();
                    //SaveImage(img);
                    current2dImg = img;
                    current2dTime = DateTime.Now;


                    break;

                case "FindTrayLocation":
                    tbFirstLocation.Inputs[0].Value = img;
                    tbFirstLocation.Run();
                    double pitchoffSet = 0;
                    double __traypos = 0;
                    CogCaliperTool cal = (CogCaliperTool)tbFirstLocation.Tools["CogCaliperTool1"];
                    if (cal.Results != null)
                    {
                        if (cal.Results.Count > 0)
                        {
                            __traypos = Math.Round(cal.Results[0].PositionX, 2);
                            //PitchOffSet = Math.Round(traypos * PitchConstant,2);
                            pitchoffSet = cal.Results[0].Width;
                            if ((pitchoffSet >= T635) && (pitchoffSet < T1016))
                            {
                                _IndexValue = 635;//it is a constant
                            }
                            if (pitchoffSet >= T1016)
                            {
                                _IndexValue = 1005;// it is a constant
                            }
                            //if (pitchoffSet > T1016) _IndexValue = 1016;
                            if (pitchoffSet <= T556)
                            {
                                _IndexValue = 565;// it is a constant
                            }
                            log.Debug("_IndexValue" + _IndexValue);
                            log.Debug("PitchOffset" + pitchoffSet);
                            //if (pitchoffSet < T556) _IndexValue = 556;
                            //display data
                            Application.Current.Dispatcher.BeginInvoke((Action)delegate
                                   {
                                       traypos = __traypos;
                                       PitchOffSet = pitchoffSet;
                                       displayIndexValue = _IndexValue;
                                       //display images
                                       ICogRecord topRecord = tbFirstLocation.CreateLastRunRecord();
                                       ICogRecord displayrecord = topRecord.SubRecords["CogCaliperTool1.InputImage"];
                                       CarrierTrayDisplay.Record = displayrecord;
                                       CarrierTrayDisplay.Fit(true);
                                       //end of image display
                                   });
                        }
                    }
                    else
                        throw new Exception("Tray Find Error");
                    break;

                case "ReadSingleCode":
                    tbSingleID.Inputs[0].Value = img;
                    tbSingleID.Run();
                    CogIDTool id = (CogIDTool)tbSingleID.Tools["CogIDTool1"];
                    if (id.Results != null)
                    {
                        if (id.Results.Count > 0)
                        {
                            traysn = id.Results[0].DecodedData.DecodedString;
                        }
                    }
                    break;
            }
        }

        public void SaveImage()
        {
            if (current2dImg== null)
            {
                return;
            }
            try
            {
                var image = current2dImg;
                int W = image.Width;
                int H = image.Height;
                Cognex.VisionPro.ICogImage8PixelMemory tM = image.Get8GreyPixelMemory(Cognex.VisionPro.CogImageDataModeConstants.Read, 0, 0, W, H);
                Bitmap grayscale = new Bitmap(tM.Width, tM.Height, tM.Stride, PixelFormat.Format8bppIndexed, tM.Scan0);
                Bitmap bitmap = new Bitmap(1, 1, PixelFormat.Format8bppIndexed);
                ColorPalette palette = bitmap.Palette;
                string hdisk = AppDomain.CurrentDomain.BaseDirectory.Substring(0, 1);
                string savepath1 = $"{hdisk}:\\Image2D";
                string imgPath = savepath1 + @"\" + "-" + current2dTime.ToString("MMddThhmmss") + "-a.bmp";
                if (!Directory.Exists(savepath1))
                {
                    Directory.CreateDirectory(savepath1);
                }
                for (int i = 0; i <= bitmap.Palette.Entries.Length - 1; i++)
                {
                    palette.Entries[i] = Color.FromArgb(255, i, i, i);
                }
                bitmap.Dispose();
                grayscale.Palette = palette;
                if (!File.Exists(imgPath))
                {
                    grayscale.Save(imgPath, ImageFormat.Bmp);
                }
                tM.Dispose();
            }
            catch
            {
            }

        }


        Image img2d;

        public bool CarrierMapProcessing()
        {
            try
            {
                CogPMAlignTool pmtool = (CogPMAlignTool)tbCarrierTrayMap.Tools["CogPMAlignTool1"];
                CogPMAlignTool pmtool1 = (CogPMAlignTool)tbCarrierTrayMap.Tools["CogPMAlignTool2"];
                CogIDTool idtool = (CogIDTool)tbCarrierTrayMap.Tools["CogIDTool1"];
                List<double> pos = new List<double>();

                if (pmtool1.Results.Count > pmtool.Results.Count)
                {
                    foreach (CogPMAlignResult rst in pmtool1.Results)
                    {
                        pos.Add(rst.GetPose().TranslationX);
                    }
                }
                else
                {
                    foreach (CogPMAlignResult rst in pmtool.Results)
                    {
                        pos.Add(rst.GetPose().TranslationX);
                    }
                }
                //sort the position is ascending order
                var sorted = from dbl in pos
                             orderby dbl ascending
                             select dbl;
                double lastpos = -1;
                double _pitch = -1;
                List<double> _pitchlist = new List<double>();
                __position = new ObservableCollection<string>();
                __trayidlist = new ObservableCollection<string>();
                __pitch = new ObservableCollection<string>();
                foreach (double p in sorted)
                {
                    if (lastpos < 0)
                        lastpos = p;
                    else
                    {
                        _pitch = p - lastpos;
                        lastpos = p;
                        _pitchlist.Add(_pitch);
                        __pitch.Add(_pitch.ToString("#.##"));
                        log.Debug(" pitch " + _pitch.ToString("#.##"));
                        
                    }
                    __position.Add(p.ToString("#.##"));
                }
                __maxPitch = _pitchlist.Max().ToString("#.##");
                __minPitch = _pitchlist.Min().ToString("#.##");
                __avePitch = _pitchlist.Average().ToString("#.##");

                var idsorted = from id in idtool.Results orderby id.CenterX ascending select id;

                trayidlist = new ObservableCollection<string>();
                itpk = 0;
                log.Debug("scover tray prefix :" + sCoverTrayPrefix);
                foreach (CogIDResult idrst in idsorted)
                {
                    __trayidlist.Add(idrst.DecodedData.DecodedString);
                    log.Debug("other " + idrst.ID);
                    log.Debug("decoded :" + idrst.DecodedData.DecodedString);
                    if (idrst.DecodedData.DecodedString.Contains(sCoverTrayPrefix)) itpk++;
                }

                //user interface update
                Application.Current.Dispatcher.BeginInvoke((Action)delegate
                                        {
                                            position = new ObservableCollection<string>();
                                            pitch = new ObservableCollection<string>();
                                            trayidlist = new ObservableCollection<string>();
                                            foreach (string str in __position)
                                            {
                                                position.Add(str);
                                            }
                                            foreach (string str1 in __trayidlist)
                                            {
                                                try
                                                {
                                                    log.Debug("trayidlist :" + str1);
                                                    trayidlist.Add(str1);
                                                }
                                                catch (Exception ex)
                                                {
                                                    log.Debug(ex.ToString());
                                                }
                                            }
                                            foreach (string str2 in __pitch)
                                            {
                                                pitch.Add(str2);
                                            }
                                            maxPitch = __maxPitch;
                                            minPitch = __minPitch;
                                            avePitch = __avePitch;
                                            //display images
                                            ICogRecord topRecord = this.tbCarrierTrayMap.CreateLastRunRecord();
                                            ICogRecord displayrecord = topRecord.SubRecords["CogIPOneImageTool1.OutputImage"];//CogIPOneImageTool1
                                            CarrierTrayDisplay.Record = displayrecord;
                                            CarrierTrayDisplay.Fit(true);

                                            //img2d = CarrierTrayDisplay.CreateContentBitmap(CogDisplayContentBitmapConstants.Image);

                                            //string hdisk = AppDomain.CurrentDomain.BaseDirectory.Substring(0, 1);
                                            //string savepath1 = $"{hdisk}:\\Image2D";
                                            //string imgPath = savepath1 + @"\" + "-" + DateTime.Now.ToString("MMddThhmmss") + "-a.bmp";
                                            //if (!Directory.Exists(savepath1))
                                            //{
                                            //    Directory.CreateDirectory(savepath1);
                                            //}
                                            //if (!File.Exists(imgPath))
                                            //{
                                            //    img2d.Save(imgPath);
                                            //}

                                            //end of image display
                                        });
            }
            catch (Exception ex) {
                log.Error(ex.ToString());
                return false; }
            return true;
        }

        public void SetupMode(int i)
        {
            camera.Parameters[PLCamera.ExposureTime].SetValue(Setups[i].Exposure);
            camera.Parameters[PLCamera.Gain].SetValue(Setups[i].Gain);
            camera.Parameters[PLCamera.BinningHorizontal].SetValue(Setups[i].Hbin);
            camera.Parameters[PLCamera.BinningVertical].SetValue(Setups[i].Vbin);
        }

        public CogImage8Grey GrabImage()
        {
            try
            {
                IGrabResult grabResult = camera.StreamGrabber.GrabOne(5000);
                if (grabResult.GrabSucceeded)
                {
                    Bitmap bmp = new Bitmap(grabResult.Width, grabResult.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                    BitmapData bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
                    PixelDataConverter converter = new PixelDataConverter();
                    converter.OutputPixelFormat = PixelType.Mono8;
                    IntPtr ptrBmp = bmpData.Scan0;
                    converter.Convert(ptrBmp, bmpData.Stride * bmp.Height, grabResult); //Exception handling TODO
                    bmp.UnlockBits(bmpData);
                    return new CogImage8Grey(bmp);
                }
            }
            catch (Exception ex)
            { }
            finally
            {
            }
            return null;
        }

        static public UsbCamera loadFile(string filename)
        {
            XmlSerializer cameraserializer;
            cameraserializer = new XmlSerializer(typeof(UsbCamera));
            TextReader reader12 = new StreamReader(filename);
            UsbCamera usb = (UsbCamera)cameraserializer.Deserialize(reader12);
            reader12.Close();
            return usb;
        }

        static public void SaveFile(string filename, UsbCamera camera)
        {
            XmlSerializer cameraserializer;
            cameraserializer = new XmlSerializer(typeof(UsbCamera));
            TextWriter wrt = new StreamWriter(filename);
            cameraserializer.Serialize(wrt, camera);
            wrt.Close();
            CogSerializer.SaveObjectToFile(camera.tbCarrierTrayMap, @".\InputStacker\carriermap.vpp");
            CogSerializer.SaveObjectToFile(camera.tbSingleID, @".\InputStacker\SingleID.vpp");
            CogSerializer.SaveObjectToFile(camera.tbFirstLocation, @".\InputStacker\TrayPos.vpp");
            MessageBox.Show("Save Complete");
        }
        [XmlIgnore]
        public CogRecordDisplay CarrierTrayDisplay { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class UsbCameraSetup : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public delegate void setupselectedEvt(UsbCameraSetup setup);

        public event setupselectedEvt SetupSelectedEvent;

        private string nameField;

        private ushort exposureField;

        private byte hbinField;

        private byte vbinField;

        private byte gainField;

        /// <remarks/>
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value; NotifyPropertyChanged("Name");
            }
        }

        /// <remarks/>
        public ushort Exposure
        {
            get
            {
                return this.exposureField;
            }
            set
            {
                this.exposureField = value; NotifyPropertyChanged("Exposure");
            }
        }

        /// <remarks/>
        public byte Hbin
        {
            get
            {
                return this.hbinField;
            }
            set
            {
                this.hbinField = value; NotifyPropertyChanged("Hbin");
            }
        }

        /// <remarks/>
        public byte Vbin
        {
            get
            {
                return this.vbinField;
            }
            set
            {
                this.vbinField = value; NotifyPropertyChanged("Vbin");
            }
        }

        /// <remarks/>
        public byte Gain
        {
            get
            {
                return this.gainField;
            }
            set
            {
                this.gainField = value; NotifyPropertyChanged("Gain");
            }
        }

        private ICommand _CmdFire;

        [XmlIgnore]
        public ICommand CmdFire
        {
            get { return _CmdFire ?? (_CmdFire = new Command(p => _cmdFire((object)p))); }
        }

        public void _cmdFire(object item)
        {
            if (SetupSelectedEvent != null)
                SetupSelectedEvent(this);
        }
    }

    public class Command : ICommand
    {
        private Action<object> _action;

        public Command(Action<object> action)
        {
            _action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (parameter != null)
            {
                _action(parameter);
            }
            else
            {
                _action(null);
            }
        }
    }
}