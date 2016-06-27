using Fiddler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace SazCompare
{
    /// <summary>
    /// Interaction logic for UcSessionData.xaml
    /// </summary>
    public partial class UcSessionData : UserControl
    {
        private List<SessionData> sessionList;

        public UcSessionData()
        {
            InitializeComponent();
        }

        public void loadData(string fileName)
        {
            Session[] sessions = Fiddler.Utilities.ReadSessionArchive(fileName, false);
            if (sessions == null) return;

            sessionList = new List<SessionData>();
            foreach (Session oS in sessions)
            {
                string requestText = Encoding.UTF8.GetString(oS.requestBodyBytes);
                string action = null;
                try
                {
                    dynamic json = Json.Decode(requestText);
                    action = json["act"];
                }
                catch
                {
                    action = null;
                }
                if (action != null)
                {
                    string responseText = Encoding.UTF8.GetString(oS.responseBodyBytes);
                    SessionData sd = new SessionData()
                    {
                        action = action,
                        requestText = requestText,
                        responseText = responseText
                    };
                    sessionList.Add(sd);
                }
            }
            lvSession.ItemsSource = sessionList;

            ICollectionView view = CollectionViewSource.GetDefaultView(lvSession.ItemsSource);
            view.Refresh();

        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtRequest.Text = "";
            txtResponse.Text = "";
            SessionData sd =  (SessionData) lvSession.SelectedItem;
            txtRequest.Text = sd.requestText;
            txtResponse.Text = sd.responseText;
            lvSession.ScrollIntoView(sd);
        }

        public int SelectedIndex()
        {
            return lvSession.SelectedIndex;
        }

        public void goNext()
        {
            lvSession.SelectedIndex = lvSession.SelectedIndex + 1;
            /*
            ListViewItem item = lvSession.ItemContainerGenerator.ContainerFromIndex(lvSession.SelectedIndex) as ListViewItem;
            if (item != null) lvSession.ScrollIntoView(item);
            */
        }
    }
}
