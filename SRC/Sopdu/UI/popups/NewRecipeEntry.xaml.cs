using Cognex.VisionPro;
using Sopdu.helper;
using Sopdu.StripMapVision;
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
    /// Interaction logic for NewRecipeEntry.xaml
    /// </summary>
    public partial class NewRecipeEntry : Window
    {
        private Substrate sb;

        public NewRecipeEntry()
        {
            InitializeComponent();
        }

        public NewRecipeEntry(Substrate substrate)
        {
            InitializeComponent();
            sb = substrate;
            DataContext = substrate;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LFInvDataEntry frm = new LFInvDataEntry();

            if (frm.ShowDialog() == true)
            {
                //check if LFList exist in database;

                DBAccess db = new DBAccess();
                if (!db.SelectInv(frm.Invstring))
                {
                    foreach (string str in ((Substrate)DataContext).IFInvList)
                    {
                        if (str == frm.Invstring)//repeated string
                        {
                            MessageBox.Show("Existing Lead Frame Inv # exists");
                            return;
                        }
                    }
                    ((Substrate)DataContext).IFInvList.Add(frm.Invstring);
                }
                else
                    MessageBox.Show("Existing Lead Frame Inv # exists");
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string oldval = ((string)gridLFInvList.SelectedValue);
            LFInvDataEntry frm = new LFInvDataEntry();
            frm.setOldString(oldval);
            if (frm.ShowDialog() == true)
            {
                {
                    ((Substrate)DataContext).IFInvList.Remove(oldval);
                }
                ((Substrate)DataContext).IFInvList.Add(frm.Invstring);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ((Substrate)DataContext).IFInvList.Remove(((string)gridLFInvList.SelectedValue));
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            sb.Blocks = new Dictionary<string, StripBlock>();
            // sb.idtool = new Cognex.VisionPro.ID.CogIDTool();
            CogRectangle rec = new CogRectangle();
            //rec.GraphicDOFEnable = CogRectangleDOFConstants.All;
            //rec.Interactive = true;
            // sb.idtool.Region = (Cognex.VisionPro.ICogRegion)(rec);
            for (int i = 0; i < sb.numBlock; i++)
            {
                StripBlock bk = new StripBlock() { BlockNumber = i, column = sb.column, row = sb.row, Region = new Cognex.VisionPro.CogRectangle() };
                bk.Region.SelectedLineWidthInScreenPixels = 5;
                bk.Region.Color = Cognex.VisionPro.CogColorConstants.Red;
                bk.Region.TipText = "Block " + i.ToString();
                sb.Blocks.Add(i.ToString(), bk);
            }
            Close();
        }
    }
}