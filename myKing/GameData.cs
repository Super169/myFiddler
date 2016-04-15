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

    class GameAccount
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
        public Session Session { get; set; }
    }

    class AccountKey
    {
        public string account { get; set; }
        public string sid { get; set;  }
    }

    class HeroInfo
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

    class DecInfo
    {
        public int decId { get; set; }
        public int[] heroIdx = new int[5];
    }
}
