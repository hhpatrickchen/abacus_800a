using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;
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
    public partial class frmtoolgroup : Form
    {
        public frmtoolgroup()
        {
            InitializeComponent();
        }
        public void SetSubject(CogToolBlock tb)
        {
            cogToolBlockEditV21.Subject = tb;
        }
        public void SetSubject(CogToolBlock Tb, CogImage8Grey img, ICogRegion region, string param)
        {
            try
            {
                Tb.Inputs[0].Value = img;
                Tb.Inputs[1].Value = region;
                Tb.Inputs[2].Value = param;
            }
            catch (Exception ex) { }
            cogToolBlockEditV21.Subject = Tb;
        }

        private void frmtoolgroup_FormClosing(object sender, FormClosingEventArgs e)
        {
            cogToolBlockEditV21.Subject = null;
            cogToolBlockEditV21.Dispose();
        }
    }
}