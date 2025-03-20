using Cognex.VisionPro;
using Cognex.VisionPro.PMAlign;
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
    public partial class frmPMAlign : Form
    {
        public frmPMAlign()
        {
            InitializeComponent();
            // this.cogPMAlignEditV21.
        }

        public frmPMAlign(CogPMAlignTool subject)
        {
            InitializeComponent();
            pmAlignControl1 = new popups.PMAlignControl();
            this.pmAlignControl1.Subject = subject;// current image expected to be in.
            //this.pmAlignControl1.SetTabVisibility();
            tabControl1.TabPages.Add(pmAlignControl1.GetTabPage(3));
            tabControl1.TabPages.Add(pmAlignControl1.GetTabPage(5));
            //   tabControl1.TabPages.Add(pmAlignControl1.GetTabPage(6));
            subject.Ran += subject_Ran;
            // subject.SearchRegion = new CogRectangle();
            subject.SearchRegion.SelectedSpaceName = "#";
            ICogRecord rec = subject.CreateCurrentRecord();
            cogRecordDisplay1.Record = rec.SubRecords["InputImage"];
            this.cogRecordDisplay2.Record = pmAlignControl1.GetResultRecord();

            this.Controls.Add(pmAlignControl1.GetBar());
        }

        private void subject_Ran(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            //pmAlignControl1.GetResultRecord();
        }

        public void RemoveTrain()
        {
            //  this.pmAlignControl1.SetTabVisibility();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pmAlignControl1.Subject.Run();
            this.cogRecordDisplay2.Record = pmAlignControl1.GetResultRecord();
        }
    }
}