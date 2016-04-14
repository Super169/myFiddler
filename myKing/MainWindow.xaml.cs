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

        List<GameAccount> accounts = new List<GameAccount>();
        Object accountsLocker = new Object();
           
        public MainWindow()
        {
            InitializeComponent();
            lvPlayers.ItemsSource = displayAccounts;
        }

    }

}
