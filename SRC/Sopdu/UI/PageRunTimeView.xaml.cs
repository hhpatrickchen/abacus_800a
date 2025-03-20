using Sopdu.helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

namespace Sopdu.UI
{
    /// <summary>
    /// Interaction logic for PageRunTimeView.xaml
    /// </summary>
    public partial class PageRunTimeView : Page
    {
        public PageRunTimeView()
        {
            InitializeComponent();
        }
        public PageRunTimeView(MainWindow main)
        {
            mainaccess = main;
            InitializeComponent();

            main.mainapp.TrayDisp = this.DispTray;
            main.mainapp.TrayDisppro = this.DispTraypro;
            main.mainapp.UpdateDisplay();

            main.mainapp.SetDisplay(DispStack);
            DataContext = main.mainapp;
            //main.mainapp.GemCtrl
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            
            asyncTimer = new System.Threading.Timer(asyncTimerUpdate, autoRstEvent, 100, 3000);
        }

        private void TabItem_Initialized(object sender, EventArgs e)
        {
            try
            {
                this.Systemlog = new LogViewPanel(mainaccess);
                this.MiniLogframe = new MiniLog(mainaccess);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }
        private MainWindow mainaccess;
        System.Threading.Timer asyncTimer;
        System.Threading.AutoResetEvent autoRstEvent;
        
        private void asyncTimerUpdate(Object source)
        {
            //string sTmp = GlobalVar.eIPMode.ToString();
            //tbIPCVMode.Text = sTmp;
            string sIPMode = string.Empty;
            for (int i = 0; i < GlobalVar.lstIPMode.Count; i++)
                sIPMode = sIPMode + GlobalVar.lstIPMode[i] + ",";

            mainaccess.mainapp.menu.sIPMode = sIPMode.TrimEnd(',');
            mainaccess.mainapp.menu.sOPMode = GlobalVar.eOPMode.ToString();
            mainaccess.mainapp.menu.isIPSFARdy = RunTimeData.mainApp.IPCvr.isSFATrayRdy;
            mainaccess.mainapp.menu.isIPOHTRdy = RunTimeData.mainApp.IPCvr.isOHTRdy;
            mainaccess.mainapp.menu.isOPSFARdy = RunTimeData.mainApp.OPCvr.isSFATrayRdy;
            mainaccess.mainapp.menu.isOPOHTRdy = RunTimeData.mainApp.OPCvr.isOHTRdy;
        }

        public Page MiniLogframe
        {
            get { return (Page)this.MiniLogFrame.Content; }
            set
            {
                this.MiniLogFrame.Navigate(value);
                this.MiniLogFrame.NavigationService.RemoveBackEntry();
                MiniLogFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
                TranslateTransform scale = new TranslateTransform(0, 5);
                this.MiniLogFrame.SetValue(RenderTransformProperty, scale);
                scale.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
            }

        }
        public Page Systemlog
        {
            get { return (Page)this.SystemLogFrame.Content; }
            set
            {
                this.SystemLogFrame.Navigate(value);
                this.SystemLogFrame.NavigationService.RemoveBackEntry();
                SystemLogFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
                TranslateTransform scale = new TranslateTransform(0, 5);
                this.SystemLogFrame.SetValue(RenderTransformProperty, scale);
                scale.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
            }
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            mainaccess.mainapp.evtStart.Set();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            mainaccess.mainapp.TmpDebugEventFire.Set();
        }

        private void ClearMsgClick(object sender, RoutedEventArgs e)
        {
            mainaccess.mainapp.GemCtrl.ClearDisplayMsg();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)//retry when warning
        {
            mainaccess.mainapp.pMaster.bretry = true;
            mainaccess.mainapp.pMaster.WarningDisplayVisible = "Hidden";
            mainaccess.mainapp.pMaster.evtRstWarning.Set();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            mainaccess.mainapp.pMaster.bretry = false;
            mainaccess.mainapp.pMaster.WarningDisplayVisible = "Hidden";
            mainaccess.mainapp.pMaster.evtRstWarning.Set();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {

        }

    }

    public class VCMyEnumToString : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value.ToString()))  // This is for databinding
                return MachineState.NotInit;
            return (StringToEnum<MachineState>(value.ToString())).GetDescription(); // <-- The extention method
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value.ToString())) // This is for databinding
                return MachineState.NotInit;
            return StringToEnum<MachineState>(value.ToString());
        }

        public static T StringToEnum<T>(string name)
        {
            return (T)Enum.Parse(typeof(T), name);
        }

        #endregion IValueConverter Members
    }

    public static class EnumGetDescription
    {
        public static string GetDescription(this Enum enumObj)
        {
            FieldInfo fieldInfo = enumObj.GetType().GetField(enumObj.ToString());

            object[] attribArray = fieldInfo.GetCustomAttributes(false);

            if (attribArray.Length == 0)
            {
                return enumObj.ToString();
            }
            else
            {
                DescriptionAttribute attrib = attribArray[0] as DescriptionAttribute;
                return attrib.Description;
            }
        }
    }
    
}