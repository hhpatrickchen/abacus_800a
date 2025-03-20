using Sopdu.Devices.Base;
using Sopdu.Devices.IOModule;
using Sopdu.helper;
using Sopdu.ProcessApps.main;
using Sopdu.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using Dct.Models;

namespace Sopdu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            GlobalVar.LoadSettings();
            DbManager.Instance.ConnectDB();
            mainapp = new MainApp();
            mainapp.Init();
            log.Debug("testing");
            InitializeComponent();

            RunTimeData.mainApp = mainapp;
            DataContext = mainapp;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //LoadSettings();
        }
        private void BtnMidFrame_Loaded(object sender, RoutedEventArgs e)
        {

        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //messagebox for password
            //SaveSettings();
            mainapp.Shutdown();
        }

        public Page DispTop
        {
            get { return (Page)this.BtnTopFrame.Content; }
            set
            {
                this.BtnTopFrame.Navigate(value);
                this.BtnTopFrame.NavigationService.RemoveBackEntry();
                BtnTopFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
                TranslateTransform scale = new TranslateTransform(0, 5);
                this.BtnTopFrame.SetValue(RenderTransformProperty, scale);
                scale.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
            }
        }

        public Page DispMid
        {
            get { return (Page)this.BtnMidFrame.Content; }
            set
            {
                this.BtnMidFrame.Navigate(value);
                this.BtnMidFrame.NavigationService.RemoveBackEntry();
                BtnMidFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
                TranslateTransform scale = new TranslateTransform(0, 5);
                this.BtnMidFrame.SetValue(RenderTransformProperty, scale);
                scale.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
            }
        }

        public void Subscribe(MainBtnPanel btnpanel)
        {
            btnpanel.btnLogout += btnpanel_btnLogout;
            btnpanel.btnSystemSetupView += btnpanel_btnSystemSetupView;
            btnpanel.btnRunTimeView += btnpanel_btnRunTimeView;
        }

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public MainApp mainapp { get { return _mainapp; } set { _mainapp = value; NotifyPropertyChanged("mainapp"); } }
        private MainApp _mainapp;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            mainapp.mcEvents.evtStart.Set();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            mainapp.ResetErr();
        }

        private void AxisPanel_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void BtnDockPaneltop_Initialized(object sender, EventArgs e)
        {
            try
            {
                this.DispTop = new MainBtnPanel(this);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        private void BtnDockPanelMid_Initialized(object sender, EventArgs e)
        {
            try
            {
                this.DispMid = new PageRunTimeView(this);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }
        PageRunTimeView pageRunTimeView;
        private void btnpanel_btnRunTimeView()
        {
            if (this.DispMid.GetType() == typeof(PageRunTimeView))
                return;
            try
            {
                mainapp.safetydoorlock.SetOutput(true);
                mainapp.razor.maintainance = false;
                if (pageRunTimeView==null)
                {
                    pageRunTimeView= new PageRunTimeView(this);
                }
                this.DispMid = pageRunTimeView;
                mainapp.pMaster.EquipmentState.SetState(MachineState.NotInit);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }
        PageModuleMaintanance pageModuleMaintanance;
        private void btnpanel_btnSystemSetupView()
        {
            //message box for password
            frmPassword frm = new frmPassword();
            //if (frm.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
            //    return;
            try
            {
                MachineState machineState = mainapp.pMaster.EquipmentState.GetState();
                if ((machineState == MachineState.RunStop) || (machineState == MachineState.NotInit) ||
                     (machineState == MachineState.Init) || (machineState == MachineState.Aborted))
                {
                    if (mainapp.pMaster.RequestMaintenanceMode())
                    {
                        mainapp.safetydoorlock.SetOutput(false);
                        mainapp.razor.maintainance = true;
                        if (pageModuleMaintanance==null)
                        {
                            pageModuleMaintanance=new PageModuleMaintanance(this);
                        }
                        this.DispMid = pageModuleMaintanance;
                    }
                    else
                        MessageBox.Show("Error Entering Maintainance Mode.");
                }
                else
                {
                   MessageBox.Show("Unable to comply, machine not in stop mode.");
                }
                //
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        private void btnpanel_btnLogout()
        {
            //shutdown application
            //message box for password
            frmPassword frm = new frmPassword();
            //if (frm.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
            //    return;

            try
            {
                MachineState machineState = mainapp.pMaster.EquipmentState.GetState();
                if ((machineState == MachineState.RunStop) || (machineState == MachineState.NotInit) ||
                    (machineState == MachineState.Init) || (machineState == MachineState.Aborted))
                {
                    Environment.Exit(0);

                    Close();
                }
                else
                {
                    MessageBox.Show("Unable to comply, machine not in stop mode.");
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            
        }
        
        #region // Method and function
        private void SaveSettings()
        {
            MyLib.IniFile iniFile = new MyLib.IniFile();
            string sFileName = "SetXferMode.ini";

            string sSection = GlobalVar.sSection;
            string sKey = GlobalVar.SKey.sIPMode;
            string sVal = GlobalVar.eIPMode.ToString();
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);

            sKey = GlobalVar.SKey.sOPMode;
            sVal = GlobalVar.eOPMode.ToString();
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);
        }
        //private void LoadSettings()
        //{
        //    MyLib.IniFile iniFile = new MyLib.IniFile();
        //    string sFileName = "SetXferMode.ini";
        //    string sSection = GlobalVar.sSection;

        //    string sKey = GlobalVar.SKey.sIPMode;
        //    string sVal = iniFile.ReadValue(sFileName, sSection, sKey);
        //    if (!string.IsNullOrEmpty(sVal))
        //        GlobalVar.eIPMode = (EIPMode)Enum.Parse(typeof(EIPMode), sVal);

        //    sKey = GlobalVar.SKey.sOPMode;
        //    sVal = iniFile.ReadValue(sFileName, sSection, sKey);
        //    if (!string.IsNullOrEmpty(sVal))
        //        GlobalVar.eOPMode = (EOPMode)Enum.Parse(typeof(EOPMode), sVal);

        //    sKey = GlobalVar.SKey.sHasPassword;
        //    sVal = iniFile.ReadValue(sFileName, sSection, sKey);
        //    if (!string.IsNullOrEmpty(sVal))
        //        GlobalVar.bHasPassword = sVal=="True";

        //    sKey = GlobalVar.SKey.dFailRate;
        //    sVal = iniFile.ReadValue(sFileName, sSection, sKey);
        //    if (!string.IsNullOrEmpty(sVal))
        //        GlobalVar.iFailRate = Convert.ToInt32(sVal);

        //}
        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
           
        }
    }

    public class GlobalVar
    {
        public static bool isCheckTrayGapV2 = false;
        public static bool isTrayLost=false;
        public static bool isMachineAuto = false;
        public static bool isManualMode = false;
        public static bool isIPCVAuto { get { return !isManualMode; } }
        public static bool isOPCVAuto { get { return !isManualMode; } }
        public static bool isBypassFail = false;    //Bypass fail tray manual take away
        public static bool isDoorAtCV = true;
        public static bool isReverse = false;
        public static bool isEngagePos = true;      // Input Stacker engage position saved with recipe
        public static int iCntLimt = 500;           //x100ms
        public static int iTimeOut = 50000;         // ms
        public static string sSection = "Setting";
        public static List<DiscreteIO> lstAllDI = new List<DiscreteIO>();
        public static List<DiscreteIO> lstAllDO = new List<DiscreteIO>();
        public static ConcurrentBag<string> carrierids = new ConcurrentBag<string>();
        public static List<string> lstIPMode = new List<string>();
        public static EIPMode eIPMode = EIPMode.Manual;
        public static EOPMode eOPMode = EOPMode.Manual;
        public static bool bHasPassword = false;
        public static int iFailRate = 0;
        public static double ShutterSaveDistance = 43000;
        public static bool EnablePlasmaFan = false;

        public static int iKeysenceThreshold = 1000;

        public static string[] aIPModeID { get { return GetIPModeNameArray(); } }

        public static string CurrentUserName { get; set; }

        public static string[] GetIPModeNameArray()
        {
            string[] aRtn = null;
            aRtn = Enum.GetNames(typeof(EIPMode));
            return aRtn;
        }
        public static void LoadSettings()
        {
            MyLib.IniFile iniFile = new MyLib.IniFile();
            string sFileName = "SetXferMode.ini";
            string sSection = GlobalVar.sSection;

            string sKey = GlobalVar.SKey.sIPMode;
            string sVal = iniFile.ReadValue(sFileName, sSection, sKey);
            if (!string.IsNullOrEmpty(sVal))
            {
                string[] aTmp = sVal.Split(',');
                GlobalVar.lstIPMode.Clear();
                for (int i = 0; i < aTmp.Length; i++)
                {
                    if (!string.IsNullOrEmpty(aTmp[i]))
                        GlobalVar.lstIPMode.Add(aTmp[i]);
                }
                //GlobalVar.lstIPMode.AddRange(aTmp);
            }

            sKey = GlobalVar.SKey.sOPMode;
            sVal = iniFile.ReadValue(sFileName, sSection, sKey);
            if (!string.IsNullOrEmpty(sVal))
                GlobalVar.eOPMode = (EOPMode)Enum.Parse(typeof(EOPMode), sVal);

            sKey = GlobalVar.SKey.sReverse;
            sVal = iniFile.ReadValue(sFileName, sSection, sKey);
            if (!string.IsNullOrEmpty(sVal))
                GlobalVar.isReverse = bool.Parse(sVal);

            sKey = GlobalVar.SKey.sBypass;
            sVal = iniFile.ReadValue(sFileName, sSection, sKey);
            if (!string.IsNullOrEmpty(sVal))
                GlobalVar.isBypassFail = bool.Parse(sVal);
           
            sKey = GlobalVar.SKey.sHasPassword;
            sVal = iniFile.ReadValue(sFileName, sSection, sKey);
            if (!string.IsNullOrEmpty(sVal))
                GlobalVar.bHasPassword = sVal == "True";

            sKey = GlobalVar.SKey.enableLongSideTrayGap;
            sVal = iniFile.ReadValue(sFileName, sSection, sKey);
            if (!string.IsNullOrEmpty(sVal))
                GlobalVar.isCheckTrayGapV2 = sVal == "True";


            sKey = GlobalVar.SKey.dFailRate;
            sVal = iniFile.ReadValue(sFileName, sSection, sKey);
            if (!string.IsNullOrEmpty(sVal))
                GlobalVar.iFailRate=int.Parse(sVal);


            sKey = GlobalVar.SKey.ShutterSaveDistance;
            sVal = iniFile.ReadValue(sFileName, sSection, sKey);
            if (!string.IsNullOrEmpty(sVal))
            {
                GlobalVar.ShutterSaveDistance = double.Parse(sVal);
                if(GlobalVar.ShutterSaveDistance < 40000)
                {
                    GlobalVar.ShutterSaveDistance = 42000;
                }
            }


            //add keyence sensor threshold
            sKey = GlobalVar.SKey.sKeysenceThreshold;
            sVal = iniFile.ReadValue(sFileName, sSection, sKey);
            if (!string.IsNullOrEmpty(sVal))
                GlobalVar.iKeysenceThreshold = int.Parse(sVal);
        }
       
        public static void SaveSettings()
        {
            MyLib.IniFile iniFile = new MyLib.IniFile();
            string sFileName = "SetXferMode.ini";

            string sSection = GlobalVar.sSection;
            string sKey = GlobalVar.SKey.sIPMode;
            string sVal = string.Empty;
            for (int i = 0; i < GlobalVar.lstIPMode.Count; i++)
                sVal = sVal + GlobalVar.lstIPMode[i] + ",";
            sVal = sVal.TrimEnd(',');
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);

            sKey = GlobalVar.SKey.sOPMode;
            sVal = GlobalVar.eOPMode.ToString();
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);

            sKey = GlobalVar.SKey.sReverse;
            sVal = GlobalVar.isReverse.ToString();
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);

            sKey = GlobalVar.SKey.sBypass;
            sVal = GlobalVar.isBypassFail.ToString();
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);


            //add keyence sensor threshold
            sKey = GlobalVar.SKey.sKeysenceThreshold;
            sVal = GlobalVar.iKeysenceThreshold.ToString();
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);
        }

        public static class SKey
        {
            public static string sIPMode = "IPMode";
            public static string sOPMode = "OPMode";
            public static string dFailRate = "FailRate";
            public static string ShutterSaveDistance = "ShutterSaveDistance";
            public static string sBypass = "BypassFail";
            public static string sReverse = "Reversable";
            public static string sHasPassword = "HasPassword";
            public static string enableLongSideTrayGap = "EnableLongSideTrayGap";
            public static string sKeysenceThreshold = "KeysenceThreshold";
        }
    }

    public class RunTimeData
    {
        public static bool isCycleStopping = false;
        public static bool isPartCleared { get { return CheckPartClear(); } }
        public static bool isLinkIO { get { return CheckLinkIO(); } }
        public static bool isLinkOP { get { return CheckLinkOP(); } }
        public static string sIPAddrIO { get { return GetIPAddrIO(); } }
        public static string sIPAddrOP { get { return GetIPAddrOP(); } }

        private static Stopwatch sw = new Stopwatch();
        public static Dictionary<string, bool> kvpOPStkr = new Dictionary<string, bool>();
        public static Dictionary<string, bool> kvpOPCvr = new Dictionary<string, bool>();

        public static MainApp mainApp
        {
            get { return _mainApp; }
            set { _mainApp = value; }
        }
        private static MainApp _mainApp;

        private static bool CheckLinkIO()
        {
            if (mainApp != null)
            {
                return mainApp.ioCtrl.isConnected;
            }
            else
                return false;
        }
        private static bool CheckLinkOP()
        {
            if (mainApp != null)
            {
                return mainApp.opCtrl.isConnected;
            }
            else
                return false;
        }
        private static bool CheckPartClear()
        {
            bool isClear = false;
            if (isCycleStopping)
            {
                if (mainApp.IPCvr.isPartAbsent && mainApp.IPStkr.isPartAbsent && mainApp.Shut1.isPartAbsent &&
                mainApp.Shut2.isPartAbsent && mainApp.OPStkr.isPartAbsent && mainApp.OPCvr.isPartPresent)
                {
                    if (sw.IsRunning)
                    {
                        if (sw.ElapsedMilliseconds > 10000)
                            isClear = true;
                    }
                    else
                    {
                        sw.Reset();
                        sw.Start();
                    }
                }          
                else
                {
                    sw.Stop();
                    sw.Reset();
                }
            }
            
            return isClear;
        }

        private static string GetIPAddrIO()
        {
            string sRtn = "";
            if (mainApp != null)
                sRtn = mainApp.ioCtrl.IPAddress;

            return sRtn;
        }
        private static string GetIPAddrOP()
        {
            string sRtn = "";
            if (mainApp != null)
                sRtn = mainApp.opCtrl.IPAddress;

            return sRtn;
        }
    }

    public enum EIPMode
    { 
        OHT, 
        Conveyor,
        Manual
    }
    public enum EOPMode
    { 
        OHT, 
        Conveyor, 
        Manual
    }
}