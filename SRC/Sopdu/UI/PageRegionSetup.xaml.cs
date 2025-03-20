using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.ID;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.QuickBuild;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using Sopdu.helper;
using Sopdu.ProcessApps.ImgProcessing;
using Sopdu.ProcessApps.main;
using Sopdu.StripMapVision;
using Sopdu.UI.popups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace Sopdu.UI
{
    /// <summary>
    /// Interaction logic for PageRegionSetup.xaml
    /// </summary>
    public partial class PageRegionSetup : Page
    {
        public CogImage8Grey img01;
        public CogImage8Grey img02;
        public CogImage8Grey img03;
        private MainApp app;

        private CogImage8Grey currentimg;
        private CogFixtureTool fix;
        private CogPMAlignTool fiducialsearch;
        private List<CogRectangle> CellList;
        public CogCompositeShape compositeshapes01;

        private List<CogRectangle> t01cellist;
        private List<CogRectangle> t02cellist;
        private List<CogRectangle> t03cellist;

        private List<ICogRegion> t01cellistm;
        private List<ICogRegion> t02cellistm;
        private List<ICogRegion> t03cellistm;

        private List<CogCompositeShape> rstshapelist;
        public List<CogCompositeShape> rstshapelist01;
        public List<CogCompositeShape> rstshapelist02;
        public List<CogCompositeShape> rstshapelist03;
        private Thread Thread01;
        private Thread Thread02;
        private Thread Thread03;
        private ManualResetEvent Thread01StartEvt;
        private ManualResetEvent Thread02StartEvt;
        private ManualResetEvent Thread03StartEvt;
        private ManualResetEvent Thread01CompleteEvt;
        private ManualResetEvent Thread02CompleteEvt;
        private ManualResetEvent Thread03CompleteEvt;
        private bool terminate = false;
        public CogPMAlignTool fiducialsearch02;
        public CogPMAlignTool fiducialsearch01;
        public CogPMAlignTool fiducialsearch03;

        public PageRegionSetup()
        {
            InitializeComponent();
        }

        public PageRegionSetup(MainWindow main)
        {
            InitializeComponent();
            app = main.mainapp;
            DataContext = app;
            //DBAccess db = new DBAccess();
            //DataTable dt = db.GetRecipeList();
            //gridRecipeList.ItemsSource = dt.DefaultView;
            CellList = new List<CogRectangle>();
            rstshapelist = new List<CogCompositeShape>();
            rstshapelist01 = new List<CogCompositeShape>();
            rstshapelist02 = new List<CogCompositeShape>();
            rstshapelist03 = new List<CogCompositeShape>();
            Thread01StartEvt = new ManualResetEvent(false);
            Thread02StartEvt = new ManualResetEvent(false);
            Thread03StartEvt = new ManualResetEvent(false);
            Thread01CompleteEvt = new ManualResetEvent(false);
            Thread02CompleteEvt = new ManualResetEvent(false);
            Thread03CompleteEvt = new ManualResetEvent(false);
            //update TrayMgr
            UpdateTrayMgr();

        }
        public void UpdateTrayMgr()
        {

            tbPartLocate = (CogToolBlock)app.CurrSubstrateRecipe.cb.Tools["PartLocate"];
            tbDetectOffPocket = (CogToolBlock)app.CurrSubstrateRecipe.cb.Tools["DetectoffPocket"];
            tbDeathBug = (CogToolBlock)app.CurrSubstrateRecipe.cb.Tools["DeathBug"];
            tbFindTray = (CogToolBlock)app.CurrSubstrateRecipe.cb.Tools["FindTray"];
            if (app.TrayMgr01.InputImage != null)
            {
                unprocessimg = app.TrayMgr01.InputImage;
                CogAffineTransformTool atf_tool = (CogAffineTransformTool)tbFindTray.Tools["CogAffineTransformTool1"];// will need to provide a transform tool for both empty pocket and pattern search
                atf_tool.RunParams.ScalingY = app.CurrSubstrateRecipe.yield;
                atf_tool.RunParams.ScalingX = app.CurrSubstrateRecipe.yield;
                atf_tool.InputImage = app.TrayMgr01.InputImage;
                atf_tool.Region = null;
                atf_tool.Run();//get txformed image
                currentimg = (CogImage8Grey)atf_tool.OutputImage;
                //test code here
                CogPMAlignTool tool = (CogPMAlignTool)tbFindTray.Tools["FindTray1"];
                tool.InputImage = currentimg;
                tool.Run();
                app.TrayMgr01.fixture = (CogFixtureTool)tbFindTray.Tools["TrayFixture"];
                app.TrayMgr01.fixture.InputImage = currentimg;
                try
                {
                    app.CurrSubstrateRecipe.Blocks["0"].Region.Changed -= Region_Changed;
                }
                catch (Exception ex) { }
                app.CurrSubstrateRecipe.Blocks["0"].Region.Changed += Region_Changed;
                if (tool.Results.Count == 0)
                {
                    MessageBox.Show("unable to find tray, setup required");
                    return;
                }
                app.TrayMgr01.fixture.RunParams.UnfixturedFromFixturedTransform = tool.Results[0].GetPose();
                app.TrayMgr01.fixture.Run();
                CellList.Clear();
                Cognex.VisionPro.ICogRecord lastRecord =
                tool.CreateLastRunRecord();
                this.PatternRecDisplay.Record = lastRecord.SubRecords["InputImage"];
                PatternRecDisplay.Fit(true);
                SetupDisplay.Image = app.TrayMgr01.fixture.OutputImage;
                app.CurrSubstrateRecipe.Blocks["0"].Region.Interactive = true;
                app.CurrSubstrateRecipe.Blocks["0"].Region.GraphicDOFEnable = CogRectangleDOFConstants.All;
                SetupDisplay.InteractiveGraphics.Add(app.CurrSubstrateRecipe.Blocks["0"].Region, "Block0", true);
                SetupDisplay.Fit(true);
                RecipeDisplay.Image = app.TrayMgr01.fixture.OutputImage;
                RecipeDisplay.Fit(true);
                Region_Changed(app.CurrSubstrateRecipe.Blocks["0"].Region, null);
            }
        }
        private void BtnCreateRecipe(object sender, RoutedEventArgs e)
        {
            //open recipe creation windows
            Substrate sb = new Substrate();
            sb.column = 3; sb.row = 3; sb.numBlock = 1; sb.yield = 0.3;
            //            sb.;
            sb.RecipeName = "DEFAULT";

            sb.IFInvList = new ObservableCollection<string>();
            NewRecipeEntry frm = new NewRecipeEntry(sb);

            if (frm.ShowDialog() == true)
            {
                if (Directory.Exists(@".\Recipe\" + sb.RecipeName))
                {
                    MessageBox.Show("Recipe Insert Fail, " + sb.RecipeName + @" Exist.");
                    return;
                }

                string rdirectory = @".\Recipe\" + sb.RecipeName;//generate physical directory for reference
                Directory.CreateDirectory(rdirectory);
                //save block data;
                // only region is important
                string bkdir = rdirectory + @"\Block";
                //add in 2did
                
                //CogSerializer.SaveObjectToFile(sb.idtool, @".\Recipe\" + sb.RecipeName + @"\ID.vpp");
                System.IO.File.Copy(@".\RecipeTemplate\Default\SearchPatternCB.vpp", @".\Recipe\" + sb.RecipeName + @"\SearchPatternCB.vpp", true);
                System.IO.File.Copy(@".\RecipeTemplate\Default\TrainImage.bmp", @".\Recipe\" + sb.RecipeName + @"\TrainImage.bmp", true);
                foreach (KeyValuePair<string, StripBlock> entry in sb.Blocks)
                {
                    StripBlock bk = (StripBlock)entry.Value;
                    string bkdirectory = bkdir + bk.BlockNumber.ToString();
                    Directory.CreateDirectory(bkdirectory);
                    CogSerializer.SaveObjectToFile(bk.Region, bkdirectory + @"\region.vpp");
                }
                //save xml file

                XmlSerializer tserializer;
                tserializer = new XmlSerializer(typeof(Substrate));
                TextWriter wrt = new StreamWriter(@".\Recipe\" + sb.RecipeName + @"\traymap.xml");
                tserializer.Serialize(wrt, sb);
                wrt.Close();

                //System.IO.File.Copy(@".\Position\*.*", @".\Recipe\" + sb.RecipeName + @"\SearchPatternCB.vpp", true);
                string targetpath = @".\Recipe\" + sb.RecipeName;
                string sourcePath = @".\Positions";
                if (!Directory.Exists(targetpath))
                {
                    Directory.CreateDirectory(targetpath);
                }
                foreach (var srcPath in Directory.GetFiles(sourcePath))
                {
                    //Copy the file from sourcepath and place into mentioned target path,
                    //Overwrite the file if same file is exist in target path
                    File.Copy(srcPath, srcPath.Replace(sourcePath, targetpath), true);
                }
                //gridRecipeList.ItemsSource = db.GetRecipeList().DefaultView;
                //update recipelist
                app.RecipeList = new ObservableCollection<string>();//dont need database. host suppose to tell me which recipe to use
                string[] stringlist = Directory.GetDirectories(@".\Recipe\");
                foreach (string str in stringlist)
                {
                    string[] str11 = str.Split('\\');
                    app.RecipeList.Add(str11[2]);
                }
            }
        }

        private void gridRecipeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                //               string str = ((string)((DataRowView)gridRecipeList.SelectedItem).Row[0]).Trim();
                //                DBAccess db = new DBAccess();
                //                DataTable tb = db.GetInvList(str);
                //                gridLFList.ItemsSource = tb.DefaultView;
            }
            catch (Exception ex) { }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //DBAccess db = new DBAccess();
                string str = (string)gridRecipeList.SelectedValue;
                if (str.Length > 0)
                {
                    string rdirectory = @".\Recipe\" + str;//generate physical directory for reference
                    Directory.Delete(rdirectory, true);
                    app.RecipeList.Remove(str);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid Recipe or No Recipe Selected");
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //check if a LF INV# is being selected
            //if (gridLFList.SelectedItem != null)
            //{
            //    string str = ((string)((DataRowView)gridLFList.SelectedItem).Row[0]).Trim();
            //    LFInvDataEdit frm = new LFInvDataEdit();
            //    frm.setOldString(str);
            //    frm.ShowDialog();
            //    //update database
            //    DBAccess db = new DBAccess();
            //    DataTable tb = db.GetInvList(frm.RecipeName);
            //    gridLFList.ItemsSource = tb.DefaultView;
            //}
            //else
            //{
            //    MessageBox.Show("No LF Inv # Selected");
            //}
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)//loading of recipe
        {
            try
            {
                app.VisionLoad((string)gridRecipeList.SelectedValue);
                app.loadvisiontoprocessor();
                UpdateTrayMgr();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Recipe Load Fail : " + ex.ToString());
                return;
            }
            MessageBox.Show((string)gridRecipeList.SelectedValue + " Load Successfully");
        }

        private void tool_Ran(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            CogPMAlignTool tool = (CogPMAlignTool)sender;

            fix.InputImage = currentimg;
            fix.RunParams.UnfixturedFromFixturedTransform = tool.Results[0].GetPose();
            fix.Run();
            CellList.Clear();
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                Cognex.VisionPro.ICogRecord lastRecord =
               tool.CreateLastRunRecord();
                this.PatternRecDisplay.Record = lastRecord.SubRecords["InputImage"];
                PatternRecDisplay.Fit(true);

                SetupDisplay.Image = fix.OutputImage;
                SetupDisplay.Fit(true);
                RecipeDisplay.Image = fix.OutputImage;
                RecipeDisplay.Fit(true);
                foreach (KeyValuePair<string, StripBlock> item in app.CurrSubstrateRecipe.Blocks)
                {
                    Region_Changed(item.Value.Region, null);
                }
            });
        }

        private void IDRegionChange(object sender, CogChangedEventArgs e)
        {
            app.CurrSubstrateRecipe.algo.Inputs[0].Value = (CogImage8Grey)BCRcpDisplay.Image;
            app.CurrSubstrateRecipe.algo.Run();
            try
            {
                BCRcpDisplay.StaticGraphics.Clear();
                CogPolygon ply = ((CogIDTool)app.CurrSubstrateRecipe.algo.Tools["CogIDTool1"]).Results[0].BoundsPolygon;
                BCRcpDisplay.StaticGraphics.Add(ply, "ply");
                //setup image for defect inspection
                SetupDisplay.Image = (CogImage8Grey)app.CurrSubstrateRecipe.algo.Outputs[0].Value;
                RecipeDisplay.Image = (CogImage8Grey)app.CurrSubstrateRecipe.algo.Outputs[0].Value;
                SetupDisplay.InteractiveGraphics.Clear();
                app.CurrSubstrateRecipe.Blocks["0"].Region.SelectedSpaceName = ".";
                SetupDisplay.InteractiveGraphics.Add(app.CurrSubstrateRecipe.Blocks["0"].Region, "Block0", true);

                CogRectangle rect = new CogRectangle((CogRectangle)sender);
                double wpitch = rect.Width / app.CurrSubstrateRecipe.column;
                double hpitch = rect.Height / app.CurrSubstrateRecipe.row;
                RecipeDisplay.InteractiveGraphics.Clear();
                for (int i = 0; i < app.CurrSubstrateRecipe.column; i++)
                {
                    for (int j = 0; j < app.CurrSubstrateRecipe.row; j++)
                    {
                        CogRectangle cellrect = new CogRectangle();
                        cellrect.LineWidthInScreenPixels = 10;
                        cellrect.SelectedLineWidthInScreenPixels = 10;
                        cellrect.Height = hpitch;
                        cellrect.Width = wpitch;
                        cellrect.X = rect.X + wpitch * i;
                        cellrect.Y = rect.Y + hpitch * j;
                        cellrect.Interactive = true;
                        cellrect.TipText = (j.ToString() + "," + i.ToString());
                        cellrect.GraphicDOFEnable = CogRectangleDOFConstants.None;
                        cellrect.GraphicDOFEnableBase = CogGraphicDOFConstants.None;
                        RecipeDisplay.InteractiveGraphics.Add(cellrect, "disp", true);
                        cellrect.Changed += cellrect_Changed;
                    }
                }
                //end of setup
            }
            catch (Exception ex) { }
        }

        private void Region_Changed(object sender, CogChangedEventArgs e)//render the block images
        {
            RecipeDisplay.InteractiveGraphics.Clear();

            CogRectangle rect = new CogRectangle((CogRectangle)app.CurrSubstrateRecipe.Blocks["0"].Region);

            double hpitch = rect.Height/ app.CurrSubstrateRecipe.column;
            double wpitch= rect.Width/ app.CurrSubstrateRecipe.row;
            for (int i = 0; i < app.CurrSubstrateRecipe.row; i++)
            {
                for (int j = 0; j < app.CurrSubstrateRecipe.column; j++)
                {
                    CogRectangle cellrect = new CogRectangle();
                    cellrect.LineWidthInScreenPixels = 10;
                    cellrect.SelectedLineWidthInScreenPixels = 10;
                    cellrect.Height = hpitch;// +app.CurrSubstrateRecipe.YAllowance;
                    cellrect.Width = wpitch;// +app.CurrSubstrateRecipe.XAllowance;
                    cellrect.X = rect.X + wpitch * i;
                    //- app.CurrSubstrateRecipe.XAllowance/2;
                    cellrect.Y = rect.Y + hpitch * j;// -app.CurrSubstrateRecipe.XAllowance / 2;

                    cellrect.Interactive = true;
                    cellrect.TipText = (j.ToString() + "," + i.ToString());
                    cellrect.GraphicDOFEnable = CogRectangleDOFConstants.None;
                    cellrect.GraphicDOFEnableBase = CogGraphicDOFConstants.None;
                    RecipeDisplay.InteractiveGraphics.Add(cellrect, "disp", true);
                    cellrect.Changed += cellrect_Changed;
                    CellList.Add(cellrect);
                }
            }
        }
        private string regiontext;
        private void cellrect_Changed(object sender, CogChangedEventArgs e)
        {
            //throw new NotImplementedException();

            //generate new region
            //cellrect.Height = hpitch + app.CurrSubstrateRecipe.YAllowance;
            //cellrect.Width = wpitch + app.CurrSubstrateRecipe.XAllowance;
            //cellrect.X = rect.X + wpitch * i
            //- app.CurrSubstrateRecipe.XAllowance / 2;
            //cellrect.Y = rect.Y + hpitch * j - app.CurrSubstrateRecipe.XAllowance / 2;
            
            CogCopyRegionTool tool = new CogCopyRegionTool();
            regiontext = ((CogRectangle)sender).TipText;
            string[] co = ((CogRectangle)sender).TipText.Split(',');
            int j = int.Parse(co[0]);
            int i = int.Parse(co[1]);
            tool.Region = new CogRectangle((CogRectangle)sender);
            ((CogRectangle)tool.Region).Height = ((CogRectangle)tool.Region).Height + app.CurrSubstrateRecipe.YAllowance;
            ((CogRectangle)tool.Region).Width= ((CogRectangle)tool.Region).Width+ app.CurrSubstrateRecipe.XAllowance;
            ((CogRectangle)tool.Region).X = ((CogRectangle)tool.Region).X - app.CurrSubstrateRecipe.XAllowance/2;
            ((CogRectangle)tool.Region).Y = ((CogRectangle)tool.Region).Y - app.CurrSubstrateRecipe.YAllowance / 2;
            tool.InputImage = RecipeDisplay.Image;
            tool.Run();
            
            TumbnailDisplay.Image = tool.OutputImage;
            TumbnailDisplay.Fit(true);
            TumbnailDisplay.StaticGraphics.Clear();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)//edit tray find function
        {
         
            frmtoolgroup frm = new frmtoolgroup();
            frm.SetSubject(tbFindTray, ((CogImage8Grey)currentimg), null, regiontext);
            frm.ShowDialog();

            //show display

            //app.TrayMgr01.fixture = (CogFixtureTool)tbFindTray.Tools["TrayFixture"];
            //app.TrayMgr01.fixture.InputImage = currentimg;
            //app.TrayMgr01.fixture.RunParams.UnfixturedFromFixturedTransform = tool.Results[0].GetPose();
            //app.TrayMgr01.fixture.Run();
            //CellList.Clear();
            //Cognex.VisionPro.ICogRecord lastRecord =
            //tool.CreateLastRunRecord();
            //this.PatternRecDisplay.Record = lastRecord.SubRecords["InputImage"];
            //PatternRecDisplay.Fit(true);
            //SetupDisplay.Image = app.TrayMgr01.fixture.OutputImage;
            //app.CurrSubstrateRecipe.Blocks["0"].Region.Interactive = true;
            //app.CurrSubstrateRecipe.Blocks["0"].Region.GraphicDOFEnable = CogRectangleDOFConstants.All;
            //SetupDisplay.InteractiveGraphics.Add(app.CurrSubstrateRecipe.Blocks["0"].Region, "Block0", true);
            //SetupDisplay.Fit(true);
            //RecipeDisplay.Image = app.TrayMgr01.fixture.OutputImage;
            //RecipeDisplay.Fit(true);
            //Region_Changed(app.CurrSubstrateRecipe.Blocks["0"].Region, null);

            //end
            string bkdirectory = @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\Block";//generate physical directory for reference
            CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.Blocks["0"].Region, bkdirectory + "0" + @"\region.vpp");
            CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.cb, @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\SearchPatternCB.vpp", typeof(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter), CogSerializationOptionsConstants.Minimum);
        }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //test run
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            try
            {
                //start search.
                if (unprocessimg == null)
                {
                    MessageBox.Show("No Image Available");
                    return;
                }
                app.TrayMgr01.ReassignProcessBlockList();
                //display region list
                //RegionList

                app.TrayMgr01.visiondebug = app.EnableVisionDebug;
                CogStopwatch sw = new CogStopwatch();
                sw.Start();
                app.TrayMgr01.TriggerSearch(unprocessimg);
                app.TrayMgr01.ProcessCompleteEvt.WaitOne(20000);
                sw.Stop();
                CogImage8Grey trayimage = new CogImage8Grey((CogImage8Grey)app.TrayMgr01.fixture.OutputImage);
                MaskDisplay.Image = trayimage;
                MaskDisplay.StaticGraphics.Clear();
                MaskDisplay.Fit(true);
                BCRcpDisplay.Image = app.TrayMgr01.IPImage;
                BCRcpDisplay.Image.SelectedSpaceName = "@\\Fixture";
                BCRcpDisplay.Fit(true);
                BCRcpDisplay.StaticGraphics.Clear();
                CogRectangle TraySearchReg = new CogRectangle(app.TrayMgr01.TraySearchReg);
                BCRcpDisplay.StaticGraphics.Add(TraySearchReg, "z");
                BCRcpDisplay.Image = app.TrayMgr01.IPImage;
                BCRcpDisplay.Fit(true);
                // BCRcpDisplay.StaticGraphics.Clear();
                MaskDisplay.InteractiveGraphics.Clear();


                List<CogCompositeShape> disrst = new List<CogCompositeShape>();
                foreach (CogCompositeShape s in app.TrayMgr01.disp)
                {
                    disrst.Add(new CogCompositeShape(s));
                }
                foreach (KeyValuePair<string, TrayInspectionRst> element in app.TrayMgr01.FinalInspectionRsts)
                {

                    MaskDisplay.InteractiveGraphics.Add(element.Value.rec, "s", true);
                    MaskDisplay.InteractiveGraphics.Add(element.Value.label, "s", true);
                }

                foreach (CogCompositeShape s in disrst)
                {
                    //s.SelectedSpaceName = @"Fixture";
                    try
                    {
                        MaskDisplay.InteractiveGraphics.Add(s, "s", true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());

                    }
                }



                //foreach (CogRectangle rec in app.TrayMgr01.BlockInpsection[0].RegionList)
                //{
                //    MaskDisplay.InteractiveGraphics.Add(rec, "disp", true);
                //    //MaskDisplay.StaticGraphics.Add(rec, "ee");
                //}
                //foreach (CogRectangle rec in app.TrayMgr01.BlockInpsection[1].RegionList)
                //{
                //    MaskDisplay.InteractiveGraphics.Add(rec, "disp", true);
                //}
                //foreach (CogRectangle rec in app.TrayMgr01.BlockInpsection[2].RegionList)
                //{
                //    MaskDisplay.InteractiveGraphics.Add(rec, "disp", true);
                //}
                double totaltime = app.TrayMgr01.overallsearchtime + app.TrayMgr01.pocketprocesstime;
                System.Windows.Forms.MessageBox.Show(" Total process time = " + totaltime.ToString());
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
        }

        public CogImage8Grey GetImage01(CogRectangle rect)
        {
            CogCopyRegionTool tool = new CogCopyRegionTool();
            tool.Region = rect;
            tool.InputImage = img01;
            tool.Run();
            return (CogImage8Grey)tool.OutputImage;
        }

        public CogImage8Grey GetImage02(CogRectangle rect)
        {
            CogCopyRegionTool tool = new CogCopyRegionTool();
            tool.Region = rect;
            tool.InputImage = img02;
            tool.Run();
            return (CogImage8Grey)tool.OutputImage;
        }

        public CogImage8Grey GetImage03(CogRectangle rect)
        {
            CogCopyRegionTool tool = new CogCopyRegionTool();
            tool.Region = rect;
            tool.InputImage = img03;
            tool.Run();
            return (CogImage8Grey)tool.OutputImage;
        }

        public CogPMAlignTool fiducialsearch00 { get; set; }

        public CogPMAlignTool fiducialsearch180 { get; set; }

        public CogPMAlignTool fiducialsearchDB { get; set; }

        public CogPMAlignTool fiducialsearchEmpty { get; set; }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
//            if (app.linescanprocess.linescan.WaitImage(10000)) MessageBox.Show("Image Acquired");
                        //fiducial search setup
            frmtoolgroup frm = new frmtoolgroup();
            frm.SetSubject(tbPartLocate, ((CogImage8Grey)TumbnailDisplay.Image), null, regiontext +","+(app.EnableVisionDebug?"1":"0"));
            frm.ShowDialog();
            //save file
            //save file
            string bkdirectory = @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\Block";//generate physical directory for reference
            CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.Blocks["0"].Region, bkdirectory + "0" + @"\region.vpp");
            CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.cb, @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\SearchPatternCB.vpp",
                typeof(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter), CogSerializationOptionsConstants.Minimum);
            app.TrayMgr01.Update();
            MessageBox.Show("Update Complete");
            //string bkdirectory = @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\Block";//generate physical directory for reference
            //CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.Blocks["0"].Region, bkdirectory + "0" + @"\region.vpp");
            //CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.cb, @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\SearchPatternCB.vpp",
            //    typeof(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter), CogSerializationOptionsConstants.Minimum);
            ////app.TrayMgr01.Update();
            //end of save file*/
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
           // CogImage8Grey img =  app.linescanprocess.linescan.GetImage();
            //SetupDisplay.Image = img;
            //off pocket setup
            frmtoolgroup frm = new frmtoolgroup();
            frm.SetSubject(tbDetectOffPocket, ((CogImage8Grey)TumbnailDisplay.Image), null, regiontext);
            frm.ShowDialog();
            //save file
            string bkdirectory = @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\Block";//generate physical directory for reference
            CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.Blocks["0"].Region, bkdirectory + "0" + @"\region.vpp");
            CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.cb, @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\SearchPatternCB.vpp",
                typeof(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter), CogSerializationOptionsConstants.Minimum);
            app.TrayMgr01.Update();
            //end of save file*/
        }

        private void DBSearchBtn(object sender, RoutedEventArgs e)
        {
            //Death Bug setup
            frmtoolgroup frm = new frmtoolgroup();
            CogImage8Grey img = (CogImage8Grey)tbPartLocate.Outputs["OutputImage"].Value;
            ICogRegion reg = (ICogRegion)tbPartLocate.Outputs["OutputRegion"].Value;
            string runparam = (string)tbPartLocate.Outputs["RstString"].Value;
            if (img == null)
            {
                frm.SetSubject(tbDeathBug, ((CogImage8Grey)TumbnailDisplay.Image), null, regiontext);
            }
            else
            {
                frm.SetSubject(tbDeathBug, img, reg, runparam);

            }
            frm.ShowDialog();
            //save file
            string bkdirectory = @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\Block";//generate physical directory for reference
            CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.Blocks["0"].Region, bkdirectory + "0" + @"\region.vpp");
            CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.cb, @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\SearchPatternCB.vpp",
                typeof(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter), CogSerializationOptionsConstants.Minimum);
            app.TrayMgr01.Update();
        }

        public CogImage8Grey unprocessimg { get; set; }

        private void Test(object sender, RoutedEventArgs e)
        {

            System.Windows.Forms.OpenFileDialog frm = new System.Windows.Forms.OpenFileDialog();
            frm.Filter = "bmp files (*.bmp)|*.bmp|All files (*.*)|*.*";
            frm.DefaultExt = "bmp";
            frm.Title = "Open Image File";
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.Cancel) return;
            CogImageFileTool ftool = new CogImageFileTool();
            try
            {
                ftool.Operator.Open(frm.FileName, CogImageFileModeConstants.Read);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Image Error");
                return;
            }
            ftool.Run();


            CogIPOneImageTool rotate = new CogIPOneImageTool();
            CogIPOneImageFlipRotate flip = new CogIPOneImageFlipRotate();
            flip.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate90Deg;
            rotate.Operators.Add(flip);

                rotate.InputImage = ftool.OutputImage;
 
            rotate.Run();
            currentimg = (CogImage8Grey)rotate.OutputImage;
            unprocessimg = new CogImage8Grey(currentimg);
            tbFindTray.Inputs["Input"].Value = currentimg;
            tbFindTray.Run();
            CogPMAlignTool tool = (CogPMAlignTool)tbFindTray.Tools["FindTray1"];
           // tool.InputImage = currentimg;
           // tool.Run();
            try
            {
                app.TrayMgr01.fixture = (CogFixtureTool)tbFindTray.Tools["TrayFixture"];
                //app.TrayMgr01.fixture.InputImage = currentimg;
                //app.TrayMgr01.fixture.RunParams.UnfixturedFromFixturedTransform = tool.Results[0].GetPose();
                //app.TrayMgr01.fixture.Run();
                /*Display Setup Image*/
                SetupDisplay.Image = app.TrayMgr01.fixture.OutputImage;
                app.CurrSubstrateRecipe.Blocks["0"].Region.Interactive = true;
                app.CurrSubstrateRecipe.Blocks["0"].Region.GraphicDOFEnable = CogRectangleDOFConstants.All;
                SetupDisplay.InteractiveGraphics.Clear();
                SetupDisplay.InteractiveGraphics.Add(app.CurrSubstrateRecipe.Blocks["0"].Region, "Block0", true);
                SetupDisplay.Fit(true);
                RecipeDisplay.Image = app.TrayMgr01.fixture.OutputImage;
                RecipeDisplay.Fit(true);
                Region_Changed(app.CurrSubstrateRecipe.Blocks["0"].Region, null);
                try
                {
                    app.CurrSubstrateRecipe.Blocks["0"].Region.Changed -= Region_Changed;
                }
                catch (Exception ex) { }
                app.CurrSubstrateRecipe.Blocks["0"].Region.Changed += Region_Changed;
                if (tool.Results.Count == 0)
                {
                    MessageBox.Show("unable to find tray, setup required");
                    return;
                }

            }
            catch (Exception ex) { }
            CellList.Clear();

            Cognex.VisionPro.ICogRecord lastRecord =
            tool.CreateLastRunRecord();
            this.PatternRecDisplay.Record = lastRecord.SubRecords["InputImage"];
            PatternRecDisplay.Fit(true);                         

            //app.linescanprocess.linescan.StartGrabImage();
            /*tmp removed on 8th March
            //testform frm = new testform(app.TrayMgr01.maskcopytool);
            //frm.ShowDialog();
            System.Windows.Forms.OpenFileDialog frm = new System.Windows.Forms.OpenFileDialog();
            frm.Filter = "bmp files (*.bmp)|*.bmp|All files (*.*)|*.*";
            frm.DefaultExt = "bmp";
            frm.Title = "Open Image File";
            frm.ShowDialog();
            CogImageFileTool ftool = new CogImageFileTool();
            ftool.Operator.Open(frm.FileName, CogImageFileModeConstants.Read);
            ftool.Run();

            currentimg = (CogImage8Grey)ftool.OutputImage;
            unprocessimg = new CogImage8Grey((CogImage8Grey)ftool.OutputImage);
            //end of default image

            CogAffineTransformTool atf_tool = (CogAffineTransformTool)tbFindTray.Tools["CogAffineTransformTool1"];// will need to provide a transform tool for both empty pocket and pattern search
            atf_tool.RunParams.ScalingY = app.CurrSubstrateRecipe.yield;
            atf_tool.RunParams.ScalingX = app.CurrSubstrateRecipe.yield;
            atf_tool.InputImage = (CogImage8Grey)ftool.OutputImage;
            atf_tool.Region = null;
            atf_tool.Run();//get txformed image
            currentimg = (CogImage8Grey)atf_tool.OutputImage;
            //test code here
            CogPMAlignTool tool = (CogPMAlignTool)tbFindTray.Tools["FindTray"];
            tool.InputImage = currentimg;
            tool.Run();
            app.TrayMgr01.fixture = (CogFixtureTool)tbFindTray.Tools["TrayFixture"];
            app.TrayMgr01.fixture.InputImage = currentimg;
            app.TrayMgr01.fixture.RunParams.UnfixturedFromFixturedTransform = tool.Results[0].GetPose();
            app.TrayMgr01.fixture.Run();
            CellList.Clear();

            Cognex.VisionPro.ICogRecord lastRecord =
            tool.CreateLastRunRecord();
            this.PatternRecDisplay.Record = lastRecord.SubRecords["InputImage"];
            PatternRecDisplay.Fit(true);
            SetupDisplay.Image = app.TrayMgr01.fixture.OutputImage;
            SetupDisplay.InteractiveGraphics.Clear();
            SetupDisplay.InteractiveGraphics.Add(app.CurrSubstrateRecipe.Blocks["0"].Region, "Block0", true);
            SetupDisplay.Fit(true);
            RecipeDisplay.Image = app.TrayMgr01.fixture.OutputImage;
            RecipeDisplay.Fit(true);
            Region_Changed(app.CurrSubstrateRecipe.Blocks["0"].Region, null);
             * */
        }

        public CogToolBlock tbPartLocate { get; set; }

        public CogToolBlock tbDetectOffPocket { get; set; }

        public CogToolBlock tbDeathBug { get; set; }

        public CogToolBlock tbFindTray { get; set; }

        private void EditParam(object sender, RoutedEventArgs e)
        {
            //app.linescanprocess.linescan.StartGrabImage();
            ParamEdit frm = new ParamEdit();
            Substrate sb = new Substrate();
            sb.XAllowance= app.CurrSubstrateRecipe.XAllowance;
            sb.YAllowance = app.CurrSubstrateRecipe.YAllowance;
            sb.Distancelower = app.CurrSubstrateRecipe.Distancelower;
            sb.Distanceupper = app.CurrSubstrateRecipe.Distanceupper;
            sb.yield = app.CurrSubstrateRecipe.yield;
            sb.Exposure = app.CurrSubstrateRecipe.Exposure;
            frm.Init(sb);
            if (frm.ShowDialog() == true)
            {
                app.CurrSubstrateRecipe.XAllowance = sb.XAllowance;
                app.CurrSubstrateRecipe.YAllowance = sb.YAllowance;
                app.CurrSubstrateRecipe.Distancelower = sb.Distancelower;
                app.CurrSubstrateRecipe.Distanceupper = sb.Distanceupper;
                app.CurrSubstrateRecipe.yield = sb.yield;
                app.CurrSubstrateRecipe.Exposure = sb.Exposure;
                //save
                XmlSerializer tserializer;
                tserializer = new XmlSerializer(typeof(Substrate));
                TextWriter wrt = new StreamWriter(@".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\traymap.xml");
                tserializer.Serialize(wrt, app.CurrSubstrateRecipe);
                app.TrayMgr01.SetRegionValue(app.CurrSubstrateRecipe.XAllowance, app.CurrSubstrateRecipe.YAllowance);
                app.linescanprocess.linescan.SetExposure(app.CurrSubstrateRecipe.Exposure);
            }
 
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            //save
            string bkdirectory = @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\Block";//generate physical directory for reference
            CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.Blocks["0"].Region, bkdirectory + "0" + @"\region.vpp");
            //CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.cb, @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\SearchPatternCB.vpp",
            //    typeof(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter), CogSerializationOptionsConstants.Minimum);
            app.TrayMgr01.Update();
        }
        private Thread GrabThread;
        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            if (btngrabimg.Content.ToString() ==  "Grab Image")
            {
                if (!app.linescanprocess.linescan.StartGrabImage())
                {
                    MessageBox.Show("Start Grab Fail");
                }
                btngrabimg.Content = "Cancel Grab";
                GrabThread = new Thread(new ThreadStart(GrabThreadfn));
                GrabThread.Start();
            }
            else
            {
                app.linescanprocess.linescan.Abort();
                btngrabimg.Content = "Grab Image";
            }

        }

        private void GrabThreadfn()
        {
            while (true)
            {
                if (app.linescanprocess.linescan.WaitImage(100))
                {
                    //image captured
                    Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
                     {
                         btngrabimg.Content = "Grab Image";
                         CogIPOneImageTool rotate = new CogIPOneImageTool();
                         CogIPOneImageFlipRotate flip = new CogIPOneImageFlipRotate();
                         flip.OperationInPixelSpace = CogIPOneImageFlipRotateOperationConstants.Rotate90Deg;
                         rotate.Operators.Add(flip);
                         rotate.InputImage = app.linescanprocess.linescan.GetImage();
                         rotate.Run();
                         currentimg = (CogImage8Grey)rotate.OutputImage;
                         unprocessimg = new CogImage8Grey(currentimg);

                         tbFindTray.Inputs["Input"].Value = unprocessimg;
                         tbFindTray.Run();
           
                         CogPMAlignTool tool = (CogPMAlignTool)tbFindTray.Tools["FindTray1"];
                         try
                         {
                             app.TrayMgr01.fixture = (CogFixtureTool)tbFindTray.Tools["TrayFixture"];
                             app.TrayMgr01.fixture.InputImage = currentimg;
                             app.TrayMgr01.fixture.RunParams.UnfixturedFromFixturedTransform = tool.Results[0].GetPose();
                             app.TrayMgr01.fixture.Run();
                             /*Display Setup Image*/
                             SetupDisplay.Image = app.TrayMgr01.fixture.OutputImage;
                             app.CurrSubstrateRecipe.Blocks["0"].Region.Interactive = true;
                             app.CurrSubstrateRecipe.Blocks["0"].Region.GraphicDOFEnable = CogRectangleDOFConstants.All;
                             //SetupDisplay.InteractiveGraphics.Add(app.CurrSubstrateRecipe.Blocks["0"].Region, "Block0", true);
                             SetupDisplay.Fit(true);
                             RecipeDisplay.Image = app.TrayMgr01.fixture.OutputImage;
                             RecipeDisplay.Fit(true);
                             Region_Changed(app.CurrSubstrateRecipe.Blocks["0"].Region, null);

                         }
                         catch (Exception ex) { }
                         CellList.Clear();

                         Cognex.VisionPro.ICogRecord lastRecord =
                         tool.CreateLastRunRecord();
                         this.PatternRecDisplay.Record = lastRecord.SubRecords["InputImage"];
                         PatternRecDisplay.Fit(true);                         
                     });
                    break;
                }
                Thread.Sleep(100);
            }
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            //fiducial search setup
            frmtoolgroup frm = new frmtoolgroup();
            frm.SetSubject(tbPartLocate, ((CogImage8Grey)BCRcpDisplay.Image), app.TrayMgr01.TraySearchReg, null);
            frm.ShowDialog();
            //save file
            string bkdirectory = @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\Block";//generate physical directory for reference
            CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.Blocks["0"].Region, bkdirectory + "0" + @"\region.vpp");
            CogSerializer.SaveObjectToFile(app.CurrSubstrateRecipe.cb, @".\Recipe\" + app.CurrSubstrateRecipe.RecipeName + @"\SearchPatternCB.vpp",
                typeof(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter), CogSerializationOptionsConstants.Minimum);
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            Button_Click_5(sender, e);
        }
    }
}