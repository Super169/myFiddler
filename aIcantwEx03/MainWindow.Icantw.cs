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

        private void goAction(string sAction)
        {
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
                case "Manor.decreeInfo":
                case "Manor.getManorInfo":
                case "Patrol.getPatrolInfo":
                case "Rank.findAllPowerRank":
                case "Shop.shopNextRefreshTime":
                case "TeamDuplicate.battleStart":
                case "TeamDuplicate.duplicateList":
                case "TeamDuplicate.teamDuplicateFreeTimes":
                case "TurnCardReward.getTurnCardRewards":
                case "World.getAllTransportingUnits":
                case "World.worldSituation":
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
                case "** Tester":
                    goTaskTester();
                    break;


            }

        }


        private requestReturnObject sendRequest()
        {
            requestReturnObject rro = sendRequest(txtRequest.Text);
            UpdateRequestInfo(rro);
            return rro;
        }


        private bool goGenericRequest(string act, bool addSId = true, string body = null)
        {
            dynamic json;
            try
            {
                json = Json.Decode("{}");
                json.act = act;
                if (addSId) json.sid = txtSId.Text;
                if (body != null) json.body = body;
                txtRequest.Text = Json.Encode(json);
                return sendRequest().success;
            }
            catch (Exception ex)
            {
                txtResponse.Text = ex.Message;
                return false;
            }
        }

        // UpdateRequestInfo could be able to update UI directly
        private void UpdateRequestInfo(requestReturnObject rro)
        {
            if (rro.success)
            {
                string requestText = Encoding.UTF8.GetString(rro.oS.requestBodyBytes);
                string responseText = Encoding.UTF8.GetString(rro.oS.responseBodyBytes);
                txtResponse.Text = responseText;

                dynamic jsonRequest = Json.Decode(requestText);
                string act = jsonRequest.act;
                string sid = jsonRequest.sid;

                if (sessionSid == "")
                {
                    // Special case in capturing
                    if (sid != "")
                    {
                        sessionSid = sid;
                        txtSId.Text = sid;
                    }
                }

                txtRequest.Text = requestText;
                txtResponse.Text = responseText;

                string info = "";
                switch (act)
                {
                    case "Login.login":
                        info = showLogin(responseText);
                        break;
                    case "Hero.getPlayerHeroList":
                        info = showHero(responseText);
                        break;
                    case "World.worldSituation":
                        info = showWorldSituation(responseText);
                        break;
                }

                txtInfo.Text = info;
            }
            else
            {
                txtResponse.Text = rro.msg;
            }
        }


    }  // partial class MainWindow
}
