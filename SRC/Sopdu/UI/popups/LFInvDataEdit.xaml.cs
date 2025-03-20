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
using System.Windows.Shapes;

namespace Sopdu.UI.popups
{
    /// <summary>
    /// Interaction logic for LFInvDataEntry.xaml
    /// </summary>
    public partial class LFInvDataEdit : Window
    {
        public LFInvDataEdit()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Invstring = this.invtext.Text;
            Invstring = Invstring.Trim();
            if (Invstring.Count() > 3)
            {
                try
                {
                    //update database
                    DBAccess db = new DBAccess();
                    db.InserLFInv(RecipeName, Invstring, "default");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid LF INV #, Data could have declared elsewhere.");
                    return;
                }
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid Lead Frame Inv #");
            }
        }

        public string RecipeName { get; set; }
        public string SelectedInvStr { get; set; }
        public string Invstring { get; set; }

        internal void setOldString(string oldval)
        {
            try
            {
                DBAccess db = new DBAccess();
                RecipeName = db.GetRecipeFromLFInv(oldval);
                SelectedInvStr = oldval;
                invtext.Text = oldval;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exceptional Error In Loading LF Inv Data");
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Invstring = this.invtext.Text;
            Invstring = Invstring.Trim();
            if (Invstring.Count() > 3)
            {
                try
                {
                    //update database
                    DBAccess db = new DBAccess();
                    db.RemoveInvName(Invstring);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid LF INV #, Data could have declared elsewhere.");
                    return;
                }
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid Lead Frame Inv #");
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Invstring = this.invtext.Text;
            Invstring = Invstring.Trim();
            if (Invstring.Count() > 3)
            {
                try
                {
                    //update database
                    DBAccess db = new DBAccess();
                    db.UpdateInvName(Invstring, SelectedInvStr);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid LF INV #, Data could have declared elsewhere.");
                    return;
                }
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid Lead Frame Inv #");
            }
        }
    }
}