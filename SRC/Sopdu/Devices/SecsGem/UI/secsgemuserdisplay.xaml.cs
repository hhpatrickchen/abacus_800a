using Sopdu.helper;
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

namespace Sopdu.Devices.SecsGem.UI
{
    /// <summary>
    /// Interaction logic for secsgemuserdisplay.xaml
    /// </summary>
    public partial class secsgemuserdisplay : UserControl
    {
        public secsgemuserdisplay()
        {
            InitializeComponent();
        }

        private void listView1_Initialized(object sender, EventArgs e)
        {

        }

        private void listView1_Drop(object sender, DragEventArgs e)
        {

        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EqSecGem pmaster = ((ListView)sender).DataContext as EqSecGem;
            try
            {
                pmaster.ErrorDisplayMsg = ((CMsgClass)listView1.SelectedItem).Msg;
            }
            catch (Exception ex) { }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //get context//
            EqSecGem gemctrl = (EqSecGem)this.DataContext;
            string str = gemctrl.GetCurrentSvValue("ProcessState");
            if (str == "4")
            {
                ((ComboBox)sender).SelectedValue = (GemEquipmentState)int.Parse(gemctrl.GetCurrentSvValue("EquipmentState"));
                return;
            }
            gemctrl.SetEquipmentState((GemEquipmentState)((ComboBox)sender).SelectedValue, "EquipmentState");
        }
    }
}
