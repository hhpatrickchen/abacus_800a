using LogProject;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

namespace LogPanel
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class LogUserControl : UserControl
    {
        public static MainViewModel _mainViewModel;
        public LogUserControl()
        {
            InitializeComponent();
            _mainViewModel= new MainViewModel();
            this.DataContext = _mainViewModel;
            LoadLogData();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           
            string path = AppDomain.CurrentDomain.BaseDirectory + "LogData.xml";
            Help<ObservableCollection<LogData>>.Instance.DoSerialization(_mainViewModel.LogClasses, path);
        }


        public void LoadLogData()
        {
            for (int i = 0; i < GlobalData.LogDataList.Count; i++)
            {
                GlobalData.LogDataList[i].Item = i + 1;
                _mainViewModel.LogClasses.Add(GlobalData.LogDataList[i]);
            }
            string path = AppDomain.CurrentDomain.BaseDirectory + "LogData.xml";
            if (!File.Exists(path))
            {
                return;
            }
            var deDeserializationResult = Help<ObservableCollection<LogData>>.Instance.DoDeserialization(path);
            for (int i = 0; i < deDeserializationResult.Count; i++)
            {
                try
                {
                    var firstItem = _mainViewModel.LogClasses.First<LogData>(t => t.Name == deDeserializationResult[i].Name);
                    firstItem.SelectedIndex = deDeserializationResult[i].SelectedIndex;
                }
                catch (Exception)
                {
                    continue;
                }
            }
            GlobalData.LogDataList.Clear();
            foreach (var item in _mainViewModel.LogClasses)
            {
                GlobalData.LogDataList.Add(item);
            }
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

       
    }
}
