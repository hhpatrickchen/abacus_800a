using LogPanel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogProject
{
    public  class MainViewModel: INotifyPropertyChanged
    {
		public MainViewModel() 
		{
            //LogClasses.Add(new LogData() { Item = 1, Name = "CLASS1" });
            //LogClasses.Add(new LogData() { Item = 2, Name = "CLASS2" });
            //LogClasses.Add(new LogData() { Item = 3, Name = "CLASS3" });
            //LogClasses.Add(new LogData() { Item = 4, Name = "CLASS4" });
           
        }	
		private ObservableCollection<LogData> _logClasses = new ObservableCollection<LogData>();

		public ObservableCollection<LogData>LogClasses
        {
			get { return _logClasses; }
			set { _logClasses= value; OnPropertyChanged("LogClasses"); }
		}
       
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
    }
}
