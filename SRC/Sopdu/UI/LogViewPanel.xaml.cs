
using Sopdu.helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace Sopdu.UI
{
    /// <summary>
    /// Interaction logic for LogViewPanel.xaml
    /// </summary>
    public partial class LogViewPanel : Page
    {
        //private List<LogEntry> _Entries = new List<LogEntry>();

        //public List<LogEntry> Entries
        //{
        //    get { return _Entries; }
        //    set { _Entries = value; }
        //}

        public LogViewPanel(MainWindow mainWindow)
        {
            InitializeComponent();
            this.DataContext = mainWindow.mainapp.pMaster;
            imageError.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, Int32Rect.Empty, null);
            imageInfo.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Information.Handle, Int32Rect.Empty, null);
            imageWarn.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Warning.Handle, Int32Rect.Empty, null);
            imageDebug.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Question.Handle, Int32Rect.Empty, null);
        }

        private void buttonFindNext_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
        }

        private void textBoxFind_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void buttonFindPrevious_Click(object sender, RoutedEventArgs e)
        {
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //get context
            ProcessMaster pmaster = ((ListView)sender).DataContext as ProcessMaster;
            try
            {
                pmaster.ErrorDisplayMsg = ((CMsgClass)listView1.SelectedItem).Msg;
            }
            catch (Exception ex) { }
        }

        private void listView1_Drop(object sender, DragEventArgs e)
        {
        }

        private void listView1_Initialized(object sender, EventArgs e)
        {
            //LogEntry logentry = new LogEntry();
            //logentry.Item = 1;
            //logentry.Image = LogEntry.Images(LogEntry.IMAGE_TYPE.ERROR);
            //logentry.Message = "This is a test image";
            //logentry.MachineName = "machinename";
            //logentry.HostName = "hostname";
            //logentry.UserName = "username";
            //logentry.App = "Apps";
            //logentry.Throwable = "throwable";
            //logentry.Class = "class";
            //logentry.Method = "method";
            //logentry.File = "file";
            //logentry.Line = "line";
            //Entries.Add(logentry);
            //   this.listView1.ItemsSource = Entries;
        }
    }
}