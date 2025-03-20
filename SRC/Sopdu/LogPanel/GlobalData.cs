using LogProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogPanel
{
    public class GlobalData
    {
		private static List<LogData> _logDataList = new List<LogData>();

		public  static List<LogData> LogDataList
        {
			get { return _logDataList; }
			set { _logDataList = value; }
		}

	}
}
