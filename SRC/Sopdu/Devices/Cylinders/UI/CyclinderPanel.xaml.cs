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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sopdu.Devices.Cylinders.UI
{
    /// <summary>
    /// Interaction logic for CyclinderPanel.xaml
    /// </summary>
    public partial class CyclinderPanel : UserControl
    {
        public CyclinderPanel()
        {
            InitializeComponent();
        }

        private void btnExtend_Click(object sender, RoutedEventArgs e)
        {
            Isoloniod valve = this.DataContext as Isoloniod;
            try
            {
                valve.Extend();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnRetract_Click(object sender, RoutedEventArgs e)
        {
            Isoloniod valve = this.DataContext as Isoloniod;
            try
            {
                valve.Retract();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}