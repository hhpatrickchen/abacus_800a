using Cognex.VisionPro.ImageProcessing;
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
    public partial class testform : Form
    {
        public testform()
        {
            InitializeComponent();
        }

        public testform(CogCopyRegionTool ttool)
        {
            InitializeComponent();
            this.cogCopyRegionEditV21.Subject = ttool;
        }
    }
}