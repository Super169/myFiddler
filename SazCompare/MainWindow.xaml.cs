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
using System.ComponentModel;

namespace SazCompare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            string sSAZInfo = Assembly.GetAssembly(typeof(Ionic.Zip.ZipFile)).FullName;
            FiddlerApplication.oSAZProvider = new DNZSAZProvider();
        }

        private void btnLoad_A_Click(object sender, RoutedEventArgs e)
        {
            uc_A.loadData("a.saz");
        }

        private void btnLoad_B_Click(object sender, RoutedEventArgs e)
        {
            uc_B.loadData("b.saz");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (uc_A.SelectedIndex() < 0) return;
            if (uc_B.SelectedIndex() < 0) return;
            uc_A.goNext();
            uc_B.goNext();
        }
    }
}
