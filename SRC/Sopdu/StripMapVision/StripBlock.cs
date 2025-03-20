using Cognex.VisionPro;
using Cognex.VisionPro.Blob;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.ID;
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.PixelMap;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.QuickBuild;
using Sopdu.Devices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sopdu.StripMapVision
{
    public class StripBlock : NotifyPropertyChangedObject
    {
        public int column;
        public int row;
        public int BlockNumber;

        [XmlIgnore]
        public CogRectangle Region;
    }

    public class Substrate : NotifyPropertyChangedObject
    {
        public string RecipeName { get; set; }
        private ObservableCollection<string> _IFInvList;
        public ObservableCollection<string> IFInvList { get { return _IFInvList; } set { _IFInvList = value; NotifyPropertyChanged(); } }
        private int _row;
        public int row { get { return _row; } set { _row = value; NotifyPropertyChanged(); } }//cannnot edit
        private int _column;
        public int column { get { return _column; } set { _column = value; NotifyPropertyChanged(); } }//cannnot edit
        private double _yield;
        public double yield { get { return _yield; } set { _yield = value; NotifyPropertyChanged(); } }//cannnot edit
        private int _Exposure;
        public int Exposure { get { return _Exposure; } set { _Exposure = value; NotifyPropertyChanged(); } }//cannnot edit
        private int _numBlock;
        public int numBlock { get { return _numBlock; } set { _numBlock = value; NotifyPropertyChanged(); } }//cannnot edit
        private int _IdFactor;
        public int IdFactor { get { return _IdFactor; } set { _IdFactor = value; NotifyPropertyChanged(); } }//factor setting affinetxform for ID decoding
        private double _XAllowance;//can edit
        public double XAllowance { get { return _XAllowance; } set { _XAllowance = value; NotifyPropertyChanged(); } }
        private double _YAllowance;//can edit
        public double YAllowance { get { return _YAllowance; } set { _YAllowance = value; NotifyPropertyChanged(); } }
        private double _Distanceupper;//can edit
        public double Distanceupper { get { return _Distanceupper; } set { _Distanceupper = value; NotifyPropertyChanged(); } }
        private double _Distancelower;//can edit
        public double Distancelower { get { return _Distancelower; } set { _Distancelower = value; NotifyPropertyChanged(); } }

        [XmlIgnore]
        public Dictionary<string, StripBlock> Blocks;

        [XmlIgnore]
        public CogImage8Grey refimage;

        //[XmlIgnore]
        //public CogIDTool idtool;

        [XmlIgnore]
        public CogPMAlignTool alignTool;

        [XmlIgnore]
        public CogPixelMapTool pmap;

        [XmlIgnore]
        public CogAffineTransformTool tx;

        [XmlIgnore]
        public CogBlobTool bdetect;

        [XmlIgnore]
        public CogFixtureTool fx;

        public void Init()
        {
            //load refimage
            //load idtool;
            //load alignTool
            tx = new CogAffineTransformTool();
            pmap = new CogPixelMapTool();
            pmap.RunParams.OutputInverted = true;
            fx = new CogFixtureTool();
        }

        public CogImage8Grey GetAlignImage(CogImage8Grey img)
        {
            alignTool.InputImage = img;
            alignTool.Run();
            fx.InputImage = img;
            fx.RunParams.UnfixturedFromFixturedTransform = alignTool.Results[0].GetPose();
            fx.Run();
            return (CogImage8Grey)fx.OutputImage;
        }

        public string ReadID(CogImage8Grey img)
        {
            int step = 0;
            pmap.InputImage = img;
            pmap.Run();
            while (step < 5)
            {
                algo.Inputs[0].Value = img;
                algo.Run();
                step++;
                try
                {
                    if (((CogIDTool)algo.Tools["CogIDTool1"]).Results.Count > 0) break;
                }
                catch (Exception ex) { }
            }
            return ((CogIDTool)algo.Tools["CogIDTool1"]).Results[0].DecodedData.DecodedString;
        }

        private ICogImage ImageProcess(CogImage8Grey img, int step)
        {
            tx.RunParams.ScalingY = tx.RunParams.ScalingY + step * 0.1;
            return img;
        }

        public bool RunInpsection(CogImage8Grey img)
        {
            fx.InputImage = img;//assume pmalign is completed
            fx.Run();
            CogImage8Grey runimg = (CogImage8Grey)fx.OutputImage;
            //run through the dictionary list
            //end of dictionary list
            return true;
        }

        [XmlIgnore]
        public Cognex.VisionPro.ToolBlock.CogToolBlock algo { get; set; }

        [XmlIgnore]
        public CogJobManager cjm;

        [XmlIgnore]
        public Cognex.VisionPro.ToolBlock.CogToolBlock cb { get; set; }
    }
}