using System;
using System.Text;
using System.Windows;
using Fiddler;
using System.Threading;
using System.Web.Helpers;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

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
                    // stopFiddler();
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

        private void updateUI(Session oS)
        {
            string requestText = Encoding.UTF8.GetString(oS.requestBodyBytes);
            string responseText = Encoding.UTF8.GetString(oS.responseBodyBytes);

            dynamic jsonRequest = Json.Decode(requestText);
            string act = jsonRequest.act;
            string sid = jsonRequest.sid;
            // string cityId = jsonRequest.cityId;  // cityId is under body

            // Only update sid if empty, some action does not contain sid (e.g. Login.login)
            if (sessionSid == "")
            {
                sessionSid = sid;
                Application.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (Action)(() => txtSId.Text = sid));
            }
            /*
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtCityId.Text = cityId));
            */
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
                    info = showLogin(responseText);
                    break;
                case "Hero.getPlayerHeroList":
                    info = showHero(responseText);
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

        private void fillResponse(string responseText = "", string infoText = "")
        {
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtResponse.Text = responseText));
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtInfo.Text = infoText));
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
            string sAction = cboAction.Text.Split('|')[0].Trim();
            string sBody = "";
            switch (sAction)
            {
                case "Campaign.eliteBuyTime":
                case "Campaign.fightNext":
                case "Campaign.getLeftTimes":
                case "Campaign.getTrialsInfo":
                case "Campaign.nextEnemies":
                case "Campaign.quitCampaign":
                case "Email.openInBox":
                case "Hero.getFeastInfo":
                case "Hero.getConvenientFormations":
                case "Hero.getPlayerHeroList":
                case "Login.serverInfo":
                case "Manor.getManorInfo":
                case "Patrol.getPatrolInfo":
                case "Rank.findAllPowerRank":
                case "Shop.shopNextRefreshTime":
                case "TeamDuplicate.battleStart":
                case "TeamDuplicate.duplicateList":
                case "TeamDuplicate.teamDuplicateFreeTimes":
                case "TurnCardReward.getTurnCardRewards":
                case "World.getAllTransportingUnits":
                    goGenericRequest(sAction);
                    break;
                case "Login.login":
                    sBody = "{\"type\":\"WEB_BROWSER\",\"loginCode\":\"" + txtSId.Text + "\"}";
                    goGenericRequest(sAction, false, sBody);
                    break;
                case "World.citySituationDetail":
                    string sCityId = txtCityId.Text.Trim();
                    int iCityId = 0;
                    if (!int.TryParse(sCityId, out iCityId))
                    {
                        txtResponse.Text = "<< Please enter city Id in numeric >>";
                        txtInfo.Text = "";
                        return;
                    }

                    if ((iCityId <= 0) || (iCityId >= 130))
                    {
                        txtResponse.Text = "<< Invalid city Id >>";
                        txtInfo.Text = "";
                        return;
                    }

                    sBody = "{\"cityId\":" + txtCityId.Text + "}";
                    goGenericRequest(sAction, true, sBody);
                    break;
                case "System.ping":
                    TimeSpan t = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1);
                    Int64 jsTime = (Int64)(t.TotalMilliseconds + 0.5);
                    sBody = "{\"clientTime\":\"" + jsTime.ToString() + " \"}";
                    goGenericRequest(sAction, true, sBody);
                    break;
                case "** Retire All":
                    goTaskRetireAll();
                    break;
            }

        }

        private void goGenericRequest(string act, bool addSId = true, string body = null)
        {
            dynamic json;
            try
            {
                json = Json.Decode("{}");
                json.act = act;
                if (addSId) json.sid = txtSId.Text;
                if (body != null) json.body = body;
                txtRequest.Text = Json.Encode(json);
                sendRequest();
            }
            catch (Exception ex)
            {
                txtResponse.Text = ex.Message;
                return;
            }
        }

        private bool sendRequest()
        {
            return sendRequest(txtRequest.Text);
        }


        private bool sendRequest(string requestText)
        {
            if (oIcantwSession == null)
            {
                txtResponse.Text = "<<No session captured>>";
                return false;
            }

            try
            {
                txtResponse.Text = "";
                txtInfo.Text = "";
                string jsonString = requestText;
                byte[] requestBodyBytes = Encoding.UTF8.GetBytes(jsonString);
                oIcantwSession.oRequest["Content-Length"] = requestBodyBytes.Length.ToString();

                startFiddler(false);
                Session newSession = FiddlerApplication.oProxy.SendRequest(oIcantwSession.oRequest.headers,
                                                                            requestBodyBytes, null, OnStageChangeHandler);
            }
            catch (Exception ex)
            {
                txtResponse.Text = ex.Message;
                return false;
            }
            return true;
        }

        private void OnStageChangeHandler(object sender, StateChangeEventArgs e)
        {
            if (e.newState == SessionStates.Done)
            {
                Session oS = (Session)sender;
                updateUI(oS);
                if (taskRunning)
                {
                    Application.Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        (Action)(() => goNextTask(true)));
                }
                else stopFiddler();
            }
            else if (e.newState == SessionStates.Aborted)
            {
                if (taskRunning)
                {
                    Application.Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        (Action)(() => goNextTask(false)));
                }
                else stopFiddler();
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
                fillResponse("<< Waiting for icantw session >>");
                startFiddler();
            }
        }

        private void btnSaveSession_Click(object sender, RoutedEventArgs e)
        {
            if (oIcantwSession == null)
            {
                fillResponse("<< Session not yet captured >>");
                return;
            }
            bool bSuccess;
            Session[] sessions = { oIcantwSession };
            try
            {

                // bSuccess = Fiddler.Utilities.WriteSessionArchive(sSessionFileName, sessions, sSessionFilePwd, false);
                bSuccess = Fiddler.Utilities.WriteSessionArchive(sSessionFileName, sessions, null, false);

                if (bSuccess) fillResponse("Session saved successfully");
                else fillResponse("Fail to save the session");
            }
            catch (Exception ex)
            {
                txtResponse.Text = ex.Message;
                txtInfo.Text = "";
            }
        }

        private void btnLoadSession_Click(object sender, RoutedEventArgs e)
        {
            Session[] sessions = null;
            try
            {

                sessions = Fiddler.Utilities.ReadSessionArchive(sSessionFileName, false);
                if (sessions == null)
                {
                    fillResponse("Fail reading session file");
                }
                else if (sessions.Length == 0)
                {
                    fillResponse("Session file is empty");
                }
                else
                {
                    oIcantwSession = sessions[0];
                    sessionSid = ""; // reset sid
                    updateUI(oIcantwSession);
                }
            }
            catch (Exception ex)
            {
                fillResponse(ex.Message);
            }

        }

        private void btnJson_Click(object sender, RoutedEventArgs e)
        {
            if (txtRequest.Text.Trim() == "") fillResponse("<< Please enter JSON string as request >>");
            try
            {
                dynamic jsonRequest = Json.Decode(txtRequest.Text.Trim());
                fillResponse("Conversion to JSON: Success\n\n" + jsonToString(jsonRequest));
            }
            catch (Exception ex)
            {
                fillResponse("Conversion to JSON: FAILED!\n\n" + ex.Message);
            }

            Window1 w = new Window1();
            w.Show();

        }

        private string jsonToString(dynamic json)
        {
            string jString = "";

            foreach (Object o in json)
            {
                if (o is KeyValuePair<string, object>)
                {
                    KeyValuePair<string, object> x = (KeyValuePair<string, object>)o;
                    if (x.Value is string)
                    {
                        jString += string.Format("{0} : {1}\n", x.Key, x.Value);
                    }
                    else if (x.Value is DynamicJsonObject)
                    {
                        // contain another JSON object
                        jString += string.Format("****** {0} : Json Object - start\n", x.Key);
                        jString += jsonToString(x.Value);
                        jString += string.Format("****** {0} : Json Object - end\n", x.Key);
                    }
                    else if (x.Value is DynamicJsonArray)
                    {
                        jString += string.Format("------ {0} : Json Array - start\n", x.Key);

                        foreach (var arrayValue in (DynamicJsonArray)x.Value)
                        {
                            if (arrayValue is string)
                            {
                                jString += string.Format("{0}\n", arrayValue);
                            }
                            else if (arrayValue is DynamicJsonObject)
                            {
                                jString += jsonToString(arrayValue);
                            }
                            else
                            {
                                jString += string.Format("<< {0} >>\n", arrayValue.ToString());
                            }
                        }
                        // jString += jsonToString(x.Value);
                        jString += string.Format("------ {0} : Json Array - end\n", x.Key);
                    }
                    else
                    {
                        jString += String.Format("{0} : << {1} >>\n", x.Key, x.Value.ToString());
                    }

                }
                else
                {
                    jString += String.Format("** Invalid entry: {0}\n", o.ToString());
                }
                KeyValuePair<string, object> j = (KeyValuePair<string, object>)o;
            }

            return jString;
        }

    }

}
