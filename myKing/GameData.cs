using Fiddler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace myKing
{

    [Serializable]
    public class GameAccountProfile
    {
        public string Account { get; set; }
        public int[] BossWarHeros = new int[7];
        public int BossWarChiefIdx = -1;
        public string BossWarBody = "";

        public void fromGameAccount(GameAccount oGA)
        {
            this.Account = oGA.Account;
            for (int i = 0; i < 7; i++) this.BossWarHeros[i] = oGA.BossWarHeros[i];
            this.BossWarChiefIdx = oGA.BossWarChiefIdx;
            this.BossWarBody = oGA.BossWarBody;
        }

        public void toGameAccount(GameAccount oGA)
        {
            if (oGA.Account == this.Account)
            {
                for (int i = 0; i < 7; i++) oGA.BossWarHeros[i] = this.BossWarHeros[i];
                oGA.BossWarChiefIdx = this.BossWarChiefIdx;
                oGA.BossWarBody = this.BossWarBody;
            }
        }

    }

    public class GameAccount
    {
        public string Sid { get; set; }
        public string Account { get; set; }
        public string Server { get; set; }
        public string NickName { get; set; }
        public string CorpsName { get; set; }
        public string Level { get; set; }
        public string VipLevel { get; set; }
        public List<HeroInfo> Heros { get; set; }
        public List<DecInfo> decHeros { get; set; }
        public int[] BossWarHeros = new int[7];
        public int BossWarChiefIdx = -1;
        public string BossWarBody { get; set; }
        public Session Session { get; set; }

        public string HeroName(int heroIdx)
        {
            HeroInfo hi = this.Heros.SingleOrDefault(x => x.idx == heroIdx);
            if (hi == null) return "????";
            return hi.nm;
        }
    }

    public class AccountKey
    {
        public string account { get; set; }
        public string sid { get; set;  }
    }

    public class HeroInfo
    {
        public int idx { get; set; }
        public string nm { get; set; }
        public string army { get; set; }
        public int lv { get; set; }
        public int power { get; set; }
        public int cfd { get; set; }
        public int intl { get; set; }
        public int strg { get; set; }
        public int chrm { get; set; }
        public int attk { get; set; }
        public int dfnc { get; set; }
        public int spd { get; set; }
    }

    public class DecInfo
    {
        public int decId { get; set; }
        public int[] heroIdx = new int[5];
    }
}
