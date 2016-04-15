using System;
using System.Collections.Generic;
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
    /// Interaction logic for WarHero.xaml
    /// </summary>
    public partial class WarHero : UserControl
    {

        public event EventHandler Click;

        public int heroIdx = 0;
        public string nm = "";
        public bool chief = false;
        public bool selected = false;
        
        public WarHero()
        {
            InitializeComponent();
            SetColor();
            SetSelected(false);
        }

        public void SetHero(int heroIdx, string nm)
        {
            this.heroIdx = heroIdx;
            this.nm = nm;
            button.Content = nm;
            SetColor();
        }

        public void SetChief(bool chief)
        {
            this.chief = chief;
            SetColor();
        }

        private void SetColor()
        {
            if (heroIdx == 0)
            {
                button.Background = Brushes.Gray;
            }
            else if (chief)
            {
                button.Background = Brushes.Red;
            }
            else
            {
                button.Background = Brushes.White;
            }

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (Click != null) Click(this, EventArgs.Empty);
        }

        public void SetSelected(bool selected = false)
        {
            this.selected = selected;
            if (selected)
            {
                button.BorderThickness = new Thickness(5, 5, 5, 5);
            } else
            {
                button.BorderThickness = new Thickness(0, 0, 0, 0);
            }
        }
    }
}
