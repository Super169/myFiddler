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
                    btnGetHeroInfo.IsEnabled = (lvPlayers.Items.Count > 0);
                    btnDecreeInfo.IsEnabled = (lvPlayers.Items.Count > 0);
                    break;
                case GameStatus.DetectAccount:
                    btnDetect.Content = "停止偵測";
                    btnGetHeroInfo.IsEnabled = false;
                    btnDecreeInfo.IsEnabled = false;
                    break;

            }
        }

        void UpdateAccountList(GameAccount oGA)
        {
            lock(accountsLocker)
            {
                GameAccount oExists = displayAccounts.SingleOrDefault(x => x.Account == oGA.Account);
                if (oExists != null) displayAccounts.Remove(oExists);
                displayAccounts.Add(oGA);
            }
        }


        // AfterSessionCompleteHandler only used for account detection
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

                AccountKey oAK = null;
                lock(accountsLocker)
                {
                    if (!accounts.Exists(x => x.sid == sid))
                    {
                        oAK = new AccountKey() { sid = sid };
                        accounts.Add(oAK);
                    }
                }

                if (oAK == null) return;
                LoginInfo info = myKingInterface.getLogin_login(oS, sid);

                if (info.sid == null)
                {
                    // Error reading sid, remove the key
                    lock (accountsLocker)
                    {
                        accounts.Remove(oAK);
                        return;
                    }
                }

                GameAccount oGA = new GameAccount()
                {
                    Sid = sid,
                    Account = info.account,
                    Server = info.serverTitle,
                    NickName = info.nickName,
                    Level = info.LEVEL,
                    VipLevel = info.VIP_LEVEL,
                    Heros = new List<HeroInfo>(),
                    Session = oS
                };

                AccountKey oFindAccount = accounts.SingleOrDefault(x => x.account == info.account);
                lock(accountsLocker)
                {
                    if (oFindAccount == null)
                    { 
                        oAK.account = info.account;
                    }
                    else
                    {
                        oFindAccount.sid = info.sid;
                        accounts.Remove(oAK);
                    }
                }
                Application.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    (Action)(() => UpdateAccountList(oGA)));
            }
        }

    }
}
