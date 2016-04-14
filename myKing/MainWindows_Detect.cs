using Fiddler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Windows;
using static myKing.myKingInterface;

namespace myKing
{
    public partial class MainWindow : Window
    {
        const string ICANTW_HOST = "icantw.com";
        const string ICANTW_PATH = "/m.do";

        enum GameStatus
        {
            Idle, DetectAccount, Waiting
        }

        GameStatus gameStatus = GameStatus.Idle;

        void SetGameStatus(GameStatus newStatus)
        {
            this.gameStatus = newStatus;
            switch (newStatus)
            {
                case GameStatus.Idle:
                    btnDetect.Content = "偵測帳戶";
                    break;
                case GameStatus.DetectAccount:
                    btnDetect.Content = "停止偵測";
                    break;

            }
        }

        void UpdateAccountList()
        {
            GameAccount ga = accounts.Last();
            displayAccounts.Add(ga);

        }


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
                    LoginInfo info = myKingInterface.getLogin_login(oS, sid);
                    accounts.Last().Account = info.account;
                    accounts.Last().Server = info.serverTitle;
                    accounts.Last().NickName = info.nickName;
                    accounts.Last().CorpsName = info.CORPS_NAME;
                    accounts.Last().Level = info.LEVEL;
                    accounts.Last().VipLevel = info.VIP_LEVEL;

                    Application.Current.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                        (Action)(() => UpdateAccountList()));
                }
            }
        }

        private void btnDetect_Click(object sender, RoutedEventArgs e)
        {
            if (gameStatus == GameStatus.Idle)
            {
                // TODO: Detect running accounts in current computer using FiddlerCore
                if (!myFiddler.IsStarted())
                {
                    SetGameStatus(GameStatus.DetectAccount);
                    myFiddler.AfterSessionComplete += AfterSessionCompleteHandler;
                    myFiddler.ConfigFiddler("IcanTW");
                    myFiddler.Startup(true);
                }

            } else 
            {
                myFiddler.Shutdown();
                SetGameStatus(GameStatus.Idle);
            }

        }
    }
}
