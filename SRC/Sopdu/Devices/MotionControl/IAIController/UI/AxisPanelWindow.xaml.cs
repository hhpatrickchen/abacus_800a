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
        DeltaController.DeltaController deltaController;
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

            deltaController = new DeltaController.DeltaController();
            
            deltaController.MotorAxis = new DeltaEtherCATAxis(new DeltaControllerChannel(CardNo, nodeID, slotNo), axisNumber); // 替換為具體的 Axis 實現類別
            deltaController.Init(CardNo, nodeID, slotNo);

            deltaController.MotorAxis.bIsEnable = true;

            deltaController.MotorAxis.DisplayName = "Test Name";
            //load axis position
            deltaController.MotorAxis.PositionFilePath = @".\Positions\" + "MP1" + ".zip";
            deltaController.MotorAxis.ReadPositionFile();
            
            this.DataContext = deltaController.MotorAxis;
         
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            deltaController.Shutdown();
        }
    }
}
