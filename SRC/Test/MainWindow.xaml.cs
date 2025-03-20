using Dct.Models;
using Dct.Models.Repository;
using Dct.UI.Alarm.Views;
using HandyControl.Tools;
using System;
using System.Windows;

namespace Test
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DbManager.Instance.ConnectDB();

            //var re = DbManager.Instance.GetRepository<AlarmHistoryRepository>();

            //re.SetAlarm(new Dct.Models.Entity.AlarmHistoryEntity() { Code = "A05", Description = "A01 aa", Type = "Alarm", StationID = "1" }, out _);
            //Thread.Sleep(3000);
            //re.ClearAllAlarm( out _); 
            //re.SetAlarm(new Dct.Models.Entity.AlarmHistoryEntity() { Code = "A05", Description = "A01 aa", Type = "Alarm", StationID = "1" }, out _);
            //Thread.Sleep(1000);
            //re.ClearAllAlarm(out _);

            //re.SetAlarm(new Dct.Models.Entity.AlarmHistoryEntity() { Code = "A07", Description = "A02 aa", Type = "Alarm", StationID = "1" }, out _);
            //Thread.Sleep(1500);
            //re.ClearAllAlarm(out _);


            //var re1 = DbManager.Instance.GetRepository<ProductResultRepository>();

            //re1.Insert(new Dct.Models.Entity.ProductResultEntity() { Code = "A05", StartTime = DateTime.Now.AddMinutes(-5), Recipe = "AAA", EndTime = DateTime.Now, Result = "Fail" }, out _);
            //re1.Insert(new Dct.Models.Entity.ProductResultEntity() { Code = "A06", StartTime = DateTime.Now.AddMinutes(-8), Recipe = "AAA", EndTime = DateTime.Now, Result = "Fail" }, out _);
            //re1.Insert(new Dct.Models.Entity.ProductResultEntity() { Code = "A07", StartTime = DateTime.Now.AddMinutes(-9), Recipe = "AAA", EndTime = DateTime.Now, Result = "Fail" }, out _);
            //re1.Insert(new Dct.Models.Entity.ProductResultEntity() { Code = "A08", StartTime = DateTime.Now.AddMinutes(-1), Recipe = "AAA", EndTime = DateTime.Now, Result = "Fail" }, out _);
            //re1.Insert(new Dct.Models.Entity.ProductResultEntity() { Code = "A09", StartTime = DateTime.Now.AddMinutes(-5), Recipe = "AAA", EndTime = DateTime.Now, Result = "Fail" }, out _);


            //var re2 = DbManager.Instance.GetRepository<ParamterChangeHistoryRepository>();

            //re2.Insert(new Dct.Models.Entity.ParameterChangeHistoryEntity() { UserName="Admin", StationID="1", Name="A1", OldValue="123", NewValue="23", ChangeTime=DateTime.Now}, out _);
            //re2.Insert(new Dct.Models.Entity.ParameterChangeHistoryEntity() { UserName="Admin", StationID="1", Name="A1", OldValue="123", NewValue="23", ChangeTime=DateTime.Now}, out _);

            var re3 = DbManager.Instance.GetRepository<FingerEngagementRepository>();

            re3.Insert(new Dct.Models.Entity.FingerEngagementEntity() { Name = "Shutter01 Chk1", ShutterName="Shutter01", StartTime = DateTime.Now }, out _);
            re3.Insert(new Dct.Models.Entity.FingerEngagementEntity() { Name = "Shutter01 Chk1", ShutterName="Shutter01", StartTime = DateTime.Now }, out _);
            re3.Insert(new Dct.Models.Entity.FingerEngagementEntity() { Name = "Shutter01 Chk2", ShutterName="Shutter01", StartTime = DateTime.Now }, out _);
            re3.Insert(new Dct.Models.Entity.FingerEngagementEntity() { Name = "Shutter01 Chk2", ShutterName="Shutter01", StartTime = DateTime.Now }, out _);
            re3.Insert(new Dct.Models.Entity.FingerEngagementEntity() { Name = "Shutter02 Chk3", ShutterName="Shutter02", StartTime = DateTime.Now }, out _);
            re3.Insert(new Dct.Models.Entity.FingerEngagementEntity() { Name = "Shutter02 Chk2", ShutterName="Shutter02", StartTime = DateTime.Now }, out _);
            re3.Insert(new Dct.Models.Entity.FingerEngagementEntity() { Name = "Shutter02 Chk2", ShutterName="Shutter02", StartTime = DateTime.Now }, out _);

        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            StatisticsWindow aa = new StatisticsWindow();
            aa.Show();
        }
    }
}
