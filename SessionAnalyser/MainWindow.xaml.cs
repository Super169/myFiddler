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

namespace SessionAnalyser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // string matchAction = "BossWar.enterWar";
        // string matchAction = "BossWar.sendTroop";
        string matchAction = "BossWar.leaveWar";

        public MainWindow()
        {
            InitializeComponent();

            string sSAZInfo = Assembly.GetAssembly(typeof(Ionic.Zip.ZipFile)).FullName;
            FiddlerApplication.oSAZProvider = new DNZSAZProvider();
        }

        private void btnLoadSession_Click(object sender, RoutedEventArgs e)
        {
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
                            } else if (requestText.Contains("BossWar.enterWar"))
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
                    }
                    txtResult.Text = info + "\n";
                }

            } catch (Exception ex)
            {
                txtResult.Text = "Error:\n" + ex.Message;
            }

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
    }
}
