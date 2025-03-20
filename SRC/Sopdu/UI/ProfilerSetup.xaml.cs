using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using Sopdu.ProcessApps.ImgProcessing;
using Sopdu.StripMapVision;
using Sopdu.UI.popups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for ProfilerSetup.xaml
    /// </summary>
    public partial class ProfilerSetup : Page
    {
        private MainWindow mainWindow;
        private ICogImage CurrentImage;
        public ProfilerSetup()
        {
            InitializeComponent();
        }

        public ProfilerSetup(MainWindow mainWindow)
        {
            InitializeComponent();
            MainBtnPanel.userChangedEvent+=UpdateUi;
            this.mainWindow = mainWindow;
            this.DataContext = mainWindow.mainapp;
            this.imgacqctrl.Subject = mainWindow.mainapp.cogacqfifotool;
            //axisUI.DataContext = mainWindow.mainapp.paxis.MotorAxis;
            SetupDisplayRegion();
            //mainWindow.mainapp.razor.btnruncomplete += razor_btnruncomplete;
        }

        private void UpdateUi(bool isAdminLogin)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                if (isAdminLogin)
                    this.VisionSetup.IsEnabled=true;
                else
                    this.VisionSetup.IsEnabled=true;
            });
        }

        private void razor_btnruncomplete()
        {
            //throw new NotImplementedException();
            // Application.Current.Dispatcher.BeginInvoke((Action)delegate// use begin invoke to avoid hung up
            //   {
            log.Debug("enable button");
            btnRun.IsEnabled = true;
            log.Debug("enable button complete");
            //   });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.mainapp.saveVisionRecipe();
            MessageBox.Show("Save Complete");
        }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                btnRun.IsEnabled = false;
                mainWindow.mainapp.razor.maintainance = true;
                mainWindow.mainapp.razor.SetImage(RecipeDisplay);
                mainWindow.mainapp.razor.SetSecondaryDisplay(SetupDisplay, display3D);
                //get images//
                //
                //mainWindow.mainapp.razor.imgfiletool.Run();// get input images
                display3D.Clear();
                //if (mainWindow.mainapp.razor.imgfiletool.OutputImage == null)
                //{
                //    MessageBox.Show("Image Not available");

                //    //if (btnruncomplete != null)
                //    razor_btnruncomplete();
                //    return;
                //}
                if (CurrentImage == null)
                {
                    MessageBox.Show("Image Not available");
                    razor_btnruncomplete();
                    return;
                }
                mainWindow.mainapp.razor.updateparam();
                ProfileTrayImageInfo rst = new ProfileTrayImageInfo();
                rst.trayimage = new CogImage16Range((CogImage16Range)CurrentImage);
                mainWindow.mainapp.razor.TriggerSearchm(rst);
                //mainWindow.mainapp.razor.ProcessCompleteEvt.WaitOne();
                razor_btnruncomplete();
                //mainWindow.mainapp.razor.Run();
            }
            catch (Exception ex) { log.Error(ex.ToString()); }
        }

        public void SetupDisplayRegion()
        {
            SetupDisplay.InteractiveGraphics.Clear();
            RecipeDisplay.InteractiveGraphics.Clear();
            SetupDisplay.Image = mainWindow.mainapp.razor.iptool.OutputImage;
            mainWindow.mainapp.razor.bk.Region.Interactive = true;
            mainWindow.mainapp.razor.bk.Region.GraphicDOFEnable = CogRectangleDOFConstants.All;
            SetupDisplay.InteractiveGraphics.Add(mainWindow.mainapp.razor.bk.Region, "Block0", true);
            SetupDisplay.Fit(true);
            RecipeDisplay.Image = SetupDisplay.Image;
            Region_Changed(null, null);
            RecipeDisplay.Fit(true);
            UpdateProfilerDetails();
        }

        public void UpdateProfilerDetails()
        {
            try
            {
                mainWindow.mainapp.razor.bk.Region.Changed -= Region_Changed;
            }
            catch (Exception ex) { }
            mainWindow.mainapp.razor.bk.Region.Changed += Region_Changed;
        }

        private void Region_Changed(object sender, CogChangedEventArgs e)
        {
            RecipeDisplay.InteractiveGraphics.Clear();
            mainWindow.mainapp.razor.CellDictionary.Clear();
            CogRectangle rect = new CogRectangle((CogRectangle)mainWindow.mainapp.razor.bk.Region);
            double hpitch = rect.Height / mainWindow.mainapp.razor.pocketpercolumn;
            double wpitch = rect.Width / mainWindow.mainapp.razor.pocketperrow;
            for (int i = 0; i < mainWindow.mainapp.razor.pocketperrow; i++)
            {
                for (int j = 0; j < mainWindow.mainapp.razor.pocketpercolumn; j++)
                {
                    CogRectangle cellrect = new CogRectangle();
                    cellrect.LineWidthInScreenPixels = 4;
                    cellrect.SelectedLineWidthInScreenPixels = 4;
                    cellrect.Height = hpitch - 2;// +app.CurrSubstrateRecipe.YAllowance;
                    cellrect.Width = wpitch - 2;// +app.CurrSubstrateRecipe.XAllowance;
                    cellrect.X = rect.X + wpitch * i;
                    cellrect.Y = rect.Y + hpitch * j;// -app.CurrSubstrateRecipe.XAllowance / 2;

                    cellrect.Interactive = true;
                    // cellrect.TipText = (j.ToString() + "," + i.ToString());
                    cellrect.TipText = ((mainWindow.mainapp.razor.pocketpercolumn - j - 1).ToString() + "," + (mainWindow.mainapp.razor.pocketperrow-i-1).ToString());
                    cellrect.GraphicDOFEnable = CogRectangleDOFConstants.None;
                    cellrect.GraphicDOFEnableBase = CogGraphicDOFConstants.None;
                    RecipeDisplay.InteractiveGraphics.Add(cellrect, "disp", true);
                    //cellrect.Changed += cellrect_Changed; //this is not needed for this situation
                    //mainWindow.mainapp.razor.CellList.Add(cellrect);
                    mainWindow.mainapp.razor.CellDictionary.Add(cellrect.TipText, cellrect);
                }
            }
        }

        private void cellrect_Changed(object sender, CogChangedEventArgs e)
        {
            //Not Applicable for this project
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            frmtoolgroup frm = new frmtoolgroup();
            frm.SetSubject(this.mainWindow.mainapp.razor.tb);
            frm.ShowDialog();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            frmImageFileTool frm = new frmImageFileTool();
            frm.SetSubJect(this.mainWindow.mainapp.razor.imgfiletool);
            frm.ShowDialog();
            this.mainWindow.mainapp.razor.imgfiletool.Run();
            CurrentImage = this.mainWindow.mainapp.razor.imgfiletool.OutputImage;


        }

        private void BtnCreateRecipe(object sender, RoutedEventArgs e)
        {
            //create recipe
            //open recipe creation windows
            Substrate sb = new Substrate();
            sb.column = 3; sb.row = 3; sb.numBlock = 1; sb.yield = 0.3;
            //            sb.;
            sb.RecipeName = "DEFAULT";

            sb.IFInvList = new ObservableCollection<string>();
            NewRecipeEntry frm = new NewRecipeEntry(sb);

            if (frm.ShowDialog() == true)
            {
                string strrecipefolder = AppDomain.CurrentDomain.BaseDirectory + @"\Recipe\";
                if (Directory.Exists(strrecipefolder + sb.RecipeName))
                {
                    MessageBox.Show("Recipe Insert Fail, " + sb.RecipeName + @" Exist.");
                    return;
                }

                string rdirectory = strrecipefolder + sb.RecipeName;//generate physical directory for reference
                Directory.CreateDirectory(rdirectory);
                //save block data;
                // only region is important
                string bkdir = rdirectory + @"\Block";
                //add in 2did

                //file copy
                System.IO.File.Copy(AppDomain.CurrentDomain.BaseDirectory + @"3DRecipeTemplate\3Drecipe.vpp", strrecipefolder + sb.RecipeName + @"\3Drecipe.vpp", true);
                System.IO.File.Copy(AppDomain.CurrentDomain.BaseDirectory + @"3DRecipeTemplate\razor.xml", strrecipefolder + sb.RecipeName + @"\razor.xml", true);
                System.IO.File.Copy(AppDomain.CurrentDomain.BaseDirectory + @"3DRecipeTemplate\ACQ.vpp", strrecipefolder + sb.RecipeName + @"\ACQ.vpp", true);

                //read razor file
                XmlSerializer tserializer = new XmlSerializer(typeof(Razor));
                TextReader wrt = new StreamReader(strrecipefolder + sb.RecipeName + @"\razor.xml");
                Razor razor = (Razor)tserializer.Deserialize(wrt);
                wrt.Close();
                razor.pocketpercolumn = sb.column;
                razor.pocketperrow = sb.row;
                //write razor file
                StreamWriter wrt1 = new StreamWriter(strrecipefolder + sb.RecipeName + @"\razor.xml");
                tserializer.Serialize(wrt1, razor);
                wrt.Close();
                foreach (KeyValuePair<string, StripBlock> entry in sb.Blocks)
                {
                    StripBlock bk = (StripBlock)entry.Value;
                    string bkdirectory = bkdir + bk.BlockNumber.ToString();
                    Directory.CreateDirectory(bkdirectory);
                    CogSerializer.SaveObjectToFile(bk.Region, bkdirectory + @"\region.vpp");
                }
                //save xml file

                tserializer = new XmlSerializer(typeof(Substrate));
                wrt1 = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"\Recipe\" + sb.RecipeName + @"\traymap.xml");
                tserializer.Serialize(wrt1, sb);
                wrt.Close();

                //System.IO.File.Copy(@".\Position\*.*", @".\Recipe\" + sb.RecipeName + @"\SearchPatternCB.vpp", true);
                string targetpath = AppDomain.CurrentDomain.BaseDirectory + @"\Recipe\" + sb.RecipeName;
                string sourcePath = AppDomain.CurrentDomain.BaseDirectory + @"\Positions";
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
                this.mainWindow.mainapp.RecipeList = new ObservableCollection<string>();//dont need database. host suppose to tell me which recipe to use
                string[] stringlist = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory + @"\Recipe\");
                foreach (string str in stringlist)
                {
                    string[] str11 = str.Split('\\');
                    mainWindow.mainapp.RecipeList.Add(str11[str11.Count() - 1]);
                }
            }
        }

        private void BtnLoadRecipe(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mainWindow.mainapp.razor != null)
                {
                    try
                    {
                        //mainWindow.mainapp.razor.btnruncomplete -= razor_btnruncomplete;
                    }
                    catch (Exception ex)
                    {
                        log.Debug("remove error btn event error");
                    }
                    //razor.cjm.Shutdown();
                    mainWindow.mainapp.razor.mcEvents.SetTerminate(true);
                    mainWindow.mainapp.razor.Shutdown();
                    mainWindow.mainapp.razor = null;
                }

                mainWindow.mainapp.ReadVisionFile((string)gridRecipeList.SelectedValue);
                mainWindow.mainapp.razor.Init(new helper.GenericEvents());
                SetupDisplayRegion();
                //mainWindow.mainapp.razor.btnruncomplete += razor_btnruncomplete;
                mainWindow.mainapp.imgprocessor.Load3DVisionObject(mainWindow.mainapp.razor);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Recipe Load Fail : " + ex.ToString());
                return;
            }
            MessageBox.Show((string)gridRecipeList.SelectedValue + " Load Successfully");
        }

        private void gridRecipeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void BtnRemoveRecipe(object sender, RoutedEventArgs e)
        {
            try
            {
                //DBAccess db = new DBAccess();
                string str = (string)gridRecipeList.SelectedValue;
                if (str.Length > 0)
                {
                    string rdirectory = AppDomain.CurrentDomain.BaseDirectory + @"\Recipe\" + str;//generate physical directory for reference
                    Directory.Delete(rdirectory, true);
                    this.mainWindow.mainapp.RecipeList.Remove(str);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid Recipe or No Recipe Selected");
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //display3D.Dispose();
        }

        internal void shutdown()
        {
            display3D.Dispose();
        }

        private void btnAcq_Click(object sender, RoutedEventArgs e)
        {
            Thread th = new Thread(new ThreadStart(tmpacqfunction));
            th.Start();
        }

        private void tmpacqfunction()
        {
            this.mainWindow.mainapp.cogacqfifotool.Run();
            this.CurrentImage = mainWindow.mainapp.cogacqfifotool.OutputImage;
            CogImageFileTool tool = new CogImageFileTool();
            string datetime = DateTime.Now.ToString("MMddThhmmss");
            bool exists = System.IO.Directory.Exists(@".\testimage\");
            if (!exists) System.IO.Directory.CreateDirectory(@".\testimage\");
            tool.Operator.Open(@".\testimage\" + datetime + @".idb", CogImageFileModeConstants.Write);
            tool.InputImage = mainWindow.mainapp.cogacqfifotool.OutputImage;
            tool.Run();
            tool.Operator.Close();
            MessageBox.Show("Image Acquired");

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainBtnPanel.IsAdminLogin)
                this.VisionSetup.IsEnabled=true;
            else
                this.VisionSetup.IsEnabled=false;
        }
    }
}
