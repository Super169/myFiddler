using System;
using Fiddler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Reflection;

namespace myKing
{
    public static class myFiddler
    {
        const int FIDDLER_PORT = 8899;
        static bool _sysProxy = false;
        public delegate void CallbackEventHandler(Fiddler.Session oS);
        public static event CallbackEventHandler AfterSessionComplete;

        public static void ConfigFiddler(string appName)
        {
            Fiddler.FiddlerApplication.SetAppDisplayName(appName);

            Fiddler.FiddlerApplication.OnNotification += delegate (object sender, NotificationEventArgs oNEA)
            {
                // Console.WriteLine("** NotifyUser: " + oNEA.NotifyString);
            };

            Fiddler.FiddlerApplication.Log.OnLogString += delegate (object sender, LogEventArgs oLEA)
            {
                // Console.WriteLine("** LogString: " + oLEA.LogString);
            };

            Fiddler.FiddlerApplication.AfterSessionComplete += delegate (Fiddler.Session oS)
            {
                if (AfterSessionComplete != null)
                    AfterSessionComplete(oS);
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

        public static Boolean IsStarted()
        {
            return FiddlerApplication.IsStarted();
        }

        public static Boolean IsSysProxy()
        {
            return _sysProxy;
        }

        public static void Startup(bool sysProxy = false)
        {
            FiddlerCoreStartupFlags oFCSF = FiddlerCoreStartupFlags.Default;
            if (!sysProxy) oFCSF &= ~FiddlerCoreStartupFlags.RegisterAsSystemProxy;
            _sysProxy = sysProxy;

            if (!FiddlerApplication.IsStarted()) Fiddler.FiddlerApplication.Startup(FIDDLER_PORT, oFCSF);
            Thread.Sleep(500);
        }

        public static void Shutdown()
        {
            if (FiddlerApplication.IsStarted()) FiddlerApplication.Shutdown();
        }
    }
}
