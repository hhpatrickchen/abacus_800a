using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sopdu.ProcessApps.main
{
    public class MenuObj : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (GetType().GetProperty(propertyName) != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                throw new ArgumentException("propertyName");
            }
        }

        public string LastUser;
        private string _DefaultRecipe;
        public string DefaultRecipe { get { return _DefaultRecipe; } set { _DefaultRecipe = value; NotifyPropertyChanged(); } }
        public string EquipmentName;

        public string sIPMode
        {
            get { return _sIPMode; }
            set { _sIPMode = value; NotifyPropertyChanged(); }
        }
        private string _sIPMode;

        public string sOPMode
        {
            get { return _sOPMode; }
            set { _sOPMode = value; NotifyPropertyChanged(); }
        }
        private string _sOPMode;

        public bool isIPSFARdy
        {
            get { return _isIPSFARdy; }
            set { _isIPSFARdy = value; NotifyPropertyChanged(); }
        }
        private bool _isIPSFARdy;

        public bool isIPOHTRdy
        {
            get { return _isIPOHTRdy; }
            set { _isIPOHTRdy = value; NotifyPropertyChanged(); }
        }
        private bool _isIPOHTRdy;

        public bool isOPSFARdy
        {
            get { return _isOPSFARdy; }
            set { _isOPSFARdy = value; NotifyPropertyChanged(); }
        }
        private bool _isOPSFARdy;

        public bool isOPOHTRdy
        {
            get { return _isOPOHTRdy; }
            set { _isOPOHTRdy = value; NotifyPropertyChanged(); }
        }
        private bool _isOPOHTRdy;
    }

    public class RTViewItem : INotifyPropertyChanged
    {
        public string sIPMode
        {
            get { return _sIPMode; }
            set { _sIPMode = value; NotifyPropertyChanged(); }
        }
        private string _sIPMode;

        public string sOPMode
        {
            get { return _sOPMode; }
            set { _sOPMode = value; NotifyPropertyChanged(); }
        }
        private string _sOPMode;

        public bool isIPSFARdy
        {
            get { return _isIPSFARdy; }
            set { _isIPSFARdy = value; NotifyPropertyChanged(); }
        }
        private bool _isIPSFARdy;

        public bool isIPOHTRdy
        {
            get { return _isIPOHTRdy; }
            set { _isIPOHTRdy = value; NotifyPropertyChanged(); }
        }
        private bool _isIPOHTRdy;

        public bool isOPSFARdy
        {
            get { return _isOPSFARdy; }
            set { _isOPSFARdy = value; NotifyPropertyChanged(); }
        }
        private bool _isOPSFARdy;

        public bool isOPOHTRdy
        {
            get { return _isOPOHTRdy; }
            set { _isOPOHTRdy = value; NotifyPropertyChanged(); }
        }
        private bool _isOPOHTRdy;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (GetType().GetProperty(propertyName) != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                throw new ArgumentException("propertyName");
            }
        }
    }
}