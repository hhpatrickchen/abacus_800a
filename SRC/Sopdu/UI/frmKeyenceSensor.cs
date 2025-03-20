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
    public partial class frmKeyenceSensor : Form
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public frmKeyenceSensor()
        {
            InitializeComponent();
        }

        private void frmKeyenceSensor_Load(object sender, EventArgs e)
        {
            int Threshold = GlobalVar.iKeysenceThreshold;

            nmthreshold.Value = Threshold;

            log.Debug($"Load keyence sensor threshold={Threshold}");
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            int value = Convert.ToInt32(nmthreshold.Value); // 轉換為 int
            GlobalVar.iKeysenceThreshold = value;

            log.Debug($"save keyence sensor threshold={value}");

            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
