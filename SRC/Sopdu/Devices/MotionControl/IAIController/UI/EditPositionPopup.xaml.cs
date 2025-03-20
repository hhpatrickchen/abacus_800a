using Sopdu.Devices.MotionControl.Base;
using Sopdu.Devices.MotionControl.IAIController.PConAxis;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Sopdu.Devices.MotionControl.IAIController.UI
{
    /// <summary>
    /// Interaction logic for EditPositionPopup.xaml
    /// </summary>
    public partial class EditPositionPopup : Window
    {
        public EditPositionPopup(AxisPosition axis)
        {
            InitializeComponent();
            DataContext = axis;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //    DialogResult = true;
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}