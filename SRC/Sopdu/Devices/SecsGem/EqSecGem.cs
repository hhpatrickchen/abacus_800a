using Insphere.Connectivity.Application.MessageServices;
using Insphere.Connectivity.Application.SecsToHost;
using Insphere.Connectivity.Common;
using Insphere.Connectivity.Common.ToolModel;
using Sopdu.Devices;
using Sopdu.helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Sopdu.Devices.SecsGem
{
    public class EqSecGem : GenericDevice
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region SecsToHost.Net Local Variables

        public ManualResetEvent cmdS14F2evt;
        public ManualResetEvent cmdS10F5evt;
        public ManualResetEvent cmdS3F17evt;
        public ManualResetEvent cmdS2F41evt;
        public ManualResetEvent cmdS2F42evt;
        public ManualResetEvent cmdHostSetCompleteEvt;
        public string strS2F42Reststring;
        public string strS3F17SendString;
        public MapData mapData;
        public GEMController gemController;
        private MessageServiceManager ServiceManager;

        public ObservableCollection<CMsgClass> _DisplayMsg;
        public ObservableCollection<CMsgClass> DisplayMsg { get { return _DisplayMsg; } set { _DisplayMsg = value; NotifyPropertyChanged("DisplayMsg"); } }
        private string _ErrorDisplayMsg;
        public string ErrorDisplayMsg { get { return _ErrorDisplayMsg; } set { _ErrorDisplayMsg = value; NotifyPropertyChanged("ErrorDisplayMsg"); } }

        private string _ErrorDisplayMsg1;
        public string ErrorDisplayMsg1 { get { return _ErrorDisplayMsg1; } set { _ErrorDisplayMsg1 = value; NotifyPropertyChanged("ErrorDisplayMsg1"); } }

        private string _MachineMsg;
        public string MachineMsg { get { return _MachineMsg; } set { _MachineMsg = value; NotifyPropertyChanged("MachineMsg"); } }

        private delegate void SetLoggerCallback(string text);

        #endregion SecsToHost.Net Local Variables

        private ObservableCollection<string> _enabledEventList;
        public ObservableCollection<string> enabledEventList { get { return _enabledEventList; } set { _enabledEventList = value; NotifyPropertyChanged(); } }

        private ObservableCollection<string> _disabledEventList;
        public ObservableCollection<string> disabledEventList { get { return _disabledEventList; } set { _disabledEventList = value; NotifyPropertyChanged(); } }

        private ObservableCollection<string> _SVList;
        public ObservableCollection<string> SVList { get { return _SVList; } set { _SVList = value; NotifyPropertyChanged(); } }

        private ObservableCollection<string> _ECList;
        public ObservableCollection<string> ECList { get { return _ECList; } set { _ECList = value; NotifyPropertyChanged(); } }

        private ObservableCollection<string> _AlarmList;
        public ObservableCollection<string> AlarmList { get { return _AlarmList; } set { _AlarmList = value; NotifyPropertyChanged(); } }

        private ObservableCollection<string> _SetAlarmList;
        public ObservableCollection<string> SetAlarmList { get { return _SetAlarmList; } set { _SetAlarmList = value; NotifyPropertyChanged(); } }

        private string _gemComState;
        public string gemComstate { get { return _gemComState; } set { _gemComState = value; NotifyPropertyChanged(); } }

        private string _gemControlState;
        public string gemControlstate { get { return _gemControlState; } set { _gemControlState = value; NotifyPropertyChanged(); } }

        private string _gemProcessingState;
        public string gemProcessingstate { get { return _gemProcessingState; } set { _gemProcessingState = value; NotifyPropertyChanged(); } }

        private string _gemLoadPortTxferState;
        public string gemLoadPortTxferstate { get { return _gemLoadPortTxferState; } set { _gemLoadPortTxferState = value; NotifyPropertyChanged(); } }

        //gemLoadPortAssociatestate
        private string _gemLoadPortAssociatestate;

        public string gemLoadPortAssociatestate { get { return _gemLoadPortAssociatestate; } set { _gemLoadPortAssociatestate = value; NotifyPropertyChanged(); } }

        private string _gemLoadPortReservationState;
        public string gemLoadPortReservationstate { get { return _gemLoadPortReservationState; } set { _gemLoadPortReservationState = value; NotifyPropertyChanged(); } }

        private string _gemLoadPortAccessMode;
        public string gemLoadPortAccessMode { get { return _gemLoadPortAccessMode; } set { _gemLoadPortAccessMode = value; NotifyPropertyChanged(); } }

        private string _gemCarrierStatus;
        public string gemCarrierStatus { get { return _gemCarrierStatus; } set { _gemCarrierStatus = value; NotifyPropertyChanged(); } }

        private string _gemCarrierAccessingStatus;
        public string gemCarrierAccessingStatus { get { return _gemCarrierAccessingStatus; } set { _gemCarrierAccessingStatus = value; NotifyPropertyChanged(); } }

        private string _gemSlotMapStatus;
        public string gemSlotMapStatus { get { return _gemSlotMapStatus; } set { _gemSlotMapStatus = value; NotifyPropertyChanged(); } }

        //output port
        private string _gemLoadOutputPortTxferState;

        public string gemLoadOutputPortTxferstate { get { return _gemLoadOutputPortTxferState; } set { _gemLoadOutputPortTxferState = value; NotifyPropertyChanged(); } }

        //gemLoadOutputPortAssociatestate
        private string _gemLoadOutputPortAssociatestate;

        public string gemLoadOutputPortAssociatestate { get { return _gemLoadOutputPortAssociatestate; } set { _gemLoadOutputPortAssociatestate = value; NotifyPropertyChanged(); } }

        private string _gemLoadOutputPortReservationState;
        public string gemLoadOutputPortReservationstate { get { return _gemLoadOutputPortReservationState; } set { _gemLoadOutputPortReservationState = value; NotifyPropertyChanged(); } }

        private string _gemLoadOutputPortAccessMode;
        public string gemLoadOutputPortAccessMode { get { return _gemLoadOutputPortAccessMode; } set { _gemLoadOutputPortAccessMode = value; NotifyPropertyChanged(); } }

        private string _gemPP_Recipename;
        public string gemPP_Recipename { get { return _gemPP_Recipename; } set { _gemPP_Recipename = value; NotifyPropertyChanged(); } }

        private string _OPgemDisplayVisible;
        public string OPgemDisplayVisible { get { return _OPgemDisplayVisible; } set { _OPgemDisplayVisible = value; NotifyPropertyChanged(); } }
        private string _DispgemDisplayVisible;
        public string DispgemDisplayVisible { get { return _DispgemDisplayVisible; } set { _DispgemDisplayVisible = value; NotifyPropertyChanged(); } }

        public IList<GemEquipmentState> gemEquipmentStates { get { return Enum.GetValues(typeof(GemEquipmentState)).Cast<GemEquipmentState>().ToList<GemEquipmentState>(); } }
        private GemEquipmentState _currentgemEquipmentstate;
        public GemEquipmentState currentgemEquipmentstate { get { return _currentgemEquipmentstate; } set { _currentgemEquipmentstate = value; NotifyPropertyChanged(); } }

        //end of output port
        public EqSecGem()
        {
            SetAlarmList = new ObservableCollection<string>();
            SetDisplay(false);
            DisplayMsg = new ObservableCollection<CMsgClass>();
            terminate = false;
            cmdS10F5evt = new ManualResetEvent(false);//error toggle from host
            cmdS3F17evt = new ManualResetEvent(false);
            cmdHostSetCompleteEvt = new ManualResetEvent(false);
            cmdS14F2evt = new ManualResetEvent(false);
            cmdS2F41evt = new ManualResetEvent(false);
            cmdS2F42evt = new ManualResetEvent(false);
            // Initialize SecsToHost.Net
            InitializeSecsToHost();

            // Setup the communication default settings
            InitializeCommunication();

            // Setup the data collection settings
            InitializeDataCollection();

            // Setup the Process Program settings
            InitializeProcessProgram();

            // Setup the Alarm Management settings
            InitializeAlarmManagement();
        }

        public void SetDisplay(bool visible)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                if (visible)
                {
                    OPgemDisplayVisible = "Visible";
                    DispgemDisplayVisible = "Hidden";
                }
                else
                {
                    OPgemDisplayVisible = "Hidden";
                    DispgemDisplayVisible = "Visible";
                }
            });
        }

        /// <summary>
        /// Initialize SecsToHost.Net
        /// </summary>
        private void InitializeSecsToHost()
        {
            gemController = new GEMController();

            try
            {
                // Subscribe to the primary message event
                gemController.PrimaryMessageIn += new EquipmentController.SECsPrimaryInEventHandler(OnPrimaryMessageIn);

                // Subscribe to the secondary message event
                gemController.SecondaryMessageIn += new Insphere.Connectivity.Application.SecsToHost.GEMController.SECsSecondaryInEventHandler(OnSecondaryMessageIn);

                // Subscribe to the Message Trace event generated by the GEM controller.
                gemController.MessageActivityTrace += new Insphere.Connectivity.Application.SecsToHost.GEMController.SECsMessageActivityEventHandler(OnMessageActivityTrace);

                // Subscribe to the Communication state transition event
                gemController.CommunicationStateChanged += new Insphere.Connectivity.Application.SecsToHost.GEMController.SECsEventHandler(OnCommunicationStateChanged);

                // Subscribe to the GEM control state transition event
                gemController.ControlStateChanged += new Insphere.Connectivity.Application.SecsToHost.GEMController.SECsEventHandler(OnControlStateChanged);

                // Subscribe to the GEM processing state transition event
                gemController.ProcessingStateChanged += new Insphere.Connectivity.Application.SecsToHost.GEMController.SECsEventHandler(OnProcessingStateChanged);

                // Subscribe to the GEM spooling state transition event
                gemController.SpoolingStateChanged += new Insphere.Connectivity.Application.SecsToHost.GEMController.SECsEventHandler(OnSpoolingStateChanged);

                // Subscribe to the GEM Clock change event
                gemController.ClockChanged += new Insphere.Connectivity.Application.SecsToHost.GEMController.SECsClockEventHandler(OnClockChanged);

                // Subscribe to the GEM Remote Command Request
                gemController.RemoteCommandIn += new Insphere.Connectivity.Application.SecsToHost.GEMController.SECsRemoteCommandEventHandler(OnRemoteCommandIn);
                // Initialize GEM Controller
                //gemController.Initialize(@".\GemProtocol\LBSCounter.xml", @".\GemLog");
                gemController.Initialize(@".\LBSCounterremote.xml", @"C:\LogFiles");

                ServiceManager = gemController.Services;

                gemController.LoggerTimeFormat = @"yyyy/MM/dd HH:mm:ss.fff";
            }
            catch (Exception ex)
            {
                //all logging mechanism use abacus own log method
                //Logger("Error: " + ex.ToString());
            }

            if (!gemController.IsInitialized)
            {
                //Logger("Error: Failed to initialize SecsToHost.Net");
            }
        }

        #region GEM Controller Events

        /// <summary>
        /// SECS II primary message event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPrimaryMessageIn(object sender, SECsPrimaryInEventArgs e)
        {
            switch (e.EventId)
            {
                case PrimaryEventType.NewEquipmentConstantRequest:
                    OnEquipmentConstantChangeRequest(e);
                    break;

                case PrimaryEventType.StatusVariableRequest:
                    OnStatusVariableRequest(e);
                    break;

                case PrimaryEventType.ProcessProgramLoadInquire:	// Host initiate download of recipe request to equipment
                    // You may want to perform some logic before granting the equipment
                    //SECsMessage reply = ServiceManager.ProcessProgram.ProcessProgramLoadGrant(0);
                    //gemController.SendReply(reply, e.TransactionID);
                    break;

                case PrimaryEventType.ProcessProgramSend:	// Host download recipe to equipment
                    //OnHostRecipeDownload(e);
                    break;

                case PrimaryEventType.ProcessProgramRequest: // Host request equipment to upload recipe
                    //OnHostRecipeUploadRequest(e);
                    break;

                case PrimaryEventType.DeleteProcessProgramRequest: // Host request delete of Process Program
                    //OnDeleteProcessProgramRequest(e);
                    break;

                case PrimaryEventType.CurrentEPPDRequest: // Host request listing of current EPPD
                    //OnCurrentEPPDRequest(e);
                    break;

                //Obsolete in 1.2: This event is handled in RemoteCommandIn
                //case PrimaryEventType.RemoteCommand:	// Host send remote command
                //    OnRemoteCommand(e);
                //    break;

                case PrimaryEventType.TerminalDisplay:	// Host request to display terminal message
                    OnTerminalDisplay(e);
                    break;

                case PrimaryEventType.CustomMessageRequest: // Any other message there is not built-in in ServiceManager will fall into this section
                    OnCustomMessageRequest(e);
                    break;

                case PrimaryEventType.AlarmsEnabled:
                    OnAlarmEnableRequest(e);
                    break;

                default:
                    //Logger(e.EventId.ToString());
                    break;
            }
        }

        private void OnAlarmEnableRequest(SECsPrimaryInEventArgs e)
        {
            cmdHostSetCompleteEvt.Set(); ;
        }

        /// <summary>
        /// SECS II secondary message event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSecondaryMessageIn(object sender, SECsSecondaryInEventArgs e)
        {
            switch (e.EventId)
            {
                case SecondaryEventType.ProcessProgramInquireGrant: // S7F2 Reply
                    log.Debug("AA1");
                    if (e.Outputs.DataItem[0].ToInt() == 0)
                    {
                        //Logger("PP Inquire Granted");
                        //Thread myThread = new Thread(new ThreadStart(this.OnUploadRecipeToHost));
                        //myThread.Start();
                        //ThreadSafeUIHelper.Invoke(this, "OnUploadRecipeToHost", null);
                        //OnUploadRecipeToHost();
                    }
                    else
                    {
                        //Logger("PP Inquire Not Granted");
                    }
                    break;

                case SecondaryEventType.ProcessProgramSendReply:	// S7F4 Reply
                    log.Debug("AA2");
                    //Logger("Process Program Send Ack: " + e.Outputs.DataItem["ACKC7"].Value.ToString());
                    break;

                case SecondaryEventType.MapDataType2Ack: // S12F10
                    log.Debug("AA3");
                    //OnWaferMapUploadAck(e);
                    break;

                case SecondaryEventType.MapDataType2Download:	// S12F16
                    log.Debug("AA4");
                    //OnWaferMapDownload(e);
                    break;

                case SecondaryEventType.CustomMessageReply: // Custom Message Reply/Acknowlegement.. bascially we only use traymap here
                    log.Debug("AA5");
                    OnCustomMessageReply(e);
                    break;
            }
        }

        /// <summary>
        /// GEM controller message trace.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageActivityTrace(object sender, SECsMessageActivityEventArgs e)
        {
            //Logger(e.Severity.ToString() + ": " + e.Message);
        }

        /// <summary>
        /// GEM communication state transition event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCommunicationStateChanged(object sender, SECsEventArgs e)
        {
            //use this to show communication state
            Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                switch (gemController.CommunicationState)
                {
                    case CommunicationState.Disabled:
                        gemComstate = "DISABLED";
                        break;

                    case CommunicationState.Enabled:
                        gemComstate = "ENABLED";
                        break;

                    case CommunicationState.EnabledNotCommunicating:
                        gemComstate = "ENABLED/NOT COMMUNICATING";
                        break;

                    case CommunicationState.EnabledCommunicating:
                        gemComstate = "ENABLED/COMMUNICATING";
                        break;

                    case CommunicationState.WaitCRA:
                        gemComstate = "WAITCRA";
                        break;

                    case CommunicationState.WaitDelay:
                        gemComstate = "WAITDELAY";
                        break;
                }
            });
        }

        /// <summary>
        /// GEM control state transition event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnControlStateChanged(object sender, SECsEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                switch (gemController.ControlState)
                {
                    case ControlState.OnlineLocal:
                        gemControlstate = "ONLINE/LOCAL";
                        break;

                    case ControlState.OnlineRemote:
                        gemControlstate = "ONLINE/REMOTE";
                        break;

                    case ControlState.EquipmentOffline:
                        gemControlstate = "EQUIPMENT OFFLINE";
                        //ThreadSafeUIHelper.Invoke(this, "UpdateTextBox", new object[] { txtControlState, "EQUIPMENT OFFLINE" });
                        //ThreadSafeUIHelper.Invoke(this, "UpdateTextBox", new object[] { btnControlState, "Go ONLINE" });
                        break;

                    case ControlState.HostOffline:
                        gemControlstate = "HOST OFFLINE";
                        break;

                    case ControlState.AttemptOnline:
                        gemControlstate = "ATTEMPT ONLINE";
                        break;

                    case ControlState.Unknown:
                        gemControlstate = "UNKNOWN";
                        break;

                    default:
                        //ThreadSafeUIHelper.Invoke(this, "UpdateTextBox", new object[] { txtControlState, gemController.ControlState.ToString().ToUpper() });
                        break;
                }
            });
        }

        /// <summary>
        /// GEM control processing state transition event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnProcessingStateChanged(object sender, SECsEventArgs e)
        {
            //ThreadSafeUIHelper.Invoke(this, "UpdateTextBox", new object[] { cboProcessingState, gemController.ProcessingState.ToString().ToUpper() });

            Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                gemProcessingstate = gemController.ProcessingState.ToString();
            });
        }

        /// <summary>
        /// GEM spooling state transition event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSpoolingStateChanged(object sender, SECsEventArgs e)
        {
            //ThreadSafeUIHelper.Invoke(this, "UpdateTextBox", new object[] { txtSpoolingState, gemController.SpoolingState.ToString().ToUpper() });
            //ThreadSafeUIHelper.Invoke(this, "UpdateTextBox", new object[] { txtSpoolingSubState, gemController.SpoolingSubState.ToString().ToUpper() });
        }

        private void OnRemoteCommandIn(object sender, SECsRemoteCommandEventArgs e)
        {
            switch (e.CommandType)
            {
                case RemoteCommandType.S2F21RemoteCommand:
                    //OnS2F21RemoteCommand(e);
                    break;

                case RemoteCommandType.S2F41HostCommand:
                    OnS2F41HostCommand(e);
                    break;

                case RemoteCommandType.S2F49EnhancedRemoteCommand:
                    //OnS2F49EnhancedRemoteCommand(e);
                    break;
            }
        }

        private void OnClockChanged(object sender, SECsClockEventArgs e)
        {
            if (e.IsSuccessful)
            {
                //Logger("Clock changed successfully!");
            }
            else
            {
                //Logger("Clock changed failed!. Error: " + e.ExceptionMessage);
            }
        }

        #endregion GEM Controller Events

        #region Communication Tab

        /// <summary>
        /// Initialze the Communication setting
        /// </summary>
        public void InitializeCommunication()
        {
            // Communication state default to Disabled
            //this.txtCommunicationStatus.Text = "DISABLED";

            // Set the Control state to Offline mode
            gemController.SetOffline();

            // Set the remote control switch to Local
            gemController.SetLocal();

            // Set the processing state to Idle
            gemController.ProcessingState = ProcessingState.Idle;

            //this.cboProcessingState.Text = gemController.ProcessingState.ToString().ToUpper();
            this.OnSpoolingStateChanged(this, null);

            //rdoLocal.Checked = true;
        }

        /// <summary>
        /// Enable/Disable the GEM communication
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetCommunicationState(bool enable)
        {
            try
            {
                if (enable)
                {
                    gemController.SetEnable();
                }
                else
                {
                    gemController.SetDisable();
                }
            }
            catch { }
        }

        /// <summary>
        /// Set online/offline for the control state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetControlState(bool online)
        {
            if (online)
            {
                gemController.SetOnline();
            }
            else
            {
                gemController.SetOffline();
            }
        }

        /// <summary>
        /// Set Local Mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetLocalMode()
        {
            gemController.SetLocal();
        }

        /// <summary>
        /// Set Remote Mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetRemoteMode()
        {
            gemController.SetRemote();
        }

        /// <summary>
        /// Update Processing state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetProcessingState(ProcessingState processstate)
        {
            if (gemController.ProcessingState != processstate)//valid state change
            {
                UpdateSV("PreviousProcessState", ((int)gemController.ProcessingState).ToString());
                gemController.ProcessingState = processstate;
                // gemProcessingstate = ((int)gemController.ProcessingState).ToString();//should have been displayed
                gemController.SendCollectionEvent("ProcessStatus");
            }
        }

        #endregion Communication Tab

        #region Data Collection Tab

        /// <summary>
        /// Initialize the data collection UI settings
        /// </summary>
        public void InitializeDataCollection()
        {
            // Populate the collection event into selection box
            RefreshEventList();

            // Populate the status variables
            SVList = new ObservableCollection<string>();
            for (int j = 0; j < gemController.EquipmentModel.StatusVariables.Count; j++)
            {
                VariableType svid = gemController.EquipmentModel.StatusVariables[j];

                if (svid.valueType != SECSFormats.List)
                    this.SVList.Add(svid.logicalName);
            }

            // Populate the equipment constants
            ECList = new ObservableCollection<string>();
            for (int h = 0; h < gemController.EquipmentModel.EquipmentConstants.Count; h++)
            {
                VariableType ecid = gemController.EquipmentModel.EquipmentConstants[h];
                if (ecid.valueType != SECSFormats.List)
                    this.ECList.Add(ecid.logicalName);
            }
            gemLoadPortTxferstate = ((LoadPortTxferState)int.Parse(this.GetCurrentSvValue("LoadPortTransferState1"))).ToString();
            gemLoadPortAssociatestate = ((LoadPortAssocState)int.Parse(this.GetCurrentSvValue("LoadPortAssociationState1"))).ToString();
            gemLoadPortReservationstate = ((LoadPortReservState)int.Parse(this.GetCurrentSvValue("LoadPortReservationState1"))).ToString();
            gemLoadPortAccessMode = ((LoadPortAccessMode)int.Parse(this.GetCurrentSvValue("AccessMode1"))).ToString();

            gemLoadOutputPortTxferstate = ((LoadPortTxferState)int.Parse(this.GetCurrentSvValue("LoadPortTransferState2"))).ToString();
            gemLoadOutputPortAssociatestate = ((LoadPortAssocState)int.Parse(this.GetCurrentSvValue("LoadPortAssociationState2"))).ToString();
            gemLoadOutputPortReservationstate = ((LoadPortReservState)int.Parse(this.GetCurrentSvValue("LoadPortReservationState2"))).ToString();
            gemLoadOutputPortAccessMode = ((LoadPortAccessMode)int.Parse(this.GetCurrentSvValue("AccessMode2"))).ToString();

            gemCarrierStatus = ((CarrierIDStatus)int.Parse(this.GetCurrentSvValue("CarrierIdStatus1"))).ToString();
            gemSlotMapStatus = ((CarrierSlotMapStatus)int.Parse(this.GetCurrentSvValue("CarrierSlotMapStatus1"))).ToString();
            gemCarrierAccessingStatus = ((CarrierAccessingStatus)int.Parse(this.GetCurrentSvValue("CarrierAccessingStatus1"))).ToString();
            currentgemEquipmentstate = (GemEquipmentState)int.Parse(this.GetCurrentSvValue("EquipmentState"));
        }

        public void RestartSecsGem()
        {
            gemController.Disconnect();
            gemController.Dispose();
            gemController = null;
            InitializeSecsToHost();
        }

        public void SetEquipmentState(GemEquipmentState state, string ceidname)
        {
            GemEquipmentState currentstate = (GemEquipmentState)int.Parse(this.GetCurrentSvValue("EquipmentState"));
            if (currentstate != state)
            {
                UpdateSV("PreviousEquipmentState", ((int)currentstate).ToString());
                UpdateSV("EquipmentState", ((int)state).ToString());
            }
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetLoadPortTxferState01(LoadPortTxferState state, string ceidname)
        {
            LoadPortTxferState currentstate = (LoadPortTxferState)int.Parse(this.GetCurrentSvValue("LoadPortTransferState1"));
            if (currentstate != state)
            {
                UpdateSV("PreviousLoadPortTransferState1", ((int)currentstate).ToString());
                UpdateSV("LoadPortTransferState1", ((int)state).ToString());
                gemLoadPortTxferstate = ((LoadPortTxferState)int.Parse(this.GetCurrentSvValue("LoadPortTransferState1"))).ToString();
            }
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetLoadPortTxferState02(LoadPortTxferState state, string ceidname)
        {
            LoadPortTxferState currentstate = (LoadPortTxferState)int.Parse(this.GetCurrentSvValue("LoadPortTransferState2"));
            if (currentstate != state)
            {
                UpdateSV("PreviousLoadPortTransferState2", ((int)currentstate).ToString());
                UpdateSV("LoadPortTransferState2", ((int)state).ToString());
                gemLoadOutputPortTxferstate = ((LoadPortTxferState)int.Parse(this.GetCurrentSvValue("LoadPortTransferState2"))).ToString();
            }
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetLoadPortAssociateState01(LoadPortAssocState state, string ceidname)
        {
            LoadPortAssocState currentstate = (LoadPortAssocState)int.Parse(this.GetCurrentSvValue("LoadPortAssociationState1"));
            if (currentstate != state)
            {
                UpdateSV("PreviousLoadPortAssociationState1", ((int)currentstate).ToString());
                UpdateSV("LoadPortAssociationState1", ((int)state).ToString());
                gemLoadPortAssociatestate = ((LoadPortAssocState)int.Parse(this.GetCurrentSvValue("LoadPortAssociationState1"))).ToString();
            }
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetLoadPortAssociateState02(LoadPortAssocState state, string ceidname)
        {
            LoadPortAssocState currentstate = (LoadPortAssocState)int.Parse(this.GetCurrentSvValue("LoadPortAssociationState2"));
            if (currentstate != state)
            {
                UpdateSV("PreviousLoadPortAssociationState2", ((int)currentstate).ToString());
                UpdateSV("LoadPortAssociationState2", ((int)state).ToString());
                gemLoadOutputPortAssociatestate = ((LoadPortAssocState)int.Parse(this.GetCurrentSvValue("LoadPortAssociationState2"))).ToString();
            }
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetLoadPortReservationState01(LoadPortReservState state, string ceidname)
        {
            LoadPortReservState currentstate = (LoadPortReservState)int.Parse(this.GetCurrentSvValue("LoadPortReservationState1"));
            if (currentstate != state)
            {
                UpdateSV("PreviousLoadPortReservationState1", ((int)currentstate).ToString());
                UpdateSV("LoadPortReservationState1", ((int)state).ToString());
                gemLoadPortReservationstate = ((LoadPortReservState)int.Parse(this.GetCurrentSvValue("LoadPortReservationState1"))).ToString();
            }
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetLoadPortReservationState02(LoadPortReservState state, string ceidname)
        {
            LoadPortReservState currentstate = (LoadPortReservState)int.Parse(this.GetCurrentSvValue("LoadPortReservationState1"));
            if (currentstate != state)
            {
                UpdateSV("PreviousLoadPortReservationState2", ((int)currentstate).ToString());
                UpdateSV("LoadPortReservationState2", ((int)state).ToString());
                gemLoadOutputPortReservationstate = ((LoadPortReservState)int.Parse(this.GetCurrentSvValue("LoadPortReservationState2"))).ToString();
            }
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetLoadPortAccessMode01(LoadPortAccessMode state, string ceidname)
        {
            LoadPortAccessMode currentstate = (LoadPortAccessMode)int.Parse(this.GetCurrentSvValue("AccessMode1"));
            if (currentstate != state)
            {
                UpdateSV("PreviousAccessMode1", ((int)currentstate).ToString());
                UpdateSV("AccessMode1", ((int)state).ToString());
                gemLoadPortAccessMode = ((LoadPortAccessMode)int.Parse(this.GetCurrentSvValue("AccessMode1"))).ToString();
            }
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetLoadPortAccessMode02(LoadPortAccessMode state, string ceidname)
        {
            LoadPortAccessMode currentstate = (LoadPortAccessMode)int.Parse(this.GetCurrentSvValue("AccessMode1"));
            if (currentstate != state)
            {
                UpdateSV("PreviousAccessMode2", ((int)currentstate).ToString());
                UpdateSV("AccessMode2", ((int)state).ToString());
                gemLoadOutputPortAccessMode = ((LoadPortAccessMode)int.Parse(this.GetCurrentSvValue("AccessMode2"))).ToString();
            }
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetCarrierStatus(CarrierIDStatus state, string carrierid, List<string> traylist, string ceidname)
        {
            CarrierIDStatus currentstate = (CarrierIDStatus)int.Parse(this.GetCurrentSvValue("CarrierIdStatus1"));
            SECsDataItem dataitem = new SECsDataItem(SECsFormat.List);
            for (int i = traylist.Count - 1; i > -1; i--)
            {
                dataitem.Add("TrayID", traylist[i], SECsFormat.Ascii);
            }
            if (currentstate != state)
            {
                UpdateSV("PreviousCarrierIdStatus1", ((int)currentstate).ToString());
                UpdateSV("CarrierIdStatus1", ((int)state).ToString());
                gemCarrierStatus = ((CarrierIDStatus)int.Parse(this.GetCurrentSvValue("CarrierIdStatus1"))).ToString();
            }
            gemController.SetListAttribute("TrayList", AttributeType.DV, dataitem);
            gemController.SetAttribute("CarrierID", AttributeType.DV, carrierid);
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetSlotMapStatus(CarrierSlotMapStatus state, string ceidname, string carrierid, List<string> traylist)
        {
            CarrierSlotMapStatus currentstate = (CarrierSlotMapStatus)int.Parse(this.GetCurrentSvValue("CarrierSlotMapStatus1"));
            if (currentstate != state)
            {
                UpdateSV("PreviousCarrierSlotMapStatus1", ((int)currentstate).ToString());
                UpdateSV("CarrierSlotMapStatus1", ((int)state).ToString());
                gemSlotMapStatus = ((CarrierSlotMapStatus)int.Parse(this.GetCurrentSvValue("CarrierSlotMapStatus1"))).ToString();
            }
            gemController.SetAttribute("CarrierID", AttributeType.DV, carrierid);
            SECsDataItem dataitem = new SECsDataItem(SECsFormat.List);
            //foreach (string str in traylist)
            //   // dataitem.Add(str);
            //    dataitem.Add("TrayID", str, SECsFormat.Ascii);
            //   // dataitem.Add("TrayList", str, SECsFormat.Ascii);
            for (int i = traylist.Count - 1; i > -1; i--)
            {
                dataitem.Add("TrayID", traylist[i], SECsFormat.Ascii);
            }
            gemController.SetListAttribute("TrayList", AttributeType.DV, dataitem);
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetCarrierAccessStatus(CarrierAccessingStatus state, string ceidname)
        {
            CarrierAccessingStatus currentstate = (CarrierAccessingStatus)int.Parse(this.GetCurrentSvValue("CarrierAccessingStatus1"));
            if (currentstate != state)
            {
                UpdateSV("PreviousCarrierAccessingStatus1", ((int)currentstate).ToString());
                UpdateSV("CarrierAccessingStatus1", ((int)state).ToString());
                gemCarrierAccessingStatus = ((CarrierAccessingStatus)int.Parse(this.GetCurrentSvValue("CarrierAccessingStatus1"))).ToString();
            }
            gemController.SendCollectionEvent(ceidname);
        }

        public void SetCIDState(CarrierIDStatus state, string ceidname, string carrierid)
        {
            CarrierIDStatus currentstate = (CarrierIDStatus)int.Parse(this.GetCurrentSvValue("CarrierIdStatus1"));
            if (currentstate != state)
            {
                UpdateSV("PreviousCarrierIdStatus1", ((int)currentstate).ToString());
                UpdateSV("CarrierIdStatus1", ((int)state).ToString());
                gemCarrierStatus = ((CarrierIDStatus)int.Parse(this.GetCurrentSvValue("CarrierIdStatus1"))).ToString();
            }
            gemController.SetAttribute("CarrierID", AttributeType.DV, carrierid);
            gemController.SendCollectionEvent(ceidname);
        }

        //CEID 600
        public void InspectionStartStartEvent(string carrierid, List<string> traylist)
        {
            gemController.SetAttribute("CarrierID", AttributeType.DV, carrierid);
            SECsDataItem dataitem = new SECsDataItem(SECsFormat.List);
            //foreach (string str in traylist)
            //    //dataitem.Add(str);
            //    dataitem.Add("TrayID", str, SECsFormat.Ascii);
            for (int i = traylist.Count - 1; i > -1; i--)
            {
                dataitem.Add("TrayID", traylist[i], SECsFormat.Ascii);
            }
            gemController.SetListAttribute("TrayList", AttributeType.DV, dataitem);
            gemController.SendCollectionEvent("InspectionStart");
        }

        //S14F1 Request MapData from Host
        public void GetMapData(string trayid)
        {
            //S14F1 Request Map
            log.Debug("Get MapData From Host : " + trayid);
            cmdS14F2evt.Reset();
            SECsMessage customMessage = ServiceManager.CustomMessage.CreateMessage(14, 1, true);

            customMessage.DataItem.AddList(); // This function will add a LIST data type to the

            customMessage.DataItem[0].Add("OBJSPEC", "Equipment", SECsFormat.Ascii);
            customMessage.DataItem[0].Add("OBJSPEC", "Substrate", SECsFormat.Ascii);
            customMessage.DataItem[0].AddList();
            customMessage.DataItem[0][2].Add("SUBSTRATEID", trayid, SECsFormat.Ascii);
            customMessage.DataItem[0].AddList();
            customMessage.DataItem[0].AddList();
            customMessage.DataItem[0][4].Add("OBJTYPE", "MapData", SECsFormat.Ascii);

            //customMessage.DataItem[0].Add("OBJSPEC", "Equipment", SECsFormat.Ascii);
            //customMessage.DataItem[0].Add("OBJTYPE", "MapData", SECsFormat.Ascii);
            //customMessage.DataItem[0].AddList();
            //customMessage.DataItem[0][2].Add("SUBSTRATEID", trayid, SECsFormat.Ascii);
            //customMessage.DataItem[0].AddList();
            //customMessage.DataItem[0].AddList();
            gemController.Send(customMessage);
        }

        //CEID 601,603
        public void TrayInspectionStart_END(bool start, string trayid, MapData traymap, string id, int qty = 0)
        {
            if (traymap == null)
            {
                log.Debug("kong" + trayid);  
                return; } 
            string straymap = XMLConverter.ToXml(traymap, traymap.GetType());
            gemController.SetAttribute("TrayID", AttributeType.DV, trayid);
            gemController.SetAttribute("TrayMap", AttributeType.DV, straymap);
            gemController.SetAttribute("InspectionID", AttributeType.DV, id);
            gemController.SetAttribute("Qty", AttributeType.DV, qty.ToString());//Qty

            if (start)
                gemController.SendCollectionEvent("TrayInspectionStart");
            else
                gemController.SendCollectionEvent("TrayInspectionEnd");
        }

        //CEID 602
        public void TrayMapResult(string trayid, MapData traymap, string carrierid, string id)
        {
           // if (traymap.Layouts[0].ChildLayouts.ChildLayouts)
            log.Debug("mapdata" + trayid); 
            string straymap = XMLConverter.ToXml(traymap, traymap.GetType());
            gemController.SetAttribute("CarrierID", AttributeType.DV, carrierid);
            gemController.SetAttribute("TrayID", AttributeType.DV, trayid);
            gemController.SetAttribute("TrayMap", AttributeType.DV, straymap);
            gemController.SetAttribute("InspectionID", AttributeType.DV, id);
            gemController.SendCollectionEvent("TrayMapResult");
        }

        //CEID 604
        public void InspectionComplete(string carrierid, List<string> traylist)
        {
            gemController.SetAttribute("CarrierID", AttributeType.DV, carrierid);
            SECsDataItem dataitem = new SECsDataItem(SECsFormat.List);
            //foreach (string str in traylist)
            //    //dataitem.Add(str);
            //    dataitem.Add("TrayID", str, SECsFormat.Ascii);
            for (int i = traylist.Count - 1; i > -1; i--)
            {
                dataitem.Add("TrayID", traylist[i], SECsFormat.Ascii);
            }
            gemController.SetListAttribute("TrayList", AttributeType.DV, dataitem);
            gemController.SendCollectionEvent("InspectionComplete");
        }

        //CEID605
        public void BufferQtyChanged(int avaliablebuffer, int occupiedbuffer)
        {
            gemController.SetAttribute("AvailableBufferQty", AttributeType.SV, avaliablebuffer.ToString());
            gemController.SetAttribute("OccupiedBufferQty", AttributeType.SV, occupiedbuffer.ToString());
        }

        //CEID606
        public void CarrierReadStart(string carrierid, int inspectionid, string ceidname)
        {
            gemController.SetAttribute("CarrierID", AttributeType.DV, carrierid);
            gemController.SetAttribute("InspectionID", AttributeType.DV, inspectionid.ToString());
            gemController.SendCollectionEvent(ceidname);
        }

        /// <summary>
        /// Get the current status variable value. Use the GetAttribute method to query a NON LIST type data item.
        /// Use GetListAttribute to get the list type data item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public string GetCurrentSvValue(string svname)
        {
            return gemController.GetAttribute(svname, AttributeType.SV);
        }

        /// <summary>
        /// Set or update the status variable.
        /// Use the GetAttribute method to query a NON LIST type data item. Use GetListAttribute to get the list type data item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UpdateSV(string svname, string svvalue)
        {
            try
            {
                gemController.SetAttribute(svname, AttributeType.SV, svvalue);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Get the current equipment constant value. this will trigger the EC Changed Event
        /// Use the GetAttribute method to query a NON LIST type data item.
        /// Use GetListAttribute to get the list type data item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public string GetECValue(string ecname)
        {
            return gemController.GetAttribute(ecname, AttributeType.EC);
        }

        /// <summary>
        /// Set or update the status variable.
        /// Use the GetAttribute method to query a NON LIST type data item. Use GetListAttribute to get the list type data item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetECValue(string ecname, string ecvalue)
        {
            try
            {
                gemController.SetAttribute(ecname, AttributeType.EC, ecvalue);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Sending the selected event name to the Host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendEvent(string eventname)
        {
            gemController.SendCollectionEvent(eventname);
        }

        /// <summary>
        /// Enable selected event report.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableEvent(string eventname)
        {
            this.gemController.ActivateEventReport(eventname, true);
            this.RefreshEventList();
        }

        /// <summary>
        /// Refresh the enabled and disabled events list
        /// </summary>
        private void RefreshEventList()
        {
            ArrayList enabledEvents = this.gemController.GetAllEnabledEvents();
            enabledEventList = new ObservableCollection<string>();
            if (enabledEvents.Count > 0)// there are enabled events
            {
                for (int i = 0; i < enabledEvents.Count; i++)//update enabled event
                {
                    this.enabledEventList.Add(enabledEvents[i].ToString());
                }
            }

            disabledEventList = new ObservableCollection<string>();
            if (gemController.EquipmentModel.DataCollections.CEIDs.Count > 0)
            {
                // Populate the collection event into selection box
                for (int j = 0; j < gemController.EquipmentModel.DataCollections.CEIDs.Count; j++)
                {
                    CEID ceid = gemController.EquipmentModel.DataCollections.CEIDs[j];
                    if (!enabledEvents.Contains(ceid.logicalName))
                        this.disabledEventList.Add(ceid.logicalName);
                }
            }
        }

        /// <summary>
        /// Disabled the event report.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisableEvent(string eventname)
        {
            this.gemController.ActivateEventReport(eventname, false);
            this.RefreshEventList();
        }

        #endregion Data Collection Tab

        #region Process Program Tab

        /// <summary>
        /// Initialize the Process Program Management necessary information.
        /// You can Set ProcessProgram path and the current loaded recipe.
        /// </summary>
        private void InitializeProcessProgram()
        {
            // Set the path of the recipe. This path will be used to save the recipe to disks
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            gemController.SetProcessProgramPath(basePath);

            // Set the current selected recipe.
            gemController.SetCurrentSelectedProgram("Recipe.prg");

            //this.txtCurrentLoadedRecipe.Text = gemController.CurrentSelectedProgram;
        }

        /// <summary>
        /// Handle the Process program download from Host
        /// </summary>
        /// <param name="e"></param>
        private void OnHostRecipeDownload(SECsPrimaryInEventArgs e)
        {
            // Acknowledge the equipment via S7F4
            SECsMessage acknowledge = ServiceManager.ProcessProgram.ProcessProgramAcknowledge(0);
            gemController.SendReply(acknowledge, e.TransactionID);

            // Retrieve the PPID
            string ppid = e.Inputs.DataItem["Ln"]["PPID"].Value.ToString();

            // Get the format of PPBODY
            SECsFormat bodyType = e.Inputs.DataItem["Ln"]["PPBODY"].Format;

            if (bodyType == SECsFormat.Binary)
            {
                // Retrieve the PPBODY
                byte[] ppbody = (byte[])e.Inputs.DataItem["Ln"]["PPBODY"].Value;

                gemController.SaveProcessProgramToDisk(ppid, ppbody);
            }
            else
            {
                string ppbody = e.Inputs.DataItem["Ln"]["PPBODY"].ToString();

                gemController.SaveProcessProgramToDisk(ppid, ppbody);
            }
        }

        /// <summary>
        /// Host requests the equipment to send upload the requested process program.
        /// </summary>
        /// <param name="e"></param>
        private void OnHostRecipeUploadRequest(SECsPrimaryInEventArgs e)
        {
            try
            {
                //Logger("Received Recipe Download request for: " + e.Inputs.DataItem["PPID"].ToString());

                // Retrieving the requested Recipe File
                string recipeName = e.Inputs.DataItem["PPID"].ToString();
                string recipeFileName = this.gemController.ProcessProgramPath + "\\" + recipeName;

                if (File.Exists(recipeFileName))
                {
                    // Trying to open the request recipe file
                    FileStream fs = new FileStream(recipeFileName, FileMode.Open, FileAccess.Read);

                    // Create binary reader
                    BinaryReader r = new BinaryReader(fs);

                    // Read the binary content
                    byte[] ppbody = r.ReadBytes((int)fs.Length);

                    // Send the recipe body in the reply

                    SECsMessage recipeToReply = ServiceManager.ProcessProgram.ProcessProgramData(recipeName, ppbody);
                    gemController.SendReply(recipeToReply, e.TransactionID);

                    //Logger("Recipe: " + recipeName + " downloaded!");

                    r.Close();
                    fs.Close();
                }
                else
                {
                    SECsMessage recipeToReply = ServiceManager.ProcessProgram.ProcessProgramData(string.Empty, null);
                    gemController.SendReply(recipeToReply, e.TransactionID);
                }
            }
            catch (Exception ex)
            {
                SECsMessage recipeToReply = ServiceManager.ProcessProgram.ProcessProgramData(string.Empty, null);
                gemController.SendReply(recipeToReply, e.TransactionID);
                //Logger(ex.ToString());
            }
        }

        /// <summary>
        /// Host requests the equipment to delete the Process Program
        /// </summary>
        /// <param name="e"></param>
        private void OnDeleteProcessProgramRequest(SECsPrimaryInEventArgs e)
        {
            // Delete all Process Program in the storage
            if (e.Inputs.DataItem["Ln"].Count == 0)
            {
                // Perform the deletion of the Process Program
            }
            else // Delete the list of selected Process Program
            {
                for (int i = 0; i < e.Inputs.DataItem["Ln"].Count; i++)
                {
                    // PPID to be deleted
                    string ppid = e.Inputs.DataItem["Ln"][i].ToString();

                    // Perform the deletion of the selected Process Program
                }
            }

            SECsMessage messageToReply = ServiceManager.ProcessProgram.DeleteProcessProgramAcknowledge(0);

            this.gemController.SendReply(messageToReply, e.TransactionID);
        }

        /// <summary>
        /// Host requests the equipment to send the current EPPD
        /// </summary>
        /// <param name="e"></param>
        private void OnCurrentEPPDRequest(SECsPrimaryInEventArgs e)
        {
            SECsDataItem ppList = new SECsDataItem();
            ppList.Add("ppid01.prg", "ppid01.prg", SECsFormat.Ascii);
            ppList.Add("ppid02.prg", "ppid02.prg", SECsFormat.Ascii);

            SECsMessage messageToReply = ServiceManager.ProcessProgram.CurrentEPPDReply(ppList);
            this.gemController.SendReply(messageToReply, e.TransactionID);
        }

        /// <summary>
        /// Refresh the cboPPList that lists all the process programs in the path specified.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRefresh_Click(object sender, System.EventArgs e)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(gemController.ProcessProgramPath);

            if (directoryInfo != null)
            {
                //this.cboPPList.Items.Clear();

                FileInfo[] files = directoryInfo.GetFiles();

                foreach (FileInfo file in files)
                {
                    //   this.cboPPList.Items.Add(file.Name);
                }
            }
        }

        private void OnUploadInquire()
        {
            string ppid = "test";//tmp
            string recipeFileName = gemController.ProcessProgramPath + "\\" + ppid;

            FileStream fs = new FileStream(recipeFileName, FileMode.Open, FileAccess.Read);
            // Create binary reader
            BinaryReader r = new BinaryReader(fs);

            // Read the binary content of selected recipe
            byte[] ppbody = r.ReadBytes((int)fs.Length);

            // Send Recipe download inquire to equipment
            SECsMessage ppInquire = ServiceManager.ProcessProgram.ProcessProgramLoadInquire(ppid, ppbody.Length);
            //Logger("Sending Process program inquire to host");
            gemController.Send(ppInquire);

            r.Close();
            fs.Close();
        }

        /// <summary>
        /// Upload the recipe to the Host
        /// </summary>
        private void OnUploadRecipeToHost()
        {
            string ppid = "test";
            string recipeFileName = gemController.ProcessProgramPath + "\\" + ppid;

            FileStream fs = new FileStream(recipeFileName, FileMode.Open, FileAccess.Read);
            // Create binary reader
            BinaryReader r = new BinaryReader(fs);

            // Read the binary content of selected recipe
            byte[] ppbody = r.ReadBytes((int)fs.Length);

            SECsMessage recipeToSend = ServiceManager.ProcessProgram.ProcessProgramSend(ppid, ppbody);
            //Logger("Sending Process program upload");
            gemController.Send(recipeToSend);

            //			// Send Recipe download inquire to equipment
            //			SECsMessage ppInquire = ServiceManager.ProcessProgram.ProcessProgramLoadInquire(ppid, ppbody.Length);
            //			Logger("Sending Process program inquire to host");
            //			SECsMessage reply = gemController.SendAndWait(ppInquire);
            //
            //			if (reply != null) {
            //				if (reply.DataItem[0].ToInt() == 0) { // OK
            //					// Send Recipe download to equipment
            //					// Use asynchronous method as not to hold the thread. Use SendAndWait might hold the thread for
            //					// quite sometimes depending how big is the recipe body to be transfered.
            //					SECsMessage recipeToSend = ServiceManager.ProcessProgram.ProcessProgramSend(ppid, ppbody);
            //					Logger("Sending Process program upload");
            //					gemController.Send(recipeToSend);
            //				} else {
            //					Logger("PPGNT: " + reply.DataItem[0].ToString());
            //				}
            //			} else {
            //				Logger("No reply for process program load inquire");
            //			}

            r.Close();
            fs.Close();
        }

        /// <summary>
        /// Upload the recipe to Host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUploadRecipe_Click(object sender, System.EventArgs e)
        {
            //Thread myThread = new Thread(new ThreadStart(OnUploadInquire));
            //myThread.Start();
            OnUploadRecipeToHost();
        }

        #endregion Process Program Tab

        #region Request from Host handler

        /// <summary>
        /// Handle the remote command sent by Host
        /// </summary>
        /// <param name="e"></param>
        private void OnS2F21RemoteCommand(SECsRemoteCommandEventArgs args)//this is not used
        {
            switch (args.CommandLogicalName)
            {
                case "START":	// Start Command
                    gemController.ProcessingState = ProcessingState.Executing;
                    break;

                case "PAUSE":
                    gemController.ProcessingState = ProcessingState.Pause;
                    break;

                case "STOP":
                    gemController.ProcessingState = ProcessingState.Idle;
                    break;
            }

            // Send reply
            SECsMessage reply = ServiceManager.ControlDiagnostic.RemoteCommandAcknowledge(0);
            gemController.SendReply(reply, args.TransactionId);
        }

        private void OnS2F41HostCommand(SECsRemoteCommandEventArgs args)
        {
            switch (args.CommandLogicalName)
            {//recievingg PP select and other host commands //hwchang\
                case "PP-SELECT":
                    gemController.SetAttribute("PPExecName", AttributeType.SV, args.CommandParameters["PPID"].value);//set recipe name in PPExecName
                    strS2F42Reststring = "1";
                    cmdS2F42evt.Reset();
                    cmdS2F41evt.Set();
                    //wait for recipe load
                    while (!cmdS2F42evt.WaitOne(100))
                    {
                        if (terminate == true)
                        {
                            break;
                        }
                    }
                    SECsMessage PP_SELECTreply = ServiceManager.CustomMessage.CreateMessage(2, 42, false);
                    PP_SELECTreply.DataItem.AddList(); // This function will add a LIST data type to the
                    // collection
                    // Add OperatorBatchId item to the root LIST collection
                    PP_SELECTreply.DataItem[0].Add("reply_pp-select", strS2F42Reststring, SECsFormat.Ascii);
                    this.gemController.SendReply(PP_SELECTreply, args.TransactionId);
                    break;

                case "START":
                    //S2F42
                    SECsMessage PP_SELECTreply1 = ServiceManager.CustomMessage.CreateMessage(2, 42, false);
                    cmdS2F42evt.Reset();
                    strS2F42Reststring = "1";
                    cmdS2F41evt.Set();

                    while (!cmdS2F42evt.WaitOne(100))
                    {
                        if (terminate == true)
                        {
                            break;
                        }
                    }
                    PP_SELECTreply1.DataItem.AddList(); // This function will add a LIST data type to the
                    // collection
                    // Add OperatorBatchId item to the root LIST collection
                    PP_SELECTreply1.DataItem[0].Add("reply_pp-select", strS2F42Reststring, SECsFormat.Ascii);

                    this.gemController.SendReply(PP_SELECTreply1, args.TransactionId);
                    break;

                case "LotTrackIn":	// LOT_TRACK_IN
                    CommandParameterCollection invalidParameters = new CommandParameterCollection();
                    //Logger("Command: " + args.CommandLogicalName);
                    //Logger("Param LotId: " + args.CommandParameters["LotId"].value);
                    //Logger("Param Quantity: " + args.CommandParameters["Quantity"].value);

                    if (Convert.ToInt32(args.CommandParameters["Quantity"].value) > 1000)
                    {
                        args.CommandParameters["Quantity"].ack = 2;
                        invalidParameters.Add(args.CommandParameters["Quantity"]);
                    }
                    SECsMessage reply = ServiceManager.ControlDiagnostic.HostCommandAcknowledge(args.CommandLogicalName, 0, invalidParameters);

                    this.gemController.SendReply(reply, args.TransactionId);

                    SECsMessage message = ServiceManager.DataCollection.EventReportSend("EquipmentOFFLINE");
                    gemController.SendAndWait(message);
                    break;
            }
        }

        private void OnS2F49EnhancedRemoteCommand(SECsRemoteCommandEventArgs args)
        {
            SECsMessage reply;
            CommandParameterCollection invalidParameters = new CommandParameterCollection();
            switch (args.CommandLogicalName)
            {
                case "PP-SELECT":
                    //Logger("Enhanced Command: " + args.CommandLogicalName);
                    //Logger("Param PPID: " + args.CommandParameters["PPID"].value);

                    reply = ServiceManager.ControlDiagnostic.EnhancedRemoteCommandAcknowledge(args.CommandLogicalName, 0, invalidParameters);

                    this.gemController.SendReply(reply, args.TransactionId);
                    break;

                case "START":
                    reply = ServiceManager.ControlDiagnostic.EnhancedRemoteCommandAcknowledge(args.CommandLogicalName, 0, invalidParameters);
                    this.gemController.SendReply(reply, args.TransactionId);

                    break;

                case "STOP":
                    reply = ServiceManager.ControlDiagnostic.EnhancedRemoteCommandAcknowledge(args.CommandLogicalName, 0, invalidParameters);
                    this.gemController.SendReply(reply, args.TransactionId);

                    break;
            }
        }

        /// <summary>
        /// This sample shows how to intercept the auto handler stream by SecsToHost.Net
        /// You can perform any action by the requested SVIDs before SecsToHost.Net replies Host.
        /// </summary>
        /// <param name="e"></param>
        private void OnStatusVariableRequest(SECsPrimaryInEventArgs e)
        {
            // Requesting all SVIDs
            if (e.Inputs.DataItem[0].Count == 0)
            {
                // TO DO: Update all the status variable value
            }
            else // Request some specific SVIDs
            {
                for (int i = 0; i < e.Inputs.DataItem[0].Count; i++)
                {
                    string logicalName = e.Inputs.DataItem[0][i].Name;

                    // You can call SetAttribute method to update the SVID value manually.
                }
            }
        }

        /// <summary>
        /// Handle the equipment constant value change request by Host
        /// </summary>
        /// <param name="e"></param>
        private void OnEquipmentConstantChangeRequest(SECsPrimaryInEventArgs e)
        {
            for (int i = 0; i < e.Inputs.DataItem["Ln"].Count; i++)
            {
                string ecname = e.Inputs.DataItem["Ln"][i][0].Name;
                string ecval = e.Inputs.DataItem["Ln"][i][1].ToString();

                // Update the value of the request EC
                // No longer need to call this command explicitly for this purpose.

                //gemController.SetAttribute(ecname, AttributeType.EC, ecval);
            }

            // Set reply to acknowledge

            // No longer need to call this command. Handled in gemController layer.

            // SECsMessage reply = ServiceManager.ControlDiagnostic.NewEquipmentConstantAcknowledge(0);
            // gemController.SendReply(reply, e.TransactionID);
        }

        private void OnTerminalDisplay(SECsPrimaryInEventArgs e)
        {
            string terminalMessage = e.Inputs.DataItem["Ln"]["TEXT"].ToString();

            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                this.ErrorDisplayMsg1 = terminalMessage;
                if (DisplayMsg.Count == 20)
                {
                    DisplayMsg.RemoveAt(19);
                    DisplayMsg.Insert(0, new CMsgClass() { time = DateTime.Now.ToString(), Level = "INFO", Msg = terminalMessage });
                    ErrorDisplayMsg = terminalMessage;
                }
                else
                {
                    DisplayMsg.Insert(0, new CMsgClass() { time = DateTime.Now.ToString(), Level = "INFO", Msg = terminalMessage });
                    ErrorDisplayMsg = terminalMessage;
                }
            });

            // acknowledge Host
            SECsMessage reply = ServiceManager.TerminalDisplay.TerminalDisplayAcknowledge(0);
            gemController.SendReply(reply, e.TransactionID);

            //MessageBox.Show(terminalMessage, "Terminal Message from Host");
        }

        #endregion Request from Host handler

        #region Custom Message Handlers

        /// <summary>
        /// Handle the custom message reply or acknowledgement
        /// </summary>
        /// <param name="e"></param>
        private void OnCustomMessageReply(SECsSecondaryInEventArgs e)
        {
            string SF = "S" + e.Outputs.Stream.ToString() + "F" + e.Outputs.Function.ToString();

            switch (SF)
            {
                case "S64F102":
                   
                    HandleS64F102(e.Outputs);
                    break;

                case "S14F2"://custom message recived for traymap
                   
                    HandleS14F2(e.Outputs);
                    break;
  
            }
        }

        private void HandleS14F2(SECsMessage sECsMessage)//decipher traymap
        {
            string rst = sECsMessage.DataItem[0][1][0].Value.ToString();
            if (rst == "0")
            {
                log.Debug("MapData Recieved Successfully");
                string substrateId = sECsMessage.DataItem[0][0][0][0].Value.ToString();
                log.Debug("Substrate ID " + substrateId);
                string substrateType = sECsMessage.DataItem[0][0][0][1][0][0].Value.ToString();
                log.Debug("Substrate Type " + substrateType);
                string substrateMap = sECsMessage.DataItem[0][0][0][1][0][1].Value.ToString();
                log.Debug("MapData :" + substrateMap);
                mapData = (MapData)XMLConverter.FromXml(substrateMap, typeof(MapData));
                foreach (MapDataLayout lo in mapData.Layouts)
                {
                    if (lo.ChildLayouts != null)
                    {
                        if (lo.ChildLayouts.ChildLayouts != null)
                        {
                            lo.ChildLayouts.ChildLayout = lo.ChildLayouts.ChildLayouts;
                            lo.ChildLayouts.ChildLayouts = null;
                        }
                    }
                }
                log.Debug("MapData Recieved Complete" );
            }
            else
            {
                mapData = null;
                log.Debug("MapData is Null");
            }
            cmdS14F2evt.Set();//S14F2 replied
            log.Debug("S14F2 Event Rx");
        }

        /// <summary>
        /// Handle the custom message reply or acknowledgement
        /// </summary>
        /// <param name="e"></param>
        private void OnCustomMessageRequest(SECsPrimaryInEventArgs e)//dont think its in used
        {
            string SF = "S" + e.Inputs.Stream.ToString() + "F" + e.Inputs.Function.ToString();

            switch (SF)
            {
                case "S101F1":
                    HandleS101F1(e.Inputs, e.TransactionID);
                    break;

                case "S12F101": // Array handling demo
                    HandleS12F101(e.Inputs, e.TransactionID);
                    break;

                case "S1F99": // Demo purpose for specific function
                    gemController.UnrecognizedFunction(e.Inputs.Header);
                    break;

                case "S99F1": // Demo purpose for specific stream
                    gemController.UnrecognizedStream(e.Inputs.Header);
                    break;

                case "S3F17":
                    HandleS3F17(e.Inputs, e.TransactionID);
                    break;

                case "S10F3":
                    HandleS10F3(e.Inputs, e.TransactionID);
                    break;

                case "S10F5":
                    HandleS10F5(e.Inputs, e.TransactionID);
                    break;

                default: // Just trigger either unrecognize stream/function
                    gemController.UnrecognizedStream(e.Inputs.Header);
                    break;
            }
        }

        private void HandleS10F3(SECsMessage input, int transactionId)
        {
            string str1 = "";
            //string str1 = input.DataItem[0][1][0].Value.ToString();
            //string str2 = input.DataItem[0][1][1].Value.ToString();
            for (int i = 0; i < input.DataItem[0][1].Count; i++)
            {
                str1 = str1 + input.DataItem[0][1].Value.ToString() + @"\n";
            }
            //set event that theres an error
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                if (DisplayMsg.Count == 20)
                {
                    DisplayMsg.RemoveAt(19);
                    DisplayMsg.Insert(0, new CMsgClass() { time = DateTime.Now.ToString(), Level = "INFO", Msg = str1 });
                    ErrorDisplayMsg = str1;
                }
                else
                {
                    DisplayMsg.Insert(0, new CMsgClass() { time = DateTime.Now.ToString(), Level = "INFO", Msg = str1 });
                    ErrorDisplayMsg = str1;
                }
            });
            // acknowledge Host
            SECsMessage reply = ServiceManager.CustomMessage.CreateMessage(10, 4, false);
            reply.DataItem.AddList();
            reply.DataItem[0].Add("reply", "0", SECsFormat.U1);
            reply.DataItem[0].AddList();
            gemController.SendReply(reply, transactionId);
        }

        private void HandleS10F5(SECsMessage input, int transactionId)
        {
            string str1 = "";
            //string str1 = input.DataItem[0][1][0].Value.ToString();
            //string str2 = input.DataItem[0][1][1].Value.ToString();
            for (int i = 0; i < input.DataItem[0][1].Count; i++)
            {
                str1 = str1 + input.DataItem[0][1][i].Value.ToString() + System.Environment.NewLine;
            }
            //set event that theres an error
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                ErrorDisplayMsg1 = str1;
                if (DisplayMsg.Count == 20)
                {
                    DisplayMsg.RemoveAt(19);
                    DisplayMsg.Insert(0, new CMsgClass() { time = DateTime.Now.ToString(), Level = "INFO", Msg = str1 });
                    ErrorDisplayMsg = str1;
                }
                else
                {
                    DisplayMsg.Insert(0, new CMsgClass() { time = DateTime.Now.ToString(), Level = "INFO", Msg = str1 });
                    ErrorDisplayMsg = str1;
                }
            });
            SECsMessage reply = ServiceManager.CustomMessage.CreateMessage(10, 6, false);
            reply.DataItem.AddList();
            reply.DataItem[0].Add("reply", "0", SECsFormat.U1);
            reply.DataItem[0].AddList();
            gemController.SendReply(reply, transactionId);
            cmdS10F5evt.Set();
        }

        private void HandleS3F17(SECsMessage input, int transactionId)
        {
            if (input.NeedReply)
            {   //ProceedWithCarrier msg
                // Prepare the reply message;
                strS3F17SendString = "";
                strS3F17SendString = input.DataItem[0][1].Value.ToString();
                cmdS3F17evt.Set();
                SECsMessage reply = ServiceManager.CustomMessage.CreateMessage(3, 18, false);
                reply.DataItem.AddList();
                reply.DataItem[0].Add("reply", "0", SECsFormat.U1);
                reply.DataItem[0].AddList();
                gemController.SendReply(reply, transactionId);
                //set event that transaction taken
            }
        }

        /// <summary>
        /// Demonstrate how to send custom SECS message to the equipment
        /// The following example send S64F101 with the following structure:
        /// <L [5]
        ///     <A 'B1002'> OperatorBatchId
        ///     <A 'Dave Hunter'> OperatorName
        ///     <U4 3001> OperationId
        ///     <L [3]
        ///          <F4 1.02> Temperature
        ///          <U4 242> ActionId
        ///          <A  'Replenish Material'> Action Description
        ///     >
        ///     <L [2]
        ///          <B 0x01> Machine status flag
        ///          <A  'Machine is operational'> Machine status description
        ///     >
        /// >
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendS64F101_Click(object sender, System.EventArgs e)
        {
            SECsMessage customMessage = ServiceManager.CustomMessage.CreateMessage(64, 101, true);

            customMessage.DataItem.AddList(); // This function will add a LIST data type to the
            // collection

            // Add OperatorBatchId item to the root LIST collection
            customMessage.DataItem[0].Add("OperatorBatchId", "B1002", SECsFormat.Ascii);

            // Add OperatorBatchId item to the root LIST collection
            customMessage.DataItem[0].Add("OperatorName", "Dave Hunter", SECsFormat.Ascii);

            // Add OperationId to the root LIST collection
            customMessage.DataItem[0].Add("OperationId", 3001, SECsFormat.U4);
            customMessage.DataItem[0][2].IsArray = false;

            // Add LIST item to the root LIST collection;
            customMessage.DataItem[0].AddList();

            // Add the Temperature
            customMessage.DataItem[0][3].Add("Temperature", 1.02, SECsFormat.F4);

            // Add the Action Id
            customMessage.DataItem[0][3].Add("ActionId", "242", SECsFormat.U4);

            // Add the Action Description
            customMessage.DataItem[0][3].Add("ActionDescription", "Replenish Material",
            SECsFormat.Ascii);

            // Add another LIST item to the root LIST collection;
            customMessage.DataItem[0].AddList();

            // Add the Machine status flag
            customMessage.DataItem[0][4].Add("MachineStatus", "1", SECsFormat.Binary);

            // Add the Machine status description
            customMessage.DataItem[0][4].Add("MachineStatusDescription", "Machine is operational",
            SECsFormat.Ascii);

            // Send message S64F101 to equipment
            gemController.Send(customMessage);
        }

        /// <summary>
        /// This function demonstrate how to handle the Custom SECS messages initiated by equipment (e.g.: custom event)
        /// The following message structure is expected in this example:
        /// <L [3]\n");
        ///      <U4 100> Custom Event Id
        ///      <A 'Machine Offline'> Event Desc
        ///      <L [2]
        ///          <B 0x00> Machine status flag
        ///          <L [1]
        ///              <I2 10> Some value
        ///          >
        ///      >
        /// >
        ///
        /// Host will reply the following structure upon receiving S101F1:
        /// <B 0x00> Acknowledge OK
        /// <L [1]
        ///      <A 'Host Acknowledge OK'>
        /// >
        /// </summary>
        /// <param name="input"></param>
        private void HandleS101F1(SECsMessage input, int transactionId)
        {
            // Get Custom Event Id
            string eventId = input.DataItem[0][0].Value.ToString();

            // Get event description
            string eventDesc = input.DataItem[0][1].Value.ToString();

            // Get Machine Flag
            string machineFlag = input.DataItem[0][2][0].Value.ToString();

            // Get Some other value
            string otherValue = input.DataItem[0][2][1][0].Value.ToString();

            if (input.NeedReply)
            {
                // Prepare the reply message;
                SECsMessage reply = ServiceManager.CustomMessage.CreateMessage(101, 2, false);

                reply.DataItem.AddList();

                reply.DataItem[0].Add("ACK", machineFlag, SECsFormat.Binary);

                reply.DataItem[0].AddList();
                reply.DataItem[0][1].Add("Description", "Host Acknowledge OK", SECsFormat.Ascii);

                // Reply S101F2 to equipment with the reference of TransactionId from the primary message.
                gemController.SendReply(reply, transactionId);
                // Logger("Received S101F1 from Host and Equipment replied with S101F2");
            }
        }

        /// <summary>
        /// This function the reply sent by equipment.
        /// This example expect the reply of S64F102 in the following structure
        /// <L [2]
        ///     <B 0x01> Acknowledgement code
        ///     <L [2]>
        ///          <U4 101> Error Code
        ///          <A 'No Material Loaded'> Error Desc
        ///     >
        /// >
        /// </summary>
        /// <param name="output"></param>
        private void HandleS64F102(SECsMessage output)
        {
            if (output != null)
            {
                // Get the acknowledgement code
                int ack = output.DataItem[0][0].ToInt();

                if (ack != 0)
                { // Meaning there is an error
                    // Get the error code
                    string errorCode = output.DataItem[0][1][0].Value.ToString();
                    string errorDesc = output.DataItem[0][1][1].Value.ToString();

                    // Logger("S64F102, Host replies: " + ack + ". Error code: " + errorCode + " Error Desc: " + errorDesc);
                }
                else
                {
                    // Logger("S64F102, Host replies: " + ack);
                }
            }
        }

        private void btnReceivingS101F1_Click(object sender, System.EventArgs e)
        {
            // MessageBox.Show("Use your simulator or SWIFT Machine Emulator to trigger this event");
        }

        /// <summary>
        /// This function demonstrate the use of Array within SecsToHost.Net.
        /// The following formats define the mapping between SECS and .NET
        /// I1		: int[]
        /// I2		: Int16[]
        /// I4		: Int32[]
        /// I8		: Int64[]
        /// U1		: uint[]
        /// U2		: UInt16[]
        /// U4		: UInt32[]
        /// U8		: UInt64[]
        /// F4		: Single[]
        /// F8		: Double[]
        /// Boolean	: bool[]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendS12F99Array_Click(object sender, System.EventArgs e)
        {
            SECsMessage arrayMessage = ServiceManager.CustomMessage.CreateMessage(12, 99, true);

            arrayMessage.DataItem.AddList(); // This function will add a LIST data type to the
            // collection

            arrayMessage.DataItem[0].Add("Item_Ascii", null, SECsFormat.Ascii);

            arrayMessage.DataItem[0].Add("Item_I1", new int[] { 11, 12, 13 }, SECsFormat.I1);
            arrayMessage.DataItem[0]["Item_I1"].IsArray = true;

            arrayMessage.DataItem[0].Add("Item_I2", new Int16[] { -12, 13, -14 }, SECsFormat.I2);
            arrayMessage.DataItem[0]["Item_I2"].IsArray = true;

            arrayMessage.DataItem[0].Add("Item_I4", new Int32[] { 14, -15, 16 }, SECsFormat.I4);
            arrayMessage.DataItem[0]["Item_I4"].IsArray = true;

            arrayMessage.DataItem[0].Add("Item_I8", new Int64[] { -18, -19, 20 }, SECsFormat.I8);
            arrayMessage.DataItem[0]["Item_I8"].IsArray = true;

            arrayMessage.DataItem[0].Add("Item_U1", new uint[] { 21, 22, 23 }, SECsFormat.U1);
            arrayMessage.DataItem[0]["Item_U1"].IsArray = true;

            arrayMessage.DataItem[0].Add("Item_U2", new UInt16[] { 22, 23, 24 }, SECsFormat.U2);
            arrayMessage.DataItem[0]["Item_U2"].IsArray = true;

            arrayMessage.DataItem[0].Add("Item_U4", new UInt32[] { 24, 25, 26 }, SECsFormat.U4);
            arrayMessage.DataItem[0]["Item_U4"].IsArray = true;

            arrayMessage.DataItem[0].Add("Item_U8", new UInt64[] { 323232328, 29, 30 }, SECsFormat.U8);
            arrayMessage.DataItem[0]["Item_U8"].IsArray = true;

            arrayMessage.DataItem[0].Add("Item_F4", new Single[] { 44.21f, 45.22f, 46.23f }, SECsFormat.F4);
            arrayMessage.DataItem[0]["Item_F4"].IsArray = true;

            arrayMessage.DataItem[0].Add("Item_F8", new Double[] { 48234343424.112321, 49.44, 50.66 }, SECsFormat.F8);
            arrayMessage.DataItem[0]["Item_F8"].IsArray = true;

            arrayMessage.DataItem[0].Add("Item_Boolean", new bool[] { true, false, true }, SECsFormat.Boolean);
            arrayMessage.DataItem[0]["Item_Boolean"].IsArray = true;

            // Send message S12F99 to equipment
            gemController.Send(arrayMessage);
        }

        /// <summary>
        /// This function demonstrate how to receive and handle Array data item sent by Host
        /// The following formats define the mapping between SECS and .NET
        /// I1		: int[]
        /// I2		: Int16[]
        /// I4		: Int32[]
        /// I8		: Int64[]
        /// U1		: uint[]
        /// U2		: UInt16[]
        /// U4		: UInt32[]
        /// U8		: UInt64[]
        /// F4		: Single[]
        /// F8		: Double[]
        /// Boolean	: bool[]
        /// </summary>
        /// <param name="output"></param>
        private void HandleS12F101(SECsMessage input, int transactionId)
        {
            if (input != null)
            {
                byte mybyte = input.DataItem[0].ToByte();	// Binary
                bool[] arrayBool = input.DataItem[1].ToArrayBoolean();	// Boolean
                string str = input.DataItem[2].ToString();	// Ascii

                Int64[] arrayInt64 = input.DataItem[3].ToArrayInt64();	// I8
                Int32[] arrayInt32 = input.DataItem[4].ToArrayInt32(); // I4
                Int16[] arrayInt16 = input.DataItem[5].ToArrayInt16(); // I2
                int[] arrayInt = input.DataItem[6].ToArrayInt(); // I1

                Double[] arrayDouble = input.DataItem[7].ToArrayDouble();	// F8
                Single[] arraySingle = input.DataItem[8].ToArraySingle();	// F4

                UInt64[] arrayUInt64 = input.DataItem[9].ToArrayUInt64();	// U8
                UInt32[] arrayUInt32 = input.DataItem[10].ToArrayUInt32(); // U4
                UInt16[] arrayUInt16 = input.DataItem[11].ToArrayUInt16(); // U2
                uint[] arrayUInt = input.DataItem[12].ToArrayUInt(); // U1
            }
        }

        /// <summary>
        /// This function demontrate how to send wafer map using the CustomMessage service
        /// also shows how to use array.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendS12F9_Click(object sender, System.EventArgs e)
        {
            SECsMessage waferMapUpload = ServiceManager.CustomMessage.CreateMessage(12, 9, true);

            waferMapUpload.DataItem.AddList();
            waferMapUpload.DataItem[0].Add("MID", "Wafer-01", SECsFormat.Ascii);
            waferMapUpload.DataItem[0].Add("IDTYPE", 2, SECsFormat.Binary);
            waferMapUpload.DataItem[0].Add("STRP", 0, SECsFormat.U2);

            waferMapUpload.DataItem[0].Add("BINLT");
            waferMapUpload.DataItem[0]["BINLT"].IsArray = true;
            waferMapUpload.DataItem[0]["BINLT"].Format = SECsFormat.U2;

            // Let's assume the wafer map is 4x6 with PRAXI=0
            // 1 0 0 0 0 1
            // 0 0 0 0 0 0
            // 0 0 0 0 0 0
            // 1 0 0 0 0 1

            UInt16[] maps = new UInt16[24] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
            waferMapUpload.DataItem[0]["BINLT"].Value = maps;

            gemController.Send(waferMapUpload);
        }

        #endregion Custom Message Handlers

        #region Alarm Management Tab

        /// <summary>
        /// Initialize and Populate the alarm dropdown list
        /// </summary>
        private void InitializeAlarmManagement()
        {
            AlarmList = new ObservableCollection<string>();
            for (int i = 0; i < gemController.EquipmentModel.Alarms.Count; i++)
            {
                ALID alarm = gemController.EquipmentModel.Alarms[i];
                this.AlarmList.Add(alarm.logicalName);
            }
        }

        /// <summary>
        /// SET the selected Alarm. In the equipment application, just pass in the alarm logical name to set
        /// and GEM Controller will be taking care in mapping it to the actual alarm ID.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetAlarm(string alarmLogicalName)
        {
            try
            {
                // Set alarm
                if (!SetAlarmList.Contains(alarmLogicalName))
                {
                    gemController.SetAlarm(alarmLogicalName);
                    SetAlarmList.Add((string)alarmLogicalName);
                }
                //PopulateAlarmsSet();
            }
            catch (Exception ex)
            {
                log.Debug("Alarm Set Error" + ex.ToString());
            }
        }

        /// <summary>
        /// CLEAR the selected Alarm. In the equipment application, just pass in the alarm logical name to clear
        /// and GEM Controller will be taking care in mapping it to the actual alarm ID.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ClearAlarm(string alarmLogicalName)
        {
            // Set alarm
            if (SetAlarmList.Contains(alarmLogicalName))
            {
                gemController.ClearAlarm(alarmLogicalName);
                SetAlarmList.Remove(alarmLogicalName);
            }
            //PopulateAlarmsSet();//manually clear
        }

        public void ClearAllAlarm()
        {
            // Set alarm
            try
            {
                foreach (string str in SetAlarmList)
                {
                    gemController.ClearAlarm(str);
                }
            }
            catch (Exception ex)
            {
                log.Debug("Alarm Clear Error" + ex.ToString());
            }
            //PopulateAlarmsSet();//manually clear
        }
        /// <summary>
        /// Populate the alarm set dropdown list
        /// </summary>
        private void PopulateAlarmsSet()
        {

            ArrayList alarmsSet = gemController.GetAllSetAlarms();
            //this.cboClearAlarm.Items.Clear();
            SetAlarmList.Clear();
            for (int i = 0; i < alarmsSet.Count; i++)
            {
                this.SetAlarmList.Add((string)alarmsSet[i]);
            }
        }

        #endregion Alarm Management Tab

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public bool terminate { get; set; }

        internal void ClearDisplayMsg()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            {
                {
                    OPgemDisplayVisible = "Hidden";
                    DispgemDisplayVisible = "Visible";
                    MachineMsg = "";
                    ErrorDisplayMsg1 = "";
                }
            });
        }


        //internal void SetAlarm(string p)
        //{
        //    gemController.SetAlarm(p);
        //    //throw new NotImplementedException();
        //}
    }

    // Summary:
    public enum GemEquipmentState
    {
        Production = 1,
        Standby = 2,
        Engineering = 3,
        DowntimeSchedule = 4,
        DowntimeUnSchedule = 5,
        NonScheduledTime = 6
    }

    //     The behavior of the load port transfer state0

    public enum LoadPortTxferState
    {
        OutOfService = 1,
        InService = 2,
        TransferReady = 3,
        TransferBlocked = 4,
        ReadyToLoad = 5,
        ReadyToUnload = 6,
    }

    // Summary:
    //     The behavior of the load port transfer state0

    public enum LoadPortAssocState
    {
        NotAssociated = 1,
        Associated = 2,
    }

    public enum LoadPortReservState
    {
        NotReserved = 1,
        Reserved = 2,
    }

    public enum LoadPortAccessMode
    {
        Manual = 1,
        Auto = 2,
    }

    public enum CarrierIDStatus
    {
        IdNotRead = 1,
        WaitingForHost = 2,
        IdVerificationOK = 3,
        IdVerificationFailed = 4,
    }

    public enum CarrierSlotMapStatus
    {
        SlotMapNotRead = 1,
        WaitingForHost = 2,
        SlotMapVerificationOK = 3,
        SlotMapVerificationFailed = 4,
    }

    public enum CarrierAccessingStatus
    {
        NotAccessed = 1,
        InAccess = 2,
        CarrierComplete = 3,
        CarrierStopped = 4,
    }
}