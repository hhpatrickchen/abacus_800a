using Sopdu.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Sopdu.helper
{
    public class CMsgClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private string _time;
        public string time { get { return _time; } set { _time = value; NotifyPropertyChanged("time"); } }
        private string _Level;
        public string Level { get { return _Level; } set { _Level = value; NotifyPropertyChanged("Level"); } }
        private string _Msg;
        public string Msg { get { return _Msg; } set { _Msg = value; NotifyPropertyChanged("Msg"); } }
    }
}
