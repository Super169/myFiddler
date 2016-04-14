using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        ObservableCollection<GameAccount> displayAccounts = new ObservableCollection<GameAccount>();

        List<AccountKey> accounts = new List<AccountKey>();

        // List<GameAccount> accounts = new List<GameAccount>();


        Object accountsLocker = new Object();
           
        public MainWindow()
        {
            InitializeComponent();
            lvPlayers.ItemsSource = displayAccounts;
            SetGameStatus(GameStatus.Idle);
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
                    myFiddler.ConfigFiddler("King.IcanTW");
                    myFiddler.Startup(true);
                }

            }
            else
            {
                myFiddler.Shutdown();
                SetGameStatus(GameStatus.Idle);
            }

        }

        private void btnGetHeroInfo_Click(object sender, RoutedEventArgs e)
        {
            GameAccount oGA = (GameAccount) lvPlayers.SelectedItem;
            if (oGA == null) return;
            myFiddler.Startup(false);
            myKingInterface.requestReturnObject rro = myKingInterface.getHeroInfo(oGA.Session, oGA.Sid);
            myFiddler.Shutdown();
            if (rro.success)
            {
                string info = "";
                DynamicJsonArray heros = myKingInterface.ExtractHeros(rro.session);
                if (heros != null)
                {
                    oGA.Heros.Clear();
                    foreach (dynamic hero in heros)
                    {
                        HeroInfo hi = new HeroInfo() { idx = hero.idx, nm = hero.nm, lv = hero.lv, power = hero.power, cfd = hero.cfd,
                                                       intl = hero.intl, strg = hero.strg, chrm = hero.chrm, attk = hero.attk, dfnc = hero.dfnc, spd = hero.spd
                                                     };
                        info += string.Format("{0} : {1} : {2} : {3} : {4} : {5}", hi.idx, hi.nm, hi.army, hi.lv, hi.power, hi.cfd);
                        info += string.Format(" : {0} : {1} : {2} : {3} : {4} : {5}", hi.intl, hi.strg, hi.chrm, hi.attk, hi.dfnc, hi.spd);
                        info += "\n";
                        oGA.Heros.Add(hi);
                    };
                 }
                 txtResult.Text = info;
            } else
            {
                txtResult.Text = "Fail getting Hero Information:\n" + rro.msg;
            }
        }

        private void btnDecreeInfo_Click(object sender, RoutedEventArgs e)
        {
            GameAccount oGA = (GameAccount)lvPlayers.SelectedItem;
            if (oGA == null) return;
            myFiddler.Startup(false);
            myKingInterface.requestReturnObject rro = myKingInterface.getDecreeInfo(oGA.Session, oGA.Sid);
            if (rro.success)
            {
                txtResult.Text = myKingInterface.CleanUpResponse(Encoding.UTF8.GetString(rro.session.responseBodyBytes));
            }
            myFiddler.Shutdown();
        }
    }

}
