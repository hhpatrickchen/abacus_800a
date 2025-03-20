using LogPanel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LogProject
{
   
    public  class LogData : INotifyPropertyChanged
    {
      
        public LogData() 
		{
           
        }
       
        private int _item;
      
        public int Item
		{
			get { return _item; }
			set { _item = value; OnPropertyChanged("Item"); }
		}


        private string _name;

		public string Name
		{
			get { return _name; }
			set { _name = value; OnPropertyChanged("Name"); }
		}

       


        private int _selectedIndex=0;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { _selectedIndex = value; OnPropertyChanged("SelectedIndex"); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
       
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private List<string> _comboBoxList = new List<string>() { "True", "False" };
        public List<string> ComboBoxList
        {
            get { return _comboBoxList; }
            set { _comboBoxList = value; }
        }


    }
}

