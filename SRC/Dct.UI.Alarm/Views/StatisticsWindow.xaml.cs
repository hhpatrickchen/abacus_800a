using HandyControl.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Dct.UI.Alarm.Views
{
    /// <summary>
    /// StatisticsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StatisticsWindow : Window
    {
        public StatisticsWindow()
        {
            InitializeComponent();
            // 设置 HandyControl 的语言
            ConfigHelper.Instance.SetLang("en");
        }
    }
}
