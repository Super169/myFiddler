using Fiddler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Windows;

namespace myKing
{
    public partial class MainWindow : Window
    {
        const string ICANTW_HOST = "icantw.com";
        const string ICANTW_PATH = "/m.do";


        void AfterSessionCompleteHandler(Fiddler.Session oS)
        {
            string hostname = oS.hostname.ToLower();

            if (hostname.Contains(ICANTW_HOST) && oS.PathAndQuery.Equals(ICANTW_PATH))
            {

                string requestText = Encoding.UTF8.GetString(oS.requestBodyBytes);
                string responseText = Encoding.UTF8.GetString(oS.responseBodyBytes);

                dynamic jsonRequest = Json.Decode(requestText);
                string act = jsonRequest.act;
                string sid = jsonRequest.sid;

                if (sid == null) return;

                bool accountExists = false;
                GameAccount oGA = null;
                lock (accountsLocker)
                {
                    foreach (GameAccount ac in accounts)
                    {
                        if (accountExists = (ac.Sid == sid)) break;
                    }
                    if (!accountExists)
                    {
                        try
                        {
                            oGA = new GameAccount() { Sid = sid };
                            accounts.Add(oGA);

                        } catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }

                if (oGA != null)
                {
                    string oGAsid = oGA.Sid;
                    myKingInterface.getLogin_login(oS, sid);
                }
            }
        }

        private void btnDetect_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Detect running accounts in current computer using FiddlerCore
            if (!myFiddler.IsStarted())
            {
                myFiddler.AfterSessionComplete += AfterSessionCompleteHandler;
                myFiddler.ConfigFiddler("IcanTW");
                myFiddler.Startup(true);
            }


/*
            switch (accounts.Count)
            {
                case 0:
                    accounts.Add(new GameAccount() { Server = "S44 群英會盟", NickName = "超級一六九", CorpsName = "江左聯盟", Level = 91 });
                    break;
                case 1:
                    accounts.Add(new GameAccount() { Server = "S45 眾志成城", NickName = "無名無姓", CorpsName = "", Level = 53 });
                    break;
                case 2:
                    accounts.Add(new GameAccount() { Server = "S46 爭霸天下", NickName = "怕死的水子遠", CorpsName = "九輪燎原", Level = 89 });
                    break;
            }
*/
        }
    }
}
