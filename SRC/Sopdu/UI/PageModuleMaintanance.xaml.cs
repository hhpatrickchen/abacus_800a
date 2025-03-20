using Insphere.Connectivity.Application.SecsToHost;
using Sopdu.Devices.SecsGem;
using Sopdu.helper;
using Sopdu.ProcessApps.main;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sopdu.UI
{
    /// <summary>
    /// Interaction logic for PageModuleMaintanance.xaml
    /// </summary>
    public partial class PageModuleMaintanance : Page
    {
        private ObservableCollection<Process> p;
        private MainWindow main;

        public PageModuleMaintanance()
        {
            InitializeComponent();
        }

        public PageModuleMaintanance(MainWindow mainwin)
        {
            main = mainwin;
            InitializeComponent();
            DataContext = mainwin.mainapp;
            //DataContext = mainwin.mainapp.pMaster.ProcessList;
            main.mainapp.GemCtrl.SetProcessingState(ProcessingState.Idle);
            main.mainapp.GemCtrl.SetEquipmentState(GemEquipmentState.Standby, "EquipmentState");
        }

        public PageModuleMaintanance(ObservableCollection<Process> process)
        {
            // TODO: Complete member initialization
            p = process;
            InitializeComponent();
            DataContext = process;
        }

        private void TabControl_Initialized(object sender, EventArgs e)
        {
        }

        public Page DispMid
        {
            get { return (Page)this.BtnMidFrame.Content; }
            set
            {
                this.BtnMidFrame.Navigate(value);
                this.BtnMidFrame.NavigationService.RemoveBackEntry();
                BtnMidFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
                TranslateTransform scale = new TranslateTransform(0, 5);
                this.BtnMidFrame.SetValue(RenderTransformProperty, scale);
                scale.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
            }
        }

        public Page DispInputStacker
        {
            get { return (Page)this.BtnInputStackerFrame.Content; }
            set
            {
                this.BtnInputStackerFrame.Navigate(value);
                this.BtnInputStackerFrame.NavigationService.RemoveBackEntry();
                BtnInputStackerFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
                TranslateTransform scale = new TranslateTransform(0, 5);
                this.BtnInputStackerFrame.SetValue(RenderTransformProperty, scale);
                scale.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
            }
        }

        private void BtnDockPanelMid_Initialized(object sender, EventArgs e)
        {
            try
            {
                DispInputStacker = new InputStackerCameraPage(main);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        private void BtnMidFrame_Initialized(object sender, EventArgs e)
        {
            try
            {
               //this.DispMid = new PageRegionSetup(main);
                this.DispMid = new ProfilerSetup(main);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        private void gridRecipeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void BtnCreateRecipe(object sender, RoutedEventArgs e)
        {
        }

        private void BtnLoadRecipe(object sender, RoutedEventArgs e)
        {
        }

        private void BtnRemoveRecipe(object sender, RoutedEventArgs e)
        {
        }

        private void BtnInputStackerFrame_Initialized(object sender, EventArgs e)
        {
            try
            {
                DispInputStacker = new InputStackerCameraPage(main);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                if (main != null)
                    if (((System.Windows.Controls.ComboBox)sender).SelectedIndex == 1)
                    {
                        main.mainapp.GemCtrl.SetEquipmentState(GemEquipmentState.Engineering, "EquipmentState");
                        main.mainapp.GemCtrl.SetControlState(false);
                        main.mainapp.GemCtrl.SetCommunicationState(false);
                        main.mainapp.GemCtrl.SetLocalMode();
                    }
                    else
                    {

                        //if already online skip
                        if (main.mainapp.GemCtrl.gemController.CommunicationState != CommunicationState.Disabled) return;
                        //end
                        {
                            main.mainapp.GemCtrl.SetCommunicationState(true);
                            main.mainapp.GemCtrl.SetControlState(true);
                            main.mainapp.GemCtrl.SetRemoteMode();
                            main.mainapp.GemCtrl.SetProcessingState(ProcessingState.Idle);
                            main.mainapp.GemCtrl.SetEquipmentState(GemEquipmentState.Standby, "EquipmentState");
                            main.mainapp.GemCtrl.SetLoadPortReservationState01(LoadPortReservState.NotReserved, "LoadPortReservationStateNotReserved1");
                            main.mainapp.GemCtrl.SetLoadPortReservationState02(LoadPortReservState.NotReserved, "LoadPortReservationStateNotReserved2");
                            main.mainapp.GemCtrl.SetLoadPortAccessMode01(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual1");
                            main.mainapp.GemCtrl.SetLoadPortAccessMode02(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual2");
                            main.mainapp.GemCtrl.SetLoadPortAssociateState01(LoadPortAssocState.NotAssociated, "LoadPortAssociationStateNotAssociated1");
                            main.mainapp.GemCtrl.SetLoadPortAssociateState02(LoadPortAssocState.NotAssociated, "LoadPortAssociationStateNotAssociated2");
                            if (main.mainapp.GemCtrl.cmdHostSetCompleteEvt.WaitOne(2000))
                                MessageBox.Show("Host Respond to Online request");
                            else
                                MessageBox.Show("Host Respond Time Out, yet to recieve CEID reassignment");

                        }
                    }
            }
            catch (Exception ex) { }
            finally { Mouse.OverrideCursor = null; }
        }

        private void BtnSetFrame_Initialized(object sender, EventArgs e)
        {

        }
        private void btnShowDIO_Click(object sender, EventArgs e)
        {
            FrmAllDIO frmAllDIO = new FrmAllDIO();
            if (frmAllDIO.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

            }

        }
        private void btnSelectMode_Click(object sender, EventArgs e)
        {
            FrmModeOption frmModeOption = new FrmModeOption();
            if (frmModeOption.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // options for input port
                if (GlobalVar.lstIPMode.Contains(EIPMode.OHT.ToString()))
                {
                    // SetOHT to Auto
                    main.mainapp.GemCtrl.SetLoadPortAccessMode01(LoadPortAccessMode.Auto, "LoadPortAccessModeStateAuto1");
                    //                    main.mainapp.GemCtrl.SetLoadPortAccessMode02(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual2");

                }
                else
                {
                    // SetOHT to Manual
                    main.mainapp.GemCtrl.SetLoadPortAccessMode01(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual1");
                    //                    main.mainapp.GemCtrl.SetLoadPortAccessMode02(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual2");


                }
                if (GlobalVar.eOPMode == EOPMode.OHT)
                {
                    main.mainapp.GemCtrl.SetLoadPortAccessMode02(LoadPortAccessMode.Auto, "LoadPortAccessModeStateAuto2");
                }
                else
                {
                    main.mainapp.GemCtrl.SetLoadPortAccessMode02(LoadPortAccessMode.Manual, "LoadPortAccessModeStateManual2");
                }
            }
        }

        private void LogPanel_Click(object sender, RoutedEventArgs e)
        {
            LogControlPanel logPanel = new LogControlPanel();
            logPanel.ShowDialog();
        }

        private void KeyenceDetect_Click(object sender, RoutedEventArgs e)
        {
            frmKeyenceSensor frmKeyenceSensor = new frmKeyenceSensor();
            frmKeyenceSensor.ShowDialog();
        }
    }
}