using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Sopdu
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public SplashScreen()
        {
            InitializeComponent();
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            lassmebly.Content = "File ver : " + version + "   Assembly ver :" + assembly.GetName().Version.ToString();
            log.Debug("Start up");
            log.Debug(lassmebly.Content);
        }
    }

    /// <summary>
    /// Message listener, singlton pattern.
    /// Inherit from DependencyObject to implement DataBinding.
    /// </summary>
    public class MessageListener : DependencyObject
    {
        /// <summary>
        ///
        /// </summary>
        private static MessageListener mInstance;

        /// <summary>
        ///
        /// </summary>
        private MessageListener()
        {
        }

        /// <summary>
        /// Get MessageListener instance
        /// </summary>
        public static MessageListener Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = new MessageListener();
                return mInstance;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public void ReceiveMessage(string message)
        {
            Message = message;
            Debug.WriteLine(Message);
            DispatcherHelper.DoEvents();
        }

        /// <summary>
        /// Get or set received message
        /// </summary>
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        /// <summary>
        ///
        /// </summary>
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(MessageListener), new UIPropertyMetadata(null));
    }

    /// <summary>
    /// Helper to show or close given splash window
    /// </summary>
    public static class Splasher
    {
        /// <summary>
        ///
        /// </summary>
        private static Window mSplash;

        /// <summary>
        /// Get or set the splash screen window
        /// </summary>
        public static Window Splash
        {
            get
            {
                return mSplash;
            }
            set
            {
                mSplash = value;
            }
        }

        /// <summary>
        /// Show splash screen
        /// </summary>
        public static void ShowSplash()
        {
            if (mSplash != null)
            {
                mSplash.Show();
            }
        }

        /// <summary>
        /// Close splash screen
        /// </summary>
        public static void CloseSplash()
        {
            if (mSplash != null)
            {
                mSplash.Close();

                if (mSplash is IDisposable)
                    (mSplash as IDisposable).Dispose();
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    public static class DispatcherHelper
    {
        /// <summary>
        /// Simulate Application.DoEvents function of <see cref=" System.Windows.Forms.Application"/> class.
        /// </summary>
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrames), frame);

            try
            {
                Dispatcher.PushFrame(frame);
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        private static object ExitFrames(object frame)
        {
            ((DispatcherFrame)frame).Continue = false;

            return null;
        }
    }
}