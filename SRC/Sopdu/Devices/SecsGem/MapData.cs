using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Sopdu.Devices.SecsGem
{
    ///MapData defination <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class MapData
    {
        private MapDataLayout[] layoutsField;

        private MapDataSubstrates substratesField;

        private MapDataSubstrateMaps substrateMapsField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Layout", IsNullable = false)]
        public MapDataLayout[] Layouts
        {
            get
            {
                return this.layoutsField;
            }
            set
            {
                this.layoutsField = value;
            }
        }

        /// <remarks/>
        public MapDataSubstrates Substrates
        {
            get
            {
                return this.substratesField;
            }
            set
            {
                this.substratesField = value;
            }
        }

        /// <remarks/>
        public MapDataSubstrateMaps SubstrateMaps
        {
            get
            {
                return this.substrateMapsField;
            }
            set
            {
                this.substrateMapsField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MapDataLayout
    {
        private MapDataLayoutDimension dimensionField;

        private MapDataLayoutChildLayouts childLayoutsField;
        //private MapDataLayoutChildLayouts childLayoutField;
        private string layoutIdField;

        private string defaultUnitsField;

        private bool topLevelField;

        /// <remarks/>
        public MapDataLayoutDimension Dimension
        {
            get
            {
                return this.dimensionField;
            }
            set
            {
                this.dimensionField = value;
            }
        }

        /// <remarks/>
        public MapDataLayoutChildLayouts ChildLayouts
        {
            get
            {
                return this.childLayoutsField;
            }
            set
            {
                this.childLayoutsField = value;
            }
        }


        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string LayoutId
        {
            get
            {
                return this.layoutIdField;
            }
            set
            {
                this.layoutIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string DefaultUnits
        {
            get
            {
                return this.defaultUnitsField;
            }
            set
            {
                this.defaultUnitsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool TopLevel
        {
            get
            {
                return this.topLevelField;
            }
            set
            {
                this.topLevelField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MapDataLayoutDimension
    {
        private byte xField;

        private byte yField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte X
        {
            get
            {
                return this.xField;
            }
            set
            {
                this.xField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte Y
        {
            get
            {
                return this.yField;
            }
            set
            {
                this.yField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MapDataLayoutChildLayouts
    {
        private MapDataLayoutChildLayoutsChildLayouts childLayoutsField;
        private MapDataLayoutChildLayoutsChildLayouts childLayoutField;
        /// <remarks/>
        public MapDataLayoutChildLayoutsChildLayouts ChildLayouts
        {
            get
            {
                return this.childLayoutsField;
            }
            set
            {
                this.childLayoutsField = value;
            }
        }

        /// <remarks/>
        public MapDataLayoutChildLayoutsChildLayouts ChildLayout
        {
            get
            {
                return this.childLayoutField;
            }
            set
            {
                this.childLayoutField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MapDataLayoutChildLayoutsChildLayouts
    {
        private string layoutIdField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string LayoutId
        {
            get
            {
                return this.layoutIdField;
            }
            set
            {
                this.layoutIdField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MapDataSubstrates
    {
        private MapDataSubstratesSubstrate substrateField;

        /// <remarks/>
        public MapDataSubstratesSubstrate Substrate
        {
            get
            {
                return this.substrateField;
            }
            set
            {
                this.substrateField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MapDataSubstratesSubstrate
    {
        private string substrateTypeField;

        private string substrateIdField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SubstrateType
        {
            get
            {
                return this.substrateTypeField;
            }
            set
            {
                this.substrateTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SubstrateId
        {
            get
            {
                return this.substrateIdField;
            }
            set
            {
                this.substrateIdField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MapDataSubstrateMaps
    {
        private MapDataSubstrateMapsSubstrateMap substrateMapField;

        /// <remarks/>
        public MapDataSubstrateMapsSubstrateMap SubstrateMap
        {
            get
            {
                return this.substrateMapField;
            }
            set
            {
                this.substrateMapField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MapDataSubstrateMapsSubstrateMap
    {
        private MapDataSubstrateMapsSubstrateMapOverlay overlayField;

        private string substrateTypeField;

        private string substrateIdField;

        private string layoutSpecifierField;

        private ushort orientationField;

        private string originLocationField;

        private string axisDirectionField;

        private string substrateSideField;

        /// <remarks/>
        public MapDataSubstrateMapsSubstrateMapOverlay Overlay
        {
            get
            {
                return this.overlayField;
            }
            set
            {
                this.overlayField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SubstrateType
        {
            get
            {
                return this.substrateTypeField;
            }
            set
            {
                this.substrateTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SubstrateId
        {
            get
            {
                return this.substrateIdField;
            }
            set
            {
                this.substrateIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string LayoutSpecifier
        {
            get
            {
                return this.layoutSpecifierField;
            }
            set
            {
                this.layoutSpecifierField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort Orientation
        {
            get
            {
                return this.orientationField;
            }
            set
            {
                this.orientationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string OriginLocation
        {
            get
            {
                return this.originLocationField;
            }
            set
            {
                this.originLocationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string AxisDirection
        {
            get
            {
                return this.axisDirectionField;
            }
            set
            {
                this.axisDirectionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string SubstrateSide
        {
            get
            {
                return this.substrateSideField;
            }
            set
            {
                this.substrateSideField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MapDataSubstrateMapsSubstrateMapOverlay
    {
        private MapDataSubstrateMapsSubstrateMapOverlayBinCodeMap binCodeMapField;

        private string mapNameField;

        private byte mapVersionField;

        /// <remarks/>
        public MapDataSubstrateMapsSubstrateMapOverlayBinCodeMap BinCodeMap
        {
            get
            {
                return this.binCodeMapField;
            }
            set
            {
                this.binCodeMapField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string MapName
        {
            get
            {
                return this.mapNameField;
            }
            set
            {
                this.mapNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte MapVersion
        {
            get
            {
                return this.mapVersionField;
            }
            set
            {
                this.mapVersionField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MapDataSubstrateMapsSubstrateMapOverlayBinCodeMap
    {
        private MapDataSubstrateMapsSubstrateMapOverlayBinCodeMapBinDefinition[] binDefinitionsField;

        private string[] binCodeField;

        private byte nullBinField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("BinDefinition", IsNullable = false)]
        public MapDataSubstrateMapsSubstrateMapOverlayBinCodeMapBinDefinition[] BinDefinitions
        {
            get
            {
                return this.binDefinitionsField;
            }
            set
            {
                this.binDefinitionsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("BinCode")]
        public string[] BinCode
        {
            get
            {
                return this.binCodeField;
            }
            set
            {
                this.binCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte NullBin
        {
            get
            {
                return this.nullBinField;
            }
            set
            {
                this.nullBinField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MapDataSubstrateMapsSubstrateMapOverlayBinCodeMapBinDefinition
    {
        private string binCodeField;

        private byte binCountField;

        private string binDescriptionField;

        private string pickField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string BinCode
        {
            get
            {
                return this.binCodeField;
            }
            set
            {
                this.binCodeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte BinCount
        {
            get
            {
                return this.binCountField;
            }
            set
            {
                this.binCountField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string BinDescription
        {
            get
            {
                return this.binDescriptionField;
            }
            set
            {
                this.binDescriptionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Pick
        {
            get
            {
                return this.pickField;
            }
            set
            {
                this.pickField = value;
            }
        }
    }

    //end of mapdata defination
    public class XMLConverter
    {
        /// <summary>
        /// Returns the set of included namespaces for the serializer.
        /// </summary>
        /// <returns>
        /// The set of included namespaces for the serializer.
        /// </returns>
        public static XmlSerializerNamespaces GetNamespaces()
        {
            XmlSerializerNamespaces ns;
            ns = new XmlSerializerNamespaces();
            ns.Add("xs", "http://www.w3.org/2001/XMLSchema");
            ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            return ns;
        }

        public static string TargetNamespace
        {
            get
            {
                return "http://www.w3.org/2001/XMLSchema";
            }
        }

        //Creates an object from an XML string.
        public static object FromXml(string Xml, System.Type ObjType)
        {
            XmlSerializer ser;
            ser = new XmlSerializer(ObjType);
            StringReader stringReader;
            stringReader = new StringReader(Xml);
            XmlTextReader xmlReader;
            xmlReader = new XmlTextReader(stringReader);
            object obj;
            obj = ser.Deserialize(xmlReader);
            xmlReader.Close();
            stringReader.Close();
            return obj;
        }

        //Serializes the <i>Obj</i> to an XML string.
        public static string ToXml(object Obj, System.Type ObjType)
        {
            XmlSerializer ser;
            ser = new XmlSerializer(ObjType, XMLConverter.TargetNamespace);
            //ser = new XmlSerializer(ObjType, "");
            MemoryStream memStream;
            memStream = new MemoryStream();
            XmlTextWriter xmlWriter;
            xmlWriter = new XmlTextWriter(memStream, Encoding.UTF8);
            xmlWriter.Namespaces = true;
            ser.Serialize(xmlWriter, Obj, XMLConverter.GetNamespaces());
            //ser.Serialize(xmlWriter, Obj);
            xmlWriter.Close();
            memStream.Close();
            string xml;
            xml = Encoding.UTF8.GetString(memStream.GetBuffer());
            xml = xml.Substring(xml.IndexOf(Convert.ToChar(60)));
            xml = xml.Substring(0, (xml.LastIndexOf(Convert.ToChar(62)) + 1));
            return xml;
        }

        public static string ToXmlNoNameSpace(object Obj)
        {
            //this avoids xml document declaration
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Indent = false,
                OmitXmlDeclaration = true
            };

            var stream = new MemoryStream();
            using (XmlWriter xw = XmlWriter.Create(stream, settings))
            {
                //this avoids xml namespace declaration
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces(
                                   new[] { XmlQualifiedName.Empty });
                XmlSerializer x = new XmlSerializer(Obj.GetType(), "");
                x.Serialize(xw, Obj, ns);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}