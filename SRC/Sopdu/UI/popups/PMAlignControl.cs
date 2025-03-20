using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sopdu.UI.popups
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class PMAlignControl : Cognex.VisionPro.PMAlign.CogPMAlignEditV2
    {
        public PMAlignControl()
        {
            InitializeComponent();
        }

        public void SetTabVisibility()
        {
            this.tabControl.TabPages.Remove(this.tabControl.TabPages[0]);
            this.tabControl.TabPages.Remove(this.tabControl.TabPages[0]);
            this.tabControl.TabPages.Remove(this.tabControl.TabPages[0]);
            this.tabControl.TabPages.Remove(this.tabControl.TabPages[1]);
            //this.CurrentRecord.SubRecords.Remove("TrainImage");

            this.mCogRecordsDisplay.Hide();
        }

        public TabPage GetTabPage(int i)
        {
            return this.tabControl.TabPages[i];
        }

        internal Cognex.VisionPro.ICogRecord GetResultRecord()
        {
            return LastRunRecord.SubRecords["InputImage"];
            //throw new NotImplementedException();
        }

        internal Cognex.VisionPro.ICogRecord GetInputRecord()
        {
            return LastRunRecord.SubRecords["InputImage"];
        }

        internal StatusBar GetBar()
        {
            return this.sbrStatus;
        }
    }
}

//this.tbbHelp.Visible = false;
//this.tbbOpen.Visible = false;
//this.tbbReset.Visible = false;
//this.tbbSave.Visible = false;
//this.tbbSaveAs.Visible = false;
////  this.tabControl.TabPages[0].Hide();
////this.tbbRun.Visible = false;