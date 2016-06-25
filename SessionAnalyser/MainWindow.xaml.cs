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
using System.Runtime.Serialization.Formatters.Binary;

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
        List<MyRecord> myList = new List<MyRecord>();

        public MainWindow()
        {
            InitializeComponent();

            string sSAZInfo = Assembly.GetAssembly(typeof(Ionic.Zip.ZipFile)).FullName;
            FiddlerApplication.oSAZProvider = new DNZSAZProvider();

            // Load myRecord if any
            loadData();
        }

        private void btnLoadSession_Click(object sender, RoutedEventArgs e)
        {
            goAnalyse();
            MessageBox.Show("Record loaded");
        }

        private void goAnalyse()
        {
            string sDir = Directory.GetCurrentDirectory();
            goCheckDirectory(sDir);
        }

        private void goCheckDirectory(string sDir)
        {
            List<string> files = new List<String>();
            try
            {
                foreach (string f in Directory.GetFiles(sDir, "*.saz"))
                {
                    goCheck(f);
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    goCheckDirectory(d);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                    if ((info != null) && (info != "")) txtResult.Text += info;
                }

            }
            catch (Exception ex)
            {
                txtResult.Text += "Error:\n" + ex.Message;
            }

        }

        public static string CleanUpResponse(string responseText, int minLength = 7)
        {
            if (responseText == null) return null;

            // No need to clearn up for short return
            if (responseText.Length < 20 && responseText.StartsWith("{")) return responseText;

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
                    // The default 7 is just a rough estimate based on previous catpured packets
                    if (data[i].Length >= minLength) jsonString += data[i];
                }
            }
            return jsonString;
        }

        private string ActionAnalyser(Session oS)
        {
            string returnVal = null;
            string requestText = Encoding.UTF8.GetString(oS.requestBodyBytes);
            string responseText = Encoding.UTF8.GetString(oS.responseBodyBytes);
            responseText = CleanUpResponse(responseText);
            try
            {
                dynamic json = Json.Decode(requestText);
                dynamic jresponse = Json.Decode(responseText);
                string action = json["act"];
                string style = jresponse["style"];
                string prompt = jresponse["prompt"];
                if (action != null) action = action.Trim();
                if ((action != null) && (action != ""))
                {
                    MyRecord rec = myList.Find(x => ((x.action == action) && (x.style == style) && (x.prompt == prompt)));
                    if (rec == null)
                    {
                        myList.Add(new MyRecord()
                        {
                            action = action,
                            style = style,
                            prompt = prompt,
                            requestBody = requestText,
                            responseBody = responseText
                        });
                        returnVal = string.Format("{0} # {1} # {2} # {3}\n", action, style, prompt, requestText);
                    }
                }
            }
            catch { }
            return returnVal;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Stream stream = File.Open("data.bin", FileMode.Create);
                BinaryFormatter bin = new BinaryFormatter();
                bin.Serialize(stream, myList);
                stream.Close();
                MessageBox.Show("Record saved");
            }
            catch { }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            loadData();
        }

        private void loadData()
        {
            try
            {
                if (File.Exists("data.bin"))
                {
                    Stream stream = File.Open("data.bin", FileMode.Open);
                    BinaryFormatter bin = new BinaryFormatter();
                    myList = (List<MyRecord>)bin.Deserialize(stream);
                    stream.Close();

                    string data = "";
                    foreach (MyRecord rec in myList)
                    {
                        data += string.Format("{0} # {1} # {2} # {3}\n", rec.action, rec.style, rec.prompt, rec.requestBody);
                    }
                    txtResult.Text = data;
                    MessageBox.Show("Record loaded");
                }
            }
            catch { }
        }
    }
}
