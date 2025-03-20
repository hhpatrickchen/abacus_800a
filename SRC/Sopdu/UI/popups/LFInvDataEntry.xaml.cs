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

namespace Sopdu.UI.popups
{
    /// <summary>
    /// Interaction logic for LFInvDataEntry.xaml
    /// </summary>
    public partial class LFInvDataEntry : Window
    {
        public LFInvDataEntry()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Invstring = this.invtext.Text;
            Invstring = Invstring.Trim();
            if (Invstring.Count() > 3)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid Lead Frame Inv #");
            }
        }

        public string Invstring { get; set; }

        internal void setOldString(string oldval)
        {
            invtext.Text = oldval;
        }
    }
}