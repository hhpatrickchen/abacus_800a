using HandyControl.Tools;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Sopdu
{
    //    /// <summary>
    //    /// Interaction logic for App.xaml
    //    /// </summary>
    public partial class App : Application
    {
        private static Mutex instanceMutex;

        private void App_Startup(object sender, StartupEventArgs e)
        {            
           
            ShutdownMode = System.Windows.ShutdownMode.OnLastWindowClose;
            bool createdNew = true;
            instanceMutex = new Mutex(true, @"Abacus System", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Cannot Have Two Instance of Program Running");
                Application.Current.Shutdown();
                return;
            }
            Splasher.Splash = new SplashScreen();
            Splasher.ShowSplash();
            MainWindow mainWindow;
            try
            {
                mainWindow = new MainWindow();
                Splasher.CloseSplash();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString()); Splasher.CloseSplash();
            }
        }
    }
}