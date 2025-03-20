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
    /// Interaction logic for ParamEdit.xaml
    /// </summary>
    public partial class ParamEdit : Window
    {
        public ParamEdit()
        {
            InitializeComponent();
        }

        public void Init(Substrate sb)
        {
            DataContext = sb;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
            return;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
            return;
        }
    }
}