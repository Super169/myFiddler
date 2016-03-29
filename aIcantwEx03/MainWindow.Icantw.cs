using System;
using System.Text;
using System.Windows;
using Fiddler;
using System.Threading;
using System.Web.Helpers;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

namespace aIcantwEx03
{
    partial class MainWindow
    {
        static string sIcantwHost = "icantw.com";
        static string sIcantwPath = "/m.do";
        static Session oIcantwSession = null;
        static int iFiddlerPort = 8877;
        static string sCurrentFolder = System.IO.Directory.GetCurrentDirectory();
        static string sSessionFileName = sCurrentFolder + "\\icantw.saz";
        static string sessionSid = "";

        #region "Fiddler Related"

        private void configFiddler()
        {
            Fiddler.FiddlerApplication.SetAppDisplayName("Icantw Capture");

            Fiddler.FiddlerApplication.OnNotification += delegate (object sender, NotificationEventArgs oNEA)
            {
                Console.WriteLine("** NotifyUser: " + oNEA.NotifyString);
            };

            Fiddler.FiddlerApplication.Log.OnLogString += delegate (object sender, LogEventArgs oLEA)
            {
                Console.WriteLine("** LogString: " + oLEA.LogString);
            };

            Fiddler.FiddlerApplication.AfterSessionComplete += delegate (Fiddler.Session oS)
            {
                string hostname = oS.hostname.ToLower();
                if (hostname.Contains(sIcantwHost) && oS.PathAndQuery.Equals(sIcantwPath))
                {
                    if (oIcantwSession == null)
                    {
                        oIcantwSession = oS;
                        updateUI(oS);
                    }
                }
            };

            #region "SAZ Support"

            string sSAZInfo = Assembly.GetAssembly(typeof(Ionic.Zip.ZipFile)).FullName;

            DNZSAZProvider.fnObtainPwd = () =>
            {
                Console.WriteLine("Enter the password (or just hit Enter to cancel):");
                string sResult = Console.ReadLine();
                Console.WriteLine();
                return sResult;
            };

            FiddlerApplication.oSAZProvider = new DNZSAZProvider();

            #endregion "SAZ Support"


            Fiddler.CONFIG.IgnoreServerCertErrors = false;

            FiddlerApplication.Prefs.SetBoolPref("fiddler.network.streaming.abortifclientaborts", true);

        }

        private void startFiddler(bool useProxy = true)
        {
            FiddlerCoreStartupFlags oFCSF = FiddlerCoreStartupFlags.Default;
            if (!useProxy) oFCSF &= ~FiddlerCoreStartupFlags.RegisterAsSystemProxy;

            if (!FiddlerApplication.IsStarted()) Fiddler.FiddlerApplication.Startup(iFiddlerPort, oFCSF);
            Thread.Sleep(500);
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => btnCaptureSession.Content = "Stop"));
        }

        private void stopFiddler()
        {
            if (FiddlerApplication.IsStarted()) Fiddler.FiddlerApplication.Shutdown();
            Thread.Sleep(500);
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => btnCaptureSession.Content = "Capture"));
        }

        #endregion "Fiddler Related"

 
        private void UpdateRequestInfo(requestReturnObject rro)
        {
            if (rro.success)
            {
                string requestText = Encoding.UTF8.GetString(rro.oS.requestBodyBytes);
                string responseText = Encoding.UTF8.GetString(rro.oS.responseBodyBytes);
                txtResponse.Text = responseText;
            }
            else
            {
                txtResponse.Text = rro.msg;
            }

        }


    }  // partial class MainWindow
}
