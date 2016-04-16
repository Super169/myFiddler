using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace myKing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<GameAccount> gameAccounts = new ObservableCollection<GameAccount>();

        List<AccountKey> accounts = new List<AccountKey>();

        Object accountsLocker = new Object();

        enum GameStatus
        {
            Idle, DetectAccount, Waiting
        }

        GameStatus gameStatus = GameStatus.Idle;

        public MainWindow()
        {
            InitializeComponent();
            this.Title = "大重帝輔助工具 之 神將無雙  v" + Assembly.GetExecutingAssembly().GetName().Version;
            lvPlayers.ItemsSource = gameAccounts;
            SetGameStatus(GameStatus.Idle);
        }

        void SetGameStatus(GameStatus newStatus)
        {
            this.gameStatus = newStatus;
            switch (newStatus)
            {
                case GameStatus.Idle:
                    // Just set the background as other button which will not change color
                    btnDetect.Background = btnGetHeroInfo.Background;
                    btnDetect.Content = "偵測帳戶";
                    btnGetHeroInfo.IsEnabled = (lvPlayers.Items.Count > 0);
                    btnDecreeInfo.IsEnabled = (lvPlayers.Items.Count > 0);
                    // btnBossWar.IsEnabled = (lvPlayers.Items.Count > 0);
                    btnBossWarSettings.IsEnabled = (lvPlayers.Items.Count > 0);
                    break;
                case GameStatus.DetectAccount:
                    btnDetect.Background = Brushes.Red;
                    btnDetect.Content = "停止偵測";
                    btnGetHeroInfo.IsEnabled = false;
                    btnDecreeInfo.IsEnabled = false;
                    // btnBossWar.IsEnabled = false;
                    btnBossWarSettings.IsEnabled = false;
                    break;

            }
        }

        private void btnDetect_Click(object sender, RoutedEventArgs e)
        {
            if (gameStatus == GameStatus.Idle)
            {
                SetGameStatus(GameStatus.DetectAccount);
                myFiddler.AfterSessionComplete += AfterSessionCompleteHandler;
                myFiddler.ConfigFiddler("King.IcanTW");
                myFiddler.Startup(true);
            }
            else
            {
                myFiddler.Shutdown();
                getPlayerDetails();
                SetGameStatus(GameStatus.Idle);
            }

        }

        private void getPlayerDetails()
        {
            foreach (GameAccount oGA in gameAccounts)
            {
                if (oGA.Heros.Count == 0) myKingInterface.readHerosInfo(oGA);
            }
        }

        private bool herosReady(GameAccount oGA, bool updateInfo = false)
        {
            if (oGA.Heros.Count == 0) myKingInterface.readHerosInfo(oGA);
            if (updateInfo && (oGA.Heros.Count==0)) txtResult.Text = (gameAccounts.Count == 0 ? "請先偵測帳戶" : "讀取英雄資料失敗");
            return (oGA.Heros.Count > 0);
        }

        private GameAccount getSelectedAccount(bool updateInfo = false)
        {
            GameAccount oGA = (GameAccount)lvPlayers.SelectedItem;
            if (updateInfo && (oGA == null)) txtResult.Text = (gameAccounts.Count == 0 ? "請先偵測帳戶" : "請先選擇帳戶");
            return oGA;
        }

        private void btnGetHeroInfo_Click(object sender, RoutedEventArgs e)
        {

            GameAccount oGA = getSelectedAccount(true);
            if (oGA == null) return;

            if (!herosReady(oGA, true)) return;

            string info = "";
            foreach (HeroInfo hi in oGA.Heros)
            {
                info += string.Format("{0} : {1} : {2} : {3} : {4} : {5}", hi.idx, hi.nm, hi.army, hi.lv, hi.power, hi.cfd);
                info += string.Format(" : {0} : {1} : {2} : {3} : {4} : {5}", hi.intl, hi.strg, hi.chrm, hi.attk, hi.dfnc, hi.spd);
                info += "\n";
            }
            txtResult.Text = info;
        }

        private void btnDecreeInfo_Click(object sender, RoutedEventArgs e)
        {
            string info = "";

            GameAccount oGA = getSelectedAccount(true);
            if (oGA == null) return;

            oGA.decHeros.Clear();

            if (!herosReady(oGA, true)) return;

            myFiddler.Startup(false);
            myKingInterface.requestReturnObject rro = myKingInterface.getDecreeInfo(oGA.Session, oGA.Sid);
            myFiddler.Shutdown();

            if (rro.success)
            {
//                txtResult.Text = myKingInterface.CleanUpResponse(Encoding.UTF8.GetString(rro.session.responseBodyBytes));
                try
                {
                    dynamic json = myKingInterface.getJsonFromResponse(rro.session);

                    if (json != null)
                    {
                        DynamicJsonArray decHeros = json.decHeros;

                        foreach (dynamic decree in decHeros)
                        {
                            DecInfo decInfo = new DecInfo() { decId = decree.decId };
                            info += decInfo.decId.ToString() + ": ";
                            DynamicJsonArray heros = decree.heros;
                            foreach (dynamic hero in heros)
                            {
                                int heroIdx = (hero.open ? hero.heroIdx : -1);

                                decInfo.heroIdx[hero.pos - 1] = heroIdx;
                                if (heroIdx > 0)
                                {
                                    info += "[" + oGA.HeroName(heroIdx) + "]";
                                } else if (heroIdx == 0)
                                {
                                    info += "[+] ";
                                } else
                                {
                                    info += "[-] ";
                                }
                            }
                            oGA.decHeros.Add(decInfo);
                            info += "\n";
                        }

                    }

                } catch (Exception ex)
                {
                    info = "Error reading decreeInfo:\n" + ex.Message;
                }
            } else
            {
                info = "Error reading decreeInfo:\n" + rro.msg;
            }
            txtResult.Text = info;

        }

        private void btnBossWarSettings_Click(object sender, RoutedEventArgs e)
        {

            GameAccount oGA = getSelectedAccount(true);
            if ((oGA == null) || (!herosReady(oGA, true))) return;

            BossWarSettings Window = new BossWarSettings(); 
            Window.Owner = this;
            Window.Title = "神將無雙佈陣";
            Window.setData(oGA);
            Window.Show();

        }

        private void btnBossWar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("功能尚未公開");
            //BossWarSettings Window = new BossWarSettings();
            //Window.Owner = this;
            //Window.Title = "神將無雙佈陣";
            //Window.Show();
        }
    }

}
