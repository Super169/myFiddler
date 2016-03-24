using System;
using System.Text;
using System.Windows;
using Fiddler;
using System.Threading;
using System.Web.Helpers;
using System.Reflection;

namespace aIcantwEx02
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static string sIcantwHost = "icantw.com";
        static string sIcantwPath = "/m.do";
        static Session oIcantwSession = null;
        static int iFiddlerPort = 8877;
        static string sCurrentFolder = System.IO.Directory.GetCurrentDirectory();
        static string sSessionFileName = sCurrentFolder + "\\icantw.saz";
        static string sessionSid = "";

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
//                    Fiddler.FiddlerApplication.Shutdown();
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


        private void startFiddler()
        {
            if (!FiddlerApplication.IsStarted()) Fiddler.FiddlerApplication.Startup(iFiddlerPort, FiddlerCoreStartupFlags.Default);
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

        private void updateUI(Session oS)
        {
            string requestText = Encoding.UTF8.GetString(oS.requestBodyBytes);
            string responseText = Encoding.UTF8.GetString(oS.responseBodyBytes);

            dynamic jsonRequest = Json.Decode(requestText);
            dynamic jsonResponse = Json.Decode(responseText);
            string act = jsonRequest.act;
            string sid = jsonRequest.sid;
            string cityId = jsonRequest.cityId;

            // Only update sid if empty, some action does not contain sid (e.g. Login.login)
            if (sessionSid == "")
            {
                sessionSid = sid;
                Application.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (Action)(() => txtSId.Text = sid));
            }
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtCityId.Text = cityId));

            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtRequest.Text = requestText));
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtResponse.Text = responseText));
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => btnSend.IsEnabled = true));

            string info = "";
            switch (act)
            {
                case "Login.login":
                    info = jsonResponse.serverTitle + " : " + jsonResponse.nickname;
                    break;
            }
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtInfo.Text = info));


        }

        private void clearSession()
        {
            oIcantwSession = null;
            sessionSid = "";
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtSId.Text = ""));
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtRequest.Text = ""));
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtResponse.Text = ""));
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtInfo.Text = ""));
        }

        public MainWindow()
        {
            InitializeComponent();

            configFiddler();

        }

        private void winClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Fiddler.FiddlerApplication.Shutdown();
            Thread.Sleep(500);
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            sendRequest();
        }
        
        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            string sAction = cboAction.Text;
            string sBody = "";
            switch (sAction)
            {
                case "Login.serverInfo":
                case "Patrol.getPatrolInfo":
                case "Rank.findAllPowerRank":
                    goGenericRequest(sAction);
                    break;
                case "Login.login":
                    sBody = "{\"type\":\"WEB_BROWSER\",\"loginCode\":\"" + txtSId.Text + "\"}";
                    goGenericRequest(sAction, false, sBody);
                    break;
                case "World.citySituationDetail":
                    sBody = "{\"cityId\":" + txtCityId.Text + "}";
                    goGenericRequest(sAction, true, sBody);
                    break;
                case "System.ping":
                    TimeSpan t = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1);
                    Int64 jsTime = (Int64) (t.TotalMilliseconds + 0.5);
                    sBody = "{\"clientTime\":\"" + jsTime.ToString() +" \"}";
                    goGenericRequest(sAction, true, sBody);
                    break;
            }

        }

        private void goGenericRequest(string act, bool addSId = true,  string body = "")
        {
            dynamic json;
            try
            {
                json = Json.Decode("{}");
                json.act = act;
                if (addSId) json.sid = txtSId.Text;
                if (body != null)   json.body = body;
                txtRequest.Text = Json.Encode(json);
                sendRequest();
            }
            catch (Exception ex)
            {
                txtResponse.Text = ex.Message;
                return;
            }
        }

        private void sendRequest()
        {
            if (oIcantwSession == null)
            {
                txtResponse.Text = "<<No session captured>>";
                return;
            }

            try
            {
                txtResponse.Text = "";
                txtInfo.Text = "";
                string jsonString = txtRequest.Text;
                byte[] requestBodyBytes = Encoding.UTF8.GetBytes(jsonString);
                oIcantwSession.oRequest["Content-Length"] = requestBodyBytes.Length.ToString();

                startFiddler();
                Session newSession = FiddlerApplication.oProxy.SendRequest(oIcantwSession.oRequest.headers,
                                                                            requestBodyBytes, null, OnStageChangeHandler);
            }
            catch (Exception ex)
            {
                txtResponse.Text = ex.Message;
            }
        }


        private void OnStageChangeHandler(object sender, StateChangeEventArgs e)
        {
            if (e.newState == SessionStates.Done)
            {
                Session oS = (Session)sender;
                stopFiddler();
                updateUI(oS);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnCaptureSession_Click(object sender, RoutedEventArgs e)
        {
            if (FiddlerApplication.IsStarted()) stopFiddler();
            else
            {
                clearSession();
                startFiddler();
            }
        }

        private void btnSaveSession_Click(object sender, RoutedEventArgs e)
        {
            if (oIcantwSession == null)
            {
                txtResponse.Text = "<< Session not yet captured >>";
                txtInfo.Text = "";
                return;
            }
            bool bSuccess;
            Session[] sessions = { oIcantwSession };
            try
            {
                startFiddler();

                // bSuccess = Fiddler.Utilities.WriteSessionArchive(sSessionFileName, sessions, sSessionFilePwd, false);
                bSuccess = Fiddler.Utilities.WriteSessionArchive(sSessionFileName, sessions, null, false);

                if (bSuccess)
                {
                    txtResponse.Text = "Session saved successfully";
                } else
                {
                    txtResponse.Text = "Fail to save the session";
                }
                txtInfo.Text = "";
            }
            catch (Exception ex)
            {
                txtResponse.Text = ex.Message;
                txtInfo.Text = "";
            } finally
            {
                stopFiddler();
            }

        }

        private void btnLoadSession_Click(object sender, RoutedEventArgs e)
        {
            Session[] sessions = null;
            try
            {
                startFiddler();

                sessions = Fiddler.Utilities.ReadSessionArchive( sSessionFileName, false);
                if (sessions == null)
                {
                    txtResponse.Text = "Fail reading session file";

                } else if (sessions.Length == 0)
                {
                    txtResponse.Text = "Session file is empty";

                } else
                {
                    oIcantwSession = sessions[0];
                    sessionSid = ""; // reset sid
                    updateUI(oIcantwSession);
                }
                txtInfo.Text = "";
            }
            catch (Exception ex)
            {
                txtResponse.Text = ex.Message;
                txtInfo.Text = "";
            } finally
            {
                stopFiddler();
            }

        }
    }

}
