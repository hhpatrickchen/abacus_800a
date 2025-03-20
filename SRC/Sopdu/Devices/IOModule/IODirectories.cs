using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sopdu.Devices.IOModule
{
    public class IODirectories : GenericDevice
    {
        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public ObservableCollection<DiscreteIO> IOs;

        [XmlIgnore]
        public Dictionary<string, DiscreteIO> IpDirectory;

        public void Init()
        {
            IpDirectory = new Dictionary<string, DiscreteIO>();
            for (int i = 0; i < IOs.Count; i++)
            {
                IpDirectory.Add(IOs[i].Name, IOs[i]);
            }
        }
    }
}