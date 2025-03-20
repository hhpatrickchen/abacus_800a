using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sopdu.Devices.IOModule
{
    public class SystemIO : GenericDevice
    {
        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        [XmlIgnore]
        public DiscreteIO ipEMO;

        [XmlIgnore]
        public bool terminate;
    }
}