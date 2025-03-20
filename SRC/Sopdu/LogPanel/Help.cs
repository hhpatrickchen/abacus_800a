using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LogPanel
{
    public class Help<T>
    {
        private static readonly object lockObject = new object();
        private static Help<T> instance;

        private Help() { }

        public static Help<T> Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new Help<T>();
                        }
                    }
                }
                return instance;
            }
        }
        BinaryFormatter formatter = new BinaryFormatter();
        public void DoSerialization(T Entity,string Path)
        {
            lock (lockObject)
            {
                XmlSerializer xml = new XmlSerializer(typeof(T));
                using (FileStream stream = new FileStream(Path, FileMode.Create))
                {
                    xml.Serialize(stream, Entity);
                }
            }
        }
        public T DoDeserialization(string Path )
        {
            lock (lockObject)
            {
                T deserializedEntity;
                XmlSerializer xml = new XmlSerializer(typeof(T));
                using (FileStream stream = new FileStream(Path, FileMode.Open))
                {
                    deserializedEntity = (T)xml.Deserialize(stream);
                }
                return deserializedEntity;
            }
        }
    }
}
