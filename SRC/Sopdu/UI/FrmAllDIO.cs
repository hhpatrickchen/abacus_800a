using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyLib;

namespace Sopdu.UI
{
    public partial class FrmAllDIO : Form
    {
        public FrmAllDIO()
        {
            InitializeComponent();
        }

        private void FrmAllDIO_Load(object sender, EventArgs e)
        {
            CenterToParent();

            InitDIOCheckBox();

            timer1.Start();
        }
        private void FrmAllDIO_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
        private void FrmAllDIO_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Stop();
        }
        private void FrmAllDIO_SizeChanged(object sender, EventArgs e)
        {
            //gbAllDIList.Width = this.Width / 2 - 20;
            //gbAllDIList.Height = this.Height - 120;
            //gbAllDOList.Width = this.Width / 2 - 20;
            //gbAllDOList.Height = this.Height - 120;

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdataDIOCheckBox();

            GUI.SetButton(RunTimeData.isLinkIO, ledCnntModbus1, Color.LawnGreen, Color.Red);
            GUI.SetButton(RunTimeData.isLinkOP, ledCnntModbus2, Color.LawnGreen, Color.Red);
            tbIPAddrModbus1.Text = RunTimeData.sIPAddrIO;
            tbIPAddrModbus2.Text = RunTimeData.sIPAddrOP;
        }
        private void clbAllDOList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            clbAllDOList.Enabled = false;
            short chNo = (short)e.Index;
            bool isOn;
            if (e.NewValue == CheckState.Checked)
                isOn = true;
            else
                isOn = false;

            GlobalVar.lstAllDO[chNo].Logic = isOn;
            clbAllDOList.Enabled = true;
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #region // Variable definition and property

        #endregion

        #region // Method and Function
        private void InitDIOCheckBox()
        {
            string sName = "";
            bool isOn = false;
            for (int i = 0; i < GlobalVar.lstAllDI.Count; i++)
            {
                sName = GlobalVar.lstAllDI[i].ShowID + "-" + GlobalVar.lstAllDI[i].DisplayName;
                isOn = GlobalVar.lstAllDI[i].Logic;
                clbAllDIList.Items.Add(sName, isOn);
            }

            for (int i = 0; i < GlobalVar.lstAllDO.Count; i++)
            {
                sName = GlobalVar.lstAllDO[i].ShowID + "-" + GlobalVar.lstAllDO[i].DisplayName;
                isOn = GlobalVar.lstAllDO[i].Logic;
                clbAllDOList.Items.Add(sName, isOn);
            }
        }
        private void UpdataDIOCheckBox()
        {
            bool isOn = false;
            if (clbAllDIList != null && clbAllDIList.Items.Count == GlobalVar.lstAllDI.Count)
            {
                for (int i = 0; i < GlobalVar.lstAllDI.Count; i++)
                {
                    isOn = GlobalVar.lstAllDI[i].Logic;
                    if (isOn)
                        clbAllDIList.SetItemCheckState(i, CheckState.Checked);
                    else
                        clbAllDIList.SetItemCheckState(i, CheckState.Unchecked);
                }
            }

            if (clbAllDOList != null && clbAllDOList.Items.Count == GlobalVar.lstAllDO.Count)
            {
                for (int i = 0; i < GlobalVar.lstAllDO.Count; i++)
                {
                    isOn = GlobalVar.lstAllDO[i].Logic;
                    if (isOn)
                        clbAllDOList.SetItemCheckState(i, CheckState.Checked);
                    else
                        clbAllDOList.SetItemCheckState(i, CheckState.Unchecked);
                }
            }
        }
        #endregion

    }
}
