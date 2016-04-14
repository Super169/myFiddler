using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            txtResult.Text = myKingInterface.getHeroInfo(oGA.Session, oGA.Sid);
            myFiddler.Shutdown();

        }
    }

}
