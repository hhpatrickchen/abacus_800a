using Cognex.VisionPro;
using DALSA.SaperaLT.SapClassBasic;
using Sopdu.Devices.SecsGem;
using Sopdu.ProcessApps.ImgProcessing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sopdu.Devices.CameraLink
{
    class DalsaCameraLink
    {
        private SapAcquisition m_Acquisition;
        private SapBuffer m_Buffers;
        private SapAcqToBuf m_Xfer;
       // private SapView m_View;
        public bool m_IsSignalDetected;
        private bool m_online;
        private SapLocation m_ServerLocation;
        public string framegrabberconfigfile;
        private Bitmap image1 = null;

        // The object that will contain the palette information for the bitmap
        private ColorPalette imgpal = null;

        // The Mutex object that will protect image objects during processing
        private static Mutex imageMutex = new Mutex();

        public DalsaCameraLink()//constructor
        {
            framegrabberconfigfile = @".\7thMarch2019.ccf";
            m_ServerLocation = new SapLocation("Xtium-CL_MX4_1", 0);//probably setup it at xml file
            m_Acquisition = new SapAcquisition(m_ServerLocation, framegrabberconfigfile);
            if (SapBuffer.IsBufferTypeSupported(m_ServerLocation, SapBuffer.MemoryType.ScatterGather))
                m_Buffers = new SapBufferWithTrash(2, m_Acquisition, SapBuffer.MemoryType.ScatterGather);
            else
                m_Buffers = new SapBufferWithTrash(2, m_Acquisition, SapBuffer.MemoryType.ScatterGatherPhysical);

            m_Xfer = new SapAcqToBuf(m_Acquisition, m_Buffers);
            // m_View = new SapView(m_Buffers);

            //event for view
            m_Xfer.Pairs[0].EventType = SapXferPair.XferEventType.EndOfFrame;
            m_Xfer.XferNotify += new SapXferNotifyHandler(xfer_XferNotify);

            m_Xfer.XferNotifyContext = this;

            // event for signal status
            m_Acquisition.SignalNotify += new SapSignalNotifyHandler(GetSignalStatus);
            m_Acquisition.SignalNotifyContext = this;
            bool success = CreateObjects();
            int exposure;
            m_Acquisition.GetParameter(SapAcquisition.Prm.LINE_INTEGRATE_PULSE1_DURATION, out exposure);//set integration time
            m_Acquisition.GetParameter(SapAcquisition.Prm.LINE_INTEGRATE_PULSE0_DURATION, out exposure);//set integration time
            m_Acquisition.GetParameter(SapAcquisition.Prm.LINE_INTEGRATE_PULSE0_DELAY, out exposure);//set integration time
            m_Acquisition.GetParameter(SapAcquisition.Prm.LINE_INTEGRATE_PULSE1_DELAY, out exposure);//set integration time      
            m_Acquisition.GetParameter(SapAcquisition.Prm.LINE_INTEGRATE_METHOD, out exposure);//set integration time
            m_Acquisition.GetParameter(SapAcquisition.Prm.LINE_INTEGRATE_DURATION, out exposure);//set integration time
            m_Acquisition.GetParameter(SapAcquisition.Prm.LINE_INTEGRATE_ENABLE, out exposure);//set integration time
        }

        public bool StartGrabImage()
        {
            return m_Xfer.Snap();
        }
        public bool WaitImage(int timeout)
        {
            
            return m_Xfer.Wait(timeout);

        }

        public CogImage8Grey GetImage()
        {
            Int32 width, height, bufferPitch;
            IntPtr bufferAddress;
            width = m_Buffers.Width;
            height = m_Buffers.Height;
            bufferPitch = m_Buffers.Pitch;

            m_Buffers.GetAddress(out bufferAddress);

            try
            {
                imageMutex.WaitOne();

                image1 = new Bitmap(width, height, bufferPitch, PixelFormat.Format8bppIndexed, bufferAddress);
                imgpal = image1.Palette;

                // Build bitmap palette Y8
                for (uint i = 0; i < 256; i++)
                {
                    imgpal.Entries[i] = Color.FromArgb(
                    (byte)0xFF,
                    (byte)i,
                    (byte)i,
                    (byte)i);
                }

                image1.Palette = imgpal;
                CogImage8Grey img = new CogImage8Grey(image1);
                return img;
            }
            finally
            {
                imageMutex.ReleaseMutex();
            }
            return null;
        }
        private void GetSignalStatus(object sender, SapSignalNotifyEventArgs e)
        {
            SapAcquisition.AcqSignalStatus signalStatus = e.SignalStatus;
            m_IsSignalDetected = (signalStatus != SapAcquisition.AcqSignalStatus.None);
            //throw new NotImplementedException();
        }

        private void xfer_XferNotify(object sender, SapXferNotifyEventArgs e)
        {          

                //Int32 width, height, bufferPitch;
                //IntPtr bufferAddress;
                //width = m_Buffers.Width;
                //height = m_Buffers.Height;
                //bufferPitch = m_Buffers.Pitch;

                //m_Buffers.GetAddress(out bufferAddress);

                //try
                //{
                //    imageMutex.WaitOne();

                //    image1 = new Bitmap(width, height, bufferPitch, PixelFormat.Format8bppIndexed, bufferAddress);
                //    imgpal = image1.Palette;

                //    // Build bitmap palette Y8
                //    for (uint i = 0; i < 256; i++)
                //    {
                //        imgpal.Entries[i] = Color.FromArgb(
                //        (byte)0xFF,
                //        (byte)i,
                //        (byte)i,
                //        (byte)i);
                //    }

                //    image1.Palette = imgpal;
                //    CogImage8Grey img = new CogImage8Grey(image1);                    
                //}
                //finally
                //{
                //    imageMutex.ReleaseMutex();
                //}               

        }

        // Call Create method  
        private bool CreateObjects()
        {
            // Create acquisition object
            if (m_Acquisition != null && !m_Acquisition.Initialized)
            {
                if (m_Acquisition.Create() == false)
                {
                    DestroyObjects();
                    return false;
                }
            }
            // Create buffer object
            if (m_Buffers != null && !m_Buffers.Initialized)
            {
                if (m_Buffers.Create() == false)
                {
                    DestroyObjects();
                    return false;
                }
                m_Buffers.Clear();
            }
            // Create view object
            //if (m_View != null && !m_View.Initialized)
            //{
            //    if (m_View.Create() == false)
            //    {
            //        DestroyObjects();
            //        return false;
            //    }
            //}
            // Create Xfer object
            if (m_Xfer != null && !m_Xfer.Initialized)
            {
                if (m_Xfer.Create() == false)
                {
                    DestroyObjects();
                    return false;
                }
            }
            return true;
        }

        //Call Destroy method
        private void DestroyObjects()
        {
            if (m_Xfer != null && m_Xfer.Initialized)
                m_Xfer.Destroy();
            //if (m_View != null && m_View.Initialized)
            //    m_View.Destroy();
            if (m_Buffers != null && m_Buffers.Initialized)
                m_Buffers.Destroy();
            if (m_Acquisition != null && m_Acquisition.Initialized)
                m_Acquisition.Destroy();
        }

        private void DisposeObjects()
        {
            if (m_Xfer != null)
            { m_Xfer.Dispose(); m_Xfer = null; }
            //if (m_View != null)
            //{ m_View.Dispose(); m_View = null; }
            if (m_Buffers != null)
            { m_Buffers.Dispose(); m_Buffers = null; }
            if (m_Acquisition != null)
            { m_Acquisition.Dispose(); m_Acquisition = null; }

        }

        internal bool Abort()
        {
            return m_Xfer.Abort();
        }

        internal void Shutdown()
        {
            Abort();
            DestroyObjects();
            DisposeObjects();
        }

        internal bool SetExposure(int p)
        {
            //throw new NotImplementedException();
           return m_Acquisition.SetParameter(SapAcquisition.Prm.LINE_INTEGRATE_DURATION, (p * 85),true);//set integration time
            
        }
    }

    public class TrayImageInfo
    {
        public string serialnumber;
        public CogImage8Grey trayimage;
        public CogImage16Range tray3Dimage;
        public CogImage16Range tray3Dimagefordebug;
        public MapData mapdata;
        public CogImage8Grey trayimagefordebug;
        public List<CogCompositeShape> disrst;
        public CogRectangle TraySearchReg;
        public List<TrayInspectionRst> trayrstgraphic;
        public bool pass;    
        public  Dictionary<string,string> Result3D { get; set; }
        public string inspectionid { get; set; }
    }
}
