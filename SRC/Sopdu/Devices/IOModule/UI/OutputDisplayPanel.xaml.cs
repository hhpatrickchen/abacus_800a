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

namespace Sopdu.Devices.IOModule.UI
{
    /// <summary>
    /// Interaction logic for OutputDisplayPanel.xaml
    /// </summary>
    public partial class OutputDisplayPanel : UserControl
    {
        public OutputDisplayPanel()
        {
            InitializeComponent();
        }

        private void btnToggle_Click(object sender, RoutedEventArgs e)
        {
            DiscreteIO output = this.DataContext as DiscreteIO;
            try
            {
                if (output.IOName=="Output53"|| output.IOName== "Output54"|| output.IOName == "Output55"||
                    output.IOName == "Output57" || output.IOName == "Output60" || output.IOName == "Output49" || 
                    output.IOName == "Output50" || output.IOName == "Output51" || output.IOName == "Output66" || output.IOName == "Output68")
                {
                    var result = MessageBox.Show("Trigger stocker handshake signal?", "", MessageBoxButton.OKCancel);
                    if (result.ToString()=="OK")
                    {
                        output.Logic = !output.Logic;
                    }
                }
                else output.Logic = !output.Logic;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}