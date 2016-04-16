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
using System.Windows.Shapes;

namespace myKing
{
    /// <summary>
    /// Interaction logic for BossWarSettings.xaml
    /// </summary>
    public partial class BossWarSettings : Window
    {
        GameAccount oGA;

        WarHero[] warHeros = new WarHero[7];
        WarHero selectedWH = null;

        public void setData(GameAccount oGA)
        {
            this.oGA = oGA;
            lvHero.ItemsSource = oGA.Heros;
        }

        public BossWarSettings()
        {
            InitializeComponent();
            warHeros[0] = wh00;
            warHeros[1] = wh01;
            warHeros[2] = wh02;
            warHeros[3] = wh03;
            warHeros[4] = wh04;
            warHeros[5] = wh05;
            warHeros[6] = wh06;
            foreach (WarHero wh in warHeros)
            {
                wh.SetHero(0, "");
            }
        }

        private void warHero_Click(object sender, EventArgs e)
        {
            WarHero wh = (WarHero)sender;
            bool selection = !wh.selected;
            if (selectedWH != null) selectedWH.SetSelected(false);
            wh.SetSelected(selection);
            if (selection) selectedWH = wh;
            else selectedWH = null;
        }

        private void WarHeroClick(int idx)
        {
            wh00.SetSelected(!wh00.selected);

            if (wh00.heroIdx == 0) return;
            if (wh00.chief)
            {
                wh00.SetChief(false);
            }
            else
            {
                foreach (WarHero wh in warHeros) wh.SetChief(false);
                wh00.SetChief(true);
            }
            setStatus();
        }

        private void setStatus()
        {
            btnChief.IsEnabled = ((selectedWH != null)  && (selectedWH.heroIdx > 0));
            btnClear.IsEnabled = (selectedWH != null);
        }

    }

}
