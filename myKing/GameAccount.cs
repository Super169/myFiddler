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
        public Session Session { get; set; }
    }

    class AccountKey
    {
        public string account { get; set; }
        public string sid { get; set;  }
    }

}
