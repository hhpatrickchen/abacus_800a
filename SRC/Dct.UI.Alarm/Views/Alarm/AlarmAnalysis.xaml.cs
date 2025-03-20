using HandyControl.Controls;
using HandyControl.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dct.UI.Alarm.Views.Alarm
{
    /// <summary>
    /// AlarmAnalysis.xaml 的交互逻辑
    /// </summary>
    public partial class AlarmAnalysis : UserControl
    {
        public AlarmAnalysis()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //// 获取 Popup 元素
            //var popup = (Popup)StartDateTimePicker.Template.FindName("PART_Popup", StartDateTimePicker);
            //if (popup != null && popup.Child is FrameworkElement popupContent)
            //{
            //    // 遍历 Popup 的子控件寻找 Button
            //    var button = popupContent.FindName("ConfirmButton") as Button;
            //    if (button != null)
            //    {
            //        button.Content = "确定时间";
            //    }
            //}
        }
    }
}
