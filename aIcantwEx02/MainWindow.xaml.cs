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


            dynamic json = Json.Decode(requestText);
            string sid = json.sid;
            string cityId = json.cityId;

            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtSId.Text = sid));
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

        private void btnCityInfo_Click(object sender, RoutedEventArgs e)
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


    }

}
