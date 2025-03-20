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
    public partial class frmImageFileTool : Form
    {
        public frmImageFileTool()
        {
            InitializeComponent();
        }
        internal void SetSubJect(Cognex.VisionPro.ImageFile.CogImageFileTool cogImageFileTool)
        {
            cogImageFileEditV21.Subject = cogImageFileTool;
        }
    }
}
