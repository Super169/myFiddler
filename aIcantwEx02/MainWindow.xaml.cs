using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Fiddler;
using System.Threading;
using System.Web.Helpers;

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
        static string sessionSid = "";
        static int iFiddlerPort = 8877;

        private void startFiddler()
        {
            Fiddler.FiddlerApplication.SetAppDisplayName("Icantw Capture");

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

            Fiddler.CONFIG.IgnoreServerCertErrors = false;

            FiddlerApplication.Prefs.SetBoolPref("fiddler.network.streaming.abortifclientaborts", true);

            FiddlerCoreStartupFlags oFCSF = FiddlerCoreStartupFlags.Default;
            Fiddler.FiddlerApplication.Startup(iFiddlerPort, oFCSF);

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

        public MainWindow()
        {
            InitializeComponent();

            startFiddler();

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
            switch (sAction)
            {
                case "Login.login":
                    goLogin_login();
                    break;
                case "World.citySituationDetail":
                    goWorld_citySituationDetail();
                    break;
            }

        }

        // {"act":"Login.login","body":"{\"type\":\"WEB_BROWSER\",\"loginCode\":\"<<--sid-->>\"}"}

        private void goLogin_login()
        {
            dynamic json;
            try
            {
                json = Json.Decode("{}");
                json.act = "Login.login";
                json.body = "{\"type\":\"WEB_BROWSER\",\"loginCode\":\"" + txtSId.Text + "\"}";
                txtRequest.Text = Json.Encode(json);
                sendRequest();
            }
            catch (Exception ex)
            {
                txtResponse.Text = ex.Message;
                return;
            }
        }

        // {"act":"World.citySituationDetail","sid":"<<--sid-->>","body":"{\"cityId\":<<--cityId-->>}"}

        private void goWorld_citySituationDetail()
        {
            if (txtCityId.Text.Trim() == "" )
            {
                txtResponse.Text = "<<Please provide CityId>>";
                return;
            }
            int cityId = 0;
            dynamic json;
            try
            {
                cityId = int.Parse(txtCityId.Text.Trim());
                json = Json.Decode("{}");
                json.act = "World.citySituationDetail";
                json.sid = txtSId.Text;
                json.body = "{\"cityId\":" + txtCityId.Text + "}";
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
                updateUI(oS);
            }

        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }

}
