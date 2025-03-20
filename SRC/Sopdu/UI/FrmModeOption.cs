using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sopdu.UI
{
    public partial class FrmModeOption : Form
    {
        public FrmModeOption()
        {
            InitializeComponent();
        }

        private void FrmModeOption_Load(object sender, EventArgs e)
        {
            CenterToParent();

            //ConfigcbbModeIP();
            ConfigclbIPMode();
            ConfigcbbModeOP();

            timer1.Start();
            numFailRate.Value = GlobalVar.iFailRate;
            checkBox1.Checked = GlobalVar.isCheckTrayGapV2;
            numericUpDown1.Value = (int)GlobalVar.ShutterSaveDistance;
        }
        private void FrmModeOption_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
        private void FrmModeOption_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Stop();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetItemStatus();

            SetLedStatus();

            tbMessage.Text = sMsg;
        }
        private void cbbModeIP_Click(object sender, EventArgs e)
        {
            if (!isIPModeSetAllow)
            {
                cbbModeIP.Text = GlobalVar.eIPMode.ToString();
                return;
            }                
        }
        private void cbbModeIP_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void cbbModeIP_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (!isIPModeSetAllow)
            {
                cbbModeIP.Text = GlobalVar.eIPMode.ToString();
                //MessageBox.Show("Input Conveyor Loading Mode Changes not Allowed!");
                sMsg = "Input Conveyor Loading Mode Changes not Allowed!";
                return;
            }
        }
        private void cbbModeOP_Click(object sender, EventArgs e)
        {
            if (!isOPModeSetAllow)
            {
                cbbModeOP.Text = GlobalVar.eOPMode.ToString();
                return;
            }                
        }
        private void cbbModeOP_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void cbbModeOP_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (!isOPModeSetAllow)
            {
                cbbModeOP.Text = GlobalVar.eOPMode.ToString();
                //MessageBox.Show("Output Conveyor Unloading Mode Changes not Allowed!");
                sMsg = "Output Conveyor Unloading Mode Changes not Allowed!";
                return;
            }
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            if (CheckclbStates())
            {
                GlobalVar.lstIPMode.Clear();
                for (int i = 0; i < clbIPMode.Items.Count; i++)
                {
                    if (clbIPMode.GetItemChecked(i))
                    {
                        string sName = clbIPMode.Items[i].ToString();
                        GlobalVar.lstIPMode.Add(sName);
                    }
                }

                string sVal = string.Empty;                
                //GlobalVar.eIPMode = (EIPMode)Enum.Parse(typeof(EIPMode), sVal);
                sVal = cbbModeOP.Text;
                GlobalVar.eOPMode = (EOPMode)Enum.Parse(typeof(EOPMode), sVal);
                GlobalVar.SaveSettings();
                //SaveSettings();
                DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                sMsg = "Input Conveyor Loading Mode not Selected!";
                DialogResult = DialogResult.None;
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #region // Variable definition and property
        private bool isIPModeSetAllow { get { return CheckIPModeSetAllow(); } }
        private bool isOPModeSetAllow { get { return CheckOPModeSetAllow(); } }
        private bool isSFARdyIP, isOHTRdyIP, isTrayPresentIPCV;
        private bool isTrayPresentIPStk, isTrayPresentSht1, isTrayPresentSht2, isTrayPresentOPStk;
        private bool isTrayAbsentIPCV, isTrayAbsentIPStk, isTrayAbsentOPStk, isTrayAbsentOPCV;
        private bool isSFARdyOP, isOHTRdyOP, isTrayPresentOPCV;
        private string sMsg = "";

        private void btnsave_Click(object sender, EventArgs e)
        {
            if (numericUpDown1.Value < 42000)
            {
                MessageBox.Show("The safety distance is too small");
                return;
            }

            btnsave.Enabled = false;
            try
            {
                SaveSettings();
                GlobalVar.LoadSettings();
            }
            catch (Exception)
            {

            }

            btnsave.Enabled = true;
        }
        #endregion

        #region // Method and function
        private void ConfigclbIPMode()
        {
            string sName = "";
            bool isOn = false;
            if (clbIPMode != null && clbIPMode.Items != null && clbIPMode.Items.Count > 0)
                clbIPMode.Items.Clear();
            for (int i = 0; i < GlobalVar.aIPModeID.Length; i++)
            {
                sName = GlobalVar.aIPModeID[i];
                if (GlobalVar.lstIPMode.Contains(sName))
                    isOn = true;
                else
                    isOn = false;
                clbIPMode.Items.Add(sName, isOn);
            }
        }
        private void ConfigcbbModeIP()
        {
            string[] aTmp = Enum.GetNames(typeof(EIPMode));
            ConfigCombobox(cbbModeIP, aTmp);

            cbbModeIP.Text = GlobalVar.eIPMode.ToString();
        }
        private void ConfigcbbModeOP()
        {
            string[] aTmp = Enum.GetNames(typeof(EOPMode));
            ConfigCombobox(cbbModeOP, aTmp);

            cbbModeOP.Text = GlobalVar.eOPMode.ToString();
        }
        private void ConfigCombobox(ComboBox cbb, string[] aItems)
        {
            cbb.Items.Clear();
            cbb.Items.AddRange(aItems);
        }
        private bool CheckclbStates()
        {
            bool isOK = false;
            for (int i = 0; i < clbIPMode.Items.Count; i++)
            {
                if (clbIPMode.CheckedItems.Count > 0)
                    isOK = true;
            }
            return isOK;
        }
        private bool CheckIPModeSetAllow()
        {
            isTrayAbsentIPCV = RunTimeData.mainApp.IPCvr.isPartAbsent;
            isTrayAbsentIPStk = RunTimeData.mainApp.IPStkr.isPartAbsent;
            isTrayAbsentOPCV = RunTimeData.mainApp.OPCvr.isPartAbsent;
            isTrayAbsentOPStk = RunTimeData.mainApp.OPStkr.isPartAbsent;

            //isSFARdyIP = RunTimeData.mainApp.IPCvr.isSFATrayRdy;
            //isSFARdyOP = RunTimeData.mainApp.OPCvr.isSFATrayRdy;
            //isOHTRdyIP = RunTimeData.mainApp.IPCvr.isOHTRdy;
            //isOHTRdyOP = RunTimeData.mainApp.OPCvr.isOHTRdy;

            if (isTrayAbsentIPCV && isTrayAbsentIPStk && !isSFARdyIP && !isOHTRdyIP &&
                isTrayAbsentOPCV && isTrayAbsentOPStk)
                return true;
            else
                return false;
        }
        private bool CheckOPModeSetAllow()
        {
            isTrayAbsentIPCV = RunTimeData.mainApp.IPCvr.isPartAbsent;
            isTrayAbsentIPStk = RunTimeData.mainApp.IPStkr.isPartAbsent;
            isTrayAbsentOPCV = RunTimeData.mainApp.OPCvr.isPartAbsent;
            isTrayAbsentOPStk = RunTimeData.mainApp.OPStkr.isPartAbsent;

            if (isTrayAbsentIPCV && isTrayAbsentIPStk &&
                isTrayAbsentOPCV && isTrayAbsentOPStk)
                return true;
            else
                return false;
        }
        private void GetItemStatus()
        {
            isSFARdyIP = RunTimeData.mainApp.IPCvr.isSFATrayRdy;
            isOHTRdyIP = RunTimeData.mainApp.IPCvr.isOHTRdy;
            isTrayPresentIPCV = RunTimeData.mainApp.IPCvr.isPartPresent;

            isTrayPresentIPCV = RunTimeData.mainApp.IPCvr.isPartPresent;
            isTrayPresentIPStk = RunTimeData.mainApp.IPStkr.isPartPresent;
            isTrayPresentOPStk = RunTimeData.mainApp.OPStkr.isPartPresent;
            isTrayPresentOPCV = RunTimeData.mainApp.OPCvr.isPartPresent;

            //isTrayPresentIPStk = RunTimeData.mainApp.ioCtrl.InputList.IOs[21].Logic;
            //isTrayPresentSht1 = RunTimeData.mainApp.ioCtrl.InputList.IOs[29].Logic & RunTimeData.mainApp.ioCtrl.InputList.IOs[30].Logic &
            //                    RunTimeData.mainApp.ioCtrl.InputList.IOs[31].Logic & RunTimeData.mainApp.ioCtrl.InputList.IOs[32].Logic;
            //isTrayPresentSht2 = RunTimeData.mainApp.ioCtrl.InputList.IOs[45].Logic & RunTimeData.mainApp.ioCtrl.InputList.IOs[46].Logic &
            //                    RunTimeData.mainApp.ioCtrl.InputList.IOs[47].Logic & RunTimeData.mainApp.ioCtrl.InputList.IOs[48].Logic;
            //isTrayPresentOPStk = RunTimeData.mainApp.ioCtrl.InputList.IOs[35].Logic & RunTimeData.mainApp.ioCtrl.InputList.IOs[51].Logic;

            //isTrayPresentIPStk = RunTimeData.mainApp.IPStkr.

            isSFARdyOP = RunTimeData.mainApp.OPCvr.isSFATrayRdy;
            isOHTRdyOP = RunTimeData.mainApp.OPCvr.isOHTRdy;
            isTrayPresentOPCV = RunTimeData.mainApp.OPCvr.isPartPresent;
        }
        private void SetLedStatus()
        {
            //isTrayPresentIPCV = RunTimeData.mainApp.IPCvr.isPartPresent;
            //isTrayPresentIPStk = RunTimeData.mainApp.IPStkr.isPartPresent;
            //isTrayPresentOPStk = RunTimeData.mainApp.OPStkr.isPartPresent;
            //isTrayPresentOPCV = RunTimeData.mainApp.OPCvr.isPartPresent;

            //isSFARdyIP = RunTimeData.mainApp.IPCvr.isSFATrayRdy;
            //isSFARdyOP = RunTimeData.mainApp.OPCvr.isSFATrayRdy;
            //isOHTRdyIP = RunTimeData.mainApp.IPCvr.isOHTRdy;
            //isOHTRdyOP = RunTimeData.mainApp.OPCvr.isOHTRdy;

            MyLib.GUI.SetButton(isSFARdyIP, ledSFARdyIP, Color.LawnGreen, Color.LightGray);
            MyLib.GUI.SetButton(isOHTRdyIP, ledOHTRdyIP, Color.LawnGreen, Color.LightGray);
            MyLib.GUI.SetButton(isTrayPresentIPCV, ledPresentIPCV, Color.LawnGreen, Color.LightGray);

            MyLib.GUI.SetButton(isTrayPresentIPStk, ledPresentIPSTK, Color.LawnGreen, Color.LightGray);
            MyLib.GUI.SetButton(isTrayPresentSht1, ledPresentSHT1, Color.LawnGreen, Color.LightGray);
            MyLib.GUI.SetButton(isTrayPresentSht2, ledPresentSHT2, Color.LawnGreen, Color.LightGray);
            MyLib.GUI.SetButton(isTrayPresentOPStk, ledPresentOPSTK, Color.LawnGreen, Color.LightGray);

            MyLib.GUI.SetButton(isSFARdyOP, ledSFARdyOP, Color.LawnGreen, Color.LightGray);
            MyLib.GUI.SetButton(isOHTRdyOP, ledOHTRdyOP, Color.LawnGreen, Color.LightGray);
            MyLib.GUI.SetButton(isTrayPresentOPCV, ledPresentOPCV, Color.LawnGreen, Color.LightGray);
        }
        private void SaveSettings()
        {
            MyLib.IniFile iniFile = new MyLib.IniFile();
            string sFileName = "SetXferMode.ini";
            string sSection = GlobalVar.sSection;
            string sKey = GlobalVar.SKey.sIPMode;
            string sVal = GlobalVar.eIPMode.ToString();
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);

            sKey = GlobalVar.SKey.sOPMode;
            sVal = GlobalVar.eOPMode.ToString();
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);

            sKey = GlobalVar.SKey.dFailRate;
            sVal = numFailRate.Value.ToString();
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);

            sKey = GlobalVar.SKey.ShutterSaveDistance;
            sVal = numericUpDown1.Value.ToString();
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);

            sKey = GlobalVar.SKey.enableLongSideTrayGap;
            sVal = checkBox1.Checked ? "True":"False";
            iniFile.WriteValue(sFileName, sSection, sKey, sVal);
        }
        #endregion

    }
}
