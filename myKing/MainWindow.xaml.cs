using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
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
        List<GameAccountProfile> gameAccountProfiles = new List<GameAccountProfile>();

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
            btnSaveProfile.Visibility = Visibility.Hidden;
            btnRestoreProfile.Visibility = Visibility.Hidden;
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
                    btnBossWar.IsEnabled = (lvPlayers.Items.Count > 0);
                    btnBossWarSettings.IsEnabled = (lvPlayers.Items.Count > 0);
                    break;
                case GameStatus.DetectAccount:
                    btnDetect.Background = Brushes.Red;
                    btnDetect.Content = "停止偵測";
                    btnGetHeroInfo.IsEnabled = false;
                    btnDecreeInfo.IsEnabled = false;
                    btnBossWar.IsEnabled = false;
                    btnBossWarSettings.IsEnabled = false;
                    break;

            }
        }

        private void btnDetect_Click(object sender, RoutedEventArgs e)
        {
            if (gameStatus == GameStatus.Idle)
            {
                UpdateResult("偵測帳戶 - 開始", true);
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
                if (lvPlayers.SelectedIndex == -1) lvPlayers.SelectedIndex = 0;
                RestoreProfile();
                UpdateResult("偵測帳戶 - 結束", true);
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
            if (updateInfo && (oGA.Heros.Count==0)) MessageBox.Show((gameAccounts.Count == 0 ? "請先偵測帳戶" : "讀取英雄資料失敗"));
            return (oGA.Heros.Count > 0);
        }

        private GameAccount getSelectedAccount(bool updateInfo = false)
        {
            GameAccount oGA = (GameAccount)lvPlayers.SelectedItem;
            if (updateInfo && (oGA == null)) MessageBox.Show((gameAccounts.Count == 0 ? "請先偵測帳戶" : "請先選擇帳戶"));
            return oGA;
        }


        #region "Profile Related"

        private void btnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            SaveProfile();
        }

        private void btnRestoreProfile_Click(object sender, RoutedEventArgs e)
        {
            RestoreProfile();
        }


        private void SaveProfile()
        {
            if (gameAccounts.Count == 0) return;

            foreach (GameAccount oGA in gameAccounts)
            {
                GameAccountProfile oGAP = gameAccountProfiles.SingleOrDefault(x => x.Account == oGA.Account);
                if (oGAP == null)
                {
                    oGAP = new GameAccountProfile();
                    oGAP.fromGameAccount(oGA);
                    gameAccountProfiles.Add(oGAP);
                }
                else
                {
                    oGAP.fromGameAccount(oGA);
                }
            }

            FileStream fs = null;
            try
            {
                fs = new FileStream("myKing.dat", FileMode.OpenOrCreate);
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, gameAccountProfiles);
                UpdateResult("資料儲存成功", true);
            }
            catch (Exception ex)
            {
                UpdateResult("Error saving data:\n" + ex.Message, true);
            }
            finally
            {
                if (fs != null) fs.Close();
            }
        }

        private void RestoreProfile()
        {
            gameAccountProfiles.Clear();

            FileStream fs = null;
            try
            {
                fs = new FileStream("myKing.dat", FileMode.Open);
                BinaryFormatter formatter = new BinaryFormatter();
                gameAccountProfiles = (List<GameAccountProfile>)formatter.Deserialize(fs);
            }
            catch (Exception ex)
            {
                UpdateResult("Error reading data:\n" + ex.Message, true);
                return;
            }
            finally
            {
                if (fs != null) fs.Close();
            }


            int updCnt = 0;
            string updAccount = "";
            foreach (GameAccountProfile oGAP in gameAccountProfiles)
            {
                GameAccount oGA = gameAccounts.SingleOrDefault(x => x.Account == oGAP.Account);
                if (oGA != null)
                {
                    updCnt++;
                    oGAP.toGameAccount(oGA);
                    updAccount += oGA.Server + ": " + oGA.NickName + "\n";
                }
            }
            refreshAccountList();
            UpdateResult(string.Format("資料成功讀取, 以下 {0} 個帳記更新了\n{1}", updCnt, updAccount), true);
        }


        #endregion


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
            UpdateResult(info);
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
            UpdateResult(info);

        }

        private void btnBossWarSettings_Click(object sender, RoutedEventArgs e)
        {
            GameAccount oGA = getSelectedAccount(true);
            if ((oGA == null) || (!herosReady(oGA, true))) return;

            BossWarSettings Window = new BossWarSettings(); 
            Window.Owner = this;
            Window.Title = "神將無雙佈陣";
            Window.setData(oGA);
            bool? dialogResult = Window.ShowDialog();
            if (dialogResult == true)
            {
                refreshAccountList();
                SaveProfile();
            }
        }

        private void btnBossWar_Click(object sender, RoutedEventArgs e)
        {
            string acInfo;
            if (gameAccounts.Count == 0)
            {
                UpdateResult("神將無雙 - 開始失敗, 未有帳戶資料", true);
                return;
            }

            UpdateResult("神將無雙 - 開始\n", true);
            foreach (GameAccount oGA in gameAccounts)
            {
                acInfo = oGA.Server + " | " + oGA.NickName + " | ";
                if (herosReady(oGA, true))
                {
                    if ((oGA.BossWarBody == null) || (oGA.BossWarBody == ""))
                    {
                        UpdateResult(acInfo + "FAIL | 沒有神將無雙的佈陣資料", true);
                    } else
                    {
                        UpdateResult(acInfo + "神將無雙 - 出兵 - 開始");
                        bool warSuccess;
                        string info = "";
                        myFiddler.Startup(false);
                        warSuccess = myKingInterface.GoBossWarOnce(oGA.Session, oGA.Sid, oGA.BossWarBody, out info);
                        myFiddler.Shutdown();
                        UpdateResult(info, false);
                        UpdateResult(acInfo + "神將無雙 - 出兵 - 結束\n");
                    }
                }
                else
                {
                    UpdateResult(acInfo + "FAIL | 沒有英雄資料", true);
                }
            }
            /*
                        GameAccount oGA = getSelectedAccount(true);
                        if ((oGA == null) || (!herosReady(oGA, true))) return;
                        if ((oGA.BossWarBody == null) || (oGA.BossWarBody == ""))
                        {
                            MessageBox.Show("請先設定神將無雙的佈陣");
                            return;
                        }
                        string info = "";
                        bool warSuccess;
                        myFiddler.Startup(false);
                        warSuccess = myKingInterface.GoBossWarOnce(oGA.Session, oGA.Sid, oGA.BossWarBody, out info);
                        myFiddler.Shutdown();
                        UpdateResult(info);
            */
            UpdateResult("神將無雙 - 結束");
        }

        private void UpdateResult(string info, bool addTime = false, bool reset = false)
        {
            if (reset) txtResult.Text = "";
            if (addTime) txtResult.Text += DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss | ");
            txtResult.Text += info + "\n";
            txtResult.ScrollToEnd();
        }

        
    }

}
