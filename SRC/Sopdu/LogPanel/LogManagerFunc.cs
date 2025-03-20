using LogProject;
using NLog;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogPanel
{
    public class LogTool<T>
    {
        private Logger _logger;
       
        private  LogData _classeCanLog = new LogData();

        public   LogData ClasseCanLog
        {
            get { return _classeCanLog; }
            set { _classeCanLog = value; }
        }


        private bool CheckForLog()
        {
            try
            {
                if (GlobalData.LogDataList.First(t => t.Name == ClasseCanLog.Name).SelectedIndex == 0)
                     return true;
                else return false;
            }
            catch (Exception)
            {
                return false;
            }
           
        }
        public LogTool()
        {
            ClasseCanLog.Name = typeof(T).FullName;
            _logger = NLog.LogManager.GetLogger(ClasseCanLog.Name);
            if (!GlobalData.LogDataList.Any(t=>t.Name== ClasseCanLog.Name))
            {
                GlobalData.LogDataList.Add(ClasseCanLog);
            }
        }
       
        public void DebugLog(string Message)
        {
            if (!CheckForLog()) { return; }
            _logger?.Debug(Message);
        }
        public void WarnLog(string Message)
        {
            if (!CheckForLog()) { return; }
            _logger?.Warn(Message);
        }

        public void InfoLog(string Message)
        {
            if (!CheckForLog()) { return; }
            _logger?.Info(Message);
        }

        public void ErrorLog(string Message)
        {
            if (!CheckForLog()) { return; }
            _logger?.Error(Message);
        }
        public void TraceLog(string Message)
        {
            if (!CheckForLog()) { return; }
            _logger?.Trace(Message);
        }

        public void FatalLog(string Message)
        {
            if (!CheckForLog()) { return; }
            _logger?.Fatal(Message);

        }
    }
}
