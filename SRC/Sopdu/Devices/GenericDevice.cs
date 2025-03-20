using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sopdu.Devices
{
    public abstract class GenericDevice : NotifyPropertyChangedObject
    {
        private bool _isManualAllowed;

        public GenericDevice()
        {
            this.IsManualAllowed = true;
        }

        public bool IsManualAllowed
        {
            get
            {
                return _isManualAllowed;
            }
            set
            {
                _isManualAllowed = value;
                NotifyPropertyChanged();
            }
        }

        public abstract string Name
        {
            get;
        }
    }

    public abstract class NotifyPropertyChangedObject : INotifyPropertyChanged
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
    }

}