using Dct.UI.Alarm.Views;
using Sopdu.ProcessApps.main;
using Sopdu.ProcessApps.ProcessModules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using UserManagement;


namespace Sopdu.UI
{
    /// <summary>
    /// Interaction logic for MainBtnPanel.xaml
    /// </summary>
    public partial class MainBtnPanel : Page
    {

        public event btnfunctRunTimeView btnRunTimeView;

        public delegate void btnfunctRunTimeView();

        public event btnfunctSystemSetupView btnSystemSetupView;

        public delegate void btnfunctSystemSetupView();

        public event btnfunctLogout btnLogout;

        public delegate void btnfunctLogout();

        public static event Action<bool> userChangedEvent;
        private DispatcherTimer timer;
        public Stopwatch sw = new Stopwatch();


        public MainBtnPanel()
        {
            InitializeComponent();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!GlobalVar.isManualMode)
            {
                if (sw.ElapsedMilliseconds > 60000)
                {
                    this.UserManagementControl1._userControlTagViewModel.IsAdminLogin = false;
                    this.UserManagementControl1._userControlTagViewModel.IsOperatorLogin = false;
                    this.UserManagementControl1._userControlTagViewModel.IsLogin = false;
                    //BtnMaintenance.IsEnabled = false;
                    sw.Reset();
                }
            }
            else
            {
                if (sw.IsRunning)
                {
                    sw.Restart();
                }
            }
        }

        public static bool IsAdminLogin = false;

        private void _userControlTagViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var b = (UserControlTagViewModel)sender;

            if (e.PropertyName == "IsAdminLogin")
            {
                if (b.IsAdminLogin)
                {
                    BtnMaintenance.IsEnabled = true;
                    IsAdminLogin = true;
                }
                else
                {
                    //BtnMaintenance.IsEnabled = false;
                    //IsAdminLogin = false;

                    BtnMaintenance.IsEnabled = true;
                    IsAdminLogin = true;
                }
                userChangedEvent?.Invoke(b.IsAdminLogin);

            }
            if (b.IsLogin)
            {
                GlobalVar.CurrentUserName = this.UserManagementControl1._userControlTagViewModel.CurrentUserName;
                BtnMaintenance.IsEnabled = true;
            }
            else
            { 
                //BtnMaintenance.IsEnabled = false; 
            }
            if (b.IsLogin&& !sw.IsRunning)
            {
                sw.Start();
            }
        }

        MainApp mainApp;

        public MainBtnPanel(MainWindow mainWindow)
        {
            InitializeComponent();
            timer=new DispatcherTimer();
            timer.Tick+=Timer_Tick;
            timer.Interval=TimeSpan.FromSeconds(2);
            timer.Start();
            this.UserManagementControl1._userControlTagViewModel.PropertyChanged+=_userControlTagViewModel_PropertyChanged;
            this.ManualAppoinment.DataContext=InputCV.uIModel;
            mainWindow.Subscribe(this);

            mainApp = mainWindow.mainapp;
        }

        private void BtnManualAppointment_Click(object sender, RoutedEventArgs e)
        {
            InputCV.TriggerAppointment = !InputCV.TriggerAppointment;
            if (InputCV.TriggerAppointment == true)
            {
                InputCV.uIModel.ForegroundColor = "red";
                InputCV.sw1.Start();

            }
            else
            {
                InputCV.uIModel.ForegroundColor = "#FF005DAA";
            }

        }

        private void BtnRunTime_Click(object sender, RoutedEventArgs e)
        {
            btnRunTimeView();
            GlobalVar.isManualMode = false;
        }

        private void BtnMaintenance_Click(object sender, RoutedEventArgs e)
        {
            btnSystemSetupView();
            GlobalVar.isManualMode = true;
        }

        private void BtnLogOut_Click(object sender, RoutedEventArgs e)
        {
            btnLogout();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //UserManagementWindow.RegisterStatus =!GlobalVar.bHasPassword;
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            mainApp.pMaster.SetPause();
            if (MessageBox.Show("The device has been paused, click OK to continue running", "tips", MessageBoxButton.OK, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                mainApp.pMaster.ResetPause();
            }
        }

        private void BtnAlarmStatistic_Click(object sender, RoutedEventArgs e)
        {
            StatisticsWindow statisticWindow = new StatisticsWindow();
            statisticWindow.Show();
        }
    }
}