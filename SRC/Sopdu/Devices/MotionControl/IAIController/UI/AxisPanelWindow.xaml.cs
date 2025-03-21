using Sopdu.Devices.MotionControl.Base;
using Sopdu.Devices.MotionControl.DeltaController;
using Sopdu.Devices.MotionControl.DeltaEtherCAT;
using System;
using System.Collections.Generic;
using System.Drawing;
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
    /// AxisPanelWindow.xaml 的互動邏輯
    /// </summary>
    public partial class AxisPanelWindow : Window
    {
        public AxisPanelWindow()
        {
            InitializeComponent();
            InitializeAxisPanel();
        }
        private void InitializeAxisPanel()
        {
            ushort CardNo = 0;
            ushort nodeID = 0;
            ushort slotNo = 0;
            byte axisNumber = 0;
            Axis axis = new DeltaEtherCATAxis(new DeltaControllerChannel(CardNo, nodeID, slotNo), axisNumber); // 替換為具體的 Axis 實現類別

            axis.bIsEnable = true;

            axis.DisplayName = "Test Name";
            //load axis position
            axis.PositionFilePath = @".\Positions\" + "MP1" + ".zip";
            axis.ReadPositionFile();

            this.DataContext = axis;
         
        }
    }
}
