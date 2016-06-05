using Fiddler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.Web.Helpers;
using System.IO;

namespace SessionAnalyser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        // string matchAction = "BossWar.enterWar";
        // string matchAction = "BossWar.sendTroop";

        List<string> actionList = new List<string>();

        public MainWindow()
        {
            InitializeComponent();

            string sSAZInfo = Assembly.GetAssembly(typeof(Ionic.Zip.ZipFile)).FullName;
            FiddlerApplication.oSAZProvider = new DNZSAZProvider();
        }

        public static string CleanUpResponse(string responseText)
        {
            if (responseText == null) return null;

            string jsonString = null;

            string[] data = responseText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < data.Length; i++)
            {
                if (jsonString == null)
                {
                    // Not yet started, JSON string will start will "{", ignore all others
                    if (data[i].StartsWith("{")) jsonString = data[i];
                }
                else
                {
                    // study studying the behavious, it seesm that the dummy row has only few characters (hexdecimal value?), but not exactly the length
                    if (data[i].Length > 6) jsonString += data[i];
                }
            }

            return jsonString;
        }

        private void btnLoadSession_Click(object sender, RoutedEventArgs e)
        {
            /*
            try
            {
                 string sCurrentFolder = System.IO.Directory.GetCurrentDirectory();
                 string sSessionFileName = sCurrentFolder + "\\a.saz";

                Session[] sessions = Fiddler.Utilities.ReadSessionArchive(sSessionFileName, false);
                if (sessions == null)
                {
                    txtResult.Text = "No session found";
                } else
                {

                    string info = "";
                    foreach (Session oS in sessions)
                    {
                        // info += BossWarAnalyser(oS);
                        // info += ArcheryAnalyser(oS);
                        info += ActionAnalyser(oS);
                    }
                    txtResult.Text = info + "\n";
                }

            } catch (Exception ex)
            {
                txtResult.Text = "Error:\n" + ex.Message;
            }
            */
            goAnalyse();

        }

        private void goAnalyse()
        {
            string[] fileEntries = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.saz");
            foreach(string fileName in fileEntries)
            {
                goCheck(fileName);
            }

        }

        private void goCheck(string fileName)
        {
            try
            {
                // string sCurrentFolder = System.IO.Directory.GetCurrentDirectory();
                // string sSessionFileName = sCurrentFolder + "\\a.saz";

                Session[] sessions = Fiddler.Utilities.ReadSessionArchive(fileName, false);
                if (sessions == null)
                {
                }
                else
                {
                    string info = "";
                    foreach (Session oS in sessions)
                    {
                        info += ActionAnalyser(oS);
                    }
                    txtResult.Text += info + "\n";
                }

            }
            catch (Exception ex)
            {
                txtResult.Text += "Error:\n" + ex.Message;
            }

        }

        private string ActionAnalyser(Session oS)
        {
            string requestText = Encoding.UTF8.GetString(oS.requestBodyBytes);
            try
            {
                dynamic json = Json.Decode(requestText);
                string action = json["act"];
                if (!actionList.Contains(action))
                {
                    actionList.Add(action);
                    return action + " # " + requestText + "\n";
                }
            }
            catch { }

            return "";
        }

        private string BossWarAnalyser(Session oS)
        {
            string info = "";
            string requestText = Encoding.UTF8.GetString(oS.requestBodyBytes);
            if (requestText.Contains("BossWar.enterWar") || requestText.Contains("BossWar.sendTroop") || requestText.Contains("BossWar.leaveWar"))
            {
                info += oS.Timers.ClientBeginRequest.ToString("hh:mm:ss") + " | ";
                string responseText = Encoding.UTF8.GetString(oS.responseBodyBytes);
                dynamic json = Json.Decode(responseText);
                if (requestText.Contains("BossWar.sendTroop"))
                {
                    info += "sendTroop | " + responseText;
                }
                else if (requestText.Contains("BossWar.leaveWar"))
                {
                    info += "leaveWar | " + responseText;
                }
                else if (requestText.Contains("BossWar.enterWar"))
                {
                    info += "enterWar | ";
                    if (json != null)
                    {
                        info += string.Format("{0} : {1}", json.sendCount, json.inspireTime);
                        if (json.bossInfo != null) info += string.Format(" : {0}", json.bossInfo.hpp);
                    }
                }
                info += "\n";
            }
            return info;
        }

        private string ArcheryAnalyser(Session oS)
        {
            string info = "";
            string requestText = Encoding.UTF8.GetString(oS.requestBodyBytes);
            if (requestText.Contains("Archery.shoot") || requestText.Contains("Archery.getArcheryInfo"))
            {
                info += oS.Timers.ClientBeginRequest.ToString("HH:mm:ss") + " | ";
                string responseText = Encoding.UTF8.GetString(oS.responseBodyBytes);
                dynamic json = Json.Decode(responseText);
                if (requestText.Contains("Archery.getArcheryInfo"))
                {
                    info += "INFO | ";
                    if ((json != null) && (json.wind != null))
                    {
                        info += json.wind.ToString();
                    }
                    else
                    {
                        info += "Missing";
                    }
                } else if (requestText.Contains("Archery.shoot"))
                {
                    dynamic jRequest = Json.Decode(requestText);
                    if (jRequest != null && jRequest.body != null)
                    {
                        dynamic jBody = Json.Decode(jRequest.body);
                        if (jBody != null) 
                        info += string.Format("{0} | {1}", jBody.x, jBody.y);

                        if ((json != null) && (json.x != null) && (json.y != null) && (json.ring != null) && (json.nWind != null))
                        {
                            info += string.Format(" | {0} | {1} | {2} | {3}", json.x, json.y, json.ring, json.nWind);
                        }
                    }
                }
                info += "\n";
            }
            return info;
        }

        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string sCurrentFolder = System.IO.Directory.GetCurrentDirectory();
                string sSessionFileNameA = sCurrentFolder + "\\a.saz";
                string sSessionFileNameB = sCurrentFolder + "\\b.saz";

                Session[] sessionsA = Fiddler.Utilities.ReadSessionArchive(sSessionFileNameA, false);
                Session[] sessionsB = Fiddler.Utilities.ReadSessionArchive(sSessionFileNameB, false);
                if (sessionsA == null)
                {
                    txtResult.Text = "Session A not found";
                    return;
                }

                if (sessionsB == null)
                {
                    txtResult.Text = "Session B not found";
                    return;
                }

                HTTPRequestHeaders ha = sessionsA[0].oRequest.headers;
                HTTPRequestHeaders hb = sessionsB[0].oRequest.headers;

                if (ha == null)
                {
                    txtResult.Text = "Session A has no header";
                    return;
                }

                if (hb == null)
                {
                    txtResult.Text = "Session B has no header";
                    return;

                }


                /*
                    string jsonString = requestText;
                    byte[] requestBodyBytes = Encoding.UTF8.GetBytes(jsonString);
                    oIcantwSession.oRequest["Content-Length"] = requestBodyBytes.Length.ToString();

                    startFiddler(false);
                    rro.oS = FiddlerApplication.oProxy.SendRequestAndWait(oIcantwSession.oRequest.headers,
                                                                                   requestBodyBytes, null, OnStageChangeHandler);
                */




            }
            catch (Exception ex)
            {
                txtResult.Text = "Error:\n" + ex.Message;
            }


        }
    }
}
