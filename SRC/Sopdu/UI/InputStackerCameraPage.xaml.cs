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

namespace Sopdu.UI
{
    /// <summary>
    /// Interaction logic for InputStackerCameraPage.xaml
    /// </summary>
    public partial class InputStackerCameraPage : Page
    {
        private MainWindow main;

        public InputStackerCameraPage()
        {
            InitializeComponent();
        }

        public InputStackerCameraPage(MainWindow main)
        {
            InitializeComponent();
            this.main = main;
            DataContext = main.mainapp;
            carriermapui.Init(main.mainapp.InputCVCamera);
            singlebcrui.Init(main.mainapp.InputCVCamera);
            trayposui.Init(main.mainapp.InputCVCamera);
        }
    }
}