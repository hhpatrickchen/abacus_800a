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
    public partial class frmSearchTrain : Form
    {
        public frmSearchTrain()
        {
            InitializeComponent();
        }

        public frmSearchTrain(CogPMAlignTool subject)
        {
            InitializeComponent();
            this.pmAlignControl1.Subject = subject;
        }
    }
}